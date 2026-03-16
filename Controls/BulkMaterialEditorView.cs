using Material_Editor.Dialogs;
using Material_Editor.Models;
using Material_Editor.Services;
using Material_Editor.Theming;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal sealed class BulkMaterialEditorView : ThemeAwareUserControl
    {
        private static readonly IReadOnlyDictionary<string, string[]> VisibilityDrivenFieldMap =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [ControlNames.Refraction] = new[] { ControlNames.RefractionFalloff, ControlNames.RefractionPower },
                [ControlNames.EnvironmentMapping] = new[] { ControlNames.EnvironmentMaskScale },
                [ControlNames.SpecularEnabled] = new[] { ControlNames.SpecularColor, ControlNames.SpecularMultiplier },
                [ControlNames.RimLighting] = new[] { ControlNames.RimPower },
                [ControlNames.SubsurfaceLighting] = new[] { ControlNames.SubsurfaceLightingRolloff },
                [ControlNames.EmittanceEnabled] = new[] { ControlNames.EmittanceColor, ControlNames.EmittanceMultiplier },
                [ControlNames.AdaptativeEmissive] = new[] { ControlNames.AdaptEmissiveExposureOffset, ControlNames.AdaptEmissiveFinalExposureMin, ControlNames.AdaptEmissiveFinalExposureMax },
                [ControlNames.Hair] = new[] { ControlNames.HairTintColor },
                [ControlNames.Tessellate] = new[] { ControlNames.DisplacementTexBias, ControlNames.DisplacementTexScale, ControlNames.TessellationPNScale, ControlNames.TessellationBaseFactor, ControlNames.TessellationFadeDistance },
                [ControlNames.Terrain] = new[] { ControlNames.UnkInt1BGSM, ControlNames.TerrainThresholdFalloff, ControlNames.TerrainTilingDistance, ControlNames.TerrainRotationAngle }
            };

        private static readonly IReadOnlySet<string> AutoManagedFieldLabels =
            new HashSet<string>(VisibilityDrivenFieldMap.Values.SelectMany(labels => labels), StringComparer.OrdinalIgnoreCase);

        private const string DirtyColumnName = "__dirty";
        private const string VersionColumnName = "__version";
        private const string PathColumnName = "__path";

        private readonly DataGridView grid;
        private readonly Label introLabel;
        private readonly Label summaryLabel;
        private readonly Label validationLabel;
        private readonly Button chooseFieldsButton;
        private readonly ColorToggleCheckBox backupCheckBox;
        private readonly Dictionary<(string FilePath, string Label), string> invalidCellTexts = new();
        private readonly Dictionary<(string FilePath, string Label), bool> pendingBooleanOverrides = new();
        private readonly HashSet<string> pendingDirtyRowPaths = new(StringComparer.OrdinalIgnoreCase);

        private BulkMaterialEditSession session;
        private Config config;
        private ThemeDefinition theme;
        private HashSet<string> selectedLabelPreferences = new(StringComparer.OrdinalIgnoreCase);
        private List<MaterialFieldDescriptor> selectedDescriptors = new();
        private ComboBox activeComboEditingControl;
        private bool suppressGridEvents;

        public BulkMaterialEditorView()
        {
            Dock = DockStyle.Fill;
            Margin = new Padding(0);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(mainLayout);

            introLabel = new Label
            {
                Text = "Bulk mode edits many files at once. Use row selection for Save Selected and Remove Selected Files.",
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };
            mainLayout.Controls.Add(introLabel, 0, 0);

            grid = new BulkEditorDataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                EditMode = DataGridViewEditMode.EditOnEnter,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ShowCellToolTips = true,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText
            };
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            grid.CellBeginEdit += Grid_CellBeginEdit;
            grid.CellValueChanged += Grid_CellValueChanged;
            grid.CellEndEdit += Grid_CellEndEdit;
            grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
            grid.DataError += Grid_DataError;
            grid.EditingControlShowing += Grid_EditingControlShowing;
            grid.CellMouseUp += Grid_CellMouseUp;
            grid.SelectionChanged += (s, e) => RefreshSummary();
            grid.KeyDown += Grid_KeyDown;
            mainLayout.Controls.Add(grid, 0, 1);

            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0)
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainLayout.Controls.Add(footerLayout, 0, 2);

            var statusLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoSize = true,
                Margin = new Padding(0)
            };
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerLayout.Controls.Add(statusLayout, 0, 0);

            summaryLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            statusLayout.Controls.Add(summaryLabel, 0, 0);

            validationLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0)
            };
            statusLayout.Controls.Add(validationLabel, 0, 1);

            var optionsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(12, 0, 0, 0)
            };
            footerLayout.Controls.Add(optionsPanel, 1, 0);

            backupCheckBox = new ColorToggleCheckBox
            {
                Text = "Create .bak backups",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(0, 6, 12, 0)
            };
            backupCheckBox.CheckedChanged += (s, e) => RefreshSummary();
            optionsPanel.Controls.Add(backupCheckBox);

            chooseFieldsButton = new Button
            {
                Text = "Choose Fields...",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            chooseFieldsButton.Click += (s, e) => ChooseFields(FindForm());
            optionsPanel.Controls.Add(chooseFieldsButton);
        }

        public event EventHandler StateChanged;

        public BulkMaterialEditSession Session => session;
        public bool BackupBeforeWrite => backupCheckBox.Checked;
        public IReadOnlyList<MaterialFieldDescriptor> SelectedDescriptors => selectedDescriptors;
        public bool HasDirtyRows => session != null && session.Rows.Any(IsRowEffectivelyDirty);
        public int DirtyRowCount => session?.Rows.Count(IsRowEffectivelyDirty) ?? 0;
        public IReadOnlyList<BulkMaterialEditRow> SelectedRows => grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.Tag as BulkMaterialEditRow)
            .Where(row => row != null)
            .Distinct()
            .ToArray();

        public int SelectedDirtyRowCount => SelectedRows.Count(IsRowEffectivelyDirty);
        public int SelectedRowCount => SelectedRows.Count;

        public void Initialize(BulkMaterialEditSession session, Config config, bool backupBeforeWrite)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.config = config;
            backupCheckBox.Checked = backupBeforeWrite;
            selectedLabelPreferences = BuildDefaultSelectedLabelPreferences(session);
            selectedDescriptors = ResolveVisibleDescriptors(session, selectedLabelPreferences);
            invalidCellTexts.Clear();
            pendingBooleanOverrides.Clear();
            pendingDirtyRowPaths.Clear();
            Visible = true;
            RebuildGrid();
            ApplyCurrentAppearance();
            RefreshSummary();
        }

        public void ClearSession()
        {
            session = null;
            selectedLabelPreferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            selectedDescriptors = new List<MaterialFieldDescriptor>();
            invalidCellTexts.Clear();
            pendingBooleanOverrides.Clear();
            pendingDirtyRowPaths.Clear();
            grid.Rows.Clear();
            grid.Columns.Clear();
            summaryLabel.Text = string.Empty;
            validationLabel.Text = string.Empty;
            Visible = false;
            OnStateChanged();
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            this.theme = appearance.Theme;
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
            validationLabel.ForeColor = string.IsNullOrEmpty(validationLabel.Text) ? appearance.Theme.Palette.Foreground : appearance.Theme.Semantics.Error;
            ApplyGridThemeStyles();
        }

        private ThemeDefinition RequireTheme()
        {
            this.theme ??= ActiveTheme;
            return this.theme;
        }

        public BulkMaterialSessionAddResult AddFiles(IEnumerable<string> filePaths)
        {
            if (session == null)
                return new BulkMaterialSessionAddResult();

            BulkMaterialSessionAddResult result = session.AddFiles(filePaths);
            selectedDescriptors = ResolveVisibleDescriptors(session, selectedLabelPreferences);
            RebuildGrid();
            RefreshSummary();
            return result;
        }

        public void NotifySessionChanged()
        {
            pendingBooleanOverrides.Clear();
            pendingDirtyRowPaths.Clear();
            foreach (BulkMaterialEditRow row in session?.Rows ?? Array.Empty<BulkMaterialEditRow>())
                row.RefreshDynamicState();

            selectedDescriptors = ResolveVisibleDescriptors(session, selectedLabelPreferences);
            RebuildGrid();
            RefreshSummary();
        }

        public IReadOnlyList<FieldCopyResult> SaveAllChanges()
        {
            if (session == null)
                return Array.Empty<FieldCopyResult>();

            CommitPendingEdits();
            var results = session.ApplyChanges(BackupBeforeWrite);
            RebuildGrid();
            RefreshSummary();
            return results;
        }

        public IReadOnlyList<FieldCopyResult> SaveSelectedChanges()
        {
            if (session == null)
                return Array.Empty<FieldCopyResult>();

            CommitPendingEdits();
            var selectedRows = SelectedRows;
            var results = session.ApplySelectedChanges(selectedRows, BackupBeforeWrite);
            RebuildGrid();
            ReselectRows(selectedRows.Select(row => row.FilePath));
            RefreshSummary();
            return results;
        }

        public void CommitPendingEdits()
        {
            if (session == null || grid.IsDisposed)
                return;

            if (grid.IsCurrentCellDirty)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);

            if (grid.IsCurrentCellInEditMode)
                grid.EndEdit();

            if (grid.CurrentCell?.OwningColumn is DataGridViewComboBoxColumn)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);

            bool gridChangedSession = false;
            bool visibilityChanged = false;

            foreach (((string filePath, string label) key, bool pendingValue) in pendingBooleanOverrides.ToArray())
            {
                BulkMaterialEditRow row = session.Rows.FirstOrDefault(candidate => string.Equals(candidate.FilePath, key.filePath, StringComparison.OrdinalIgnoreCase));
                MaterialFieldDescriptor descriptor = selectedDescriptors.FirstOrDefault(candidate => string.Equals(candidate.Label, key.label, StringComparison.OrdinalIgnoreCase))
                    ?? session.FindDescriptor(key.label);
                if (row == null || descriptor == null)
                    continue;

                BulkMaterialEditCellState cell = row.GetCell(descriptor);
                bool currentValue = cell.CurrentValue is bool currentBool && currentBool;
                if (currentValue != pendingValue)
                {
                    if (!session.TrySetCellValue(row, descriptor, pendingValue, out _))
                        continue;

                    gridChangedSession = true;
                    if (VisibilityDrivenFieldMap.ContainsKey(descriptor.Label))
                    {
                        row.RefreshDynamicState();
                        visibilityChanged = true;
                    }
                }

                pendingBooleanOverrides.Remove(key);
                if (row.IsDirty)
                    pendingDirtyRowPaths.Remove(row.FilePath);
            }

            if (!gridChangedSession)
                return;

            if (visibilityChanged)
            {
                selectedDescriptors = ResolveVisibleDescriptors(session, selectedLabelPreferences);
                RebuildGrid();
            }
            else
            {
                for (int rowIndex = 0; rowIndex < grid.Rows.Count; rowIndex++)
                {
                    if (grid.Rows[rowIndex].Tag is BulkMaterialEditRow row)
                        RefreshGridRow(rowIndex, row);
                }
            }

            RefreshSummary();
        }

        public bool IsDirtyRow(BulkMaterialEditRow row)
        {
            return IsRowEffectivelyDirty(row);
        }

        public void ChooseFields(IWin32Window owner)
        {
            if (session == null)
                return;

            var selectedLabels = new HashSet<string>(selectedLabelPreferences, StringComparer.OrdinalIgnoreCase);
            var availableDescriptors = session.AllDescriptors
                .Where(descriptor => !AutoManagedFieldLabels.Contains(descriptor.Label))
                .Where(descriptor => selectedLabels.Contains(descriptor.Label) || FieldIsSupportedInAnyRow(session.Rows, descriptor))
                .ToArray();
            using var dialog = new FieldSelectionDialog(availableDescriptors, selectedLabels, config, session.MaterialType);
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;

            if (dialog.SelectedFields == null || dialog.SelectedFields.Count == 0)
            {
                MessageBox.Show(owner, "Choose at least one field to display.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            selectedLabelPreferences = new HashSet<string>(
                dialog.SelectedFields.Select(descriptor => descriptor.Label),
                StringComparer.OrdinalIgnoreCase);
            selectedDescriptors = ResolveVisibleDescriptors(session, selectedLabelPreferences);
            RebuildGrid();
        }

        private void RebuildGrid()
        {
            suppressGridEvents = true;
            try
            {
                grid.SuspendLayout();
                grid.Rows.Clear();
                grid.Columns.Clear();

                if (session == null)
                    return;

                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = DirtyColumnName,
                    HeaderText = "Dirty",
                    Width = 54,
                    ReadOnly = true,
                    Frozen = true
                });
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = VersionColumnName,
                    HeaderText = "Version",
                    Width = 64,
                    ReadOnly = true,
                    Frozen = true
                });
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = PathColumnName,
                    HeaderText = "File",
                    Width = 360,
                    ReadOnly = true,
                    Frozen = true
                });

                foreach (MaterialFieldDescriptor descriptor in selectedDescriptors)
                    grid.Columns.Add(CreateFieldColumn(descriptor));

                foreach (BulkMaterialEditRow row in session.Rows)
                {
                    int rowIndex = grid.Rows.Add();
                    var gridRow = grid.Rows[rowIndex];
                    gridRow.Tag = row;
                    PopulateGridRow(gridRow, row);
                }
            }
            finally
            {
                grid.ResumeLayout();
                suppressGridEvents = false;
            }

            ApplyGridSizing();
            ApplyGridThemeStyles();
            OnStateChanged();
        }

        private DataGridViewColumn CreateFieldColumn(MaterialFieldDescriptor descriptor)
        {
            string columnName = GetFieldColumnName(descriptor.Label);
            return descriptor.EditorKind switch
            {
                BulkFieldEditorKind.Boolean => ColorToggleDataGridView.CreateColumn(null, descriptor.Label, name: columnName, width: 72),
                BulkFieldEditorKind.Enum => new DataGridViewComboBoxColumn
                {
                    Name = columnName,
                    HeaderText = descriptor.Label,
                    Width = 150,
                    DataSource = Enum.GetNames(descriptor.EnumType),
                    FlatStyle = FlatStyle.Flat,
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                },
                _ => new DataGridViewTextBoxColumn
                {
                    Name = columnName,
                    HeaderText = descriptor.Label,
                    Width = descriptor.EditorKind is BulkFieldEditorKind.TexturePath or BulkFieldEditorKind.MaterialPath or BulkFieldEditorKind.Text ? 180 : 120
                }
            };
        }

        private void PopulateGridRow(DataGridViewRow gridRow, BulkMaterialEditRow row)
        {
            gridRow.Cells[DirtyColumnName].Value = IsRowEffectivelyDirty(row) ? "*" : string.Empty;
            gridRow.Cells[VersionColumnName].Value = row.HasLoadError ? string.Empty : row.Version.ToString();
            gridRow.Cells[PathColumnName].Value = GetDisplayFilePath(row.FilePath);
            gridRow.Cells[PathColumnName].ToolTipText = row.FilePath;

            foreach (MaterialFieldDescriptor descriptor in selectedDescriptors)
            {
                var cell = row.GetCell(descriptor);
                var gridCell = gridRow.Cells[GetFieldColumnName(descriptor.Label)];
                if (invalidCellTexts.TryGetValue((row.FilePath, descriptor.Label), out string invalidText))
                    gridCell.Value = invalidText;
                else if (descriptor.EditorKind == BulkFieldEditorKind.Boolean && pendingBooleanOverrides.TryGetValue((row.FilePath, descriptor.Label), out bool pendingBooleanValue))
                    gridCell.Value = pendingBooleanValue;
                else
                    gridCell.Value = cell.GetGridValue();

                gridCell.ReadOnly = row.HasLoadError || !cell.IsSupported;
                gridCell.ErrorText = cell.ErrorMessage ?? string.Empty;
                ApplyFieldCellTheme(gridCell, row, cell);
            }

            RefreshGridRowState(gridRow, row);
        }

        private void RefreshGridRowState(DataGridViewRow gridRow, BulkMaterialEditRow row)
        {
            bool isDirty = IsRowEffectivelyDirty(row);
            gridRow.Cells[DirtyColumnName].Value = isDirty ? "*" : string.Empty;

            string errorText = row.HasLoadError
                ? row.LoadError
                : row.Cells.FirstOrDefault(cell => cell.HasError)?.ErrorMessage ?? string.Empty;
            gridRow.ErrorText = errorText;

            if (row.HasLoadError)
                gridRow.DefaultCellStyle.BackColor = GetLoadErrorBackColor();
            else if (row.HasValidationErrors)
                gridRow.DefaultCellStyle.BackColor = GetValidationBackColor();
            else if (isDirty)
                gridRow.DefaultCellStyle.BackColor = GetDirtyRowBackColor();
            else
                gridRow.DefaultCellStyle.BackColor = grid.DefaultCellStyle.BackColor;

            gridRow.DefaultCellStyle.ForeColor = RequireTheme().Palette.Foreground;
            gridRow.DefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
            gridRow.DefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.SelectionForeColor;
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            MaterialFieldDescriptor descriptor = TryGetFieldDescriptor(grid.Columns[e.ColumnIndex].Name);
            if (descriptor?.EditorKind == BulkFieldEditorKind.Boolean)
                e.Cancel = true;
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (suppressGridEvents || e.RowIndex < 0 || e.ColumnIndex < 0 || session == null)
                return;

            DataGridViewColumn column = grid.Columns[e.ColumnIndex];
            MaterialFieldDescriptor descriptor = TryGetFieldDescriptor(column.Name);
            if (descriptor == null)
                return;

            if (descriptor.EditorKind == BulkFieldEditorKind.Boolean || descriptor.EditorKind == BulkFieldEditorKind.Enum)
                ApplyCellEdit(e.RowIndex, descriptor);
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (suppressGridEvents || e.RowIndex < 0 || e.ColumnIndex < 0 || session == null)
                return;

            MaterialFieldDescriptor descriptor = TryGetFieldDescriptor(grid.Columns[e.ColumnIndex].Name);
            if (descriptor == null)
                return;

            if (descriptor.EditorKind != BulkFieldEditorKind.Boolean && descriptor.EditorKind != BulkFieldEditorKind.Enum)
                ApplyCellEdit(e.RowIndex, descriptor);
        }

        private void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (session == null || e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Rows[e.RowIndex].Tag is not BulkMaterialEditRow row)
                return;

            MaterialFieldDescriptor descriptor = TryGetFieldDescriptor(grid.Columns[e.ColumnIndex].Name);
            if (descriptor == null)
            {
                e.ToolTipText = e.ColumnIndex switch
                {
                    1 => row.Version.ToString(),
                    2 => row.FilePath,
                    _ => row.LoadError
                };
                return;
            }

            var cell = row.GetCell(descriptor);
            e.ToolTipText = cell.HasError
                ? cell.ErrorMessage
                : invalidCellTexts.TryGetValue((row.FilePath, descriptor.Label), out string invalidText)
                    ? invalidText
                    : cell.GetDisplayText();
        }

        private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = false;
        }

        private void Grid_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (suppressGridEvents || session == null)
                return;

            TryToggleBooleanCell(e.RowIndex, e.ColumnIndex);
        }

        private void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (activeComboEditingControl != null)
            {
                activeComboEditingControl.DrawItem -= ComboEditingControl_DrawItem;
                activeComboEditingControl = null;
            }

            if (e.Control is TextBox textBox)
            {
                textBox.BorderStyle = BorderStyle.FixedSingle;
                ThemeDefinition currentTheme = RequireTheme();
                textBox.BackColor = currentTheme.Palette.PanelBackground;
                textBox.ForeColor = currentTheme.Palette.Foreground;
                return;
            }

            if (e.Control is ComboBox comboBox)
            {
                ThemeDefinition currentTheme = RequireTheme();
                comboBox.FlatStyle = FlatStyle.Flat;
                comboBox.BackColor = currentTheme.Palette.ControlBackground;
                comboBox.ForeColor = currentTheme.Palette.Foreground;
                comboBox.DrawMode = DrawMode.OwnerDrawFixed;
                comboBox.DrawItem -= ComboEditingControl_DrawItem;
                comboBox.DrawItem += ComboEditingControl_DrawItem;
                activeComboEditingControl = comboBox;
            }
        }

        private void ComboEditingControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (sender is not ComboBox comboBox)
                return;

            e.DrawBackground();

            ThemeDefinition currentTheme = RequireTheme();
            Color background = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? (currentTheme.Palette.Accent.IsEmpty ? currentTheme.Palette.MenuBackground : currentTheme.Palette.Accent)
                : currentTheme.Palette.PanelBackground;

            using var backgroundBrush = new SolidBrush(background);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

            if (e.Index >= 0)
            {
                string text = comboBox.Items[e.Index]?.ToString() ?? string.Empty;
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    comboBox.Font,
                    Rectangle.Inflate(e.Bounds, -2, 0),
                    currentTheme.Palette.Foreground,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            e.DrawFocusRectangle();
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (grid.CurrentCell != null && TryToggleBooleanCell(grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex, e))
                return;

            if (e.Control && e.KeyCode == Keys.A)
            {
                grid.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
                RefreshSummary();
                return;
            }

            if (grid.Rows.Count == 0 || grid.CurrentCell == null || !e.Shift)
                return;

            int currentRowIndex = grid.CurrentCell.RowIndex;
            if (e.KeyCode == Keys.Home)
            {
                SelectRange(0, currentRowIndex);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.End)
            {
                SelectRange(currentRowIndex, grid.Rows.Count - 1);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void SelectRange(int startIndex, int endIndex)
        {
            startIndex = Math.Max(0, startIndex);
            endIndex = Math.Min(grid.Rows.Count - 1, endIndex);
            if (startIndex > endIndex)
                (startIndex, endIndex) = (endIndex, startIndex);

            grid.ClearSelection();
            for (int index = startIndex; index <= endIndex; index++)
                grid.Rows[index].Selected = true;

            if (grid.Rows.Count > 0)
                grid.CurrentCell = grid.Rows[endIndex].Cells[Math.Min(2, grid.Columns.Count - 1)];

            RefreshSummary();
        }

        private void ApplyCellEdit(int rowIndex, MaterialFieldDescriptor descriptor, object inputValueOverride = null)
        {
            if (session == null || grid.Rows[rowIndex].Tag is not BulkMaterialEditRow row)
                return;

            DataGridViewCell gridCell = grid.Rows[rowIndex].Cells[GetFieldColumnName(descriptor.Label)];
            string keyFilePath = row.FilePath;
            string[] selectedPaths = SelectedRows.Select(selectedRow => selectedRow.FilePath).ToArray();
            string focusFilePath = row.FilePath;
            string focusColumnName = GetFieldColumnName(descriptor.Label);

            object inputValue = inputValueOverride ?? gridCell.Value;
            if (session.TrySetCellValue(row, descriptor, inputValue, out string errorMessage))
            {
                suppressGridEvents = true;
                try
                {
                    invalidCellTexts.Remove((keyFilePath, descriptor.Label));
                    gridCell.Value = row.GetCell(descriptor).GetGridValue();
                    gridCell.ErrorText = string.Empty;
                }
                finally
                {
                    suppressGridEvents = false;
                }
            }
            else
            {
                invalidCellTexts[(keyFilePath, descriptor.Label)] = Convert.ToString(gridCell.Value) ?? string.Empty;
                gridCell.ErrorText = errorMessage ?? "Invalid value.";
            }

            RefreshGridRowState(grid.Rows[rowIndex], row);
            RefreshSummary();

            if (VisibilityDrivenFieldMap.ContainsKey(descriptor.Label))
                row.RefreshDynamicState();

            if (VisibilityDrivenFieldMap.ContainsKey(descriptor.Label)
                && TryRefreshVisibilityDrivenColumns(selectedPaths, focusFilePath, focusColumnName))
            {
                return;
            }

            if (VisibilityDrivenFieldMap.ContainsKey(descriptor.Label))
                RefreshGridRow(rowIndex, row);
        }

        private void RefreshSummary()
        {
            if (session == null)
            {
                summaryLabel.Text = string.Empty;
                validationLabel.Text = string.Empty;
                OnStateChanged();
                return;
            }

            int loadedCount = session.Rows.Count(row => !row.HasLoadError);
            int loadErrorCount = session.Rows.Count(row => row.HasLoadError);
            summaryLabel.Text = $"{loadedCount} loaded, {DirtyRowCount} dirty, {SelectedDirtyRowCount} selected dirty, {session.ErrorRowCount} with validation errors, {loadErrorCount} failed to load. {SelectedRowCount} row(s) selected. Backups {(backupCheckBox.Checked ? "on" : "off")}.";

            validationLabel.Text = session.HasValidationErrors
                ? "Validation errors are blocking save for affected rows."
                : loadErrorCount > 0
                    ? "Some rows could not be loaded and will report as failures during save."
                    : string.Empty;
            validationLabel.ForeColor = session.HasValidationErrors
                ? RequireTheme().Semantics.Error
                : loadErrorCount > 0
                    ? RequireTheme().Semantics.Warning
                    : RequireTheme().Palette.Foreground;

            OnStateChanged();
        }

        private static string GetFieldColumnName(string label)
        {
            return $"field::{label}";
        }

        private static string GetDisplayFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            const string materialsBackslash = "materials\\";
            const string materialsSlash = "materials/";
            int index = filePath.IndexOf(materialsBackslash, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
                return filePath[(index + materialsBackslash.Length)..];

            index = filePath.IndexOf(materialsSlash, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
                return filePath[(index + materialsSlash.Length)..];

            return filePath;
        }

        private static HashSet<string> BuildDefaultSelectedLabelPreferences(BulkMaterialEditSession session)
        {
            if (session == null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return session.AllDescriptors
                .Where(descriptor => !AutoManagedFieldLabels.Contains(descriptor.Label))
                .Where(descriptor => FieldIsSupportedInAnyRow(session.Rows, descriptor))
                .Where(descriptor => descriptor.EditorKind == BulkFieldEditorKind.Boolean || FieldHasMeaningfulData(session.Rows, descriptor))
                .Select(descriptor => descriptor.Label)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool FieldIsSupportedInAnyRow(IEnumerable<BulkMaterialEditRow> rows, MaterialFieldDescriptor descriptor)
        {
            foreach (BulkMaterialEditRow row in rows ?? Array.Empty<BulkMaterialEditRow>())
            {
                if (row.HasLoadError)
                    continue;

                if (row.Material != null && descriptor.IsSupported(row.Material))
                    return true;
            }

            return false;
        }

        private static bool FieldHasMeaningfulData(IEnumerable<BulkMaterialEditRow> rows, MaterialFieldDescriptor descriptor)
        {
            foreach (BulkMaterialEditRow row in rows ?? Array.Empty<BulkMaterialEditRow>())
            {
                if (row.HasLoadError)
                    continue;

                if (row.Material == null || !descriptor.IsSupported(row.Material))
                    continue;

                if (!string.IsNullOrWhiteSpace(descriptor.FormatValue(descriptor.GetValue(row.Material))))
                    return true;
            }

            return false;
        }

        private static List<MaterialFieldDescriptor> ResolveVisibleDescriptors(BulkMaterialEditSession session, IReadOnlyCollection<string> selectedLabels)
        {
            if (session == null)
                return new List<MaterialFieldDescriptor>();

            var selected = new HashSet<string>(selectedLabels ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var effectiveLabels = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);

            foreach ((string controllerLabel, string[] dependentLabels) in VisibilityDrivenFieldMap)
            {
                if (!selected.Contains(controllerLabel) || !FieldIsEnabledInAnyRow(session.Rows, controllerLabel))
                    continue;

                foreach (string dependentLabel in dependentLabels)
                    effectiveLabels.Add(dependentLabel);
            }

            var visibleDescriptors = session.AllDescriptors
                .Where(descriptor => effectiveLabels.Contains(descriptor.Label))
                .Where(descriptor => FieldIsSupportedInAnyRow(session.Rows, descriptor))
                .ToList();

            return visibleDescriptors.Count > 0
                ? visibleDescriptors
                : session.AllDescriptors
                    .Where(descriptor => !AutoManagedFieldLabels.Contains(descriptor.Label))
                    .ToList();
        }

        private static bool FieldIsEnabledInAnyRow(IEnumerable<BulkMaterialEditRow> rows, string controllerLabel)
        {
            foreach (BulkMaterialEditRow row in rows ?? Array.Empty<BulkMaterialEditRow>())
            {
                if (row.HasLoadError)
                    continue;

                MaterialFieldDescriptor descriptor = row.Cells
                    .Select(cell => cell.Descriptor)
                    .FirstOrDefault(cellDescriptor => string.Equals(cellDescriptor.Label, controllerLabel, StringComparison.OrdinalIgnoreCase));
                if (descriptor == null || row.Material == null || !descriptor.IsSupported(row.Material))
                    continue;

                if (descriptor.GetValue(row.Material) is bool isEnabled && isEnabled)
                    return true;
            }

            return false;
        }

        private bool TryRefreshVisibilityDrivenColumns(IReadOnlyCollection<string> selectedPaths, string focusFilePath, string focusColumnName)
        {
            List<MaterialFieldDescriptor> resolvedDescriptors = ResolveVisibleDescriptors(session, selectedLabelPreferences);
            if (resolvedDescriptors.Select(descriptor => descriptor.Label).SequenceEqual(
                    selectedDescriptors.Select(descriptor => descriptor.Label),
                    StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            selectedDescriptors = resolvedDescriptors;
            RebuildGrid();
            ReselectRows(selectedPaths);
            RestoreCurrentCell(focusFilePath, focusColumnName);
            RefreshSummary();
            return true;
        }

        private void RestoreCurrentCell(string focusFilePath, string focusColumnName)
        {
            if (string.IsNullOrWhiteSpace(focusFilePath) || string.IsNullOrWhiteSpace(focusColumnName))
                return;

            if (!grid.Columns.Contains(focusColumnName))
                return;

            foreach (DataGridViewRow gridRow in grid.Rows)
            {
                if (gridRow.Tag is not BulkMaterialEditRow row
                    || !string.Equals(row.FilePath, focusFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                grid.CurrentCell = gridRow.Cells[focusColumnName];
                CenterCurrentCellColumn();
                return;
            }
        }

        private void RefreshGridRow(int rowIndex, BulkMaterialEditRow row)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count || row == null)
                return;

            suppressGridEvents = true;
            try
            {
                PopulateGridRow(grid.Rows[rowIndex], row);
            }
            finally
            {
                suppressGridEvents = false;
            }
        }

        private bool TryToggleBooleanCell(int rowIndex, int columnIndex, KeyEventArgs keyEventArgs = null)
        {
            if (session == null || rowIndex < 0 || columnIndex < 0)
                return false;

            MaterialFieldDescriptor descriptor = TryGetFieldDescriptor(grid.Columns[columnIndex].Name);
            if (descriptor?.EditorKind != BulkFieldEditorKind.Boolean)
                return false;

            if (grid.Rows[rowIndex].Tag is not BulkMaterialEditRow row)
                return false;

            DataGridViewCell gridCell = grid.Rows[rowIndex].Cells[columnIndex];
            if (gridCell.ReadOnly)
                return false;

            bool currentValue = row.GetCell(descriptor).CurrentValue is bool boolValue && boolValue;
            bool nextValue = !currentValue;
            string keyFilePath = row.FilePath;
            pendingDirtyRowPaths.Add(keyFilePath);
            pendingBooleanOverrides[(keyFilePath, descriptor.Label)] = nextValue;
            string[] selectedPaths = SelectedRows.Select(selectedRow => selectedRow.FilePath).ToArray();
            string focusFilePath = row.FilePath;
            string focusColumnName = GetFieldColumnName(descriptor.Label);

            suppressGridEvents = true;
            try
            {
                grid.CurrentCell = gridCell;

                if (session.TrySetCellValue(row, descriptor, nextValue, out string errorMessage))
                {
                    invalidCellTexts.Remove((keyFilePath, descriptor.Label));
                    gridCell.Value = row.GetCell(descriptor).GetGridValue();
                    gridCell.ErrorText = string.Empty;
                }
                else
                {
                    invalidCellTexts[(keyFilePath, descriptor.Label)] = Convert.ToString(nextValue) ?? string.Empty;
                    gridCell.ErrorText = errorMessage ?? "Invalid value.";
                    return false;
                }
            }
            finally
            {
                suppressGridEvents = false;
            }

            RefreshGridRowState(grid.Rows[rowIndex], row);
            RefreshSummary();

            if (VisibilityDrivenFieldMap.ContainsKey(descriptor.Label))
                row.RefreshDynamicState();

            if (VisibilityDrivenFieldMap.ContainsKey(descriptor.Label)
                && TryRefreshVisibilityDrivenColumns(selectedPaths, focusFilePath, focusColumnName))
            {
                if (keyEventArgs != null)
                {
                    keyEventArgs.Handled = true;
                    keyEventArgs.SuppressKeyPress = true;
                }

                return true;
            }

            if (VisibilityDrivenFieldMap.ContainsKey(descriptor.Label))
                RefreshGridRow(rowIndex, row);

            if (keyEventArgs != null)
            {
                keyEventArgs.Handled = true;
                keyEventArgs.SuppressKeyPress = true;
            }

            return true;
        }

        private static bool GetBooleanCellValue(DataGridViewCell gridCell)
        {
            if (gridCell == null)
                return false;

            return TryConvertToBoolean(gridCell.Value, out bool value)
                || TryConvertToBoolean(gridCell.EditedFormattedValue, out value)
                || TryConvertToBoolean(gridCell.FormattedValue, out value)
                ? value
                : false;
        }

        private static bool TryConvertToBoolean(object value, out bool result)
        {
            switch (value)
            {
                case bool boolValue:
                    result = boolValue;
                    return true;
                case CheckState checkState:
                    result = checkState == CheckState.Checked;
                    return true;
                case string text when bool.TryParse(text, out bool parsed):
                    result = parsed;
                    return true;
                case null:
                    result = false;
                    return false;
            }

            try
            {
                result = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                result = false;
                return false;
            }
        }

        private bool IsRowEffectivelyDirty(BulkMaterialEditRow row)
        {
            if (row == null)
                return false;

            if (pendingDirtyRowPaths.Contains(row.FilePath))
                return true;

            if (row.IsDirty)
                return true;

            foreach ((string filePath, string label) in pendingBooleanOverrides.Keys)
            {
                if (!string.Equals(filePath, row.FilePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                MaterialFieldDescriptor descriptor = session?.FindDescriptor(label);
                if (descriptor == null)
                    continue;

                object originalValue = row.GetCell(descriptor).OriginalValue;
                bool pendingValue = pendingBooleanOverrides[(filePath, label)];
                bool originalBooleanValue = originalValue is bool boolValue && boolValue;
                if (pendingValue != originalBooleanValue)
                    return true;
            }

            return false;
        }

        private void CenterCurrentCellColumn()
        {
            if (grid.CurrentCell == null || grid.CurrentCell.ColumnIndex < 0)
                return;

            DataGridViewColumn currentColumn = grid.Columns[grid.CurrentCell.ColumnIndex];
            if (!currentColumn.Visible || currentColumn.Frozen)
                return;

            int frozenWidth = grid.Columns
                .Cast<DataGridViewColumn>()
                .Where(column => column.Visible && column.Frozen)
                .Sum(column => column.Width);

            int availableWidth = Math.Max(120, grid.DisplayRectangle.Width - frozenWidth);
            if (grid.DisplayRectangle.Width <= frozenWidth)
                return;

            int targetLeadingWidth = Math.Max(0, (availableWidth - currentColumn.Width) / 2);
            int accumulatedWidth = 0;
            int desiredLeadingWidth = 0;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible || column.Frozen)
                    continue;

                if (column.Index == currentColumn.Index)
                {
                    desiredLeadingWidth = Math.Max(0, accumulatedWidth - targetLeadingWidth);
                    break;
                }

                accumulatedWidth += column.Width;
            }

            int runningWidth = 0;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible || column.Frozen)
                    continue;

                if (runningWidth + column.Width > desiredLeadingWidth)
                {
                    try
                    {
                        grid.FirstDisplayedScrollingColumnIndex = column.Index;
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    return;
                }

                runningWidth += column.Width;
            }
        }

        private MaterialFieldDescriptor TryGetFieldDescriptor(string columnName)
        {
            if (session == null || string.IsNullOrWhiteSpace(columnName) || !columnName.StartsWith("field::", StringComparison.Ordinal))
                return null;

            return session.FindDescriptor(columnName["field::".Length..]);
        }

        private void ApplyGridThemeStyles()
        {
            ThemeDefinition currentTheme = RequireTheme();
            grid.BackgroundColor = currentTheme.Palette.PanelBackground;
            grid.GridColor = currentTheme.Palette.Accent.IsEmpty ? currentTheme.Palette.Foreground : currentTheme.Palette.Accent;
            grid.DefaultCellStyle.BackColor = currentTheme.Palette.PanelBackground;
            grid.DefaultCellStyle.ForeColor = currentTheme.Palette.Foreground;
            grid.DefaultCellStyle.SelectionBackColor = currentTheme.Palette.Accent.IsEmpty ? currentTheme.Palette.MenuBackground : currentTheme.Palette.Accent;
            grid.DefaultCellStyle.SelectionForeColor = currentTheme.Palette.Foreground;
            grid.AlternatingRowsDefaultCellStyle.BackColor = ThemeApplicator.GetAlternatingRowBackground(currentTheme);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = currentTheme.Palette.Foreground;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.SelectionForeColor;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.DefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
                column.DefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.SelectionForeColor;

                if (column.Name is DirtyColumnName or VersionColumnName or PathColumnName)
                {
                    column.DefaultCellStyle.BackColor = ThemeApplicator.GetFrozenColumnBackground(currentTheme);
                    column.DefaultCellStyle.ForeColor = currentTheme.Palette.Foreground;
                }
            }

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Tag is BulkMaterialEditRow bulkRow)
                {
                    foreach (MaterialFieldDescriptor descriptor in selectedDescriptors)
                    {
                        DataGridViewCell gridCell = row.Cells[GetFieldColumnName(descriptor.Label)];
                        ApplyFieldCellTheme(gridCell, bulkRow, bulkRow.GetCell(descriptor));
                    }

                    RefreshGridRowState(row, bulkRow);
                }
            }

            ApplyGridSizing();
        }

        private void ApplyFieldCellTheme(DataGridViewCell gridCell, BulkMaterialEditRow row, BulkMaterialEditCellState cell)
        {
            if (gridCell == null || row == null || cell == null)
                return;

            if (gridCell.ReadOnly && !row.HasLoadError)
            {
                ThemeDefinition currentTheme = RequireTheme();
                gridCell.Style.BackColor = GetReadOnlyCellBackColor();
                gridCell.Style.ForeColor = currentTheme.Palette.Foreground;
                gridCell.Style.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
                gridCell.Style.SelectionForeColor = grid.DefaultCellStyle.SelectionForeColor;
                return;
            }

            gridCell.Style.BackColor = Color.Empty;
            gridCell.Style.ForeColor = Color.Empty;
            gridCell.Style.SelectionBackColor = Color.Empty;
            gridCell.Style.SelectionForeColor = Color.Empty;
        }

        private void ReselectRows(IEnumerable<string> filePaths)
        {
            var selectedPaths = new HashSet<string>(filePaths ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (selectedPaths.Count == 0)
                return;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Tag is BulkMaterialEditRow bulkRow && selectedPaths.Contains(bulkRow.FilePath))
                    row.Selected = true;
            }
        }

        private Color GetReadOnlyCellBackColor()
        {
            return ThemeApplicator.GetReadOnlyBackground(RequireTheme());
        }

        private Color GetLoadErrorBackColor()
        {
            return ThemeApplicator.GetLoadErrorBackground(RequireTheme());
        }

        private Color GetValidationBackColor()
        {
            return ThemeApplicator.GetValidationBackground(RequireTheme());
        }

        private Color GetDirtyRowBackColor()
        {
            return ThemeApplicator.GetDirtyBackground(RequireTheme());
        }

        private void ApplyGridSizing()
        {
            if (grid.Columns.Count == 0)
                return;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.Name is DirtyColumnName or VersionColumnName)
                    continue;

                if (column is DataGridViewCheckBoxColumn)
                {
                    column.Width = 52;
                    continue;
                }

                int preferredWidth = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCellsExceptHeader, true) + 16;
                int minWidth = column.Name == PathColumnName ? 220 : 72;
                int maxWidth = column.Name == PathColumnName
                    ? 460
                    : column is DataGridViewComboBoxColumn
                        ? 220
                        : 320;

                column.Width = Math.Max(minWidth, Math.Min(maxWidth, preferredWidth));
            }

            grid.AutoResizeColumnHeadersHeight();
        }

        private void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private sealed class BulkEditorDataGridView : DataGridView
        {
            public BulkEditorDataGridView()
            {
                DoubleBuffered = true;
            }
        }
    }
}
