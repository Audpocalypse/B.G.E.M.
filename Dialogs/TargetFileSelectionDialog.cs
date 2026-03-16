using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class TargetFileSelectionDialog : ThemeAwareForm
    {
        private readonly ListView targetList;
        private readonly Button addFilesButton;
        private readonly Button addFolderButton;
        private readonly Button removeButton;
        private readonly Button okButton;
        private readonly ColorToggleCheckBox backupCheckBox;
        private readonly ColorToggleCheckBox advancedModeCheckBox;
        private readonly HashSet<string> filePaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly Label introLabel;
        private readonly Button cancelButton;
        private readonly bool allowAdvancedMode;
        private readonly bool allowMaterialTypeSelection;
        private readonly ComboBox materialTypeComboBox;
        private readonly Label materialTypeLabel;
        private MaterialType materialType;

        public TargetFileSelectionDialog(MaterialType materialType)
            : this(materialType, allowMaterialTypeSelection: false, allowAdvancedMode: true)
        {
        }

        public TargetFileSelectionDialog()
            : this(MaterialType.Material, allowMaterialTypeSelection: true, allowAdvancedMode: false)
        {
        }

        public IReadOnlyList<string> TargetFiles => targetList.Items
            .Cast<ListViewItem>()
            .Select(item => item.Tag as string)
            .ToList();

        public bool BackupBeforeWrite => backupCheckBox.Checked;
        public bool UseAdvancedMode => allowAdvancedMode && advancedModeCheckBox.Checked;
        public MaterialType SelectedMaterialType => allowMaterialTypeSelection && materialTypeComboBox.SelectedItem is MaterialType selectedType
            ? selectedType
            : materialType;

        private TargetFileSelectionDialog(MaterialType materialType, bool allowMaterialTypeSelection, bool allowAdvancedMode)
        {
            this.materialType = materialType;
            this.allowMaterialTypeSelection = allowMaterialTypeSelection;
            this.allowAdvancedMode = allowAdvancedMode;

            Text = "Select Target Files";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(680, 620);
            MinimumSize = new Size(620, 560);
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                Padding = new Padding(12)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Controls.Add(mainLayout);

            int rowIndex = 0;
            if (allowMaterialTypeSelection)
            {
                var materialLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 8)
                };
                materialLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                materialLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

                materialTypeLabel = new Label
                {
                    Text = "Material type:",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 6, 8, 0),
                    Visible = true
                };
                materialLayout.Controls.Add(materialTypeLabel, 0, 0);

                materialTypeComboBox = new ComboBox
                {
                    Dock = DockStyle.Left,
                    Width = 220,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Visible = true
                };
                materialTypeComboBox.Items.Add(MaterialType.Material);
                materialTypeComboBox.Items.Add(MaterialType.Effect);
                materialTypeComboBox.SelectedItem = materialType;
                materialTypeComboBox.SelectedIndexChanged += (s, e) =>
                {
                    if (materialTypeComboBox.SelectedItem is MaterialType selectedType)
                    {
                        this.materialType = selectedType;
                        UpdateIntroText();
                    }
                };
                materialLayout.Controls.Add(materialTypeComboBox, 1, 0);

                mainLayout.Controls.Add(materialLayout, 0, rowIndex++);
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            else
            {
                materialTypeLabel = null;
                materialTypeComboBox = null;
            }

            introLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8)
            };
            mainLayout.Controls.Add(introLabel, 0, rowIndex++);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            targetList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Margin = new Padding(0, 0, 0, 12)
            };
            targetList.Columns.Add("Target File", 560);
            targetList.SelectedIndexChanged += (s, e) => UpdateRemoveButton();
            targetList.SizeChanged += (s, e) => ResizeColumns();
            mainLayout.Controls.Add(targetList, 0, rowIndex++);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var actionLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            addFilesButton = CreateCommandButton("Add Files...");
            addFilesButton.Click += (s, e) => AddFiles();
            actionLayout.Controls.Add(addFilesButton);

            addFolderButton = CreateCommandButton("Add Folder...");
            addFolderButton.Click += (s, e) => AddFolder();
            actionLayout.Controls.Add(addFolderButton);

            removeButton = CreateCommandButton("Remove");
            removeButton.Enabled = false;
            removeButton.Click += (s, e) => RemoveSelected();
            actionLayout.Controls.Add(removeButton);

            mainLayout.Controls.Add(actionLayout, 0, rowIndex++);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var optionsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 12)
            };

            backupCheckBox = new ColorToggleCheckBox
            {
                Text = "Create .bak backup for each overwritten file",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            optionsLayout.Controls.Add(backupCheckBox);

            advancedModeCheckBox = new ColorToggleCheckBox
            {
                Text = "Advanced Iterative Mode",
                AutoSize = true,
                Checked = false,
                Visible = allowAdvancedMode,
                Margin = new Padding(0)
            };
            optionsLayout.Controls.Add(advancedModeCheckBox);
            mainLayout.Controls.Add(optionsLayout, 0, rowIndex++);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var footerLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };

            cancelButton = CreateCommandButton("Cancel", DialogResult.Cancel);
            footerLayout.Controls.Add(cancelButton);

            okButton = CreateCommandButton("OK", DialogResult.OK);
            okButton.Enabled = false;
            okButton.Click += (s, e) => UpdateOkState();
            footerLayout.Controls.Add(okButton);
            mainLayout.Controls.Add(footerLayout, 0, rowIndex);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Load += (_, _) => UpdateWrappingWidths();
            SizeChanged += (_, _) => UpdateWrappingWidths();

            UpdateIntroText();
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
        }

        private void AddFiles()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = MaterialFileTypeHelper.GetFileDialogFilter(SelectedMaterialType),
                Multiselect = true,
                Title = "Select material files..."
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            AddPaths(dialog.FileNames);
        }

        private void AddFolder()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select a folder to scan for materials..."
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                IEnumerable<string> matches = Directory.EnumerateFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories)
                    .Where(IsAllowedFileType);

                AddPaths(matches);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to scan the selected folder for material files.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AddPaths(IEnumerable<string> paths)
        {
            int skippedByType = 0;

            foreach (string path in paths)
            {
                try
                {
                    string fullPath = Path.GetFullPath(path);
                    if (!IsAllowedFileType(fullPath))
                    {
                        skippedByType++;
                        continue;
                    }

                    if (filePaths.Add(fullPath))
                    {
                        var item = new ListViewItem(fullPath) { Tag = fullPath };
                        targetList.Items.Add(item);
                    }
                }
                catch (Exception)
                {
                    // Ignore invalid paths.
                }
            }

            ResizeColumns();
            UpdateOkState();

            if (skippedByType > 0)
            {
                string extension = MaterialFileTypeHelper.GetExpectedExtension(SelectedMaterialType);
                MessageBox.Show(
                    this,
                    $"Ignored {skippedByType} file(s) that do not match the current material type ({extension}).",
                    "Ignored Files",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void RemoveSelected()
        {
            foreach (ListViewItem item in targetList.SelectedItems)
            {
                filePaths.Remove(item.Tag as string);
                targetList.Items.Remove(item);
            }

            ResizeColumns();
            UpdateOkState();
        }

        private void UpdateRemoveButton()
        {
            removeButton.Enabled = targetList.SelectedItems.Count > 0;
        }

        private void UpdateOkState()
        {
            okButton.Enabled = targetList.Items.Count > 0;
        }

        private bool IsAllowedFileType(string filePath)
        {
            return filePath.EndsWith(MaterialFileTypeHelper.GetExpectedExtension(SelectedMaterialType), StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateIntroText()
        {
            string extension = MaterialFileTypeHelper.GetExpectedExtension(SelectedMaterialType);
            string materialLabel = MaterialFileTypeHelper.GetDisplayName(SelectedMaterialType);
            introLabel.Text = $"Add {materialLabel} files ({extension}) or folders to apply the selected fields to.";
        }

        private void ResizeColumns()
        {
            if (targetList.Columns.Count == 0 || targetList.ClientSize.Width <= 0)
                return;

            targetList.Columns[0].Width = Math.Max(targetList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4, 320);
        }

        private void UpdateWrappingWidths()
        {
            introLabel.MaximumSize = new Size(Math.Max(ClientSize.Width - 24, 320), 0);
        }

        private static Button CreateCommandButton(string text, DialogResult dialogResult = DialogResult.None)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = dialogResult,
                Margin = new Padding(0, 0, 8, 0)
            };
        }
    }
}
