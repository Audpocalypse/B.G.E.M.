using MaterialLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Material_Editor
{
    public sealed class MaterialVariationFieldAssignment
    {
        public MaterialVariationFieldAssignment(MaterialFieldDescriptor descriptor, string pattern)
        {
            Descriptor = descriptor;
            Pattern = pattern ?? string.Empty;
        }

        public MaterialFieldDescriptor Descriptor { get; }
        public string Pattern { get; }
    }

    public sealed class MaterialVariationOptions
    {
        public int StartIndex { get; init; } = 1;
        public int Count { get; init; } = 1;
        public int Step { get; init; } = 1;
        public string OutputPattern { get; init; }
        public IReadOnlyList<MaterialVariationFieldAssignment> Fields { get; init; } = Array.Empty<MaterialVariationFieldAssignment>();
        public bool VaryGreyscaleToPaletteScale { get; init; }
        public float GreyscaleToPaletteScaleStart { get; init; } = 1f;
        public float GreyscaleToPaletteScaleStep { get; init; }
    }

    public static class MaterialVariationGenerator
    {
        private static readonly Regex IndexPlaceholderRegex = new(@"\{index(?:\:([^\}]+))?\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool ContainsIndexPlaceholder(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            return IndexPlaceholderRegex.IsMatch(pattern);
        }

        public static IReadOnlyList<FieldCopyResult> Generate(BaseMaterialFile template, MaterialVariationOptions options, bool serializeAsJson)
        {
            var results = new List<FieldCopyResult>();

            if (template == null || options == null)
            {
                results.Add(new FieldCopyResult(string.Empty, FieldCopyStatus.Failed, "Template or options missing."));
                return results;
            }

            if (!ContainsIndexPlaceholder(options.OutputPattern))
            {
                results.Add(new FieldCopyResult(string.Empty, FieldCopyStatus.Failed, "Output pattern must contain an {index} placeholder."));
                return results;
            }

            if (options.Count <= 0)
            {
                results.Add(new FieldCopyResult(string.Empty, FieldCopyStatus.Failed, "Count must be at least 1."));
                return results;
            }

            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < options.Count; i++)
            {
                int currentIndex = options.StartIndex + i * options.Step;
                string targetPath;
                try
                {
                    targetPath = FormatWithIndex(options.OutputPattern, currentIndex);
                    if (string.IsNullOrEmpty(targetPath))
                        throw new FormatException("Output path resolved to empty string.");
                }
                catch (Exception ex)
                {
                    results.Add(new FieldCopyResult(string.Empty, FieldCopyStatus.Failed, $"Failed to evaluate output pattern: {ex.Message}"));
                    continue;
                }

                string normalizedPath;
                try
                {
                    normalizedPath = Path.GetFullPath(targetPath);
                }
                catch (Exception ex)
                {
                    results.Add(new FieldCopyResult(targetPath, FieldCopyStatus.Failed, $"Invalid target path: {ex.Message}"));
                    continue;
                }

                if (!seenPaths.Add(normalizedPath))
                {
                    results.Add(new FieldCopyResult(normalizedPath, FieldCopyStatus.Failed, "Duplicate target path; skipping."));
                    continue;
                }

            BaseMaterialFile clone = MaterialFileCloner.Clone(template);

            if (options.VaryGreyscaleToPaletteScale && clone is BGSM greyscaleClone)
            {
                float grayscaledValue = options.GreyscaleToPaletteScaleStart + options.GreyscaleToPaletteScaleStep * i;
                greyscaleClone.GrayscaleToPaletteScale = grayscaledValue;
            }

            foreach (var field in options.Fields)
            {
                try
                {
                    string value = ReplaceIndexPlaceholders(field.Pattern, currentIndex);
                        field.Descriptor.SetValue(clone, value);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new FieldCopyResult(normalizedPath, FieldCopyStatus.Failed, $"Failed to apply field '{field.Descriptor.Label}': {ex.Message}"));
                        clone = null;
                        break;
                    }
                }

                if (clone == null)
                    continue;

                try
                {
                    var directory = Path.GetDirectoryName(normalizedPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    using var stream = new FileStream(normalizedPath, FileMode.Create, FileAccess.Write);

                    if (serializeAsJson)
                    {
                        using var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  ");
                        var serializer = new DataContractJsonSerializer(clone.GetType(), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                        serializer.WriteObject(writer, clone);
                        writer.Flush();
                    }
                    else
                    {
                        if (!clone.Save(stream))
                        {
                            throw new IOException("Failed to write binary material.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new FieldCopyResult(normalizedPath, FieldCopyStatus.Failed, $"Failed to save material: {ex.Message}"));
                    continue;
                }

                results.Add(new FieldCopyResult(normalizedPath, FieldCopyStatus.Success, "Variation generated successfully."));
            }

            return results;
        }

        public static string FormatWithIndex(string pattern, int index)
        {
            if (!ContainsIndexPlaceholder(pattern))
                throw new FormatException("Pattern must include an {index} placeholder.");

            return ReplaceIndexPlaceholders(pattern, index);
        }

        public static IReadOnlyList<string> PreviewOutputPaths(MaterialVariationOptions options)
        {
            var paths = new List<string>();
            if (options == null || options.Count <= 0 || string.IsNullOrWhiteSpace(options.OutputPattern))
                return paths;

            for (int i = 0; i < options.Count; i++)
            {
                int currentIndex = options.StartIndex + i * options.Step;
                paths.Add(FormatWithIndex(options.OutputPattern, currentIndex));
            }

            return paths;
        }

        public static string PreviewFieldValue(string pattern, int index)
        {
            return ReplaceIndexPlaceholders(pattern, index);
        }

        private static string ReplaceIndexPlaceholders(string pattern, int index)
        {
            if (string.IsNullOrEmpty(pattern))
                return pattern;

            return IndexPlaceholderRegex.Replace(pattern, match =>
            {
                string fmt = match.Groups[1].Value;
                if (string.IsNullOrEmpty(fmt))
                    return index.ToString(CultureInfo.InvariantCulture);

                try
                {
                    return index.ToString(fmt, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return index.ToString(CultureInfo.InvariantCulture);
                }
            });
        }
    }
}
