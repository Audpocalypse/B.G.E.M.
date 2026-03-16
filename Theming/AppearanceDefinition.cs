using System.Drawing;

namespace Material_Editor.Theming
{
    internal sealed class AppearanceDefinition
    {
        public AppearanceDefinition(ThemeDefinition theme, Font font)
        {
            Theme = theme ?? throw new System.ArgumentNullException(nameof(theme));
            Font = CloneFont(font ?? SystemFonts.MessageBoxFont);
            BoldFont = new Font(Font, FontStyle.Bold);
            MonospaceFont = CreateMonospaceFont(Font);
        }

        public ThemeDefinition Theme { get; }
        public Font Font { get; }
        public Font BoldFont { get; }
        public Font MonospaceFont { get; }

        private static Font CloneFont(Font font)
        {
            return new Font(font.FontFamily, font.Size, font.Style, GraphicsUnit.Point);
        }

        private static Font CreateMonospaceFont(Font baseFont)
        {
            FontFamily family;
            try
            {
                family = new FontFamily("Consolas");
            }
            catch
            {
                family = FontFamily.GenericMonospace;
            }

            return new Font(family, baseFont.Size, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    internal sealed class AppearanceChangedEventArgs : System.EventArgs
    {
        public AppearanceChangedEventArgs(AppearanceDefinition appearance)
        {
            Appearance = appearance;
        }

        public AppearanceDefinition Appearance { get; }
    }

    internal sealed class AppearanceInitializationResult
    {
        public AppearanceInitializationResult(AppearanceDefinition activeAppearance, string startupWarning)
        {
            ActiveAppearance = activeAppearance;
            StartupWarning = startupWarning ?? string.Empty;
        }

        public AppearanceDefinition ActiveAppearance { get; }
        public string StartupWarning { get; }
    }
}
