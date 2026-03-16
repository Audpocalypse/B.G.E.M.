using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Material_Editor.Dialogs;
using Material_Editor.Models;
using Material_Editor.Services;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private void OpenFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select a folder to scan for BGSM or BGEM files."
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var files = MaterialFileSelectionService.EnumerateFolder(dialog.SelectedPath);
            if (files.Count == 0)
            {
                MessageBox.Show(this, "No BGSM or BGEM files were found in the selected folder.", "No Files Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenMaterialSelection(files, appendToBulk: false);
        }

        private void AddFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsBulkMode || bulkSession == null)
                return;

            using var dialog = new OpenFileDialog
            {
                Filter = "Material/Effect File (.bgsm; .bgem)|*.bgsm;*.bgem",
                Title = "Add materials to the current bulk session...",
                Multiselect = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            OpenMaterialSelection(dialog.FileNames, appendToBulk: true);
        }

        private void AddFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsBulkMode || bulkSession == null)
                return;

            using var dialog = new FolderBrowserDialog
            {
                Description = "Select a folder to add BGSM or BGEM files from."
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var files = MaterialFileSelectionService.EnumerateFolder(dialog.SelectedPath);
            if (files.Count == 0)
            {
                MessageBox.Show(this, "No BGSM or BGEM files were found in the selected folder.", "No Files Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenMaterialSelection(files, appendToBulk: true);
        }

        private void SaveSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveBulkChanges(selectedOnly: true);
        }

        private void RemoveSelectedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveSelectedBulkFiles();
        }

        private void SetBulkDirtyRemoveBehavior(BulkDirtyRemoveBehavior behavior)
        {
            config.BulkDirtyRemoveBehavior = behavior;
        }

        private void UpdateWorkspaceCommandState()
        {
            bool hasSingleFile = currentMaterial != null && !IsBulkMode;
            bool hasBulkSession = IsBulkMode && bulkSession != null;

            saveToolStripMenuItem.Enabled = hasSingleFile || hasBulkSession;
            saveAsToolStripMenuItem.Enabled = hasSingleFile || hasBulkSession;
            closeToolStripMenuItem.Enabled = hasSingleFile || hasBulkSession;
            saveSelectedToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.SelectedRowCount > 0;
            addFilesToolStripMenuItem.Enabled = hasBulkSession;
            addFolderToolStripMenuItem.Enabled = hasBulkSession;
            removeSelectedFilesToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.SelectedRowCount > 0;
            generateVariationsToolStripMenuItem.Enabled = hasSingleFile;
            overwriteFilesByFieldToolStripMenuItem.Enabled = hasSingleFile;

            openFolderToolStripMenuItem.Enabled = true;
            saveSelectedToolStripMenuItem.Visible = hasBulkSession;
            addFilesToolStripMenuItem.Visible = hasBulkSession;
            addFolderToolStripMenuItem.Visible = hasBulkSession;
            removeSelectedFilesToolStripMenuItem.Visible = hasBulkSession;
        }

        private void UpdateWindowTitle()
        {
            Text = GetTitleText();
        }

        private bool ConfirmCanReplaceWorkspace()
        {
            if (IsBulkMode && bulkSession != null && bulkEditorView != null)
                bulkEditorView.CommitPendingEdits();

            if (IsBulkMode && bulkSession != null && bulkEditorView != null && bulkEditorView.HasDirtyRows)
            {
                IReadOnlyList<BulkMaterialEditRow> dirtyRows = bulkSession.Rows.Where(row => bulkEditorView.IsDirtyRow(row)).ToArray();
                return HandleDirtyBulkRowsBeforeContinuing(
                    dirtyRows,
                    dirtyRows,
                    "Bulk Save Summary",
                    "close or replace the current bulk workspace");
            }

            if (changed)
            {
                DialogResult res = MessageBox.Show(
                    "There are unsaved changes to the file.\nDo want to save them before continuing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    SaveToolStripMenuItem_Click(null, null);
                    return !changed;
                }

                return res != DialogResult.Cancel;
            }

            return true;
        }

        private bool SaveBulkChanges(bool selectedOnly, bool showEmptyMessage = true)
        {
            if (!IsBulkMode || bulkSession == null)
                return false;

            IReadOnlyList<BulkMaterialEditRow> scopeRows = selectedOnly
                ? bulkEditorView.SelectedRows
                : bulkSession.Rows.Where(row => !row.HasLoadError).ToArray();

            if (scopeRows.Count == 0)
            {
                if (showEmptyMessage)
                    MessageBox.Show(this, selectedOnly ? "Select one or more rows to save." : "There are no rows to save.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            IReadOnlyList<FieldCopyResult> results = selectedOnly
                ? bulkEditorView.SaveSelectedChanges()
                : bulkEditorView.SaveAllChanges();

            using var summary = new OverwriteSummaryDialog(results, selectedOnly ? "Save Selected Summary" : "Bulk Save Summary");
            summary.ShowDialog(this);
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();

            return results.All(result => result.Status != FieldCopyStatus.Failed);
        }

        private void SaveBulkAs()
        {
            if (!IsBulkMode || bulkSession == null)
                return;

            IReadOnlyList<BulkMaterialEditRow> exportRows = bulkEditorView.SelectedRows.Count > 0
                ? bulkEditorView.SelectedRows
                : bulkSession.Rows.Where(row => !row.HasLoadError).ToArray();

            if (exportRows.Count == 0)
            {
                MessageBox.Show(this, "There are no rows available to export.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new BulkExportDialog(exportRows);
            if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.OutputPattern))
                return;

            IReadOnlyList<FieldCopyResult> results = BulkMaterialExportService.Export(exportRows, dialog.OutputPattern, serializeToJSONToolStripMenuItem.Checked);
            using var summary = new OverwriteSummaryDialog(results, "Bulk Save As Summary");
            summary.ShowDialog(this);
        }

        private void OpenMaterialSelection(IEnumerable<string> rawPaths, bool appendToBulk)
        {
            var summary = MaterialFileSelectionService.Analyze(rawPaths);
            int ignoredUnsupported = summary.UnsupportedFiles.Count;
            if (ignoredUnsupported > 0)
            {
                MessageBox.Show(this, $"Ignored {ignoredUnsupported} file(s) that are not BGSM or BGEM materials.", "Ignored Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            MaterialType? selectedType = ResolveSelectedMaterialType(summary, appendToBulk);
            if (!selectedType.HasValue)
                return;

            IReadOnlyList<string> selectedPaths = summary.GetFiles(selectedType.Value);
            if (selectedPaths.Count == 0)
            {
                MessageBox.Show(this, "No files of the chosen material type were found in the selection.", "No Matching Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!appendToBulk && !ConfirmCanReplaceWorkspace())
                return;

            if (appendToBulk && IsBulkMode && bulkSession != null)
            {
                BulkMaterialSessionAddResult addResult = bulkEditorView.AddFiles(selectedPaths);
                if (addResult.DuplicateCount > 0)
                {
                    MessageBox.Show(this, $"Skipped {addResult.DuplicateCount} file(s) that were already in the bulk session.", "Duplicate Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                return;
            }

            if (selectedPaths.Count == 1)
            {
                OpenMaterial(selectedPaths[0]);
                return;
            }

            EnterBulkMode(selectedType.Value, selectedPaths);
        }

        private MaterialType? ResolveSelectedMaterialType(MaterialFileSelectionSummary summary, bool appendToBulk)
        {
            if (appendToBulk && IsBulkMode && bulkSession != null)
            {
                MaterialType currentType = bulkSession.MaterialType;
                int skipped = currentType == MaterialType.Material ? summary.EffectFiles.Count : summary.MaterialFiles.Count;
                if (skipped > 0)
                {
                    string skippedType = currentType == MaterialType.Material ? ".bgem" : ".bgsm";
                    MessageBox.Show(this, $"Ignored {skipped} file(s) that do not match the current bulk session type ({skippedType}).", "Ignored Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                return currentType;
            }

            if (summary.MaterialFiles.Count == 0 && summary.EffectFiles.Count == 0)
                return null;

            if (!summary.HasMixedMaterialTypes)
                return summary.MaterialFiles.Count > 0 ? MaterialType.Material : MaterialType.Effect;

            using var dialog = new MaterialTypeChoiceDialog(summary);
            return dialog.ShowDialog(this) == DialogResult.OK
                ? dialog.SelectedMaterialType
                : null;
        }

        private void EnterBulkMode(MaterialType materialType, IReadOnlyList<string> filePaths)
        {
            ExitBulkMode();
            ClearSingleWorkspace();

            bulkSession = BulkMaterialEditSession.Create(materialType, filePaths);
            bulkEditorView.Initialize(bulkSession, config, bulkBackupBeforeWrite);
            bulkEditorView.Visible = true;

            topControlsLayout.Visible = false;
            contentHostLayout.Visible = false;
            contentScrollPanel.AutoScroll = false;

            workspaceMode = WorkspaceMode.Bulk;
            changed = false;
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
        }

        private void ExitBulkMode()
        {
            bulkSession = null;
            if (bulkEditorView != null)
                bulkEditorView.ClearSession();

            topControlsLayout.Visible = true;
            contentHostLayout.Visible = true;
            contentScrollPanel.AutoScroll = true;
            workspaceMode = currentMaterial != null ? WorkspaceMode.Single : WorkspaceMode.Empty;
        }

        private void RemoveSelectedBulkFiles()
        {
            if (!IsBulkMode || bulkSession == null)
                return;

            IReadOnlyList<BulkMaterialEditRow> selectedRows = bulkEditorView.SelectedRows;
            if (selectedRows.Count == 0)
            {
                MessageBox.Show(this, "Select one or more rows to remove.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            IReadOnlyList<BulkMaterialEditRow> dirtyRows = selectedRows.Where(row => bulkEditorView.IsDirtyRow(row)).ToArray();
            if (!HandleDirtyBulkRowsBeforeContinuing(
                dirtyRows,
                selectedRows,
                "Remove Selected Summary",
                "remove selected files from the bulk session"))
                return;

            bulkSession.RemoveRows(selectedRows);
            bulkEditorView.NotifySessionChanged();
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
        }

        private bool HandleDirtyBulkRowsBeforeContinuing(
            IReadOnlyList<BulkMaterialEditRow> dirtyRows,
            IReadOnlyList<BulkMaterialEditRow> saveRows,
            string summaryTitle,
            string actionDescription)
        {
            if (dirtyRows == null || dirtyRows.Count == 0)
                return true;

            BulkDirtyRemoveBehavior behavior = config.BulkDirtyRemoveBehavior;
            if (behavior == BulkDirtyRemoveBehavior.Ask)
            {
                using var dialog = new BulkDirtyFileRemovalDialog(dirtyRows.Count, actionDescription);
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Choice == BulkDirtyFileRemovalChoice.Cancel)
                    return false;

                if (dialog.RememberedBehavior.HasValue)
                    SetBulkDirtyRemoveBehavior(dialog.RememberedBehavior.Value);

                behavior = dialog.Choice == BulkDirtyFileRemovalChoice.Save
                    ? BulkDirtyRemoveBehavior.Save
                    : BulkDirtyRemoveBehavior.Discard;
            }

            if (behavior != BulkDirtyRemoveBehavior.Save)
                return true;

            IReadOnlyList<FieldCopyResult> results = bulkSession.ApplySelectedChanges(saveRows, bulkEditorView.BackupBeforeWrite);
            using var summary = new OverwriteSummaryDialog(results, summaryTitle);
            summary.ShowDialog(this);

            if (results.Any(result => result.Status == FieldCopyStatus.Failed))
            {
                bulkEditorView.NotifySessionChanged();
                return false;
            }

            bulkEditorView.NotifySessionChanged();
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
            return true;
        }

        private string GetTitleText()
        {
            if (IsBulkMode && bulkSession != null)
            {
                string typeLabel = bulkSession.MaterialType == MaterialType.Effect ? "BGEM" : "BGSM";
                string dirtyPrefix = bulkEditorView?.HasDirtyRows == true ? "*" : string.Empty;
                return $"{dirtyPrefix}{ApplicationTitle} - Bulk {typeLabel} ({bulkSession.Rows.Count} files)";
            }

            string fileName = WorkFileName;
            if (string.IsNullOrEmpty(fileName))
                return ApplicationTitle;

            if (currentMaterial != null)
                return $"{ApplicationTitle} – {fileName} (Version {currentMaterial.Version})";

            return $"{ApplicationTitle} – {fileName}";
        }
    }
}
