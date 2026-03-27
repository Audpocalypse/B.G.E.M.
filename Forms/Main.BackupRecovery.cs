using Material_Editor.Dialogs;
using Material_Editor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private void InitializeRecoveryMenu()
        {
            recoveryToolStripMenuItem = new ToolStripMenuItem("Recovery");

            recoverMostRecentBackupToolStripMenuItem = new ToolStripMenuItem("Recover Most Recent Backup");
            recoverMostRecentBackupToolStripMenuItem.Click += (s, e) => RecoverBackups(useNewestBackup: true);

            recoverLeastRecentBackupToolStripMenuItem = new ToolStripMenuItem("Recover Least Recent Backup");
            recoverLeastRecentBackupToolStripMenuItem.Click += (s, e) => RecoverBackups(useNewestBackup: false);

            browseBackupsToolStripMenuItem = new ToolStripMenuItem("Browse Backups...");
            browseBackupsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.B;
            browseBackupsToolStripMenuItem.Click += BrowseBackupsToolStripMenuItem_Click;

            recoveryToolStripMenuItem.DropDownItems.Add(recoverMostRecentBackupToolStripMenuItem);
            recoveryToolStripMenuItem.DropDownItems.Add(recoverLeastRecentBackupToolStripMenuItem);
            recoveryToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            recoveryToolStripMenuItem.DropDownItems.Add(browseBackupsToolStripMenuItem);

            int closeIndex = fileToolStripMenuItem.DropDownItems.IndexOf(closeToolStripMenuItem);
            if (closeIndex >= 0)
                fileToolStripMenuItem.DropDownItems.Insert(closeIndex + 1, recoveryToolStripMenuItem);
            else
                fileToolStripMenuItem.DropDownItems.Add(recoveryToolStripMenuItem);
        }

        private void BrowseBackupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IReadOnlyList<string> sourcePaths = GetRecoverySourcePaths(requireSelection: true);
            if (sourcePaths.Count == 0)
                return;

            IReadOnlyList<MaterialBackupEntry> backups = MaterialBackupService.GetAvailableBackups(sourcePaths);
            if (backups.Count == 0)
            {
                MessageBox.Show(this, "No backups were found for the current file selection.", "No Backups Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new BackupRecoveryDialog(backups, "Available Backups");
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedBackup == null)
                return;

            RecoverSpecificBackup(dialog.SelectedBackup);
        }

        private void RecoverBackups(bool useNewestBackup)
        {
            IReadOnlyList<string> sourcePaths = GetRecoverySourcePaths(requireSelection: true);
            if (sourcePaths.Count == 0)
                return;

            if (!PrepareRecoveryTargets(sourcePaths))
                return;

            var results = new List<FieldCopyResult>(sourcePaths.Count);
            foreach (string sourcePath in sourcePaths)
            {
                FieldCopyResult result = useNewestBackup
                    ? MaterialBackupService.RestoreNewest(sourcePath, config, backupCurrentBeforeRestore: config.CreateBackupsByDefault)
                    : MaterialBackupService.RestoreOldest(sourcePath, config, backupCurrentBeforeRestore: config.CreateBackupsByDefault);
                results.Add(result);
            }

            ShowRecoveryResults(results, useNewestBackup ? "Recover Most Recent Backup" : "Recover Least Recent Backup");
            RefreshWorkspaceAfterRecovery(sourcePaths, results);
        }

        private void RecoverSpecificBackup(MaterialBackupEntry backup)
        {
            if (backup == null)
                return;

            string sourcePath = MaterialFilePersistence.NormalizePath(backup.SourcePath);
            if (!PrepareRecoveryTargets(new[] { sourcePath }))
                return;

            FieldCopyResult result = MaterialBackupService.RestoreBackup(backup, config, backupCurrentBeforeRestore: config.CreateBackupsByDefault);
            ShowRecoveryResults(new[] { result }, "Backup Recovery");
            RefreshWorkspaceAfterRecovery(new[] { sourcePath }, new[] { result });
        }

        private IReadOnlyList<string> GetRecoverySourcePaths(bool requireSelection)
        {
            if (IsBulkMode && bulkSession != null && bulkEditorView != null)
            {
                IReadOnlyList<BulkMaterialEditRow> selectedRows = bulkEditorView.SelectedRows;
                if (selectedRows.Count == 0)
                {
                    if (requireSelection)
                    {
                        MessageBox.Show(this, "Select one or more files in the bulk editor first.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    return Array.Empty<string>();
                }

                return selectedRows
                    .Select(row => row.FilePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            if (!string.IsNullOrWhiteSpace(workFilePath))
                return new[] { MaterialFilePersistence.NormalizePath(workFilePath) };

            if (requireSelection)
            {
                MessageBox.Show(this, "Open a material file first.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return Array.Empty<string>();
        }

        private bool PrepareRecoveryTargets(IReadOnlyList<string> sourcePaths)
        {
            if (sourcePaths == null || sourcePaths.Count == 0)
                return false;

            if (!IsBulkMode)
                return ConfirmCanReplaceWorkspace();

            IReadOnlyList<BulkMaterialEditRow> selectedRows = bulkEditorView.SelectedRows
                .Where(row => sourcePaths.Contains(row.FilePath, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            IReadOnlyList<BulkMaterialEditRow> dirtyRows = selectedRows
                .Where(row => bulkEditorView.IsDirtyRow(row))
                .ToArray();

            return HandleDirtyBulkRowsBeforeContinuing(
                dirtyRows,
                dirtyRows,
                "Backup Recovery Save Summary",
                "recover backups for the selected files");
        }

        private void RefreshWorkspaceAfterRecovery(IReadOnlyList<string> sourcePaths, IReadOnlyList<FieldCopyResult> results)
        {
            if (results == null || results.All(result => result.Status != FieldCopyStatus.Success))
                return;

            string[] recoveredPaths = results
                .Where(result => result.Status == FieldCopyStatus.Success)
                .Select(result => MaterialFilePersistence.NormalizePath(result.TargetPath))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (recoveredPaths.Length == 0)
                return;

            if (IsBulkMode && bulkSession != null)
            {
                IReadOnlyList<BulkMaterialEditRow> rowsToReload = bulkSession.Rows
                    .Where(row => recoveredPaths.Contains(row.FilePath, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
                if (rowsToReload.Count > 0)
                {
                    bulkSession.ReloadRows(rowsToReload);
                    bulkEditorView.NotifySessionChanged();
                    bulkEditorView.SelectFileRows(recoveredPaths);
                    UpdateWorkspaceCommandState();
                    UpdateWindowTitle();
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(workFilePath)
                && recoveredPaths.Contains(MaterialFilePersistence.NormalizePath(workFilePath), StringComparer.OrdinalIgnoreCase))
            {
                OpenMaterial(workFilePath);
            }
        }

        private void ShowRecoveryResults(IReadOnlyList<FieldCopyResult> results, string title)
        {
            using var summary = new OverwriteSummaryDialog(results, title);
            summary.ShowDialog(this);
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
        }
    }
}
