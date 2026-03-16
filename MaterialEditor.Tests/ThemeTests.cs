using Material_Editor.Theming;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MaterialEditor.Tests
{
    internal static class ThemeTests
    {
        public static void RunAll()
        {
            ThemeCatalog_LoadsValidXmlTheme();
            ThemeCatalog_SkipsInvalidAndDuplicateThemes();
            ThemeCatalog_SavesThemeAndRoundTripsAccentEmpty();
            ThemeService_UsesFallbackThemeWhenDirectoryHasNoThemes();
            ThemeService_NormalizesLegacyThemeIds();
            ThemeService_FallsBackWhenSavedThemeIsMissing();
            ThemeService_ReloadThemesForTestingMakesSavedThemeAvailable();
            ThemeEditorValidator_RejectsInvalidThemeDefinitions();
        }

        private static void ThemeCatalog_LoadsValidXmlTheme()
        {
            string directory = CreateTempDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "custom.xml"),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<theme id=""custom"" name=""Custom Theme"">
  <palette formBackground=""#112233"" controlBackground=""#223344"" panelBackground=""#334455"" menuBackground=""#445566"" foreground=""#556677"" accent=""#667788"" />
  <semantic success=""#102030"" warning=""#203040"" error=""#304050"" dirty=""#405060"" readOnly=""#506070"" loadError=""#607080"" validation=""#708090"" checkboxOff=""#8090A0"" selectedToggle=""#90A0B0"" />
</theme>");

                ThemeCatalogLoadResult result = ThemeCatalog.LoadFromDirectory(directory);

                AssertEqual(1, result.Themes.Count, nameof(ThemeCatalog_LoadsValidXmlTheme));
                AssertEqual("custom", result.Themes[0].Id, nameof(ThemeCatalog_LoadsValidXmlTheme));
                AssertEqual("Custom Theme", result.Themes[0].DisplayName, nameof(ThemeCatalog_LoadsValidXmlTheme));
                AssertEqual("#112233", ColorToHex(result.Themes[0].Palette.FormBackground), nameof(ThemeCatalog_LoadsValidXmlTheme));
                AssertEqual("#304050", ColorToHex(result.Themes[0].Semantics.Error), nameof(ThemeCatalog_LoadsValidXmlTheme));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void ThemeCatalog_SkipsInvalidAndDuplicateThemes()
        {
            string directory = CreateTempDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "valid.xml"),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<theme id=""shared"" name=""Shared"">
  <palette formBackground=""#111111"" controlBackground=""#222222"" panelBackground=""#333333"" menuBackground=""#444444"" foreground=""#555555"" accent=""empty"" />
  <semantic success=""#666666"" warning=""#777777"" error=""#888888"" dirty=""#999999"" readOnly=""#AAAAAA"" loadError=""#BBBBBB"" validation=""#CCCCCC"" checkboxOff=""#DDDDDD"" selectedToggle=""#EEEEEE"" />
</theme>");

                File.WriteAllText(Path.Combine(directory, "duplicate.xml"),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<theme id=""shared"" name=""Duplicate"">
  <palette formBackground=""#111111"" controlBackground=""#222222"" panelBackground=""#333333"" menuBackground=""#444444"" foreground=""#555555"" accent=""#666666"" />
  <semantic success=""#666666"" warning=""#777777"" error=""#888888"" dirty=""#999999"" readOnly=""#AAAAAA"" loadError=""#BBBBBB"" validation=""#CCCCCC"" checkboxOff=""#DDDDDD"" selectedToggle=""#EEEEEE"" />
</theme>");

                File.WriteAllText(Path.Combine(directory, "invalid.xml"),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<theme id=""broken"" name=""Broken"">
  <palette formBackground=""red"" controlBackground=""#222222"" panelBackground=""#333333"" menuBackground=""#444444"" foreground=""#555555"" accent=""#666666"" />
  <semantic success=""#666666"" warning=""#777777"" error=""#888888"" dirty=""#999999"" readOnly=""#AAAAAA"" loadError=""#BBBBBB"" validation=""#CCCCCC"" checkboxOff=""#DDDDDD"" selectedToggle=""#EEEEEE"" />
</theme>");

                ThemeCatalogLoadResult result = ThemeCatalog.LoadFromDirectory(directory);

                AssertEqual(1, result.Themes.Count, nameof(ThemeCatalog_SkipsInvalidAndDuplicateThemes));
                AssertTrue(result.Warnings.Count >= 2, nameof(ThemeCatalog_SkipsInvalidAndDuplicateThemes));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void ThemeService_UsesFallbackThemeWhenDirectoryHasNoThemes()
        {
            string directory = CreateTempDirectory();
            try
            {
                ThemeInitializationResult result = ThemeService.InitializeForTesting(string.Empty, directory);

                AssertEqual(ThemeIds.WindowsDefault, result.ActiveTheme.Id, nameof(ThemeService_UsesFallbackThemeWhenDirectoryHasNoThemes));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void ThemeService_NormalizesLegacyThemeIds()
        {
            AssertEqual(ThemeIds.Default, ThemeService.NormalizeThemeId("Default"), nameof(ThemeService_NormalizesLegacyThemeIds));
            AssertEqual(ThemeIds.PipBoy3000, ThemeService.NormalizeThemeId("PipBoy3000"), nameof(ThemeService_NormalizesLegacyThemeIds));
            AssertEqual(ThemeIds.PipBoy3000, ThemeService.NormalizeThemeId("Pip-boy3000"), nameof(ThemeService_NormalizesLegacyThemeIds));
        }

        private static void ThemeService_FallsBackWhenSavedThemeIsMissing()
        {
            string directory = CreateTempDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "default.xml"),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<theme id=""default"" name=""Default"">
  <palette formBackground=""#111111"" controlBackground=""#222222"" panelBackground=""#333333"" menuBackground=""#444444"" foreground=""#555555"" accent=""empty"" />
  <semantic success=""#666666"" warning=""#777777"" error=""#888888"" dirty=""#999999"" readOnly=""#AAAAAA"" loadError=""#BBBBBB"" validation=""#CCCCCC"" checkboxOff=""#DDDDDD"" selectedToggle=""#EEEEEE"" />
</theme>");

                ThemeInitializationResult result = ThemeService.InitializeForTesting("missing-theme", directory);

                AssertEqual(ThemeIds.Default, result.ActiveTheme.Id, nameof(ThemeService_FallsBackWhenSavedThemeIsMissing));
                AssertTrue(!string.IsNullOrWhiteSpace(result.StartupWarning), nameof(ThemeService_FallsBackWhenSavedThemeIsMissing));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void ThemeCatalog_SavesThemeAndRoundTripsAccentEmpty()
        {
            string directory = CreateTempDirectory();
            try
            {
                var theme = new ThemeDefinition(
                    "roundtrip",
                    "Round Trip",
                    new ThemePalette(
                        Color.FromArgb(0x11, 0x22, 0x33),
                        Color.FromArgb(0x22, 0x33, 0x44),
                        Color.FromArgb(0x33, 0x44, 0x55),
                        Color.FromArgb(0x44, 0x55, 0x66),
                        Color.FromArgb(0xEE, 0xEE, 0xEE),
                        Color.Empty),
                    new ThemeSemanticColors(
                        Color.FromArgb(0x10, 0x20, 0x30),
                        Color.FromArgb(0x20, 0x30, 0x40),
                        Color.FromArgb(0x30, 0x40, 0x50),
                        Color.FromArgb(0x40, 0x50, 0x60),
                        Color.FromArgb(0x50, 0x60, 0x70),
                        Color.FromArgb(0x60, 0x70, 0x80),
                        Color.FromArgb(0x70, 0x80, 0x90),
                        Color.FromArgb(0x80, 0x90, 0xA0),
                        Color.FromArgb(0x90, 0xA0, 0xB0)));

                string filePath = ThemeCatalog.SaveThemeToDirectory(theme, directory);
                string xml = File.ReadAllText(filePath);
                ThemeCatalogLoadResult result = ThemeCatalog.LoadFromDirectory(directory);

                AssertTrue(xml.Contains("accent=\"empty\"", StringComparison.OrdinalIgnoreCase), nameof(ThemeCatalog_SavesThemeAndRoundTripsAccentEmpty));
                AssertEqual(1, result.Themes.Count, nameof(ThemeCatalog_SavesThemeAndRoundTripsAccentEmpty));
                AssertEqual("roundtrip", result.Themes[0].Id, nameof(ThemeCatalog_SavesThemeAndRoundTripsAccentEmpty));
                AssertEqual(true, result.Themes[0].Palette.Accent.IsEmpty, nameof(ThemeCatalog_SavesThemeAndRoundTripsAccentEmpty));
                AssertEqual("#304050", ColorToHex(result.Themes[0].Semantics.Error), nameof(ThemeCatalog_SavesThemeAndRoundTripsAccentEmpty));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void ThemeService_ReloadThemesForTestingMakesSavedThemeAvailable()
        {
            string directory = CreateTempDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "default.xml"),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<theme id=""default"" name=""Default"">
  <palette formBackground=""#111111"" controlBackground=""#222222"" panelBackground=""#333333"" menuBackground=""#444444"" foreground=""#555555"" accent=""empty"" />
  <semantic success=""#666666"" warning=""#777777"" error=""#888888"" dirty=""#999999"" readOnly=""#AAAAAA"" loadError=""#BBBBBB"" validation=""#CCCCCC"" checkboxOff=""#DDDDDD"" selectedToggle=""#EEEEEE"" />
</theme>");

                ThemeService.InitializeForTesting("default", directory);

                ThemeCatalog.SaveThemeToDirectory(
                    new ThemeDefinition(
                        "custom",
                        "Custom Theme",
                        new ThemePalette(Color.Black, Color.Black, Color.Black, Color.Black, Color.White, Color.Empty),
                        new ThemeSemanticColors(Color.Green, Color.Yellow, Color.Red, Color.Blue, Color.Gray, Color.Maroon, Color.Orange, Color.Purple, Color.Silver)),
                    directory);

                ThemeService.ReloadThemesForTesting(directory);

                AssertEqual(true, ThemeService.AvailableThemes.Any(theme => string.Equals(theme.Id, "custom", StringComparison.OrdinalIgnoreCase)), nameof(ThemeService_ReloadThemesForTestingMakesSavedThemeAvailable));
                AssertEqual("default", ThemeService.CurrentTheme.Id, nameof(ThemeService_ReloadThemesForTestingMakesSavedThemeAvailable));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void ThemeEditorValidator_RejectsInvalidThemeDefinitions()
        {
            var availableThemes = new[]
            {
                new ThemeDefinition("default", "Default", new ThemePalette(Color.Black, Color.Black, Color.Black, Color.Black, Color.White, Color.Empty), new ThemeSemanticColors(Color.Green, Color.Yellow, Color.Red, Color.Blue, Color.Gray, Color.Maroon, Color.Orange, Color.Purple, Color.Silver)),
                new ThemeDefinition("custom", "Custom", new ThemePalette(Color.Black, Color.Black, Color.Black, Color.Black, Color.White, Color.Empty), new ThemeSemanticColors(Color.Green, Color.Yellow, Color.Red, Color.Blue, Color.Gray, Color.Maroon, Color.Orange, Color.Purple, Color.Silver))
            };

            var blankId = new EditableThemeDefinition { DisplayName = "Blank" };
            var duplicateId = new EditableThemeDefinition { ThemeId = "custom", DisplayName = "Duplicate" };
            var protectedId = new EditableThemeDefinition { ThemeId = "default", DisplayName = "Default Copy" };
            var overwriteCustom = new EditableThemeDefinition { ThemeId = "custom", DisplayName = "Updated Custom" };

            AssertEqual(false, ThemeEditorValidator.ValidateForSave(blankId, availableThemes, string.Empty, false).IsValid, nameof(ThemeEditorValidator_RejectsInvalidThemeDefinitions));
            AssertEqual(false, ThemeEditorValidator.ValidateForSave(duplicateId, availableThemes, string.Empty, false).IsValid, nameof(ThemeEditorValidator_RejectsInvalidThemeDefinitions));
            AssertEqual(false, ThemeEditorValidator.ValidateForSave(protectedId, availableThemes, "default", true).IsValid, nameof(ThemeEditorValidator_RejectsInvalidThemeDefinitions));
            AssertEqual(true, ThemeEditorValidator.ValidateForSave(overwriteCustom, availableThemes, "custom", false).IsValid, nameof(ThemeEditorValidator_RejectsInvalidThemeDefinitions));
        }

        private static string ColorToHex(System.Drawing.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static string CreateTempDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), "material-editor-theme-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void DeleteDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            Directory.Delete(directory, recursive: true);
        }

        private static void AssertTrue(bool condition, string testName)
        {
            if (!condition)
                throw new InvalidOperationException($"{testName} failed.");
        }

        private static void AssertEqual<T>(T expected, T actual, string testName)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException($"{testName} failed. Expected '{expected}', got '{actual}'.");
        }
    }
}
