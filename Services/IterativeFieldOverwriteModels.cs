using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Material_Editor.AdvancedVariant;

namespace Material_Editor.Services
{
    public sealed class FieldOverwriteOptions
    {
        public IReadOnlyList<MaterialFieldDescriptor> Descriptors { get; init; } = Array.Empty<MaterialFieldDescriptor>();
        public IReadOnlyList<string> TargetFiles { get; init; } = Array.Empty<string>();
        public bool BackupBeforeWrite { get; init; }
        public IterativeFieldOverwriteOptions IterativeOptions { get; init; }
    }

    public sealed class IterativeFieldOverwriteOptions
    {
        public int StartIndex { get; init; } = 1;
        public int Count { get; init; } = 1;
        public int Step { get; init; } = 1;
        public IReadOnlyList<IterativeFieldAssignment> Assignments { get; init; } = Array.Empty<IterativeFieldAssignment>();
        public IReadOnlyList<IterativeTargetOverride> Targets { get; init; } = Array.Empty<IterativeTargetOverride>();
        public IReadOnlyList<IterativeFieldValueOverride> FieldValueOverrides { get; init; } = Array.Empty<IterativeFieldValueOverride>();
        public AdvancedVariantOptions AdvancedVariant { get; init; }
    }

    public sealed class IterativeFieldAssignment
    {
        public IterativeFieldAssignment(MaterialFieldDescriptor descriptor, string pattern)
        {
            Descriptor = descriptor;
            Pattern = pattern ?? string.Empty;
        }

        public IterativeFieldAssignment(MaterialFieldDescriptor descriptor, float startValue, float stepValue)
        {
            Descriptor = descriptor;
            NumericStartValue = startValue;
            NumericStepValue = stepValue;
            Pattern = string.Empty;
        }

        public MaterialFieldDescriptor Descriptor { get; }
        public string Pattern { get; }
        public float? NumericStartValue { get; }
        public float? NumericStepValue { get; }

        public bool IsNumericSequence => NumericStartValue.HasValue && NumericStepValue.HasValue;
    }

    public sealed class IterativeTargetOverride
    {
        public IterativeTargetOverride(string targetPath, string indexTok, bool isEnabled = true)
        {
            TargetPath = targetPath ?? string.Empty;
            IndexTok = indexTok ?? string.Empty;
            IsEnabled = isEnabled;
        }

        public string TargetPath { get; }
        public string IndexTok { get; }
        public bool IsEnabled { get; }
    }

    public sealed class IterativeFieldValueOverride
    {
        public IterativeFieldValueOverride(string targetPath, string fieldLabel, string value)
        {
            TargetPath = targetPath ?? string.Empty;
            FieldLabel = fieldLabel ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string TargetPath { get; }
        public string FieldLabel { get; }
        public string Value { get; }
    }

    internal sealed class IterativeTargetContext : IIndexedTokenContext
    {
        public IterativeTargetContext(
            string targetPath,
            bool isCompatibleType,
            bool selectedByIteration,
            bool manuallyEnabled,
            bool willApply,
            int compatibleOrder,
            int? appliedSequence,
            int? appliedOrdinal,
            int? indexValue,
            string indexTok,
            AdvancedVariantResolvedContext advancedContext)
        {
            TargetPath = targetPath ?? string.Empty;
            IsCompatibleType = isCompatibleType;
            SelectedByIteration = selectedByIteration;
            ManuallyEnabled = manuallyEnabled;
            WillApply = willApply;
            CompatibleOrder = compatibleOrder;
            AppliedSequence = appliedSequence;
            AppliedOrdinal = appliedOrdinal;
            IndexValue = indexValue;
            AdvancedContext = advancedContext;
            Index = advancedContext?.Index ?? (indexValue.HasValue
                ? indexValue.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty);
            IndexNN = advancedContext?.IndexNN ?? (indexValue.HasValue
                ? indexValue.Value.ToString("00", CultureInfo.InvariantCulture)
                : string.Empty);
            IndexTok = advancedContext?.IndexTok
                ?? (string.IsNullOrWhiteSpace(indexTok) ? Index : indexTok.Trim());
            IndexTokFallback = advancedContext != null
                ? advancedContext.IndexTokFallback
                : string.IsNullOrWhiteSpace(indexTok);
        }

        public string TargetPath { get; }
        public bool IsCompatibleType { get; }
        public bool SelectedByIteration { get; }
        public bool ManuallyEnabled { get; }
        public bool WillApply { get; }
        public int CompatibleOrder { get; }
        public int? AppliedSequence { get; }
        public int? AppliedOrdinal { get; }
        public int? IndexValue { get; }
        public AdvancedVariantResolvedContext AdvancedContext { get; }
        public string Index { get; }
        public string IndexNN { get; }
        public string IndexTok { get; }
        public bool IndexTokFallback { get; }

        public string GetLayerIndexText(int zeroBasedLayerIndex) => AdvancedContext?.GetLayerIndexText(zeroBasedLayerIndex) ?? string.Empty;
        public string GetLayerIndexNNText(int zeroBasedLayerIndex) => AdvancedContext?.GetLayerIndexNNText(zeroBasedLayerIndex) ?? string.Empty;
        public string GetLayerIndexToken(int zeroBasedLayerIndex) => AdvancedContext?.GetLayerIndexToken(zeroBasedLayerIndex) ?? string.Empty;
    }

    internal static class IterativeFieldOverwritePlanner
    {
        public static IReadOnlyList<IterativeTargetContext> BuildContexts(MaterialType materialType, IReadOnlyList<string> targetFiles, IterativeFieldOverwriteOptions options)
        {
            options ??= new IterativeFieldOverwriteOptions();

            int startIndex = options.StartIndex;
            int count = Math.Max(0, options.Count);
            int step = Math.Max(1, options.Step);
            string expectedExtension = MaterialFileTypeHelper.GetExpectedExtension(materialType);
            AdvancedVariantResolvedContext[] advancedContexts = ResolveAdvancedContexts(options.AdvancedVariant);
            var targetOverrides = (options.Targets ?? Array.Empty<IterativeTargetOverride>())
                .GroupBy(item => MaterialFilePersistence.NormalizePath(item.TargetPath), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

            var orderedTargets = (targetFiles ?? Array.Empty<string>())
                .Select(MaterialFilePersistence.NormalizePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var contexts = new List<IterativeTargetContext>(orderedTargets.Length);
            int compatibleOrder = 0;
            int appliedCount = 0;
            int advancedContextIndex = 0;

            foreach (string targetPath in orderedTargets)
            {
                bool isCompatibleType = targetPath.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase);
                int targetCompatibleOrder = isCompatibleType ? compatibleOrder++ : -1;
                bool selectedByIteration = isCompatibleType
                    && count > 0
                    && targetCompatibleOrder % step == 0
                    && appliedCount < count;
                bool manuallyEnabled = !targetOverrides.TryGetValue(targetPath, out IterativeTargetOverride targetOverride)
                    || targetOverride.IsEnabled;
                bool willApply = selectedByIteration && manuallyEnabled;

                int? appliedSequence = null;
                int? appliedOrdinal = null;
                int? indexValue = null;

                if (selectedByIteration)
                {
                    appliedSequence = appliedCount + 1;
                    appliedOrdinal = appliedCount;
                    indexValue = startIndex + appliedCount * step;
                    appliedCount++;
                }

                string indexTok = targetOverride?.IndexTok ?? string.Empty;
                AdvancedVariantResolvedContext advancedContext = selectedByIteration && advancedContextIndex < advancedContexts.Length
                    ? advancedContexts[advancedContextIndex++]
                    : null;
                contexts.Add(new IterativeTargetContext(
                    targetPath,
                    isCompatibleType,
                    selectedByIteration,
                    manuallyEnabled,
                    willApply,
                    targetCompatibleOrder,
                    appliedSequence,
                    appliedOrdinal,
                    indexValue,
                    indexTok,
                    advancedContext));
            }

            return contexts;
        }

        internal static AdvancedVariantResolvedContext[] ResolveAdvancedContexts(AdvancedVariantOptions options)
        {
            if (options == null || options.Layers == null || options.Layers.Count == 0)
                return Array.Empty<AdvancedVariantResolvedContext>();

            if (AdvancedVariantEngine.Validate(options).Count > 0)
                return Array.Empty<AdvancedVariantResolvedContext>();

            try
            {
                return AdvancedVariantEngine.Resolve(options)
                    .Where(context => context.Enabled)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<AdvancedVariantResolvedContext>();
            }
        }
    }
}
