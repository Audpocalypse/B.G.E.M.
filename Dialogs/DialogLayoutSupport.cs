using System;
using System.Drawing;
using System.Windows.Forms;
using Material_Editor.Theming;

namespace Material_Editor.Dialogs
{
    internal static class DialogLayoutSupport
    {
        public static Label CreateWrappingLabel(string text, Padding margin, bool bold = false)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = margin
            };

            if (bold)
                AppearanceApplicator.SetFontRole(label, AppearanceFontRole.Bold);

            return label;
        }

        public static Label CreateInlineLabel(string text, Padding margin)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = margin
            };
        }

        public static Button CreateCommandButton(string text, Padding margin)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = margin
            };
        }

        public static NumericUpDown CreateNumericInput(
            decimal minimum,
            decimal maximum,
            decimal value,
            int width,
            Padding margin,
            int decimalPlaces = 0,
            decimal increment = 1m,
            HorizontalAlignment textAlign = HorizontalAlignment.Left)
        {
            return new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = value < minimum ? minimum : value > maximum ? maximum : value,
                DecimalPlaces = decimalPlaces,
                Increment = increment,
                Width = width,
                Anchor = AnchorStyles.Left,
                Margin = margin,
                TextAlign = textAlign
            };
        }

        public static TextBox CreatePreviewTextBox(int height, Padding margin)
        {
            return new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Top,
                Height = height,
                Margin = margin,
                TabStop = false,
                ShortcutsEnabled = true
            };
        }

        public static void UpdateMainLayoutWidth(ScrollableControl scrollHost, Control mainLayout, int minimumWidth)
        {
            if (scrollHost == null || mainLayout == null)
                return;

            int availableWidth = scrollHost.ClientSize.Width;
            if (scrollHost.VerticalScroll.Visible)
                availableWidth -= SystemInformation.VerticalScrollBarWidth;

            mainLayout.Width = Math.Max(availableWidth, minimumWidth);
        }

        public static void SetWrappingWidth(Label label, int minimumWidth)
        {
            if (label?.Parent == null)
                return;

            int availableWidth = 0;
            for (Control current = label.Parent; current != null; current = current.Parent)
                availableWidth = Math.Max(availableWidth, current.ClientSize.Width);

            if (label.Parent is ScrollableControl scrollableControl)
                availableWidth -= scrollableControl.Padding.Horizontal;

            availableWidth = Math.Max(minimumWidth, availableWidth - label.Margin.Horizontal);
            if (label.MaximumSize.Width != availableWidth)
                label.MaximumSize = new Size(availableWidth, 0);
        }

        public static void SetWrappingWidths(int minimumWidth, params Label[] labels)
        {
            foreach (Label label in labels)
                SetWrappingWidth(label, minimumWidth);
        }
    }
}
