using MaterialLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Material_Editor.Services
{
    internal static class BulkMaterialExportService
    {
        public static IReadOnlyList<string> PreviewOutputPaths(IEnumerable<BulkMaterialEditRow> rows, string outputPattern)
        {
            return BuildWorkItems(rows, outputPattern)
                .Select(item => item.TargetPath)
                .ToArray();
        }

        public static IReadOnlyList<FieldCopyResult> Export(IEnumerable<BulkMaterialEditRow> rows, string outputPattern, bool serializeAsJson)
        {
            var results = new List<FieldCopyResult>();
            foreach (ExportWorkItem item in BuildWorkItems(rows, outputPattern))
            {
                if (!string.IsNullOrWhiteSpace(item.ValidationError))
                {
                    results.Add(new FieldCopyResult(item.TargetPath ?? string.Empty, FieldCopyStatus.Failed, item.ValidationError));
                    continue;
                }

                if (item.Row.HasLoadError)
                {
                    results.Add(new FieldCopyResult(item.Row.FilePath, FieldCopyStatus.Failed, item.Row.LoadError));
                    continue;
                }

                try
                {
                    string directory = Path.GetDirectoryName(item.NormalizedPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);

                    BaseMaterialFile clone = MaterialFileCloner.Clone(item.Row.Material);
                    MaterialFilePersistence.SaveMaterial(item.NormalizedPath, clone, serializeAsJson);
                    results.Add(new FieldCopyResult(item.NormalizedPath, FieldCopyStatus.Success, "Exported successfully."));
                }
                catch (Exception ex)
                {
                    results.Add(new FieldCopyResult(item.NormalizedPath ?? item.TargetPath ?? string.Empty, FieldCopyStatus.Failed, ex.Message));
                }
            }

            return results;
        }

        private static IReadOnlyList<ExportWorkItem> BuildWorkItems(IEnumerable<BulkMaterialEditRow> rows, string outputPattern)
        {
            var workItems = new List<ExportWorkItem>();
            int index = 0;
            foreach (BulkMaterialEditRow row in (rows ?? Array.Empty<BulkMaterialEditRow>()).Where(row => row != null))
            {
                workItems.Add(BuildWorkItem(row, outputPattern, index));
                index++;
            }

            FinalizeWorkItems(workItems);
            return workItems;
        }

        private static ExportWorkItem BuildWorkItem(BulkMaterialEditRow row, string outputPattern, int sequenceIndex)
        {
            string sourceFileName = Path.GetFileNameWithoutExtension(row.FilePath);
            string sourceExtension = Path.GetExtension(row.FilePath);
            string evaluatedPath = (outputPattern ?? string.Empty)
                .Replace("{name}", sourceFileName, StringComparison.OrdinalIgnoreCase)
                .Replace("{ext}", sourceExtension, StringComparison.OrdinalIgnoreCase)
                .Replace("{indexNN}", (sequenceIndex + 1).ToString("00"), StringComparison.OrdinalIgnoreCase)
                .Replace("{index}", (sequenceIndex + 1).ToString(), StringComparison.OrdinalIgnoreCase);

            return new ExportWorkItem
            {
                Row = row,
                TargetPath = evaluatedPath
            };
        }

        private static void FinalizeWorkItems(IReadOnlyList<ExportWorkItem> workItems)
        {
            foreach (ExportWorkItem item in workItems)
            {
                if (string.IsNullOrWhiteSpace(item.TargetPath))
                {
                    item.ValidationError = "Output pattern resolved to an empty path.";
                    continue;
                }

                try
                {
                    item.NormalizedPath = Path.GetFullPath(item.TargetPath);
                }
                catch (Exception ex)
                {
                    item.ValidationError = $"Invalid output path: {ex.Message}";
                }
            }

            var duplicates = workItems
                .Where(item => string.IsNullOrWhiteSpace(item.ValidationError))
                .GroupBy(item => item.NormalizedPath, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);

            foreach (var duplicateGroup in duplicates)
            {
                foreach (ExportWorkItem item in duplicateGroup)
                    item.ValidationError = "Duplicate output path.";
            }
        }

        private sealed class ExportWorkItem
        {
            public BulkMaterialEditRow Row { get; init; }
            public string TargetPath { get; init; }
            public string NormalizedPath { get; set; }
            public string ValidationError { get; set; }
        }
    }
}
