using Material_Editor.Controls;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Material_Editor.Theming
{
    internal enum AppearanceFontRole
    {
        Default,
        Bold,
        Monospace
    }

    internal static class AppearanceApplicator
    {
        private sealed class FontRoleState
        {
            public AppearanceFontRole Role { get; set; }
        }

        private static readonly ConditionalWeakTable<Control, FontRoleState> FontRoles = new();
        private static readonly ConditionalWeakTable<ToolStripItem, FontRoleState> ToolStripFontRoles = new();

        public static void SetFontRole(Control control, AppearanceFontRole role)
        {
            if (control == null)
                return;

            FontRoles.GetOrCreateValue(control).Role = role;
        }

        public static void SetFontRole(ToolStripItem item, AppearanceFontRole role)
        {
            if (item == null)
                return;

            ToolStripFontRoles.GetOrCreateValue(item).Role = role;
        }

        public static void ApplyToForm(Form form, AppearanceDefinition appearance)
        {
            if (form == null || appearance == null)
                return;

            ApplyFont(form, appearance);
            ThemeApplicator.ApplyToForm(form, appearance.Theme);
        }

        public static void ApplyToContainer(Control root, AppearanceDefinition appearance, Color background)
        {
            if (root == null || appearance == null)
                return;

            ApplyFontsRecursive(root, appearance);
            ThemeApplicator.ApplyToContainer(root, appearance.Theme, background);
        }

        public static void ApplyToToolStrip(ToolStrip toolStrip, AppearanceDefinition appearance)
        {
            if (toolStrip == null || appearance == null)
                return;

            toolStrip.Font = ResolveFont(toolStrip, appearance);
            ThemeApplicator.ApplyToToolStrip(toolStrip, appearance.Theme);

            foreach (ToolStripItem item in toolStrip.Items)
                ApplyToToolStripItem(item, appearance);
        }

        public static void ApplyToComboBox(ComboBox comboBox, AppearanceDefinition appearance, Color background)
        {
            if (comboBox == null || appearance == null)
                return;

            ApplyFont(comboBox, appearance);
            ThemeApplicator.ApplyToComboBox(comboBox, appearance.Theme, background);
        }

        public static Font ResolveFont(Control control, AppearanceDefinition appearance)
        {
            if (control == null || appearance == null)
                return null;

            AppearanceFontRole role = FontRoles.TryGetValue(control, out FontRoleState state)
                ? state.Role
                : AppearanceFontRole.Default;
            return ResolveFont(role, appearance);
        }

        public static Font ResolveFont(ToolStripItem item, AppearanceDefinition appearance)
        {
            if (item == null || appearance == null)
                return null;

            AppearanceFontRole role = ToolStripFontRoles.TryGetValue(item, out FontRoleState state)
                ? state.Role
                : AppearanceFontRole.Default;
            return ResolveFont(role, appearance);
        }

        private static void ApplyFontsRecursive(Control control, AppearanceDefinition appearance)
        {
            ApplyFont(control, appearance);

            foreach (Control child in control.Controls)
                ApplyFontsRecursive(child, appearance);
        }

        private static void ApplyFont(Control control, AppearanceDefinition appearance)
        {
            Font font = ResolveFont(control, appearance);
            if (font != null)
                control.Font = font;
        }

        private static void ApplyToToolStripItem(ToolStripItem item, AppearanceDefinition appearance)
        {
            item.Font = ResolveFont(item, appearance);

            if (item is ToolStripDropDownItem dropDownItem)
            {
                dropDownItem.DropDown.Font = appearance.Font;
                foreach (ToolStripItem child in dropDownItem.DropDownItems)
                    ApplyToToolStripItem(child, appearance);
            }
        }

        private static Font ResolveFont(AppearanceFontRole role, AppearanceDefinition appearance)
        {
            return role switch
            {
                AppearanceFontRole.Bold => appearance.BoldFont,
                AppearanceFontRole.Monospace => appearance.MonospaceFont,
                _ => appearance.Font
            };
        }
    }
}
