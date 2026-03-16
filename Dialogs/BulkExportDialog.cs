using Material_Editor.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class BulkExportDialog : ThemeAwareForm
    {
        private readonly IReadOnlyList<BulkMaterialEditRow> rows;
        private readonly Label introLabel;
        private readonly TextBox outputPatternTextBox;
        private readonly TextBox previewTextBox;
        private readonly Label summaryLabel;

        public BulkExportDialog(IReadOnlyList<BulkMaterialEditRow> rows)
        {
            this.rows = rows ?? Array.Empty<BulkMaterialEditRow>();

            Text = "Bulk Save As";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 360);
            MinimumSize = new Size(720, 340);
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(12)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(mainLayout);

            introLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8),
                Text = "Export the current bulk rows to new paths. Use {name}, {ext}, {index}, or {indexNN} in the output pattern."
            };
            mainLayout.Controls.Add(introLabel, 0, 0);

            var outputLabel = new Label
            {
                Text = "Output pattern",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            mainLayout.Controls.Add(outputLabel, 0, 1);

            var patternLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            patternLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            patternLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            outputPatternTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Text = BuildDefaultOutputPattern()
            };
            outputPatternTextBox.TextChanged += (s, e) => RefreshPreview();
            patternLayout.Controls.Add(outputPatternTextBox, 0, 0);

            var browseButton = new Button
            {
                Text = "Browse...",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            browseButton.Click += BrowseButton_Click;
            patternLayout.Controls.Add(browseButton, 1, 0);
            mainLayout.Controls.Add(patternLayout, 0, 2);

            var previewLabel = new Label
            {
                Text = "Preview",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            mainLayout.Controls.Add(previewLabel, 0, 3);

            previewTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Margin = new Padding(0, 0, 0, 12)
            };
            mainLayout.Controls.Add(previewTextBox, 0, 4);

            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0)
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            summaryLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 12, 0)
            };
            footerLayout.Controls.Add(summaryLabel, 0, 0);

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
                Text = "Export",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.OK,
                Margin = new Padding(0)
            };
            okButton.Click += (s, e) => OutputPattern = outputPatternTextBox.Text.Trim();
            buttonLayout.Controls.Add(okButton);

            footerLayout.Controls.Add(buttonLayout, 1, 0);
            mainLayout.Controls.Add(footerLayout, 0, 5);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Load += (_, _) => UpdateIntroWidth();
            SizeChanged += (_, _) => UpdateIntroWidth();
            RefreshPreview();
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
        }

        public string OutputPattern { get; private set; }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choose an output folder for exported materials."
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            string extension = rows.FirstOrDefault()?.FilePath is string filePath
                ? Path.GetExtension(filePath)
                : ".bgsm";

            outputPatternTextBox.Text = Path.Combine(dialog.SelectedPath, "{name}_{indexNN}" + extension);
        }

        private string BuildDefaultOutputPattern()
        {
            string firstPath = rows.FirstOrDefault()?.FilePath;
            if (string.IsNullOrWhiteSpace(firstPath))
                return Path.Combine(Environment.CurrentDirectory, "{name}_{indexNN}.bgsm");

            string directory = Path.GetDirectoryName(firstPath) ?? Environment.CurrentDirectory;
            string extension = Path.GetExtension(firstPath);
            return Path.Combine(directory, "{name}_{indexNN}" + extension);
        }

        private void RefreshPreview()
        {
            IReadOnlyList<string> previewPaths = BulkMaterialExportService.PreviewOutputPaths(rows, outputPatternTextBox.Text);
            previewTextBox.Text = previewPaths.Count == 0
                ? "No rows selected for export."
                : string.Join(Environment.NewLine, previewPaths.Take(12));

            if (previewPaths.Count > 12)
                previewTextBox.AppendText($"{Environment.NewLine}...and {previewPaths.Count - 12} more");

            summaryLabel.Text = $"{rows.Count} row(s) will be exported.";
        }

        private void UpdateIntroWidth()
        {
            introLabel.MaximumSize = new Size(Math.Max(ClientSize.Width - 24, 320), 0);
        }
    }
}
