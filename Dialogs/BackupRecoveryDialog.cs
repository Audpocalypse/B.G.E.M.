using Material_Editor.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class BackupRecoveryDialog : ThemeAwareForm
    {
        private readonly DataGridView backupsGrid;
        private readonly Label summaryLabel;
        private readonly Button recoverButton;
        private readonly ContextMenuStrip selectionMenu;

        public BackupRecoveryDialog(IReadOnlyList<MaterialBackupEntry> backups, string title)
        {
            backups ??= Array.Empty<MaterialBackupEntry>();

            Text = string.IsNullOrWhiteSpace(title) ? "Available Backups" : title;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(980, 580);
            MinimumSize = new Size(760, 420);
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

            backupsGrid = new DataGridView
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
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
                ShowCellToolTips = true
            };
            backupsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Source",
                DataPropertyName = nameof(BackupRow.SourcePath),
                FillWeight = 34f,
                MinimumWidth = 220
            });
            backupsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Backup",
                DataPropertyName = nameof(BackupRow.DisplayLabel),
                FillWeight = 25f,
                MinimumWidth = 180
            });
            backupsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Created",
                DataPropertyName = nameof(BackupRow.CreatedDisplay),
                FillWeight = 18f,
                MinimumWidth = 140
            });
            backupsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Size",
                DataPropertyName = nameof(BackupRow.SizeDisplay),
                FillWeight = 10f,
                MinimumWidth = 90
            });
            backupsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Type",
                DataPropertyName = nameof(BackupRow.Kind),
                FillWeight = 13f,
                MinimumWidth = 100
            });
            backupsGrid.CellDoubleClick += BackupsGrid_CellDoubleClick;
            backupsGrid.SelectionChanged += (_, _) => UpdateRecoverButtonState();
            backupsGrid.CellToolTipTextNeeded += BackupsGrid_CellToolTipTextNeeded;
            backupsGrid.DataSource = backups.Select(backup => new BackupRow(backup)).ToList();
            selectionMenu = GridSelectionMenuSupport.Attach(
                backupsGrid,
                selectAll: SelectAllBackupRows,
                selectNone: ClearBackupSelection,
                canSelectAll: () => backupsGrid.Rows.Count > 0,
                canSelectNone: () => backupsGrid.SelectedRows.Count > 0);
            mainLayout.Controls.Add(backupsGrid, 0, 0);

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
                Text = $"{backups.Count} backup file(s) available."
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
            footerLayout.Controls.Add(buttonLayout, 1, 0);

            var cancelButton = new Button
            {
                Text = "Close",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(8, 0, 0, 0)
            };
            buttonLayout.Controls.Add(cancelButton);

            recoverButton = new Button
            {
                Text = "Recover Selected",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Enabled = backups.Count > 0,
                Margin = new Padding(8, 0, 0, 0)
            };
            recoverButton.Click += RecoverButton_Click;
            buttonLayout.Controls.Add(recoverButton);

            mainLayout.Controls.Add(footerLayout, 0, 1);

            AcceptButton = recoverButton;
            CancelButton = cancelButton;
            UpdateRecoverButtonState();
        }

        public MaterialBackupEntry SelectedBackup
        {
            get
            {
                if (backupsGrid.SelectedRows.Count == 0)
                    return null;

                DataGridViewRow selectedRow = backupsGrid.CurrentRow != null && backupsGrid.CurrentRow.Selected
                    ? backupsGrid.CurrentRow
                    : backupsGrid.SelectedRows.Cast<DataGridViewRow>().OrderBy(row => row.Index).FirstOrDefault();
                return selectedRow?.DataBoundItem is BackupRow row
                    ? row.Entry
                    : null;
            }
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
            AppearanceApplicator.ApplyToToolStrip(selectionMenu, appearance);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.A) && backupsGrid.Rows.Count > 0)
            {
                SelectAllBackupRows();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SelectAllBackupRows()
        {
            if (backupsGrid.Rows.Count == 0)
                return;

            backupsGrid.ClearSelection();
            foreach (DataGridViewRow row in backupsGrid.Rows)
                row.Selected = true;

            backupsGrid.CurrentCell = backupsGrid.Rows[0].Cells[0];
            UpdateRecoverButtonState();
        }

        private void ClearBackupSelection()
        {
            backupsGrid.ClearSelection();
            backupsGrid.CurrentCell = null;
            UpdateRecoverButtonState();
        }

        private void RecoverButton_Click(object sender, EventArgs e)
        {
            if (SelectedBackup == null)
                return;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BackupsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            RecoverButton_Click(sender, EventArgs.Empty);
        }

        private void UpdateRecoverButtonState()
        {
            recoverButton.Enabled = SelectedBackup != null;
            summaryLabel.Text = $"{backupsGrid.Rows.Count} backup file(s) available. {backupsGrid.SelectedRows.Count} selected.";
        }

        private void BackupsGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || backupsGrid.Rows[e.RowIndex].DataBoundItem is not BackupRow row)
                return;

            e.ToolTipText = e.ColumnIndex switch
            {
                0 => row.SourcePath,
                1 => row.Entry.BackupPath,
                2 => row.CreatedDisplay,
                3 => row.SizeDisplay,
                4 => row.Kind,
                _ => string.Empty
            };
        }

        private sealed class BackupRow
        {
            public BackupRow(MaterialBackupEntry entry)
            {
                Entry = entry;
                SourcePath = entry.SourcePath;
                DisplayLabel = entry.DisplayLabel;
                CreatedDisplay = entry.CreatedUtc == DateTime.MinValue
                    ? string.Empty
                    : entry.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                SizeDisplay = FormatBytes(entry.SizeBytes);
                Kind = entry.IsRetainedOriginal
                    ? "Retained Original"
                    : entry.IsLegacy ? "Legacy .bak" : "Timestamped";
            }

            public MaterialBackupEntry Entry { get; }
            public string SourcePath { get; }
            public string DisplayLabel { get; }
            public string CreatedDisplay { get; }
            public string SizeDisplay { get; }
            public string Kind { get; }

            private static string FormatBytes(long value)
            {
                if (value < 1024)
                    return $"{value} B";

                double size = value;
                string[] units = { "KB", "MB", "GB", "TB" };
                int unitIndex = -1;
                do
                {
                    size /= 1024d;
                    unitIndex++;
                }
                while (size >= 1024d && unitIndex < units.Length - 1);

                return $"{size:0.#} {units[unitIndex]}";
            }
        }
    }
}
