using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Material_Editor
{
    internal static class DialogThemeHelper
    {
        private sealed class ListViewThemeState
        {
            public ThemePalette Palette { get; set; }
            public bool HandlersAttached { get; set; }
        }

        private static readonly ConditionalWeakTable<ListView, ListViewThemeState> ListViewThemes = new();

        public static void Apply(Form form, ThemePalette palette, UITheme theme)
        {
            if (form == null)
                return;

            AppIconProvider.Apply(form);
            form.BackColor = palette.PanelBackground;
            form.ForeColor = palette.Foreground;
            ApplyToControl(form, palette, palette.PanelBackground);
        }

        private static void ApplyToControl(Control control, ThemePalette palette, Color background)
        {
            if (control == null)
                return;

            control.BackColor = background;
            control.ForeColor = palette.Foreground;

            switch (control)
            {
                case Button button:
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = palette.Accent.IsEmpty ? Color.Gray : palette.Accent;
                    button.FlatAppearance.MouseOverBackColor = palette.Accent.IsEmpty ? palette.MenuBackground : ControlPaint.Light(palette.Accent);
                    button.FlatAppearance.MouseDownBackColor = palette.Accent.IsEmpty ? palette.ControlBackground : palette.Accent;
                    button.UseVisualStyleBackColor = false;
                    button.BackColor = palette.ControlBackground;
                    button.ForeColor = palette.Foreground;
                    break;
                case RichTextBox richTextBox:
                    richTextBox.BorderStyle = BorderStyle.None;
                    richTextBox.BackColor = palette.PanelBackground;
                    richTextBox.ForeColor = palette.Foreground;
                    break;
                case TextBoxBase textBox:
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = palette.PanelBackground;
                    textBox.ForeColor = palette.Foreground;
                    break;
                case NumericUpDown numeric:
                    numeric.BackColor = palette.PanelBackground;
                    numeric.ForeColor = palette.Foreground;
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = background;
                    checkBox.ForeColor = palette.Foreground;
                    break;
                case GroupBox groupBox:
                    groupBox.BackColor = background;
                    groupBox.ForeColor = palette.Foreground;
                    break;
                case Label label:
                    label.BackColor = label.BorderStyle == BorderStyle.None ? background : palette.PanelBackground;
                    label.ForeColor = palette.Foreground;
                    break;
                case Panel panel:
                    panel.BackColor = background;
                    panel.ForeColor = palette.Foreground;
                    break;
                case ListView listView:
                    ApplyToListView(listView, palette);
                    break;
                case DataGridView grid:
                    ApplyToGrid(grid, palette);
                    break;
            }

            foreach (Control child in control.Controls)
            {
                var childBackground = child is GroupBox ? palette.ControlBackground : background;
                if (control is GroupBox)
                    childBackground = palette.ControlBackground;
                if (child is TextBoxBase || child is NumericUpDown || child is DataGridView)
                    childBackground = palette.PanelBackground;
                ApplyToControl(child, palette, childBackground);
            }
        }

        private static void ApplyToGrid(DataGridView grid, ThemePalette palette)
        {
            grid.BackgroundColor = palette.PanelBackground;
            grid.GridColor = palette.Accent.IsEmpty ? palette.Foreground : palette.Accent;
            grid.EnableHeadersVisualStyles = false;
            grid.BorderStyle = BorderStyle.FixedSingle;

            grid.DefaultCellStyle.BackColor = palette.PanelBackground;
            grid.DefaultCellStyle.ForeColor = palette.Foreground;
            grid.DefaultCellStyle.SelectionBackColor = palette.Accent.IsEmpty ? palette.MenuBackground : palette.Accent;
            grid.DefaultCellStyle.SelectionForeColor = palette.Foreground;

            grid.ColumnHeadersDefaultCellStyle.BackColor = palette.MenuBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.Foreground;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = palette.MenuBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = palette.Foreground;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            grid.RowHeadersDefaultCellStyle.BackColor = palette.MenuBackground;
            grid.RowHeadersDefaultCellStyle.ForeColor = palette.Foreground;
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = palette.MenuBackground;
            grid.RowHeadersDefaultCellStyle.SelectionForeColor = palette.Foreground;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        }

        private static void ApplyToListView(ListView listView, ThemePalette palette)
        {
            listView.BackColor = palette.PanelBackground;
            listView.ForeColor = palette.Foreground;
            listView.BorderStyle = BorderStyle.FixedSingle;
            listView.HideSelection = false;
            listView.OwnerDraw = true;

            var state = ListViewThemes.GetOrCreateValue(listView);
            state.Palette = palette;

            if (state.HandlersAttached)
                return;

            listView.DrawColumnHeader += ListView_DrawColumnHeader;
            listView.DrawItem += ListView_DrawItem;
            listView.DrawSubItem += ListView_DrawSubItem;
            state.HandlersAttached = true;
        }

        private static void ListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (sender is not ListView listView || !ListViewThemes.TryGetValue(listView, out var state))
            {
                e.DrawDefault = true;
                return;
            }

            using var backgroundBrush = new SolidBrush(state.Palette.MenuBackground);
            using var borderPen = new Pen(state.Palette.Accent.IsEmpty ? state.Palette.Foreground : state.Palette.Accent);
            var textBounds = Rectangle.Inflate(e.Bounds, -6, 0);

            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            e.Graphics.DrawRectangle(borderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, listView.Font, textBounds, state.Palette.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (sender is not ListView listView || !ListViewThemes.TryGetValue(listView, out var state))
            {
                e.DrawDefault = true;
                return;
            }

            if (listView.View != View.Details)
            {
                e.DrawDefault = true;
                return;
            }

            if (listView.CheckBoxes)
            {
                e.DrawDefault = true;
                return;
            }

            var backgroundColor = e.Item.Selected
                ? (state.Palette.Accent.IsEmpty ? state.Palette.MenuBackground : state.Palette.Accent)
                : state.Palette.PanelBackground;

            using var backgroundBrush = new SolidBrush(backgroundColor);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

            if (listView.CheckBoxes)
                DrawListViewCheckbox(e, state.Palette);

            if (e.Item.Selected && listView.Focused)
                e.DrawFocusRectangle();
        }

        private static void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (sender is not ListView listView || !ListViewThemes.TryGetValue(listView, out var state))
            {
                e.DrawDefault = true;
                return;
            }

            if (listView.View != View.Details)
            {
                e.DrawDefault = true;
                return;
            }

            if (listView.CheckBoxes)
            {
                e.DrawDefault = true;
                return;
            }

            var bounds = e.Bounds;
            if (e.ColumnIndex == 0 && listView.CheckBoxes)
            {
                bounds.X += 22;
                bounds.Width = Math.Max(0, bounds.Width - 22);
            }

            var backgroundColor = e.Item.Selected
                ? (state.Palette.Accent.IsEmpty ? state.Palette.MenuBackground : state.Palette.Accent)
                : state.Palette.PanelBackground;
            var textColor = e.Item.Selected
                ? state.Palette.Foreground
                : ResolveItemTextColor(e, state.Palette);

            using var backgroundBrush = new SolidBrush(backgroundColor);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listView.Font, Rectangle.Inflate(bounds, -4, 0), textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static Color ResolveItemTextColor(DrawListViewSubItemEventArgs e, ThemePalette palette)
        {
            if (!e.SubItem.ForeColor.IsEmpty && e.SubItem.ForeColor.A > 0)
                return e.SubItem.ForeColor;

            if (!e.Item.ForeColor.IsEmpty && e.Item.ForeColor.A > 0)
                return e.Item.ForeColor;

            return palette.Foreground;
        }

        private static void DrawListViewCheckbox(DrawListViewItemEventArgs e, ThemePalette palette)
        {
            const int leftPadding = 4;
            int checkBoxSize = 14;
            var glyphBounds = new Rectangle(
                e.Bounds.Left + leftPadding,
                e.Bounds.Top + Math.Max(0, (e.Bounds.Height - checkBoxSize) / 2),
                checkBoxSize,
                checkBoxSize);

            CheckBoxState state = e.Item.Checked
                ? CheckBoxState.CheckedNormal
                : CheckBoxState.UncheckedNormal;

            if (Application.RenderWithVisualStyles)
            {
                CheckBoxRenderer.DrawCheckBox(e.Graphics, glyphBounds.Location, state);
                return;
            }

            using var borderPen = new Pen(palette.Accent.IsEmpty ? palette.Foreground : palette.Accent);
            using var fillBrush = new SolidBrush(palette.PanelBackground);
            e.Graphics.FillRectangle(fillBrush, glyphBounds);
            e.Graphics.DrawRectangle(borderPen, glyphBounds);

            if (!e.Item.Checked)
                return;

            using var checkPen = new Pen(palette.Foreground, 2f);
            e.Graphics.DrawLines(checkPen, new[]
            {
                new Point(glyphBounds.Left + 3, glyphBounds.Top + 7),
                new Point(glyphBounds.Left + 6, glyphBounds.Bottom - 4),
                new Point(glyphBounds.Right - 3, glyphBounds.Top + 3)
            });
        }
    }
}
