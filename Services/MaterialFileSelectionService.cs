using Material_Editor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Material_Editor.Services
{
    internal sealed class MaterialFileSelectionSummary
    {
        public IReadOnlyList<string> MaterialFiles { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> EffectFiles { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> UnsupportedFiles { get; init; } = Array.Empty<string>();

        public bool HasMixedMaterialTypes => MaterialFiles.Count > 0 && EffectFiles.Count > 0;

        public IReadOnlyList<string> GetFiles(MaterialType materialType)
        {
            return materialType == MaterialType.Effect ? EffectFiles : MaterialFiles;
        }
    }

    internal static class MaterialFileSelectionService
    {
        public static MaterialFileSelectionSummary Analyze(IEnumerable<string> paths)
        {
            var normalizedPaths = (paths ?? Array.Empty<string>())
                .Select(MaterialFilePersistence.NormalizePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var materialFiles = new List<string>();
            var effectFiles = new List<string>();
            var unsupportedFiles = new List<string>();

            foreach (string path in normalizedPaths)
            {
                string extension = Path.GetExtension(path);
                if (extension.Equals(".bgsm", StringComparison.OrdinalIgnoreCase))
                    materialFiles.Add(path);
                else if (extension.Equals(".bgem", StringComparison.OrdinalIgnoreCase))
                    effectFiles.Add(path);
                else
                    unsupportedFiles.Add(path);
            }

            return new MaterialFileSelectionSummary
            {
                MaterialFiles = materialFiles,
                EffectFiles = effectFiles,
                UnsupportedFiles = unsupportedFiles
            };
        }

        public static IReadOnlyList<string> EnumerateFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return Array.Empty<string>();

            try
            {
                return Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .Where(path =>
                    {
                        string extension = Path.GetExtension(path);
                        return extension.Equals(".bgsm", StringComparison.OrdinalIgnoreCase)
                            || extension.Equals(".bgem", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
