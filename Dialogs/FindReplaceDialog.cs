using Material_Editor.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class FindReplaceDialog : ThemeAwareForm
    {
        private sealed class SelectionRadioButton : RadioButton
        {
            protected override bool ShowFocusCues => false;
        }

        private readonly IReadOnlyList<MaterialFieldDescriptor> availableDescriptors;
        private readonly bool replaceMode;
        private readonly Action<BulkFindReplaceAction, BulkFindReplaceRequest> executeAction;
        private readonly Dictionary<BulkFindScope, SelectionRadioButton> scopeButtons = new();
        private readonly Dictionary<BulkFindValueType, SelectionRadioButton> valueTypeButtons = new();
        private readonly TextBox findTextBox;
        private readonly TextBox replaceTextBox;
        private readonly ComboBox findBooleanComboBox;
        private readonly ComboBox replaceBooleanComboBox;
        private readonly CheckedListBox fieldListBox;
        private readonly Label fieldsLabel;
        private readonly Label findValueLabel;
        private readonly Label replaceValueLabel;
        private readonly Panel replaceValuePanel;
        private readonly FlowLayoutPanel findActionsPanel;
        private readonly FlowLayoutPanel replaceActionsPanel;
        private readonly FlowLayoutPanel filterActionsPanel;
        private readonly Button findPreviousButton;
        private readonly Button findNextButton;
        private readonly Button replaceButton;
        private readonly Button replaceAllButton;
        private readonly Button applyFilterButton;

        private BulkFindScope selectedScope;
        private BulkFindValueType selectedValueType;

        public FindReplaceDialog(
            IReadOnlyList<MaterialFieldDescriptor> availableDescriptors,
            BulkFindReplaceRequest initialRequest,
            bool replaceMode,
            Action<BulkFindReplaceAction, BulkFindReplaceRequest> executeAction)
        {
            this.availableDescriptors = availableDescriptors ?? Array.Empty<MaterialFieldDescriptor>();
            this.replaceMode = replaceMode;
            this.executeAction = executeAction;

            Text = replaceMode ? "Find and Replace" : "Find";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = replaceMode ? new Size(690, 480) : new Size(620, 440);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var request = (initialRequest ?? new BulkFindReplaceRequest()).Clone();

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(mainLayout);

            var optionsGroup = new GroupBox
            {
                Text = "Search Options",
                Dock = DockStyle.Fill,
                Height = 104,
                Margin = new Padding(0, 0, 0, 10)
            };
            mainLayout.Controls.Add(optionsGroup, 0, 0);

            var optionsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(8, 6, 8, 8)
            };
            optionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            optionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsGroup.Controls.Add(optionsLayout);

            var scopeLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 10, 8),
                Text = "Scope:"
            };
            optionsLayout.Controls.Add(scopeLabel, 0, 0);

            var scopePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            optionsLayout.Controls.Add(scopePanel, 1, 0);

            scopePanel.Controls.Add(CreateScopeButton(BulkFindScope.All, "All"));
            scopePanel.Controls.Add(CreateScopeButton(BulkFindScope.ByFile, "By File"));
            scopePanel.Controls.Add(CreateScopeButton(BulkFindScope.ByField, "By Field"));

            var typeLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 10, 0),
                Text = "Value Type:"
            };
            optionsLayout.Controls.Add(typeLabel, 0, 1);

            var typePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                Margin = new Padding(0)
            };
            optionsLayout.Controls.Add(typePanel, 1, 1);

            typePanel.Controls.Add(CreateValueTypeButton(BulkFindValueType.Text, "Text"));
            typePanel.Controls.Add(CreateValueTypeButton(BulkFindValueType.Number, "Number"));
            typePanel.Controls.Add(CreateValueTypeButton(BulkFindValueType.Boolean, "Checkbox"));

            var valueLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = replaceMode ? 2 : 1,
                Margin = new Padding(0, 0, 0, 4)
            };
            valueLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            valueLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            valueLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (replaceMode)
                valueLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.Controls.Add(valueLayout, 0, 1);

            findValueLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 10, 8),
                Text = "Find:"
            };
            valueLayout.Controls.Add(findValueLabel, 0, 0);

            var findValuePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 32,
                Margin = new Padding(0, 0, 0, 8)
            };
            valueLayout.Controls.Add(findValuePanel, 1, 0);

            findTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            findValuePanel.Controls.Add(findTextBox);

            findBooleanComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            findBooleanComboBox.Items.AddRange(new object[] { "Active", "Inactive" });
            findValuePanel.Controls.Add(findBooleanComboBox);

            replaceValueLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 10, 8),
                Text = "Replace:"
            };
            valueLayout.Controls.Add(replaceValueLabel, 0, 1);

            replaceValuePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 32,
                Margin = new Padding(0, 0, 0, 8)
            };
            valueLayout.Controls.Add(replaceValuePanel, 1, 1);

            replaceTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            replaceValuePanel.Controls.Add(replaceTextBox);

            replaceBooleanComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            replaceBooleanComboBox.Items.AddRange(new object[] { "Active", "Inactive" });
            replaceValuePanel.Controls.Add(replaceBooleanComboBox);

            var fieldsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 6)
            };
            fieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.Controls.Add(fieldsPanel, 0, 2);

            fieldsLabel = new Label
            {
                AutoSize = true,
                Text = "Fields:",
                Margin = new Padding(0, 0, 0, 6)
            };
            fieldsPanel.Controls.Add(fieldsLabel, 0, 0);

            fieldListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false
            };
            fieldsPanel.Controls.Add(fieldListBox, 0, 1);

            var actionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.Controls.Add(actionLayout, 0, 3);

            findActionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            actionLayout.Controls.Add(findActionsPanel, 0, 0);

            findPreviousButton = CreateActionButton("Find Previous", (s, e) => ExecuteRequestedAction(BulkFindReplaceAction.FindPrevious));
            findActionsPanel.Controls.Add(findPreviousButton);

            findNextButton = CreateActionButton("Find Next", (s, e) => ExecuteRequestedAction(BulkFindReplaceAction.FindNext));
            findActionsPanel.Controls.Add(findNextButton);

            replaceActionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.None,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            actionLayout.Controls.Add(replaceActionsPanel, 1, 0);

            replaceButton = CreateActionButton("Replace", (s, e) => ExecuteRequestedAction(BulkFindReplaceAction.Replace));
            replaceActionsPanel.Controls.Add(replaceButton);

            replaceAllButton = CreateActionButton("Replace All", (s, e) => ExecuteRequestedAction(BulkFindReplaceAction.ReplaceAll));
            replaceActionsPanel.Controls.Add(replaceAllButton);

            filterActionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            actionLayout.Controls.Add(filterActionsPanel, 2, 0);

            applyFilterButton = CreateActionButton("Apply as Filter", (s, e) => ExecuteRequestedAction(BulkFindReplaceAction.ApplyAsFilter));
            applyFilterButton.Margin = new Padding(8, 0, 0, 0);
            filterActionsPanel.Controls.Add(applyFilterButton);

            AcceptButton = replaceMode ? replaceButton : findNextButton;

            ApplyInitialRequest(request);
            UpdateValueEditors();
            UpdateFieldList();
            UpdateFieldSelectionState();
            UpdateButtonState();
        }

        public BulkFindReplaceRequest Request { get; private set; }

        private SelectionRadioButton CreateScopeButton(BulkFindScope scope, string text)
        {
            var button = CreateSelectorButton(text, (s, e) =>
            {
                if (((RadioButton)s).Checked)
                    SetSelectedScope(scope);
            });
            scopeButtons[scope] = button;
            return button;
        }

        private SelectionRadioButton CreateValueTypeButton(BulkFindValueType valueType, string text)
        {
            var button = CreateSelectorButton(text, (s, e) =>
            {
                if (((RadioButton)s).Checked)
                    SetSelectedValueType(valueType);
            });
            valueTypeButtons[valueType] = button;
            return button;
        }

        private static SelectionRadioButton CreateSelectorButton(string text, EventHandler checkedChanged)
        {
            var button = new SelectionRadioButton
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 0, 16, 0),
                UseMnemonic = false
            };
            button.CheckedChanged += checkedChanged;
            return button;
        }

        private static Button CreateActionButton(string text, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 8, 0)
            };
            button.Click += clickHandler;
            return button;
        }

        private void ApplyInitialRequest(BulkFindReplaceRequest request)
        {
            request ??= new BulkFindReplaceRequest();

            SetSelectedScope(request.Scope);
            SetSelectedValueType(request.ValueType);

            findTextBox.Text = request.FindText ?? string.Empty;
            replaceTextBox.Text = request.ReplaceText ?? string.Empty;
            findBooleanComboBox.SelectedIndex = request.FindBooleanValue ? 0 : 1;
            replaceBooleanComboBox.SelectedIndex = request.ReplaceBooleanValue ? 0 : 1;
            Request = request.Clone();
        }

        private void SetSelectedScope(BulkFindScope scope)
        {
            selectedScope = scope;
            UpdateSelectorButtonStyles();
            UpdateFieldSelectionState();
        }

        private void SetSelectedValueType(BulkFindValueType valueType)
        {
            selectedValueType = valueType;
            UpdateSelectorButtonStyles();
            UpdateValueEditors();
            UpdateFieldList();
            UpdateFieldSelectionState();
        }

        private void UpdateSelectorButtonStyles()
        {
            ThemeDefinition theme = AppearanceService.IsInitialized ? ActiveTheme : null;
            foreach ((BulkFindScope scope, SelectionRadioButton button) in scopeButtons)
                ApplySelectorButtonStyle(button, theme, scope == selectedScope);

            foreach ((BulkFindValueType valueType, SelectionRadioButton button) in valueTypeButtons)
                ApplySelectorButtonStyle(button, theme, valueType == selectedValueType);
        }

        private static void ApplySelectorButtonStyle(RadioButton button, ThemeDefinition theme, bool isSelected)
        {
            if (button == null)
                return;

            button.Checked = isSelected;
            button.Appearance = Appearance.Normal;
            button.FlatStyle = FlatStyle.Standard;
            if (theme != null)
            {
                button.BackColor = theme.Palette.ControlBackground;
                button.ForeColor = theme.Palette.Foreground;
            }
        }

        private void UpdateValueEditors()
        {
            bool useBooleanEditors = selectedValueType == BulkFindValueType.Boolean;
            findValueLabel.Text = useBooleanEditors ? "Find state:" : "Find:";
            replaceValueLabel.Text = useBooleanEditors ? "Replace state:" : "Replace:";

            findTextBox.Visible = !useBooleanEditors;
            findBooleanComboBox.Visible = useBooleanEditors;
            replaceTextBox.Visible = !useBooleanEditors;
            replaceBooleanComboBox.Visible = useBooleanEditors;

            replaceValueLabel.Visible = replaceMode;
            replaceValuePanel.Visible = replaceMode;
        }

        private void UpdateFieldList()
        {
            var compatibleLabels = new HashSet<string>(
                availableDescriptors
                    .Where(descriptor => IsDescriptorCompatible(descriptor, selectedValueType))
                    .Select(descriptor => descriptor.Label),
                StringComparer.OrdinalIgnoreCase);

            var previouslyChecked = new HashSet<string>(
                fieldListBox.CheckedItems.Cast<string>(),
                StringComparer.OrdinalIgnoreCase);
            if (previouslyChecked.Count == 0 && Request?.FieldLabels != null)
            {
                foreach (string label in Request.FieldLabels)
                    previouslyChecked.Add(label);
            }

            fieldListBox.BeginUpdate();
            try
            {
                fieldListBox.Items.Clear();
                foreach (MaterialFieldDescriptor descriptor in availableDescriptors.Where(descriptor => compatibleLabels.Contains(descriptor.Label)))
                {
                    int index = fieldListBox.Items.Add(descriptor.Label);
                    if (previouslyChecked.Contains(descriptor.Label))
                        fieldListBox.SetItemChecked(index, true);
                }
            }
            finally
            {
                fieldListBox.EndUpdate();
            }

            if (fieldListBox.Items.Count > 0 && fieldListBox.CheckedItems.Count == 0)
                fieldListBox.SetItemChecked(0, true);
        }

        private void UpdateFieldSelectionState()
        {
            bool isByField = selectedScope == BulkFindScope.ByField;
            fieldsLabel.Enabled = isByField;
            fieldListBox.Enabled = isByField;
        }

        private void UpdateButtonState()
        {
            replaceActionsPanel.Visible = replaceMode;
        }

        private void ExecuteRequestedAction(BulkFindReplaceAction action)
        {
            if (!TryBuildRequest(action, out BulkFindReplaceRequest request, out string validationMessage))
            {
                MessageBox.Show(this, validationMessage, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Request = request.Clone();
            executeAction?.Invoke(action, request.Clone());
        }

        private bool TryBuildRequest(BulkFindReplaceAction action, out BulkFindReplaceRequest request, out string validationMessage)
        {
            request = new BulkFindReplaceRequest
            {
                Scope = selectedScope,
                ValueType = selectedValueType,
                FindText = findTextBox.Text ?? string.Empty,
                ReplaceText = replaceTextBox.Text ?? string.Empty,
                FindBooleanValue = findBooleanComboBox.SelectedIndex != 1,
                ReplaceBooleanValue = replaceBooleanComboBox.SelectedIndex != 1,
                FieldLabels = fieldListBox.CheckedItems.Cast<string>().ToList()
            };
            validationMessage = null;

            if (request.ValueType != BulkFindValueType.Boolean && string.IsNullOrWhiteSpace(request.FindText))
            {
                validationMessage = request.ValueType == BulkFindValueType.Number
                    ? "Enter a numeric value to find."
                    : "Enter text to find.";
                return false;
            }

            if (request.Scope == BulkFindScope.ByField && request.FieldLabels.Count == 0)
            {
                validationMessage = "Choose at least one field to search.";
                return false;
            }

            bool isReplaceAction = action == BulkFindReplaceAction.Replace || action == BulkFindReplaceAction.ReplaceAll;
            if (isReplaceAction && request.ValueType != BulkFindValueType.Boolean && string.IsNullOrWhiteSpace(request.ReplaceText))
            {
                validationMessage = request.ValueType == BulkFindValueType.Number
                    ? "Enter a numeric replacement value."
                    : "Enter text to replace with.";
                return false;
            }

            return true;
        }

        private static bool IsDescriptorCompatible(MaterialFieldDescriptor descriptor, BulkFindValueType valueType)
        {
            if (descriptor == null)
                return false;

            return valueType switch
            {
                BulkFindValueType.Number => descriptor.EditorKind == BulkFieldEditorKind.Number,
                BulkFindValueType.Boolean => descriptor.EditorKind == BulkFieldEditorKind.Boolean,
                _ => descriptor.EditorKind is BulkFieldEditorKind.Text
                    or BulkFieldEditorKind.TexturePath
                    or BulkFieldEditorKind.MaterialPath
            };
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
            UpdateSelectorButtonStyles();
        }
    }
}
