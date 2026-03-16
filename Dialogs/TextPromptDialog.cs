using System;
using System.Drawing;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class TextPromptDialog : ThemeAwareForm
    {
        private readonly Label promptLabel;
        private readonly TextBox valueTextBox;

        public TextPromptDialog(string title, string prompt, string initialValue)
        {
            Text = title ?? "Enter Value";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 168);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(mainLayout);

            promptLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8),
                Text = prompt ?? string.Empty
            };
            mainLayout.Controls.Add(promptLabel, 0, 0);

            valueTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 12),
                Text = initialValue ?? string.Empty
            };
            mainLayout.Controls.Add(valueTextBox, 0, 1);

            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(8, 0, 0, 0)
            };
            buttonLayout.Controls.Add(cancelButton);

            var okButton = new Button
            {
                Text = "OK",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.OK,
                Margin = new Padding(0)
            };
            okButton.Click += (s, e) => PromptValue = valueTextBox.Text.Trim();
            buttonLayout.Controls.Add(okButton);

            mainLayout.Controls.Add(buttonLayout, 0, 2);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            PromptValue = initialValue?.Trim() ?? string.Empty;

            Load += (_, _) => UpdatePromptWidth();
            SizeChanged += (_, _) => UpdatePromptWidth();
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
        }

        public string PromptValue { get; private set; }

        private void UpdatePromptWidth()
        {
            promptLabel.MaximumSize = new Size(Math.Max(ClientSize.Width - 24, 200), 0);
        }
    }
}
