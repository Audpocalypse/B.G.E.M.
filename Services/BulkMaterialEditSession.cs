using MaterialLib;
using Material_Editor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Material_Editor.Services
{
    internal static class BulkMaterialEditValueComparer
    {
        public static bool ValuesEqual(object original, object current)
        {
            if (original == null && current == null)
                return true;

            if (original == null || current == null)
                return false;

            if (original is float originalFloat && current is float currentFloat)
                return Math.Abs(originalFloat - currentFloat) < 0.0001f;

            if (original is double originalDouble && current is double currentDouble)
                return Math.Abs(originalDouble - currentDouble) < 0.0001;

            if (original is decimal originalDecimal && current is decimal currentDecimal)
                return Math.Abs(originalDecimal - currentDecimal) < 0.0001m;

            return Equals(original, current);
        }
    }

    internal sealed class BulkMaterialEditUndoChange
    {
        public BulkMaterialEditUndoChange(string filePath, string descriptorLabel, object previousValue, object currentValue)
        {
            FilePath = filePath ?? string.Empty;
            DescriptorLabel = descriptorLabel ?? string.Empty;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
        }

        public string FilePath { get; }
        public string DescriptorLabel { get; }
        public object PreviousValue { get; }
        public object CurrentValue { get; }
    }

    internal sealed class BulkMaterialEditCellState
    {
        public BulkMaterialEditCellState(MaterialFieldDescriptor descriptor, BaseMaterialFile originalMaterial, BaseMaterialFile currentMaterial)
        {
            Descriptor = descriptor;
            Refresh(originalMaterial, currentMaterial);
        }

        public MaterialFieldDescriptor Descriptor { get; }
        public bool IsSupported { get; private set; }
        public object OriginalValue { get; private set; }
        public object CurrentValue { get; private set; }
        public string ErrorMessage { get; private set; }

        public bool IsDirty => !ValuesEqual(OriginalValue, CurrentValue);
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public void Refresh(BaseMaterialFile originalMaterial, BaseMaterialFile currentMaterial)
        {
            Refresh(originalMaterial, currentMaterial, clearError: true);
        }

        public void Refresh(BaseMaterialFile originalMaterial, BaseMaterialFile currentMaterial, bool clearError)
        {
            IsSupported = currentMaterial != null && Descriptor.IsSupported(currentMaterial);
            OriginalValue = originalMaterial != null ? Descriptor.GetValue(originalMaterial) : null;
            CurrentValue = currentMaterial != null ? Descriptor.GetValue(currentMaterial) : null;

            if (clearError || !IsSupported)
                ErrorMessage = null;
        }

        public void RefreshCurrentState(BaseMaterialFile currentMaterial, bool clearError)
        {
            IsSupported = currentMaterial != null && Descriptor.IsSupported(currentMaterial);
            CurrentValue = currentMaterial != null ? Descriptor.GetValue(currentMaterial) : null;

            if (clearError || !IsSupported)
                ErrorMessage = null;
        }

        public object GetGridValue()
        {
            return IsSupported ? Descriptor.CoerceGridValue(CurrentValue) : string.Empty;
        }

        public string GetDisplayText()
        {
            return IsSupported ? Descriptor.FormatValue(CurrentValue) : string.Empty;
        }

        public bool TrySetValue(BaseMaterialFile material, object inputValue, out string errorMessage)
        {
            errorMessage = null;

            if (!IsSupported || material == null)
            {
                ErrorMessage = "Field is not supported for this material.";
                errorMessage = ErrorMessage;
                return false;
            }

            try
            {
                object parsedValue = ParseInputValue(inputValue);
                Descriptor.SetValue(material, parsedValue);
                CurrentValue = Descriptor.GetValue(material);
                ErrorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                errorMessage = ErrorMessage;
                return false;
            }
        }

        public void ClearError()
        {
            ErrorMessage = null;
        }

        private object ParseInputValue(object inputValue)
        {
            if (Descriptor.EditorKind == BulkFieldEditorKind.Boolean)
                return inputValue is bool boolValue ? boolValue : ParseText(inputValue);

            if (Descriptor.EditorKind == BulkFieldEditorKind.Enum && inputValue != null)
            {
                if (inputValue.GetType().IsEnum)
                    return inputValue;

                return ParseText(inputValue);
            }

            return ParseText(inputValue);
        }

        private object ParseText(object inputValue)
        {
            string text = inputValue?.ToString() ?? string.Empty;
            if (Descriptor.TryParseTextValue(text, out object parsedValue, out string errorMessage))
                return parsedValue;

            throw new FormatException(errorMessage ?? "Invalid value.");
        }

        private static bool ValuesEqual(object original, object current) => BulkMaterialEditValueComparer.ValuesEqual(original, current);
    }

    internal sealed class BulkMaterialEditRow
    {
        private readonly Dictionary<string, BulkMaterialEditCellState> cells;

        private BulkMaterialEditRow(string filePath, BaseMaterialFile material, BaseMaterialFile originalMaterial, bool isJson, string loadError, int insertionIndex)
        {
            FilePath = filePath ?? string.Empty;
            Material = material;
            OriginalMaterial = originalMaterial;
            IsJson = isJson;
            LoadError = loadError ?? string.Empty;
            InsertionIndex = insertionIndex;
            FileName = Path.GetFileName(FilePath) ?? string.Empty;
            RefreshFileMetadata();
            cells = new Dictionary<string, BulkMaterialEditCellState>(StringComparer.OrdinalIgnoreCase);
        }

        public string FilePath { get; }
        public BaseMaterialFile Material { get; }
        public BaseMaterialFile OriginalMaterial { get; private set; }
        public bool IsJson { get; }
        public string LoadError { get; }
        public int InsertionIndex { get; }
        public string FileName { get; }
        public string DisplayPath { get; private set; }
        public string DisplayFolder { get; private set; }
        public DateTime CreationTimeUtc { get; private set; }
        public DateTime LastWriteTimeUtc { get; private set; }
        public uint Version => Material?.Version ?? 0;
        public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadError);
        public bool IsDirty => !HasLoadError && cells.Values.Any(cell => cell.IsDirty);
        public bool HasValidationErrors => !HasLoadError && cells.Values.Any(cell => cell.HasError);

        public IEnumerable<BulkMaterialEditCellState> Cells => cells.Values;

        public IReadOnlyList<string> GetValidationErrorMessages()
        {
            return cells.Values
                .Where(cell => cell.HasError)
                .Select(cell => $"{cell.Descriptor.Label}: {cell.ErrorMessage}")
                .ToArray();
        }

        public static BulkMaterialEditRow Load(string filePath, MaterialType materialType, IEnumerable<MaterialFieldDescriptor> descriptors, int insertionIndex = -1)
        {
            string normalizedPath = MaterialFilePersistence.NormalizePath(filePath);
            string expectedExtension = MaterialFileTypeHelper.GetExpectedExtension(materialType);

            if (!normalizedPath.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
                return new BulkMaterialEditRow(normalizedPath, null, null, false, "Incompatible material type.", insertionIndex);

            if (!MaterialFilePersistence.TryLoadMaterial(normalizedPath, out BaseMaterialFile material, out bool isJson, out string errorMessage))
                return new BulkMaterialEditRow(normalizedPath, null, null, isJson, errorMessage ?? "Failed to load material.", insertionIndex);

            if (MaterialFileTypeHelper.GetMaterialType(material) != materialType)
                return new BulkMaterialEditRow(normalizedPath, null, null, isJson, "Incompatible material type.", insertionIndex);

            var row = new BulkMaterialEditRow(
                normalizedPath,
                material,
                MaterialFileCloner.Clone(material),
                isJson,
                string.Empty,
                insertionIndex);

            foreach (MaterialFieldDescriptor descriptor in descriptors)
                row.cells[descriptor.Label] = new BulkMaterialEditCellState(descriptor, row.OriginalMaterial, row.Material);

            return row;
        }

        public BulkMaterialEditCellState GetCell(MaterialFieldDescriptor descriptor)
        {
            if (!cells.TryGetValue(descriptor.Label, out BulkMaterialEditCellState cell))
            {
                cell = new BulkMaterialEditCellState(descriptor, OriginalMaterial, Material);
                cells[descriptor.Label] = cell;
            }

            return cell;
        }

        public void AcceptChanges()
        {
            OriginalMaterial = MaterialFileCloner.Clone(Material);
            foreach (BulkMaterialEditCellState cell in cells.Values)
                cell.Refresh(OriginalMaterial, Material);
        }

        public void RefreshDynamicState()
        {
            foreach (BulkMaterialEditCellState cell in cells.Values)
                cell.RefreshCurrentState(Material, clearError: false);
        }

        public void RefreshFileMetadata()
        {
            DisplayPath = GetDisplayFilePath(FilePath);
            DisplayFolder = GetDisplayFolder(DisplayPath);

            try
            {
                CreationTimeUtc = File.Exists(FilePath) ? File.GetCreationTimeUtc(FilePath) : DateTime.MinValue;
                LastWriteTimeUtc = File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : DateTime.MinValue;
            }
            catch
            {
                CreationTimeUtc = DateTime.MinValue;
                LastWriteTimeUtc = DateTime.MinValue;
            }
        }

        private static string GetDisplayFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            const string materialsBackslash = "materials\\";
            const string materialsSlash = "materials/";
            int index = filePath.IndexOf(materialsBackslash, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
                return filePath[(index + materialsBackslash.Length)..];

            index = filePath.IndexOf(materialsSlash, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
                return filePath[(index + materialsSlash.Length)..];

            return filePath;
        }

        private static string GetDisplayFolder(string displayPath)
        {
            if (string.IsNullOrWhiteSpace(displayPath))
                return string.Empty;

            string normalizedPath = displayPath.Replace('/', '\\');
            string directory = Path.GetDirectoryName(normalizedPath);
            return string.IsNullOrWhiteSpace(directory) ? string.Empty : directory;
        }
    }

    internal sealed class BulkMaterialEditSession
    {
        private readonly Dictionary<string, MaterialFieldDescriptor> descriptorsByLabel;
        private readonly List<BulkMaterialEditRow> rows;
        private readonly Stack<IReadOnlyList<BulkMaterialEditUndoChange>> undoHistory;
        private readonly Stack<IReadOnlyList<BulkMaterialEditUndoChange>> redoHistory;
        private int nextInsertionIndex;

        private BulkMaterialEditSession(MaterialType materialType, IReadOnlyList<MaterialFieldDescriptor> descriptors)
        {
            MaterialType = materialType;
            AllDescriptors = descriptors;
            descriptorsByLabel = descriptors.ToDictionary(descriptor => descriptor.Label, StringComparer.OrdinalIgnoreCase);
            rows = new List<BulkMaterialEditRow>();
            undoHistory = new Stack<IReadOnlyList<BulkMaterialEditUndoChange>>();
            redoHistory = new Stack<IReadOnlyList<BulkMaterialEditUndoChange>>();
            nextInsertionIndex = 0;
        }

        public MaterialType MaterialType { get; }
        public IReadOnlyList<MaterialFieldDescriptor> AllDescriptors { get; }
        public IReadOnlyList<BulkMaterialEditRow> Rows => rows;
        public bool CanUndo => undoHistory.Count > 0;
        public bool CanRedo => redoHistory.Count > 0;
        public bool HasDirtyRows => Rows.Any(row => row.IsDirty);
        public bool HasValidationErrors => Rows.Any(row => row.HasValidationErrors);
        public int DirtyRowCount => Rows.Count(row => row.IsDirty);
        public int ErrorRowCount => Rows.Count(row => row.HasValidationErrors);

        public static BulkMaterialEditSession Create(MaterialType materialType, IReadOnlyList<string> targetFiles)
        {
            IReadOnlyList<MaterialFieldDescriptor> descriptors = MaterialFieldRegistry.GetDescriptors(materialType);
            var session = new BulkMaterialEditSession(materialType, descriptors);
            session.AddFiles(targetFiles);
            return session;
        }

        public MaterialFieldDescriptor FindDescriptor(string label)
        {
            return descriptorsByLabel.TryGetValue(label ?? string.Empty, out MaterialFieldDescriptor descriptor)
                ? descriptor
                : null;
        }

        public bool TrySetCellValue(BulkMaterialEditRow row, MaterialFieldDescriptor descriptor, object inputValue, out string errorMessage, bool recordUndo = false)
        {
            errorMessage = null;

            if (row == null || descriptor == null)
            {
                errorMessage = "Missing row or field metadata.";
                return false;
            }

            if (row.HasLoadError)
            {
                errorMessage = row.LoadError;
                return false;
            }

            BulkMaterialEditCellState cell = row.GetCell(descriptor);
            object previousValue = cell.CurrentValue;
            if (!cell.TrySetValue(row.Material, inputValue, out errorMessage))
                return false;

            if (recordUndo && !BulkMaterialEditValueComparer.ValuesEqual(previousValue, cell.CurrentValue))
            {
                RecordUndoEntry(new[]
                {
                    new BulkMaterialEditUndoChange(row.FilePath, descriptor.Label, previousValue, cell.CurrentValue)
                });
            }

            return true;
        }

        public BulkMaterialSessionAddResult AddFiles(IEnumerable<string> targetFiles)
        {
            var normalizedPaths = (targetFiles ?? Array.Empty<string>())
                .Select(MaterialFilePersistence.NormalizePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            int duplicateCount = 0;
            var addedRows = new List<BulkMaterialEditRow>();
            var existingPaths = new HashSet<string>(rows.Select(row => row.FilePath), StringComparer.OrdinalIgnoreCase);

            foreach (string path in normalizedPaths)
            {
                if (!existingPaths.Add(path))
                {
                    duplicateCount++;
                    continue;
                }

                BulkMaterialEditRow row = BulkMaterialEditRow.Load(path, MaterialType, AllDescriptors, nextInsertionIndex++);
                addedRows.Add(row);
            }

            rows.AddRange(addedRows);

            return new BulkMaterialSessionAddResult
            {
                AddedRows = addedRows,
                DuplicateCount = duplicateCount
            };
        }

        public int RemoveRows(IEnumerable<BulkMaterialEditRow> selectedRows)
        {
            var rowsToRemove = new HashSet<string>(
                (selectedRows ?? Array.Empty<BulkMaterialEditRow>())
                    .Where(row => row != null)
                    .Select(row => row.FilePath),
                StringComparer.OrdinalIgnoreCase);

            if (rowsToRemove.Count == 0)
                return 0;

            DiscardUndoEntriesForPaths(rowsToRemove);
            return rows.RemoveAll(row => rowsToRemove.Contains(row.FilePath));
        }

        public IReadOnlyList<BulkMaterialEditRow> ReloadRows(IEnumerable<BulkMaterialEditRow> selectedRows)
        {
            var pathsToReload = new HashSet<string>(
                (selectedRows ?? Array.Empty<BulkMaterialEditRow>())
                    .Where(row => row != null)
                    .Select(row => row.FilePath),
                StringComparer.OrdinalIgnoreCase);

            if (pathsToReload.Count == 0)
                return Array.Empty<BulkMaterialEditRow>();

            DiscardUndoEntriesForPaths(pathsToReload);
            var reloadedRows = new List<BulkMaterialEditRow>();
            for (int index = 0; index < rows.Count; index++)
            {
                BulkMaterialEditRow existingRow = rows[index];
                if (!pathsToReload.Contains(existingRow.FilePath))
                    continue;

                BulkMaterialEditRow reloadedRow = BulkMaterialEditRow.Load(existingRow.FilePath, MaterialType, AllDescriptors, existingRow.InsertionIndex);
                rows[index] = reloadedRow;
                reloadedRows.Add(reloadedRow);
            }

            return reloadedRows;
        }

        public IReadOnlyList<FieldCopyResult> ApplyChanges(bool backupBeforeWrite, Config config = null)
        {
            return ApplyRows(rows, backupBeforeWrite, config);
        }

        public void RecordUndoEntry(IEnumerable<BulkMaterialEditUndoChange> changes)
        {
            BulkMaterialEditUndoChange[] recordedChanges = (changes ?? Array.Empty<BulkMaterialEditUndoChange>())
                .Where(change => change != null)
                .Where(change => !string.IsNullOrWhiteSpace(change.FilePath) && !string.IsNullOrWhiteSpace(change.DescriptorLabel))
                .Where(change => !BulkMaterialEditValueComparer.ValuesEqual(change.PreviousValue, change.CurrentValue))
                .ToArray();

            if (recordedChanges.Length == 0)
                return;

            undoHistory.Push(recordedChanges);
            redoHistory.Clear();
        }

        public IReadOnlyList<BulkMaterialEditUndoChange> UndoLastChange()
        {
            return ApplyHistoryChange(undoHistory, redoHistory, applyPreviousValue: true);
        }

        public IReadOnlyList<BulkMaterialEditUndoChange> RedoLastChange()
        {
            return ApplyHistoryChange(redoHistory, undoHistory, applyPreviousValue: false);
        }

        public IReadOnlyList<FieldCopyResult> ApplySelectedChanges(IEnumerable<BulkMaterialEditRow> selectedRows, bool backupBeforeWrite, Config config = null)
        {
            return ApplyRows(
                (selectedRows ?? Array.Empty<BulkMaterialEditRow>())
                    .Where(row => row != null)
                    .Distinct()
                    .ToArray(),
                backupBeforeWrite,
                config);
        }

        private IReadOnlyList<FieldCopyResult> ApplyRows(IEnumerable<BulkMaterialEditRow> targetRows, bool backupBeforeWrite, Config config)
        {
            var results = new List<FieldCopyResult>();
            var savedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (BulkMaterialEditRow row in targetRows ?? Array.Empty<BulkMaterialEditRow>())
            {
                if (row.HasLoadError)
                {
                    results.Add(new FieldCopyResult(row.FilePath, FieldCopyStatus.Failed, row.LoadError));
                    continue;
                }

                if (row.HasValidationErrors)
                {
                    string details = string.Join("; ", row.GetValidationErrorMessages());
                    string message = string.IsNullOrWhiteSpace(details)
                        ? "Resolve validation errors before applying."
                        : $"Resolve validation errors before applying. {details}";
                    results.Add(new FieldCopyResult(row.FilePath, FieldCopyStatus.Failed, message));
                    continue;
                }

                if (!row.IsDirty)
                {
                    results.Add(new FieldCopyResult(row.FilePath, FieldCopyStatus.Skipped, "No changes to apply."));
                    continue;
                }

                FieldCopyResult saveResult = MaterialFilePersistence.SaveMaterialResult(
                    row.FilePath,
                    row.Material,
                    row.IsJson,
                    "Updated successfully.",
                    backupBeforeWrite,
                    config);
                if (saveResult.Status == FieldCopyStatus.Success)
                {
                    row.AcceptChanges();
                    savedPaths.Add(row.FilePath);
                }

                results.Add(saveResult);
            }

            if (savedPaths.Count > 0)
                DiscardUndoEntriesForPaths(savedPaths);

            return results;
        }

        private void DiscardUndoEntriesForPaths(IReadOnlyCollection<string> affectedPaths)
        {
            if (affectedPaths == null || affectedPaths.Count == 0)
                return;

            var pathSet = new HashSet<string>(affectedPaths, StringComparer.OrdinalIgnoreCase);
            DiscardHistoryEntriesForPaths(undoHistory, pathSet);
            DiscardHistoryEntriesForPaths(redoHistory, pathSet);
        }

        private IReadOnlyList<BulkMaterialEditUndoChange> ApplyHistoryChange(
            Stack<IReadOnlyList<BulkMaterialEditUndoChange>> sourceHistory,
            Stack<IReadOnlyList<BulkMaterialEditUndoChange>> destinationHistory,
            bool applyPreviousValue)
        {
            while (sourceHistory.Count > 0)
            {
                IReadOnlyList<BulkMaterialEditUndoChange> changes = sourceHistory.Pop();
                var appliedChanges = new List<BulkMaterialEditUndoChange>();
                var inverseChanges = new List<BulkMaterialEditUndoChange>();
                var affectedRows = new HashSet<BulkMaterialEditRow>();
                List<BulkMaterialEditUndoChange> pendingChanges = changes.ToList();
                bool madeProgress;
                do
                {
                    madeProgress = false;
                    var deferredChanges = new List<BulkMaterialEditUndoChange>();

                    foreach (BulkMaterialEditUndoChange change in pendingChanges)
                    {
                        BulkMaterialEditRow row = rows.FirstOrDefault(candidate => string.Equals(candidate.FilePath, change.FilePath, StringComparison.OrdinalIgnoreCase));
                        MaterialFieldDescriptor descriptor = FindDescriptor(change.DescriptorLabel);
                        if (row == null || descriptor == null || row.HasLoadError)
                            continue;

                        BulkMaterialEditCellState cell = row.GetCell(descriptor);
                        object currentValue = cell.CurrentValue;
                        object targetValue = applyPreviousValue ? change.PreviousValue : change.CurrentValue;
                        if (BulkMaterialEditValueComparer.ValuesEqual(targetValue, currentValue))
                            continue;

                        if (!cell.TrySetValue(row.Material, targetValue, out _))
                        {
                            deferredChanges.Add(change);
                            continue;
                        }

                        appliedChanges.Add(new BulkMaterialEditUndoChange(change.FilePath, change.DescriptorLabel, change.PreviousValue, change.CurrentValue));
                        inverseChanges.Add(applyPreviousValue
                            ? new BulkMaterialEditUndoChange(change.FilePath, change.DescriptorLabel, targetValue, currentValue)
                            : new BulkMaterialEditUndoChange(change.FilePath, change.DescriptorLabel, currentValue, targetValue));
                        affectedRows.Add(row);
                        row.RefreshDynamicState();
                        madeProgress = true;
                    }

                    pendingChanges = deferredChanges;
                }
                while (madeProgress && pendingChanges.Count > 0);

                foreach (BulkMaterialEditRow row in affectedRows)
                    row.RefreshDynamicState();

                if (inverseChanges.Count > 0)
                {
                    destinationHistory.Push(inverseChanges);
                    return appliedChanges;
                }
            }

            return Array.Empty<BulkMaterialEditUndoChange>();
        }

        private static void DiscardHistoryEntriesForPaths(
            Stack<IReadOnlyList<BulkMaterialEditUndoChange>> history,
            IReadOnlySet<string> affectedPaths)
        {
            if (history == null || affectedPaths == null || affectedPaths.Count == 0 || history.Count == 0)
                return;

            IReadOnlyList<BulkMaterialEditUndoChange>[] retainedEntries = history
                .Reverse()
                .Where(entry => entry.All(change => !affectedPaths.Contains(change.FilePath)))
                .ToArray();

            history.Clear();
            foreach (IReadOnlyList<BulkMaterialEditUndoChange> entry in retainedEntries)
                history.Push(entry);
        }
    }

    internal sealed class BulkMaterialSessionAddResult
    {
        public IReadOnlyList<BulkMaterialEditRow> AddedRows { get; init; } = Array.Empty<BulkMaterialEditRow>();
        public int DuplicateCount { get; init; }
    }
}
