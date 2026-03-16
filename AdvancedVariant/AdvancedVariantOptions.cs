using System;
using System.Collections.Generic;

namespace Material_Editor.AdvancedVariant
{
    public sealed class AdvancedVariantOptions
    {
        public IReadOnlyList<AdvancedVariantLayerDefinition> Layers { get; init; } = Array.Empty<AdvancedVariantLayerDefinition>();
        public IReadOnlyList<AdvancedVariantRule> Rules { get; init; } = Array.Empty<AdvancedVariantRule>();
    }
}
