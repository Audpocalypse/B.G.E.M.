using Material_Editor.AdvancedVariant;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        private static readonly Regex LegacyIndexPlaceholderRegex = new(@"\{index(?:\:[^\}]+)?\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            headerLabel = CreateWrappingLabel("Generate multiple material files from the open template.", new Padding(0, 0, 0, 6));
            AppearanceApplicator.SetFontRole(headerLabel, AppearanceFontRole.Bold);
            mainLayout.Controls.Add(headerLabel, 0, 0);

            introLabel = CreateWrappingLabel(
                "Follow the steps below: name the outputs, choose simple or advanced indexing, then enable any string or path fields you want to rewrite.",
                new Padding(0, 0, 0, 12));
            mainLayout.Controls.Add(introLabel, 0, 1);

            var outputStepLabel = CreateSectionHeader("1. Output Naming", new Padding(0, 0, 0, 4));
            mainLayout.Controls.Add(outputStepLabel, 0, 2);

            outputHelpLabel = CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 6));
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

            mainLayout.Controls.Add(CreateCaptionLabel("Output Preview", new Padding(0, 0, 0, 4)), 0, 5);

            outputPreviewTextBox = CreatePreviewTextBox(64, new Padding(0, 0, 0, 12));
            mainLayout.Controls.Add(outputPreviewTextBox, 0, 6);

            var indexingStepLabel = CreateSectionHeader("2. Indexing", new Padding(0, 0, 0, 4));
            mainLayout.Controls.Add(indexingStepLabel, 0, 7);

            advancedModeCheckBox = new ColorToggleCheckBox
            {
                Text = "Use Advanced Mode",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            advancedModeCheckBox.CheckedChanged += AdvancedModeCheckBox_CheckedChanged;
            mainLayout.Controls.Add(advancedModeCheckBox, 0, 8);

            indexHelpLabel = CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 6));
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

            rangeLayout.Controls.Add(CreateInlineLabel("Start index:"), 0, 0);
            startIndexControl = CreateNumericInput(0, 9999, 1);
            startIndexControl.ValueChanged += HandleConfigurationChanged;
            rangeLayout.Controls.Add(startIndexControl, 1, 0);

            rangeLayout.Controls.Add(CreateInlineLabel("Count:"), 2, 0);
            countControl = CreateNumericInput(1, 999, 3);
            countControl.ValueChanged += HandleConfigurationChanged;
            rangeLayout.Controls.Add(countControl, 3, 0);

            rangeLayout.Controls.Add(CreateInlineLabel("Step:"), 4, 0);
            stepControl = CreateNumericInput(1, 999, 1);
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

            var countsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 8)
            };

            advancedLayerCountControls = new NumericUpDown[4];
            for (int index = 0; index < advancedLayerCountControls.Length; index++)
            {
                var countItemPanel = new FlowLayoutPanel
                {
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.LeftToRight,
                    Margin = new Padding(0, 0, 16, 6)
                };
                countItemPanel.Controls.Add(CreateInlineLabel($"Layer {index + 1} count:"));

                decimal defaultValue = index == 0 ? 3 : 0;
                var layerCount = CreateNumericInput(0, 99, defaultValue);
                layerCount.ValueChanged += HandleAdvancedDefinitionChanged;
                advancedLayerCountControls[index] = layerCount;
                countItemPanel.Controls.Add(layerCount);

                countsLayout.Controls.Add(countItemPanel);
            }
            advancedLayout.Controls.Add(countsLayout, 0, 0);

            advancedHelpLabel = CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 6));
            advancedLayout.Controls.Add(advancedHelpLabel, 0, 1);

            advancedWarningLabel = CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 8));
            advancedLayout.Controls.Add(advancedWarningLabel, 0, 2);

            advancedLayout.Controls.Add(CreateCaptionLabel("Naming Rules", new Padding(0, 0, 0, 4)), 0, 3);

            var ruleButtonsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 6)
            };

            addRuleButton = new Button
            {
                Text = "Add Rule",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 8, 0)
            };
            addRuleButton.Click += AddRuleButton_Click;
            ruleButtonsLayout.Controls.Add(addRuleButton);

            deleteRuleButton = new Button
            {
                Text = "Delete Rule",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 8, 0)
            };
            deleteRuleButton.Click += DeleteRuleButton_Click;
            ruleButtonsLayout.Controls.Add(deleteRuleButton);

            moveRuleUpButton = new Button
            {
                Text = "Move Up",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 8, 0)
            };
            moveRuleUpButton.Click += MoveRuleUpButton_Click;
            ruleButtonsLayout.Controls.Add(moveRuleUpButton);

            moveRuleDownButton = new Button
            {
                Text = "Move Down",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
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
            advancedLayerColumns = new DataGridViewComboBoxColumn[4];
            for (int index = 0; index < advancedLayerColumns.Length; index++)
            {
                var column = new DataGridViewComboBoxColumn
                {
                    DataPropertyName = nameof(AdvancedRuleRow.Layer1MatchText).Replace("1", (index + 1).ToString()),
                    HeaderText = $"Layer {index + 1}",
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                    FlatStyle = FlatStyle.Flat,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 10f
                };
                advancedLayerColumns[index] = column;
                advancedGrid.Columns.Add(column);
            }
            advancedGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AdvancedRuleRow.IndexTok),
                HeaderText = "indexTok",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18f
            });
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

            greyscaleLayout.Controls.Add(CreateInlineLabel("Start value:"), 0, 1);
            greyscaleStartControl = CreateNumericInput(-1000, 1000, 1, 3, 0.1m);
            greyscaleStartControl.ValueChanged += HandleConfigurationChanged;
            greyscaleLayout.Controls.Add(greyscaleStartControl, 1, 1);

            greyscaleLayout.Controls.Add(CreateInlineLabel("Step:"), 2, 1);
            greyscaleStepControl = CreateNumericInput(-1000, 1000, 0, 3, 0.1m);
            greyscaleStepControl.ValueChanged += HandleConfigurationChanged;
            greyscaleLayout.Controls.Add(greyscaleStepControl, 3, 1);

            greyscaleNoteLabel = CreateWrappingLabel(
                "Each generated file will use start + step * file index offset when enabled.",
                new Padding(0, 8, 0, 0));
            greyscaleLayout.Controls.Add(greyscaleNoteLabel, 0, 2);
            greyscaleLayout.SetColumnSpan(greyscaleNoteLabel, 5);
            greyscaleGroup.Controls.Add(greyscaleLayout);
            mainLayout.Controls.Add(greyscaleGroup, 0, 12);

            var fieldStepLabel = CreateSectionHeader("3. Fields To Vary", new Padding(0, 0, 0, 4));
            mainLayout.Controls.Add(fieldStepLabel, 0, 13);

            fieldHelpLabel = CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 8));
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

            mainLayout.Controls.Add(CreateCaptionLabel("Field Preview", new Padding(0, 0, 0, 4)), 0, 16);

            fieldPreviewTextBox = CreatePreviewTextBox(52, new Padding(0, 0, 0, 8));
            mainLayout.Controls.Add(fieldPreviewTextBox, 0, 17);

            var previewStepLabel = CreateSectionHeader("4. Preview / Generate", new Padding(0, 0, 0, 4));
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

            summaryLabel = CreateWrappingLabel(string.Empty, new Padding(0, 0, 0, 4));
            footerMessageLayout.Controls.Add(summaryLabel, 0, 0);

            validationLabel = CreateWrappingLabel(string.Empty, new Padding(0));
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

        private Label CreateSectionHeader(string text, Padding margin)
        {
            var label = CreateWrappingLabel(text, margin);
            AppearanceApplicator.SetFontRole(label, AppearanceFontRole.Bold);
            return label;
        }

        private static Label CreateCaptionLabel(string text, Padding margin)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = margin
            };
        }

        private static Label CreateWrappingLabel(string text, Padding margin)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = margin
            };
        }

        private static Label CreateInlineLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 4, 8, 0)
            };
        }

        private static NumericUpDown CreateNumericInput(decimal minimum, decimal maximum, decimal value, int decimalPlaces = 0, decimal increment = 1m)
        {
            return new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = value,
                DecimalPlaces = decimalPlaces,
                Increment = increment,
                Width = 104,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 16, 0)
            };
        }

        private static TextBox CreatePreviewTextBox(int height, Padding margin)
        {
            return new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Top,
                Height = height,
                Margin = margin,
                TabStop = false,
                ShortcutsEnabled = true
            };
        }

        private void HandleLayoutChanged(object sender, EventArgs e)
        {
            UpdateMainLayoutWidth();
            UpdateFieldGridHeight();
            UpdateWrappingLabelWidths();
        }

        private void UpdateMainLayoutWidth()
        {
            if (scrollHost == null || mainLayout == null)
                return;

            int availableWidth = scrollHost.ClientSize.Width;
            if (scrollHost.VerticalScroll.Visible)
                availableWidth -= SystemInformation.VerticalScrollBarWidth;

            mainLayout.Width = Math.Max(availableWidth, 820);
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
            SetWrappingWidth(headerLabel);
            SetWrappingWidth(introLabel);
            SetWrappingWidth(outputHelpLabel);
            SetWrappingWidth(indexHelpLabel);
            SetWrappingWidth(advancedHelpLabel);
            SetWrappingWidth(advancedWarningLabel);
            SetWrappingWidth(greyscaleNoteLabel);
            SetWrappingWidth(fieldHelpLabel);
            SetWrappingWidth(summaryLabel);
            SetWrappingWidth(validationLabel);
        }

        private static void SetWrappingWidth(Label label)
        {
            if (label?.Parent == null)
                return;

            int availableWidth = 0;
            for (Control current = label.Parent; current != null; current = current.Parent)
                availableWidth = Math.Max(availableWidth, current.ClientSize.Width);

            if (label.Parent is ScrollableControl scrollableControl)
                availableWidth -= scrollableControl.Padding.Horizontal;

            availableWidth = Math.Max(100, availableWidth - label.Margin.Horizontal);
            if (label.MaximumSize.Width != availableWidth)
                label.MaximumSize = new Size(availableWidth, 0);
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
            if (advancedGrid.IsCurrentCellDirty
                && advancedGrid.CurrentCell is DataGridViewComboBoxCell)
                advancedGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
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
                advancedRuleRows[selectedIndex].PropertyChanged -= AdvancedRuleRow_PropertyChanged;
                advancedRuleRows.RemoveAt(selectedIndex);
            }
            finally
            {
                suppressAdvancedRowEvents = false;
            }

            if (advancedRuleRows.Count == 0)
                AddAdvancedRuleRow();

            SelectAdvancedRuleRow(Math.Min(selectedIndex, advancedRuleRows.Count - 1));
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

            e.ToolTipText = e.ColumnIndex switch
            {
                0 => GetLayerColumnTooltipText(0),
                1 => GetLayerColumnTooltipText(1),
                2 => GetLayerColumnTooltipText(2),
                3 => GetLayerColumnTooltipText(3),
                4 => row.IndexTok,
                _ => string.Empty
            };
        }

        private void AdvancedGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
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
            UpdateAdvancedWarning(options, advancedResolvedContexts);
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
            var rules = advancedRuleRows
                .Where(row => row.HasMeaningfulContent)
                .Select((row, index) => row.ToRule(index))
                .ToArray();

            return new AdvancedVariantOptions
            {
                Layers = layers,
                Rules = rules
            };
        }

        private IReadOnlyList<AdvancedVariantLayerDefinition> BuildAdvancedLayers()
        {
            var layers = new List<AdvancedVariantLayerDefinition>();
            for (int index = 0; index < advancedLayerCountControls.Length; index++)
            {
                int count = (int)advancedLayerCountControls[index].Value;
                if (count <= 0)
                    continue;

                layers.Add(new AdvancedVariantLayerDefinition(
                    $"Layer {index + 1}",
                    Enumerable.Range(1, count).ToArray()));
            }

            return layers;
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

            string normalized = LegacyIndexPlaceholderRegex.Replace(outputPatternTextBox.Text, "{indexNN}");
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
                var layers = BuildAdvancedLayers();
                if (layers.Count == 0)
                    return;

                AddAdvancedRuleRow();
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
            bool hasSelection = selectedIndex >= 0 && selectedIndex < advancedRuleRows.Count;
            bool canModifyRules = IsAdvancedMode && BuildAdvancedLayers().Count > 0;

            addRuleButton.Enabled = canModifyRules;
            deleteRuleButton.Enabled = canModifyRules && hasSelection && advancedRuleRows.Count > 1;
            moveRuleUpButton.Enabled = canModifyRules && hasSelection && selectedIndex > 0;
            moveRuleDownButton.Enabled = canModifyRules && hasSelection && selectedIndex >= 0 && selectedIndex < advancedRuleRows.Count - 1;
        }

        private void AddAdvancedRuleRow()
        {
            var row = new AdvancedRuleRow();
            row.PropertyChanged += AdvancedRuleRow_PropertyChanged;
            advancedRuleRows.Add(row);
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
                var row = advancedRuleRows[selectedIndex];
                advancedRuleRows.RemoveAt(selectedIndex);
                advancedRuleRows.Insert(targetIndex, row);
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
            if (rowIndex < 0 || rowIndex >= advancedGrid.Rows.Count)
                return;

            advancedGrid.ClearSelection();
            advancedGrid.Rows[rowIndex].Selected = true;
            advancedGrid.CurrentCell = advancedGrid.Rows[rowIndex].Cells[0];
        }

        private void UpdateAdvancedLayerColumnChoices()
        {
            for (int index = 0; index < advancedLayerColumns.Length; index++)
            {
                int count = index < advancedLayerCountControls.Length
                    ? (int)advancedLayerCountControls[index].Value
                    : 0;

                string[] choices = new[] { string.Empty }
                    .Concat(Enumerable.Range(1, count).Select(value => value.ToString()))
                    .ToArray();
                SetComboColumnChoices(advancedLayerColumns[index], choices);
            }
        }

        private static void SetComboColumnChoices(DataGridViewComboBoxColumn column, IEnumerable<string> values)
        {
            column.Items.Clear();
            foreach (var value in values)
                column.Items.Add(value);
        }

        private void NormalizeAdvancedRuleRowsForLayerCounts()
        {
            suppressAdvancedRowEvents = true;
            try
            {
                for (int index = 0; index < advancedRuleRows.Count; index++)
                {
                    var row = advancedRuleRows[index];
                    row.Layer1MatchText = NormalizeLayerMatchText(row.Layer1MatchText, 0);
                    row.Layer2MatchText = NormalizeLayerMatchText(row.Layer2MatchText, 1);
                    row.Layer3MatchText = NormalizeLayerMatchText(row.Layer3MatchText, 2);
                    row.Layer4MatchText = NormalizeLayerMatchText(row.Layer4MatchText, 3);
                }
            }
            finally
            {
                suppressAdvancedRowEvents = false;
            }
        }

        private string NormalizeLayerMatchText(string value, int layerIndex)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            int count = layerIndex < advancedLayerCountControls.Length
                ? (int)advancedLayerCountControls[layerIndex].Value
                : 0;

            if (count <= 0)
                return string.Empty;

            if (int.TryParse(value, out int parsed) && parsed >= 1 && parsed <= count)
                return parsed.ToString();

            return string.Empty;
        }

        private string GetLayerColumnTooltipText(int zeroBasedLayerIndex)
        {
            int count = zeroBasedLayerIndex < advancedLayerCountControls.Length
                ? (int)advancedLayerCountControls[zeroBasedLayerIndex].Value
                : 0;

            return count > 0
                ? $"Choose 1-{count}, or leave blank to match any Layer {zeroBasedLayerIndex + 1} value."
                : $"Leave blank while Layer {zeroBasedLayerIndex + 1} count is 0.";
        }

        private void UpdateAdvancedWarning(MaterialVariationOptions options, IReadOnlyList<AdvancedVariantResolvedContext> resolvedContexts)
        {
            if (!IsAdvancedMode)
            {
                advancedWarningLabel.Text = string.Empty;
                advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Empty);
                return;
            }

            var unnamedContexts = resolvedContexts
                .Where(context => context.Enabled && context.IndexTokFallback)
                .ToArray();

            if (resolvedContexts.Count == 0)
            {
                advancedWarningLabel.Text = "0 variation(s) remain unnamed.";
                advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Empty);
                return;
            }

            if (unnamedContexts.Length == 0)
            {
                advancedWarningLabel.Text = "0 variation(s) remain unnamed.";
                advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Empty);
                return;
            }

            advancedWarningLabel.Text = $"{unnamedContexts.Length} variation(s) remain unnamed. They will fall back to numeric index values.";
            advancedWarningToolTip.SetToolTip(
                advancedWarningLabel,
                string.Join(
                    Environment.NewLine,
                    unnamedContexts.Select(context => $"{BuildAdvancedLayerDescription(context)} -> {{indexTok}}={context.IndexTok}")));
        }

        private static string BuildAdvancedLayerDescription(AdvancedVariantResolvedContext context)
        {
            return string.Join(
                ", ",
                context.LayerIndices
                    .Select((value, index) => $"Layer{index + 1}={value}"));
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

        private sealed class AdvancedRuleRow : INotifyPropertyChanged
        {
            private string layer1MatchText;
            private string layer2MatchText;
            private string layer3MatchText;
            private string layer4MatchText;
            private string indexTok;

            public event PropertyChangedEventHandler PropertyChanged;

            public string Layer1MatchText
            {
                get => layer1MatchText;
                set => SetField(ref layer1MatchText, value ?? string.Empty, nameof(Layer1MatchText));
            }

            public string Layer2MatchText
            {
                get => layer2MatchText;
                set => SetField(ref layer2MatchText, value ?? string.Empty, nameof(Layer2MatchText));
            }

            public string Layer3MatchText
            {
                get => layer3MatchText;
                set => SetField(ref layer3MatchText, value ?? string.Empty, nameof(Layer3MatchText));
            }

            public string Layer4MatchText
            {
                get => layer4MatchText;
                set => SetField(ref layer4MatchText, value ?? string.Empty, nameof(Layer4MatchText));
            }

            public string IndexTok
            {
                get => indexTok;
                set => SetField(ref indexTok, value ?? string.Empty, nameof(IndexTok));
            }

            public bool HasMeaningfulContent =>
                !string.IsNullOrWhiteSpace(Layer1MatchText)
                || !string.IsNullOrWhiteSpace(Layer2MatchText)
                || !string.IsNullOrWhiteSpace(Layer3MatchText)
                || !string.IsNullOrWhiteSpace(Layer4MatchText)
                || !string.IsNullOrWhiteSpace(IndexTok);

            public AdvancedVariantRule ToRule(int sourceOrder)
            {
                return new AdvancedVariantRule
                {
                    Layer1Index = ParseLayerMatch(Layer1MatchText),
                    Layer2Index = ParseLayerMatch(Layer2MatchText),
                    Layer3Index = ParseLayerMatch(Layer3MatchText),
                    Layer4Index = ParseLayerMatch(Layer4MatchText),
                    IndexToken = NormalizeToken(IndexTok),
                    SourceOrder = sourceOrder
                };
            }

            private static int? ParseLayerMatch(string value)
            {
                return int.TryParse(value, out int parsed) ? parsed : null;
            }

            private static string NormalizeToken(string value)
            {
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }

            private void SetField(ref string field, string value, string propertyName)
            {
                if (field == value)
                    return;

                field = value;
                OnPropertyChanged(propertyName);
            }

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
