using Material_Editor.AdvancedVariant;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class VariationGeneratorDialog : ThemeAwareForm
    {
        private static readonly HashSet<string> RowsWithoutDefaultApplySeed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Environment",
            "Glow",
            "Inner Layer",
            "Wrinkles",
            "Displacement",
            "Root Material Path"
        };

        private readonly BindingList<FieldRow> fieldRows;
        private readonly BindingList<AdvancedRuleRow> advancedRuleRows;
        private readonly Panel scrollHost;
        private readonly TableLayoutPanel mainLayout;
        private readonly Panel fieldGridHost;
        private readonly DataGridView fieldGrid;
        private readonly DataGridView advancedGrid;
        private readonly DataGridViewComboBoxColumn[] advancedLayerColumns;
        private readonly NumericUpDown startIndexControl;
        private readonly NumericUpDown countControl;
        private readonly NumericUpDown stepControl;
        private readonly NumericUpDown[] advancedLayerCountControls;
        private readonly TextBox outputPatternTextBox;
        private readonly TextBox outputPreviewTextBox;
        private readonly TextBox fieldPreviewTextBox;
        private readonly Label headerLabel;
        private readonly Label introLabel;
        private readonly Label outputHelpLabel;
        private readonly Label indexHelpLabel;
        private readonly Label fieldHelpLabel;
        private readonly Label advancedHelpLabel;
        private readonly Label advancedWarningLabel;
        private readonly Label summaryLabel;
        private readonly Label validationLabel;
        private readonly Button okButton;
        private readonly Button addRuleButton;
        private readonly Button deleteRuleButton;
        private readonly Button moveRuleUpButton;
        private readonly Button moveRuleDownButton;
        private readonly ColorToggleCheckBox greyscaleModeCheckBox;
        private readonly NumericUpDown greyscaleStartControl;
        private readonly NumericUpDown greyscaleStepControl;
        private readonly Label greyscaleNoteLabel;
        private readonly ColorToggleCheckBox advancedModeCheckBox;
        private readonly GroupBox legacyRangeGroup;
        private readonly GroupBox advancedGroup;
        private readonly ToolTip advancedWarningToolTip;

        private bool suppressAdvancedRowEvents;
        private IReadOnlyList<AdvancedVariantResolvedContext> advancedResolvedContexts = Array.Empty<AdvancedVariantResolvedContext>();

        public VariationGeneratorDialog(
            IReadOnlyList<MaterialFieldDescriptor> descriptors,
            IReadOnlyList<string> currentValues,
            string suggestedOutputPattern)
        {
            if (descriptors == null)
                throw new ArgumentNullException(nameof(descriptors));

            Text = "Generate Variations";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1120, 980);
            MinimumSize = new Size(1020, 860);
            MaximizeBox = true;
            MinimizeBox = true;
            ShowInTaskbar = false;
            SuspendLayout();

            scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Margin = new Padding(0)
            };
            scrollHost.Resize += HandleLayoutChanged;
            Controls.Add(scrollHost);

            mainLayout = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 20,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12),
                Margin = new Padding(0)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int index = 0; index < mainLayout.RowCount; index++)
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            scrollHost.Controls.Add(mainLayout);

            headerLabel = DialogLayoutSupport.CreateWrappingLabel("Generate multiple material files from the open template.", new Padding(0, 0, 0, 6), bold: true);
            mainLayout.Controls.Add(headerLabel, 0, 0);

            introLabel = DialogLayoutSupport.CreateWrappingLabel(
                "Follow the steps below: name the outputs, choose simple or advanced indexing, then enable any string or path fields you want to rewrite.",
                new Padding(0, 0, 0, 12));
            mainLayout.Controls.Add(introLabel, 0, 1);

            var outputStepLabel = DialogLayoutSupport.CreateWrappingLabel("1. Output Naming", new Padding(0, 0, 0, 4), bold: true);
            mainLayout.Controls.Add(outputStepLabel, 0, 2);

            outputHelpLabel = DialogLayoutSupport.CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 6));
            mainLayout.Controls.Add(outputHelpLabel, 0, 3);

            var outputPatternLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            outputPatternLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            outputPatternLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            outputPatternTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Text = suggestedOutputPattern ?? string.Empty
            };
            outputPatternTextBox.TextChanged += HandleConfigurationChanged;
            outputPatternLayout.Controls.Add(outputPatternTextBox, 0, 0);

            var browseButton = new Button
            {
                Text = "Browse...",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            browseButton.Click += BrowseForFolder;
            outputPatternLayout.Controls.Add(browseButton, 1, 0);
            mainLayout.Controls.Add(outputPatternLayout, 0, 4);

            mainLayout.Controls.Add(DialogLayoutSupport.CreateWrappingLabel("Output Preview", new Padding(0, 0, 0, 4)), 0, 5);

            outputPreviewTextBox = DialogLayoutSupport.CreatePreviewTextBox(64, new Padding(0, 0, 0, 12));
            mainLayout.Controls.Add(outputPreviewTextBox, 0, 6);

            var indexingStepLabel = DialogLayoutSupport.CreateWrappingLabel("2. Indexing", new Padding(0, 0, 0, 4), bold: true);
            mainLayout.Controls.Add(indexingStepLabel, 0, 7);

            advancedModeCheckBox = new ColorToggleCheckBox
            {
                Text = "Use Advanced Mode",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            advancedModeCheckBox.CheckedChanged += AdvancedModeCheckBox_CheckedChanged;
            mainLayout.Controls.Add(advancedModeCheckBox, 0, 8);

            indexHelpLabel = DialogLayoutSupport.CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 6));
            mainLayout.Controls.Add(indexHelpLabel, 0, 9);

            legacyRangeGroup = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 8)
            };
            var rangeLayout = new TableLayoutPanel
            {
                ColumnCount = 7,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0)
            };
            for (int index = 0; index < 6; index++)
                rangeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rangeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            rangeLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Start index:", new Padding(0, 4, 8, 0)), 0, 0);
            startIndexControl = DialogLayoutSupport.CreateNumericInput(0, 9999, 1, 104, new Padding(0, 0, 16, 0));
            startIndexControl.ValueChanged += HandleConfigurationChanged;
            rangeLayout.Controls.Add(startIndexControl, 1, 0);

            rangeLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Count:", new Padding(0, 4, 8, 0)), 2, 0);
            countControl = DialogLayoutSupport.CreateNumericInput(1, 999, 3, 104, new Padding(0, 0, 16, 0));
            countControl.ValueChanged += HandleConfigurationChanged;
            rangeLayout.Controls.Add(countControl, 3, 0);

            rangeLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Step:", new Padding(0, 4, 8, 0)), 4, 0);
            stepControl = DialogLayoutSupport.CreateNumericInput(1, 999, 1, 104, new Padding(0, 0, 16, 0));
            stepControl.ValueChanged += HandleConfigurationChanged;
            rangeLayout.Controls.Add(stepControl, 5, 0);

            legacyRangeGroup.Controls.Add(rangeLayout);
            mainLayout.Controls.Add(legacyRangeGroup, 0, 10);

            advancedGroup = new GroupBox
            {
                Text = "Advanced Variant",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 12),
                Visible = false
            };
            var advancedLayout = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0)
            };
            advancedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            (FlowLayoutPanel countsLayout, advancedLayerCountControls) = AdvancedVariantDialogSupport.CreateLayerCountLayout(
                layerCount: 4,
                defaultValueFactory: index => index == 0 ? 3m : 0m,
                valueChangedHandler: HandleAdvancedDefinitionChanged,
                layoutMargin: new Padding(0, 0, 0, 8),
                itemMargin: new Padding(0, 0, 16, 6),
                labelMargin: new Padding(0, 4, 8, 0),
                numericWidth: 104,
                numericMargin: new Padding(0, 0, 16, 0));
            advancedLayout.Controls.Add(countsLayout, 0, 0);

            advancedHelpLabel = DialogLayoutSupport.CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 6));
            advancedLayout.Controls.Add(advancedHelpLabel, 0, 1);

            advancedWarningLabel = DialogLayoutSupport.CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 8));
            advancedLayout.Controls.Add(advancedWarningLabel, 0, 2);

            advancedLayout.Controls.Add(DialogLayoutSupport.CreateWrappingLabel("Naming Rules", new Padding(0, 0, 0, 4)), 0, 3);

            var ruleButtonsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 6)
            };

            addRuleButton = DialogLayoutSupport.CreateCommandButton("Add Rule", new Padding(0, 0, 8, 0));
            addRuleButton.Click += AddRuleButton_Click;
            ruleButtonsLayout.Controls.Add(addRuleButton);

            deleteRuleButton = DialogLayoutSupport.CreateCommandButton("Delete Rule", new Padding(0, 0, 8, 0));
            deleteRuleButton.Click += DeleteRuleButton_Click;
            ruleButtonsLayout.Controls.Add(deleteRuleButton);

            moveRuleUpButton = DialogLayoutSupport.CreateCommandButton("Move Up", new Padding(0, 0, 8, 0));
            moveRuleUpButton.Click += MoveRuleUpButton_Click;
            ruleButtonsLayout.Controls.Add(moveRuleUpButton);

            moveRuleDownButton = DialogLayoutSupport.CreateCommandButton("Move Down", new Padding(0));
            moveRuleDownButton.Click += MoveRuleDownButton_Click;
            ruleButtonsLayout.Controls.Add(moveRuleDownButton);

            advancedLayout.Controls.Add(ruleButtonsLayout, 0, 4);

            advancedRuleRows = new BindingList<AdvancedRuleRow>();
            advancedGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ShowCellToolTips = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            advancedLayerColumns = AdvancedVariantDialogSupport.AddRuleColumns(advancedGrid, layerCount: 4, layerFillWeight: 10f, indexTokenFillWeight: 18f);
            advancedGrid.CurrentCellDirtyStateChanged += AdvancedGrid_CurrentCellDirtyStateChanged;
            advancedGrid.CellBeginEdit += AdvancedGrid_CellBeginEdit;
            advancedGrid.CellEndEdit += AdvancedGrid_CellEndEdit;
            advancedGrid.CellValueChanged += AdvancedGrid_CellValueChanged;
            advancedGrid.CellToolTipTextNeeded += AdvancedGrid_CellToolTipTextNeeded;
            advancedGrid.DataError += AdvancedGrid_DataError;
            advancedGrid.SelectionChanged += HandleConfigurationChanged;
            advancedGrid.DataSource = advancedRuleRows;

            var advancedGridHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 210,
                Margin = new Padding(0)
            };
            advancedGridHost.Controls.Add(advancedGrid);
            advancedLayout.Controls.Add(advancedGridHost, 0, 5);

            advancedWarningToolTip = new ToolTip
            {
                ShowAlways = true,
                AutoPopDelay = 30000,
                InitialDelay = 200,
                ReshowDelay = 100
            };
            advancedGroup.Controls.Add(advancedLayout);
            mainLayout.Controls.Add(advancedGroup, 0, 11);

            var greyscaleGroup = new GroupBox
            {
                Text = "Greyscale to Palette Scale (optional)",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 12)
            };
            var greyscaleLayout = new TableLayoutPanel
            {
                ColumnCount = 5,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0)
            };
            for (int index = 0; index < 4; index++)
                greyscaleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            greyscaleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            greyscaleModeCheckBox = new ColorToggleCheckBox
            {
                Text = "Vary the grayscale-to-palette scale per file.",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            greyscaleModeCheckBox.CheckedChanged += HandleConfigurationChanged;
            greyscaleLayout.Controls.Add(greyscaleModeCheckBox, 0, 0);
            greyscaleLayout.SetColumnSpan(greyscaleModeCheckBox, 5);

            greyscaleLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Start value:", new Padding(0, 4, 8, 0)), 0, 1);
            greyscaleStartControl = DialogLayoutSupport.CreateNumericInput(-1000, 1000, 1, 104, new Padding(0, 0, 16, 0), 3, 0.1m);
            greyscaleStartControl.ValueChanged += HandleConfigurationChanged;
            greyscaleLayout.Controls.Add(greyscaleStartControl, 1, 1);

            greyscaleLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Step:", new Padding(0, 4, 8, 0)), 2, 1);
            greyscaleStepControl = DialogLayoutSupport.CreateNumericInput(-1000, 1000, 0, 104, new Padding(0, 0, 16, 0), 3, 0.1m);
            greyscaleStepControl.ValueChanged += HandleConfigurationChanged;
            greyscaleLayout.Controls.Add(greyscaleStepControl, 3, 1);

            greyscaleNoteLabel = DialogLayoutSupport.CreateWrappingLabel(
                "Each generated file will use start + step * file index offset when enabled.",
                new Padding(0, 8, 0, 0));
            greyscaleLayout.Controls.Add(greyscaleNoteLabel, 0, 2);
            greyscaleLayout.SetColumnSpan(greyscaleNoteLabel, 5);
            greyscaleGroup.Controls.Add(greyscaleLayout);
            mainLayout.Controls.Add(greyscaleGroup, 0, 12);

            var fieldStepLabel = DialogLayoutSupport.CreateWrappingLabel("3. Fields To Vary", new Padding(0, 0, 0, 4), bold: true);
            mainLayout.Controls.Add(fieldStepLabel, 0, 13);

            fieldHelpLabel = DialogLayoutSupport.CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 8));
            mainLayout.Controls.Add(fieldHelpLabel, 0, 14);

            fieldRows = new BindingList<FieldRow>(CreateFieldRows(descriptors, currentValues).ToList());
            foreach (var row in fieldRows)
                row.PropertyChanged += FieldRow_PropertyChanged;

            fieldGrid = new DataGridView
            {
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ShowCellToolTips = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ScrollBars = ScrollBars.None
            };
            fieldGrid.Columns.Add(ColorToggleDataGridView.CreateColumn(
                nameof(FieldRow.Enabled),
                "Apply",
                width: 60,
                autoSizeNone: true));
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FieldRow.Label),
                HeaderText = "Texture slot",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 22f,
                ReadOnly = true
            });
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FieldRow.CurrentValue),
                HeaderText = "Template value",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 33f,
                ReadOnly = true
            });
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FieldRow.Pattern),
                HeaderText = "Replacement pattern",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 45f
            });
            fieldGrid.CurrentCellDirtyStateChanged += FieldGrid_CurrentCellDirtyStateChanged;
            fieldGrid.CellValueChanged += FieldGrid_CellValueChanged;
            fieldGrid.CellBeginEdit += FieldGrid_CellBeginEdit;
            fieldGrid.CellEndEdit += FieldGrid_CellEndEdit;
            fieldGrid.CellToolTipTextNeeded += FieldGrid_CellToolTipTextNeeded;
            fieldGrid.SelectionChanged += HandleConfigurationChanged;
            fieldGrid.DataBindingComplete += FieldGrid_DataBindingComplete;
            fieldGrid.DataSource = fieldRows;

            fieldGridHost = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 8)
            };
            fieldGridHost.Controls.Add(fieldGrid);
            mainLayout.Controls.Add(fieldGridHost, 0, 15);

            mainLayout.Controls.Add(DialogLayoutSupport.CreateWrappingLabel("Field Preview", new Padding(0, 0, 0, 4)), 0, 16);

            fieldPreviewTextBox = DialogLayoutSupport.CreatePreviewTextBox(52, new Padding(0, 0, 0, 8));
            mainLayout.Controls.Add(fieldPreviewTextBox, 0, 17);

            var previewStepLabel = DialogLayoutSupport.CreateWrappingLabel("4. Preview / Generate", new Padding(0, 0, 0, 4), bold: true);
            mainLayout.Controls.Add(previewStepLabel, 0, 18);

            var footerLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0)
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var footerMessageLayout = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0)
            };
            footerMessageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            summaryLabel = DialogLayoutSupport.CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 4));
            footerMessageLayout.Controls.Add(summaryLabel, 0, 0);

            validationLabel = DialogLayoutSupport.CreateWrappingLabel(string.Empty, new Padding(0));
            footerMessageLayout.Controls.Add(validationLabel, 0, 1);

            footerLayout.Controls.Add(footerMessageLayout, 0, 0);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Dock = DockStyle.Top,
                Margin = new Padding(12, 0, 0, 0)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(8, 0, 0, 0)
            };
            buttonPanel.Controls.Add(cancelButton);

            okButton = new Button
            {
                Text = "Generate",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.None,
                Margin = new Padding(8, 0, 0, 0)
            };
            okButton.Click += OkButton_Click;
            buttonPanel.Controls.Add(okButton);

            footerLayout.Controls.Add(buttonPanel, 1, 0);
            mainLayout.Controls.Add(footerLayout, 0, 19);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            if (fieldGrid.Rows.Count > 0)
            {
                fieldGrid.Rows[0].Selected = true;
                fieldGrid.CurrentCell = fieldGrid.Rows[0].Cells[1];
            }

            SynchronizeAdvancedRows();
            SelectAdvancedRuleRow(0);

            Load += HandleLayoutChanged;
            SizeChanged += HandleLayoutChanged;
            UpdateModeTexts();
            UpdateWrappingLabelWidths();
            RefreshUiState();
            ResumeLayout(true);
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
            RefreshValidationColors();
        }

        private void HandleLayoutChanged(object sender, EventArgs e)
        {
            DialogLayoutSupport.UpdateMainLayoutWidth(scrollHost, mainLayout, 820);
            UpdateFieldGridHeight();
            UpdateWrappingLabelWidths();
        }

        private void UpdateFieldGridHeight()
        {
            if (fieldGrid == null || fieldGridHost == null)
                return;

            int headerHeight = fieldGrid.ColumnHeadersVisible ? fieldGrid.ColumnHeadersHeight : 0;
            int rowsHeight = fieldGrid.Rows.GetRowsHeight(DataGridViewElementStates.Visible);
            int desiredHeight = Math.Max(80, headerHeight + rowsHeight + 2);

            if (fieldGrid.Height != desiredHeight)
                fieldGrid.Height = desiredHeight;
        }

        private void UpdateWrappingLabelWidths()
        {
            DialogLayoutSupport.SetWrappingWidths(
                100,
                headerLabel,
                introLabel,
                outputHelpLabel,
                indexHelpLabel,
                advancedHelpLabel,
                advancedWarningLabel,
                greyscaleNoteLabel,
                fieldHelpLabel,
                summaryLabel,
                validationLabel);
        }

        public MaterialVariationOptions Options { get; private set; }

        private bool IsAdvancedMode => advancedModeCheckBox.Checked;

        private static IEnumerable<FieldRow> CreateFieldRows(IReadOnlyList<MaterialFieldDescriptor> descriptors, IReadOnlyList<string> currentValues)
        {
            for (int index = 0; index < descriptors.Count; index++)
            {
                var descriptor = descriptors[index];
                var value = currentValues != null && index < currentValues.Count ? currentValues[index] : string.Empty;
                yield return new FieldRow(descriptor, value);
            }
        }

        private void BrowseForFolder(object sender, EventArgs e)
        {
            string currentPattern = outputPatternTextBox.Text?.Trim() ?? string.Empty;
            string defaultFileName = IsAdvancedMode ? "variation_{indexNN}.bgsm" : "variation_{index}.bgsm";
            string initialDirectory = string.Empty;
            string fileName = defaultFileName;

            if (!string.IsNullOrWhiteSpace(currentPattern))
            {
                try
                {
                    string fullPath = Path.GetFullPath(currentPattern);
                    string directory = Path.GetDirectoryName(fullPath);
                    string currentFileName = Path.GetFileName(fullPath);

                    if (!string.IsNullOrWhiteSpace(directory))
                        initialDirectory = directory;

                    if (!string.IsNullOrWhiteSpace(currentFileName))
                        fileName = currentFileName;
                }
                catch
                {
                    fileName = Path.GetFileName(currentPattern);
                    if (string.IsNullOrWhiteSpace(fileName))
                        fileName = defaultFileName;
                }
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Choose an output file pattern",
                Filter = "BGSM files (*.bgsm)|*.bgsm|All files (*.*)|*.*",
                CheckFileExists = false,
                OverwritePrompt = false,
                AddExtension = false,
                FileName = fileName
            };

            if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
                dialog.InitialDirectory = initialDirectory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            outputPatternTextBox.Text = dialog.FileName;
        }

        private void AdvancedModeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (IsAdvancedMode)
            {
                EnsureAdvancedLayerDefaults();
                NormalizeOutputPatternForAdvancedMode();
            }

            UpdateModeTexts();
            SynchronizeAdvancedRows();
            RefreshUiState();
        }

        private void HandleAdvancedDefinitionChanged(object sender, EventArgs e)
        {
            SynchronizeAdvancedRows();
            RefreshUiState();
        }

        private void FieldGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (fieldGrid.IsCurrentCellDirty && fieldGrid.CurrentCell?.ColumnIndex == 0)
                fieldGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void FieldGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                var row = fieldGrid.Rows[e.RowIndex].DataBoundItem as FieldRow;
                if (row?.Enabled == true && ShouldSeedPatternFromOutputNaming(row))
                {
                    row.Pattern = BuildReplacementPatternFromOutputNaming(row.CurrentValue, out string warningMessage);
                    if (!string.IsNullOrEmpty(warningMessage))
                    {
                        MessageBox.Show(
                            this,
                            warningMessage,
                            "Path Warning",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }

            RefreshUiState();
        }

        private void FieldGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 3)
                return;

            fieldGrid.SelectionChanged -= HandleConfigurationChanged;
        }

        private void FieldGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            RefreshUiState();
            fieldGrid.SelectionChanged += HandleConfigurationChanged;
        }

        private void FieldGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (fieldGrid.Rows[e.RowIndex].DataBoundItem is not FieldRow row)
                return;

            e.ToolTipText = e.ColumnIndex switch
            {
                2 => row.CurrentValue,
                3 => row.Pattern,
                _ => string.Empty
            };
        }

        private void FieldGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            UpdateFieldGridHeight();
        }

        private void AdvancedGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            AdvancedVariantDialogSupport.CommitComboBoxEditIfDirty(advancedGrid);
        }

        private void AdvancedGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != advancedLayerColumns.Length)
                return;

            advancedGrid.SelectionChanged -= HandleConfigurationChanged;
        }

        private void AdvancedGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            RefreshUiState();
            advancedGrid.SelectionChanged += HandleConfigurationChanged;
        }

        private void AdvancedGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (suppressAdvancedRowEvents || e.RowIndex < 0)
                return;

            RefreshUiState();
        }

        private void AddRuleButton_Click(object sender, EventArgs e)
        {
            AddAdvancedRuleRow();
            RefreshUiState();
        }

        private void DeleteRuleButton_Click(object sender, EventArgs e)
        {
            int selectedIndex = GetSelectedAdvancedRuleIndex();
            if (selectedIndex < 0 || selectedIndex >= advancedRuleRows.Count)
                return;

            suppressAdvancedRowEvents = true;
            try
            {
                selectedIndex = AdvancedVariantDialogSupport.DeleteSelectedRule(
                    advancedRuleRows,
                    selectedIndex,
                    AdvancedRuleRow_PropertyChanged,
                    ensureAtLeastOneRule: true);
            }
            finally
            {
                suppressAdvancedRowEvents = false;
            }

            SelectAdvancedRuleRow(selectedIndex);
            RefreshUiState();
        }

        private void MoveRuleUpButton_Click(object sender, EventArgs e)
        {
            MoveAdvancedRule(-1);
        }

        private void MoveRuleDownButton_Click(object sender, EventArgs e)
        {
            MoveAdvancedRule(1);
        }

        private void AdvancedGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (advancedGrid.Rows[e.RowIndex].DataBoundItem is not AdvancedRuleRow row)
                return;

            e.ToolTipText = AdvancedVariantDialogSupport.GetRuleCellTooltipText(row, e.ColumnIndex, advancedLayerCountControls, row.IndexTok);
        }

        private void AdvancedGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            AdvancedVariantDialogSupport.SuppressGridDataError(e);
        }

        private void FieldRow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is FieldRow row && e.PropertyName == nameof(FieldRow.Pattern) && !row.Enabled && !string.IsNullOrWhiteSpace(row.Pattern))
            {
                row.Enabled = true;
                return;
            }

            if (e.PropertyName == nameof(FieldRow.Pattern) && fieldGrid.IsCurrentCellInEditMode)
                return;

            RefreshUiState();
        }

        private void AdvancedRuleRow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (suppressAdvancedRowEvents)
                return;

            if (advancedGrid.IsCurrentCellInEditMode)
                return;

            RefreshUiState();
        }

        private void HandleConfigurationChanged(object sender, EventArgs e)
        {
            RefreshUiState();
        }

        private void RefreshUiState()
        {
            bool greyscaleEnabled = greyscaleModeCheckBox.Checked;
            greyscaleStartControl.Enabled = greyscaleEnabled;
            greyscaleStepControl.Enabled = greyscaleEnabled;

            UpdateModeTexts();
            UpdateAdvancedRuleButtonState();

            var options = CreateOptions();
            advancedResolvedContexts = ResolveAdvancedContexts(options);
            UpdateAdvancedWarning(advancedResolvedContexts);
            string message = ValidateOptions(options, out var hasWarnings);

            outputPreviewTextBox.Text = BuildOutputPreview(options);
            fieldPreviewTextBox.Text = BuildFieldPreview();
            summaryLabel.Text = BuildSummaryText(options);
            validationLabel.Text = message;
            validationLabel.ForeColor = hasWarnings ? ActiveTheme.Semantics.Warning : ActiveTheme.Semantics.Error;
            okButton.Enabled = string.IsNullOrEmpty(message) || hasWarnings;
        }

        private void UpdateModeTexts()
        {
            legacyRangeGroup.Visible = !IsAdvancedMode;
            advancedGroup.Visible = IsAdvancedMode;

            outputHelpLabel.Text = IsAdvancedMode
                ? "Use {index}, {indexNN}, {indexTok}, {indexLayer1}, {indexNNLayer1}, or {indexTokLayer1}. Example: armor_{indexTokLayer1}_{indexTokLayer2}.bgsm"
                : "Use {index} or {index:00}. Example: armor_{index:00}.bgsm";

            indexHelpLabel.Text = IsAdvancedMode
                ? "Enter counts for up to 4 layers. Each non-zero layer generates indices 1 through its count."
                : "Example: start 1, count 12, step 1 generates 1 through 12.";

            advancedHelpLabel.Text = "Add rules from broad to specific. Blank layer cells are wildcards, and later rows win when specificity is equal.";

            fieldHelpLabel.Text = IsAdvancedMode
                ? "Checked rows replace the template value. Patterns can use {index}, {indexNN}, {indexTok}, {indexLayer1}, {indexNNLayer1}, and {indexTokLayer1}."
                : "Checked rows replace the template value. Unchecked rows keep the original texture path unchanged. Example: textures\\armor_{index:00}.dds";
        }

        private void RefreshValidationColors()
        {
            advancedWarningLabel.ForeColor = ActiveTheme.Semantics.Warning;
            validationLabel.ForeColor = string.IsNullOrEmpty(validationLabel.Text)
                ? ActiveTheme.Palette.Foreground
                : okButton.Enabled
                    ? ActiveTheme.Semantics.Warning
                    : ActiveTheme.Semantics.Error;
        }

        private string BuildReplacementPatternFromOutputNaming(string templateValue, out string warningMessage)
        {
            warningMessage = string.Empty;
            var outputBase = outputPatternTextBox.Text ?? string.Empty;
            outputBase = ContentPathHelper.NormalizeSeparators(outputBase);

            if (ContentPathHelper.TryGetRelativePath(outputBase, "materials", out string relativeOutputBase))
            {
                outputBase = relativeOutputBase;
            }
            else
            {
                warningMessage = "The output path pattern does not contain 'Materials\\'. This may not be a valid material-relative path, but you can continue.";
            }

            if (outputBase.EndsWith(".bgsm", StringComparison.OrdinalIgnoreCase) ||
                outputBase.EndsWith(".bgem", StringComparison.OrdinalIgnoreCase))
            {
                outputBase = outputBase.Substring(0, outputBase.Length - 5);
            }

            var normalizedTemplate = ContentPathHelper.NormalizeSeparators(templateValue);
            int filenameIndex = normalizedTemplate.LastIndexOf('/');
            string templateFilename = filenameIndex >= 0
                ? normalizedTemplate.Substring(filenameIndex + 1)
                : normalizedTemplate;

            string templateSuffix = string.Empty;
            int underscoreIndex = templateFilename.LastIndexOf('_');
            if (underscoreIndex >= 0)
            {
                templateSuffix = templateFilename.Substring(underscoreIndex);
            }
            else
            {
                int templateExtensionIndex = templateFilename.LastIndexOf('.');
                if (templateExtensionIndex >= 0)
                    templateSuffix = templateFilename.Substring(templateExtensionIndex);
            }

            return outputBase + templateSuffix;
        }

        private static bool ShouldSeedPatternFromOutputNaming(FieldRow row)
        {
            return row != null && !RowsWithoutDefaultApplySeed.Contains(row.Label);
        }

        private MaterialVariationOptions CreateOptions()
        {
            var assignments = fieldRows
                .Where(row => row.Enabled && !string.IsNullOrWhiteSpace(row.Pattern))
                .Select(row => new MaterialVariationFieldAssignment(row.Descriptor, row.Pattern.Trim()))
                .ToArray();

            return new MaterialVariationOptions
            {
                StartIndex = (int)startIndexControl.Value,
                Count = (int)countControl.Value,
                Step = (int)stepControl.Value,
                OutputPattern = outputPatternTextBox.Text.Trim(),
                VaryGreyscaleToPaletteScale = greyscaleModeCheckBox.Checked,
                GreyscaleToPaletteScaleStart = (float)greyscaleStartControl.Value,
                GreyscaleToPaletteScaleStep = (float)greyscaleStepControl.Value,
                Fields = assignments,
                AdvancedVariant = IsAdvancedMode ? CreateAdvancedVariantOptions() : null
            };
        }

        private AdvancedVariantOptions CreateAdvancedVariantOptions()
        {
            var layers = BuildAdvancedLayers();
            var rules = AdvancedVariantDialogSupport.BuildRules(advancedRuleRows, includeDefaultRuleWhenEmpty: false);

            return new AdvancedVariantOptions
            {
                Layers = layers,
                Rules = rules
            };
        }

        private IReadOnlyList<AdvancedVariantLayerDefinition> BuildAdvancedLayers()
        {
            return AdvancedVariantDialogSupport.BuildLayers(advancedLayerCountControls, "Layer {0}");
        }

        private string ValidateOptions(MaterialVariationOptions options, out bool hasWarnings)
        {
            hasWarnings = false;

            if (string.IsNullOrWhiteSpace(options.OutputPattern))
                return "Enter an output path pattern before generating.";

            if (!IsAdvancedMode && !MaterialVariationGenerator.ContainsIndexPlaceholder(options.OutputPattern))
                return "The output path must include an {index} placeholder.";

            if (IsAdvancedMode)
            {
                if (options.AdvancedVariant == null || options.AdvancedVariant.Layers.Count == 0)
                    return "Enter at least one non-zero layer count for Advanced mode.";

                if (advancedResolvedContexts.Count == 0 || !advancedResolvedContexts.Any(context => context.Enabled))
                    return "Enable at least one resolved advanced row before generating.";
            }

            IReadOnlyList<string> previewPaths;
            try
            {
                previewPaths = MaterialVariationGenerator.PreviewOutputPaths(options);
            }
            catch (Exception ex)
            {
                return $"The output path pattern is invalid: {ex.Message}";
            }

            var duplicates = previewPaths
                .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(duplicates))
                return $"The current settings would generate duplicate output paths, for example: {duplicates}";

            if (!options.Fields.Any())
            {
                hasWarnings = true;
                return "No replacement rows are enabled. This will generate renamed clones of the template only.";
            }

            return string.Empty;
        }

        private string BuildOutputPreview(MaterialVariationOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.OutputPattern))
                return IsAdvancedMode
                    ? "Enter a path pattern such as armor_{indexNN}_{indexTok}.bgsm."
                    : "Enter a path pattern such as armor_{index:00}.bgsm.";

            try
            {
                var previewPaths = MaterialVariationGenerator.PreviewOutputPaths(options);
                if (previewPaths.Count == 0)
                    return "No files would be generated.";

                var first = previewPaths.First();
                var last = previewPaths.Last();
                return previewPaths.Count == 1
                    ? first
                    : $"First: {first}{Environment.NewLine}Last: {last}";
            }
            catch
            {
                return "The current path pattern is invalid.";
            }
        }

        private string BuildFieldPreview()
        {
            var selectedRow = fieldGrid.CurrentRow?.DataBoundItem as FieldRow
                ?? fieldRows.FirstOrDefault(row => row.Enabled);

            if (selectedRow == null)
                return "Select a texture slot to preview its replacement.";

            if (!selectedRow.Enabled || string.IsNullOrWhiteSpace(selectedRow.Pattern))
                return $"{selectedRow.Label} stays as {selectedRow.CurrentValue}";

            if (!IsAdvancedMode)
            {
                var previewValue = MaterialVariationGenerator.PreviewFieldValue(selectedRow.Pattern, (int)startIndexControl.Value);
                return $"{selectedRow.Label} -> {previewValue}";
            }

            var previewContext = advancedResolvedContexts.FirstOrDefault(context => context.Enabled);
            if (previewContext == null)
                return "Select or enable an advanced row to preview generated values.";

            var advancedPreviewValue = MaterialVariationGenerator.PreviewFieldValue(selectedRow.Pattern, previewContext);
            return $"{selectedRow.Label} [{previewContext.IndexNN}] -> {advancedPreviewValue}";
        }

        private string BuildSummaryText(MaterialVariationOptions options)
        {
            if (!IsAdvancedMode)
                return $"Will generate {options.Count} files starting at index {options.StartIndex} with step {options.Step}.";

            int totalRows = advancedResolvedContexts.Count;
            int enabledRows = advancedResolvedContexts.Count(context => context.Enabled);
            int layers = options.AdvancedVariant?.Layers.Count ?? 0;
            return $"Will generate {enabledRows} file(s) from {totalRows} resolved variation(s) across {layers} layer(s).";
        }

        private IReadOnlyList<AdvancedVariantResolvedContext> ResolveAdvancedContexts(MaterialVariationOptions options)
        {
            if (!IsAdvancedMode)
                return Array.Empty<AdvancedVariantResolvedContext>();

            try
            {
                return options.AdvancedVariant == null
                    ? Array.Empty<AdvancedVariantResolvedContext>()
                    : AdvancedVariantEngine.Resolve(options.AdvancedVariant).ToArray();
            }
            catch
            {
                return Array.Empty<AdvancedVariantResolvedContext>();
            }
        }

        private void EnsureAdvancedLayerDefaults()
        {
            if (advancedLayerCountControls.Any(control => control.Value > 0))
                return;

            advancedLayerCountControls[0].Value = Math.Max(1, countControl.Value);
        }

        private void NormalizeOutputPatternForAdvancedMode()
        {
            if (string.IsNullOrWhiteSpace(outputPatternTextBox.Text))
                return;

            string normalized = MaterialVariationGenerator.NormalizeLegacyOutputPatternForAdvancedMode(outputPatternTextBox.Text);
            if (!string.Equals(normalized, outputPatternTextBox.Text, StringComparison.Ordinal))
                outputPatternTextBox.Text = normalized;
        }

        private void SynchronizeAdvancedRows()
        {
            UpdateAdvancedLayerColumnChoices();
            NormalizeAdvancedRuleRowsForLayerCounts();

            if (advancedRuleRows.Count > 0)
                return;

            suppressAdvancedRowEvents = true;
            try
            {
                if (!AdvancedVariantDialogSupport.EnsureAtLeastOneRule(
                    advancedRuleRows,
                    BuildAdvancedLayers().Count,
                    AdvancedRuleRow_PropertyChanged))
                {
                    return;
                }
            }
            finally
            {
                suppressAdvancedRowEvents = false;
            }

            SelectAdvancedRuleRow(0);
        }

        private void UpdateAdvancedRuleButtonState()
        {
            int selectedIndex = GetSelectedAdvancedRuleIndex();
            bool canModifyRules = IsAdvancedMode && BuildAdvancedLayers().Count > 0;
            AdvancedVariantDialogSupport.UpdateRuleButtonState(
                addRuleButton,
                deleteRuleButton,
                moveRuleUpButton,
                moveRuleDownButton,
                selectedIndex,
                advancedRuleRows.Count,
                canModifyRules);
        }

        private void AddAdvancedRuleRow()
        {
            AdvancedVariantDialogSupport.AddRule(advancedRuleRows, AdvancedRuleRow_PropertyChanged);
            SelectAdvancedRuleRow(advancedRuleRows.Count - 1);
        }

        private void MoveAdvancedRule(int direction)
        {
            int selectedIndex = GetSelectedAdvancedRuleIndex();
            int targetIndex = selectedIndex + direction;
            if (selectedIndex < 0 || targetIndex < 0 || targetIndex >= advancedRuleRows.Count)
                return;

            suppressAdvancedRowEvents = true;
            try
            {
                targetIndex = AdvancedVariantDialogSupport.MoveSelectedRule(advancedRuleRows, selectedIndex, direction);
            }
            finally
            {
                suppressAdvancedRowEvents = false;
            }

            SelectAdvancedRuleRow(targetIndex);
            RefreshUiState();
        }

        private int GetSelectedAdvancedRuleIndex()
        {
            return advancedGrid.CurrentRow?.Index ?? -1;
        }

        private void SelectAdvancedRuleRow(int rowIndex)
        {
            AdvancedVariantDialogSupport.SelectGridRow(advancedGrid, rowIndex);
        }

        private void UpdateAdvancedLayerColumnChoices()
        {
            AdvancedVariantDialogSupport.UpdateLayerColumnChoices(advancedLayerColumns, advancedLayerCountControls);
        }

        private void NormalizeAdvancedRuleRowsForLayerCounts()
        {
            suppressAdvancedRowEvents = true;
            try
            {
                AdvancedVariantDialogSupport.NormalizeLayerMatchTexts(advancedRuleRows, advancedLayerCountControls);
            }
            finally
            {
                suppressAdvancedRowEvents = false;
            }
        }

        private void UpdateAdvancedWarning(IReadOnlyList<AdvancedVariantResolvedContext> resolvedContexts)
        {
            if (!IsAdvancedMode)
            {
                advancedWarningLabel.Text = string.Empty;
                advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Empty);
                return;
            }

            IReadOnlyList<AdvancedVariantResolvedContext> enabledContexts = resolvedContexts
                .Where(context => context.Enabled)
                .ToArray();
            IReadOnlyList<string> tooltipLines = AdvancedVariantDialogSupport.BuildFallbackTooltipLines(
                enabledContexts,
                includeLayerTokenFallbacks: false,
                out int fallbackContextCount);

            if (resolvedContexts.Count == 0)
            {
                advancedWarningLabel.Text = "0 variation(s) remain unnamed.";
                advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Empty);
                return;
            }

            if (fallbackContextCount == 0)
            {
                advancedWarningLabel.Text = "0 variation(s) remain unnamed.";
                advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Empty);
                return;
            }

            advancedWarningLabel.Text = $"{fallbackContextCount} variation(s) remain unnamed. They will fall back to numeric index values.";
            advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Join(Environment.NewLine, tooltipLines));
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            var options = CreateOptions();
            string message = ValidateOptions(options, out var hasWarnings);
            if (!string.IsNullOrEmpty(message) && !hasWarnings)
            {
                MessageBox.Show(this, message, "Cannot Generate Variations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (hasWarnings)
            {
                var result = MessageBox.Show(
                    this,
                    $"{message}\n\nDo you want to continue?",
                    "Generate Renamed Clones",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;
            }

            Options = options;
            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class FieldRow : INotifyPropertyChanged
        {
            private bool enabled;
            private string pattern;

            public FieldRow(MaterialFieldDescriptor descriptor, string value)
            {
                Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
                CurrentValue = value ?? string.Empty;
                pattern = CurrentValue;
                enabled = false;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public MaterialFieldDescriptor Descriptor { get; }
            public string Label => Descriptor.Label;
            public string CurrentValue { get; }

            public bool Enabled
            {
                get => enabled;
                set
                {
                    if (enabled == value)
                        return;

                    enabled = value;
                    OnPropertyChanged(nameof(Enabled));
                }
            }

            public string Pattern
            {
                get => pattern;
                set
                {
                    if (pattern == value)
                        return;

                    pattern = value ?? string.Empty;
                    OnPropertyChanged(nameof(Pattern));
                }
            }

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
