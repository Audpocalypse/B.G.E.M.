using Material_Editor.Controls;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Material_Editor.Theming
{
    internal static class ThemeApplicator
    {
        private sealed class MenuColorTable : ProfessionalColorTable
        {
            private readonly ThemeDefinition theme;

            public MenuColorTable(ThemeDefinition theme)
            {
                this.theme = theme;
                UseSystemColors = false;
            }

            public override Color ToolStripDropDownBackground => theme.Palette.MenuBackground;
            public override Color ImageMarginGradientBegin => theme.Palette.MenuBackground;
            public override Color ImageMarginGradientMiddle => theme.Palette.MenuBackground;
            public override Color ImageMarginGradientEnd => theme.Palette.MenuBackground;
            public override Color MenuBorder => theme.Palette.Accent.IsEmpty ? theme.Palette.Foreground : theme.Palette.Accent;
            public override Color MenuItemBorder => theme.Palette.Accent.IsEmpty ? theme.Palette.Foreground : theme.Palette.Accent;
            public override Color MenuItemSelected => theme.Palette.Accent.IsEmpty ? theme.Palette.MenuBackground : theme.Palette.Accent;
            public override Color MenuItemSelectedGradientBegin => MenuItemSelected;
            public override Color MenuItemSelectedGradientEnd => MenuItemSelected;
            public override Color MenuItemPressedGradientBegin => theme.Palette.MenuBackground;
            public override Color MenuItemPressedGradientMiddle => theme.Palette.MenuBackground;
            public override Color MenuItemPressedGradientEnd => theme.Palette.MenuBackground;
            public override Color CheckBackground => theme.Palette.MenuBackground;
            public override Color CheckSelectedBackground => MenuItemSelected;
            public override Color CheckPressedBackground => MenuItemSelected;
        }

        private sealed class ListViewThemeState
        {
            public ThemeDefinition Theme { get; set; }
            public bool HandlersAttached { get; set; }
        }

        private sealed class ComboThemeState
        {
            public ThemeDefinition Theme { get; set; }
            public bool HandlersAttached { get; set; }
        }

        private static readonly ConditionalWeakTable<ListView, ListViewThemeState> ListViewThemes = new();
        private static readonly ConditionalWeakTable<ComboBox, ComboThemeState> ComboThemes = new();

        public static void ApplyToForm(Form form, ThemeDefinition theme)
        {
            if (form == null || theme == null)
                return;

            AppIconProvider.Apply(form);
            form.BackColor = theme.Palette.FormBackground;
            form.ForeColor = theme.Palette.Foreground;
        }

        public static void ApplyToContainer(Control root, ThemeDefinition theme, Color background)
        {
            if (root == null || theme == null)
                return;

            root.BackColor = background;
            root.ForeColor = theme.Palette.Foreground;
            ApplyToControl(root, theme, background);
        }

        public static void ApplyToComboBox(ComboBox comboBox, ThemeDefinition theme, Color background)
        {
            if (comboBox == null || theme == null)
                return;

            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = background;
            comboBox.ForeColor = theme.Palette.Foreground;
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.ItemHeight = Math.Max(TextRenderer.MeasureText("Sample", comboBox.Font).Height + 2, comboBox.ItemHeight);

            ComboThemeState state = ComboThemes.GetOrCreateValue(comboBox);
            state.Theme = theme;

            if (state.HandlersAttached)
                return;

            comboBox.DrawItem += ComboBox_DrawItem;
            state.HandlersAttached = true;
        }

        public static void ApplyToToolStrip(ToolStrip toolStrip, ThemeDefinition theme)
        {
            if (toolStrip == null || theme == null)
                return;

            toolStrip.BackColor = theme.Palette.MenuBackground;
            toolStrip.ForeColor = theme.Palette.Foreground;
            toolStrip.RenderMode = ToolStripRenderMode.Professional;
            toolStrip.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(theme));

            if (toolStrip is ToolStripDropDownMenu dropDownMenu)
                ConfigureDropDownMenu(dropDownMenu);

            foreach (ToolStripItem item in toolStrip.Items)
                ApplyToToolStripItem(item, theme);
        }

        public static Color Blend(Color background, Color foreground, double amount)
        {
            amount = Math.Max(0d, Math.Min(1d, amount));
            double inverse = 1d - amount;
            return Color.FromArgb(
                255,
                (int)Math.Round(background.R * inverse + foreground.R * amount),
                (int)Math.Round(background.G * inverse + foreground.G * amount),
                (int)Math.Round(background.B * inverse + foreground.B * amount));
        }

        public static Color GetReadOnlyBackground(ThemeDefinition theme)
        {
            return Blend(theme.Palette.PanelBackground, theme.Semantics.ReadOnly, 0.28d);
        }

        public static Color GetLoadErrorBackground(ThemeDefinition theme)
        {
            return Blend(theme.Palette.PanelBackground, theme.Semantics.LoadError, 0.28d);
        }

        public static Color GetValidationBackground(ThemeDefinition theme)
        {
            return Blend(theme.Palette.PanelBackground, theme.Semantics.Validation, 0.24d);
        }

        public static Color GetDirtyBackground(ThemeDefinition theme)
        {
            return Blend(theme.Palette.PanelBackground, theme.Semantics.Dirty, 0.18d);
        }

        public static Color GetFrozenColumnBackground(ThemeDefinition theme)
        {
            return Blend(theme.Palette.PanelBackground, theme.Palette.MenuBackground, 0.2d);
        }

        public static Color GetAlternatingRowBackground(ThemeDefinition theme)
        {
            return Blend(theme.Palette.PanelBackground, theme.Palette.MenuBackground, 0.12d);
        }

        private static void ApplyToControl(Control control, ThemeDefinition theme, Color background)
        {
            control.BackColor = background;
            control.ForeColor = theme.Palette.Foreground;

            switch (control)
            {
                case MenuStrip menuStrip:
                    ApplyToToolStrip(menuStrip, theme);
                    break;
                case Button button:
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = theme.Palette.Accent.IsEmpty ? Color.Gray : theme.Palette.Accent;
                    button.FlatAppearance.MouseOverBackColor = theme.Palette.Accent.IsEmpty ? theme.Palette.MenuBackground : ControlPaint.Light(theme.Palette.Accent);
                    button.FlatAppearance.MouseDownBackColor = theme.Palette.Accent.IsEmpty ? theme.Palette.ControlBackground : theme.Palette.Accent;
                    button.UseVisualStyleBackColor = false;
                    button.BackColor = button.Tag is ColorControl colorControl ? colorControl.CurrentColor : theme.Palette.ControlBackground;
                    button.ForeColor = theme.Palette.Foreground;
                    break;
                case RichTextBox richTextBox:
                    richTextBox.BorderStyle = BorderStyle.None;
                    richTextBox.BackColor = theme.Palette.PanelBackground;
                    richTextBox.ForeColor = theme.Palette.Foreground;
                    break;
                case TextBoxBase textBox:
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = theme.Palette.PanelBackground;
                    textBox.ForeColor = theme.Palette.Foreground;
                    break;
                case ComboBox comboBox:
                    ApplyToComboBox(comboBox, theme, theme.Palette.ControlBackground);
                    break;
                case NumericUpDown numeric:
                    numeric.BackColor = theme.Palette.PanelBackground;
                    numeric.ForeColor = theme.Palette.Foreground;
                    break;
                case ColorToggleCheckBox colorToggleCheckBox:
                    colorToggleCheckBox.ApplyTheme(theme, background);
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = background;
                    checkBox.ForeColor = theme.Palette.Foreground;
                    break;
                case RadioButton radioButton:
                    radioButton.FlatStyle = FlatStyle.Flat;
                    radioButton.FlatAppearance.BorderSize = 1;
                    radioButton.FlatAppearance.BorderColor = theme.Palette.Accent.IsEmpty ? Color.Gray : theme.Palette.Accent;
                    radioButton.BackColor = radioButton.Checked ? theme.Semantics.SelectedToggle : background;
                    radioButton.ForeColor = theme.Palette.Foreground;
                    break;
                case GroupBox groupBox:
                    groupBox.BackColor = background;
                    groupBox.ForeColor = theme.Palette.Foreground;
                    break;
                case Label label:
                    label.BackColor = label.BorderStyle == BorderStyle.None ? background : theme.Palette.PanelBackground;
                    break;
                case Panel panel:
                    panel.BackColor = background;
                    panel.ForeColor = theme.Palette.Foreground;
                    break;
                case ListView listView:
                    ApplyToListView(listView, theme);
                    break;
                case DataGridView grid:
                    ApplyToGrid(grid, theme);
                    break;
            }

            foreach (Control child in control.Controls)
            {
                Color childBackground = child is GroupBox ? theme.Palette.ControlBackground : background;
                if (control is GroupBox)
                    childBackground = theme.Palette.ControlBackground;
                if (child is TextBoxBase || child is NumericUpDown || child is DataGridView || child is ListView)
                    childBackground = theme.Palette.PanelBackground;
                if (child is Button)
                    childBackground = theme.Palette.ControlBackground;

                ApplyToControl(child, theme, childBackground);
            }
        }

        private static void ApplyToGrid(DataGridView grid, ThemeDefinition theme)
        {
            grid.BackgroundColor = theme.Palette.PanelBackground;
            grid.GridColor = theme.Palette.Accent.IsEmpty ? theme.Palette.Foreground : theme.Palette.Accent;
            grid.EnableHeadersVisualStyles = false;
            grid.BorderStyle = BorderStyle.FixedSingle;

            grid.DefaultCellStyle.Font = grid.Font;
            grid.DefaultCellStyle.BackColor = theme.Palette.PanelBackground;
            grid.DefaultCellStyle.ForeColor = theme.Palette.Foreground;
            grid.DefaultCellStyle.SelectionBackColor = theme.Palette.Accent.IsEmpty ? theme.Palette.MenuBackground : theme.Palette.Accent;
            grid.DefaultCellStyle.SelectionForeColor = theme.Palette.Foreground;

            grid.ColumnHeadersDefaultCellStyle.Font = grid.Font;
            grid.ColumnHeadersDefaultCellStyle.BackColor = theme.Palette.MenuBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.Palette.Foreground;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.Palette.MenuBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = theme.Palette.Foreground;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            grid.RowHeadersDefaultCellStyle.Font = grid.Font;
            grid.RowHeadersDefaultCellStyle.BackColor = theme.Palette.MenuBackground;
            grid.RowHeadersDefaultCellStyle.ForeColor = theme.Palette.Foreground;
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = theme.Palette.MenuBackground;
            grid.RowHeadersDefaultCellStyle.SelectionForeColor = theme.Palette.Foreground;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        }

        private static void ApplyToListView(ListView listView, ThemeDefinition theme)
        {
            listView.BackColor = theme.Palette.PanelBackground;
            listView.ForeColor = theme.Palette.Foreground;
            listView.BorderStyle = BorderStyle.FixedSingle;
            listView.HideSelection = false;
            listView.OwnerDraw = true;

            ListViewThemeState state = ListViewThemes.GetOrCreateValue(listView);
            state.Theme = theme;

            if (state.HandlersAttached)
                return;

            listView.DrawColumnHeader += ListView_DrawColumnHeader;
            listView.DrawItem += ListView_DrawItem;
            listView.DrawSubItem += ListView_DrawSubItem;
            state.HandlersAttached = true;
        }

        private static void ApplyToToolStripItem(ToolStripItem item, ThemeDefinition theme)
        {
            item.BackColor = theme.Palette.MenuBackground;
            item.ForeColor = theme.Palette.Foreground;

            if (item is ToolStripDropDownItem dropDownItem)
            {
                dropDownItem.DropDown.BackColor = theme.Palette.MenuBackground;
                dropDownItem.DropDown.ForeColor = theme.Palette.Foreground;
                if (dropDownItem.DropDown is ToolStripDropDownMenu dropDownMenu)
                    ConfigureDropDownMenu(dropDownMenu);
                foreach (ToolStripItem child in dropDownItem.DropDownItems)
                    ApplyToToolStripItem(child, theme);
            }
        }

        private static void ConfigureDropDownMenu(ToolStripDropDownMenu dropDownMenu)
        {
            bool hasCheckableItems = false;
            bool hasImageItems = false;

            foreach (ToolStripItem item in dropDownMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    hasCheckableItems |= menuItem.CheckOnClick || menuItem.Checked;
                    hasImageItems |= menuItem.Image != null;
                }

                if (hasCheckableItems && hasImageItems)
                    break;
            }

            dropDownMenu.ShowImageMargin = hasImageItems;
            dropDownMenu.ShowCheckMargin = hasCheckableItems;
        }

        private static void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (sender is not ComboBox comboBox || !ComboThemes.TryGetValue(comboBox, out ComboThemeState state))
                return;

            e.DrawBackground();

            Color background = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? (state.Theme.Palette.Accent.IsEmpty ? state.Theme.Palette.MenuBackground : state.Theme.Palette.Accent)
                : state.Theme.Palette.PanelBackground;

            using var backgroundBrush = new SolidBrush(background);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

            string text = e.Index >= 0
                ? comboBox.Items[e.Index]?.ToString() ?? string.Empty
                : comboBox.Text ?? string.Empty;

            TextRenderer.DrawText(
                e.Graphics,
                text,
                comboBox.Font,
                Rectangle.Inflate(e.Bounds, -2, 0),
                state.Theme.Palette.Foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.DrawFocusRectangle();
        }

        private static void ListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (sender is not ListView listView || !ListViewThemes.TryGetValue(listView, out ListViewThemeState state))
            {
                e.DrawDefault = true;
                return;
            }

            using var backgroundBrush = new SolidBrush(state.Theme.Palette.MenuBackground);
            using var borderPen = new Pen(state.Theme.Palette.Accent.IsEmpty ? state.Theme.Palette.Foreground : state.Theme.Palette.Accent);
            Rectangle textBounds = Rectangle.Inflate(e.Bounds, -6, 0);

            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            e.Graphics.DrawRectangle(borderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, listView.Font, textBounds, state.Theme.Palette.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (sender is not ListView listView || !ListViewThemes.TryGetValue(listView, out ListViewThemeState state))
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

            Color backgroundColor = e.Item.Selected
                ? (state.Theme.Palette.Accent.IsEmpty ? state.Theme.Palette.MenuBackground : state.Theme.Palette.Accent)
                : state.Theme.Palette.PanelBackground;

            using var backgroundBrush = new SolidBrush(backgroundColor);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

            if (e.Item.Selected && listView.Focused)
                e.DrawFocusRectangle();
        }

        private static void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (sender is not ListView listView || !ListViewThemes.TryGetValue(listView, out ListViewThemeState state))
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

            Color backgroundColor = e.Item.Selected
                ? (state.Theme.Palette.Accent.IsEmpty ? state.Theme.Palette.MenuBackground : state.Theme.Palette.Accent)
                : state.Theme.Palette.PanelBackground;
            Color textColor = e.Item.Selected
                ? state.Theme.Palette.Foreground
                : ResolveItemTextColor(e, state.Theme);

            using var backgroundBrush = new SolidBrush(backgroundColor);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listView.Font, Rectangle.Inflate(e.Bounds, -4, 0), textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static Color ResolveItemTextColor(DrawListViewSubItemEventArgs e, ThemeDefinition theme)
        {
            if (!e.SubItem.ForeColor.IsEmpty && e.SubItem.ForeColor.A > 0)
                return e.SubItem.ForeColor;

            if (!e.Item.ForeColor.IsEmpty && e.Item.ForeColor.A > 0)
                return e.Item.ForeColor;

            return theme.Palette.Foreground;
        }
    }
}
