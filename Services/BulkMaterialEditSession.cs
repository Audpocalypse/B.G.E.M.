using MaterialLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Material_Editor.Services
{
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

        private static bool ValuesEqual(object original, object current)
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

    internal sealed class BulkMaterialEditRow
    {
        private readonly Dictionary<string, BulkMaterialEditCellState> cells;

        private BulkMaterialEditRow(string filePath, BaseMaterialFile material, BaseMaterialFile originalMaterial, bool isJson, string loadError)
        {
            FilePath = filePath ?? string.Empty;
            Material = material;
            OriginalMaterial = originalMaterial;
            IsJson = isJson;
            LoadError = loadError ?? string.Empty;
            cells = new Dictionary<string, BulkMaterialEditCellState>(StringComparer.OrdinalIgnoreCase);
        }

        public string FilePath { get; }
        public BaseMaterialFile Material { get; }
        public BaseMaterialFile OriginalMaterial { get; private set; }
        public bool IsJson { get; }
        public string LoadError { get; }
        public uint Version => Material?.Version ?? 0;
        public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadError);
        public bool IsDirty => !HasLoadError && cells.Values.Any(cell => cell.IsDirty);
        public bool HasValidationErrors => !HasLoadError && cells.Values.Any(cell => cell.HasError);

        public IEnumerable<BulkMaterialEditCellState> Cells => cells.Values;

        public static BulkMaterialEditRow Load(string filePath, MaterialType materialType, IEnumerable<MaterialFieldDescriptor> descriptors)
        {
            string normalizedPath = MaterialFilePersistence.NormalizePath(filePath);
            string expectedExtension = MaterialFileTypeHelper.GetExpectedExtension(materialType);

            if (!normalizedPath.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
                return new BulkMaterialEditRow(normalizedPath, null, null, false, "Incompatible material type.");

            if (!MaterialFilePersistence.TryLoadMaterial(normalizedPath, out BaseMaterialFile material, out bool isJson, out string errorMessage))
                return new BulkMaterialEditRow(normalizedPath, null, null, isJson, errorMessage ?? "Failed to load material.");

            if (MaterialFileTypeHelper.GetMaterialType(material) != materialType)
                return new BulkMaterialEditRow(normalizedPath, null, null, isJson, "Incompatible material type.");

            var row = new BulkMaterialEditRow(
                normalizedPath,
                material,
                MaterialFileCloner.Clone(material),
                isJson,
                string.Empty);

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
                cell.Refresh(OriginalMaterial, Material, clearError: false);
        }
    }

    internal sealed class BulkMaterialEditSession
    {
        private readonly Dictionary<string, MaterialFieldDescriptor> descriptorsByLabel;
        private readonly List<BulkMaterialEditRow> rows;

        private BulkMaterialEditSession(MaterialType materialType, IReadOnlyList<MaterialFieldDescriptor> descriptors)
        {
            MaterialType = materialType;
            AllDescriptors = descriptors;
            descriptorsByLabel = descriptors.ToDictionary(descriptor => descriptor.Label, StringComparer.OrdinalIgnoreCase);
            rows = new List<BulkMaterialEditRow>();
        }

        public MaterialType MaterialType { get; }
        public IReadOnlyList<MaterialFieldDescriptor> AllDescriptors { get; }
        public IReadOnlyList<BulkMaterialEditRow> Rows => rows;
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

        public bool TrySetCellValue(BulkMaterialEditRow row, MaterialFieldDescriptor descriptor, object inputValue, out string errorMessage)
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
            return cell.TrySetValue(row.Material, inputValue, out errorMessage);
        }

        public BulkMaterialSessionAddResult AddFiles(IEnumerable<string> targetFiles)
        {
            var normalizedPaths = (targetFiles ?? Array.Empty<string>())
                .Select(MaterialFilePersistence.NormalizePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
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

                BulkMaterialEditRow row = BulkMaterialEditRow.Load(path, MaterialType, AllDescriptors);
                addedRows.Add(row);
            }

            rows.AddRange(addedRows);
            rows.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.FilePath, right.FilePath));

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

            return rows.RemoveAll(row => rowsToRemove.Contains(row.FilePath));
        }

        public IReadOnlyList<FieldCopyResult> ApplyChanges(bool backupBeforeWrite)
        {
            return ApplyRows(rows, backupBeforeWrite);
        }

        public IReadOnlyList<FieldCopyResult> ApplySelectedChanges(IEnumerable<BulkMaterialEditRow> selectedRows, bool backupBeforeWrite)
        {
            return ApplyRows(
                (selectedRows ?? Array.Empty<BulkMaterialEditRow>())
                    .Where(row => row != null)
                    .Distinct()
                    .ToArray(),
                backupBeforeWrite);
        }

        private IReadOnlyList<FieldCopyResult> ApplyRows(IEnumerable<BulkMaterialEditRow> targetRows, bool backupBeforeWrite)
        {
            var results = new List<FieldCopyResult>();

            foreach (BulkMaterialEditRow row in targetRows ?? Array.Empty<BulkMaterialEditRow>())
            {
                if (row.HasLoadError)
                {
                    results.Add(new FieldCopyResult(row.FilePath, FieldCopyStatus.Failed, row.LoadError));
                    continue;
                }

                if (row.HasValidationErrors)
                {
                    results.Add(new FieldCopyResult(row.FilePath, FieldCopyStatus.Failed, "Resolve validation errors before applying."));
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
                    backupBeforeWrite);
                if (saveResult.Status == FieldCopyStatus.Success)
                {
                    row.AcceptChanges();
                }

                results.Add(saveResult);
            }

            return results;
        }
    }

    internal sealed class BulkMaterialSessionAddResult
    {
        public IReadOnlyList<BulkMaterialEditRow> AddedRows { get; init; } = Array.Empty<BulkMaterialEditRow>();
        public int DuplicateCount { get; init; }
    }
}
