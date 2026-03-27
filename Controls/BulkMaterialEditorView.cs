using Material_Editor.Dialogs;
using Material_Editor.Models;
using Material_Editor.Services;
using Material_Editor.Theming;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal sealed partial class BulkMaterialEditorView : ThemeAwareUserControl
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
                [ControlNames.Terrain] = new[] { ControlNames.UnkInt1BGSM, ControlNames.TerrainThresholdFalloff, ControlNames.TerrainTilingDistance, ControlNames.TerrainRotationAngle },
                [ControlNames.EnvMapping] = new[] { ControlNames.EnvMappingMaskScale },
                [ControlNames.GlassEnabled] = new[] { ControlNames.GlassFresnelColor, ControlNames.GlassBlurScaleBase, ControlNames.GlassBlurScaleFactor, ControlNames.GlassRefractionScaleBase },
                [ControlNames.FalloffEnabled] = new[] { ControlNames.FalloffStartAngle, ControlNames.FalloffStopAngle, ControlNames.FalloffStartOpacity, ControlNames.FalloffStopOpacity },
                [ControlNames.SoftEnabled] = new[] { ControlNames.SoftDepth }
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
        private readonly ContextMenuStrip fileContextMenu;
        private readonly ContextMenuStrip fieldContextMenu;
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
        private int selectionAnchorRowIndex = -1;
        private int selectionAnchorColumnIndex = -1;
        private int rowDragAnchorIndex = -1;
        private bool rowDragActive;
        private bool rowDragAdditive;
        private int fieldDragAnchorRowIndex = -1;
        private int fieldDragAnchorColumnIndex = -1;
        private bool fieldDragActive;
        private bool fieldDragAdditive;
        private int dragCurrentRowIndex = -1;
        private int dragCurrentColumnIndex = -1;
        private int contextRowIndex = -1;
        private int contextColumnIndex = -1;
        private bool dragPreviewActive;
        private bool dragPreviewRowMode;
        private int dragPreviewStartRowIndex = -1;
        private int dragPreviewStartColumnIndex = -1;
        private int dragPreviewEndRowIndex = -1;
        private int dragPreviewEndColumnIndex = -1;
        private int selectionRefreshSuppressionDepth;

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
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = true,
                EditMode = DataGridViewEditMode.EditProgrammatically,
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
            grid.CellMouseDown += Grid_CellMouseDown;
            grid.CellMouseEnter += Grid_CellMouseEnter;
            grid.CellMouseUp += Grid_CellMouseUp;
            grid.CellDoubleClick += Grid_CellDoubleClick;
            grid.MouseMove += Grid_MouseMove;
            grid.MouseUp += Grid_MouseUp;
            grid.Paint += Grid_Paint;
            grid.SelectionChanged += Grid_SelectionChanged;
            grid.KeyDown += Grid_KeyDown;
            mainLayout.Controls.Add(grid, 0, 1);

            fileContextMenu = CreateFileContextMenu();
            fieldContextMenu = CreateFieldContextMenu();

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
                Text = "Create timestamped backups in backup folder",
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
        public event EventHandler SaveFilesRequested;
        public event EventHandler SaveFilesAsRequested;
        public event EventHandler CloseFilesRequested;
        public event EventHandler RevealInExplorerRequested;
        public event EventHandler ReloadFilesRequested;
        public event EventHandler SendToSingleEditorRequested;
        public event EventHandler SendToGenerateVariationsRequested;
        public event EventHandler SendToOverwriteFilesRequested;

        public BulkMaterialEditSession Session => session;
        public bool BackupBeforeWrite => backupCheckBox.Checked;
        public IReadOnlyList<MaterialFieldDescriptor> SelectedDescriptors => selectedDescriptors;
        public bool HasDirtyRows => session != null && session.Rows.Any(IsRowEffectivelyDirty);
        public int DirtyRowCount => session?.Rows.Count(IsRowEffectivelyDirty) ?? 0;
        public IReadOnlyList<BulkMaterialEditRow> SelectedRows => GetOrderedSelectedRows();

        public int SelectedDirtyRowCount => SelectedRows.Count(IsRowEffectivelyDirty);
        public int SelectedRowCount => SelectedRows.Count;
        public bool CanFindSelection => session != null;
        public bool CanFindReplaceSelection => session != null;
        public bool CanUndo => session?.CanUndo == true;
        public bool CanRedo => session?.CanRedo == true;
        public bool CanEditOrToggleSelection => TryGetEditOrToggleMenuText(GetSelectedFieldCells(editableOnly: true), out _);
        public bool CanCutFieldsSelection => GetSelectedFieldCells(editableOnly: true).Count > 0;
        public bool CanCutRowsSelection => SelectedRows.Count > 0;
        public bool CanCopyRowsSelection => SelectedRows.Count > 0;
        public bool CanCopyFieldsSelection => GetSelectedFieldCells(editableOnly: false).Count > 0;
        public bool CanPasteRowsSelection => SelectedRows.Count > 0 && TryGetClipboardMatrix(out _);
        public bool CanPasteFieldsSelection => GetSelectedFieldCells(editableOnly: true).Count > 0 && TryGetClipboardMatrix(out _);
        public bool CanClearSelection => GetSelectedFieldCells(editableOnly: true).Count > 0;
        public bool CanSelectAll => session != null && grid.Rows.Count > 0;
        public bool CanSelectCurrentRow => session != null && grid.CurrentCell != null && grid.CurrentCell.RowIndex >= 0;
        public bool CanSelectDirtyRows => session?.Rows.Any(IsRowEffectivelyDirty) == true;
        public bool CanSelectErrorRows => session?.Rows.Any(row => row.HasLoadError || row.HasValidationErrors) == true;

        public void Initialize(BulkMaterialEditSession session, Config config, bool backupBeforeWrite)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.config = config;
            backupCheckBox.Checked = backupBeforeWrite;
            ResetProjectionState();
            selectedLabelPreferences = BuildDefaultSelectedLabelPreferences(session);
            invalidCellTexts.Clear();
            pendingBooleanOverrides.Clear();
            pendingDirtyRowPaths.Clear();
            Visible = true;
            RefreshVisibleDescriptorsAndGrid(refreshSummary: false);
            ApplyCurrentAppearance();
            RefreshSummary();
        }

        public void ClearSession()
        {
            session = null;
            ResetProjectionState();
            selectedLabelPreferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            selectedDescriptors = new List<MaterialFieldDescriptor>();
            invalidCellTexts.Clear();
            pendingBooleanOverrides.Clear();
            pendingDirtyRowPaths.Clear();
            selectionAnchorRowIndex = -1;
            selectionAnchorColumnIndex = -1;
            rowDragAnchorIndex = -1;
            rowDragActive = false;
            rowDragAdditive = false;
            fieldDragAnchorRowIndex = -1;
            fieldDragAnchorColumnIndex = -1;
            fieldDragActive = false;
            fieldDragAdditive = false;
            dragCurrentRowIndex = -1;
            dragCurrentColumnIndex = -1;
            contextRowIndex = -1;
            contextColumnIndex = -1;
            ClearDragSelectionPreview();
            grid.Rows.Clear();
            grid.Columns.Clear();
            summaryLabel.Text = string.Empty;
            validationLabel.Text = string.Empty;
            Visible = false;
            OnStateChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (activeComboEditingControl != null)
                {
                    activeComboEditingControl.DrawItem -= ComboEditingControl_DrawItem;
                    activeComboEditingControl = null;
                }

                grid.ContextMenuStrip = null;
                fileContextMenu?.Dispose();
                fieldContextMenu?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            this.theme = appearance.Theme;
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.FormBackground);
            validationLabel.ForeColor = string.IsNullOrEmpty(validationLabel.Text) ? appearance.Theme.Palette.Foreground : appearance.Theme.Semantics.Error;
            ApplyGridThemeStyles();
            ApplyContextMenuTheme(appearance);
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
            RefreshVisibleDescriptorsAndGrid();
            return result;
        }

        public void NotifySessionChanged()
        {
            pendingBooleanOverrides.Clear();
            pendingDirtyRowPaths.Clear();
            foreach (BulkMaterialEditRow row in session?.Rows ?? Array.Empty<BulkMaterialEditRow>())
                row.RefreshDynamicState();

            RefreshVisibleDescriptorsAndGrid();
        }

        public IReadOnlyList<FieldCopyResult> SaveAllChanges()
        {
            if (session == null)
                return Array.Empty<FieldCopyResult>();

            CommitPendingEdits();
            ClearStaleValidationErrors(visibleRows);
            var results = session.ApplySelectedChanges(visibleRows, BackupBeforeWrite, config);
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
            ClearStaleValidationErrors(selectedRows);
            var results = session.ApplySelectedChanges(selectedRows, BackupBeforeWrite, config);
            RebuildGrid();
            ReselectRows(selectedRows.Select(row => row.FilePath));
            RefreshSummary();
            return results;
        }

        public bool IsDirtyRow(BulkMaterialEditRow row)
        {
            return IsRowEffectivelyDirty(row);
        }

        public void SelectFileRows(IEnumerable<string> filePaths)
        {
            ReselectRows(filePaths);
            RefreshSummary();
        }

        public void ExecuteFindSelection()
        {
            OpenFindDialog(replaceMode: false);
        }

        public void ExecuteFindReplaceSelection()
        {
            OpenFindDialog(replaceMode: true);
        }

        public bool ExecuteUndo()
        {
            return UndoLastChange();
        }

        public bool ExecuteRedo()
        {
            return RedoLastChange();
        }

        public void ExecuteEditOrToggleSelection()
        {
            EditOrToggleSelectedCells();
        }

        public void ExecuteCutFields()
        {
            CutSelectedFieldsToClipboard();
        }

        public void ExecuteCopyRows()
        {
            CopySelectedRowsToClipboard();
        }

        public void ExecuteCutRows()
        {
            CutSelectedRowsToClipboard();
        }

        public void ExecuteCopyFields()
        {
            CopySelectedFieldsToClipboard();
        }

        public void ExecutePasteRows()
        {
            PasteRowsFromClipboard();
        }

        public void ExecutePasteFields()
        {
            PasteFieldsFromClipboard();
        }

        public void ExecuteClearSelection()
        {
            ClearSelectedFields();
        }

        public void ExecuteSelectAll()
        {
            if (grid.Rows.Count == 0)
                return;

            grid.SelectAll();
            RefreshSummary();
        }

        public void ExecuteSelectCurrentRow()
        {
            SelectCurrentRow();
        }

        public void ExecuteSelectPageAbove()
        {
            SelectCurrentViewportPage(direction: -1);
        }

        public void ExecuteSelectPageBelow()
        {
            SelectCurrentViewportPage(direction: 1);
        }

        public void ExecuteSelectAllAbove()
        {
            SelectToBoundary(direction: -1);
        }

        public void ExecuteSelectAllBelow()
        {
            SelectToBoundary(direction: 1);
        }

        public void ExecuteSelectDirtyRows()
        {
            SelectRowsByPredicate(IsRowEffectivelyDirty);
        }

        public void ExecuteSelectErrorRows()
        {
            SelectRowsByPredicate(row => row.HasLoadError || row.HasValidationErrors);
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
            RefreshVisibleDescriptorsAndGrid();
        }

        public void ClearStaleValidationErrors(IEnumerable<BulkMaterialEditRow> targetRows = null)
        {
            if (session == null)
                return;

            IReadOnlyCollection<BulkMaterialEditRow> rowsToCheck = (targetRows ?? session.Rows)
                .Where(row => row != null)
                .Distinct()
                .ToArray();
            if (rowsToCheck.Count == 0)
                return;

            foreach (BulkMaterialEditRow row in rowsToCheck)
            {
                bool rowIsClean = !row.IsDirty;
                foreach (BulkMaterialEditCellState cell in row.Cells)
                {
                    if (cell == null)
                        continue;

                    (string FilePath, string Label) key = (row.FilePath, cell.Descriptor.Label);
                    bool hasInvalidUiText = invalidCellTexts.ContainsKey(key);
                    bool hasPendingBoolean = pendingBooleanOverrides.ContainsKey(key);

                    if (rowIsClean)
                    {
                        invalidCellTexts.Remove(key);
                        pendingBooleanOverrides.Remove(key);
                        cell.ClearError();
                        continue;
                    }

                    if (!cell.HasError)
                        continue;

                    if (hasInvalidUiText || hasPendingBoolean)
                        continue;

                    cell.ClearError();
                }

                row.RefreshDynamicState();
            }
        }

        private void RebuildGrid()
        {
            suppressGridEvents = true;
            try
            {
                grid.SuspendLayout();
                grid.Rows.Clear();
                grid.Columns.Clear();
                visibleRows = BuildVisibleRows();

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

                foreach (BulkMaterialEditRow row in visibleRows)
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
            gridRow.Cells[PathColumnName].Value = row.DisplayPath;
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
                gridRow.DefaultCellStyle.BackColor = Color.Empty;

            gridRow.DefaultCellStyle.ForeColor = row.HasLoadError || row.HasValidationErrors || isDirty
                ? ThemeApplicator.GetEditableForeground(RequireTheme())
                : Color.Empty;
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

        private void Grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            HandleGridCellMouseDown(e);
        }

        private void Grid_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            HandleGridCellMouseEnter(e);
        }

        private void Grid_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            HandleGridCellMouseUp(e);
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (suppressGridEvents || session == null || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            MaterialFieldDescriptor descriptor = TryGetFieldDescriptor(grid.Columns[e.ColumnIndex].Name);
            if (descriptor == null)
                return;

            if (descriptor.EditorKind == BulkFieldEditorKind.Boolean)
            {
                TryToggleBooleanCell(e.RowIndex, e.ColumnIndex);
                return;
            }

            if (grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly)
                return;

            SelectSingleCell(e.RowIndex, e.ColumnIndex);
            BeginEditCurrentCellOrOpenDialog();
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            HandleGridMouseMove(e);
        }

        private void Grid_MouseUp(object sender, MouseEventArgs e)
        {
            HandleGridMouseUp(e);
        }

        private void Grid_Paint(object sender, PaintEventArgs e)
        {
            PaintDragSelectionPreview(e.Graphics);
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (selectionRefreshSuppressionDepth > 0)
                return;

            RefreshSummary();
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
                textBox.ForeColor = ThemeApplicator.GetEditableForeground(currentTheme);
                return;
            }

            if (e.Control is ComboBox comboBox)
            {
                ThemeDefinition currentTheme = RequireTheme();
                comboBox.FlatStyle = FlatStyle.Flat;
                comboBox.BackColor = currentTheme.Palette.ControlBackground;
                comboBox.ForeColor = ThemeApplicator.GetEditableForeground(currentTheme);
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
                    ThemeApplicator.GetEditableForeground(currentTheme),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            e.DrawFocusRectangle();
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (HandleGridEditShortcut(e))
                return;

            if (HandleGridSelectionShortcut(e))
                return;

            if (HandleGridClipboardShortcut(e))
                return;

            if (grid.CurrentCell != null
                && e.KeyCode == Keys.Space
                && !e.Control
                && !e.Alt
                && TryToggleBooleanCell(grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex, e))
                return;

            if (e.Control && e.KeyCode == Keys.A)
            {
                grid.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
                RefreshSummary();
                return;
            }
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
            TryApplyCellValue(row, descriptor, gridCell, inputValue, Convert.ToString(gridCell.Value) ?? string.Empty);
            HandlePostEditRefresh(rowIndex, row, descriptor, selectedPaths, focusFilePath, focusColumnName);
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
            int visibleLoadedCount = visibleRows.Count(row => !row.HasLoadError);
            int visibleDirtyCount = visibleRows.Count(IsRowEffectivelyDirty);
            int visibleErrorCount = visibleRows.Count(row => row.HasValidationErrors);
            int visibleLoadErrorCount = visibleRows.Count(row => row.HasLoadError);
            summaryLabel.Text = $"Filter: {GetFilterDisplayName(ActiveFilter)}. {visibleRows.Count} shown of {session.Rows.Count}, {visibleLoadedCount} loaded shown, {visibleDirtyCount} dirty shown, {SelectedDirtyRowCount} selected dirty, {visibleErrorCount} shown with validation errors, {visibleLoadErrorCount} shown failed to load. {SelectedRowCount} row(s) selected. Backups {(backupCheckBox.Checked ? "on" : "off")}.";

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
            grid.GridColor = ThemeApplicator.GetTableBorderColor(currentTheme);
            grid.DefaultCellStyle.BackColor = currentTheme.Palette.PanelBackground;
            grid.DefaultCellStyle.ForeColor = ThemeApplicator.GetEditableForeground(currentTheme);
            grid.DefaultCellStyle.SelectionBackColor = currentTheme.Palette.Accent.IsEmpty ? currentTheme.Palette.MenuBackground : currentTheme.Palette.Accent;
            grid.DefaultCellStyle.SelectionForeColor = ThemeApplicator.GetEditableForeground(currentTheme);
            grid.AlternatingRowsDefaultCellStyle.BackColor = ThemeApplicator.GetAlternatingRowBackground(currentTheme);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = ThemeApplicator.GetEditableForeground(currentTheme);
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.SelectionForeColor;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.DefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
                column.DefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.SelectionForeColor;

                if (column.Name is DirtyColumnName or VersionColumnName or PathColumnName)
                {
                    column.DefaultCellStyle.BackColor = ThemeApplicator.GetFrozenColumnBackground(currentTheme);
                    column.DefaultCellStyle.ForeColor = ThemeApplicator.GetEditableForeground(currentTheme);
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

        private void UpdateDragSelectionPreview(bool rowMode, int startRowIndex, int startColumnIndex, int endRowIndex, int endColumnIndex)
        {
            Rectangle previousBounds = GetDragSelectionPreviewBounds();
            dragPreviewActive = true;
            dragPreviewRowMode = rowMode;
            dragPreviewStartRowIndex = startRowIndex;
            dragPreviewStartColumnIndex = startColumnIndex;
            dragPreviewEndRowIndex = endRowIndex;
            dragPreviewEndColumnIndex = endColumnIndex;
            InvalidateDragSelectionPreview(previousBounds, GetDragSelectionPreviewBounds());
        }

        private void ClearDragSelectionPreview()
        {
            Rectangle previousBounds = GetDragSelectionPreviewBounds();
            dragPreviewActive = false;
            dragPreviewRowMode = false;
            dragPreviewStartRowIndex = -1;
            dragPreviewStartColumnIndex = -1;
            dragPreviewEndRowIndex = -1;
            dragPreviewEndColumnIndex = -1;
            InvalidateDragSelectionPreview(previousBounds, Rectangle.Empty);
        }

        private void InvalidateDragSelectionPreview(Rectangle previousBounds, Rectangle nextBounds)
        {
            Rectangle invalidationBounds = previousBounds;
            if (!nextBounds.IsEmpty)
                invalidationBounds = invalidationBounds.IsEmpty ? nextBounds : Rectangle.Union(invalidationBounds, nextBounds);

            if (invalidationBounds.IsEmpty)
                return;

            invalidationBounds.Inflate(2, 2);
            grid.Invalidate(invalidationBounds);
        }

        private Rectangle GetDragSelectionPreviewBounds()
        {
            if (!dragPreviewActive
                || grid.Rows.Count == 0
                || grid.Columns.Count == 0
                || dragPreviewStartRowIndex < 0
                || dragPreviewEndRowIndex < 0)
            {
                return Rectangle.Empty;
            }

            int startRowIndex = Math.Max(0, Math.Min(dragPreviewStartRowIndex, dragPreviewEndRowIndex));
            int endRowIndex = Math.Min(grid.Rows.Count - 1, Math.Max(dragPreviewStartRowIndex, dragPreviewEndRowIndex));

            Rectangle topRowBounds = grid.GetRowDisplayRectangle(startRowIndex, true);
            Rectangle bottomRowBounds = grid.GetRowDisplayRectangle(endRowIndex, true);
            if (topRowBounds.Height <= 0 || bottomRowBounds.Height <= 0)
                return Rectangle.Empty;

            if (dragPreviewRowMode)
            {
                Rectangle visibleColumnsBounds = GetVisibleColumnBounds();
                if (visibleColumnsBounds.Width <= 0)
                    return Rectangle.Empty;

                return Rectangle.FromLTRB(
                    visibleColumnsBounds.Left,
                    topRowBounds.Top,
                    visibleColumnsBounds.Right,
                    bottomRowBounds.Bottom);
            }

            int startColumnIndex = Math.Max(0, Math.Min(dragPreviewStartColumnIndex, dragPreviewEndColumnIndex));
            int endColumnIndex = Math.Min(grid.Columns.Count - 1, Math.Max(dragPreviewStartColumnIndex, dragPreviewEndColumnIndex));
            Rectangle leftColumnBounds = grid.GetColumnDisplayRectangle(startColumnIndex, true);
            Rectangle rightColumnBounds = grid.GetColumnDisplayRectangle(endColumnIndex, true);
            if (leftColumnBounds.Width <= 0 || rightColumnBounds.Width <= 0)
                return Rectangle.Empty;

            return Rectangle.FromLTRB(
                leftColumnBounds.Left,
                topRowBounds.Top,
                rightColumnBounds.Right,
                bottomRowBounds.Bottom);
        }

        private Rectangle GetVisibleColumnBounds()
        {
            int left = int.MaxValue;
            int right = int.MinValue;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible)
                    continue;

                Rectangle bounds = grid.GetColumnDisplayRectangle(column.Index, true);
                if (bounds.Width <= 0)
                    continue;

                left = Math.Min(left, bounds.Left);
                right = Math.Max(right, bounds.Right);
            }

            return left == int.MaxValue || right <= left
                ? Rectangle.Empty
                : Rectangle.FromLTRB(left, 0, right, grid.Height);
        }

        private void PaintDragSelectionPreview(Graphics graphics)
        {
            Rectangle previewBounds = GetDragSelectionPreviewBounds();
            if (previewBounds.IsEmpty)
                return;

            Color selectionColor = grid.DefaultCellStyle.SelectionBackColor;
            if (selectionColor.IsEmpty)
                selectionColor = RequireTheme().Palette.Accent;

            using var fillBrush = new SolidBrush(Color.FromArgb(96, selectionColor));
            using var borderPen = new Pen(selectionColor);
            graphics.FillRectangle(fillBrush, previewBounds);
            graphics.DrawRectangle(borderPen, previewBounds.Left, previewBounds.Top, Math.Max(0, previewBounds.Width - 1), Math.Max(0, previewBounds.Height - 1));
        }

        private void PerformSelectionMutation(Action action)
        {
            if (action == null)
                return;

            selectionRefreshSuppressionDepth++;
            try
            {
                grid.SuspendLayout();
                action();
            }
            finally
            {
                grid.ResumeLayout();
                selectionRefreshSuppressionDepth--;
            }
        }

        private void ApplyFieldCellTheme(DataGridViewCell gridCell, BulkMaterialEditRow row, BulkMaterialEditCellState cell)
        {
            if (gridCell == null || row == null || cell == null)
                return;

            if (gridCell.ReadOnly && !row.HasLoadError)
            {
                ThemeDefinition currentTheme = RequireTheme();
                gridCell.Style.BackColor = GetReadOnlyCellBackColor();
                gridCell.Style.ForeColor = ThemeApplicator.GetEditableForeground(currentTheme);
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

            PerformSelectionMutation(() =>
            {
                grid.ClearSelection();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Tag is BulkMaterialEditRow bulkRow && selectedPaths.Contains(bulkRow.FilePath))
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                            cell.Selected = true;
                    }
                }
            });
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
