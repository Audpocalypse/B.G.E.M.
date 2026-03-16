using Material_Editor.Theming;
using System.Drawing;

namespace Material_Editor.Controls
{
    internal static class ColorToggleRenderer
    {
        public static void DrawCheckbox(Graphics graphics, Rectangle bounds, bool isChecked, ThemeDefinition theme, Color background, bool clearBackground = true, int? indicatorSize = null)
        {
            if (graphics == null)
                return;

            Color checkedColor = theme?.Semantics.Success ?? Color.Green;
            Color uncheckedColor = theme?.Semantics.Error ?? Color.Red;
            Color indicatorColor = isChecked ? checkedColor : uncheckedColor;

            if (clearBackground)
                graphics.Clear(background);

            int resolvedIndicatorSize = indicatorSize
                ?? System.Math.Max(14, System.Math.Min(bounds.Width, bounds.Height) - 4);
            resolvedIndicatorSize = System.Math.Min(resolvedIndicatorSize, System.Math.Max(8, System.Math.Min(bounds.Width, bounds.Height) - 4));

            int x = bounds.X + System.Math.Max(0, (bounds.Width - resolvedIndicatorSize) / 2);
            int y = bounds.Y + System.Math.Max(0, (bounds.Height - resolvedIndicatorSize) / 2);
            var indicatorBounds = new Rectangle(x, y, resolvedIndicatorSize, resolvedIndicatorSize);

            using var fillBrush = new SolidBrush(indicatorColor);
            using var borderPen = new Pen(indicatorColor);
            graphics.FillRectangle(fillBrush, indicatorBounds);
            graphics.DrawRectangle(borderPen, indicatorBounds);

            if (!isChecked)
                return;

            float checkThickness = resolvedIndicatorSize >= 18 ? 2f : 1.75f;
            using var checkPen = new Pen(Color.White, checkThickness);
            int left = indicatorBounds.Left + 3;
            int midX = indicatorBounds.Left + indicatorBounds.Width / 2 - 1;
            int right = indicatorBounds.Right - 3;
            int top = indicatorBounds.Top + indicatorBounds.Height / 2;
            int bottom = indicatorBounds.Bottom - 4;
            graphics.DrawLines(checkPen, new[]
            {
                new Point(left, top),
                new Point(midX, bottom),
                new Point(right, indicatorBounds.Top + 3)
            });
        }
    }
}
