using System;
using System.Drawing;

namespace Material_Editor.Theming
{
    internal static class AppearanceService
    {
        private static AppearanceDefinition currentAppearance;
        private static bool isInitialized;
        private static bool isSubscribedToThemeChanges;

        public static event EventHandler<AppearanceChangedEventArgs> AppearanceChanged;

        public static bool IsInitialized => isInitialized;

        public static AppearanceDefinition CurrentAppearance
        {
            get
            {
                EnsureInitialized();
                return currentAppearance;
            }
        }

        public static AppearanceInitializationResult Initialize(string requestedThemeId, Font font)
        {
            ThemeInitializationResult themeInitialization = ThemeService.Initialize(requestedThemeId);
            return InitializeCore(themeInitialization, font);
        }

        internal static AppearanceInitializationResult InitializeForTesting(string requestedThemeId, Font font, string directoryPath)
        {
            ThemeInitializationResult themeInitialization = ThemeService.InitializeForTesting(requestedThemeId, directoryPath);
            return InitializeCore(themeInitialization, font);
        }

        private static AppearanceInitializationResult InitializeCore(ThemeInitializationResult themeInitialization, Font font)
        {
            SubscribeToThemeChanges();
            currentAppearance = CreateAppearance(themeInitialization.ActiveTheme, font);
            isInitialized = true;
            return new AppearanceInitializationResult(currentAppearance, themeInitialization.StartupWarning);
        }

        public static void EnsureInitialized()
        {
            if (!isInitialized)
                Initialize(string.Empty, SystemFonts.MessageBoxFont);
        }

        public static bool SetCurrentTheme(string themeId)
        {
            EnsureInitialized();
            return ThemeService.SetCurrentTheme(themeId);
        }

        public static bool SetFont(Font font)
        {
            EnsureInitialized();

            Font nextFont = font ?? SystemFonts.MessageBoxFont;
            if (AreEquivalent(currentAppearance.Font, nextFont))
                return false;

            currentAppearance = CreateAppearance(currentAppearance.Theme, nextFont);
            AppearanceChanged?.Invoke(null, new AppearanceChangedEventArgs(currentAppearance));
            return true;
        }

        private static void SubscribeToThemeChanges()
        {
            if (isSubscribedToThemeChanges)
                return;

            ThemeService.ThemeChanged += ThemeService_ThemeChanged;
            isSubscribedToThemeChanges = true;
        }

        private static void ThemeService_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            Font currentFont = currentAppearance?.Font ?? SystemFonts.MessageBoxFont;
            currentAppearance = CreateAppearance(e.Theme, currentFont);
            isInitialized = true;
            AppearanceChanged?.Invoke(null, new AppearanceChangedEventArgs(currentAppearance));
        }

        private static AppearanceDefinition CreateAppearance(ThemeDefinition theme, Font font)
        {
            return new AppearanceDefinition(theme, font);
        }

        private static bool AreEquivalent(Font left, Font right)
        {
            if (left == null || right == null)
                return left == right;

            return string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
                && Math.Abs(left.SizeInPoints - right.SizeInPoints) < 0.01f
                && left.Style == right.Style;
        }
    }
}
