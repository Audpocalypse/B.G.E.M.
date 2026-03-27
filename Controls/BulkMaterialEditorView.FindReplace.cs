using Material_Editor.Dialogs;
using Material_Editor.Models;
using Material_Editor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal sealed partial class BulkMaterialEditorView
    {
        private const string SearchResultsFilterName = "Custom Search Results";

        private enum SearchNavigationDirection
        {
            Forward,
            Backward
        }

        private BulkFindReplaceRequest lastFindReplaceRequest = new();

        private sealed class BulkSearchMatch
        {
            public BulkSearchMatch(BulkMaterialEditRow row, MaterialFieldDescriptor descriptor, int rowOrder, int fieldOrder)
            {
                Row = row;
                Descriptor = descriptor;
                RowOrder = rowOrder;
                FieldOrder = fieldOrder;
            }

            public BulkMaterialEditRow Row { get; }
            public MaterialFieldDescriptor Descriptor { get; }
            public int RowOrder { get; }
            public int FieldOrder { get; }
            public string FilePath => Row.FilePath;
            public string ColumnName => GetFieldColumnName(Descriptor.Label);
        }

        private void OpenFindDialog(bool replaceMode)
        {
            if (session == null)
                return;

            using var dialog = new FindReplaceDialog(
                session.AllDescriptors,
                BuildInitialFindReplaceRequest(),
                replaceMode,
                ExecuteDialogAction);
            dialog.ShowDialog(FindForm());
        }

        private void ExecuteDialogAction(BulkFindReplaceAction action, BulkFindReplaceRequest request)
        {
            if (request == null)
                return;

            lastFindReplaceRequest = request.Clone();

            switch (action)
            {
                case BulkFindReplaceAction.FindPrevious:
                    ExecuteFindRequest(request, SearchNavigationDirection.Backward);
                    break;
                case BulkFindReplaceAction.Replace:
                    ExecuteReplaceSingleRequest(request);
                    break;
                case BulkFindReplaceAction.ReplaceAll:
                    ExecuteReplaceRequest(request);
                    break;
                case BulkFindReplaceAction.ApplyAsFilter:
                    ApplyRequestAsFilter(request);
                    break;
                default:
                    ExecuteFindRequest(request, SearchNavigationDirection.Forward);
                    break;
            }
        }

        private BulkFindReplaceRequest BuildInitialFindReplaceRequest()
        {
            BulkFindReplaceRequest request = lastFindReplaceRequest?.Clone() ?? new BulkFindReplaceRequest();
            List<DataGridViewCell> selectedFieldCells = GetSelectedFieldCells(editableOnly: false);
            if (selectedFieldCells.Count == 0)
                return request;

            var selectedDescriptors = selectedFieldCells
                .Select(cell => TryGetFieldDescriptor(grid.Columns[cell.ColumnIndex].Name))
                .Where(descriptor => descriptor != null)
                .Distinct()
                .ToList();
            if (selectedDescriptors.Count == 0)
                return request;

            request.Scope = BulkFindScope.ByField;
            request.FieldLabels = selectedDescriptors
                .Select(descriptor => descriptor.Label)
                .ToList();

            BulkFindValueType inferredValueType = InferValueType(selectedDescriptors[0]);
            if (selectedDescriptors.All(descriptor => InferValueType(descriptor) == inferredValueType))
                request.ValueType = inferredValueType;

            request.FieldLabels = request.FieldLabels
                .Where(label => IsDescriptorCompatibleForSearch(session.FindDescriptor(label), request.ValueType))
                .ToList();

            return request;
        }

        private static BulkFindValueType InferValueType(MaterialFieldDescriptor descriptor)
        {
            return descriptor?.EditorKind switch
            {
                BulkFieldEditorKind.Number => BulkFindValueType.Number,
                BulkFieldEditorKind.Boolean => BulkFindValueType.Boolean,
                _ => BulkFindValueType.Text
            };
        }

        private void ExecuteFindRequest(BulkFindReplaceRequest request)
        {
            ExecuteFindRequest(request, SearchNavigationDirection.Forward);
        }

        private void ExecuteFindRequest(BulkFindReplaceRequest request, SearchNavigationDirection direction)
        {
            List<BulkSearchMatch> matches = FindMatches(request);
            if (matches.Count == 0)
            {
                MessageBox.Show(FindForm(), BuildNoMatchesMessage(request), "Find", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            EnsureMatchedPathsVisible(matches.Select(match => match.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

            if (request.Scope == BulkFindScope.ByFile)
            {
                BulkSearchMatch fileMatch = GetRelativeFileMatch(matches, direction);
                SelectSingleResultRow(fileMatch.FilePath);
                return;
            }

            BulkSearchMatch cellMatch = GetRelativeCellMatch(matches, direction);
            EnsureFieldVisible(cellMatch.Descriptor.Label);
            SelectSingleCellByReference(cellMatch.FilePath, cellMatch.ColumnName);
        }

        private void ExecuteReplaceRequest(BulkFindReplaceRequest request)
        {
            List<BulkSearchMatch> matches = FindMatches(request);
            if (matches.Count == 0)
            {
                MessageBox.Show(FindForm(), BuildNoMatchesMessage(request), "Find and Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int replacedCount = ApplyReplaceRequest(matches, request);
            if (replacedCount == 0)
            {
                MessageBox.Show(FindForm(), "Matches were found, but none of them could be replaced with the selected value.", "Find and Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string[] matchedPaths = matches
                .Select(match => match.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            EnsureMatchedPathsVisible(matchedPaths);

            if (request.Scope == BulkFindScope.ByFile)
            {
                ReselectRows(matchedPaths);
                RestoreCurrentCell(matches[0].FilePath, PathColumnName);
            }
            else
            {
                EnsureFieldsVisible(matches.Select(match => match.Descriptor.Label));
                ReselectCells(matches.Select(match => (match.FilePath, match.ColumnName)).ToArray());
                RestoreCurrentCell(matches[0].FilePath, matches[0].ColumnName);
            }

            RefreshSummary();
        }

        private void ExecuteReplaceSingleRequest(BulkFindReplaceRequest request)
        {
            List<BulkSearchMatch> matches = FindMatches(request);
            if (matches.Count == 0)
            {
                MessageBox.Show(FindForm(), BuildNoMatchesMessage(request), "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (request.Scope == BulkFindScope.ByFile)
            {
                BulkSearchMatch targetFile = GetReplaceTargetFileMatch(matches);
                if (targetFile == null)
                {
                    MessageBox.Show(FindForm(), "No matching file is available to replace.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                BulkSearchMatch[] fileMatches = matches
                    .Where(match => string.Equals(match.FilePath, targetFile.FilePath, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                int replacedCount = ApplyReplaceRequest(fileMatches, request);
                if (replacedCount == 0)
                {
                    MessageBox.Show(FindForm(), "The selected file contains matches, but none of them could be replaced with the selected value.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                EnsureMatchedPathsVisible(new[] { targetFile.FilePath });
                SelectSingleResultRow(targetFile.FilePath);
                return;
            }

            BulkSearchMatch targetMatch = GetReplaceTargetCellMatch(matches);
            if (targetMatch == null)
            {
                MessageBox.Show(FindForm(), "No matching field is available to replace.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int singleReplaceCount = ApplyReplaceRequest(new[] { targetMatch }, request);
            if (singleReplaceCount == 0)
            {
                MessageBox.Show(FindForm(), "The current match could not be replaced with the selected value.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            EnsureMatchedPathsVisible(new[] { targetMatch.FilePath });
            EnsureFieldVisible(targetMatch.Descriptor.Label);
            SelectSingleCellByReference(targetMatch.FilePath, targetMatch.ColumnName);
        }

        private void ApplyRequestAsFilter(BulkFindReplaceRequest request)
        {
            List<BulkSearchMatch> matches = FindMatches(request);
            if (matches.Count == 0)
            {
                MessageBox.Show(FindForm(), BuildNoMatchesMessage(request), "Apply as Filter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetCustomRowFilter(
                matches.Select(match => match.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                SearchResultsFilterName);
        }

        private void EnsureMatchedPathsVisible(IReadOnlyCollection<string> matchedPaths)
        {
            if (matchedPaths == null || matchedPaths.Count == 0)
                return;

            if (matchedPaths.Any(path => !visibleRows.Any(row => string.Equals(row.FilePath, path, StringComparison.OrdinalIgnoreCase))))
                SetRowFilter(BulkMaterialRowFilter.AllFiles);
        }

        private List<BulkSearchMatch> FindMatches(BulkFindReplaceRequest request)
        {
            var matches = new List<BulkSearchMatch>();
            if (session == null || request == null)
                return matches;

            List<BulkMaterialEditRow> orderedRows = BuildSearchRows().ToList();
            IReadOnlyList<MaterialFieldDescriptor> descriptors = ResolveSearchDescriptors(request);
            if (descriptors.Count == 0 || orderedRows.Count == 0)
                return matches;

            Dictionary<string, int> rowOrderByFilePath = orderedRows
                .Select((row, index) => new { row.FilePath, Index = index })
                .ToDictionary(item => item.FilePath, item => item.Index, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> fieldOrderByLabel = descriptors
                .Select((descriptor, index) => new { descriptor.Label, Index = index })
                .ToDictionary(item => item.Label, item => item.Index, StringComparer.OrdinalIgnoreCase);

            foreach (BulkMaterialEditRow row in orderedRows)
            {
                if (row == null || row.HasLoadError)
                    continue;

                foreach (MaterialFieldDescriptor descriptor in descriptors)
                {
                    BulkMaterialEditCellState cell = row.GetCell(descriptor);
                    if (!cell.IsSupported || !CellMatchesRequest(cell, descriptor, request))
                        continue;

                    matches.Add(new BulkSearchMatch(
                        row,
                        descriptor,
                        rowOrderByFilePath[row.FilePath],
                        fieldOrderByLabel[descriptor.Label]));
                }
            }

            return matches;
        }

        private IReadOnlyList<MaterialFieldDescriptor> ResolveSearchDescriptors(BulkFindReplaceRequest request)
        {
            if (session == null || request == null)
                return Array.Empty<MaterialFieldDescriptor>();

            IReadOnlyCollection<string> selectedFieldLabels = request.FieldLabels?.ToArray() ?? Array.Empty<string>();
            IEnumerable<MaterialFieldDescriptor> descriptors = request.Scope == BulkFindScope.ByField
                ? session.AllDescriptors.Where(descriptor => selectedFieldLabels.Contains(descriptor.Label, StringComparer.OrdinalIgnoreCase))
                : session.AllDescriptors;

            return descriptors
                .Where(descriptor => IsDescriptorCompatibleForSearch(descriptor, request.ValueType))
                .ToArray();
        }

        private IEnumerable<BulkMaterialEditRow> BuildSearchRows()
        {
            if (session == null)
                return Array.Empty<BulkMaterialEditRow>();

            return IsGroupedByFolder
                ? OrderRowsByFolder(session.Rows)
                : OrderRows(session.Rows);
        }

        private bool CellMatchesRequest(BulkMaterialEditCellState cell, MaterialFieldDescriptor descriptor, BulkFindReplaceRequest request)
        {
            if (cell == null || descriptor == null || request == null)
                return false;

            return request.ValueType switch
            {
                BulkFindValueType.Number => TryParseSearchValue(descriptor, request.FindText, out object searchedNumber)
                    && BulkMaterialEditValueComparer.ValuesEqual(searchedNumber, cell.CurrentValue),
                BulkFindValueType.Boolean => cell.CurrentValue is bool currentBool && currentBool == request.FindBooleanValue,
                _ => (cell.GetDisplayText() ?? string.Empty).IndexOf(request.FindText ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0
            };
        }

        private static bool IsDescriptorCompatibleForSearch(MaterialFieldDescriptor descriptor, BulkFindValueType valueType)
        {
            if (descriptor == null)
                return false;

            return valueType switch
            {
                BulkFindValueType.Number => descriptor.EditorKind == BulkFieldEditorKind.Number,
                BulkFindValueType.Boolean => descriptor.EditorKind == BulkFieldEditorKind.Boolean,
                _ => descriptor.EditorKind is BulkFieldEditorKind.Text
                    or BulkFieldEditorKind.TexturePath
                    or BulkFieldEditorKind.MaterialPath
            };
        }

        private static bool TryParseSearchValue(MaterialFieldDescriptor descriptor, string text, out object parsedValue)
        {
            parsedValue = null;
            return descriptor != null
                && descriptor.TryParseTextValue(text ?? string.Empty, out parsedValue, out _);
        }

        private BulkSearchMatch GetRelativeCellMatch(IReadOnlyList<BulkSearchMatch> matches, SearchNavigationDirection direction)
        {
            if (matches == null || matches.Count == 0)
                return null;

            if (!TryGetCurrentSearchPosition(matches, direction, out int currentRowOrder, out int currentFieldOrder))
                return direction == SearchNavigationDirection.Forward ? matches[0] : matches[^1];

            if (direction == SearchNavigationDirection.Forward)
            {
                BulkSearchMatch nextMatch = matches.FirstOrDefault(match =>
                    match.RowOrder > currentRowOrder
                    || (match.RowOrder == currentRowOrder && match.FieldOrder > currentFieldOrder));
                return nextMatch ?? matches[0];
            }

            BulkSearchMatch previousMatch = matches.LastOrDefault(match =>
                match.RowOrder < currentRowOrder
                || (match.RowOrder == currentRowOrder && match.FieldOrder < currentFieldOrder));
            return previousMatch ?? matches[^1];
        }

        private BulkSearchMatch GetRelativeFileMatch(IReadOnlyList<BulkSearchMatch> matches, SearchNavigationDirection direction)
        {
            if (matches == null || matches.Count == 0)
                return null;

            List<BulkSearchMatch> distinctMatches = matches
                .GroupBy(match => match.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(match => match.RowOrder)
                .ToList();
            if (distinctMatches.Count == 0)
                return null;

            string currentFilePath = GetCurrentFilePath();
            if (!string.IsNullOrWhiteSpace(currentFilePath))
            {
                int currentIndex = distinctMatches.FindIndex(match => string.Equals(match.FilePath, currentFilePath, StringComparison.OrdinalIgnoreCase));
                if (currentIndex >= 0)
                {
                    int nextIndex = direction == SearchNavigationDirection.Forward
                        ? (currentIndex + 1) % distinctMatches.Count
                        : (currentIndex - 1 + distinctMatches.Count) % distinctMatches.Count;
                    return distinctMatches[nextIndex];
                }
            }

            return direction == SearchNavigationDirection.Forward ? distinctMatches[0] : distinctMatches[^1];
        }

        private BulkSearchMatch GetReplaceTargetCellMatch(IReadOnlyList<BulkSearchMatch> matches)
        {
            if (matches == null || matches.Count == 0)
                return null;

            if (grid.CurrentCell != null
                && grid.CurrentCell.RowIndex >= 0
                && grid.CurrentCell.ColumnIndex >= 0
                && grid.CurrentCell.RowIndex < grid.Rows.Count
                && grid.Rows[grid.CurrentCell.RowIndex].Tag is BulkMaterialEditRow currentRow)
            {
                string currentColumnName = grid.Columns[grid.CurrentCell.ColumnIndex].Name;
                BulkSearchMatch currentMatch = matches.FirstOrDefault(match =>
                    string.Equals(match.FilePath, currentRow.FilePath, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(match.ColumnName, currentColumnName, StringComparison.OrdinalIgnoreCase));
                if (currentMatch != null)
                    return currentMatch;
            }

            return GetRelativeCellMatch(matches, SearchNavigationDirection.Forward);
        }

        private BulkSearchMatch GetReplaceTargetFileMatch(IReadOnlyList<BulkSearchMatch> matches)
        {
            if (matches == null || matches.Count == 0)
                return null;

            string currentFilePath = GetCurrentFilePath();
            if (!string.IsNullOrWhiteSpace(currentFilePath))
            {
                BulkSearchMatch currentMatch = matches.FirstOrDefault(match =>
                    string.Equals(match.FilePath, currentFilePath, StringComparison.OrdinalIgnoreCase));
                if (currentMatch != null)
                    return currentMatch;
            }

            return GetRelativeFileMatch(matches, SearchNavigationDirection.Forward);
        }

        private bool TryGetCurrentSearchPosition(IReadOnlyList<BulkSearchMatch> matches, SearchNavigationDirection direction, out int currentRowOrder, out int currentFieldOrder)
        {
            currentRowOrder = -1;
            currentFieldOrder = -1;
            if (matches == null || matches.Count == 0)
                return false;

            string currentFilePath = GetCurrentFilePath();
            if (string.IsNullOrWhiteSpace(currentFilePath))
                return false;

            BulkSearchMatch rowReferenceMatch = matches.FirstOrDefault(match =>
                string.Equals(match.FilePath, currentFilePath, StringComparison.OrdinalIgnoreCase));
            if (rowReferenceMatch == null)
                return false;

            currentRowOrder = rowReferenceMatch.RowOrder;
            currentFieldOrder = direction == SearchNavigationDirection.Forward ? -1 : int.MaxValue;

            if (grid.CurrentCell == null || grid.CurrentCell.ColumnIndex < 0 || grid.CurrentCell.RowIndex < 0)
                return true;

            string currentColumnName = grid.Columns[grid.CurrentCell.ColumnIndex].Name;
            BulkSearchMatch exactMatch = matches.FirstOrDefault(match =>
                string.Equals(match.FilePath, currentFilePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(match.ColumnName, currentColumnName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                currentFieldOrder = exactMatch.FieldOrder;
                return true;
            }

            MaterialFieldDescriptor descriptor = TryGetFieldDescriptor(currentColumnName);
            if (descriptor == null)
                return true;

            int descriptorOrder = matches
                .Where(match => string.Equals(match.Descriptor.Label, descriptor.Label, StringComparison.OrdinalIgnoreCase))
                .Select(match => match.FieldOrder)
                .DefaultIfEmpty(direction == SearchNavigationDirection.Forward ? -1 : int.MaxValue)
                .First();
            currentFieldOrder = descriptorOrder;
            return true;
        }

        private string GetCurrentFilePath()
        {
            if (grid.CurrentCell != null
                && grid.CurrentCell.RowIndex >= 0
                && grid.CurrentCell.RowIndex < grid.Rows.Count
                && grid.Rows[grid.CurrentCell.RowIndex].Tag is BulkMaterialEditRow currentRow)
            {
                return currentRow.FilePath;
            }

            return SelectedRows.FirstOrDefault()?.FilePath ?? string.Empty;
        }

        private int ApplyReplaceRequest(IReadOnlyList<BulkSearchMatch> matches, BulkFindReplaceRequest request)
        {
            if (session == null || matches == null || matches.Count == 0 || request == null)
                return 0;

            CommitPendingEdits();

            string[] selectedRows = SelectedRows
                .Select(row => row.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            (string FilePath, string ColumnName)? currentCellReference = grid.CurrentCell == null
                ? null
                : BuildCellReference(grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex);

            bool visibilityChanged = false;
            int replacedCount = 0;
            var originalValues = new Dictionary<(string FilePath, string Label), object>();

            foreach (BulkSearchMatch match in matches)
            {
                if (!TryBuildReplacementInput(match, request, out object replacementInput))
                    continue;

                CaptureOriginalCellValue(originalValues, match.Row, match.Descriptor);
                if (!session.TrySetCellValue(match.Row, match.Descriptor, replacementInput, out _))
                    continue;

                invalidCellTexts.Remove((match.FilePath, match.Descriptor.Label));
                pendingBooleanOverrides.Remove((match.FilePath, match.Descriptor.Label));
                pendingDirtyRowPaths.Remove(match.FilePath);
                visibilityChanged |= VisibilityDrivenFieldMap.ContainsKey(match.Descriptor.Label);
                replacedCount++;
            }

            if (replacedCount == 0)
                return 0;

            foreach (string filePath in matches.Select(match => match.FilePath).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                BulkMaterialEditRow row = session.Rows.FirstOrDefault(candidate => string.Equals(candidate.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
                row?.RefreshDynamicState();
            }

            RecordUndoBatch(originalValues);

            bool rebuilt = visibilityChanged && TryRefreshVisibilityDrivenColumns(
                selectedRows,
                currentCellReference?.FilePath ?? string.Empty,
                currentCellReference?.ColumnName ?? string.Empty);

            if (!rebuilt)
                RefreshAllGridRows();

            return replacedCount;
        }

        private bool TryBuildReplacementInput(BulkSearchMatch match, BulkFindReplaceRequest request, out object replacementInput)
        {
            replacementInput = null;
            if (match == null || request == null)
                return false;

            BulkMaterialEditCellState cell = match.Row.GetCell(match.Descriptor);
            switch (request.ValueType)
            {
                case BulkFindValueType.Number:
                    if (!match.Descriptor.TryParseTextValue(request.ReplaceText ?? string.Empty, out object parsedNumber, out _))
                        return false;

                    replacementInput = parsedNumber;
                    return true;

                case BulkFindValueType.Boolean:
                    replacementInput = request.ReplaceBooleanValue;
                    return true;

                default:
                    string currentText = cell.GetDisplayText() ?? string.Empty;
                    string replacedText = currentText.Replace(request.FindText ?? string.Empty, request.ReplaceText ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                    if (string.Equals(currentText, replacedText, StringComparison.Ordinal))
                        return false;

                    replacementInput = replacedText;
                    return true;
            }
        }

        private void EnsureFieldsVisible(IEnumerable<string> labels)
        {
            if (labels == null)
                return;

            bool changed = false;
            foreach (string label in labels.Where(label => !string.IsNullOrWhiteSpace(label)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (selectedLabelPreferences.Add(label))
                    changed = true;
            }

            if (changed)
                RefreshVisibleDescriptorsAndGrid(refreshSummary: false);
        }

        private void EnsureFieldVisible(string label)
        {
            EnsureFieldsVisible(new[] { label });
        }

        private void SelectSingleCellByReference(string filePath, string columnName)
        {
            if (!TryResolveGridCell(filePath, columnName, out int rowIndex, out DataGridViewCell gridCell, out _, out _))
                return;

            PerformSelectionMutation(() =>
            {
                grid.ClearSelection();
                gridCell.Selected = true;
                grid.CurrentCell = gridCell;
            });
            SetSelectionAnchor(rowIndex, gridCell.ColumnIndex);
            CenterCurrentCellColumn();
            RefreshSummary();
        }

        private void SelectSingleResultRow(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            ReselectRows(new[] { filePath });
            RestoreCurrentCell(filePath, PathColumnName);
            RefreshSummary();
        }

        private static string BuildNoMatchesMessage(BulkFindReplaceRequest request)
        {
            return request?.Scope switch
            {
                BulkFindScope.ByFile => "No matching files were found.",
                BulkFindScope.ByField => "No matches were found in the selected field set.",
                _ => "No matches were found in the open files."
            };
        }
    }
}
