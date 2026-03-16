using Material_Editor.Controls;
using Material_Editor.Models;
using Material_Editor.Services;
using MaterialLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class FieldSelectionDialog : ThemeAwareForm
    {
        private const string SelectedColumnName = "__selected";

        private readonly DataGridView fieldGrid;
        private readonly Button selectAllButton;
        private readonly Button selectNoneButton;
        private readonly Button okButton;
        private readonly ComboBox presetComboBox;
        private readonly Button loadPresetButton;
        private readonly Button savePresetButton;
        private readonly Button deletePresetButton;
        private readonly Label descriptionLabel;
        private readonly Label headerLabel;
        private readonly Label descriptionHeaderLabel;
        private readonly string descriptionPlaceholder = "Select a field to see a short explanation of what it controls.";
        private readonly Func<MaterialFieldDescriptor, bool> initialSelectionProvider;
        private readonly Config config;
        private readonly MaterialType? presetMaterialType;

        public FieldSelectionDialog(IReadOnlyList<MaterialFieldDescriptor> descriptors, BaseMaterialFile baseline, BaseMaterialFile current)
            : this(
                descriptors,
                descriptor => descriptor.HasChanged(baseline, current),
                "Overwrite Fields",
                "Choose the fields you want to copy from the current material.",
                null,
                null)
        {
        }

        public FieldSelectionDialog(IReadOnlyList<MaterialFieldDescriptor> descriptors)
            : this(
                descriptors,
                descriptor => false,
                "Choose Bulk Columns",
                "Choose the fields you want to show in the bulk editor table.",
                null,
                null)
        {
        }

        public FieldSelectionDialog(IReadOnlyList<MaterialFieldDescriptor> descriptors, IReadOnlyCollection<string> selectedLabels)
            : this(
                descriptors,
                descriptor => selectedLabels?.Contains(descriptor.Label) == true,
                "Choose Bulk Columns",
                "Choose the fields you want to show in the bulk editor table.",
                null,
                null)
        {
        }

        public FieldSelectionDialog(
            IReadOnlyList<MaterialFieldDescriptor> descriptors,
            IReadOnlyCollection<string> selectedLabels,
            Config config,
            MaterialType materialType)
            : this(
                descriptors,
                descriptor => selectedLabels?.Contains(descriptor.Label) == true,
                "Choose Bulk Columns",
                "Choose the fields you want to show in the bulk editor table.",
                config,
                materialType)
        {
        }

        public IReadOnlyList<MaterialFieldDescriptor> SelectedFields { get; private set; }

        private FieldSelectionDialog(
            IReadOnlyList<MaterialFieldDescriptor> descriptors,
            Func<MaterialFieldDescriptor, bool> initialSelectionProvider,
            string title,
            string headerText,
            Config config,
            MaterialType? presetMaterialType)
        {
            this.initialSelectionProvider = initialSelectionProvider ?? (_ => false);
            this.config = config;
            this.presetMaterialType = presetMaterialType;

            Text = title;
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, config != null && presetMaterialType.HasValue ? 600 : 550);
            MinimumSize = new Size(560, config != null && presetMaterialType.HasValue ? 580 : 520);
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                Padding = new Padding(12)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Controls.Add(mainLayout);

            headerLabel = new Label
            {
                Text = headerText,
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8)
            };
            mainLayout.Controls.Add(headerLabel, 0, 0);

            fieldGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };
            fieldGrid.Columns.Add(ColorToggleDataGridView.CreateColumn(null, string.Empty, name: SelectedColumnName, width: 48, autoSizeNone: true));
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "field",
                HeaderText = "Field",
                FillWeight = 62f,
                ReadOnly = true
            });
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "category",
                HeaderText = "Category",
                FillWeight = 38f,
                ReadOnly = true
            });
            fieldGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (fieldGrid.IsCurrentCellDirty)
                    fieldGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            fieldGrid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == fieldGrid.Columns[SelectedColumnName].Index)
                    UpdateOkState();
            };
            fieldGrid.CellMouseUp += (s, e) =>
            {
                if (ColorToggleDataGridView.TryHandleCellMouseUp(fieldGrid, e))
                    UpdateOkState();
            };
            fieldGrid.SelectionChanged += (s, e) => UpdateDescription();
            fieldGrid.KeyDown += (s, e) =>
            {
                if (ColorToggleDataGridView.TryHandleSpaceKey(fieldGrid, e))
                    UpdateOkState();
            };
            mainLayout.Controls.Add(fieldGrid, 0, 1);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            int nextRow = 2;
            if (config != null && presetMaterialType.HasValue)
            {
                var presetLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 5,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 12)
                };
                presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                var presetLabel = new Label
                {
                    Text = "Field Presets",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 6, 8, 0)
                };
                presetLayout.Controls.Add(presetLabel, 0, 0);

                presetComboBox = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Margin = new Padding(0, 0, 8, 0)
                };
                presetComboBox.SelectedIndexChanged += (s, e) => UpdateOkState();
                presetLayout.Controls.Add(presetComboBox, 1, 0);

                loadPresetButton = CreateCommandButton("Load");
                loadPresetButton.Click += (s, e) => LoadSelectedPreset(descriptors);
                presetLayout.Controls.Add(loadPresetButton, 2, 0);

                savePresetButton = CreateCommandButton("Save Preset...");
                savePresetButton.Click += (s, e) => SaveCurrentPreset(descriptors);
                presetLayout.Controls.Add(savePresetButton, 3, 0);

                deletePresetButton = CreateCommandButton("Delete");
                deletePresetButton.Click += (s, e) => DeleteSelectedPreset();
                presetLayout.Controls.Add(deletePresetButton, 4, 0);

                mainLayout.Controls.Add(presetLayout, 0, nextRow++);
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            descriptionHeaderLabel = new Label
            {
                Text = "Field description",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            mainLayout.Controls.Add(descriptionHeaderLabel, 0, nextRow++);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            descriptionLabel = new Label
            {
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Margin = new Padding(0, 0, 0, 12),
                MinimumSize = new Size(0, 64),
                Padding = new Padding(6),
                Text = descriptionPlaceholder
            };
            mainLayout.Controls.Add(descriptionLabel, 0, nextRow++);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0)
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var selectionButtonsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };

            selectAllButton = CreateCommandButton("Select All");
            selectAllButton.Click += (s, e) => SetAllChecks(true);
            selectionButtonsLayout.Controls.Add(selectAllButton);

            selectNoneButton = CreateCommandButton("Select None");
            selectNoneButton.Click += (s, e) => SetAllChecks(false);
            selectionButtonsLayout.Controls.Add(selectNoneButton);

            footerLayout.Controls.Add(selectionButtonsLayout, 0, 0);

            var acceptButtonsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var cancelButton = CreateCommandButton("Cancel", DialogResult.Cancel);
            acceptButtonsLayout.Controls.Add(cancelButton);

            okButton = CreateCommandButton("OK", DialogResult.OK);
            okButton.Click += (s, e) => SelectFields();
            acceptButtonsLayout.Controls.Add(okButton);

            footerLayout.Controls.Add(acceptButtonsLayout, 1, 0);
            mainLayout.Controls.Add(footerLayout, 0, nextRow);
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Load += (_, _) => UpdateWrappingWidths();
            SizeChanged += (_, _) => UpdateWrappingWidths();

            PopulateGrid(descriptors);
            RefreshPresetList();
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
        }

        private void PopulateGrid(IReadOnlyList<MaterialFieldDescriptor> descriptors)
        {
            foreach (MaterialFieldDescriptor descriptor in descriptors)
            {
                int rowIndex = fieldGrid.Rows.Add(initialSelectionProvider(descriptor), descriptor.Label, descriptor.Category.ToString());
                fieldGrid.Rows[rowIndex].Tag = descriptor;
            }

            if (fieldGrid.Rows.Count > 0)
                fieldGrid.CurrentCell = fieldGrid.Rows[0].Cells["field"];

            UpdateOkState();
            UpdateDescription();
        }

        private void SetAllChecks(bool value)
        {
            foreach (DataGridViewRow row in fieldGrid.Rows)
                row.Cells[SelectedColumnName].Value = value;

            UpdateOkState();
        }

        private void UpdateOkState()
        {
            okButton.Enabled = GetCheckedRows().Count > 0;
            if (deletePresetButton != null)
                deletePresetButton.Enabled = presetComboBox?.SelectedItem is BulkFieldPreset;
        }

        private void UpdateDescription()
        {
            if (fieldGrid.CurrentRow?.Tag is MaterialFieldDescriptor descriptor)
            {
                descriptionLabel.Text = ControlFactory.GetTooltip(descriptor.Label) ?? descriptionPlaceholder;
            }
            else
            {
                descriptionLabel.Text = descriptionPlaceholder;
            }
        }

        private void SelectFields()
        {
            SelectedFields = GetCheckedRows()
                .Select(row => row.Tag as MaterialFieldDescriptor)
                .ToList();
        }

        private void RefreshPresetList()
        {
            if (presetComboBox == null || config == null || !presetMaterialType.HasValue)
                return;

            presetComboBox.Items.Clear();
            foreach (BulkFieldPreset preset in BulkEditorPreferencesService.GetPresets(config, presetMaterialType.Value))
                presetComboBox.Items.Add(preset);

            presetComboBox.DisplayMember = nameof(BulkFieldPreset.Name);
            if (presetComboBox.Items.Count > 0)
                presetComboBox.SelectedIndex = 0;

            UpdateOkState();
        }

        private void LoadSelectedPreset(IReadOnlyList<MaterialFieldDescriptor> descriptors)
        {
            if (presetComboBox?.SelectedItem is not BulkFieldPreset preset)
                return;

            var selectedFields = new HashSet<string>(
                BulkEditorPreferencesService.ResolvePresetFields(descriptors, preset).Select(descriptor => descriptor.Label),
                StringComparer.OrdinalIgnoreCase);

            foreach (DataGridViewRow row in fieldGrid.Rows)
            {
                if (row.Tag is MaterialFieldDescriptor descriptor)
                    row.Cells[SelectedColumnName].Value = selectedFields.Contains(descriptor.Label);
            }

            UpdateOkState();
        }

        private void SaveCurrentPreset(IReadOnlyList<MaterialFieldDescriptor> descriptors)
        {
            if (config == null || !presetMaterialType.HasValue)
                return;

            using var dialog = new TextPromptDialog(
                "Save Field Preset",
                "Enter a preset name for the currently checked bulk fields.",
                presetComboBox?.SelectedItem is BulkFieldPreset preset ? preset.Name : string.Empty);

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            string name = dialog.PromptValue;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "Enter a preset name before saving.", "Preset Name Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BulkEditorPreferencesService.SavePreset(
                config,
                presetMaterialType.Value,
                name,
                GetCheckedRows()
                    .Select(row => row.Tag as MaterialFieldDescriptor)
                    .Where(descriptor => descriptor != null)
                    .ToArray());

            RefreshPresetList();
            SelectPresetByName(name);
        }

        private void DeleteSelectedPreset()
        {
            if (config == null || !presetMaterialType.HasValue || presetComboBox?.SelectedItem is not BulkFieldPreset preset)
                return;

            if (MessageBox.Show(this, $"Delete the preset '{preset.Name}'?", "Delete Preset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            BulkEditorPreferencesService.DeletePreset(config, presetMaterialType.Value, preset.Name);
            RefreshPresetList();
        }

        private void SelectPresetByName(string name)
        {
            if (presetComboBox == null || string.IsNullOrWhiteSpace(name))
                return;

            for (int index = 0; index < presetComboBox.Items.Count; index++)
            {
                if (presetComboBox.Items[index] is BulkFieldPreset preset
                    && string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    presetComboBox.SelectedIndex = index;
                    break;
                }
            }
        }

        private void UpdateWrappingWidths()
        {
            headerLabel.MaximumSize = new Size(Math.Max(ClientSize.Width - 24, 280), 0);
        }

        private List<DataGridViewRow> GetCheckedRows()
        {
            return fieldGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(row => row.Cells[SelectedColumnName].Value is bool isChecked && isChecked)
                .ToList();
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
