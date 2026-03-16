using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Material_Editor.AdvancedVariant
{
    internal static class AdvancedVariantEngine
    {
        public static IReadOnlyList<AdvancedVariantValidationIssue> Validate(AdvancedVariantOptions options)
        {
            var issues = new List<AdvancedVariantValidationIssue>();
            if (options == null)
            {
                issues.Add(new AdvancedVariantValidationIssue("Advanced variant options are required."));
                return issues;
            }

            var layers = options.Layers ?? Array.Empty<AdvancedVariantLayerDefinition>();
            if (layers.Count == 0)
                issues.Add(new AdvancedVariantValidationIssue("At least one advanced variant layer is required."));

            if (layers.Count > 4)
                issues.Add(new AdvancedVariantValidationIssue("Advanced variant generation supports up to 4 layers."));

            var seenLayerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                string layerName = layer?.Name?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(layerName))
                {
                    issues.Add(new AdvancedVariantValidationIssue($"Layer {i + 1} is missing a name."));
                }
                else if (!seenLayerNames.Add(layerName))
                {
                    issues.Add(new AdvancedVariantValidationIssue($"Duplicate advanced variant layer name '{layerName}'."));
                }

                if (layer == null || layer.Indices == null || layer.Indices.Count == 0)
                {
                    issues.Add(new AdvancedVariantValidationIssue($"Layer {i + 1} must define at least one index."));
                    continue;
                }

                var seenIndices = new HashSet<int>();
                foreach (int index in layer.Indices)
                {
                    if (!seenIndices.Add(index))
                        issues.Add(new AdvancedVariantValidationIssue($"Layer '{layerName}' contains duplicate index value {index}."));
                }
            }

            var rules = options.Rules ?? Array.Empty<AdvancedVariantRule>();
            foreach (var rule in rules)
            {
                ValidateRuleLayer(rule.Layer1Index, 1, layers, issues);
                ValidateRuleLayer(rule.Layer2Index, 2, layers, issues);
                ValidateRuleLayer(rule.Layer3Index, 3, layers, issues);
                ValidateRuleLayer(rule.Layer4Index, 4, layers, issues);
            }

            return issues;
        }

        public static IReadOnlyList<AdvancedVariantResolvedContext> Resolve(AdvancedVariantOptions options)
        {
            var issues = Validate(options);
            if (issues.Count > 0)
                throw new InvalidOperationException(issues[0].Message);

            var resolved = new List<AdvancedVariantResolvedContext>();
            var layers = options.Layers;
            var activeIndices = new int[layers.Count];
            int ordinal = 0;

            ExpandLayer(options, layers, 0, activeIndices, resolved, ref ordinal);
            return resolved;
        }

        internal static string ExpandTokens(string pattern, AdvancedVariantResolvedContext context)
        {
            return MaterialVariationTokenExpander.Expand(pattern, null, context);
        }

        private static void ExpandLayer(
            AdvancedVariantOptions options,
            IReadOnlyList<AdvancedVariantLayerDefinition> layers,
            int depth,
            int[] activeIndices,
            List<AdvancedVariantResolvedContext> resolved,
            ref int ordinal)
        {
            if (depth >= layers.Count)
            {
                int[] layerIndices = activeIndices.ToArray();
                string indexToken = ResolveIndexToken(options, layerIndices, out bool indexTokenFallback);
                string[] layerTokens = ResolveLayerTokens(options, layerIndices, out bool[] layerTokenFallbacks);
                bool enabled = ResolveEnabled(options, layerIndices);
                resolved.Add(new AdvancedVariantResolvedContext(
                    ordinal++,
                    layerIndices,
                    BuildCombinedIndex(layerIndices, padded: false),
                    BuildCombinedIndex(layerIndices, padded: true),
                    indexToken,
                    indexTokenFallback,
                    layerTokens,
                    layerTokenFallbacks,
                    enabled));
                return;
            }

            foreach (int index in layers[depth].Indices)
            {
                activeIndices[depth] = index;
                ExpandLayer(options, layers, depth + 1, activeIndices, resolved, ref ordinal);
            }
        }

        private static string ResolveIndexToken(AdvancedVariantOptions options, IReadOnlyList<int> layerIndices, out bool fallbackUsed)
        {
            string resolved = null;
            foreach (var orderedRule in GetOrderedMatchingRules(options, layerIndices))
            {
                if (!string.IsNullOrWhiteSpace(orderedRule.Rule.IndexToken))
                    resolved = orderedRule.Rule.IndexToken;
            }

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                fallbackUsed = false;
                return resolved;
            }

            fallbackUsed = true;
            return BuildCombinedIndex(layerIndices, padded: false);
        }

        private static string[] ResolveLayerTokens(AdvancedVariantOptions options, IReadOnlyList<int> layerIndices, out bool[] fallbackUsed)
        {
            string[] resolved = new string[Math.Min(4, layerIndices.Count)];
            fallbackUsed = new bool[resolved.Length];

            foreach (var orderedRule in GetOrderedMatchingRules(options, layerIndices))
            {
                ApplyLayerTokenOverride(GetInferredLayerTokenOverride(orderedRule.Rule, 0), 0, resolved);
                ApplyLayerTokenOverride(GetInferredLayerTokenOverride(orderedRule.Rule, 1), 1, resolved);
                ApplyLayerTokenOverride(GetInferredLayerTokenOverride(orderedRule.Rule, 2), 2, resolved);
                ApplyLayerTokenOverride(GetInferredLayerTokenOverride(orderedRule.Rule, 3), 3, resolved);
                ApplyLayerTokenOverride(orderedRule.Rule.Layer1Token, 0, resolved);
                ApplyLayerTokenOverride(orderedRule.Rule.Layer2Token, 1, resolved);
                ApplyLayerTokenOverride(orderedRule.Rule.Layer3Token, 2, resolved);
                ApplyLayerTokenOverride(orderedRule.Rule.Layer4Token, 3, resolved);
            }

            for (int index = 0; index < resolved.Length; index++)
            {
                if (!string.IsNullOrWhiteSpace(resolved[index]))
                    continue;

                resolved[index] = layerIndices[index].ToString(CultureInfo.InvariantCulture);
                fallbackUsed[index] = true;
            }

            return resolved;
        }

        private static string GetInferredLayerTokenOverride(AdvancedVariantRule rule, int zeroBasedLayerIndex)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.IndexToken))
                return null;

            if (GetSpecificity(rule) != 1)
                return null;

            return IsLayerSpecified(rule, zeroBasedLayerIndex)
                ? rule.IndexToken
                : null;
        }

        private static void ApplyLayerTokenOverride(string tokenValue, int layerIndex, string[] resolvedTokens)
        {
            if (resolvedTokens.Length <= layerIndex)
                return;

            if (string.IsNullOrWhiteSpace(tokenValue))
                return;

            resolvedTokens[layerIndex] = tokenValue;
        }

        private static bool ResolveEnabled(AdvancedVariantOptions options, IReadOnlyList<int> layerIndices)
        {
            bool enabled = true;
            foreach (var orderedRule in GetOrderedMatchingRules(options, layerIndices))
            {
                if (orderedRule.Rule.Enabled.HasValue)
                    enabled = orderedRule.Rule.Enabled.Value;
            }

            return enabled;
        }

        private static IEnumerable<(AdvancedVariantRule Rule, int InputOrder)> GetOrderedMatchingRules(AdvancedVariantOptions options, IReadOnlyList<int> layerIndices)
        {
            return (options.Rules ?? Array.Empty<AdvancedVariantRule>())
                .Select((rule, index) => (Rule: rule, InputOrder: index))
                .Where(item => RuleMatches(item.Rule, layerIndices))
                .OrderBy(item => GetSpecificity(item.Rule))
                .ThenBy(item => item.Rule.SourceOrder)
                .ThenBy(item => item.InputOrder);
        }

        private static bool RuleMatches(AdvancedVariantRule rule, IReadOnlyList<int> layerIndices)
        {
            return RuleLayerMatches(rule.Layer1Index, 0, layerIndices)
                && RuleLayerMatches(rule.Layer2Index, 1, layerIndices)
                && RuleLayerMatches(rule.Layer3Index, 2, layerIndices)
                && RuleLayerMatches(rule.Layer4Index, 3, layerIndices);
        }

        private static bool RuleLayerMatches(int? ruleIndex, int layerIndex, IReadOnlyList<int> layerIndices)
        {
            if (!ruleIndex.HasValue)
                return true;

            if (layerIndex >= layerIndices.Count)
                return false;

            return layerIndices[layerIndex] == ruleIndex.Value;
        }

        private static int GetSpecificity(AdvancedVariantRule rule)
        {
            int specificity = 0;
            if (rule.Layer1Index.HasValue)
                specificity++;
            if (rule.Layer2Index.HasValue)
                specificity++;
            if (rule.Layer3Index.HasValue)
                specificity++;
            if (rule.Layer4Index.HasValue)
                specificity++;
            return specificity;
        }

        private static bool IsLayerSpecified(AdvancedVariantRule rule, int zeroBasedLayerIndex)
        {
            return zeroBasedLayerIndex switch
            {
                0 => rule.Layer1Index.HasValue,
                1 => rule.Layer2Index.HasValue,
                2 => rule.Layer3Index.HasValue,
                3 => rule.Layer4Index.HasValue,
                _ => false
            };
        }

        private static string BuildCombinedIndex(IReadOnlyList<int> layerIndices, bool padded)
        {
            return string.Concat(layerIndices.Select(index => padded
                ? index.ToString("00", CultureInfo.InvariantCulture)
                : index.ToString(CultureInfo.InvariantCulture)));
        }

        private static void ValidateRuleLayer(
            int? ruleIndex,
            int layerNumber,
            IReadOnlyList<AdvancedVariantLayerDefinition> layers,
            List<AdvancedVariantValidationIssue> issues)
        {
            if (!ruleIndex.HasValue)
                return;

            if (layerNumber > layers.Count)
            {
                issues.Add(new AdvancedVariantValidationIssue($"A rule specifies layer {layerNumber}, but only {layers.Count} layer(s) are defined."));
                return;
            }

            var layer = layers[layerNumber - 1];
            if (layer?.Indices == null || !layer.Indices.Contains(ruleIndex.Value))
            {
                string layerName = layer?.Name ?? $"Layer {layerNumber}";
                issues.Add(new AdvancedVariantValidationIssue($"Rule index {ruleIndex.Value} does not exist in layer '{layerName}'."));
            }
        }
    }
}
