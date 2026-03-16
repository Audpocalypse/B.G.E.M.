using System;
using System.Collections.Generic;

namespace Material_Editor.AdvancedVariant
{
    public sealed class AdvancedVariantLayerDefinition
    {
        public AdvancedVariantLayerDefinition(string name, IReadOnlyList<int> indices)
        {
            Name = name ?? string.Empty;
            Indices = indices ?? Array.Empty<int>();
        }

        public string Name { get; }
        public IReadOnlyList<int> Indices { get; }
    }
}
