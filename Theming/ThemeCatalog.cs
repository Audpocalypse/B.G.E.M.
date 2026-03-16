using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Material_Editor.Theming
{
    internal sealed class ThemeCatalogLoadResult
    {
        public ThemeCatalogLoadResult(IReadOnlyList<ThemeDefinition> themes, IReadOnlyList<string> warnings)
        {
            Themes = themes ?? Array.Empty<ThemeDefinition>();
            Warnings = warnings ?? Array.Empty<string>();
        }

        public IReadOnlyList<ThemeDefinition> Themes { get; }
        public IReadOnlyList<string> Warnings { get; }
    }

    internal static class ThemeCatalog
    {
        private const string ThemeDirectoryName = "Themes";
        private static readonly HashSet<string> ProtectedBuiltInThemeIds = new(StringComparer.OrdinalIgnoreCase)
        {
            ThemeIds.Default,
            ThemeIds.PipBoy3000
        };

        public static string GetThemeDirectoryPath()
        {
            return Path.Combine(AppContext.BaseDirectory, ThemeDirectoryName);
        }

        public static ThemeCatalogLoadResult Load()
        {
            return LoadFromDirectory(GetThemeDirectoryPath());
        }

        internal static bool IsProtectedBuiltInThemeId(string themeId)
        {
            string normalized = ThemeService.NormalizeThemeId(themeId);
            string comparable = string.IsNullOrWhiteSpace(normalized)
                ? themeId?.Trim() ?? string.Empty
                : normalized;
            return ProtectedBuiltInThemeIds.Contains(comparable);
        }

        public static string GetThemeFilePath(string themeId)
        {
            return Path.Combine(GetThemeDirectoryPath(), $"{themeId}.xml");
        }

        public static string SaveTheme(ThemeDefinition theme)
        {
            return SaveThemeToDirectory(theme, GetThemeDirectoryPath());
        }

        internal static ThemeCatalogLoadResult LoadFromDirectory(string directoryPath)
        {
            var themes = new List<ThemeDefinition>();
            var warnings = new List<string>();

            if (!Directory.Exists(directoryPath))
                return new ThemeCatalogLoadResult(themes, warnings);

            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.xml", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (!TryLoadTheme(filePath, out ThemeDefinition theme, out string error))
                {
                    warnings.Add($"Skipping theme file '{filePath}': {error}");
                    continue;
                }

                if (themes.Any(existing => string.Equals(existing.Id, theme.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    warnings.Add($"Skipping theme file '{filePath}': duplicate theme id '{theme.Id}'.");
                    continue;
                }

                themes.Add(theme);
            }

            return new ThemeCatalogLoadResult(themes, warnings);
        }

        internal static string SaveThemeToDirectory(ThemeDefinition theme, string directoryPath)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path is required.", nameof(directoryPath));

            Directory.CreateDirectory(directoryPath);

            string filePath = Path.Combine(directoryPath, $"{theme.Id}.xml");
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    "theme",
                    new XAttribute("id", theme.Id),
                    new XAttribute("name", theme.DisplayName),
                    new XElement(
                        "palette",
                        new XAttribute("formBackground", ThemeColorSerialization.Format(theme.Palette.FormBackground)),
                        new XAttribute("controlBackground", ThemeColorSerialization.Format(theme.Palette.ControlBackground)),
                        new XAttribute("panelBackground", ThemeColorSerialization.Format(theme.Palette.PanelBackground)),
                        new XAttribute("menuBackground", ThemeColorSerialization.Format(theme.Palette.MenuBackground)),
                        new XAttribute("foreground", ThemeColorSerialization.Format(theme.Palette.Foreground)),
                        new XAttribute("accent", ThemeColorSerialization.Format(theme.Palette.Accent, allowEmpty: true))),
                    new XElement(
                        "semantic",
                        new XAttribute("success", ThemeColorSerialization.Format(theme.Semantics.Success)),
                        new XAttribute("warning", ThemeColorSerialization.Format(theme.Semantics.Warning)),
                        new XAttribute("error", ThemeColorSerialization.Format(theme.Semantics.Error)),
                        new XAttribute("dirty", ThemeColorSerialization.Format(theme.Semantics.Dirty)),
                        new XAttribute("readOnly", ThemeColorSerialization.Format(theme.Semantics.ReadOnly)),
                        new XAttribute("loadError", ThemeColorSerialization.Format(theme.Semantics.LoadError)),
                        new XAttribute("validation", ThemeColorSerialization.Format(theme.Semantics.Validation)),
                        new XAttribute("checkboxOff", ThemeColorSerialization.Format(theme.Semantics.CheckboxOff)),
                        new XAttribute("selectedToggle", ThemeColorSerialization.Format(theme.Semantics.SelectedToggle)))));
            document.Save(filePath);
            return filePath;
        }

        internal static ThemeDefinition CreateWindowsFallbackTheme()
        {
            return new ThemeDefinition(
                ThemeIds.WindowsDefault,
                "Windows Default",
                new ThemePalette(
                    SystemColors.Control,
                    SystemColors.Control,
                    Color.WhiteSmoke,
                    SystemColors.Control,
                    SystemColors.ControlText,
                    Color.Empty),
                new ThemeSemanticColors(
                    Color.Green,
                    Color.DarkOrange,
                    Color.Firebrick,
                    Color.ForestGreen,
                    SystemColors.ControlDark,
                    Color.Firebrick,
                    Color.Goldenrod,
                    Color.Red,
                    SystemColors.ControlDarkDark),
                ThemeSourceKind.Fallback);
        }

        private static bool TryLoadTheme(string filePath, out ThemeDefinition theme, out string error)
        {
            theme = null;
            error = string.Empty;

            try
            {
                var document = XDocument.Load(filePath, LoadOptions.None);
                XElement root = document.Root;
                if (root == null || !string.Equals(root.Name.LocalName, "theme", StringComparison.OrdinalIgnoreCase))
                {
                    error = "missing <theme> root element.";
                    return false;
                }

                string id = GetRequiredAttribute(root, "id");
                string displayName = GetRequiredAttribute(root, "name");
                XElement paletteElement = GetRequiredChild(root, "palette");
                XElement semanticElement = GetRequiredChild(root, "semantic");

                theme = new ThemeDefinition(
                    id,
                    displayName,
                    new ThemePalette(
                        ParseColor(paletteElement, "formBackground"),
                        ParseColor(paletteElement, "controlBackground"),
                        ParseColor(paletteElement, "panelBackground"),
                        ParseColor(paletteElement, "menuBackground"),
                        ParseColor(paletteElement, "foreground"),
                        ParseColor(paletteElement, "accent", allowEmpty: true)),
                    new ThemeSemanticColors(
                        ParseColor(semanticElement, "success"),
                        ParseColor(semanticElement, "warning"),
                        ParseColor(semanticElement, "error"),
                        ParseColor(semanticElement, "dirty"),
                        ParseColor(semanticElement, "readOnly"),
                        ParseColor(semanticElement, "loadError"),
                        ParseColor(semanticElement, "validation"),
                        ParseColor(semanticElement, "checkboxOff"),
                        ParseColor(semanticElement, "selectedToggle")),
                    IsProtectedBuiltInThemeId(id) ? ThemeSourceKind.BuiltIn : ThemeSourceKind.Custom,
                    filePath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string GetRequiredAttribute(XElement element, string attributeName)
        {
            var attribute = element.Attribute(attributeName);
            string value = attribute?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"missing '{attributeName}' attribute on <{element.Name.LocalName}>.");

            return value;
        }

        private static XElement GetRequiredChild(XElement parent, string childName)
        {
            XElement child = parent.Elements().FirstOrDefault(element => string.Equals(element.Name.LocalName, childName, StringComparison.OrdinalIgnoreCase));
            if (child == null)
                throw new InvalidDataException($"missing <{childName}> element.");

            return child;
        }

        private static Color ParseColor(XElement element, string attributeName, bool allowEmpty = false)
        {
            string rawValue = GetRequiredAttribute(element, attributeName);
            if (allowEmpty && string.Equals(rawValue, "empty", StringComparison.OrdinalIgnoreCase))
                return Color.Empty;

            if (!ThemeColorSerialization.TryParse(rawValue, out Color color))
                throw new InvalidDataException($"attribute '{attributeName}' must use #RRGGBB or #AARRGGBB format.");

            return color;
        }
    }
}
