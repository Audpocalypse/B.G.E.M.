using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor
{
    internal sealed class VariationGeneratorDialog : Form
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
        private readonly TableLayoutPanel mainLayout;
        private readonly DataGridView fieldGrid;
        private readonly NumericUpDown startIndexControl;
        private readonly NumericUpDown countControl;
        private readonly NumericUpDown stepControl;
        private readonly TextBox outputPatternTextBox;
        private readonly TextBox outputPreviewTextBox;
        private readonly TextBox fieldPreviewTextBox;
        private readonly Label headerLabel;
        private readonly Label introLabel;
        private readonly Label outputHelpLabel;
        private readonly Label indexHelpLabel;
        private readonly Label fieldHelpLabel;
        private readonly Label summaryLabel;
        private readonly Label validationLabel;
        private readonly Button okButton;
        private readonly CheckBox greyscaleModeCheckBox;
        private readonly NumericUpDown greyscaleStartControl;
        private readonly NumericUpDown greyscaleStepControl;
        private readonly Label greyscaleNoteLabel;

        public VariationGeneratorDialog(
            IReadOnlyList<MaterialFieldDescriptor> descriptors,
            IReadOnlyList<string> currentValues,
            string suggestedOutputPattern,
            ThemePalette palette,
            UITheme theme)
        {
            if (descriptors == null)
                throw new ArgumentNullException(nameof(descriptors));

            Text = "Generate Variations";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(940, 860);
            MinimumSize = new Size(840, 760);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            SuspendLayout();

            mainLayout = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 18,
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int index = 0; index < mainLayout.RowCount; index++)
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles[13] = new RowStyle(SizeType.Percent, 100f);
            Controls.Add(mainLayout);

            headerLabel = CreateWrappingLabel("Generate multiple BGSM files from the open material template.", new Padding(0, 0, 0, 6));
            headerLabel.Font = new Font(Font, FontStyle.Bold);
            mainLayout.Controls.Add(headerLabel, 0, 0);

            introLabel = CreateWrappingLabel(
                "Follow the steps below: name the outputs, choose the index range, then enable any texture slots you want to rewrite.",
                new Padding(0, 0, 0, 12));
            mainLayout.Controls.Add(introLabel, 0, 1);

            var outputStepLabel = CreateSectionHeader("1. Output naming", new Padding(0, 0, 0, 4));
            mainLayout.Controls.Add(outputStepLabel, 0, 2);

            outputHelpLabel = CreateWrappingLabel("Use {index} or {index:00}. Example: armor_{index:00}.bgsm", new Padding(0, 0, 0, 6));
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

            mainLayout.Controls.Add(CreateCaptionLabel("Output preview", new Padding(0, 0, 0, 4)), 0, 5);

            outputPreviewTextBox = CreatePreviewTextBox(52, new Padding(0, 0, 0, 12));
            mainLayout.Controls.Add(outputPreviewTextBox, 0, 6);

            var indexStepLabel = CreateSectionHeader("2. Index range", new Padding(0, 0, 0, 4));
            mainLayout.Controls.Add(indexStepLabel, 0, 7);

            indexHelpLabel = CreateWrappingLabel("Example: start 1, count 12, step 1 generates 1 through 12.", new Padding(0, 0, 0, 6));
            mainLayout.Controls.Add(indexHelpLabel, 0, 8);

            var rangeGroup = new GroupBox
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
            rangeGroup.Controls.Add(rangeLayout);
            mainLayout.Controls.Add(rangeGroup, 0, 9);

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

            greyscaleModeCheckBox = new CheckBox
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
            mainLayout.Controls.Add(greyscaleGroup, 0, 10);

            var fieldStepLabel = CreateSectionHeader("3. Fields to vary", new Padding(0, 0, 0, 4));
            mainLayout.Controls.Add(fieldStepLabel, 0, 11);

            fieldHelpLabel = CreateWrappingLabel(
                "Checked rows replace the template value. Unchecked rows keep the original texture path unchanged. Example: textures\\armor_{index:00}.dds",
                new Padding(0, 0, 0, 8));
            mainLayout.Controls.Add(fieldHelpLabel, 0, 12);

            fieldRows = new BindingList<FieldRow>(CreateFieldRows(descriptors, currentValues).ToList());
            foreach (var row in fieldRows)
                row.PropertyChanged += FieldRow_PropertyChanged;

            fieldGrid = new DataGridView
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

            fieldGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(FieldRow.Enabled),
                HeaderText = "Apply",
                Width = 60,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            });
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
            fieldGrid.DataSource = fieldRows;
            var gridHost = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };
            gridHost.Controls.Add(fieldGrid);
            mainLayout.Controls.Add(gridHost, 0, 13);

            mainLayout.Controls.Add(CreateCaptionLabel("Field preview", new Padding(0, 0, 0, 4)), 0, 14);

            fieldPreviewTextBox = CreatePreviewTextBox(44, new Padding(0, 0, 0, 8));
            mainLayout.Controls.Add(fieldPreviewTextBox, 0, 15);

            var previewStepLabel = CreateSectionHeader("4. Preview / generate", new Padding(0, 0, 0, 4));
            mainLayout.Controls.Add(previewStepLabel, 0, 16);

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
            validationLabel.ForeColor = Color.Firebrick;
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
            mainLayout.Controls.Add(footerLayout, 0, 17);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            if (fieldGrid.Rows.Count > 0)
            {
                fieldGrid.Rows[0].Selected = true;
                fieldGrid.CurrentCell = fieldGrid.Rows[0].Cells[1];
            }

            Load += HandleLayoutChanged;
            SizeChanged += HandleLayoutChanged;
            DialogThemeHelper.Apply(this, palette, theme);
            UpdateWrappingLabelWidths();
            RefreshUiState();
            ResumeLayout(true);
        }

        private Label CreateSectionHeader(string text, Padding margin)
        {
            var label = CreateWrappingLabel(text, margin);
            label.Font = new Font(Font, FontStyle.Bold);
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
            UpdateWrappingLabelWidths();
        }

        private void UpdateWrappingLabelWidths()
        {
            SetWrappingWidth(headerLabel);
            SetWrappingWidth(introLabel);
            SetWrappingWidth(outputHelpLabel);
            SetWrappingWidth(indexHelpLabel);
            SetWrappingWidth(greyscaleNoteLabel);
            SetWrappingWidth(fieldHelpLabel);
            SetWrappingWidth(summaryLabel);
            SetWrappingWidth(validationLabel);
        }

        private static void SetWrappingWidth(Label label)
        {
            if (label?.Parent == null)
                return;

            int availableWidth = label.Parent.ClientSize.Width;
            if (label.Parent is ScrollableControl scrollableControl)
                availableWidth -= scrollableControl.Padding.Horizontal;

            availableWidth = Math.Max(100, availableWidth - label.Margin.Horizontal);
            if (label.MaximumSize.Width != availableWidth)
                label.MaximumSize = new Size(availableWidth, 0);
        }

        public MaterialVariationOptions Options { get; private set; }

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
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select a folder for generated BGSM files."
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            outputPatternTextBox.Text = Path.Combine(dialog.SelectedPath, "variation_{index}.bgsm");
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
                    row.Pattern = BuildReplacementPatternFromOutputNaming(row.CurrentValue);
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

        private void HandleConfigurationChanged(object sender, EventArgs e)
        {
            RefreshUiState();
        }

        private void RefreshUiState()
        {
            bool greyscaleEnabled = greyscaleModeCheckBox.Checked;
            greyscaleStartControl.Enabled = greyscaleEnabled;
            greyscaleStepControl.Enabled = greyscaleEnabled;

            var options = CreateOptions();
            string message = ValidateOptions(options, out var hasWarnings);

            outputPreviewTextBox.Text = BuildOutputPreview(options);
            fieldPreviewTextBox.Text = BuildFieldPreview((int)startIndexControl.Value);
            summaryLabel.Text = $"Will generate {options.Count} files starting at index {options.StartIndex} with step {options.Step}.";
            validationLabel.Text = message;
            validationLabel.ForeColor = hasWarnings ? Color.DarkOrange : Color.Firebrick;
            okButton.Enabled = string.IsNullOrEmpty(message) || hasWarnings;
        }

        private string BuildReplacementPatternFromOutputNaming(string templateValue)
        {
            var outputBase = outputPatternTextBox.Text ?? string.Empty;
            outputBase = outputBase.Replace('\\', '/');

            const string materialsToken = "materials/";
            int materialsIndex = outputBase.IndexOf(materialsToken, StringComparison.OrdinalIgnoreCase);
            if (materialsIndex >= 0)
                outputBase = outputBase.Substring(materialsIndex + materialsToken.Length);

            if (outputBase.EndsWith(".bgsm", StringComparison.OrdinalIgnoreCase) ||
                outputBase.EndsWith(".bgem", StringComparison.OrdinalIgnoreCase))
            {
                outputBase = outputBase.Substring(0, outputBase.Length - 5);
            }

            var normalizedTemplate = (templateValue ?? string.Empty).Replace('\\', '/');
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
                Fields = assignments
            };
        }

        private string ValidateOptions(MaterialVariationOptions options, out bool hasWarnings)
        {
            hasWarnings = false;

            if (string.IsNullOrWhiteSpace(options.OutputPattern))
                return "Enter an output path pattern before generating.";

            if (!MaterialVariationGenerator.ContainsIndexPlaceholder(options.OutputPattern))
                return "The output path must include an {index} placeholder.";

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
                return $"The current index settings would generate duplicate output paths, for example: {duplicates}";

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
                return "Enter a path pattern such as armor_{index:00}.bgsm.";

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

        private string BuildFieldPreview(int previewIndex)
        {
            var selectedRow = fieldGrid.CurrentRow?.DataBoundItem as FieldRow
                ?? fieldRows.FirstOrDefault(row => row.Enabled);

            if (selectedRow == null)
                return "Select a texture slot to preview its replacement.";

            if (!selectedRow.Enabled || string.IsNullOrWhiteSpace(selectedRow.Pattern))
                return $"{selectedRow.Label} stays as {selectedRow.CurrentValue}";

            var previewValue = MaterialVariationGenerator.PreviewFieldValue(selectedRow.Pattern, previewIndex);
            return $"{selectedRow.Label} -> {previewValue}";
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
