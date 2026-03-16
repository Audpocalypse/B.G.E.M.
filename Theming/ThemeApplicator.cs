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
            public override Color MenuBorder => GetBorderColor(theme);
            public override Color MenuItemBorder => GetBorderColor(theme);
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

        private sealed class GroupBoxThemeState
        {
            public ThemeDefinition Theme { get; set; }
            public bool HandlersAttached { get; set; }
        }

        private sealed class DataGridViewThemeState
        {
            public ThemeDefinition Theme { get; set; }
            public bool HandlersAttached { get; set; }
        }

        private static readonly ConditionalWeakTable<ListView, ListViewThemeState> ListViewThemes = new();
        private static readonly ConditionalWeakTable<ComboBox, ComboThemeState> ComboThemes = new();
        private static readonly ConditionalWeakTable<GroupBox, GroupBoxThemeState> GroupBoxThemes = new();
        private static readonly ConditionalWeakTable<DataGridView, DataGridViewThemeState> DataGridViewThemes = new();

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

            root.BackColor = root is Form ? theme.Palette.FormBackground : background;
            root.ForeColor = theme.Palette.Foreground;
            ApplyToControl(root, theme, background);
        }

        public static void ApplyToComboBox(ComboBox comboBox, ThemeDefinition theme, Color background)
        {
            if (comboBox == null || theme == null)
                return;

            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = background;
            comboBox.ForeColor = GetEditableForeground(theme);
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
            toolStrip.ForeColor = GetMenuForeground(theme);
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
            return theme.Palette.AlternatingRowBackground.IsEmpty
                ? theme.Palette.PanelBackground
                : theme.Palette.AlternatingRowBackground;
        }

        private static void ApplyToControl(Control control, ThemeDefinition theme, Color background)
        {
            Color appliedBackground = control is Form ? theme.Palette.FormBackground : background;
            control.BackColor = appliedBackground;
            control.ForeColor = ResolveControlForeground(control, theme);

            switch (control)
            {
                case MenuStrip menuStrip:
                    ApplyToToolStrip(menuStrip, theme);
                    break;
                case Button button:
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = GetAccentBorderColor(theme);
                    button.FlatAppearance.MouseOverBackColor = theme.Palette.Accent.IsEmpty ? theme.Palette.MenuBackground : ControlPaint.Light(theme.Palette.Accent);
                    button.FlatAppearance.MouseDownBackColor = theme.Palette.Accent.IsEmpty ? theme.Palette.ControlBackground : theme.Palette.Accent;
                    button.UseVisualStyleBackColor = false;
                    button.BackColor = button.Tag is ColorControl colorControl ? colorControl.CurrentColor : theme.Palette.ControlBackground;
                    button.ForeColor = theme.Palette.Foreground;
                    break;
                case RichTextBox richTextBox:
                    richTextBox.BorderStyle = BorderStyle.None;
                    richTextBox.BackColor = theme.Palette.PanelBackground;
                    richTextBox.ForeColor = GetEditableForeground(theme);
                    break;
                case TextBoxBase textBox:
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = theme.Palette.PanelBackground;
                    textBox.ForeColor = GetEditableForeground(theme);
                    break;
                case ComboBox comboBox:
                    ApplyToComboBox(comboBox, theme, theme.Palette.ControlBackground);
                    break;
                case NumericUpDown numeric:
                    numeric.BackColor = theme.Palette.PanelBackground;
                    numeric.ForeColor = GetEditableForeground(theme);
                    break;
                case ColorToggleCheckBox colorToggleCheckBox:
                    colorToggleCheckBox.ApplyTheme(theme, background);
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = background;
                    checkBox.ForeColor = GetLabelForeground(theme);
                    break;
                case RadioButton radioButton:
                    radioButton.FlatStyle = FlatStyle.Flat;
                    radioButton.FlatAppearance.BorderSize = 1;
                    radioButton.FlatAppearance.BorderColor = GetAccentBorderColor(theme);
                    radioButton.BackColor = radioButton.Checked ? theme.Semantics.SelectedToggle : background;
                    radioButton.ForeColor = GetLabelForeground(theme);
                    break;
                case GroupBox groupBox:
                    ApplyToGroupBox(groupBox, theme, background);
                    break;
                case Label label:
                    label.BackColor = label.BorderStyle == BorderStyle.None ? appliedBackground : theme.Palette.PanelBackground;
                    label.ForeColor = GetLabelForeground(theme);
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
                Color childBackground = control is Form
                    ? theme.Palette.FormBackground
                    : child is GroupBox
                        ? theme.Palette.ControlBackground
                        : background;
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
            grid.GridColor = GetTableBorderColor(theme);
            grid.EnableHeadersVisualStyles = false;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;

            grid.DefaultCellStyle.Font = grid.Font;
            grid.DefaultCellStyle.BackColor = theme.Palette.PanelBackground;
            grid.DefaultCellStyle.ForeColor = GetEditableForeground(theme);
            grid.DefaultCellStyle.SelectionBackColor = theme.Palette.Accent.IsEmpty ? theme.Palette.MenuBackground : theme.Palette.Accent;
            grid.DefaultCellStyle.SelectionForeColor = GetEditableForeground(theme);
            grid.AlternatingRowsDefaultCellStyle.BackColor = GetAlternatingRowBackground(theme);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = GetEditableForeground(theme);
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.SelectionForeColor;

            grid.ColumnHeadersDefaultCellStyle.Font = grid.Font;
            grid.ColumnHeadersDefaultCellStyle.BackColor = theme.Palette.MenuBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = GetMenuForeground(theme);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.Palette.MenuBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = GetMenuForeground(theme);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            grid.RowHeadersDefaultCellStyle.Font = grid.Font;
            grid.RowHeadersDefaultCellStyle.BackColor = theme.Palette.MenuBackground;
            grid.RowHeadersDefaultCellStyle.ForeColor = GetMenuForeground(theme);
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = theme.Palette.MenuBackground;
            grid.RowHeadersDefaultCellStyle.SelectionForeColor = GetMenuForeground(theme);
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            DataGridViewThemeState state = DataGridViewThemes.GetOrCreateValue(grid);
            state.Theme = theme;

            if (!state.HandlersAttached)
            {
                grid.Paint += DataGridView_Paint;
                grid.CellPainting += DataGridView_CellPainting;
                state.HandlersAttached = true;
            }

            grid.Invalidate();
        }

        private static void ApplyToListView(ListView listView, ThemeDefinition theme)
        {
            listView.BackColor = theme.Palette.PanelBackground;
            listView.ForeColor = theme.Palette.Foreground;
            listView.BorderStyle = BorderStyle.None;
            listView.HideSelection = false;
            listView.OwnerDraw = true;

            ListViewThemeState state = ListViewThemes.GetOrCreateValue(listView);
            state.Theme = theme;

            if (!state.HandlersAttached)
            {
                listView.DrawColumnHeader += ListView_DrawColumnHeader;
                listView.DrawItem += ListView_DrawItem;
                listView.DrawSubItem += ListView_DrawSubItem;
                listView.Paint += ListView_Paint;
                state.HandlersAttached = true;
            }

            listView.Invalidate();
        }

        private static void ApplyToToolStripItem(ToolStripItem item, ThemeDefinition theme)
        {
            item.BackColor = theme.Palette.MenuBackground;
            item.ForeColor = GetMenuForeground(theme);

            if (item is ToolStripDropDownItem dropDownItem)
            {
                dropDownItem.DropDown.BackColor = theme.Palette.MenuBackground;
                dropDownItem.DropDown.ForeColor = GetMenuForeground(theme);
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
                GetEditableForeground(state.Theme),
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
            using var borderPen = new Pen(GetTableBorderColor(state.Theme));
            Rectangle textBounds = Rectangle.Inflate(e.Bounds, -6, 0);

            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            e.Graphics.DrawRectangle(borderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, listView.Font, textBounds, GetMenuForeground(state.Theme), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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
                ? GetEditableForeground(state.Theme)
                : ResolveItemTextColor(e, state.Theme);

            using var backgroundBrush = new SolidBrush(backgroundColor);
            using var borderPen = new Pen(GetTableBorderColor(state.Theme));
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listView.Font, Rectangle.Inflate(e.Bounds, -4, 0), textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (e.ColumnIndex == 0)
                e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Left, e.Bounds.Bottom - 1);

            e.Graphics.DrawLine(borderPen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
            e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
        }

        private static void ListView_Paint(object sender, PaintEventArgs e)
        {
            if (sender is not ListView listView || !ListViewThemes.TryGetValue(listView, out ListViewThemeState state))
                return;

            Rectangle bounds = listView.ClientRectangle;
            bounds.Width = Math.Max(1, bounds.Width - 1);
            bounds.Height = Math.Max(1, bounds.Height - 1);

            using var borderPen = new Pen(GetTableBorderColor(state.Theme));
            e.Graphics.DrawRectangle(borderPen, bounds);
        }

        private static Color ResolveItemTextColor(DrawListViewSubItemEventArgs e, ThemeDefinition theme)
        {
            if (!e.SubItem.ForeColor.IsEmpty && e.SubItem.ForeColor.A > 0)
                return e.SubItem.ForeColor;

            if (!e.Item.ForeColor.IsEmpty && e.Item.ForeColor.A > 0)
                return e.Item.ForeColor;

            return theme.Palette.Foreground;
        }

        private static void ApplyToGroupBox(GroupBox groupBox, ThemeDefinition theme, Color background)
        {
            groupBox.BackColor = background;
            groupBox.ForeColor = GetLabelForeground(theme);

            GroupBoxThemeState state = GroupBoxThemes.GetOrCreateValue(groupBox);
            state.Theme = theme;

            if (groupBox is CollapsibleGroupBox or ThemedGroupBox)
            {
                groupBox.Invalidate();
                return;
            }

            if (!state.HandlersAttached)
            {
                groupBox.Paint += GroupBox_Paint;
                state.HandlersAttached = true;
            }

            groupBox.Invalidate();
        }

        private static void GroupBox_Paint(object sender, PaintEventArgs e)
        {
            if (sender is not GroupBox groupBox || !GroupBoxThemes.TryGetValue(groupBox, out GroupBoxThemeState state))
                return;

            DrawGroupBox(e.Graphics, groupBox, state.Theme);
        }

        private static void DataGridView_Paint(object sender, PaintEventArgs e)
        {
            if (sender is not DataGridView grid || !DataGridViewThemes.TryGetValue(grid, out DataGridViewThemeState state))
                return;

            Rectangle bounds = grid.ClientRectangle;
            bounds.Width = Math.Max(1, bounds.Width - 1);
            bounds.Height = Math.Max(1, bounds.Height - 1);

            using var borderPen = new Pen(GetTableBorderColor(state.Theme));
            e.Graphics.DrawRectangle(borderPen, bounds);
        }

        private static void DataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (sender is not DataGridView grid || !DataGridViewThemes.TryGetValue(grid, out DataGridViewThemeState state))
                return;

            e.Paint(e.ClipBounds, e.PaintParts & ~DataGridViewPaintParts.Border);

            using var borderPen = new Pen(GetTableBorderColor(state.Theme));
            Rectangle bounds = e.CellBounds;
            int right = bounds.Right - 1;
            int bottom = bounds.Bottom - 1;

            if (e.ColumnIndex <= 0)
                e.Graphics.DrawLine(borderPen, bounds.Left, bounds.Top, bounds.Left, bottom);

            if (e.RowIndex <= 0)
                e.Graphics.DrawLine(borderPen, bounds.Left, bounds.Top, right, bounds.Top);

            e.Graphics.DrawLine(borderPen, right, bounds.Top, right, bottom);
            e.Graphics.DrawLine(borderPen, bounds.Left, bottom, right, bottom);
            e.Handled = true;
        }

        internal static void DrawGroupBox(Graphics graphics, GroupBox groupBox, ThemeDefinition theme)
        {
            if (graphics == null || groupBox == null || theme == null)
                return;

            string text = groupBox.Text ?? string.Empty;
            Size textSize = TextRenderer.MeasureText(
                graphics,
                string.IsNullOrEmpty(text) ? " " : text,
                groupBox.Font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.SingleLine);
            int textOffset = 8;
            int top = Math.Max(1, textSize.Height / 2);
            Rectangle bounds = groupBox.ClientRectangle;

            using var borderPen = new Pen(GetBorderColor(theme));
            using var textBackgroundBrush = new SolidBrush(groupBox.BackColor);

            if (!string.IsNullOrWhiteSpace(text))
            {
                Rectangle textBounds = new(textOffset - 2, 0, textSize.Width + 4, textSize.Height);
                graphics.FillRectangle(textBackgroundBrush, textBounds);
                graphics.DrawLine(borderPen, bounds.Left, top, textOffset - 4, top);
                graphics.DrawLine(borderPen, textOffset + textSize.Width + 2, top, bounds.Right - 1, top);
                TextRenderer.DrawText(
                    graphics,
                    text,
                    groupBox.Font,
                    new Rectangle(textOffset, 0, Math.Max(0, bounds.Width - textOffset - 8), textSize.Height),
                    GetLabelForeground(theme),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
            else
            {
                graphics.DrawLine(borderPen, bounds.Left, top, bounds.Right - 1, top);
            }

            graphics.DrawLine(borderPen, bounds.Left, top, bounds.Left, bounds.Bottom - 1);
            graphics.DrawLine(borderPen, bounds.Right - 1, top, bounds.Right - 1, bounds.Bottom - 1);
            graphics.DrawLine(borderPen, bounds.Left, bounds.Bottom - 1, bounds.Right - 1, bounds.Bottom - 1);
        }

        internal static bool TryGetGroupBoxTheme(GroupBox groupBox, out ThemeDefinition theme)
        {
            if (groupBox != null && GroupBoxThemes.TryGetValue(groupBox, out GroupBoxThemeState state))
            {
                theme = state.Theme;
                return theme != null;
            }

            theme = null;
            return false;
        }

        internal static Color GetBorderColor(ThemeDefinition theme)
        {
            if (theme == null)
                return Color.Gray;

            if (!theme.Palette.BorderColor.IsEmpty)
                return theme.Palette.BorderColor;

            return theme.Palette.Accent.IsEmpty ? theme.Palette.Foreground : theme.Palette.Accent;
        }

        internal static Color GetTableBorderColor(ThemeDefinition theme)
        {
            if (theme == null)
                return Color.Gray;

            if (!theme.Palette.TableBorderColor.IsEmpty)
                return theme.Palette.TableBorderColor;

            return GetBorderColor(theme);
        }

        private static Color GetAccentBorderColor(ThemeDefinition theme)
        {
            if (theme == null)
                return Color.Gray;

            return theme.Palette.Accent.IsEmpty
                ? theme.Palette.Foreground
                : theme.Palette.Accent;
        }

        internal static Color GetMenuForeground(ThemeDefinition theme)
        {
            if (theme == null)
                return SystemColors.ControlText;

            return theme.Palette.MenuForeground.IsEmpty
                ? theme.Palette.Foreground
                : theme.Palette.MenuForeground;
        }

        internal static Color GetLabelForeground(ThemeDefinition theme)
        {
            if (theme == null)
                return SystemColors.ControlText;

            return theme.Palette.LabelForeground.IsEmpty
                ? theme.Palette.Foreground
                : theme.Palette.LabelForeground;
        }

        internal static Color GetEditableForeground(ThemeDefinition theme)
        {
            if (theme == null)
                return SystemColors.ControlText;

            return theme.Palette.EditableForeground.IsEmpty
                ? theme.Palette.Foreground
                : theme.Palette.EditableForeground;
        }

        private static Color ResolveControlForeground(Control control, ThemeDefinition theme)
        {
            return control switch
            {
                ToolStrip _ => GetMenuForeground(theme),
                Label _ or GroupBox _ or CheckBox _ or RadioButton _ => GetLabelForeground(theme),
                TextBoxBase _ or NumericUpDown _ or ComboBox _ => GetEditableForeground(theme),
                _ => theme?.Palette.Foreground ?? SystemColors.ControlText
            };
        }
    }
}
