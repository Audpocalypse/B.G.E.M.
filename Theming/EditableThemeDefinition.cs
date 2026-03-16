using System.Drawing;

namespace Material_Editor.Theming
{
    internal sealed class EditableThemeDefinition
    {
        public string ThemeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public EditableThemePalette Palette { get; } = new();
        public EditableThemeSemanticColors Semantics { get; } = new();

        public static EditableThemeDefinition FromTheme(ThemeDefinition theme)
        {
            return new EditableThemeDefinition
            {
                ThemeId = theme?.Id ?? string.Empty,
                DisplayName = theme?.DisplayName ?? string.Empty,
                Palette =
                {
                    FormBackground = theme?.Palette.FormBackground ?? Color.Empty,
                    ControlBackground = theme?.Palette.ControlBackground ?? Color.Empty,
                    PanelBackground = theme?.Palette.PanelBackground ?? Color.Empty,
                    MenuBackground = theme?.Palette.MenuBackground ?? Color.Empty,
                    Foreground = theme?.Palette.Foreground ?? Color.Empty,
                    Accent = theme?.Palette.Accent ?? Color.Empty,
                    BorderColor = theme?.Palette.BorderColor ?? Color.Empty,
                    TableBorderColor = theme?.Palette.TableBorderColor ?? Color.Empty,
                    AlternatingRowBackground = theme?.Palette.AlternatingRowBackground ?? Color.Empty,
                    MenuForeground = theme?.Palette.MenuForeground ?? Color.Empty,
                    LabelForeground = theme?.Palette.LabelForeground ?? Color.Empty,
                    EditableForeground = theme?.Palette.EditableForeground ?? Color.Empty
                },
                Semantics =
                {
                    Success = theme?.Semantics.Success ?? Color.Empty,
                    Warning = theme?.Semantics.Warning ?? Color.Empty,
                    Error = theme?.Semantics.Error ?? Color.Empty,
                    Dirty = theme?.Semantics.Dirty ?? Color.Empty,
                    ReadOnly = theme?.Semantics.ReadOnly ?? Color.Empty,
                    LoadError = theme?.Semantics.LoadError ?? Color.Empty,
                    Validation = theme?.Semantics.Validation ?? Color.Empty,
                    CheckboxOff = theme?.Semantics.CheckboxOff ?? Color.Empty,
                    SelectedToggle = theme?.Semantics.SelectedToggle ?? Color.Empty
                }
            };
        }

        public ThemeDefinition ToThemeDefinition()
        {
            return new ThemeDefinition(
                ThemeId?.Trim() ?? string.Empty,
                DisplayName?.Trim() ?? string.Empty,
                new ThemePalette(
                    Palette.FormBackground,
                    Palette.ControlBackground,
                    Palette.PanelBackground,
                    Palette.MenuBackground,
                    Palette.Foreground,
                    Palette.Accent,
                    Palette.BorderColor,
                    Palette.TableBorderColor,
                    Palette.AlternatingRowBackground,
                    Palette.MenuForeground,
                    Palette.LabelForeground,
                    Palette.EditableForeground),
                new ThemeSemanticColors(
                    Semantics.Success,
                    Semantics.Warning,
                    Semantics.Error,
                    Semantics.Dirty,
                    Semantics.ReadOnly,
                    Semantics.LoadError,
                    Semantics.Validation,
                    Semantics.CheckboxOff,
                    Semantics.SelectedToggle));
        }
    }

    internal sealed class EditableThemePalette
    {
        public Color FormBackground { get; set; }
        public Color ControlBackground { get; set; }
        public Color PanelBackground { get; set; }
        public Color MenuBackground { get; set; }
        public Color Foreground { get; set; }
        public Color Accent { get; set; }
        public Color BorderColor { get; set; }
        public Color TableBorderColor { get; set; }
        public Color AlternatingRowBackground { get; set; }
        public Color MenuForeground { get; set; }
        public Color LabelForeground { get; set; }
        public Color EditableForeground { get; set; }
    }

    internal sealed class EditableThemeSemanticColors
    {
        public Color Success { get; set; }
        public Color Warning { get; set; }
        public Color Error { get; set; }
        public Color Dirty { get; set; }
        public Color ReadOnly { get; set; }
        public Color LoadError { get; set; }
        public Color Validation { get; set; }
        public Color CheckboxOff { get; set; }
        public Color SelectedToggle { get; set; }
    }
}
