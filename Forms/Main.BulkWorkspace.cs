using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private bool TryResolveDirtyRemoveBehavior(int dirtyFileCount, string actionDescription, out BulkDirtyRemoveBehavior behavior)
        {
            behavior = config?.BulkDirtyRemoveBehavior ?? BulkDirtyRemoveBehavior.Ask;
            if (behavior != BulkDirtyRemoveBehavior.Ask)
                return true;

            using var dialog = new BulkDirtyFileRemovalDialog(dirtyFileCount, actionDescription);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Choice == BulkDirtyFileRemovalChoice.Cancel)
                return false;

            if (dialog.RememberedBehavior.HasValue)
                SetBulkDirtyRemoveBehavior(dialog.RememberedBehavior.Value);

            behavior = dialog.Choice == BulkDirtyFileRemovalChoice.Save
                ? BulkDirtyRemoveBehavior.Save
                : BulkDirtyRemoveBehavior.Discard;
            return true;
        }

        private bool HandleDirtySingleFileBeforeContinuing(string actionDescription)
        {
            if (!TryResolveDirtyRemoveBehavior(1, actionDescription, out BulkDirtyRemoveBehavior behavior))
                return false;

            if (behavior != BulkDirtyRemoveBehavior.Save)
                return true;

            SaveToolStripMenuItem_Click(null, null);
            return !changed;
        }

        private void UpdateWorkspaceCommandState()
        {
            bool hasSingleFile = currentMaterial != null && !IsBulkMode;
            bool hasBulkSession = IsBulkMode && bulkSession != null;
            bool canUndoSingle = hasSingleFile && CanUndoSingleEditor;
            bool canRedoSingle = hasSingleFile && CanRedoSingleEditor;
            bool canUndoBulk = hasBulkSession && bulkEditorView != null && bulkEditorView.CanUndo;
            bool canRedoBulk = hasBulkSession && bulkEditorView != null && bulkEditorView.CanRedo;
            bool canFind = hasBulkSession && bulkEditorView != null && bulkEditorView.CanFindSelection;
            bool canFindReplace = hasBulkSession && bulkEditorView != null && bulkEditorView.CanFindReplaceSelection;
            bool canRecover = (hasSingleFile && !string.IsNullOrWhiteSpace(workFilePath))
                || (hasBulkSession && bulkEditorView != null && bulkEditorView.SelectedRowCount > 0);

            saveToolStripMenuItem.Enabled = hasSingleFile || hasBulkSession;
            saveAsToolStripMenuItem.Enabled = hasSingleFile || hasBulkSession;
            closeToolStripMenuItem.Enabled = hasSingleFile || hasBulkSession;
            saveSelectedToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.SelectedRowCount > 0;
            addFilesToolStripMenuItem.Enabled = hasBulkSession;
            addFolderToolStripMenuItem.Enabled = hasBulkSession;
            removeSelectedFilesToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.SelectedRowCount > 0;
            generateVariationsToolStripMenuItem.Enabled = hasSingleFile;
            overwriteFilesByFieldToolStripMenuItem.Enabled = hasSingleFile;
            findToolStripMenuItem.Enabled = canFind;
            findReplaceToolStripMenuItem.Enabled = canFindReplace;
            if (recoveryToolStripMenuItem != null)
                recoveryToolStripMenuItem.Enabled = canRecover;

            openFolderToolStripMenuItem.Enabled = true;
            saveSelectedToolStripMenuItem.Visible = hasBulkSession;
            addFilesToolStripMenuItem.Visible = hasBulkSession;
            addFolderToolStripMenuItem.Visible = hasBulkSession;
            removeSelectedFilesToolStripMenuItem.Visible = hasBulkSession;
            editRevealInExplorerToolStripMenuItem.Visible = hasBulkSession;
            editReloadFromDiskToolStripMenuItem.Visible = hasBulkSession;

            if (editToolStripMenuItem != null)
            {
                bool canSendSingleEditor = hasBulkSession
                    && bulkEditorView.SelectedRows.Count == 1
                    && bulkEditorView.SelectedRows[0].Material != null
                    && !bulkEditorView.SelectedRows[0].HasLoadError;

                editToolStripMenuItem.Enabled = canUndoSingle || canRedoSingle || hasBulkSession;
                editUndoToolStripMenuItem.Enabled = canUndoSingle || canUndoBulk;
                editRedoToolStripMenuItem.Enabled = canRedoSingle || canRedoBulk;
                editSendToSingleEditorToolStripMenuItem.Enabled = canSendSingleEditor;
                editRevealInExplorerToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanCopyRowsSelection;
                editReloadFromDiskToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanCopyRowsSelection;
                editEditToggleToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanEditOrToggleSelection;
                editCutToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanCutFieldsSelection;
                editCutRowsToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanCutRowsSelection;
                editCopyRowsToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanCopyRowsSelection;
                editCopyFieldsToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanCopyFieldsSelection;
                editPasteRowsToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanPasteRowsSelection;
                editPasteFieldsToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanPasteFieldsSelection;
                editClearToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanClearSelection;
                editSelectAllToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanSelectAll;
                editSelectRowToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanSelectCurrentRow;
                editSelectPageAboveToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanSelectCurrentRow;
                editSelectPageBelowToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanSelectCurrentRow;
                editSelectAllAboveToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanSelectCurrentRow;
                editSelectAllBelowToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanSelectCurrentRow;
                editSelectDirtyRowsToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanSelectDirtyRows;
                editSelectErrorRowsToolStripMenuItem.Enabled = hasBulkSession && bulkEditorView.CanSelectErrorRows;
            }

            UpdateBulkProjectionMenuState(hasBulkSession);
        }

        private void UpdateWindowTitle()
        {
            string title = GetTitleText();
            if (!IsBulkMode && changed && !title.StartsWith("*", StringComparison.Ordinal))
                title = "*" + title;

            Text = title;
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
                return HandleDirtySingleFileBeforeContinuing("close or replace the current file");
            }

            return true;
        }

        private bool SaveBulkChanges(bool selectedOnly, bool showEmptyMessage = true)
        {
            if (!IsBulkMode || bulkSession == null)
                return false;

            IReadOnlyList<BulkMaterialEditRow> scopeRows = GetBulkSaveScopeRows(selectedOnly)
                .Where(row => !row.HasLoadError)
                .ToArray();

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

            IReadOnlyList<BulkMaterialEditRow> exportRows = GetBulkExportRows();

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

        private IReadOnlyList<BulkMaterialEditRow> GetBulkSaveScopeRows(bool selectedOnly)
        {
            if (bulkSession == null || bulkEditorView == null)
                return Array.Empty<BulkMaterialEditRow>();

            return selectedOnly
                ? bulkEditorView.SelectedRows
                : bulkEditorView.VisibleRows;
        }

        private IReadOnlyList<BulkMaterialEditRow> GetBulkExportRows()
        {
            if (bulkSession == null || bulkEditorView == null)
                return Array.Empty<BulkMaterialEditRow>();

            return bulkEditorView.SelectedRows.Count > 0
                ? bulkEditorView.SelectedRows
                : bulkEditorView.VisibleRows.Where(row => !row.HasLoadError).ToArray();
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
            bulkBackupBeforeWrite = config.CreateBackupsByDefault;
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

        private BulkMaterialEditRow GetSingleSelectedBulkRow(string actionDescription)
        {
            if (!IsBulkMode || bulkSession == null)
                return null;

            IReadOnlyList<BulkMaterialEditRow> selectedRows = bulkEditorView.SelectedRows;
            if (selectedRows.Count != 1)
            {
                MessageBox.Show(this, $"Select exactly one file to {actionDescription}.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            BulkMaterialEditRow row = selectedRows[0];
            if (row.HasLoadError || row.Material == null)
            {
                MessageBox.Show(this, "The selected row could not be loaded and cannot be sent to another workflow.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            return row;
        }

        private void SendSelectedBulkRowToSingleEditor()
        {
            BulkMaterialEditRow row = GetSingleSelectedBulkRow("open it in the single-file editor");
            if (row == null || !ConfirmCanReplaceWorkspace())
                return;

            bool markDirty = bulkEditorView?.IsDirtyRow(row) == true;
            ExitBulkMode();
            OpenMaterialState(row.FilePath, CloneMaterial(row.Material), row.OriginalMaterial ?? row.Material, markDirty, "Opening material...");
        }

        private void SendSelectedBulkRowToGenerateVariations()
        {
            BulkMaterialEditRow row = GetSingleSelectedBulkRow("generate variations");
            if (row == null)
                return;

            WorkflowExecutionResult execution = RunGenerateVariationsWorkflow(CloneMaterial(row.Material), BuildSuggestedVariationOutputPattern(row.FilePath), allowReturnToBulkEditor: true);
            if (execution?.Results == null)
                return;

            using var summary = new OverwriteSummaryDialog(execution.Results, "Generation Summary");
            summary.ShowDialog(this);
            if (execution.AddResultsToCurrentBulkEditor)
                AddWorkflowResultFilesToBulk(execution.Results);
        }

        private void SendSelectedBulkRowToOverwriteFiles()
        {
            BulkMaterialEditRow row = GetSingleSelectedBulkRow("overwrite files by field");
            if (row == null)
                return;

            WorkflowExecutionResult execution = RunOverwriteFilesByFieldWorkflow(CloneMaterial(row.Material), CloneMaterial(row.OriginalMaterial ?? row.Material), allowReturnToBulkEditor: true);
            if (execution?.Results == null)
                return;

            using var summary = new OverwriteSummaryDialog(execution.Results, "Overwrite Summary");
            summary.ShowDialog(this);
            if (execution.AddResultsToCurrentBulkEditor)
                AddWorkflowResultFilesToBulk(execution.Results);
        }

        private void RevealSelectedBulkFilesInExplorer()
        {
            if (!IsBulkMode || bulkSession == null)
                return;

            IReadOnlyList<BulkMaterialEditRow> selectedRows = bulkEditorView.SelectedRows;
            if (selectedRows.Count == 0)
            {
                MessageBox.Show(this, "Select one or more rows to reveal.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                foreach (string path in selectedRows.Select(row => row.FilePath).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to reveal one or more files in Explorer.{Environment.NewLine}{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReloadSelectedBulkFilesFromDisk()
        {
            if (!IsBulkMode || bulkSession == null)
                return;

            IReadOnlyList<BulkMaterialEditRow> selectedRows = bulkEditorView.SelectedRows;
            if (selectedRows.Count == 0)
            {
                MessageBox.Show(this, "Select one or more rows to reload.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            IReadOnlyList<BulkMaterialEditRow> dirtyRows = selectedRows.Where(row => bulkEditorView.IsDirtyRow(row)).ToArray();
            if (!HandleDirtyBulkRowsBeforeContinuing(
                dirtyRows,
                dirtyRows,
                "Reload Selected Summary",
                "reload selected files from disk"))
            {
                return;
            }

            string[] selectedPaths = selectedRows.Select(row => row.FilePath).ToArray();
            bulkSession.ReloadRows(selectedRows);
            bulkEditorView.NotifySessionChanged();
            bulkEditorView.SelectFileRows(selectedPaths);
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
        }

        private void AddWorkflowResultFilesToBulk(IReadOnlyList<FieldCopyResult> results)
        {
            if (!IsBulkMode || bulkSession == null || results == null || results.Count == 0)
                return;

            string[] addedPaths = results
                .Where(result => result.Status == FieldCopyStatus.Success)
                .Select(result => result.TargetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (addedPaths.Length == 0)
                return;

            bulkEditorView.AddFiles(addedPaths);
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

            if (!TryResolveDirtyRemoveBehavior(dirtyRows.Count, actionDescription, out BulkDirtyRemoveBehavior behavior))
                return false;

            if (behavior != BulkDirtyRemoveBehavior.Save)
                return true;

            bulkEditorView?.CommitPendingEdits();
            bulkEditorView?.ClearStaleValidationErrors(saveRows);
            IReadOnlyList<FieldCopyResult> results = bulkSession.ApplySelectedChanges(saveRows, bulkEditorView.BackupBeforeWrite, config);
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
                return changed ? $"*{ApplicationTitle}" : ApplicationTitle;

            if (currentMaterial != null)
                return $"{ApplicationTitle} – {fileName} (Version {currentMaterial.Version})";

            return $"{ApplicationTitle} – {fileName}";
        }
    }
}
