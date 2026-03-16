using Material_Editor.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal enum BulkDirtyFileRemovalChoice
    {
        Cancel,
        Save,
        Discard
    }

    internal sealed class BulkDirtyFileRemovalDialog : ThemeAwareForm
    {
        private readonly Label descriptionLabel;
        private readonly ColorToggleCheckBox rememberChoiceCheckBox;
        private readonly string actionDescription;

        public BulkDirtyFileRemovalDialog(int dirtyFileCount, string actionDescription = null)
        {
            Text = "Unsaved Bulk Changes";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 210);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            this.actionDescription = string.IsNullOrWhiteSpace(actionDescription)
                ? "continue"
                : actionDescription.Trim();

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

            descriptionLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 12),
                Text = BuildDescription(dirtyFileCount)
            };
            mainLayout.Controls.Add(descriptionLabel, 0, 0);

            rememberChoiceCheckBox = new ColorToggleCheckBox
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 12),
                Text = "Remember my choice"
            };
            mainLayout.Controls.Add(rememberChoiceCheckBox, 0, 1);

            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var cancelButton = CreateButton("Cancel", DialogResult.Cancel);
            buttonLayout.Controls.Add(cancelButton);

            var discardButton = CreateButton("Discard");
            discardButton.Click += (s, e) => CloseWith(BulkDirtyFileRemovalChoice.Discard);
            buttonLayout.Controls.Add(discardButton);

            var saveButton = CreateButton("Save");
            saveButton.Click += (s, e) => CloseWith(BulkDirtyFileRemovalChoice.Save);
            buttonLayout.Controls.Add(saveButton);

            mainLayout.Controls.Add(buttonLayout, 0, 2);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Load += (_, _) => UpdateDescriptionWidth();
            SizeChanged += (_, _) => UpdateDescriptionWidth();
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
        }

        public BulkDirtyFileRemovalChoice Choice { get; private set; } = BulkDirtyFileRemovalChoice.Cancel;
        public bool RememberChoice => rememberChoiceCheckBox.Checked;

        public BulkDirtyRemoveBehavior? RememberedBehavior => !RememberChoice
            ? null
            : Choice switch
            {
                BulkDirtyFileRemovalChoice.Save => BulkDirtyRemoveBehavior.Save,
                BulkDirtyFileRemovalChoice.Discard => BulkDirtyRemoveBehavior.Discard,
                _ => null
            };

        private void CloseWith(BulkDirtyFileRemovalChoice choice)
        {
            Choice = choice;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Button CreateButton(string text, DialogResult dialogResult = DialogResult.None)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = dialogResult,
                Margin = new Padding(8, 0, 0, 0)
            };
        }

        private void UpdateDescriptionWidth()
        {
            descriptionLabel.MaximumSize = new Size(Math.Max(ClientSize.Width - 24, 260), 0);
        }

        private string BuildDescription(int dirtyFileCount)
        {
            return
                $"You are about to {actionDescription} with {dirtyFileCount} file(s) containing unapplied edits.{Environment.NewLine}" +
                $"Save writes those files before continuing. Discard drops the edits. If you remember this choice, the same action will happen automatically until you change it back in Settings.";
        }
    }
}
