using System.Drawing;

namespace Material_Editor.Theming
{
    public readonly struct ThemePalette
    {
        public ThemePalette(
            Color formBackground,
            Color controlBackground,
            Color panelBackground,
            Color menuBackground,
            Color foreground,
            Color accent,
            Color borderColor,
            Color tableBorderColor,
            Color alternatingRowBackground,
            Color menuForeground,
            Color labelForeground,
            Color editableForeground)
        {
            FormBackground = formBackground;
            ControlBackground = controlBackground;
            PanelBackground = panelBackground;
            MenuBackground = menuBackground;
            Foreground = foreground;
            Accent = accent;
            BorderColor = borderColor;
            TableBorderColor = tableBorderColor;
            AlternatingRowBackground = alternatingRowBackground;
            MenuForeground = menuForeground;
            LabelForeground = labelForeground;
            EditableForeground = editableForeground;
        }

        public Color FormBackground { get; }
        public Color ControlBackground { get; }
        public Color PanelBackground { get; }
        public Color MenuBackground { get; }
        public Color Foreground { get; }
        public Color Accent { get; }
        public Color BorderColor { get; }
        public Color TableBorderColor { get; }
        public Color AlternatingRowBackground { get; }
        public Color MenuForeground { get; }
        public Color LabelForeground { get; }
        public Color EditableForeground { get; }
    }

    public readonly struct ThemeSemanticColors
    {
        public ThemeSemanticColors(Color success, Color warning, Color error, Color dirty, Color readOnly, Color loadError, Color validation, Color checkboxOff, Color selectedToggle)
        {
            Success = success;
            Warning = warning;
            Error = error;
            Dirty = dirty;
            ReadOnly = readOnly;
            LoadError = loadError;
            Validation = validation;
            CheckboxOff = checkboxOff;
            SelectedToggle = selectedToggle;
        }

        public Color Success { get; }
        public Color Warning { get; }
        public Color Error { get; }
        public Color Dirty { get; }
        public Color ReadOnly { get; }
        public Color LoadError { get; }
        public Color Validation { get; }
        public Color CheckboxOff { get; }
        public Color SelectedToggle { get; }
    }

    public sealed class ThemeDefinition
    {
        public ThemeDefinition(string id, string displayName, ThemePalette palette, ThemeSemanticColors semantics, ThemeSourceKind sourceKind = ThemeSourceKind.Custom, string sourcePath = "")
        {
            Id = id;
            DisplayName = displayName;
            Palette = palette;
            Semantics = semantics;
            SourceKind = sourceKind;
            SourcePath = sourcePath ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public ThemePalette Palette { get; }
        public ThemeSemanticColors Semantics { get; }
        public ThemeSourceKind SourceKind { get; }
        public string SourcePath { get; }
        public bool IsBuiltIn => SourceKind == ThemeSourceKind.BuiltIn;
        public bool IsFallback => SourceKind == ThemeSourceKind.Fallback;
    }

    public enum ThemeSourceKind
    {
        BuiltIn,
        Custom,
        Fallback
    }

    internal static class ThemeIds
    {
        public const string WindowsDefault = "windows-default";
        public const string Default = "default";
        public const string PipBoy3000 = "pipboy3000";
    }
}
