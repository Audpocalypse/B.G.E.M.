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

                BaseMaterialFile clone = MaterialFileCloner.Clone(item.Row.Material);
                results.Add(MaterialFilePersistence.SaveMaterialResult(
                    item.NormalizedPath ?? item.TargetPath ?? string.Empty,
                    clone,
                    serializeAsJson,
                    "Exported successfully."));
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
            MaterialFilePersistence.FinalizeOutputPaths(
                workItems,
                item => item.TargetPath,
                item => item.ValidationError,
                (item, normalizedPath) => item.NormalizedPath = normalizedPath,
                (item, error) => item.ValidationError = error,
                item => item.NormalizedPath,
                "Duplicate output path.");
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
