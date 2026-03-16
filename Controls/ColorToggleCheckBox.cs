using Material_Editor.Theming;
using System.Drawing;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal sealed class ColorToggleCheckBox : CheckBox
    {
        private Color checkedColor = Color.Green;
        private Color uncheckedColor = Color.Red;

        public ColorToggleCheckBox()
        {
            AutoSize = true;
            UseVisualStyleBackColor = false;
            Margin = new Padding(0);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        public void ApplyTheme(ThemeDefinition theme, Color background)
        {
            checkedColor = theme?.Semantics.Success ?? Color.Green;
            uncheckedColor = theme?.Semantics.Error ?? Color.Red;
            BackColor = background;
            ForeColor = ThemeApplicator.GetLabelForeground(theme);
            Invalidate();
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size textSize = string.IsNullOrEmpty(Text)
                ? Size.Empty
                : TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine);

            int indicatorSize = GetIndicatorSize();
            int spacing = string.IsNullOrEmpty(Text) ? 0 : 6;
            int width = Padding.Horizontal + indicatorSize + spacing + textSize.Width;
            int height = Padding.Vertical + System.Math.Max(indicatorSize, textSize.Height);
            return new Size(System.Math.Max(width, indicatorSize + Padding.Horizontal), System.Math.Max(height, indicatorSize + Padding.Vertical));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);

            int indicatorSize = GetIndicatorSize();
            int y = System.Math.Max(0, (ClientSize.Height - indicatorSize) / 2);
            var indicatorBounds = new Rectangle(0, y, indicatorSize, indicatorSize);
            var renderBounds = new Rectangle(indicatorBounds.X, indicatorBounds.Y, indicatorBounds.Width, indicatorBounds.Height);
            ColorToggleRenderer.DrawCheckbox(e.Graphics, renderBounds, Checked, BuildTheme(), BackColor);

            if (!string.IsNullOrEmpty(Text))
            {
                var textBounds = new Rectangle(indicatorSize + 6, 0, System.Math.Max(0, ClientSize.Width - indicatorSize - 6), ClientSize.Height);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    textBounds,
                    ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            if (Focused && ShowFocusCues)
            {
                var focusBounds = ClientRectangle;
                focusBounds.Width = System.Math.Max(0, focusBounds.Width - 1);
                focusBounds.Height = System.Math.Max(0, focusBounds.Height - 1);
                ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds, ForeColor, BackColor);
            }
        }

        private int GetIndicatorSize()
        {
            return System.Math.Max(14, FontHeight - 1);
        }

        private ThemeDefinition BuildTheme()
        {
            return new ThemeDefinition(
                "toggle",
                "Toggle",
                new ThemePalette(BackColor, BackColor, BackColor, BackColor, ForeColor, Color.Empty, ForeColor, ForeColor, BackColor, ForeColor, ForeColor, ForeColor),
                new ThemeSemanticColors(checkedColor, Color.Empty, uncheckedColor, Color.Empty, Color.Empty, Color.Empty, Color.Empty, uncheckedColor, Color.Empty));
        }
    }
}
