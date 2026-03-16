using Material_Editor.AdvancedVariant;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Material_Editor.Services
{
    internal static class MaterialVariationTokenExpander
    {
        private static readonly Regex LegacyIndexPlaceholderRegex = new(@"\{index(?:\:([^\}]+))?\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AdvancedTokenRegex = new(@"(?:\{|\()(?<name>indexNN|indexTok|index)(?<layer>Layer[1-4])?(?:\}|\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool ContainsLegacyIndexPlaceholder(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            return LegacyIndexPlaceholderRegex.IsMatch(pattern);
        }

        public static string ExpandLegacy(string pattern, int index)
        {
            return ExpandCore(pattern, index, null);
        }

        public static string ReplaceLegacyIndexPlaceholders(string pattern, string replacement)
        {
            if (string.IsNullOrEmpty(pattern))
                return pattern;

            return LegacyIndexPlaceholderRegex.Replace(pattern, replacement ?? string.Empty);
        }

        public static string Expand(string pattern, int? legacyIndex, AdvancedVariantResolvedContext context)
        {
            return ExpandCore(pattern, legacyIndex, context);
        }

        internal static string Expand(string pattern, int? legacyIndex, IterativeTargetContext context)
        {
            return ExpandCore(pattern, legacyIndex, context);
        }

        private static string ExpandCore(string pattern, int? legacyIndex, IIndexedTokenContext context)
        {
            if (string.IsNullOrEmpty(pattern))
                return pattern;

            string expanded = pattern;
            if (context != null)
            {
                expanded = AdvancedTokenRegex.Replace(expanded, match =>
                {
                    string tokenName = match.Groups["name"].Value;
                    int? layerNumber = GetLayerNumber(match.Groups["layer"].Value);
                    if (tokenName.Equals("index", StringComparison.OrdinalIgnoreCase))
                    {
                        return layerNumber.HasValue
                            ? context.GetLayerIndexText(layerNumber.Value - 1)
                            : context.Index;
                    }

                    if (tokenName.Equals("indexNN", StringComparison.OrdinalIgnoreCase))
                    {
                        return layerNumber.HasValue
                            ? context.GetLayerIndexNNText(layerNumber.Value - 1)
                            : context.IndexNN;
                    }

                    return layerNumber.HasValue
                        ? context.GetLayerIndexToken(layerNumber.Value - 1)
                        : context.IndexTok ?? string.Empty;
                });
            }

            if (legacyIndex.HasValue)
                expanded = ReplaceLegacyIndexPlaceholders(expanded, legacyIndex.Value);

            return expanded;
        }

        private static string ReplaceLegacyIndexPlaceholders(string pattern, int index)
        {
            return LegacyIndexPlaceholderRegex.Replace(pattern, match =>
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
        private static int? GetLayerNumber(string layerGroupValue)
        {
            if (string.IsNullOrWhiteSpace(layerGroupValue))
                return null;

            if (layerGroupValue.StartsWith("Layer", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(layerGroupValue.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture, out int layerNumber))
            {
                return layerNumber;
            }

            return null;
        }
    }
}
