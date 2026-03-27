using Material_Editor.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Material_Editor.Controls
{
    internal enum BulkMaterialRowFilter
    {
        AllFiles,
        UniqueFilesOnly,
        DuplicateFiles,
        DirtyFiles,
        ErroredFiles,
        CustomFiles
    }

    internal enum BulkMaterialSortKey
    {
        Alphabetical,
        LastModified,
        CreationDate,
        ByAddition
    }

    internal enum BulkMaterialSortDirection
    {
        Ascending,
        Descending
    }

    internal sealed partial class BulkMaterialEditorView
    {
        private BulkMaterialRowFilter activeFilter = BulkMaterialRowFilter.AllFiles;
        private BulkMaterialSortKey activeSortKey = BulkMaterialSortKey.Alphabetical;
        private BulkMaterialSortDirection activeSortDirection = BulkMaterialSortDirection.Ascending;
        private bool groupByFolder;
        private HashSet<string> customFilteredPaths = new(StringComparer.OrdinalIgnoreCase);
        private string customFilterName = SearchResultsFilterName;
        private List<BulkMaterialEditRow> visibleRows = new();

        public IReadOnlyList<BulkMaterialEditRow> VisibleRows => visibleRows;
        public BulkMaterialRowFilter ActiveFilter => activeFilter;
        public BulkMaterialSortKey ActiveSortKey => activeSortKey;
        public BulkMaterialSortDirection ActiveSortDirection => activeSortDirection;
        public bool IsGroupedByFolder => groupByFolder;
        public bool HasCustomRowFilter => customFilteredPaths.Count > 0;
        public string CustomFilterName => customFilterName;
        public bool HasActiveVisibilityFilter => activeFilter != BulkMaterialRowFilter.AllFiles;

        public void CycleRowFilter(int direction)
        {
            if (session == null)
                return;

            List<BulkMaterialRowFilter> filters = GetCycleableRowFilters();
            if (filters.Count == 0)
                return;

            int currentIndex = filters.IndexOf(activeFilter);
            if (currentIndex < 0)
                currentIndex = 0;

            int step = direction < 0 ? -1 : 1;
            int nextIndex = (currentIndex + step + filters.Count) % filters.Count;
            SetRowFilter(filters[nextIndex]);
        }

        public void SetRowFilter(BulkMaterialRowFilter filter)
        {
            if (filter == BulkMaterialRowFilter.CustomFiles && customFilteredPaths.Count == 0)
                return;

            if (activeFilter == filter)
                return;

            activeFilter = filter;
            RebuildProjectedGridPreservingSelection();
        }

        public void SetSortKey(BulkMaterialSortKey sortKey)
        {
            if (activeSortKey == sortKey)
                return;

            activeSortKey = sortKey;
            RebuildProjectedGridPreservingSelection();
        }

        public void SetSortDirection(BulkMaterialSortDirection direction)
        {
            if (activeSortDirection == direction)
                return;

            activeSortDirection = direction;
            RebuildProjectedGridPreservingSelection();
        }

        public void SetGroupByFolder(bool enabled)
        {
            if (groupByFolder == enabled)
                return;

            groupByFolder = enabled;
            RebuildProjectedGridPreservingSelection();
        }

        private void ResetProjectionState()
        {
            activeFilter = BulkMaterialRowFilter.AllFiles;
            activeSortKey = BulkMaterialSortKey.Alphabetical;
            activeSortDirection = BulkMaterialSortDirection.Ascending;
            groupByFolder = false;
            customFilteredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            customFilterName = SearchResultsFilterName;
            visibleRows = new List<BulkMaterialEditRow>();
        }

        public void SetCustomRowFilter(IEnumerable<string> filePaths, string filterName = null)
        {
            var nextPaths = new HashSet<string>(
                (filePaths ?? Array.Empty<string>())
                    .Where(path => !string.IsNullOrWhiteSpace(path)),
                StringComparer.OrdinalIgnoreCase);
            if (nextPaths.Count == 0)
                return;

            bool samePaths = nextPaths.SetEquals(customFilteredPaths);
            string normalizedName = string.IsNullOrWhiteSpace(filterName) ? SearchResultsFilterName : filterName;
            bool sameName = string.Equals(customFilterName, normalizedName, StringComparison.Ordinal);
            customFilteredPaths = nextPaths;
            customFilterName = normalizedName;

            if (activeFilter == BulkMaterialRowFilter.CustomFiles && samePaths && sameName)
                return;

            activeFilter = BulkMaterialRowFilter.CustomFiles;
            RebuildProjectedGridPreservingSelection();
        }

        private List<BulkMaterialRowFilter> GetCycleableRowFilters()
        {
            var filters = new List<BulkMaterialRowFilter>
            {
                BulkMaterialRowFilter.AllFiles,
                BulkMaterialRowFilter.UniqueFilesOnly,
                BulkMaterialRowFilter.DuplicateFiles,
                BulkMaterialRowFilter.DirtyFiles,
                BulkMaterialRowFilter.ErroredFiles
            };

            if (HasCustomRowFilter)
                filters.Add(BulkMaterialRowFilter.CustomFiles);

            return filters;
        }

        private string GetFilterDisplayName(BulkMaterialRowFilter filter)
        {
            return filter switch
            {
                BulkMaterialRowFilter.UniqueFilesOnly => "Unique Files Only",
                BulkMaterialRowFilter.DuplicateFiles => "Duplicate Files",
                BulkMaterialRowFilter.DirtyFiles => "Dirty Files",
                BulkMaterialRowFilter.ErroredFiles => "Errored Files",
                BulkMaterialRowFilter.CustomFiles => CustomFilterName,
                _ => "All Files"
            };
        }

        private void RebuildProjectedGridPreservingSelection()
        {
            if (session == null)
            {
                RebuildGrid();
                RefreshSummary();
                return;
            }

            CommitPendingEdits();

            List<(string FilePath, string ColumnName)> cellReferences = grid.SelectedCells
                .Cast<System.Windows.Forms.DataGridViewCell>()
                .Select(cell => BuildCellReference(cell.RowIndex, cell.ColumnIndex))
                .Where(reference => reference.HasValue)
                .Select(reference => reference.Value)
                .Distinct()
                .ToList();
            string[] rowReferences = SelectedRows
                .Select(row => row.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            (string FilePath, string ColumnName)? currentCellReference = grid.CurrentCell == null
                ? null
                : BuildCellReference(grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex);

            RebuildGrid();

            if (cellReferences.Count > 0)
                ReselectCells(cellReferences);
            else if (rowReferences.Length > 0)
                ReselectRows(rowReferences);

            if (currentCellReference.HasValue)
                RestoreCurrentCell(currentCellReference.Value.FilePath, currentCellReference.Value.ColumnName);

            RefreshSummary();
        }

        private List<BulkMaterialEditRow> BuildVisibleRows()
        {
            if (session == null)
                return new List<BulkMaterialEditRow>();

            Dictionary<string, int> fileNameCounts = session.Rows
                .GroupBy(row => row.FileName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            IEnumerable<BulkMaterialEditRow> filteredRows = session.Rows.Where(row => MatchesActiveFilter(row, fileNameCounts));
            return groupByFolder
                ? OrderRowsByFolder(filteredRows)
                : OrderRows(filteredRows);
        }

        private bool MatchesActiveFilter(BulkMaterialEditRow row, IReadOnlyDictionary<string, int> fileNameCounts)
        {
            if (row == null)
                return false;

            return activeFilter switch
            {
                BulkMaterialRowFilter.UniqueFilesOnly => fileNameCounts.TryGetValue(row.FileName ?? string.Empty, out int uniqueCount) && uniqueCount == 1,
                BulkMaterialRowFilter.DuplicateFiles => fileNameCounts.TryGetValue(row.FileName ?? string.Empty, out int duplicateCount) && duplicateCount > 1,
                BulkMaterialRowFilter.DirtyFiles => IsRowEffectivelyDirty(row),
                BulkMaterialRowFilter.ErroredFiles => row.HasLoadError || row.HasValidationErrors,
                BulkMaterialRowFilter.CustomFiles => customFilteredPaths.Contains(row.FilePath),
                _ => true
            };
        }

        private List<BulkMaterialEditRow> OrderRowsByFolder(IEnumerable<BulkMaterialEditRow> rows)
        {
            var ordered = new List<BulkMaterialEditRow>();
            foreach (IGrouping<string, BulkMaterialEditRow> group in (rows ?? Array.Empty<BulkMaterialEditRow>())
                .GroupBy(row => row.DisplayFolder ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
            {
                ordered.AddRange(OrderRows(group));
            }

            return ordered;
        }

        private List<BulkMaterialEditRow> OrderRows(IEnumerable<BulkMaterialEditRow> rows)
        {
            IEnumerable<BulkMaterialEditRow> source = rows ?? Array.Empty<BulkMaterialEditRow>();
            return activeSortKey switch
            {
                BulkMaterialSortKey.LastModified => activeSortDirection == BulkMaterialSortDirection.Ascending
                    ? source.OrderBy(row => row.LastWriteTimeUtc).ThenBy(row => row.DisplayPath, StringComparer.OrdinalIgnoreCase).ToList()
                    : source.OrderByDescending(row => row.LastWriteTimeUtc).ThenBy(row => row.DisplayPath, StringComparer.OrdinalIgnoreCase).ToList(),
                BulkMaterialSortKey.CreationDate => activeSortDirection == BulkMaterialSortDirection.Ascending
                    ? source.OrderBy(row => row.CreationTimeUtc).ThenBy(row => row.DisplayPath, StringComparer.OrdinalIgnoreCase).ToList()
                    : source.OrderByDescending(row => row.CreationTimeUtc).ThenBy(row => row.DisplayPath, StringComparer.OrdinalIgnoreCase).ToList(),
                BulkMaterialSortKey.ByAddition => activeSortDirection == BulkMaterialSortDirection.Ascending
                    ? source.OrderBy(row => row.InsertionIndex).ThenBy(row => row.DisplayPath, StringComparer.OrdinalIgnoreCase).ToList()
                    : source.OrderByDescending(row => row.InsertionIndex).ThenBy(row => row.DisplayPath, StringComparer.OrdinalIgnoreCase).ToList(),
                _ => activeSortDirection == BulkMaterialSortDirection.Ascending
                    ? source.OrderBy(row => row.DisplayPath, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.FilePath, StringComparer.OrdinalIgnoreCase).ToList()
                    : source.OrderByDescending(row => row.DisplayPath, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.FilePath, StringComparer.OrdinalIgnoreCase).ToList()
            };
        }
    }
}
