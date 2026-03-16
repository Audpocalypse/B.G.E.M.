using Material_Editor.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class MaterialTypeChoiceDialog : ThemeAwareForm
    {
        private readonly Label descriptionLabel;

        public MaterialTypeChoiceDialog(MaterialFileSelectionSummary selectionSummary)
        {
            if (selectionSummary == null)
                throw new ArgumentNullException(nameof(selectionSummary));

            Text = "Choose Material Type";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 190);
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

            descriptionLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 12),
                Text = $"The selection contains both BGSM and BGEM files.{Environment.NewLine}Choose which type to keep for this bulk session."
            };
            mainLayout.Controls.Add(descriptionLabel, 0, 0);

            var choiceLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            choiceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            choiceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var bgsmButton = new Button
            {
                Text = $"Use BGSM Files ({selectionSummary.MaterialFiles.Count})",
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 6, 0),
                MinimumSize = new Size(0, 58)
            };
            bgsmButton.Click += (s, e) =>
            {
                SelectedMaterialType = MaterialType.Material;
                DialogResult = DialogResult.OK;
                Close();
            };
            choiceLayout.Controls.Add(bgsmButton, 0, 0);

            var bgemButton = new Button
            {
                Text = $"Use BGEM Files ({selectionSummary.EffectFiles.Count})",
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(6, 0, 0, 0),
                MinimumSize = new Size(0, 58)
            };
            bgemButton.Click += (s, e) =>
            {
                SelectedMaterialType = MaterialType.Effect;
                DialogResult = DialogResult.OK;
                Close();
            };
            choiceLayout.Controls.Add(bgemButton, 1, 0);

            mainLayout.Controls.Add(choiceLayout, 0, 1);

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
                Margin = new Padding(0)
            };
            buttonLayout.Controls.Add(cancelButton);
            mainLayout.Controls.Add(buttonLayout, 0, 2);

            CancelButton = cancelButton;

            Load += (_, _) => UpdateDescriptionWidth();
            SizeChanged += (_, _) => UpdateDescriptionWidth();
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
        }

        public MaterialType SelectedMaterialType { get; private set; } = MaterialType.Material;

        private void UpdateDescriptionWidth()
        {
            descriptionLabel.MaximumSize = new Size(Math.Max(ClientSize.Width - 24, 280), 0);
        }
    }
}
