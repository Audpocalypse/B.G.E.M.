using System.Drawing;
using System.Globalization;

namespace Material_Editor.Theming
{
    internal static class ThemeColorSerialization
    {
        public static bool TryParse(string rawValue, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            try
            {
                string normalized = rawValue.Trim();
                if (!normalized.StartsWith("#", System.StringComparison.Ordinal))
                    return false;

                normalized = normalized[1..];
                if (normalized.Length == 6)
                {
                    color = Color.FromArgb(
                        255,
                        ParseHexByte(normalized, 0),
                        ParseHexByte(normalized, 2),
                        ParseHexByte(normalized, 4));
                    return true;
                }

                if (normalized.Length == 8)
                {
                    color = Color.FromArgb(
                        ParseHexByte(normalized, 0),
                        ParseHexByte(normalized, 2),
                        ParseHexByte(normalized, 4),
                        ParseHexByte(normalized, 6));
                    return true;
                }

                return false;
            }
            catch
            {
                color = Color.Empty;
                return false;
            }
        }

        public static string Format(Color color, bool allowEmpty = false)
        {
            if (allowEmpty && color.IsEmpty)
                return "empty";

            if (color.A < 255)
                return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static int ParseHexByte(string value, int startIndex)
        {
            return byte.Parse(value.Substring(startIndex, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
    }
}
