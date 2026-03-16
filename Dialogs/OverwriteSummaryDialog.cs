using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class OverwriteSummaryDialog : ThemeAwareForm
    {
        private readonly DataGridView resultsGrid;
        private readonly Label summaryLabel;
        private readonly Button okButton;

        public OverwriteSummaryDialog(IReadOnlyList<FieldCopyResult> results, string title = "Output Summary")
        {
            results ??= Array.Empty<FieldCopyResult>();

            Text = string.IsNullOrWhiteSpace(title) ? "Output Summary" : title;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(860, 560);
            MinimumSize = new Size(720, 420);
            MaximizeBox = true;
            MinimizeBox = true;
            ShowInTaskbar = false;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(mainLayout);

            resultsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
                ShowCellToolTips = true
            };
            resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Status",
                DataPropertyName = nameof(ResultRow.Status),
                FillWeight = 14f,
                MinimumWidth = 100
            });
            resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "File",
                DataPropertyName = nameof(ResultRow.TargetPath),
                FillWeight = 46f,
                MinimumWidth = 280
            });
            resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Details",
                DataPropertyName = nameof(ResultRow.Message),
                FillWeight = 40f,
                MinimumWidth = 220
            });
            resultsGrid.CellFormatting += ResultsGrid_CellFormatting;
            resultsGrid.CellToolTipTextNeeded += ResultsGrid_CellToolTipTextNeeded;
            resultsGrid.DataSource = results.Select(result => new ResultRow(result)).ToList();
            mainLayout.Controls.Add(resultsGrid, 0, 0);

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
                Margin = new Padding(0, 6, 12, 0),
                Text = $"{results.Count(r => r.Status == FieldCopyStatus.Success)} succeeded, {results.Count(r => r.Status == FieldCopyStatus.Skipped)} skipped, {results.Count(r => r.Status == FieldCopyStatus.Failed)} failed."
            };
            footerLayout.Controls.Add(summaryLabel, 0, 0);

            okButton = new Button
            {
                Text = "Close",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.OK,
                Margin = new Padding(0)
            };
            footerLayout.Controls.Add(okButton, 1, 0);

            mainLayout.Controls.Add(footerLayout, 0, 1);

            AcceptButton = okButton;
            CancelButton = okButton;
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
        }

        private void ResultsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || resultsGrid.Rows[e.RowIndex].DataBoundItem is not ResultRow row)
                return;

            resultsGrid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = row.StatusValue switch
            {
                FieldCopyStatus.Success => ActiveTheme.Semantics.Success,
                FieldCopyStatus.Skipped => ActiveTheme.Semantics.Warning,
                FieldCopyStatus.Failed => ActiveTheme.Semantics.Error,
                _ => resultsGrid.DefaultCellStyle.ForeColor
            };
        }

        private void ResultsGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || resultsGrid.Rows[e.RowIndex].DataBoundItem is not ResultRow row)
                return;

            e.ToolTipText = e.ColumnIndex switch
            {
                0 => row.Status,
                1 => row.TargetPath,
                2 => row.Message,
                _ => string.Empty
            };
        }

        private sealed class ResultRow
        {
            public ResultRow(FieldCopyResult result)
            {
                StatusValue = result.Status;
                Status = result.Status.ToString();
                TargetPath = result.TargetPath ?? string.Empty;
                Message = result.Message ?? string.Empty;
            }

            public FieldCopyStatus StatusValue { get; }
            public string Status { get; }
            public string TargetPath { get; }
            public string Message { get; }
        }
    }
}
