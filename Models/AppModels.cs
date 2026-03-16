using System.Collections.Generic;
using System.Drawing;

namespace Material_Editor.Models
{
    public sealed class Config
    {
        public Game GameVersion;
        public Font Font;
        public string ThemeId;
        public BulkDirtyRemoveBehavior BulkDirtyRemoveBehavior;
        public List<BulkFieldPreset> BulkFieldPresets = new();
    }

    public sealed class BulkFieldPreset
    {
        public string Name { get; set; } = string.Empty;
        public MaterialType MaterialType { get; set; }
        public List<string> FieldLabels { get; set; } = new();
    }

    public enum BulkDirtyRemoveBehavior
    {
        Ask,
        Save,
        Discard
    }

    public enum Game
    {
        FO4,
        FO76
    }

    public enum MaterialType
    {
        Material,
        Effect
    }
}
