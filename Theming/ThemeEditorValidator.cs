using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Material_Editor.Theming
{
    internal sealed class ThemeEditorValidationResult
    {
        public ThemeEditorValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message ?? string.Empty;
        }

        public bool IsValid { get; }
        public string Message { get; }
    }

    internal static class ThemeEditorValidator
    {
        public static ThemeEditorValidationResult ValidateForSave(EditableThemeDefinition theme, IReadOnlyList<ThemeDefinition> availableThemes, string originalThemeId, bool originalThemeIsBuiltIn)
        {
            if (theme == null)
                return new ThemeEditorValidationResult(false, "Theme data is missing.");

            string themeId = theme.ThemeId?.Trim() ?? string.Empty;
            string displayName = theme.DisplayName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(themeId))
                return new ThemeEditorValidationResult(false, "Theme ID is required.");

            if (themeId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return new ThemeEditorValidationResult(false, "Theme ID contains invalid filename characters.");

            if (string.IsNullOrWhiteSpace(displayName))
                return new ThemeEditorValidationResult(false, "Display name is required.");

            if (ThemeCatalog.IsProtectedBuiltInThemeId(themeId) && originalThemeIsBuiltIn && IsSameThemeId(themeId, originalThemeId))
                return new ThemeEditorValidationResult(false, "Built-in themes cannot be overwritten. Change the Theme ID to save a copy.");

            ThemeDefinition duplicateTheme = availableThemes?.FirstOrDefault(existing => IsSameThemeId(existing.Id, themeId));
            if (duplicateTheme != null && !IsSameThemeId(themeId, originalThemeId))
                return new ThemeEditorValidationResult(false, $"A theme with ID '{themeId}' already exists.");

            return new ThemeEditorValidationResult(true, string.Empty);
        }

        private static bool IsSameThemeId(string left, string right)
        {
            string normalizedLeft = NormalizeComparableThemeId(left);
            string normalizedRight = NormalizeComparableThemeId(right);
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeComparableThemeId(string themeId)
        {
            string normalized = ThemeService.NormalizeThemeId(themeId);
            return string.IsNullOrWhiteSpace(normalized)
                ? themeId?.Trim() ?? string.Empty
                : normalized;
        }
    }
}
