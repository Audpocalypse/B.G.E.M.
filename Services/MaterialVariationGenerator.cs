using Material_Editor.AdvancedVariant;
using MaterialLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Material_Editor.Services
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
        public AdvancedVariantOptions AdvancedVariant { get; init; }
    }

    public static class MaterialVariationGenerator
    {
        public static bool ContainsIndexPlaceholder(string pattern)
        {
            return MaterialVariationTokenExpander.ContainsLegacyIndexPlaceholder(pattern);
        }

        public static IReadOnlyList<FieldCopyResult> Generate(BaseMaterialFile template, MaterialVariationOptions options, bool serializeAsJson)
        {
            if (template == null || options == null)
                return new[] { new FieldCopyResult(string.Empty, FieldCopyStatus.Failed, "Template or options missing.") };

            IReadOnlyList<VariationWorkItem> workItems = options.AdvancedVariant == null
                ? BuildLegacyWorkItems(options)
                : BuildAdvancedWorkItems(options);

            return ExecuteGeneration(template, options, serializeAsJson, workItems);
        }

        public static string NormalizeLegacyOutputPatternForAdvancedMode(string pattern)
        {
            return MaterialVariationTokenExpander.ReplaceLegacyIndexPlaceholders(pattern, "{indexNN}");
        }

        private static string FormatWithIndex(string pattern, int index)
        {
            if (!ContainsIndexPlaceholder(pattern))
                throw new FormatException("Pattern must include an {index} placeholder.");

            return MaterialVariationTokenExpander.ExpandLegacy(pattern, index);
        }

        public static IReadOnlyList<string> PreviewOutputPaths(MaterialVariationOptions options)
        {
            var paths = new List<string>();
            if (options == null || string.IsNullOrWhiteSpace(options.OutputPattern))
                return paths;

            if (options.AdvancedVariant == null)
            {
                if (options.Count <= 0)
                    return paths;

                for (int i = 0; i < options.Count; i++)
                {
                    int currentIndex = options.StartIndex + i * options.Step;
                    paths.Add(FormatWithIndex(options.OutputPattern, currentIndex));
                }

                return paths;
            }

            foreach (var context in AdvancedVariantEngine.Resolve(options.AdvancedVariant).Where(context => context.Enabled))
                paths.Add(AdvancedVariantEngine.ExpandTokens(options.OutputPattern, context));

            return paths;
        }

        public static string PreviewFieldValue(string pattern, int index)
        {
            return MaterialVariationTokenExpander.ExpandLegacy(pattern, index);
        }

        public static string PreviewFieldValue(string pattern, AdvancedVariantResolvedContext context)
        {
            return MaterialVariationTokenExpander.Expand(pattern, null, context);
        }

        private static IReadOnlyList<VariationWorkItem> BuildLegacyWorkItems(MaterialVariationOptions options)
        {
            var workItems = new List<VariationWorkItem>();
            if (!ContainsIndexPlaceholder(options.OutputPattern))
            {
                workItems.Add(new VariationWorkItem
                {
                    TargetPath = options.OutputPattern ?? string.Empty,
                    ValidationError = "Output pattern must contain an {index} placeholder."
                });
                return workItems;
            }

            if (options.Count <= 0)
            {
                workItems.Add(new VariationWorkItem
                {
                    TargetPath = options.OutputPattern ?? string.Empty,
                    ValidationError = "Count must be at least 1."
                });
                return workItems;
            }

            for (int i = 0; i < options.Count; i++)
            {
                int currentIndex = options.StartIndex + i * options.Step;
                try
                {
                    string targetPath = FormatWithIndex(options.OutputPattern, currentIndex);
                    if (string.IsNullOrWhiteSpace(targetPath))
                        throw new FormatException("Output path resolved to empty string.");

                    workItems.Add(new VariationWorkItem
                    {
                        SequenceIndex = i,
                        LegacyIndex = currentIndex,
                        TargetPath = targetPath
                    });
                }
                catch (Exception ex)
                {
                    workItems.Add(new VariationWorkItem
                    {
                        SequenceIndex = i,
                        LegacyIndex = currentIndex,
                        TargetPath = string.Empty,
                        ValidationError = $"Failed to evaluate output pattern: {ex.Message}"
                    });
                }
            }

            FinalizeWorkItems(workItems);
            return workItems;
        }

        private static IReadOnlyList<VariationWorkItem> BuildAdvancedWorkItems(MaterialVariationOptions options)
        {
            var workItems = new List<VariationWorkItem>();
            if (string.IsNullOrWhiteSpace(options.OutputPattern))
            {
                workItems.Add(new VariationWorkItem
                {
                    ValidationError = "Output pattern is required for advanced variant generation."
                });
                return workItems;
            }

            IReadOnlyList<AdvancedVariantValidationIssue> issues = AdvancedVariantEngine.Validate(options.AdvancedVariant);
            if (issues.Count > 0)
            {
                foreach (var issue in issues)
                {
                    workItems.Add(new VariationWorkItem
                    {
                        TargetPath = options.OutputPattern,
                        ValidationError = issue.Message
                    });
                }

                return workItems;
            }

            foreach (var context in AdvancedVariantEngine.Resolve(options.AdvancedVariant).Where(context => context.Enabled))
            {
                try
                {
                    string targetPath = AdvancedVariantEngine.ExpandTokens(options.OutputPattern, context);
                    if (string.IsNullOrWhiteSpace(targetPath))
                        throw new FormatException("Output path resolved to empty string.");

                    workItems.Add(new VariationWorkItem
                    {
                        SequenceIndex = context.Ordinal,
                        AdvancedContext = context,
                        TargetPath = targetPath
                    });
                }
                catch (Exception ex)
                {
                    workItems.Add(new VariationWorkItem
                    {
                        SequenceIndex = context.Ordinal,
                        AdvancedContext = context,
                        TargetPath = string.Empty,
                        ValidationError = $"Failed to evaluate advanced output pattern: {ex.Message}"
                    });
                }
            }

            FinalizeWorkItems(workItems);
            return workItems;
        }

        private static void FinalizeWorkItems(IReadOnlyList<VariationWorkItem> workItems)
        {
            MaterialFilePersistence.FinalizeOutputPaths(
                workItems,
                item => item.TargetPath,
                item => item.ValidationError,
                (item, normalizedPath) => item.NormalizedPath = normalizedPath,
                (item, error) => item.ValidationError = error,
                item => item.NormalizedPath,
                "Duplicate target path; skipping.");
        }

        private static IReadOnlyList<FieldCopyResult> ExecuteGeneration(
            BaseMaterialFile template,
            MaterialVariationOptions options,
            bool serializeAsJson,
            IReadOnlyList<VariationWorkItem> workItems)
        {
            var results = new List<FieldCopyResult>();
            var fields = options.Fields ?? Array.Empty<MaterialVariationFieldAssignment>();

            foreach (var workItem in workItems)
            {
                if (!string.IsNullOrEmpty(workItem.ValidationError))
                {
                    results.Add(new FieldCopyResult(workItem.TargetPath ?? string.Empty, FieldCopyStatus.Failed, workItem.ValidationError));
                    continue;
                }

                BaseMaterialFile clone = MaterialFileCloner.Clone(template);
                if (options.VaryGreyscaleToPaletteScale && clone is BGSM greyscaleClone)
                {
                    float grayscaledValue = options.GreyscaleToPaletteScaleStart + options.GreyscaleToPaletteScaleStep * workItem.SequenceIndex;
                    greyscaleClone.GrayscaleToPaletteScale = grayscaledValue;
                }

                foreach (var field in fields)
                {
                    try
                    {
                        string value = MaterialVariationTokenExpander.Expand(field.Pattern, workItem.LegacyIndex, workItem.AdvancedContext);
                        field.Descriptor.SetValue(clone, value);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new FieldCopyResult(workItem.NormalizedPath, FieldCopyStatus.Failed, $"Failed to apply field '{field.Descriptor.Label}': {ex.Message}"));
                        clone = null;
                        break;
                    }
                }

                if (clone == null)
                    continue;

                results.Add(MaterialFilePersistence.SaveMaterialResult(
                    workItem.NormalizedPath,
                    clone,
                    serializeAsJson,
                    "Variation generated successfully.",
                    failureMessagePrefix: "Failed to save material: "));
            }

            return results;
        }

        private sealed class VariationWorkItem
        {
            public int SequenceIndex { get; init; }
            public int? LegacyIndex { get; init; }
            public AdvancedVariantResolvedContext AdvancedContext { get; init; }
            public string TargetPath { get; set; }
            public string NormalizedPath { get; set; }
            public string ValidationError { get; set; }
        }
    }
}
