using MaterialLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Material_Editor.Controls;
using Material_Editor.Dialogs;
using Material_Editor.Models;
using Material_Editor.Services;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private string ChangeFileExtension(string filePath)
        {
            if (filePath == null)
                return null;

            return Path.ChangeExtension(filePath, MaterialFileTypeHelper.GetExpectedExtension(CurrentMaterialType));
        }

        private string BuildSuggestedVariationOutputPattern()
        {
            string sourcePath = workFilePath;
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                try
                {
                    string directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                    string extension = Path.GetExtension(sourcePath);
                    if (string.IsNullOrEmpty(extension))
                        extension = ".bgsm";

                    string fileName = Path.GetFileNameWithoutExtension(sourcePath);
                    if (string.IsNullOrEmpty(fileName))
                        fileName = "variation";

                    return Path.Combine(directory, $"{fileName}_{{index}}{extension}");
                }
                catch
                {
                }
            }

            return "variation_{index}.bgsm";
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ConfirmCanReplaceWorkspace())
                return;

            ExitBulkMode();

            workFilePath = null;
            Text = ApplicationTitle;

            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
            layoutGeneral.Enabled = true;
            layoutMaterial.Enabled = true;
            layoutEffect.Enabled = true;

            SuspendAll();

            CreateMaterialControls();

            int selectedIndex;
            if (currentMaterial.Version > 2 && currentMaterial.Version <= 22)
                selectedIndex = (int)Game.FO76;
            else
                selectedIndex = (int)Game.FO4;

            if ((int)CurrentGame != selectedIndex)
                SetGameSelection((Game)selectedIndex);
            else
                FillVersionDropdown();

            SetMaterialTypeSelection(MaterialType.Material);
            workspaceMode = WorkspaceMode.Single;

            ResumeAll();
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                OpenMaterialSelection(openFileDialog.FileNames, appendToBulk: false);
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsBulkMode)
            {
                SaveBulkChanges(selectedOnly: false);
                return;
            }

            if (string.IsNullOrEmpty(workFilePath))
            {
                SaveAsToolStripMenuItem_Click(null, null);
                return;
            }

            TrySaveSingleMaterial(workFilePath, updateWorkspacePath: false);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsBulkMode)
            {
                SaveBulkAs();
                return;
            }

            saveFileDialog.Filter = CurrentMaterialType switch
            {
                MaterialType.Effect => "Effect File (.bgem)|*.bgem",
                _ => "Material File (.bgsm)|*.bgsm",
            };

            string fileName = WorkFileName;
            if (fileName != null)
                saveFileDialog.FileName = ChangeFileExtension(fileName);

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                TrySaveSingleMaterial(saveFileDialog.FileName, updateWorkspacePath: true);
        }

        private void OverwriteFilesByFieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsBulkMode)
            {
                MessageBox.Show("Overwrite Files by Field is only available in single-file mode.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (currentMaterial == null)
            {
                MessageBox.Show("Open or create a material before using the overwrite tool.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentState = CaptureCurrentMaterialState();
            if (currentState == null)
                return;

            var descriptors = MaterialFieldRegistry.GetDescriptors(currentState);
            if (descriptors.Count == 0)
            {
                MessageBox.Show("No writable fields are available for the current material.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var baseline = originalMaterial ?? CloneMaterial(currentState) ?? currentState;

            using var fieldSelection = new FieldSelectionDialog(descriptors, baseline, currentState);
            if (fieldSelection.ShowDialog(this) != DialogResult.OK || fieldSelection.SelectedFields == null || fieldSelection.SelectedFields.Count == 0)
                return;

            using var targetDialog = new TargetFileSelectionDialog(CurrentMaterialType);
            if (targetDialog.ShowDialog(this) != DialogResult.OK)
                return;

            var tool = new FieldOverwriteTool();
            IReadOnlyList<FieldCopyResult> results;
            if (targetDialog.UseAdvancedMode)
            {
                using var advancedDialog = new AdvancedFieldOverwriteDialog(currentState, fieldSelection.SelectedFields, targetDialog.TargetFiles);
                if (advancedDialog.ShowDialog(this) != DialogResult.OK || advancedDialog.Options == null)
                    return;

                results = tool.Run(currentState, new FieldOverwriteOptions
                {
                    Descriptors = fieldSelection.SelectedFields,
                    TargetFiles = targetDialog.TargetFiles,
                    BackupBeforeWrite = targetDialog.BackupBeforeWrite,
                    IterativeOptions = advancedDialog.Options
                });
            }
            else
            {
                results = tool.Run(currentState, fieldSelection.SelectedFields, targetDialog.TargetFiles, targetDialog.BackupBeforeWrite);
            }

            using var summary = new OverwriteSummaryDialog(results, "Overwrite Summary");
            summary.ShowDialog(this);
        }

        private void BulkMaterialEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenToolStripMenuItem_Click(sender, e);
        }

        private void GenerateVariationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsBulkMode)
            {
                MessageBox.Show("Generate Variations remains a single-file workflow. Use File -> Save As... in bulk mode for renamed exports.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (currentMaterial == null)
            {
                MessageBox.Show("Open a material before generating variations.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var template = CaptureCurrentMaterialState();
            if (template == null)
                return;

            var descriptors = MaterialFieldRegistry.GetDescriptors(template)
                .Where(descriptor => descriptor.GetValue(template) is string)
                .ToList();

            if (descriptors.Count == 0)
            {
                MessageBox.Show("No string or path fields are available for variation.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentValues = descriptors.Select(descriptor => descriptor.GetValue(template) as string ?? string.Empty).ToList();
            using var dialog = new VariationGeneratorDialog(descriptors, currentValues, BuildSuggestedVariationOutputPattern());
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Options == null)
                return;

            var results = MaterialVariationGenerator.Generate(template, dialog.Options, serializeToJSONToolStripMenuItem.Checked);
            using var summary = new OverwriteSummaryDialog(results, "Generation Summary");
            summary.ShowDialog(this);
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ConfirmCanReplaceWorkspace())
                return;

            if (IsBulkMode)
            {
                ExitBulkMode();
                ClearSingleWorkspace();
                UpdateWorkspaceCommandState();
                UpdateWindowTitle();
                return;
            }

            ClearSingleWorkspace();
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
        }

        private void ClearSingleWorkspace()
        {
            currentMaterial = null;
            originalMaterial = null;
            workFilePath = string.Empty;

            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Enabled = false;
            layoutGeneral.Enabled = false;
            layoutMaterial.Enabled = false;
            layoutEffect.Enabled = false;

            SuspendAll();
            ControlFactory.ClearControls();
            ResumeAll();

            Text = ApplicationTitle;
            changed = false;
            workspaceMode = WorkspaceMode.Empty;
        }

        private bool TrySaveSingleMaterial(string filePath, bool updateWorkspacePath)
        {
            BaseMaterialFile material = CreateMaterialForCurrentType();
            GetMaterialValues(material);

            if (!MaterialFilePersistence.TrySaveMaterial(filePath, material, serializeToJSONToolStripMenuItem.Checked, out _))
            {
                MessageBox.Show(string.Format("Failed to save file '{0}'!", filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (updateWorkspacePath)
            {
                workFilePath = filePath;
                saveToolStripMenuItem.Enabled = true;
            }

            currentMaterial = material;
            originalMaterial = CloneMaterial(currentMaterial);
            changed = false;
            UpdateWindowTitle();
            return true;
        }

        private BaseMaterialFile CreateMaterialForCurrentType()
        {
            return CurrentMaterialType switch
            {
                MaterialType.Effect => new BGEM(),
                _ => new BGSM(),
            };
        }

        private void OpenMaterial(string fileName)
        {
            ExitBulkMode();

            if (!MaterialFilePersistence.TryLoadMaterial(fileName, out BaseMaterialFile material, out _, out _))
            {
                MessageBox.Show(string.Format("Failed to open file '{0}'!", fileName), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (material.Version > 22)
            {
                MessageBox.Show($"Version {material.Version} not currently supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            workFilePath = fileName;
            workspaceMode = WorkspaceMode.Single;

            SuspendAll();
            CreateMaterialControls(material);

            if (currentMaterial.Version > 2 && currentMaterial.Version <= 22)
                SetGameSelection(Game.FO76);
            else
                SetGameSelection(Game.FO4);

            SetMaterialTypeSelection(material is BGEM ? MaterialType.Effect : MaterialType.Material);

            FillVersionDropdown();
            UpdateTopLevelSectionVisibility();
            generalPageSection?.SetCollapsed(true);
            ResumeAll();

            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
            layoutGeneral.Enabled = true;
            layoutMaterial.Enabled = true;
            layoutEffect.Enabled = true;

            changed = false;
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
        }
    }
}
