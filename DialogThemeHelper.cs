using System.Drawing;
using System.Windows.Forms;

namespace Material_Editor
{
    internal static class DialogThemeHelper
    {
        public static void Apply(Form form, ThemePalette palette, UITheme theme)
        {
            if (form == null || theme == UITheme.Default)
                return;

            form.BackColor = palette.FormBackground;
            form.ForeColor = palette.Foreground;
            ApplyToControl(form, palette, palette.FormBackground);
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
                    button.UseVisualStyleBackColor = false;
                    break;
                case TextBoxBase textBox:
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = palette.PanelBackground;
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
                    label.BackColor = background;
                    label.ForeColor = palette.Foreground;
                    break;
                case ListView listView:
                    listView.BackColor = palette.PanelBackground;
                    listView.ForeColor = palette.Foreground;
                    listView.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case DataGridView grid:
                    ApplyToGrid(grid, palette);
                    break;
            }

            foreach (Control child in control.Controls)
            {
                var childBackground = child is GroupBox ? palette.FormBackground : background;
                if (control is GroupBox)
                    childBackground = palette.FormBackground;
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

            grid.RowHeadersDefaultCellStyle.BackColor = palette.MenuBackground;
            grid.RowHeadersDefaultCellStyle.ForeColor = palette.Foreground;
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = palette.MenuBackground;
            grid.RowHeadersDefaultCellStyle.SelectionForeColor = palette.Foreground;
        }
    }
}
