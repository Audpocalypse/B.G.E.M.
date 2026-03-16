using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Material_Editor.Theming
{
    internal sealed class ThemeChangedEventArgs : EventArgs
    {
        public ThemeChangedEventArgs(ThemeDefinition theme)
        {
            Theme = theme;
        }

        public ThemeDefinition Theme { get; }
    }

    internal sealed class ThemeInitializationResult
    {
        public ThemeInitializationResult(ThemeDefinition activeTheme, string startupWarning)
        {
            ActiveTheme = activeTheme;
            StartupWarning = startupWarning ?? string.Empty;
        }

        public ThemeDefinition ActiveTheme { get; }
        public string StartupWarning { get; }
    }

    internal static class ThemeService
    {
        private static IReadOnlyList<ThemeDefinition> availableThemes = Array.Empty<ThemeDefinition>();
        private static ThemeDefinition currentTheme;
        private static bool isInitialized;

        public static event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        public static bool IsInitialized => isInitialized;

        public static IReadOnlyList<ThemeDefinition> AvailableThemes
        {
            get
            {
                EnsureInitialized();
                return availableThemes;
            }
        }

        public static ThemeDefinition CurrentTheme
        {
            get
            {
                EnsureInitialized();
                return currentTheme;
            }
        }

        public static string NormalizeThemeId(string rawThemeId)
        {
            if (string.IsNullOrWhiteSpace(rawThemeId))
                return string.Empty;

            string trimmed = rawThemeId.Trim();
            if (string.Equals(trimmed, "Default", StringComparison.OrdinalIgnoreCase))
                return ThemeIds.Default;
            if (string.Equals(trimmed, "PipBoy3000", StringComparison.OrdinalIgnoreCase))
                return ThemeIds.PipBoy3000;
            if (string.Equals(trimmed, "Pip-boy3000", StringComparison.OrdinalIgnoreCase))
                return ThemeIds.PipBoy3000;

            return trimmed;
        }

        public static ThemeInitializationResult Initialize(string requestedThemeId)
        {
            return InitializeCore(requestedThemeId, ThemeCatalog.Load());
        }

        internal static ThemeInitializationResult InitializeForTesting(string requestedThemeId, string directoryPath)
        {
            return InitializeCore(requestedThemeId, ThemeCatalog.LoadFromDirectory(directoryPath));
        }

        public static void EnsureInitialized()
        {
            if (!isInitialized)
                Initialize(string.Empty);
        }

        public static bool SetCurrentTheme(string themeId)
        {
            EnsureInitialized();

            string normalizedId = NormalizeThemeId(themeId);
            ThemeDefinition nextTheme = availableThemes.FirstOrDefault(theme => string.Equals(theme.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
            if (nextTheme == null)
                return false;

            if (ReferenceEquals(currentTheme, nextTheme))
                return true;

            currentTheme = nextTheme;
            ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(currentTheme));
            return true;
        }

        public static bool ReloadThemes()
        {
            return ReloadThemesCore(ThemeCatalog.Load());
        }

        internal static bool ReloadThemesForTesting(string directoryPath)
        {
            return ReloadThemesCore(ThemeCatalog.LoadFromDirectory(directoryPath));
        }

        private static bool ReloadThemesCore(ThemeCatalogLoadResult catalog)
        {
            EnsureInitialized();

            foreach (string warning in catalog.Warnings)
                Trace.TraceWarning(warning);

            var themes = catalog.Themes?.ToList() ?? new List<ThemeDefinition>();
            if (themes.Count == 0)
                themes.Add(ThemeCatalog.CreateWindowsFallbackTheme());

            availableThemes = themes;

            string currentThemeId = currentTheme?.Id ?? string.Empty;
            ThemeDefinition reloadedCurrentTheme = themes.FirstOrDefault(theme => string.Equals(theme.Id, currentThemeId, StringComparison.OrdinalIgnoreCase));
            if (reloadedCurrentTheme != null)
                return false;

            ThemeDefinition fallbackTheme = ResolveTheme(themes, currentThemeId);
            currentTheme = fallbackTheme;
            ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(currentTheme));
            return true;
        }

        private static ThemeInitializationResult InitializeCore(string requestedThemeId, ThemeCatalogLoadResult catalog)
        {
            foreach (string warning in catalog.Warnings)
                Trace.TraceWarning(warning);

            var themes = catalog.Themes?.ToList() ?? new List<ThemeDefinition>();
            if (themes.Count == 0)
                themes.Add(ThemeCatalog.CreateWindowsFallbackTheme());

            string normalizedRequestedThemeId = NormalizeThemeId(requestedThemeId);
            ThemeDefinition resolvedTheme = ResolveTheme(themes, normalizedRequestedThemeId);
            string startupWarning = string.Empty;
            if (!string.IsNullOrEmpty(normalizedRequestedThemeId) &&
                !string.Equals(normalizedRequestedThemeId, resolvedTheme.Id, StringComparison.OrdinalIgnoreCase))
            {
                startupWarning = $"Saved theme '{requestedThemeId}' is no longer available. Loaded '{resolvedTheme.DisplayName}' instead.";
            }

            availableThemes = themes;
            currentTheme = resolvedTheme;
            isInitialized = true;

            return new ThemeInitializationResult(currentTheme, startupWarning);
        }

        private static ThemeDefinition ResolveTheme(IReadOnlyList<ThemeDefinition> themes, string requestedThemeId)
        {
            if (!string.IsNullOrEmpty(requestedThemeId))
            {
                ThemeDefinition matchedTheme = themes.FirstOrDefault(theme => string.Equals(theme.Id, requestedThemeId, StringComparison.OrdinalIgnoreCase));
                if (matchedTheme != null)
                    return matchedTheme;
            }

            ThemeDefinition defaultTheme = themes.FirstOrDefault(theme => string.Equals(theme.Id, ThemeIds.Default, StringComparison.OrdinalIgnoreCase));
            return defaultTheme ?? themes[0];
        }
    }
}
