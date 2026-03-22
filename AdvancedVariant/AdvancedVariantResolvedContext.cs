using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Material_Editor.Services;

namespace Material_Editor.AdvancedVariant
{
    public sealed class AdvancedVariantResolvedContext : IIndexedTokenContext
    {
        internal AdvancedVariantResolvedContext(
            int ordinal,
            IReadOnlyList<int> layerIndices,
            IReadOnlyList<int> layerIndexPadWidths,
            string index,
            string indexNN,
            string indexTok,
            bool indexTokFallback,
            IReadOnlyList<string> layerIndexTokens,
            IReadOnlyList<bool> layerIndexTokenFallbacks,
            bool enabled)
        {
            Ordinal = ordinal;
            LayerIndices = layerIndices?.ToArray() ?? Array.Empty<int>();
            LayerIndexPadWidths = layerIndexPadWidths?.ToArray() ?? Array.Empty<int>();
            Index = index ?? string.Empty;
            IndexNN = indexNN ?? string.Empty;
            IndexTok = indexTok ?? string.Empty;
            IndexTokFallback = indexTokFallback;
            LayerIndexTokens = layerIndexTokens?.ToArray() ?? Array.Empty<string>();
            LayerIndexTokenFallbacks = layerIndexTokenFallbacks?.ToArray() ?? Array.Empty<bool>();
            Enabled = enabled;
        }

        public int Ordinal { get; }
        public IReadOnlyList<int> LayerIndices { get; }
        public IReadOnlyList<int> LayerIndexPadWidths { get; }
        public string Index { get; }
        public string IndexNN { get; }
        public string IndexTok { get; }
        public bool IndexTokFallback { get; }
        public IReadOnlyList<string> LayerIndexTokens { get; }
        public IReadOnlyList<bool> LayerIndexTokenFallbacks { get; }
        public bool Enabled { get; }

        public int? Layer1Index => GetLayerIndexOrNull(0);
        public int? Layer2Index => GetLayerIndexOrNull(1);
        public int? Layer3Index => GetLayerIndexOrNull(2);
        public int? Layer4Index => GetLayerIndexOrNull(3);

        public string Layer1IndexText => GetLayerIndexText(0);
        public string Layer2IndexText => GetLayerIndexText(1);
        public string Layer3IndexText => GetLayerIndexText(2);
        public string Layer4IndexText => GetLayerIndexText(3);

        public string Layer1IndexNNText => GetLayerIndexNNText(0);
        public string Layer2IndexNNText => GetLayerIndexNNText(1);
        public string Layer3IndexNNText => GetLayerIndexNNText(2);
        public string Layer4IndexNNText => GetLayerIndexNNText(3);

        public string Layer1IndexToken => GetLayerIndexToken(0);
        public string Layer2IndexToken => GetLayerIndexToken(1);
        public string Layer3IndexToken => GetLayerIndexToken(2);
        public string Layer4IndexToken => GetLayerIndexToken(3);

        public bool Layer1IndexTokenFallback => IsLayerIndexTokenFallback(0);
        public bool Layer2IndexTokenFallback => IsLayerIndexTokenFallback(1);
        public bool Layer3IndexTokenFallback => IsLayerIndexTokenFallback(2);
        public bool Layer4IndexTokenFallback => IsLayerIndexTokenFallback(3);

        private int? GetLayerIndexOrNull(int index)
        {
            return LayerIndices.Count > index ? LayerIndices[index] : null;
        }

        public string GetLayerIndexText(int zeroBasedLayerIndex)
        {
            int? value = GetLayerIndexOrNull(zeroBasedLayerIndex);
            return value.HasValue
                ? value.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }

        public string GetLayerIndexNNText(int zeroBasedLayerIndex)
        {
            int? value = GetLayerIndexOrNull(zeroBasedLayerIndex);
            return value.HasValue
                ? value.Value.ToString(new string('0', GetLayerPadWidth(zeroBasedLayerIndex)), CultureInfo.InvariantCulture)
                : string.Empty;
        }

        public string GetLayerIndexToken(int zeroBasedLayerIndex)
        {
            return LayerIndexTokens.Count > zeroBasedLayerIndex
                ? LayerIndexTokens[zeroBasedLayerIndex] ?? string.Empty
                : string.Empty;
        }

        public bool IsLayerIndexTokenFallback(int zeroBasedLayerIndex)
        {
            return LayerIndexTokenFallbacks.Count > zeroBasedLayerIndex
                && LayerIndexTokenFallbacks[zeroBasedLayerIndex];
        }

        private int GetLayerPadWidth(int zeroBasedLayerIndex)
        {
            if (LayerIndexPadWidths.Count <= zeroBasedLayerIndex)
                return 1;

            return Math.Max(1, LayerIndexPadWidths[zeroBasedLayerIndex]);
        }
    }
}
