using Material_Editor.AdvancedVariant;
using MaterialLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class AdvancedFieldOverwriteDialog : ThemeAwareForm
    {
        private const int ApplyColumnIndex = 0;
        private const int CurrentValueColumnIndex = 4;
        private const int NewValueColumnIndex = 5;

        private readonly BaseMaterialFile sourceState;
        private readonly IReadOnlyList<string> targetFiles;
        private readonly IReadOnlyList<MaterialFieldDescriptor> selectedDescriptors;
        private readonly BindingList<TargetRow> targetRows;
        private readonly BindingList<StringAssignmentRow> stringAssignmentRows;
        private readonly BindingList<AdvancedRuleRow> advancedRuleRows;
        private readonly Dictionary<(string TargetPath, string FieldLabel), string> fieldValueOverrides = new();
        private readonly Dictionary<string, BaseMaterialFile> materialCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loadFailedPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly MaterialFieldDescriptor grayscaleDescriptor;
        private readonly NumericUpDown startIndexControl;
        private readonly NumericUpDown countControl;
        private readonly NumericUpDown stepControl;
        private readonly NumericUpDown grayscaleStartControl;
        private readonly NumericUpDown grayscaleStepControl;
        private readonly NumericUpDown[] advancedLayerCountControls;
        private readonly Panel scrollHost;
        private readonly TableLayoutPanel mainLayout;
        private readonly DataGridView assignmentGrid;
        private readonly DataGridView targetGrid;
        private readonly DataGridView advancedGrid;
        private readonly DataGridViewComboBoxColumn[] advancedLayerColumns;
        private readonly Label advancedWarningLabel;
        private readonly Label copyAsIsLabel;
        private readonly Label summaryLabel;
        private readonly Label validationLabel;
        private readonly Button okButton;
        private readonly Button addRuleButton;
        private readonly Button deleteRuleButton;
        private readonly Button moveRuleUpButton;
        private readonly Button moveRuleDownButton;
        private readonly ToolTip advancedWarningToolTip;

        private bool suppressRefresh;
        private bool suppressAdvancedRowEvents;

        public AdvancedFieldOverwriteDialog(
            BaseMaterialFile sourceState,
            IReadOnlyList<MaterialFieldDescriptor> selectedDescriptors,
            IReadOnlyList<string> targetFiles)
        {
            this.sourceState = sourceState ?? throw new ArgumentNullException(nameof(sourceState));
            this.targetFiles = targetFiles ?? Array.Empty<string>();

            var selected = selectedDescriptors?.ToList() ?? new List<MaterialFieldDescriptor>();
            this.selectedDescriptors = new ReadOnlyCollection<MaterialFieldDescriptor>(selected);
            grayscaleDescriptor = selected.FirstOrDefault(descriptor => string.Equals(descriptor.Label, ControlNames.GrayscaleToPaletteScale, StringComparison.OrdinalIgnoreCase));

            var stringDescriptors = selected
                .Where(descriptor => descriptor != grayscaleDescriptor)
                .Where(descriptor => descriptor.GetValue(sourceState) is string)
                .ToList();
            var copyAsIsFields = selected
                .Except(stringDescriptors)
                .Where(descriptor => descriptor != grayscaleDescriptor)
                .Select(descriptor => descriptor.Label)
                .ToArray();

            targetRows = new BindingList<TargetRow>(CreateTargetRows(this.targetFiles).ToList());
            stringAssignmentRows = new BindingList<StringAssignmentRow>(stringDescriptors
                .Select(descriptor => new StringAssignmentRow(
                    descriptor,
                    descriptor.GetValue(sourceState) as string ?? string.Empty))
                .ToList());
            advancedRuleRows = new BindingList<AdvancedRuleRow>();

            int compatibleCount = Math.Max(1, targetRows.Count(row =>
                row.TargetPath.EndsWith(
                    MaterialFileTypeHelper.GetExpectedExtension(MaterialFileTypeHelper.GetMaterialType(sourceState)),
                    StringComparison.OrdinalIgnoreCase)));

            Text = "Advanced Iterative Overwrite";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1260, 920);
            MinimumSize = new Size(1020, 760);
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;

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
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 8,
                Padding = new Padding(12),
                Margin = new Padding(0)
            };
            for (int index = 0; index < 4; index++)
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            scrollHost.Controls.Add(mainLayout);

            var headerLabel = new Label
            {
                AutoSize = true,
                Text = "Edit selected target files in-place using stable sorted-path iteration plus layered advanced tokens. Overwritten values can be edited per target.",
                Margin = new Padding(0, 0, 0, 8)
            };
            mainLayout.Controls.Add(headerLabel, 0, 0);

            var iterationGroup = new GroupBox
            {
                Text = "Target Selection",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 10)
            };
            var iterationLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 6,
                Padding = new Padding(12, 10, 12, 12)
            };
            for (int index = 0; index < 6; index++)
                iterationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            iterationLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Start index:", new Padding(0, 6, 6, 0)), 0, 0);
            startIndexControl = DialogLayoutSupport.CreateNumericInput(0, 9999, 1, 76, new Padding(3), textAlign: HorizontalAlignment.Right);
            startIndexControl.ValueChanged += HandleConfigurationChanged;
            iterationLayout.Controls.Add(startIndexControl, 1, 0);

            iterationLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Count:", new Padding(0, 6, 6, 0)), 2, 0);
            countControl = DialogLayoutSupport.CreateNumericInput(1, compatibleCount, compatibleCount, 76, new Padding(3), textAlign: HorizontalAlignment.Right);
            countControl.ValueChanged += HandleConfigurationChanged;
            iterationLayout.Controls.Add(countControl, 3, 0);

            iterationLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Step:", new Padding(0, 6, 6, 0)), 4, 0);
            stepControl = DialogLayoutSupport.CreateNumericInput(1, 999, 1, 76, new Padding(3), textAlign: HorizontalAlignment.Right);
            stepControl.ValueChanged += HandleConfigurationChanged;
            iterationLayout.Controls.Add(stepControl, 5, 0);

            var iterationHelpLabel = new Label
            {
                AutoSize = true,
                Text = "Compatible targets are sorted by full path. Step chooses which files participate before layered token contexts are assigned.",
                Margin = new Padding(0, 8, 0, 0)
            };
            iterationLayout.Controls.Add(iterationHelpLabel, 0, 1);
            iterationLayout.SetColumnSpan(iterationHelpLabel, 6);
            iterationGroup.Controls.Add(iterationLayout);
            mainLayout.Controls.Add(iterationGroup, 0, 1);

            var advancedGroup = new GroupBox
            {
                Text = "Advanced Variant",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 10)
            };
            var advancedLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Padding = new Padding(12, 10, 12, 12)
            };
            advancedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            (FlowLayoutPanel countsLayout, advancedLayerCountControls) = AdvancedVariantDialogSupport.CreateLayerCountLayout(
                layerCount: 4,
                defaultValueFactory: index => index == 0 ? compatibleCount : 0m,
                valueChangedHandler: HandleAdvancedDefinitionChanged,
                layoutMargin: new Padding(0, 0, 0, 8),
                itemMargin: new Padding(0, 0, 16, 6),
                labelMargin: new Padding(0, 6, 6, 0),
                numericWidth: 76,
                numericMargin: new Padding(3),
                textAlign: HorizontalAlignment.Right);
            advancedLayout.Controls.Add(countsLayout, 0, 0);

            var advancedHelpLabel = new Label
            {
                AutoSize = true,
                Text = "Use layered naming rules like Generate Variations. Patterns can use {index}, {indexNN}, {indexTok}, {indexLayer1}, {indexNNLayer1}, and {indexTokLayer1}.",
                Margin = new Padding(0, 0, 0, 6)
            };
            advancedLayout.Controls.Add(advancedHelpLabel, 0, 1);

            advancedWarningLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            advancedLayout.Controls.Add(advancedWarningLabel, 0, 2);

            advancedLayout.Controls.Add(DialogLayoutSupport.CreateWrappingLabel("Naming Rules", new Padding(0, 0, 0, 4), bold: true), 0, 3);

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

            moveRuleDownButton = DialogLayoutSupport.CreateCommandButton("Move Down", new Padding(0, 0, 8, 0));
            moveRuleDownButton.Click += MoveRuleDownButton_Click;
            ruleButtonsLayout.Controls.Add(moveRuleDownButton);

            advancedLayout.Controls.Add(ruleButtonsLayout, 0, 4);

            advancedGrid = new FocusAwareDataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EditMode = DataGridViewEditMode.EditOnEnter,
                Margin = new Padding(0),
                ScrollFallbackTarget = scrollHost
            };
            advancedLayerColumns = AdvancedVariantDialogSupport.AddRuleColumns(advancedGrid, layerCount: 4, layerFillWeight: 12f, indexTokenFillWeight: 20f);
            advancedGrid.CurrentCellDirtyStateChanged += AdvancedGrid_CurrentCellDirtyStateChanged;
            advancedGrid.CellEndEdit += AdvancedGrid_CellEndEdit;
            advancedGrid.CellValueChanged += AdvancedGrid_CellValueChanged;
            advancedGrid.CellToolTipTextNeeded += AdvancedGrid_CellToolTipTextNeeded;
            advancedGrid.DataError += AdvancedGrid_DataError;
            advancedGrid.SelectionChanged += HandleAdvancedGridSelectionChanged;
            advancedGrid.DataSource = advancedRuleRows;

            var advancedGridHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 210,
                Margin = new Padding(0)
            };
            advancedGridHost.Controls.Add(advancedGrid);
            advancedLayout.Controls.Add(advancedGridHost, 0, 5);
            advancedGroup.Controls.Add(advancedLayout);
            mainLayout.Controls.Add(advancedGroup, 0, 2);

            var assignmentGroup = new GroupBox
            {
                Text = "Overwrite Patterns",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 10)
            };
            var assignmentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Padding = new Padding(12, 10, 12, 12)
            };
            assignmentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var assignmentHelpLabel = new Label
            {
                AutoSize = true,
                Text = "Select a field row below to preview and edit per-target overwritten values in the target table.",
                Margin = new Padding(0, 0, 0, 6)
            };
            assignmentLayout.Controls.Add(assignmentHelpLabel, 0, 0);

            assignmentGrid = new FocusAwareDataGridView
            {
                Dock = DockStyle.Top,
                Height = Math.Max(84, 36 + Math.Max(1, stringAssignmentRows.Count) * 28),
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Margin = new Padding(0, 0, 0, 8),
                ScrollBars = ScrollBars.None,
                ScrollFallbackTarget = scrollHost
            };
            assignmentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(StringAssignmentRow.FieldLabel),
                HeaderText = "Field",
                FillWeight = 26f,
                ReadOnly = true
            });
            assignmentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(StringAssignmentRow.Pattern),
                HeaderText = "Pattern",
                FillWeight = 44f
            });
            assignmentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(StringAssignmentRow.Preview),
                HeaderText = "Resolved Preview",
                FillWeight = 30f,
                ReadOnly = true
            });
            assignmentGrid.CellEndEdit += HandleAssignmentGridEdited;
            assignmentGrid.SelectionChanged += HandleAssignmentGridSelectionChanged;
            assignmentGrid.DataSource = stringAssignmentRows;
            assignmentLayout.Controls.Add(assignmentGrid, 0, 1);

            if (grayscaleDescriptor != null)
            {
                var grayscaleGroup = new GroupBox
                {
                    Text = "Grayscale To Palette Scale",
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(0, 0, 0, 8)
                };
                var grayscaleLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 4,
                    Padding = new Padding(12, 10, 12, 12)
                };
                for (int index = 0; index < 4; index++)
                    grayscaleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                grayscaleLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Start value:", new Padding(0, 6, 6, 0)), 0, 0);
                grayscaleStartControl = DialogLayoutSupport.CreateNumericInput(-1000m, 1000m, GetInitialGreyscaleValue(), 92, new Padding(3), 3, 0.1m, HorizontalAlignment.Right);
                grayscaleStartControl.ValueChanged += HandleConfigurationChanged;
                grayscaleLayout.Controls.Add(grayscaleStartControl, 1, 0);

                grayscaleLayout.Controls.Add(DialogLayoutSupport.CreateInlineLabel("Step:", new Padding(0, 6, 6, 0)), 2, 0);
                grayscaleStepControl = DialogLayoutSupport.CreateNumericInput(-1000m, 1000m, 0m, 92, new Padding(3), 3, 0.1m, HorizontalAlignment.Right);
                grayscaleStepControl.ValueChanged += HandleConfigurationChanged;
                grayscaleLayout.Controls.Add(grayscaleStepControl, 3, 0);

                grayscaleGroup.Controls.Add(grayscaleLayout);
                assignmentLayout.Controls.Add(grayscaleGroup, 0, 2);
            }
            else
            {
                grayscaleStartControl = null;
                grayscaleStepControl = null;
            }

            copyAsIsLabel = new Label
            {
                AutoSize = true,
                Text = copyAsIsFields.Length == 0
                    ? "No additional selected fields will be copied as-is."
                    : $"Copied as-is on applied targets: {string.Join(", ", copyAsIsFields)}",
                Margin = new Padding(0)
            };
            assignmentLayout.Controls.Add(copyAsIsLabel, 0, grayscaleDescriptor != null ? 3 : 2);
            assignmentGroup.Controls.Add(assignmentLayout);
            mainLayout.Controls.Add(assignmentGroup, 0, 3);

            var targetGroup = new GroupBox
            {
                Text = "Target Preview",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                MinimumSize = new Size(0, 240)
            };
            var targetLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                Padding = new Padding(12, 10, 12, 12)
            };
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            targetGrid = new FocusAwareDataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollFallbackTarget = scrollHost,
                MinimumSize = new Size(0, 185)
            };
            targetGrid.Columns.Add(ColorToggleDataGridView.CreateColumn(
                nameof(TargetRow.ApplyEnabled),
                "Apply",
                fillWeight: 7f));
            targetGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TargetRow.SequenceText),
                HeaderText = "#",
                FillWeight = 7f,
                ReadOnly = true
            });
            targetGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TargetRow.Index),
                HeaderText = "Index",
                FillWeight = 10f,
                ReadOnly = true
            });
            targetGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TargetRow.IndexNN),
                HeaderText = "IndexNN",
                FillWeight = 12f,
                ReadOnly = true
            });
            targetGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TargetRow.CurrentValue),
                HeaderText = "Current Value",
                FillWeight = 24f,
                ReadOnly = true
            });
            targetGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TargetRow.NewValue),
                HeaderText = "Overwritten Value",
                FillWeight = 24f
            });
            targetGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TargetRow.StatusText),
                HeaderText = "Status",
                FillWeight = 16f,
                ReadOnly = true
            });
            targetGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TargetRow.TargetPath),
                HeaderText = "Target File",
                FillWeight = 30f,
                ReadOnly = true
            });
            targetGrid.CurrentCellDirtyStateChanged += TargetGrid_CurrentCellDirtyStateChanged;
            targetGrid.CellBeginEdit += TargetGrid_CellBeginEdit;
            targetGrid.CellEndEdit += HandleTargetGridEdited;
            targetGrid.CellToolTipTextNeeded += TargetGrid_CellToolTipTextNeeded;
            targetGrid.DataSource = targetRows;
            targetLayout.Controls.Add(targetGrid, 0, 0);
            targetGroup.Controls.Add(targetLayout);
            mainLayout.Controls.Add(targetGroup, 0, 4);

            summaryLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            mainLayout.Controls.Add(summaryLabel, 0, 5);

            validationLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            mainLayout.Controls.Add(validationLabel, 0, 6);

            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            var cancelButton = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(0)
            };
            buttonLayout.Controls.Add(cancelButton);

            okButton = new Button
            {
                Text = "OK",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 8, 0)
            };
            okButton.Click += OkButton_Click;
            buttonLayout.Controls.Add(okButton);
            mainLayout.Controls.Add(buttonLayout, 0, 7);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            advancedWarningToolTip = new ToolTip
            {
                ShowAlways = true,
                AutoPopDelay = 30000,
                InitialDelay = 200,
                ReshowDelay = 100
            };

            SynchronizeAdvancedRows();
            RefreshPreview();
            Load += HandleLayoutChanged;
            SizeChanged += HandleLayoutChanged;
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
            advancedWarningLabel.ForeColor = appearance.Theme.Semantics.Warning;
            validationLabel.ForeColor = string.IsNullOrEmpty(validationLabel.Text) ? appearance.Theme.Palette.Foreground : appearance.Theme.Semantics.Error;
        }

        public IterativeFieldOverwriteOptions Options { get; private set; }

        private void HandleConfigurationChanged(object sender, EventArgs e)
        {
            RefreshPreview();
        }

        private void HandleAdvancedGridSelectionChanged(object sender, EventArgs e)
        {
            if (advancedGrid.IsCurrentCellInEditMode)
                return;

            UpdateAdvancedRuleButtonState();
        }

        private void HandleAssignmentGridSelectionChanged(object sender, EventArgs e)
        {
            if (assignmentGrid.IsCurrentCellInEditMode)
                return;

            RefreshPreview();
        }

        private void HandleAdvancedDefinitionChanged(object sender, EventArgs e)
        {
            SynchronizeAdvancedRows();
            RefreshPreview();
        }

        private void HandleLayoutChanged(object sender, EventArgs e)
        {
            DialogLayoutSupport.UpdateMainLayoutWidth(scrollHost, mainLayout, 900);
            UpdateAssignmentGridHeight();
            UpdateWrappingLabelWidths();
        }

        private void UpdateAssignmentGridHeight()
        {
            if (assignmentGrid == null)
                return;

            int headerHeight = assignmentGrid.ColumnHeadersVisible ? assignmentGrid.ColumnHeadersHeight : 0;
            int rowsHeight = assignmentGrid.Rows.GetRowsHeight(DataGridViewElementStates.Visible);
            int desiredHeight = Math.Max(84, headerHeight + rowsHeight + 2);
            if (assignmentGrid.Height != desiredHeight)
                assignmentGrid.Height = desiredHeight;
        }

        private void UpdateWrappingLabelWidths()
        {
            DialogLayoutSupport.SetWrappingWidths(
                120,
                copyAsIsLabel,
                summaryLabel,
                validationLabel,
                advancedWarningLabel);
        }

        private void HandleAssignmentGridEdited(object sender, DataGridViewCellEventArgs e)
        {
            RefreshPreview();
        }

        private void HandleTargetGridEdited(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= targetRows.Count)
                return;

            if (e.ColumnIndex == NewValueColumnIndex)
            {
                var descriptor = GetSelectedPreviewDescriptor();
                if (descriptor != null)
                {
                    var row = targetRows[e.RowIndex];
                    var options = CreateOptions();
                    var contexts = IterativeFieldOverwritePlanner.BuildContexts(
                        MaterialFileTypeHelper.GetMaterialType(sourceState),
                        targetFiles,
                        options);
                    var context = contexts.FirstOrDefault(item => string.Equals(
                        MaterialFilePersistence.NormalizePath(item.TargetPath),
                        MaterialFilePersistence.NormalizePath(row.TargetPath),
                        StringComparison.OrdinalIgnoreCase));
                    string defaultValue = BuildDefaultResolvedValue(row, descriptor, options, context);
                    string currentValue = row.NewValue ?? string.Empty;
                    string normalizedPath = MaterialFilePersistence.NormalizePath(row.TargetPath);

                    if (string.Equals(currentValue, defaultValue, StringComparison.Ordinal))
                        fieldValueOverrides.Remove((normalizedPath, descriptor.Label));
                    else
                        fieldValueOverrides[(normalizedPath, descriptor.Label)] = currentValue;
                }
            }

            RefreshPreview();
        }

        private void TargetGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (targetGrid.IsCurrentCellDirty && targetGrid.CurrentCell is DataGridViewCheckBoxCell)
                targetGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void TargetGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= targetRows.Count)
                return;

            var row = targetRows[e.RowIndex];
            if (e.ColumnIndex == ApplyColumnIndex)
            {
                e.Cancel = !row.CanToggleApply;
                return;
            }

            if (e.ColumnIndex == NewValueColumnIndex)
                e.Cancel = !row.CanEditValue || GetSelectedPreviewDescriptor() == null;
        }

        private void TargetGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= targetRows.Count || e.ColumnIndex < 0)
                return;

            var row = targetRows[e.RowIndex];
            e.ToolTipText = e.ColumnIndex switch
            {
                CurrentValueColumnIndex => row.CurrentValue,
                NewValueColumnIndex => row.NewValue,
                7 => row.TargetPath,
                _ => row.StatusText
            };
        }

        private void AddRuleButton_Click(object sender, EventArgs e)
        {
            AddAdvancedRuleRow();
            RefreshPreview();
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
            RefreshPreview();
        }

        private void MoveRuleUpButton_Click(object sender, EventArgs e)
        {
            MoveAdvancedRule(-1);
        }

        private void MoveRuleDownButton_Click(object sender, EventArgs e)
        {
            MoveAdvancedRule(1);
        }

        private void AdvancedGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            AdvancedVariantDialogSupport.CommitComboBoxEditIfDirty(advancedGrid);
        }

        private void AdvancedGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            RefreshPreview();
        }

        private void AdvancedGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (suppressAdvancedRowEvents || e.RowIndex < 0)
                return;

            RefreshPreview();
        }

        private void AdvancedGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= advancedRuleRows.Count || e.ColumnIndex < 0)
                return;

            e.ToolTipText = AdvancedVariantDialogSupport.GetRuleCellTooltipText(
                advancedRuleRows[e.RowIndex],
                e.ColumnIndex,
                advancedLayerCountControls,
                "Leave blank to inherit from broader matching rows.");
        }

        private void AdvancedGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            AdvancedVariantDialogSupport.SuppressGridDataError(e);
        }

        private void AdvancedRuleRow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (suppressAdvancedRowEvents || advancedGrid.IsCurrentCellInEditMode)
                return;

            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (suppressRefresh)
                return;

            suppressRefresh = true;
            try
            {
                var options = CreateOptions();
                var advancedContexts = IterativeFieldOverwritePlanner.ResolveAdvancedContexts(options.AdvancedVariant);
                var contexts = IterativeFieldOverwritePlanner.BuildContexts(
                    MaterialFileTypeHelper.GetMaterialType(sourceState),
                    targetFiles,
                    options);
                var contextsByPath = contexts.ToDictionary(context => MaterialFilePersistence.NormalizePath(context.TargetPath), StringComparer.OrdinalIgnoreCase);
                var previewDescriptor = GetSelectedPreviewDescriptor();

                UpdateDynamicHeaders(previewDescriptor);

                foreach (var row in targetRows)
                {
                    contextsByPath.TryGetValue(MaterialFilePersistence.NormalizePath(row.TargetPath), out IterativeTargetContext context);
                    row.ApplyPreview(context);
                    row.CurrentValue = previewDescriptor != null
                        ? GetCurrentValueText(row.TargetPath, previewDescriptor)
                        : string.Empty;
                    row.NewValue = previewDescriptor != null
                        ? BuildResolvedValue(row, previewDescriptor, options, context)
                        : string.Empty;
                }

                IterativeTargetContext firstAppliedContext = contexts.FirstOrDefault(context => context.IsCompatibleType && context.WillApply);
                TargetRow firstAppliedRow = firstAppliedContext == null
                    ? null
                    : targetRows.FirstOrDefault(row => string.Equals(
                        MaterialFilePersistence.NormalizePath(row.TargetPath),
                        MaterialFilePersistence.NormalizePath(firstAppliedContext.TargetPath),
                        StringComparison.OrdinalIgnoreCase));

                foreach (var row in stringAssignmentRows)
                {
                    row.Preview = firstAppliedContext == null
                        ? string.Empty
                        : BuildDefaultResolvedValue(firstAppliedRow, row.Descriptor, options, firstAppliedContext);
                }

                UpdateAdvancedWarning(advancedContexts);
                UpdateAdvancedRuleButtonState();

                int compatibleCount = contexts.Count(context => context.IsCompatibleType);
                int selectedByIterationCount = contexts.Count(context => context.IsCompatibleType && context.SelectedByIteration);
                int appliedCount = contexts.Count(context => context.IsCompatibleType && context.WillApply);
                summaryLabel.Text = $"Will update {appliedCount} of {compatibleCount} compatible target(s). {selectedByIterationCount} target(s) are currently assigned advanced contexts.";

                validationLabel.Text = ValidateOptions(contexts, advancedContexts);
                okButton.Enabled = string.IsNullOrEmpty(validationLabel.Text);

                targetGrid.Refresh();
                assignmentGrid.Refresh();
                advancedGrid.Refresh();
            }
            finally
            {
                suppressRefresh = false;
            }
        }

        private string ValidateOptions(IReadOnlyList<IterativeTargetContext> contexts, IReadOnlyList<AdvancedVariantResolvedContext> advancedContexts)
        {
            int compatibleCount = contexts.Count(context => context.IsCompatibleType);
            if (compatibleCount == 0)
                return "No compatible target files are available for the current material type.";

            int selectedByIterationCount = contexts.Count(context => context.IsCompatibleType && context.SelectedByIteration);
            if (selectedByIterationCount == 0)
                return "Iteration settings do not select any compatible target files.";

            if (advancedContexts.Count == 0)
                return "Advanced variant layers must generate at least one context.";

            if (advancedContexts.Count != selectedByIterationCount)
                return $"Advanced variant contexts ({advancedContexts.Count}) must exactly match the compatible targets selected by iteration ({selectedByIterationCount}).";

            int willApplyCount = contexts.Count(context => context.IsCompatibleType && context.WillApply);
            if (willApplyCount == 0)
                return "All selected targets are disabled. Re-enable at least one Apply checkbox to continue.";

            return string.Empty;
        }

        private IterativeFieldOverwriteOptions CreateOptions()
        {
            var assignments = new List<IterativeFieldAssignment>();
            assignments.AddRange(stringAssignmentRows.Select(row => new IterativeFieldAssignment(row.Descriptor, row.Pattern ?? string.Empty)));
            if (grayscaleDescriptor != null)
            {
                assignments.Add(new IterativeFieldAssignment(
                    grayscaleDescriptor,
                    (float)grayscaleStartControl.Value,
                    (float)grayscaleStepControl.Value));
            }

            return new IterativeFieldOverwriteOptions
            {
                StartIndex = (int)startIndexControl.Value,
                Count = (int)countControl.Value,
                Step = (int)stepControl.Value,
                Assignments = assignments,
                Targets = targetRows.Select(row => new IterativeTargetOverride(row.TargetPath, string.Empty, row.ManualApplyEnabled)).ToArray(),
                FieldValueOverrides = fieldValueOverrides
                    .Select(item => new IterativeFieldValueOverride(item.Key.TargetPath, item.Key.FieldLabel, item.Value))
                    .ToArray(),
                AdvancedVariant = CreateAdvancedVariantOptions()
            };
        }

        private AdvancedVariantOptions CreateAdvancedVariantOptions()
        {
            var layers = BuildAdvancedLayers();
            if (layers.Count == 0)
                return null;

            AdvancedVariantRule[] rules = AdvancedVariantDialogSupport.BuildRules(advancedRuleRows, includeDefaultRuleWhenEmpty: true);

            return new AdvancedVariantOptions
            {
                Layers = layers,
                Rules = rules
            };
        }

        private IReadOnlyList<AdvancedVariantLayerDefinition> BuildAdvancedLayers()
        {
            return AdvancedVariantDialogSupport.BuildLayers(advancedLayerCountControls, "Layer{0}");
        }

        private void SynchronizeAdvancedRows()
        {
            UpdateAdvancedLayerColumnChoices();
            NormalizeAdvancedRuleRowsForLayerCounts();

            if (advancedRuleRows.Count > 0)
            {
                UpdateAdvancedRuleButtonState();
                return;
            }

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
            UpdateAdvancedRuleButtonState();
        }

        private void UpdateAdvancedRuleButtonState()
        {
            int selectedIndex = GetSelectedAdvancedRuleIndex();
            bool canModify = BuildAdvancedLayers().Count > 0;
            AdvancedVariantDialogSupport.UpdateRuleButtonState(
                addRuleButton,
                deleteRuleButton,
                moveRuleUpButton,
                moveRuleDownButton,
                selectedIndex,
                advancedRuleRows.Count,
                canModify);
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
            RefreshPreview();
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

        private void UpdateAdvancedWarning(IReadOnlyList<AdvancedVariantResolvedContext> advancedContexts)
        {
            if (advancedContexts.Count == 0)
            {
                advancedWarningLabel.Text = "0 advanced context(s) are currently available.";
                advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Empty);
                return;
            }

            IReadOnlyList<string> tooltipLines = AdvancedVariantDialogSupport.BuildFallbackTooltipLines(
                advancedContexts,
                includeLayerTokenFallbacks: true,
                out int fallbackContextCount);

            if (fallbackContextCount == 0)
            {
                advancedWarningLabel.Text = "0 advanced context(s) still rely on numeric token fallbacks.";
                advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Empty);
                return;
            }

            advancedWarningLabel.Text = $"{fallbackContextCount} advanced context(s) still rely on numeric token fallbacks.";
            advancedWarningToolTip.SetToolTip(advancedWarningLabel, string.Join(Environment.NewLine, tooltipLines));
        }

        private void UpdateDynamicHeaders(MaterialFieldDescriptor descriptor)
        {
            string fieldLabel = descriptor?.Label ?? "Value";
            targetGrid.Columns[CurrentValueColumnIndex].HeaderText = $"Current {fieldLabel}";
            targetGrid.Columns[NewValueColumnIndex].HeaderText = $"Overwritten {fieldLabel}";
        }

        private MaterialFieldDescriptor GetSelectedPreviewDescriptor()
        {
            if (assignmentGrid.CurrentRow?.DataBoundItem is StringAssignmentRow selectedRow)
                return selectedRow.Descriptor;

            if (stringAssignmentRows.Count > 0)
                return stringAssignmentRows[0].Descriptor;

            return grayscaleDescriptor;
        }

        private string GetCurrentValueText(string targetPath, MaterialFieldDescriptor descriptor)
        {
            if (descriptor == null)
                return string.Empty;

            string normalizedPath = MaterialFilePersistence.NormalizePath(targetPath);
            if (loadFailedPaths.Contains(normalizedPath))
                return "<load failed>";

            if (!materialCache.TryGetValue(normalizedPath, out BaseMaterialFile material))
            {
                if (!MaterialFilePersistence.TryLoadMaterial(targetPath, out material, out _, out _))
                {
                    loadFailedPaths.Add(normalizedPath);
                    return "<load failed>";
                }

                materialCache[normalizedPath] = material;
            }

            if (!descriptor.IsSupported(material))
                return "<unsupported>";

            try
            {
                return FormatValue(descriptor.GetValue(material));
            }
            catch
            {
                return "<unavailable>";
            }
        }

        private string BuildResolvedValue(TargetRow row, MaterialFieldDescriptor descriptor, IterativeFieldOverwriteOptions options, IterativeTargetContext context)
        {
            if (descriptor == null)
                return string.Empty;

            string normalizedPath = MaterialFilePersistence.NormalizePath(row.TargetPath);
            if (fieldValueOverrides.TryGetValue((normalizedPath, descriptor.Label), out string overrideValue))
                return overrideValue ?? string.Empty;

            return BuildDefaultResolvedValue(row, descriptor, options, context);
        }

        private string BuildDefaultResolvedValue(TargetRow row, MaterialFieldDescriptor descriptor, IterativeFieldOverwriteOptions options, IterativeTargetContext context)
        {
            if (descriptor == null)
                return string.Empty;

            if (context == null || !context.IsCompatibleType || !context.SelectedByIteration)
                return string.Empty;

            IterativeFieldAssignment assignment = (options.Assignments ?? Array.Empty<IterativeFieldAssignment>())
                .LastOrDefault(item => string.Equals(item.Descriptor?.Label, descriptor.Label, StringComparison.OrdinalIgnoreCase));

            if (assignment == null)
                return FormatValue(descriptor.GetValue(sourceState));

            if (assignment.IsNumericSequence)
            {
                float value = assignment.NumericStartValue.Value + assignment.NumericStepValue.Value * (context.AppliedOrdinal ?? 0);
                return value.ToString("0.###", CultureInfo.InvariantCulture);
            }

            return MaterialVariationTokenExpander.Expand(
                assignment.Pattern ?? string.Empty,
                context.AdvancedContext == null ? context.IndexValue : null,
                context);
        }

        private decimal GetInitialGreyscaleValue()
        {
            if (grayscaleDescriptor == null)
                return 0m;

            try
            {
                object value = grayscaleDescriptor.GetValue(sourceState);
                return value == null
                    ? 0m
                    : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0m;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            var options = CreateOptions();
            var advancedContexts = IterativeFieldOverwritePlanner.ResolveAdvancedContexts(options.AdvancedVariant);
            var contexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialFileTypeHelper.GetMaterialType(sourceState),
                targetFiles,
                options);
            string validationMessage = ValidateOptions(contexts, advancedContexts);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                MessageBox.Show(this, validationMessage, "Cannot Continue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Options = options;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static IEnumerable<TargetRow> CreateTargetRows(IEnumerable<string> paths)
        {
            return (paths ?? Array.Empty<string>())
                .Select(MaterialFilePersistence.NormalizePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new TargetRow(path, true));
        }

        private static string FormatValue(object value)
        {
            return value switch
            {
                null => string.Empty,
                float number => number.ToString("0.###", CultureInfo.InvariantCulture),
                double number => number.ToString("0.###", CultureInfo.InvariantCulture),
                decimal number => number.ToString("0.###", CultureInfo.InvariantCulture),
                bool flag => flag ? "True" : "False",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        private sealed class FocusAwareDataGridView : DataGridView
        {
            public ScrollableControl ScrollFallbackTarget { get; init; }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                if (Focused || ContainsFocus)
                {
                    base.OnMouseWheel(e);
                    return;
                }

                if (ScrollFallbackTarget?.VerticalScroll.Visible != true)
                {
                    base.OnMouseWheel(e);
                    return;
                }

                int wheelStep = Math.Max(1, SystemInformation.MouseWheelScrollLines) * 16;
                int delta = e.Delta > 0 ? -wheelStep : wheelStep;
                int currentValue = ScrollFallbackTarget.VerticalScroll.Value;
                int nextValue = Math.Max(
                    ScrollFallbackTarget.VerticalScroll.Minimum,
                    Math.Min(ScrollFallbackTarget.VerticalScroll.Maximum - ScrollFallbackTarget.VerticalScroll.LargeChange + 1, currentValue + delta));

                ScrollFallbackTarget.VerticalScroll.Value = nextValue;
                ScrollFallbackTarget.PerformLayout();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);

                if (!ContainsFocus)
                    Focus();
            }
        }

        private sealed class TargetRow : INotifyPropertyChanged
        {
            private bool manuallyEnabled;
            private bool isCompatibleType;
            private bool canToggleApply;
            private bool canEditValue;
            private bool willApply;
            private string sequenceText = string.Empty;
            private string index = string.Empty;
            private string indexNN = string.Empty;
            private string currentValue = string.Empty;
            private string newValue = string.Empty;
            private string statusText = string.Empty;

            public TargetRow(string targetPath, bool manuallyEnabled)
            {
                TargetPath = targetPath ?? string.Empty;
                this.manuallyEnabled = manuallyEnabled;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public string TargetPath { get; }

            public bool IsCompatibleType
            {
                get => isCompatibleType;
                private set => SetField(ref isCompatibleType, value, nameof(IsCompatibleType));
            }

            public bool CanToggleApply
            {
                get => canToggleApply;
                private set => SetField(ref canToggleApply, value, nameof(CanToggleApply));
            }

            public bool CanEditValue
            {
                get => canEditValue;
                private set => SetField(ref canEditValue, value, nameof(CanEditValue));
            }

            public bool ManualApplyEnabled => manuallyEnabled;

            public bool ApplyEnabled
            {
                get => manuallyEnabled;
                set
                {
                    if (manuallyEnabled == value)
                        return;

                    manuallyEnabled = value;
                    OnPropertyChanged(nameof(ApplyEnabled));
                }
            }

            public bool WillApply
            {
                get => willApply;
                private set => SetField(ref willApply, value, nameof(WillApply));
            }

            public string SequenceText
            {
                get => sequenceText;
                private set => SetField(ref sequenceText, value ?? string.Empty, nameof(SequenceText));
            }

            public string Index
            {
                get => index;
                private set => SetField(ref index, value ?? string.Empty, nameof(Index));
            }

            public string IndexNN
            {
                get => indexNN;
                private set => SetField(ref indexNN, value ?? string.Empty, nameof(IndexNN));
            }

            public string CurrentValue
            {
                get => currentValue;
                set => SetField(ref currentValue, value ?? string.Empty, nameof(CurrentValue));
            }

            public string NewValue
            {
                get => newValue;
                set => SetField(ref newValue, value ?? string.Empty, nameof(NewValue));
            }

            public string StatusText
            {
                get => statusText;
                private set => SetField(ref statusText, value ?? string.Empty, nameof(StatusText));
            }

            public void ApplyPreview(IterativeTargetContext context)
            {
                IsCompatibleType = context?.IsCompatibleType ?? false;
                bool selectedByIteration = context?.SelectedByIteration ?? false;
                CanToggleApply = IsCompatibleType && selectedByIteration;
                CanEditValue = IsCompatibleType && selectedByIteration;
                ApplyEnabled = context?.ManuallyEnabled ?? manuallyEnabled;
                WillApply = context?.WillApply ?? false;
                SequenceText = context?.AppliedSequence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                Index = context?.Index ?? string.Empty;
                IndexNN = context?.IndexNN ?? string.Empty;

                if (context == null)
                {
                    StatusText = "No preview context.";
                    return;
                }

                if (!context.IsCompatibleType)
                    StatusText = "Skipped incompatible material type.";
                else if (!context.SelectedByIteration)
                    StatusText = "Skipped by iteration settings.";
                else if (!context.ManuallyEnabled)
                    StatusText = "Skipped manually.";
                else
                    StatusText = "Will overwrite.";
            }

            private void SetField<T>(ref T field, T value, string propertyName)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return;

                field = value;
                OnPropertyChanged(propertyName);
            }

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private sealed class StringAssignmentRow : INotifyPropertyChanged
        {
            private string pattern;
            private string preview;

            public StringAssignmentRow(MaterialFieldDescriptor descriptor, string pattern)
            {
                Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
                FieldLabel = descriptor.Label;
                this.pattern = pattern ?? string.Empty;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public MaterialFieldDescriptor Descriptor { get; }
            public string FieldLabel { get; }

            public string Pattern
            {
                get => pattern;
                set => SetField(ref pattern, value ?? string.Empty, nameof(Pattern));
            }

            public string Preview
            {
                get => preview;
                set => SetField(ref preview, value ?? string.Empty, nameof(Preview));
            }

            private void SetField(ref string field, string value, string propertyName)
            {
                if (field == value)
                    return;

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
