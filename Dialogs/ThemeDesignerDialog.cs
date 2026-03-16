using Material_Editor.Controls;
using Material_Editor.Theming;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class ThemeDesignerDialog : ThemeAwareForm
    {
        private readonly ThemeDefinition seedTheme;
        private readonly string originalThemeId;
        private readonly bool originalThemeIsBuiltIn;
        private readonly List<ThemeColorEditorRow> colorEditors = new();

        private readonly TextBox themeIdTextBox;
        private readonly TextBox displayNameTextBox;
        private readonly Label validationLabel;
        private readonly Label builtInHintLabel;
        private readonly Button saveButton;

        private readonly Panel previewFrame;
        private readonly Panel previewRoot;
        private readonly MenuStrip previewMenuStrip;
        private readonly ListView previewListView;
        private readonly DataGridView previewGrid;
        private readonly ColorToggleCheckBox previewToggleOn;
        private readonly ColorToggleCheckBox previewToggleOff;
        private readonly CheckBox previewCheckBox;
        private readonly RadioButton previewRadioSelected;
        private readonly RadioButton previewRadioClear;
        private readonly TextBox previewPathTextBox;
        private readonly Label previewSuccessLabel;
        private readonly Label previewWarningLabel;
        private readonly Label previewErrorLabel;
        private readonly Label previewDirtyLabel;
        private readonly Label previewReadOnlyLabel;
        private readonly Label previewLoadErrorLabel;
        private readonly Label previewValidationLabel;

        private ThemeDefinition previewTheme;
        private string currentValidationMessage = string.Empty;
        private bool hasColorParseErrors;

        public ThemeDesignerDialog(ThemeDefinition theme)
        {
            seedTheme = theme ?? throw new ArgumentNullException(nameof(theme));
            originalThemeId = theme.Id;
            originalThemeIsBuiltIn = theme.IsBuiltIn;
            previewTheme = theme;

            Text = "Theme Designer";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1280, 860);
            MinimumSize = new Size(1120, 760);
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12)
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(rootLayout);

            var bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430f));
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            rootLayout.Controls.Add(bodyLayout, 0, 0);

            var editorScrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(0, 0, 4, 0)
            };
            bodyLayout.Controls.Add(editorScrollHost, 0, 0);

            var editorLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Margin = new Padding(0)
            };
            editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            editorScrollHost.Controls.Add(editorLayout);

            editorLayout.Controls.Add(CreateIntroLabel(), 0, 0);

            var metaGroup = CreateGroupBox("Theme Details");
            editorLayout.Controls.Add(metaGroup, 0, 1);

            var metaLayout = CreateEditorGrid();
            metaGroup.Controls.Add(metaLayout);

            metaLayout.Controls.Add(CreateFieldLabel("Theme ID:"), 0, 0);
            themeIdTextBox = CreateEditorTextBox(seedTheme.Id);
            themeIdTextBox.TextChanged += HandleEditorChanged;
            metaLayout.Controls.Add(themeIdTextBox, 1, 0);

            metaLayout.Controls.Add(CreateFieldLabel("Display Name:"), 0, 1);
            displayNameTextBox = CreateEditorTextBox(seedTheme.DisplayName);
            displayNameTextBox.TextChanged += HandleEditorChanged;
            metaLayout.Controls.Add(displayNameTextBox, 1, 1);

            builtInHintLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 8, 0, 0),
                Text = originalThemeIsBuiltIn
                    ? "Built-in themes are templates only. Change the Theme ID to save a copy."
                    : "Custom themes can be updated by saving with the same Theme ID."
            };
            metaLayout.Controls.Add(builtInHintLabel, 0, 2);
            metaLayout.SetColumnSpan(builtInHintLabel, 2);

            var paletteGroup = CreateGroupBox("Palette");
            editorLayout.Controls.Add(paletteGroup, 0, 2);
            var paletteLayout = CreateColorGrid();
            paletteGroup.Controls.Add(paletteLayout);
            AddColorEditor(paletteLayout, "Form Background", seedTheme.Palette.FormBackground);
            AddColorEditor(paletteLayout, "Control Background", seedTheme.Palette.ControlBackground);
            AddColorEditor(paletteLayout, "Panel Background", seedTheme.Palette.PanelBackground);
            AddColorEditor(paletteLayout, "Menu Background", seedTheme.Palette.MenuBackground);
            AddColorEditor(paletteLayout, "Foreground", seedTheme.Palette.Foreground);
            AddColorEditor(paletteLayout, "Accent", seedTheme.Palette.Accent, allowEmpty: true);

            var semanticGroup = CreateGroupBox("Semantic Colors");
            editorLayout.Controls.Add(semanticGroup, 0, 3);
            var semanticLayout = CreateColorGrid();
            semanticGroup.Controls.Add(semanticLayout);
            AddColorEditor(semanticLayout, "Success", seedTheme.Semantics.Success);
            AddColorEditor(semanticLayout, "Warning", seedTheme.Semantics.Warning);
            AddColorEditor(semanticLayout, "Error", seedTheme.Semantics.Error);
            AddColorEditor(semanticLayout, "Dirty", seedTheme.Semantics.Dirty);
            AddColorEditor(semanticLayout, "Read Only", seedTheme.Semantics.ReadOnly);
            AddColorEditor(semanticLayout, "Load Error", seedTheme.Semantics.LoadError);
            AddColorEditor(semanticLayout, "Validation", seedTheme.Semantics.Validation);
            AddColorEditor(semanticLayout, "Checkbox Off", seedTheme.Semantics.CheckboxOff);
            AddColorEditor(semanticLayout, "Selected Toggle", seedTheme.Semantics.SelectedToggle);

            previewFrame = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0)
            };
            bodyLayout.Controls.Add(previewFrame, 1, 0);

            var previewFrameLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            previewFrameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            previewFrameLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            previewFrameLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            previewFrame.Controls.Add(previewFrameLayout);

            previewMenuStrip = CreatePreviewMenuStrip();
            previewFrameLayout.Controls.Add(previewMenuStrip, 0, 0);

            var previewScrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Margin = new Padding(0)
            };
            previewFrameLayout.Controls.Add(previewScrollHost, 0, 1);

            previewRoot = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            previewScrollHost.Controls.Add(previewRoot);

            var previewLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(14),
                Margin = new Padding(0)
            };
            previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            previewRoot.Controls.Add(previewLayout);

            previewLayout.Controls.Add(CreatePreviewHeader("Live Preview"), 0, 0);
            previewLayout.Controls.Add(CreatePreviewHeader("Inputs"), 0, 1);
            previewLayout.Controls.Add(CreateInputsPreview(), 0, 2);
            previewLayout.Controls.Add(CreatePreviewHeader("Paths & Toggles"), 0, 3);
            previewLayout.Controls.Add(CreatePathAndTogglePreview(out previewToggleOn, out previewToggleOff, out previewCheckBox, out previewRadioSelected, out previewRadioClear, out previewPathTextBox), 0, 4);
            previewLayout.Controls.Add(CreatePreviewHeader("Lists"), 0, 5);
            previewLayout.Controls.Add(CreateListPreview(out previewListView, out previewGrid), 0, 6);
            previewLayout.Controls.Add(CreatePreviewHeader("Semantic States"), 0, 7);
            previewLayout.Controls.Add(CreateSemanticPreview(
                out previewSuccessLabel,
                out previewWarningLabel,
                out previewErrorLabel,
                out previewDirtyLabel,
                out previewReadOnlyLabel,
                out previewLoadErrorLabel,
                out previewValidationLabel), 0, 8);

            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 12, 0, 0)
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rootLayout.Controls.Add(footerLayout, 0, 1);

            validationLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 6, 12, 0)
            };
            footerLayout.Controls.Add(validationLabel, 0, 0);

            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            footerLayout.Controls.Add(buttonLayout, 1, 0);

            var cancelButton = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(8, 0, 0, 0)
            };
            buttonLayout.Controls.Add(cancelButton);

            saveButton = new Button
            {
                Text = "Save Theme",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(8, 0, 0, 0)
            };
            saveButton.Click += SaveButton_Click;
            buttonLayout.Controls.Add(saveButton);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            RefreshThemeState();
        }

        public string SavedThemeId { get; private set; } = string.Empty;

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
            builtInHintLabel.ForeColor = appearance.Theme.Semantics.Warning;

            foreach (ThemeColorEditorRow row in colorEditors)
                row.ApplyOuterAppearance(appearance);

            UpdateValidationPresentation();
            ApplyPreviewTheme(previewTheme);
        }

        private Control CreateInputsPreview()
        {
            var groupBox = CreateGroupBox("Editor Controls");
            groupBox.Margin = new Padding(0, 0, 0, 12);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            groupBox.Controls.Add(layout);

            layout.Controls.Add(CreateFieldLabel("Text:"), 0, 0);
            layout.Controls.Add(new TextBox
            {
                Dock = DockStyle.Fill,
                Text = "materials\\setdressing\\trim\\paintedmetal.bgsm",
                Margin = new Padding(0, 0, 0, 8)
            }, 1, 0);

            layout.Controls.Add(CreateFieldLabel("Read-only:"), 0, 1);
            layout.Controls.Add(new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = "BGSM version 2",
                Margin = new Padding(0, 0, 0, 8)
            }, 1, 1);

            layout.Controls.Add(CreateFieldLabel("Combo:"), 0, 2);
            var comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 8)
            };
            comboBox.Items.AddRange(new object[] { "Default", "Masked", "Additive" });
            comboBox.SelectedIndex = 1;
            layout.Controls.Add(comboBox, 1, 2);

            layout.Controls.Add(CreateFieldLabel("Numeric:"), 0, 3);
            layout.Controls.Add(new NumericUpDown
            {
                Dock = DockStyle.Left,
                DecimalPlaces = 2,
                Maximum = 999,
                Value = 12.75m,
                Margin = new Padding(0, 0, 0, 8),
                Width = 120
            }, 1, 3);

            layout.Controls.Add(CreateFieldLabel("Buttons:"), 0, 4);
            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            buttonLayout.Controls.Add(new Button
            {
                Text = "Apply",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 8, 0)
            });
            buttonLayout.Controls.Add(new Button
            {
                Text = "Reset",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            });
            layout.Controls.Add(buttonLayout, 1, 4);

            return groupBox;
        }

        private Control CreatePathAndTogglePreview(
            out ColorToggleCheckBox toggleOn,
            out ColorToggleCheckBox toggleOff,
            out CheckBox checkBox,
            out RadioButton radioSelected,
            out RadioButton radioClear,
            out TextBox pathTextBox)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var pathGroup = CreateGroupBox("Path Field");
            layout.Controls.Add(pathGroup, 0, 0);

            var pathLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0)
            };
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathGroup.Controls.Add(pathLayout);

            pathLayout.Controls.Add(CreateFieldLabel("Texture:"), 0, 0);
            pathTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = "Architecture\\DiamondCity\\Walls\\dc_wall_d.dds",
                Margin = new Padding(0, 0, 8, 0)
            };
            AppearanceApplicator.SetFontRole(pathTextBox, AppearanceFontRole.Monospace);
            pathLayout.Controls.Add(pathTextBox, 1, 0);
            pathLayout.Controls.Add(new Button
            {
                Text = "...",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(6, 0, 6, 0),
                Margin = new Padding(0)
            }, 2, 0);

            var toggleGroup = CreateGroupBox("Toggles");
            layout.Controls.Add(toggleGroup, 1, 0);

            var toggleLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0),
                WrapContents = false
            };
            toggleGroup.Controls.Add(toggleLayout);

            toggleOn = new ColorToggleCheckBox
            {
                Text = "Color Toggle On",
                Checked = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            toggleLayout.Controls.Add(toggleOn);

            toggleOff = new ColorToggleCheckBox
            {
                Text = "Color Toggle Off",
                Checked = false,
                Margin = new Padding(0, 0, 0, 6)
            };
            toggleLayout.Controls.Add(toggleOff);

            checkBox = new CheckBox
            {
                Text = "Standard Checkbox",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            toggleLayout.Controls.Add(checkBox);

            radioSelected = new RadioButton
            {
                Text = "Selected Toggle",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            toggleLayout.Controls.Add(radioSelected);

            radioClear = new RadioButton
            {
                Text = "Unselected Toggle",
                Checked = false,
                AutoSize = true,
                Margin = new Padding(0)
            };
            toggleLayout.Controls.Add(radioClear);

            return layout;
        }

        private Control CreateListPreview(out ListView listView, out DataGridView grid)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));

            var listGroup = CreateGroupBox("List View");
            layout.Controls.Add(listGroup, 0, 0);

            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Margin = new Padding(12, 10, 12, 12),
                Height = 180
            };
            listView.Columns.Add("Field", 160);
            listView.Columns.Add("Value", 220);
            listView.Items.Add(new ListViewItem(new[] { "Diffuse Texture", "Textures\\metal\\sheet_d.dds" }));
            listView.Items.Add(new ListViewItem(new[] { "Alpha", "0.45" }));
            listView.Items.Add(new ListViewItem(new[] { "Lighting", "Enabled" }));
            listGroup.Controls.Add(listView);

            var gridGroup = CreateGroupBox("Data Grid");
            layout.Controls.Add(gridGroup, 1, 0);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RowHeadersVisible = false,
                Margin = new Padding(12, 10, 12, 12),
                Height = 180
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "State", HeaderText = "State", Frozen = true, Width = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Enabled", Width = 70 });
            grid.Rows.Add("Normal", "Painted metal", true);
            grid.Rows.Add("Read-only", "Version locked", false);
            grid.Rows.Add("Validation", "Missing token", true);
            grid.Rows.Add("Dirty", "Changed diffuse path", true);
            grid.Rows.Add("Load Error", "Could not load source", false);
            gridGroup.Controls.Add(grid);

            return layout;
        }

        private Control CreateSemanticPreview(
            out Label successLabel,
            out Label warningLabel,
            out Label errorLabel,
            out Label dirtyLabel,
            out Label readOnlyLabel,
            out Label loadErrorLabel,
            out Label validationStateLabel)
        {
            var groupBox = CreateGroupBox("Semantic Colors");
            groupBox.Margin = new Padding(0);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            groupBox.Controls.Add(layout);

            successLabel = CreateStateLabel("Success text");
            warningLabel = CreateStateLabel("Warning text");
            errorLabel = CreateStateLabel("Error text");
            dirtyLabel = CreateStateLabel("Dirty background");
            readOnlyLabel = CreateStateLabel("Read-only background");
            loadErrorLabel = CreateStateLabel("Load error background");
            validationStateLabel = CreateStateLabel("Validation background");

            layout.Controls.Add(successLabel, 0, 0);
            layout.Controls.Add(warningLabel, 1, 0);
            layout.Controls.Add(errorLabel, 0, 1);
            layout.Controls.Add(dirtyLabel, 1, 1);
            layout.Controls.Add(readOnlyLabel, 0, 2);
            layout.Controls.Add(loadErrorLabel, 1, 2);
            layout.Controls.Add(validationStateLabel, 0, 3);

            return groupBox;
        }

        private static MenuStrip CreatePreviewMenuStrip()
        {
            var menuStrip = new MenuStrip
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Open...");
            fileMenu.DropDownItems.Add("Save");
            fileMenu.DropDownItems.Add("Close");

            var toolsMenu = new ToolStripMenuItem("Tools");
            toolsMenu.DropDownItems.Add("Generate Variations...");
            toolsMenu.DropDownItems.Add("Bulk Material Editor...");

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("About");

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, toolsMenu, helpMenu });
            return menuStrip;
        }

        private void AddColorEditor(TableLayoutPanel layout, string label, Color initialColor, bool allowEmpty = false)
        {
            int rowIndex = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var editor = new ThemeColorEditorRow(label, initialColor, allowEmpty, HandleEditorChanged);
            colorEditors.Add(editor);

            layout.Controls.Add(editor.Label, 0, rowIndex);
            layout.Controls.Add(editor.SwatchButton, 1, rowIndex);
            layout.Controls.Add(editor.HexTextBox, 2, rowIndex);
            if (editor.EmptyCheckBox != null)
                layout.Controls.Add(editor.EmptyCheckBox, 3, rowIndex);
        }

        private void HandleEditorChanged(object sender, EventArgs e)
        {
            RefreshThemeState();
        }

        private void RefreshThemeState()
        {
            hasColorParseErrors = false;

            foreach (ThemeColorEditorRow row in colorEditors)
            {
                row.RefreshFromText();
                hasColorParseErrors |= row.HasParseError;
            }

            if (!hasColorParseErrors)
                previewTheme = BuildEditableTheme().ToThemeDefinition();

            ThemeEditorValidationResult validation = ThemeEditorValidator.ValidateForSave(
                BuildEditableTheme(),
                ThemeService.AvailableThemes,
                originalThemeId,
                originalThemeIsBuiltIn);

            if (hasColorParseErrors)
            {
                ThemeColorEditorRow firstInvalid = colorEditors.First(row => row.HasParseError);
                currentValidationMessage = $"Invalid color for '{firstInvalid.LabelText}'. Use #RRGGBB or #AARRGGBB.";
                saveButton.Enabled = false;
            }
            else
            {
                currentValidationMessage = validation.IsValid
                    ? GetSavePathMessage(themeIdTextBox.Text)
                    : validation.Message;
                saveButton.Enabled = validation.IsValid;
            }

            UpdateValidationPresentation();
            ApplyPreviewTheme(previewTheme);
        }

        private void UpdateValidationPresentation()
        {
            validationLabel.Text = currentValidationMessage;
            if (!IsHandleCreated)
                return;

            if (string.IsNullOrWhiteSpace(currentValidationMessage))
                validationLabel.ForeColor = ActiveTheme.Palette.Foreground;
            else if (saveButton.Enabled)
                validationLabel.ForeColor = ActiveTheme.Semantics.Success;
            else
                validationLabel.ForeColor = ActiveTheme.Semantics.Error;
        }

        private EditableThemeDefinition BuildEditableTheme()
        {
            var editable = new EditableThemeDefinition
            {
                ThemeId = themeIdTextBox.Text.Trim(),
                DisplayName = displayNameTextBox.Text.Trim()
            };

            editable.Palette.FormBackground = FindEditor("Form Background").GetColorValue();
            editable.Palette.ControlBackground = FindEditor("Control Background").GetColorValue();
            editable.Palette.PanelBackground = FindEditor("Panel Background").GetColorValue();
            editable.Palette.MenuBackground = FindEditor("Menu Background").GetColorValue();
            editable.Palette.Foreground = FindEditor("Foreground").GetColorValue();
            editable.Palette.Accent = FindEditor("Accent").GetColorValue();

            editable.Semantics.Success = FindEditor("Success").GetColorValue();
            editable.Semantics.Warning = FindEditor("Warning").GetColorValue();
            editable.Semantics.Error = FindEditor("Error").GetColorValue();
            editable.Semantics.Dirty = FindEditor("Dirty").GetColorValue();
            editable.Semantics.ReadOnly = FindEditor("Read Only").GetColorValue();
            editable.Semantics.LoadError = FindEditor("Load Error").GetColorValue();
            editable.Semantics.Validation = FindEditor("Validation").GetColorValue();
            editable.Semantics.CheckboxOff = FindEditor("Checkbox Off").GetColorValue();
            editable.Semantics.SelectedToggle = FindEditor("Selected Toggle").GetColorValue();

            return editable;
        }

        private ThemeColorEditorRow FindEditor(string label)
        {
            return colorEditors.First(editor => string.Equals(editor.LabelText, label, StringComparison.Ordinal));
        }

        private string GetSavePathMessage(string themeId)
        {
            string trimmed = themeId?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(trimmed)
                ? string.Empty
                : $"Will save to Themes\\{trimmed}.xml";
        }

        private void ApplyPreviewTheme(ThemeDefinition theme)
        {
            if (theme == null || previewRoot.IsDisposed)
                return;

            var previewAppearance = new AppearanceDefinition(theme, ActiveAppearance?.Font ?? Font ?? SystemFonts.MessageBoxFont);
            previewFrame.BackColor = theme.Palette.FormBackground;
            previewRoot.BackColor = theme.Palette.FormBackground;

            AppearanceApplicator.ApplyToContainer(previewRoot, previewAppearance, theme.Palette.FormBackground);
            AppearanceApplicator.ApplyToToolStrip(previewMenuStrip, previewAppearance);

            previewToggleOn.ApplyTheme(theme, previewToggleOn.Parent?.BackColor ?? theme.Palette.ControlBackground);
            previewToggleOff.ApplyTheme(theme, previewToggleOff.Parent?.BackColor ?? theme.Palette.ControlBackground);

            if (previewListView.Items.Count > 1)
            {
                previewListView.Items[1].Selected = true;
                previewListView.FocusedItem = previewListView.Items[1];
            }

            ApplyPreviewGridState(theme);

            previewSuccessLabel.ForeColor = theme.Semantics.Success;
            previewWarningLabel.ForeColor = theme.Semantics.Warning;
            previewErrorLabel.ForeColor = theme.Semantics.Error;

            ApplyStateLabel(previewDirtyLabel, ThemeApplicator.GetDirtyBackground(theme), theme.Palette.Foreground);
            ApplyStateLabel(previewReadOnlyLabel, ThemeApplicator.GetReadOnlyBackground(theme), theme.Palette.Foreground);
            ApplyStateLabel(previewLoadErrorLabel, ThemeApplicator.GetLoadErrorBackground(theme), theme.Palette.Foreground);
            ApplyStateLabel(previewValidationLabel, ThemeApplicator.GetValidationBackground(theme), theme.Palette.Foreground);
        }

        private void ApplyPreviewGridState(ThemeDefinition theme)
        {
            if (previewGrid.Columns.Count == 0 || previewGrid.Rows.Count < 5)
                return;

            previewGrid.ClearSelection();

            if (previewGrid.Columns["State"] is DataGridViewColumn frozenColumn)
            {
                frozenColumn.Frozen = true;
                frozenColumn.DefaultCellStyle.BackColor = ThemeApplicator.GetFrozenColumnBackground(theme);
                frozenColumn.DefaultCellStyle.ForeColor = theme.Palette.Foreground;
            }

            ResetPreviewGridRow(previewGrid.Rows[0]);

            DataGridViewRow readOnlyRow = previewGrid.Rows[1];
            ResetPreviewGridRow(readOnlyRow);
            readOnlyRow.Cells["Value"].Style.BackColor = ThemeApplicator.GetReadOnlyBackground(theme);
            readOnlyRow.Cells["Value"].ReadOnly = true;

            DataGridViewRow validationRow = previewGrid.Rows[2];
            ResetPreviewGridRow(validationRow);
            validationRow.Cells["Value"].Style.BackColor = ThemeApplicator.GetValidationBackground(theme);

            DataGridViewRow dirtyRow = previewGrid.Rows[3];
            ResetPreviewGridRow(dirtyRow);
            dirtyRow.DefaultCellStyle.BackColor = ThemeApplicator.GetDirtyBackground(theme);
            dirtyRow.DefaultCellStyle.ForeColor = theme.Palette.Foreground;

            DataGridViewRow loadErrorRow = previewGrid.Rows[4];
            ResetPreviewGridRow(loadErrorRow);
            loadErrorRow.DefaultCellStyle.BackColor = ThemeApplicator.GetLoadErrorBackground(theme);
            loadErrorRow.DefaultCellStyle.ForeColor = theme.Palette.Foreground;

            previewGrid.CurrentCell = previewGrid.Rows[0].Cells["Value"];
            previewGrid.Rows[0].Cells["Value"].Selected = true;
        }

        private void ResetPreviewGridRow(DataGridViewRow row)
        {
            row.DefaultCellStyle.BackColor = Color.Empty;
            row.DefaultCellStyle.ForeColor = Color.Empty;
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.Style.BackColor = Color.Empty;
                cell.Style.ForeColor = Color.Empty;
            }
        }

        private static void ApplyStateLabel(Label label, Color backColor, Color foreColor)
        {
            label.BackColor = backColor;
            label.ForeColor = foreColor;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            RefreshThemeState();
            if (!saveButton.Enabled)
                return;

            ThemeDefinition theme = BuildEditableTheme().ToThemeDefinition();
            ThemeCatalog.SaveTheme(theme);
            ThemeService.ReloadThemes();

            SavedThemeId = theme.Id;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label CreateIntroLabel()
        {
            return new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 12),
                Text = "Edit the theme values on the left and use the live preview on the right to see how menus, controls, grids, and semantic states will look."
            };
        }

        private static GroupBox CreateGroupBox(string text)
        {
            return new GroupBox
            {
                Text = text,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 12)
            };
        }

        private static TableLayoutPanel CreateEditorGrid()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            return layout;
        }

        private static TableLayoutPanel CreateColorGrid()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                Padding = new Padding(12, 10, 12, 12),
                Margin = new Padding(0),
                RowCount = 0
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return layout;
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 4, 10, 8),
                Text = text
            };
        }

        private static TextBox CreateEditorTextBox(string text)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Text = text ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };
        }

        private static Label CreatePreviewHeader(string text)
        {
            var label = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 6),
                Text = text
            };
            AppearanceApplicator.SetFontRole(label, AppearanceFontRole.Bold);
            return label;
        }

        private static Label CreateStateLabel(string text)
        {
            return new Label
            {
                AutoSize = false,
                Width = 220,
                Height = 30,
                BorderStyle = BorderStyle.FixedSingle,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0, 0, 8, 8)
            };
        }

        private sealed class ThemeColorEditorRow
        {
            private readonly bool allowEmpty;
            private readonly EventHandler changedHandler;

            public ThemeColorEditorRow(string labelText, Color initialColor, bool allowEmpty, EventHandler changedHandler)
            {
                this.allowEmpty = allowEmpty;
                this.changedHandler = changedHandler;

                LabelText = labelText;
                CurrentColor = initialColor;
                UseEmpty = allowEmpty && initialColor.IsEmpty;

                Label = new Label
                {
                    AutoSize = true,
                    Anchor = AnchorStyles.Left | AnchorStyles.Top,
                    Margin = new Padding(0, 4, 10, 8),
                    Text = labelText + ":"
                };

                SwatchButton = new Button
                {
                    Width = 48,
                    Height = 24,
                    Margin = new Padding(0, 0, 8, 8),
                    UseVisualStyleBackColor = false
                };
                SwatchButton.Click += SwatchButton_Click;

                HexTextBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 0, 8, 8),
                    Text = UseEmpty ? string.Empty : ThemeColorSerialization.Format(initialColor)
                };
                HexTextBox.TextChanged += HexTextBox_TextChanged;

                if (allowEmpty)
                {
                    EmptyCheckBox = new CheckBox
                    {
                        AutoSize = true,
                        Margin = new Padding(0, 3, 0, 8),
                        Text = "Empty",
                        Checked = UseEmpty
                    };
                    EmptyCheckBox.CheckedChanged += EmptyCheckBox_CheckedChanged;
                }

                RefreshFromText();
            }

            public string LabelText { get; }
            public Label Label { get; }
            public Button SwatchButton { get; }
            public TextBox HexTextBox { get; }
            public CheckBox EmptyCheckBox { get; }
            public Color CurrentColor { get; private set; }
            public bool UseEmpty { get; private set; }
            public bool HasParseError { get; private set; }

            public void RefreshFromText()
            {
                UseEmpty = allowEmpty && (EmptyCheckBox?.Checked ?? false);
                if (UseEmpty)
                {
                    HasParseError = false;
                    CurrentColor = Color.Empty;
                    HexTextBox.Enabled = false;
                    SwatchButton.Enabled = false;
                    UpdateSwatchButton();
                    return;
                }

                HexTextBox.Enabled = true;
                SwatchButton.Enabled = true;

                if (ThemeColorSerialization.TryParse(HexTextBox.Text, out Color parsedColor))
                {
                    CurrentColor = parsedColor;
                    HasParseError = false;
                }
                else
                {
                    HasParseError = !string.IsNullOrWhiteSpace(HexTextBox.Text);
                }

                UpdateSwatchButton();
            }

            public Color GetColorValue()
            {
                return UseEmpty ? Color.Empty : CurrentColor;
            }

            public void ApplyOuterAppearance(AppearanceDefinition appearance)
            {
                if (appearance == null)
                    return;

                Label.Font = appearance.Font;
                HexTextBox.Font = appearance.Font;
                if (EmptyCheckBox != null)
                    EmptyCheckBox.Font = appearance.Font;

                UpdateSwatchButton();
                if (HasParseError)
                    HexTextBox.BackColor = ThemeApplicator.GetValidationBackground(appearance.Theme);
            }

            private void SwatchButton_Click(object sender, EventArgs e)
            {
                using var colorDialog = new ColorDialog
                {
                    Color = CurrentColor.IsEmpty ? Color.White : CurrentColor,
                    FullOpen = true
                };

                if (colorDialog.ShowDialog() != DialogResult.OK)
                    return;

                CurrentColor = colorDialog.Color;
                HexTextBox.Text = ThemeColorSerialization.Format(CurrentColor);
                HasParseError = false;
                UpdateSwatchButton();
                changedHandler?.Invoke(this, EventArgs.Empty);
            }

            private void HexTextBox_TextChanged(object sender, EventArgs e)
            {
                RefreshFromText();
                changedHandler?.Invoke(this, EventArgs.Empty);
            }

            private void EmptyCheckBox_CheckedChanged(object sender, EventArgs e)
            {
                RefreshFromText();
                changedHandler?.Invoke(this, EventArgs.Empty);
            }

            private void UpdateSwatchButton()
            {
                HexTextBox.BackColor = SystemColors.Window;

                if (UseEmpty)
                {
                    SwatchButton.Text = "Auto";
                    SwatchButton.BackColor = SystemColors.ControlDark;
                    SwatchButton.ForeColor = SystemColors.ControlText;
                    return;
                }

                SwatchButton.Text = string.Empty;
                SwatchButton.BackColor = CurrentColor;
                SwatchButton.ForeColor = GetContrastColor(CurrentColor);
                if (HasParseError)
                    HexTextBox.BackColor = Color.MistyRose;
            }

            private static Color GetContrastColor(Color color)
            {
                int luminance = (color.R * 299) + (color.G * 587) + (color.B * 114);
                return luminance >= 140000 ? Color.Black : Color.White;
            }
        }
    }
}
