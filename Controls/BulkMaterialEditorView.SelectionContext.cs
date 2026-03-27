using Material_Editor.Dialogs;
using Material_Editor.Models;
using Material_Editor.Services;
using Material_Editor.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal sealed partial class BulkMaterialEditorView
    {
        private ToolStripMenuItem sendToMenuItem;
        private ToolStripMenuItem sendToSingleEditorMenuItem;
        private ToolStripMenuItem sendToGenerateVariationsMenuItem;
        private ToolStripMenuItem sendToOverwriteFilesMenuItem;
        private ToolStripMenuItem revealInExplorerMenuItem;
        private ToolStripMenuItem reloadFromDiskMenuItem;
        private ToolStripMenuItem saveFilesMenuItem;
        private ToolStripMenuItem saveFilesAsMenuItem;
        private ToolStripMenuItem closeFilesMenuItem;
        private ToolStripMenuItem selectDirtyRowsMenuItem;
        private ToolStripMenuItem selectErrorRowsMenuItem;
        private ToolStripMenuItem findMenuItem;
        private ToolStripMenuItem findReplaceMenuItem;
        private ToolStripMenuItem editOrToggleMenuItem;
        private ToolStripMenuItem cutFieldsMenuItem;
        private ToolStripMenuItem cutRowsMenuItem;
        private ToolStripMenuItem copyRowsMenuItem;
        private ToolStripMenuItem copyFieldsMenuItem;
        private ToolStripMenuItem pasteRowsMenuItem;
        private ToolStripMenuItem pasteFieldsMenuItem;
        private ToolStripMenuItem clearFieldsMenuItem;
        private ToolStripMenuItem fileFiltersMenuItem;
        private ToolStripMenuItem fileSortMenuItem;
        private ToolStripMenuItem fieldFiltersMenuItem;
        private ToolStripMenuItem fieldSortMenuItem;

        private ContextMenuStrip CreateFileContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Opening += FileContextMenu_Opening;

            sendToSingleEditorMenuItem = new ToolStripMenuItem("Single-file Editor", null, (s, e) => SendToSingleEditorRequested?.Invoke(this, EventArgs.Empty));
            sendToGenerateVariationsMenuItem = new ToolStripMenuItem("Generate Variations", null, (s, e) => SendToGenerateVariationsRequested?.Invoke(this, EventArgs.Empty));
            sendToOverwriteFilesMenuItem = new ToolStripMenuItem("Overwrite Files by Field", null, (s, e) => SendToOverwriteFilesRequested?.Invoke(this, EventArgs.Empty));

            sendToMenuItem = new ToolStripMenuItem("Send to...");
            sendToMenuItem.DropDownItems.Add(sendToSingleEditorMenuItem);
            sendToMenuItem.DropDownItems.Add(sendToGenerateVariationsMenuItem);
            sendToMenuItem.DropDownItems.Add(sendToOverwriteFilesMenuItem);

            revealInExplorerMenuItem = new ToolStripMenuItem("Reveal in Explorer", null, (s, e) => RevealInExplorerRequested?.Invoke(this, EventArgs.Empty));
            reloadFromDiskMenuItem = new ToolStripMenuItem("Reload From Disk", null, (s, e) => ReloadFilesRequested?.Invoke(this, EventArgs.Empty));
            saveFilesMenuItem = new ToolStripMenuItem("Save File(s)", null, (s, e) => SaveFilesRequested?.Invoke(this, EventArgs.Empty));
            saveFilesAsMenuItem = new ToolStripMenuItem("Save File(s) As...", null, (s, e) => SaveFilesAsRequested?.Invoke(this, EventArgs.Empty));
            closeFilesMenuItem = new ToolStripMenuItem("Close File(s)", null, (s, e) => CloseFilesRequested?.Invoke(this, EventArgs.Empty));
            selectDirtyRowsMenuItem = new ToolStripMenuItem("Select Dirty Record(s)", null, (s, e) => SelectRowsByPredicate(IsRowEffectivelyDirty));
            selectErrorRowsMenuItem = new ToolStripMenuItem("Select Errored Record(s)", null, (s, e) => SelectRowsByPredicate(row => row.HasLoadError || row.HasValidationErrors));

            menu.Items.Add(sendToMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(revealInExplorerMenuItem);
            menu.Items.Add(reloadFromDiskMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(saveFilesMenuItem);
            menu.Items.Add(saveFilesAsMenuItem);
            menu.Items.Add(closeFilesMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(CreateProjectionFiltersMenu(out fileFiltersMenuItem));
            menu.Items.Add(CreateProjectionSortMenu(out fileSortMenuItem));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(selectDirtyRowsMenuItem);
            menu.Items.Add(selectErrorRowsMenuItem);
            return menu;
        }

        private ContextMenuStrip CreateFieldContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Opening += FieldContextMenu_Opening;

            findMenuItem = new ToolStripMenuItem("Find...", null, (s, e) => OpenFindDialog(replaceMode: false));
            findReplaceMenuItem = new ToolStripMenuItem("Find and Replace...", null, (s, e) => OpenFindDialog(replaceMode: true));
            editOrToggleMenuItem = new ToolStripMenuItem("Edit/Toggle", null, (s, e) => EditOrToggleSelectedCells());
            cutFieldsMenuItem = new ToolStripMenuItem("Cut", null, (s, e) => CutSelectedFieldsToClipboard());
            cutRowsMenuItem = new ToolStripMenuItem("Cut Row(s)", null, (s, e) => CutSelectedRowsToClipboard());
            copyRowsMenuItem = new ToolStripMenuItem("Copy Row(s)", null, (s, e) => CopySelectedRowsToClipboard());
            copyFieldsMenuItem = new ToolStripMenuItem("Copy Field(s)", null, (s, e) => CopySelectedFieldsToClipboard());
            pasteRowsMenuItem = new ToolStripMenuItem("Paste Row(s)", null, (s, e) => PasteRowsFromClipboard());
            pasteFieldsMenuItem = new ToolStripMenuItem("Paste Field(s)", null, (s, e) => PasteFieldsFromClipboard());
            clearFieldsMenuItem = new ToolStripMenuItem("Clear", null, (s, e) => ClearSelectedFields());

            menu.Items.Add(findMenuItem);
            menu.Items.Add(findReplaceMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(editOrToggleMenuItem);
            menu.Items.Add(cutFieldsMenuItem);
            menu.Items.Add(cutRowsMenuItem);
            menu.Items.Add(copyRowsMenuItem);
            menu.Items.Add(copyFieldsMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(pasteRowsMenuItem);
            menu.Items.Add(pasteFieldsMenuItem);
            menu.Items.Add(clearFieldsMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(CreateProjectionFiltersMenu(out fieldFiltersMenuItem));
            menu.Items.Add(CreateProjectionSortMenu(out fieldSortMenuItem));
            return menu;
        }

        private ToolStripMenuItem CreateProjectionFiltersMenu(out ToolStripMenuItem filtersMenu)
        {
            filtersMenu = new ToolStripMenuItem("Filters");
            filtersMenu.DropDownItems.Add(CreateProjectionFilterItem("All Files", BulkMaterialRowFilter.AllFiles));
            filtersMenu.DropDownItems.Add(CreateProjectionFilterItem("Unique Files Only", BulkMaterialRowFilter.UniqueFilesOnly));
            filtersMenu.DropDownItems.Add(CreateProjectionFilterItem("Duplicate Files", BulkMaterialRowFilter.DuplicateFiles));
            filtersMenu.DropDownItems.Add(CreateProjectionFilterItem("Dirty Files", BulkMaterialRowFilter.DirtyFiles));
            filtersMenu.DropDownItems.Add(CreateProjectionFilterItem("Errored Files", BulkMaterialRowFilter.ErroredFiles));
            filtersMenu.DropDownItems.Add(CreateProjectionFilterItem(SearchResultsFilterName, BulkMaterialRowFilter.CustomFiles));
            return filtersMenu;
        }

        private ToolStripMenuItem CreateProjectionSortMenu(out ToolStripMenuItem sortMenu)
        {
            sortMenu = new ToolStripMenuItem("Sort");
            sortMenu.DropDownItems.Add(CreateProjectionSortDirectionItem("Ascending", BulkMaterialSortDirection.Ascending));
            sortMenu.DropDownItems.Add(CreateProjectionSortDirectionItem("Descending", BulkMaterialSortDirection.Descending));
            sortMenu.DropDownItems.Add(new ToolStripSeparator());
            sortMenu.DropDownItems.Add(CreateProjectionSortKeyItem("Alphabetical", BulkMaterialSortKey.Alphabetical));
            sortMenu.DropDownItems.Add(CreateProjectionSortKeyItem("Last Modified", BulkMaterialSortKey.LastModified));
            sortMenu.DropDownItems.Add(CreateProjectionSortKeyItem("Creation Date", BulkMaterialSortKey.CreationDate));
            sortMenu.DropDownItems.Add(CreateProjectionSortKeyItem("By Addition", BulkMaterialSortKey.ByAddition));
            sortMenu.DropDownItems.Add(new ToolStripSeparator());
            sortMenu.DropDownItems.Add(CreateProjectionGroupByFolderItem());
            return sortMenu;
        }

        private ToolStripMenuItem CreateProjectionFilterItem(string text, BulkMaterialRowFilter filter)
        {
            var item = new ToolStripMenuItem(text)
            {
                Tag = filter
            };
            item.Click += (s, e) => SetRowFilter(filter);
            return item;
        }

        private ToolStripMenuItem CreateProjectionSortKeyItem(string text, BulkMaterialSortKey sortKey)
        {
            var item = new ToolStripMenuItem(text)
            {
                Tag = sortKey
            };
            item.Click += (s, e) => SetSortKey(sortKey);
            return item;
        }

        private ToolStripMenuItem CreateProjectionSortDirectionItem(string text, BulkMaterialSortDirection direction)
        {
            var item = new ToolStripMenuItem(text)
            {
                Tag = direction
            };
            item.Click += (s, e) => SetSortDirection(direction);
            return item;
        }

        private ToolStripMenuItem CreateProjectionGroupByFolderItem()
        {
            var item = new ToolStripMenuItem("Group by Folder")
            {
                Tag = "group-by-folder"
            };
            item.Click += (s, e) => SetGroupByFolder(!IsGroupedByFolder);
            return item;
        }

        private void ApplyContextMenuTheme(AppearanceDefinition appearance)
        {
            if (appearance == null)
                return;

            AppearanceApplicator.ApplyToToolStrip(fileContextMenu, appearance);
            AppearanceApplicator.ApplyToToolStrip(fieldContextMenu, appearance);
        }

        private void FileContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool validContext = contextRowIndex >= 0
                && contextColumnIndex >= 0
                && IsPathColumn(contextColumnIndex)
                && SelectedRows.Count > 0;
            if (!validContext)
            {
                e.Cancel = true;
                return;
            }

            BulkMaterialEditRow selectedRow = SelectedRows.Count == 1 ? SelectedRows[0] : null;
            bool canSendSingle = selectedRow != null && !selectedRow.HasLoadError && selectedRow.Material != null;

            sendToMenuItem.Enabled = canSendSingle;
            sendToSingleEditorMenuItem.Enabled = canSendSingle;
            sendToGenerateVariationsMenuItem.Enabled = canSendSingle;
            sendToOverwriteFilesMenuItem.Enabled = canSendSingle;
            revealInExplorerMenuItem.Enabled = SelectedRows.Count > 0;
            reloadFromDiskMenuItem.Enabled = SelectedRows.Count > 0;
            saveFilesMenuItem.Enabled = SelectedRows.Count > 0;
            saveFilesAsMenuItem.Enabled = SelectedRows.Count > 0;
            closeFilesMenuItem.Enabled = SelectedRows.Count > 0;
            selectDirtyRowsMenuItem.Enabled = session?.Rows.Any(IsRowEffectivelyDirty) == true;
            selectErrorRowsMenuItem.Enabled = session?.Rows.Any(row => row.HasLoadError || row.HasValidationErrors) == true;
            UpdateProjectionMenuState(fileFiltersMenuItem, fileSortMenuItem);
        }

        private void FieldContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            List<DataGridViewCell> selectedFieldCells = GetSelectedFieldCells(editableOnly: false);
            List<DataGridViewCell> selectedEditableCells = GetSelectedFieldCells(editableOnly: true);
            if (contextRowIndex < 0 || contextColumnIndex < 0 || !IsFieldColumn(contextColumnIndex) || selectedFieldCells.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            findMenuItem.Enabled = session != null;
            findReplaceMenuItem.Enabled = session != null;
            editOrToggleMenuItem.Enabled = TryGetEditOrToggleMenuText(selectedEditableCells, out string menuText);
            editOrToggleMenuItem.Text = menuText;
            cutFieldsMenuItem.Enabled = selectedEditableCells.Count > 0;
            cutRowsMenuItem.Enabled = SelectedRows.Count > 0;
            copyRowsMenuItem.Enabled = SelectedRows.Count > 0;
            copyFieldsMenuItem.Enabled = selectedFieldCells.Count > 0;
            pasteRowsMenuItem.Enabled = SelectedRows.Count > 0 && TryGetClipboardMatrix(out _);
            pasteFieldsMenuItem.Enabled = selectedEditableCells.Count > 0 && TryGetClipboardMatrix(out _);
            clearFieldsMenuItem.Enabled = selectedEditableCells.Count > 0;
            UpdateProjectionMenuState(fieldFiltersMenuItem, fieldSortMenuItem);
        }

        private void UpdateProjectionMenuState(ToolStripMenuItem filtersMenu, ToolStripMenuItem sortMenu)
        {
            bool hasSession = session != null;
            if (filtersMenu != null)
            {
                filtersMenu.Enabled = hasSession;
                foreach (ToolStripItem item in filtersMenu.DropDownItems)
                {
                    if (item is ToolStripMenuItem menuItem && menuItem.Tag is BulkMaterialRowFilter filter)
                    {
                        menuItem.Checked = ActiveFilter == filter;
                        menuItem.Enabled = hasSession && (filter != BulkMaterialRowFilter.CustomFiles || HasCustomRowFilter);
                        if (filter == BulkMaterialRowFilter.CustomFiles)
                            menuItem.Text = CustomFilterName;
                    }
                }
            }

            if (sortMenu != null)
            {
                sortMenu.Enabled = hasSession;
                foreach (ToolStripItem item in sortMenu.DropDownItems)
                {
                    if (item is not ToolStripMenuItem menuItem)
                        continue;

                    if (menuItem.Tag is BulkMaterialSortDirection direction)
                        menuItem.Checked = ActiveSortDirection == direction;
                    else if (menuItem.Tag is BulkMaterialSortKey sortKey)
                        menuItem.Checked = ActiveSortKey == sortKey;
                    else if (Equals(menuItem.Tag, "group-by-folder"))
                        menuItem.Checked = IsGroupedByFolder;
                }
            }
        }

        private void HandleGridCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            if (session == null)
                return;

            contextRowIndex = e.RowIndex;
            contextColumnIndex = e.ColumnIndex;
            grid.ContextMenuStrip = null;

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (IsMetaColumn(e.ColumnIndex))
            {
                if (e.Button == MouseButtons.Left)
                {
                    fieldDragActive = false;
                    grid.Capture = true;
                    BeginRowDragSelection(e.RowIndex, Control.ModifierKeys);
                }
                else if (e.Button == MouseButtons.Right)
                    HandleRowSelectorRightClick(e.RowIndex, e.ColumnIndex);

                if (IsPathColumn(e.ColumnIndex))
                    grid.ContextMenuStrip = fileContextMenu;

                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                rowDragActive = false;
                fieldDragActive = false;
                HandleFieldRightClick(e.RowIndex, e.ColumnIndex);
                if (IsFieldColumn(e.ColumnIndex))
                    grid.ContextMenuStrip = fieldContextMenu;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                rowDragActive = false;
                grid.Capture = true;
                BeginFieldDragSelection(e.RowIndex, e.ColumnIndex, Control.ModifierKeys);
            }
        }

        private void HandleGridCellMouseEnter(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (!rowDragActive && !fieldDragActive)
                return;

            if (rowDragActive)
            {
                dragCurrentRowIndex = e.RowIndex;
                dragCurrentColumnIndex = GetPathColumnIndex();
                UpdateDragSelectionPreview(
                    true,
                    rowDragAnchorIndex,
                    GetPathColumnIndex(),
                    e.RowIndex,
                    GetPathColumnIndex());
                return;
            }

            if (!fieldDragActive || !IsFieldColumn(e.ColumnIndex))
                return;

            dragCurrentRowIndex = e.RowIndex;
            dragCurrentColumnIndex = e.ColumnIndex;
            UpdateDragSelectionPreview(
                false,
                fieldDragAnchorRowIndex,
                fieldDragAnchorColumnIndex,
                e.RowIndex,
                e.ColumnIndex);
        }

        private void HandleGridCellMouseUp(DataGridViewCellMouseEventArgs e)
        {
            FinalizeActiveDragSelection();
            grid.Capture = false;
            rowDragActive = false;
            rowDragAnchorIndex = -1;
            rowDragAdditive = false;
            fieldDragActive = false;
            fieldDragAnchorRowIndex = -1;
            fieldDragAnchorColumnIndex = -1;
            fieldDragAdditive = false;
            RefreshSummary();
        }

        private void HandleGridMouseMove(MouseEventArgs e)
        {
            if (!rowDragActive && !fieldDragActive)
                return;

            DataGridView.HitTestInfo hit = grid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
                return;

            if (rowDragActive)
            {
                dragCurrentRowIndex = hit.RowIndex;
                dragCurrentColumnIndex = GetPathColumnIndex();
                UpdateDragSelectionPreview(
                    true,
                    rowDragAnchorIndex,
                    GetPathColumnIndex(),
                    hit.RowIndex,
                    GetPathColumnIndex());
                return;
            }

            if (!IsFieldColumn(hit.ColumnIndex))
                return;

            dragCurrentRowIndex = hit.RowIndex;
            dragCurrentColumnIndex = hit.ColumnIndex;
            UpdateDragSelectionPreview(
                false,
                fieldDragAnchorRowIndex,
                fieldDragAnchorColumnIndex,
                hit.RowIndex,
                hit.ColumnIndex);
        }

        private void HandleGridMouseUp(MouseEventArgs e)
        {
            FinalizeActiveDragSelection();
            grid.Capture = false;
            rowDragActive = false;
            rowDragAnchorIndex = -1;
            rowDragAdditive = false;
            fieldDragActive = false;
            fieldDragAnchorRowIndex = -1;
            fieldDragAnchorColumnIndex = -1;
            fieldDragAdditive = false;
            RefreshSummary();
        }

        private bool HandleGridSelectionShortcut(KeyEventArgs e)
        {
            if (grid.Rows.Count == 0 || grid.CurrentCell == null)
                return false;

            if (HandleGridArrowSelectionShortcut(e))
                return true;

            bool handled = false;
            bool extendSelection = e.Shift;
            bool rowMode = IsMetaColumn(grid.CurrentCell.ColumnIndex);
            int targetRow = grid.CurrentCell.RowIndex;
            int targetColumn = grid.CurrentCell.ColumnIndex;
            int pageSize = Math.Max(1, grid.DisplayedRowCount(false) - 1);

            switch (e.KeyCode)
            {
                case Keys.Home:
                    if (e.Control)
                    {
                        targetRow = 0;
                        targetColumn = rowMode ? GetPathColumnIndex() : 0;
                    }
                    else
                    {
                        targetColumn = rowMode ? GetPathColumnIndex() : 0;
                    }
                    handled = true;
                    break;
                case Keys.End:
                    if (e.Control)
                    {
                        targetRow = grid.Rows.Count - 1;
                        targetColumn = rowMode ? GetPathColumnIndex() : grid.Columns.Count - 1;
                    }
                    else
                    {
                        targetColumn = rowMode ? GetPathColumnIndex() : grid.Columns.Count - 1;
                    }
                    handled = true;
                    break;
                case Keys.PageUp:
                    targetRow = e.Control ? 0 : Math.Max(0, targetRow - pageSize);
                    handled = true;
                    break;
                case Keys.PageDown:
                    targetRow = e.Control ? grid.Rows.Count - 1 : Math.Min(grid.Rows.Count - 1, targetRow + pageSize);
                    handled = true;
                    break;
            }

            if (!handled)
                return false;

            if (!extendSelection)
                SetSelectionAnchor(grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex);
            else
                EnsureSelectionAnchor();

            if (rowMode)
            {
                SelectRowRange(selectionAnchorRowIndex, targetRow, clearExisting: true);
                SetCurrentCellPreservingSelection(targetRow, GetPathColumnIndex());
            }
            else
            {
                SelectCellRectangle(selectionAnchorRowIndex, selectionAnchorColumnIndex, targetRow, targetColumn, clearExisting: true);
                SetCurrentCellPreservingSelection(targetRow, targetColumn);
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            RefreshSummary();
            return true;
        }

        private bool HandleGridArrowSelectionShortcut(KeyEventArgs e)
        {
            if (!e.Shift || grid.CurrentCell == null)
                return false;

            int rowIndex = grid.CurrentCell.RowIndex;
            int columnIndex = grid.CurrentCell.ColumnIndex;
            int targetRow = rowIndex;
            int targetColumn = columnIndex;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    targetColumn = Math.Max(0, columnIndex - 1);
                    break;
                case Keys.Right:
                    targetColumn = Math.Min(grid.Columns.Count - 1, columnIndex + 1);
                    break;
                case Keys.Up:
                    targetRow = Math.Max(0, rowIndex - 1);
                    break;
                case Keys.Down:
                    targetRow = Math.Min(grid.Rows.Count - 1, rowIndex + 1);
                    break;
                default:
                    return false;
            }

            EnsureSelectionAnchor();
            if (IsMetaColumn(grid.CurrentCell.ColumnIndex))
            {
                SelectRowRange(selectionAnchorRowIndex, targetRow, clearExisting: true);
                SetCurrentCellPreservingSelection(targetRow, GetPathColumnIndex());
            }
            else
            {
                SelectCellRectangle(selectionAnchorRowIndex, selectionAnchorColumnIndex, targetRow, targetColumn, clearExisting: true);
                SetCurrentCellPreservingSelection(targetRow, targetColumn);
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            RefreshSummary();
            return true;
        }

        private bool HandleGridEditShortcut(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.R)
            {
                ReloadFilesRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.T)
            {
                CycleRowFilter(e.Shift ? -1 : 1);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            if (grid.CurrentCell == null || grid.CurrentCell.RowIndex < 0 || grid.CurrentCell.ColumnIndex < 0)
                return false;

            if (e.KeyCode == Keys.F2)
            {
                BeginEditCurrentCellOrOpenDialog();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.KeyCode == Keys.Enter && !e.Control && !e.Shift && !e.Alt)
            {
                BeginEditCurrentCellOrOpenDialog();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.Enter)
            {
                SelectCurrentRow();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            if (!e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Delete)
            {
                if (GetSelectedFieldCells(editableOnly: true).Count == 0)
                    return false;

                ClearSelectedFields();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.Z && ExecuteUndo())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.Y && ExecuteRedo())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.Q)
            {
                SelectRowsByPredicate(e.Shift
                    ? row => row.HasLoadError || row.HasValidationErrors
                    : IsRowEffectivelyDirty);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return true;
            }

            return false;
        }

        private bool HandleGridClipboardShortcut(KeyEventArgs e)
        {
            if (!e.Control)
                return false;

            bool rowShortcut = e.Shift;
            switch (e.KeyCode)
            {
                case Keys.C:
                    if (rowShortcut)
                    {
                        if (SelectedRows.Count == 0)
                            return false;

                        CopySelectedRowsToClipboard();
                    }
                    else if (GetSelectedFieldCells(editableOnly: false).Count > 0)
                        CopySelectedFieldsToClipboard();
                    else if (SelectedRows.Count > 0)
                        CopySelectedRowsToClipboard();
                    else
                        return false;

                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return true;
                case Keys.X:
                    if (rowShortcut)
                    {
                        if (SelectedRows.Count == 0)
                            return false;

                        CutSelectedRowsToClipboard();
                    }
                    else if (GetSelectedFieldCells(editableOnly: true).Count > 0)
                        CutSelectedFieldsToClipboard();
                    else
                        return false;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return true;
                case Keys.V:
                    if (rowShortcut)
                    {
                        if (SelectedRows.Count == 0)
                            return false;

                        PasteRowsFromClipboard();
                    }
                    else if (GetSelectedFieldCells(editableOnly: true).Count > 0)
                        PasteFieldsFromClipboard();
                    else if (SelectedRows.Count > 0)
                        PasteRowsFromClipboard();
                    else
                        return false;

                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return true;
            }

            return false;
        }

        private void EditOrToggleSelectedCells()
        {
            List<DataGridViewCell> selectedEditableCells = GetSelectedFieldCells(editableOnly: true);
            if (selectedEditableCells.Count == 0)
                return;

            MaterialFieldDescriptor[] descriptors = selectedEditableCells
                .Select(cell => TryGetFieldDescriptor(grid.Columns[cell.ColumnIndex].Name))
                .Where(descriptor => descriptor != null)
                .Distinct()
                .ToArray();

            if (descriptors.Length == 0)
                return;

            if (descriptors.All(descriptor => descriptor.EditorKind == BulkFieldEditorKind.Boolean))
            {
                foreach (DataGridViewCell cell in selectedEditableCells.OrderBy(cell => cell.RowIndex).ThenBy(cell => cell.ColumnIndex))
                    TryToggleBooleanCell(cell.RowIndex, cell.ColumnIndex);

                return;
            }

            if (descriptors.Length != 1)
                return;

            MaterialFieldDescriptor descriptor = descriptors[0];
            DataGridViewCell focusCell = selectedEditableCells[0];
            if (grid.Rows[focusCell.RowIndex].Tag is not BulkMaterialEditRow row)
                return;

            string initialValue = invalidCellTexts.TryGetValue((row.FilePath, descriptor.Label), out string invalidText)
                ? invalidText
                : row.GetCell(descriptor).GetDisplayText();

            using var dialog = new TextPromptDialog(
                $"Edit {descriptor.Label}",
                $"Enter a value to apply to {selectedEditableCells.Count} selected cell(s) in '{descriptor.Label}'.",
                initialValue);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            var targets = selectedEditableCells
                .Select(cell => BuildPasteTarget(cell.RowIndex, cell.ColumnIndex, dialog.PromptValue))
                .Where(target => target.HasValue)
                .Select(target => target.Value)
                .ToList();
            var selection = selectedEditableCells
                .Select(cell => BuildCellReference(cell.RowIndex, cell.ColumnIndex))
                .Where(reference => reference.HasValue)
                .Select(reference => reference.Value)
                .ToList();

            ApplyPasteTargets(targets, selection.Select(reference => reference.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), selection, row.FilePath, grid.Columns[focusCell.ColumnIndex].Name);
        }

        private void BeginEditCurrentCellOrOpenDialog()
        {
            if (grid.CurrentCell == null || grid.CurrentCell.RowIndex < 0 || grid.CurrentCell.ColumnIndex < 0)
                return;

            MaterialFieldDescriptor descriptor = TryGetFieldDescriptor(grid.Columns[grid.CurrentCell.ColumnIndex].Name);
            if (descriptor == null || grid.CurrentCell.ReadOnly)
                return;

            if (descriptor.EditorKind == BulkFieldEditorKind.Boolean)
            {
                TryToggleBooleanCell(grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex);
                return;
            }

            if (descriptor.EditorKind == BulkFieldEditorKind.Enum)
            {
                grid.BeginEdit(true);
                return;
            }

            if (GetSelectedFieldCells(editableOnly: true).Count <= 1)
            {
                grid.BeginEdit(true);
                return;
            }

            EditOrToggleSelectedCells();
        }

        private void FillUpSelectedFields()
        {
            List<DataGridViewCell> selectedEditableCells = GetSelectedFieldCells(editableOnly: true);
            if (!CanFillUpSelectedFields(selectedEditableCells))
                return;

            var selectedByColumn = selectedEditableCells
                .GroupBy(cell => cell.ColumnIndex)
                .OrderBy(group => group.Key);
            var targets = new List<(string FilePath, string ColumnName, string Value)>();

            foreach (IGrouping<int, DataGridViewCell> columnGroup in selectedByColumn)
            {
                List<DataGridViewCell> cellsInColumn = columnGroup.OrderByDescending(cell => cell.RowIndex).ToList();
                DataGridViewCell sourceCell = cellsInColumn[0];
                if (grid.Rows[sourceCell.RowIndex].Tag is not BulkMaterialEditRow sourceRow
                    || TryGetFieldDescriptor(grid.Columns[sourceCell.ColumnIndex].Name) is not MaterialFieldDescriptor descriptor)
                {
                    continue;
                }

                string sourceValue = GetClipboardText(sourceRow, descriptor);
                foreach (DataGridViewCell targetCell in cellsInColumn.Skip(1))
                {
                    var target = BuildPasteTarget(targetCell.RowIndex, targetCell.ColumnIndex, sourceValue);
                    if (target.HasValue)
                        targets.Add(target.Value);
                }
            }

            ApplyFillOperationTargets(selectedEditableCells, targets);
        }

        private void FillDownSelectedFields()
        {
            List<DataGridViewCell> selectedEditableCells = GetSelectedFieldCells(editableOnly: true);
            if (!CanFillDownSelectedFields(selectedEditableCells))
                return;

            var selectedByColumn = selectedEditableCells
                .GroupBy(cell => cell.ColumnIndex)
                .OrderBy(group => group.Key);
            var targets = new List<(string FilePath, string ColumnName, string Value)>();
            var reselection = new List<(string FilePath, string ColumnName)>();

            foreach (IGrouping<int, DataGridViewCell> columnGroup in selectedByColumn)
            {
                List<DataGridViewCell> cellsInColumn = columnGroup.OrderBy(cell => cell.RowIndex).ToList();
                DataGridViewCell sourceCell = cellsInColumn[0];
                if (grid.Rows[sourceCell.RowIndex].Tag is not BulkMaterialEditRow sourceRow
                    || TryGetFieldDescriptor(grid.Columns[sourceCell.ColumnIndex].Name) is not MaterialFieldDescriptor descriptor)
                {
                    continue;
                }

                string sourceValue = GetClipboardText(sourceRow, descriptor);
                foreach (DataGridViewCell targetCell in cellsInColumn.Skip(1))
                {
                    var target = BuildPasteTarget(targetCell.RowIndex, targetCell.ColumnIndex, sourceValue);
                    var reference = BuildCellReference(targetCell.RowIndex, targetCell.ColumnIndex);
                    if (!target.HasValue || !reference.HasValue)
                        continue;

                    targets.Add(target.Value);
                    reselection.Add(reference.Value);
                }
            }

            ApplyFillOperationTargets(selectedEditableCells, targets);
        }

        private void ApplyFillOperationTargets(
            IReadOnlyList<DataGridViewCell> selectedEditableCells,
            IReadOnlyList<(string FilePath, string ColumnName, string Value)> targets)
        {
            if (selectedEditableCells == null || selectedEditableCells.Count == 0 || targets == null || targets.Count == 0)
                return;

            List<(string FilePath, string ColumnName)> fullSelection = selectedEditableCells
                .Select(cell => BuildCellReference(cell.RowIndex, cell.ColumnIndex))
                .Where(reference => reference.HasValue)
                .Select(reference => reference.Value)
                .ToList();

            if (fullSelection.Count == 0)
                return;

            ApplyPasteTargets(
                targets,
                fullSelection.Select(reference => reference.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                fullSelection,
                fullSelection[0].FilePath,
                fullSelection[0].ColumnName);
        }

        private void CutSelectedFieldsToClipboard()
        {
            if (!TryCopySelectedFieldsToClipboard())
                return;

            ClearSelectedFields();
        }

        private void CopySelectedRowsToClipboard()
        {
            TryCopySelectedRowsToClipboard();
        }

        private bool TryCopySelectedRowsToClipboard()
        {
            if (SelectedRows.Count == 0)
                return false;

            var lines = new List<string>(SelectedRows.Count);
            foreach (BulkMaterialEditRow row in SelectedRows)
            {
                string[] values = selectedDescriptors
                    .Select(descriptor => GetClipboardText(row, descriptor))
                    .ToArray();
                lines.Add(string.Join("\t", values));
            }

            return TrySetClipboardText(string.Join(Environment.NewLine, lines), "copy rows");
        }

        private void CutSelectedRowsToClipboard()
        {
            BulkMaterialEditRow[] targetRows = SelectedRows.ToArray();
            if (targetRows.Length == 0 || !TryCopySelectedRowsToClipboard())
                return;

            var targets = new List<(string FilePath, string ColumnName, string Value)>();
            foreach (BulkMaterialEditRow row in targetRows)
            {
                foreach (MaterialFieldDescriptor descriptor in selectedDescriptors)
                {
                    targets.Add((
                        row.FilePath,
                        GetFieldColumnName(descriptor.Label),
                        GetCutValue(descriptor)));
                }
            }

            if (targets.Count == 0)
                return;

            string focusFilePath = targetRows[0].FilePath;
            string focusColumnName = selectedDescriptors.Count > 0 ? GetFieldColumnName(selectedDescriptors[0].Label) : string.Empty;
            ApplyPasteTargets(targets, targetRows.Select(row => row.FilePath).ToArray(), null, focusFilePath, focusColumnName);
        }

        private void CopySelectedFieldsToClipboard()
        {
            TryCopySelectedFieldsToClipboard();
        }

        private bool TryCopySelectedFieldsToClipboard()
        {
            List<DataGridViewCell> selectedFieldCells = GetSelectedFieldCells(editableOnly: false);
            if (selectedFieldCells.Count == 0)
                return false;

            int[] rows = selectedFieldCells.Select(cell => cell.RowIndex).Distinct().OrderBy(index => index).ToArray();
            int[] columns = selectedFieldCells.Select(cell => cell.ColumnIndex).Distinct().OrderBy(index => index).ToArray();
            var selectedKeys = new HashSet<(int Row, int Column)>(selectedFieldCells.Select(cell => (cell.RowIndex, cell.ColumnIndex)));
            var builder = new StringBuilder();

            for (int rowPosition = 0; rowPosition < rows.Length; rowPosition++)
            {
                if (rowPosition > 0)
                    builder.AppendLine();

                int rowIndex = rows[rowPosition];
                for (int columnPosition = 0; columnPosition < columns.Length; columnPosition++)
                {
                    if (columnPosition > 0)
                        builder.Append('\t');

                    int columnIndex = columns[columnPosition];
                    if (!selectedKeys.Contains((rowIndex, columnIndex))
                        || grid.Rows[rowIndex].Tag is not BulkMaterialEditRow row
                        || TryGetFieldDescriptor(grid.Columns[columnIndex].Name) is not MaterialFieldDescriptor descriptor)
                    {
                        continue;
                    }

                    builder.Append(GetClipboardText(row, descriptor));
                }
            }

            return TrySetClipboardText(builder.ToString(), "copy fields");
        }

        private void ClearSelectedFields()
        {
            List<DataGridViewCell> selectedEditableCells = GetSelectedFieldCells(editableOnly: true);
            if (selectedEditableCells.Count == 0)
                return;

            var targets = new List<(string FilePath, string ColumnName, string Value)>();
            var reselection = new List<(string FilePath, string ColumnName)>();
            foreach (DataGridViewCell cell in selectedEditableCells)
            {
                if (grid.Rows[cell.RowIndex].Tag is not BulkMaterialEditRow row
                    || TryGetFieldDescriptor(grid.Columns[cell.ColumnIndex].Name) is not MaterialFieldDescriptor descriptor)
                {
                    continue;
                }

                targets.Add((row.FilePath, grid.Columns[cell.ColumnIndex].Name, GetCutValue(descriptor)));
                reselection.Add((row.FilePath, grid.Columns[cell.ColumnIndex].Name));
            }

            if (targets.Count == 0)
                return;

            string focusFilePath = reselection[0].FilePath;
            string focusColumnName = reselection[0].ColumnName;
            ApplyPasteTargets(
                targets,
                reselection.Select(reference => reference.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                reselection,
                focusFilePath,
                focusColumnName);
        }

        private void PasteRowsFromClipboard()
        {
            if (!TryGetClipboardMatrix(out List<string[]> matrix) || matrix.Count == 0 || SelectedRows.Count == 0)
                return;

            BulkMaterialEditRow[] targetRows = SelectedRows.ToArray();
            var targets = new List<(string FilePath, string ColumnName, string Value)>();

            for (int rowIndex = 0; rowIndex < targetRows.Length; rowIndex++)
            {
                string[] sourceValues = matrix[Math.Min(rowIndex, matrix.Count - 1)];
                for (int columnIndex = 0; columnIndex < selectedDescriptors.Count && columnIndex < sourceValues.Length; columnIndex++)
                    targets.Add((targetRows[rowIndex].FilePath, GetFieldColumnName(selectedDescriptors[columnIndex].Label), sourceValues[columnIndex]));
            }

            string focusFilePath = targetRows[0].FilePath;
            string focusColumnName = selectedDescriptors.Count > 0 ? GetFieldColumnName(selectedDescriptors[0].Label) : string.Empty;
            ApplyPasteTargets(targets, targetRows.Select(row => row.FilePath).ToArray(), null, focusFilePath, focusColumnName);
        }

        private void PasteFieldsFromClipboard()
        {
            List<DataGridViewCell> selectedEditableCells = GetSelectedFieldCells(editableOnly: true);
            if (!TryGetClipboardMatrix(out List<string[]> matrix) || matrix.Count == 0 || selectedEditableCells.Count == 0)
                return;

            int[] targetRows = selectedEditableCells.Select(cell => cell.RowIndex).Distinct().OrderBy(index => index).ToArray();
            int[] targetColumns = selectedEditableCells.Select(cell => cell.ColumnIndex).Distinct().OrderBy(index => index).ToArray();
            bool fillSelection = matrix.Count == 1 && matrix[0].Length == 1;
            var selectedKeys = new HashSet<(int Row, int Column)>(selectedEditableCells.Select(cell => (cell.RowIndex, cell.ColumnIndex)));
            var targets = new List<(string FilePath, string ColumnName, string Value)>();
            var reselection = new List<(string FilePath, string ColumnName)>();

            for (int rowPosition = 0; rowPosition < targetRows.Length; rowPosition++)
            {
                for (int columnPosition = 0; columnPosition < targetColumns.Length; columnPosition++)
                {
                    int rowIndex = targetRows[rowPosition];
                    int columnIndex = targetColumns[columnPosition];
                    if (!selectedKeys.Contains((rowIndex, columnIndex)))
                        continue;

                    string value = fillSelection
                        ? matrix[0][0]
                        : GetMatrixValue(matrix, rowPosition, columnPosition);
                    if (value == null)
                        continue;

                    var target = BuildPasteTarget(rowIndex, columnIndex, value);
                    var reference = BuildCellReference(rowIndex, columnIndex);
                    if (!target.HasValue || !reference.HasValue)
                        continue;

                    targets.Add(target.Value);
                    reselection.Add(reference.Value);
                }
            }

            if (targets.Count == 0)
                return;

            string focusFilePath = reselection[0].FilePath;
            string focusColumnName = reselection[0].ColumnName;
            ApplyPasteTargets(targets, reselection.Select(reference => reference.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), reselection, focusFilePath, focusColumnName);
        }

        private IReadOnlyList<BulkMaterialEditRow> GetOrderedSelectedRows()
        {
            if (grid == null || grid.Rows.Count == 0)
                return Array.Empty<BulkMaterialEditRow>();

            int[] selectedIndexes = grid.SelectedCells
                .Cast<DataGridViewCell>()
                .Select(cell => cell.RowIndex)
                .Where(index => index >= 0 && index < grid.Rows.Count)
                .Distinct()
                .OrderBy(index => index)
                .ToArray();

            if (selectedIndexes.Length == 0 && grid.CurrentCell != null)
                selectedIndexes = new[] { grid.CurrentCell.RowIndex };

            return selectedIndexes
                .Select(index => grid.Rows[index].Tag as BulkMaterialEditRow)
                .Where(row => row != null)
                .Distinct()
                .ToArray();
        }

        private void ApplyPasteTargets(
            IReadOnlyList<(string FilePath, string ColumnName, string Value)> targets,
            IReadOnlyCollection<string> rowPathsToRestore,
            IReadOnlyCollection<(string FilePath, string ColumnName)> cellsToRestore,
            string focusFilePath,
            string focusColumnName)
        {
            if (targets == null || targets.Count == 0 || session == null)
                return;

            CommitPendingEdits();

            bool visibilityChanged = false;
            var visibilityAffectedRows = new HashSet<BulkMaterialEditRow>();
            var changedRows = new HashSet<BulkMaterialEditRow>();
            var originalValues = new Dictionary<(string FilePath, string Label), object>();
            List<(string FilePath, string ColumnName, string Value)> pendingTargets = targets.ToList();
            suppressGridEvents = true;
            try
            {
                bool madeProgress;
                do
                {
                    madeProgress = false;
                    var deferredTargets = new List<(string FilePath, string ColumnName, string Value)>();
                    var changedRowsThisPass = new HashSet<BulkMaterialEditRow>();

                    foreach ((string filePath, string columnName, string value) in pendingTargets)
                    {
                        if (!TryResolveGridCell(filePath, columnName, out _, out DataGridViewCell gridCell, out BulkMaterialEditRow row, out MaterialFieldDescriptor descriptor)
                            || gridCell.ReadOnly)
                        {
                            continue;
                        }

                        if (!TryApplyCellValue(row, descriptor, gridCell, value, value ?? string.Empty, originalValues))
                        {
                            deferredTargets.Add((filePath, columnName, value));
                            continue;
                        }

                        changedRowsThisPass.Add(row);
                        changedRows.Add(row);
                        if (VisibilityDrivenFieldMap.ContainsKey(descriptor.Label))
                        {
                            visibilityAffectedRows.Add(row);
                            visibilityChanged = true;
                        }

                        madeProgress = true;
                    }

                    foreach (BulkMaterialEditRow row in changedRowsThisPass)
                        row.RefreshDynamicState();

                    pendingTargets = deferredTargets;
                }
                while (madeProgress && pendingTargets.Count > 0);
            }
            finally
            {
                suppressGridEvents = false;
            }

            foreach (BulkMaterialEditRow row in visibilityAffectedRows)
                row.RefreshDynamicState();

            ClearStaleValidationErrors(changedRows.Concat(visibilityAffectedRows));
            RecordUndoBatch(originalValues);

            bool rebuilt = visibilityChanged && TryRefreshVisibilityDrivenColumns(rowPathsToRestore, focusFilePath, focusColumnName);
            if (!rebuilt)
                RefreshAllGridRows();

            if (cellsToRestore != null && cellsToRestore.Count > 0)
                ReselectCells(cellsToRestore);
            else if (rowPathsToRestore != null && rowPathsToRestore.Count > 0)
                ReselectRows(rowPathsToRestore);

            RestoreCurrentCell(focusFilePath, focusColumnName);
            RefreshSummary();
        }

        private void SelectCurrentRow()
        {
            if (grid.CurrentCell == null || grid.CurrentCell.RowIndex < 0)
                return;

            int rowIndex = grid.CurrentCell.RowIndex;
            SelectRowRange(rowIndex, rowIndex, clearExisting: true);
            SetCurrentCellPreservingSelection(rowIndex, GetPathColumnIndex());
            SetSelectionAnchor(rowIndex, GetPathColumnIndex());
            RefreshSummary();
        }

        private void SelectCurrentViewportPage(int direction)
        {
            if (grid.CurrentCell == null || grid.Rows.Count == 0)
                return;

            int currentRow = grid.CurrentCell.RowIndex;
            int pageSize = Math.Max(1, grid.DisplayedRowCount(false) - 1);
            int targetRow = direction < 0
                ? Math.Max(0, currentRow - pageSize)
                : Math.Min(grid.Rows.Count - 1, currentRow + pageSize);

            EnsureSelectionAnchor();
            ExtendCurrentSelectionToRow(targetRow);
        }

        private void SelectToBoundary(int direction)
        {
            if (grid.CurrentCell == null || grid.Rows.Count == 0)
                return;

            EnsureSelectionAnchor();
            ExtendCurrentSelectionToRow(direction < 0 ? 0 : grid.Rows.Count - 1);
        }

        private void ExtendCurrentSelectionToRow(int targetRow)
        {
            bool rowMode = IsMetaColumn(grid.CurrentCell?.ColumnIndex ?? -1);
            targetRow = Math.Max(0, Math.Min(grid.Rows.Count - 1, targetRow));

            if (rowMode)
            {
                SelectRowRange(selectionAnchorRowIndex, targetRow, clearExisting: true);
                SetCurrentCellPreservingSelection(targetRow, GetPathColumnIndex());
            }
            else
            {
                SelectCellRectangle(selectionAnchorRowIndex, selectionAnchorColumnIndex, targetRow, grid.CurrentCell.ColumnIndex, clearExisting: true);
                SetCurrentCellPreservingSelection(targetRow, grid.CurrentCell.ColumnIndex);
            }

            RefreshSummary();
        }

        private void BeginRowDragSelection(int rowIndex, Keys modifiers)
        {
            bool extendSelection = modifiers.HasFlag(Keys.Shift) && selectionAnchorRowIndex >= 0;
            rowDragActive = true;
            rowDragAdditive = modifiers.HasFlag(Keys.Control) && !extendSelection;
            rowDragAnchorIndex = extendSelection ? selectionAnchorRowIndex : rowIndex;
            fieldDragActive = false;
            dragCurrentRowIndex = rowIndex;
            dragCurrentColumnIndex = GetPathColumnIndex();

            if (!extendSelection)
                SetSelectionAnchor(rowIndex, GetPathColumnIndex());

            UpdateDragSelectionPreview(
                true,
                rowDragAnchorIndex,
                GetPathColumnIndex(),
                rowIndex,
                GetPathColumnIndex());
        }

        private void BeginFieldDragSelection(int rowIndex, int columnIndex, Keys modifiers)
        {
            bool extendSelection = modifiers.HasFlag(Keys.Shift) && selectionAnchorRowIndex >= 0 && selectionAnchorColumnIndex >= 0;
            fieldDragActive = true;
            fieldDragAdditive = modifiers.HasFlag(Keys.Control) && !extendSelection;
            fieldDragAnchorRowIndex = extendSelection ? selectionAnchorRowIndex : rowIndex;
            fieldDragAnchorColumnIndex = extendSelection ? selectionAnchorColumnIndex : columnIndex;
            rowDragActive = false;
            dragCurrentRowIndex = rowIndex;
            dragCurrentColumnIndex = columnIndex;

            if (!extendSelection)
                SetSelectionAnchor(rowIndex, columnIndex);

            UpdateDragSelectionPreview(
                false,
                fieldDragAnchorRowIndex,
                fieldDragAnchorColumnIndex,
                rowIndex,
                columnIndex);
        }

        private void HandleRowSelectorRightClick(int rowIndex, int columnIndex)
        {
            if (!IsRowSelected(rowIndex))
                SelectRowRange(rowIndex, rowIndex, clearExisting: true);

            ClearDragSelectionPreview();
            SetCurrentCellPreservingSelection(rowIndex, columnIndex);
            SetSelectionAnchor(rowIndex, columnIndex);
            rowDragActive = false;
            rowDragAnchorIndex = -1;
            rowDragAdditive = false;
            fieldDragActive = false;
            dragCurrentRowIndex = rowIndex;
            dragCurrentColumnIndex = columnIndex;
            RefreshSummary();
        }

        private void HandleFieldRightClick(int rowIndex, int columnIndex)
        {
            DataGridViewCell clickedCell = grid.Rows[rowIndex].Cells[columnIndex];
            if (!clickedCell.Selected)
                SelectCellRectangle(rowIndex, columnIndex, rowIndex, columnIndex, clearExisting: true);

            ClearDragSelectionPreview();
            SetCurrentCellPreservingSelection(rowIndex, columnIndex);
            SetSelectionAnchor(rowIndex, columnIndex);
            rowDragActive = false;
            fieldDragActive = false;
            dragCurrentRowIndex = rowIndex;
            dragCurrentColumnIndex = columnIndex;
            RefreshSummary();
        }

        private void SelectRowRange(int startRowIndex, int endRowIndex, bool clearExisting)
        {
            if (grid.Rows.Count == 0)
                return;

            startRowIndex = Math.Max(0, Math.Min(grid.Rows.Count - 1, startRowIndex));
            endRowIndex = Math.Max(0, Math.Min(grid.Rows.Count - 1, endRowIndex));
            if (startRowIndex > endRowIndex)
                (startRowIndex, endRowIndex) = (endRowIndex, startRowIndex);

            PerformSelectionMutation(() =>
            {
                if (clearExisting)
                    grid.ClearSelection();

                for (int rowIndex = startRowIndex; rowIndex <= endRowIndex; rowIndex++)
                {
                    foreach (DataGridViewCell cell in grid.Rows[rowIndex].Cells)
                        cell.Selected = true;
                }
            });
        }

        private void SelectCellRectangle(int startRowIndex, int startColumnIndex, int endRowIndex, int endColumnIndex, bool clearExisting)
        {
            if (grid.Rows.Count == 0 || grid.Columns.Count == 0)
                return;

            startRowIndex = Math.Max(0, Math.Min(grid.Rows.Count - 1, startRowIndex));
            endRowIndex = Math.Max(0, Math.Min(grid.Rows.Count - 1, endRowIndex));
            startColumnIndex = Math.Max(0, Math.Min(grid.Columns.Count - 1, startColumnIndex));
            endColumnIndex = Math.Max(0, Math.Min(grid.Columns.Count - 1, endColumnIndex));

            if (startRowIndex > endRowIndex)
                (startRowIndex, endRowIndex) = (endRowIndex, startRowIndex);
            if (startColumnIndex > endColumnIndex)
                (startColumnIndex, endColumnIndex) = (endColumnIndex, startColumnIndex);

            PerformSelectionMutation(() =>
            {
                if (clearExisting)
                    grid.ClearSelection();

                for (int rowIndex = startRowIndex; rowIndex <= endRowIndex; rowIndex++)
                {
                    for (int columnIndex = startColumnIndex; columnIndex <= endColumnIndex; columnIndex++)
                        grid.Rows[rowIndex].Cells[columnIndex].Selected = true;
                }
            });
        }

        private void FinalizeActiveDragSelection()
        {
            if (rowDragActive && rowDragAnchorIndex >= 0 && dragCurrentRowIndex >= 0)
            {
                ClearDragSelectionPreview();
                SelectRowRange(rowDragAnchorIndex, dragCurrentRowIndex, clearExisting: !rowDragAdditive);
                SetCurrentCellPreservingSelection(dragCurrentRowIndex, GetPathColumnIndex());
                dragCurrentRowIndex = -1;
                dragCurrentColumnIndex = -1;
                return;
            }

            if (fieldDragActive
                && fieldDragAnchorRowIndex >= 0
                && fieldDragAnchorColumnIndex >= 0
                && dragCurrentRowIndex >= 0
                && dragCurrentColumnIndex >= 0)
            {
                ClearDragSelectionPreview();
                SelectCellRectangle(
                    fieldDragAnchorRowIndex,
                    fieldDragAnchorColumnIndex,
                    dragCurrentRowIndex,
                    dragCurrentColumnIndex,
                    clearExisting: !fieldDragAdditive);
                SetCurrentCellPreservingSelection(dragCurrentRowIndex, dragCurrentColumnIndex);
                dragCurrentRowIndex = -1;
                dragCurrentColumnIndex = -1;
                return;
            }

            if (dragCurrentRowIndex < 0 || dragCurrentColumnIndex < 0)
            {
                ClearDragSelectionPreview();
                return;
            }

            ClearDragSelectionPreview();
            SetCurrentCellPreservingSelection(dragCurrentRowIndex, dragCurrentColumnIndex);
            dragCurrentRowIndex = -1;
            dragCurrentColumnIndex = -1;
        }

        private void ReselectCells(IReadOnlyCollection<(string FilePath, string ColumnName)> cellReferences)
        {
            if (cellReferences == null || cellReferences.Count == 0)
                return;

            PerformSelectionMutation(() =>
            {
                grid.ClearSelection();
                bool currentCellAssigned = false;
                foreach ((string filePath, string columnName) in cellReferences)
                {
                    if (!TryResolveGridCell(filePath, columnName, out _, out DataGridViewCell gridCell, out _, out _))
                        continue;

                    gridCell.Selected = true;
                    if (!currentCellAssigned)
                    {
                        grid.CurrentCell = gridCell;
                        currentCellAssigned = true;
                    }
                }
            });
        }

        private bool TryResolveGridCell(
            string filePath,
            string columnName,
            out int rowIndex,
            out DataGridViewCell gridCell,
            out BulkMaterialEditRow row,
            out MaterialFieldDescriptor descriptor)
        {
            rowIndex = -1;
            gridCell = null;
            row = null;
            descriptor = TryGetFieldDescriptor(columnName);
            if (descriptor == null || !grid.Columns.Contains(columnName))
                return false;

            for (int index = 0; index < grid.Rows.Count; index++)
            {
                if (grid.Rows[index].Tag is not BulkMaterialEditRow candidate
                    || !string.Equals(candidate.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                rowIndex = index;
                row = candidate;
                gridCell = grid.Rows[index].Cells[columnName];
                return true;
            }

            return false;
        }

        private bool TryGetEditOrToggleMenuText(IReadOnlyCollection<DataGridViewCell> selectedEditableCells, out string menuText)
        {
            menuText = "Edit/Toggle";
            if (selectedEditableCells == null || selectedEditableCells.Count == 0)
                return false;

            MaterialFieldDescriptor[] descriptors = selectedEditableCells
                .Select(cell => TryGetFieldDescriptor(grid.Columns[cell.ColumnIndex].Name))
                .Where(descriptor => descriptor != null)
                .Distinct()
                .ToArray();

            if (descriptors.Length == 0)
                return false;

            if (descriptors.All(descriptor => descriptor.EditorKind == BulkFieldEditorKind.Boolean))
            {
                menuText = "Toggle";
                return true;
            }

            if (descriptors.Length == 1)
            {
                menuText = "Edit...";
                return true;
            }

            return false;
        }

        private bool CanFillDownSelectedFields(IReadOnlyCollection<DataGridViewCell> selectedEditableCells)
        {
            if (selectedEditableCells == null || selectedEditableCells.Count < 2)
                return false;

            return selectedEditableCells
                .GroupBy(cell => cell.ColumnIndex)
                .Any(group => group.Select(cell => cell.RowIndex).Distinct().Count() > 1);
        }

        private bool CanFillUpSelectedFields(IReadOnlyCollection<DataGridViewCell> selectedEditableCells)
        {
            return CanFillDownSelectedFields(selectedEditableCells);
        }

        private void SelectSingleCell(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count || columnIndex < 0 || columnIndex >= grid.Columns.Count)
                return;

            PerformSelectionMutation(() =>
            {
                grid.ClearSelection();
                DataGridViewCell cell = grid.Rows[rowIndex].Cells[columnIndex];
                cell.Selected = true;
                grid.CurrentCell = cell;
            });
            SetSelectionAnchor(rowIndex, columnIndex);
            CenterCurrentCellColumn();
            RefreshSummary();
        }

        private int ReplaceAllMatchesInColumn(int columnIndex, MaterialFieldDescriptor descriptor, string findText, string replaceText)
        {
            if (columnIndex < 0 || descriptor == null || string.IsNullOrEmpty(findText))
                return 0;

            return ApplyReplaceRequest(
                FindMatches(new BulkFindReplaceRequest
                {
                    Scope = BulkFindScope.ByField,
                    ValueType = BulkFindValueType.Text,
                    FindText = findText ?? string.Empty,
                    ReplaceText = replaceText ?? string.Empty,
                    FieldLabels = new List<string> { descriptor.Label }
                }),
                new BulkFindReplaceRequest
                {
                    Scope = BulkFindScope.ByField,
                    ValueType = BulkFindValueType.Text,
                    FindText = findText ?? string.Empty,
                    ReplaceText = replaceText ?? string.Empty,
                    FieldLabels = new List<string> { descriptor.Label }
                });
        }

        private List<DataGridViewCell> GetSelectedFieldCells(bool editableOnly)
        {
            return grid.SelectedCells
                .Cast<DataGridViewCell>()
                .Where(cell => cell.RowIndex >= 0 && cell.ColumnIndex >= 0 && IsFieldColumn(cell.ColumnIndex))
                .Where(cell => !editableOnly || !cell.ReadOnly)
                .OrderBy(cell => cell.RowIndex)
                .ThenBy(cell => cell.ColumnIndex)
                .ToList();
        }

        private string GetClipboardText(BulkMaterialEditRow row, MaterialFieldDescriptor descriptor)
        {
            if (row == null || descriptor == null)
                return string.Empty;

            if (invalidCellTexts.TryGetValue((row.FilePath, descriptor.Label), out string invalidText))
                return invalidText;

            return row.GetCell(descriptor).GetDisplayText();
        }

        private bool TryGetClipboardMatrix(out List<string[]> matrix)
        {
            matrix = new List<string[]>();
            if (!TryGetClipboardText(out string text) || string.IsNullOrWhiteSpace(text))
                return false;

            string[] rows = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string row in rows)
            {
                if (row.Length == 0 && rows.Length > 1 && matrix.Count == rows.Length - 1)
                    continue;

                matrix.Add(row.Split('\t'));
            }

            return matrix.Count > 0;
        }

        private static string GetMatrixValue(IReadOnlyList<string[]> matrix, int rowIndex, int columnIndex)
        {
            if (matrix == null || matrix.Count == 0 || rowIndex >= matrix.Count)
                return null;

            string[] row = matrix[rowIndex];
            if (row == null || columnIndex >= row.Length)
                return null;

            return row[columnIndex];
        }

        private static bool TryGetClipboardText(out string text)
        {
            try
            {
                text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
                return true;
            }
            catch
            {
                text = string.Empty;
                return false;
            }
        }

        private static string GetCutValue(MaterialFieldDescriptor descriptor)
        {
            if (descriptor == null)
                return string.Empty;

            return descriptor.EditorKind switch
            {
                BulkFieldEditorKind.Boolean => bool.FalseString,
                BulkFieldEditorKind.Number => "0",
                BulkFieldEditorKind.Enum => descriptor.EnumType != null && descriptor.EnumType.IsEnum
                    ? Enum.GetNames(descriptor.EnumType).FirstOrDefault() ?? string.Empty
                    : string.Empty,
                BulkFieldEditorKind.Color => "#000000",
                _ => string.Empty
            };
        }

        private bool TrySetClipboardText(string text, string actionDescription)
        {
            try
            {
                Clipboard.SetText(text ?? string.Empty);
                return true;
            }
            catch
            {
                MessageBox.Show(FindForm(), $"Unable to {actionDescription}. The clipboard is not currently available.", "Clipboard Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }

        private void SelectRowsByPredicate(Func<BulkMaterialEditRow, bool> predicate)
        {
            if (predicate == null)
                return;

            PerformSelectionMutation(() =>
            {
                grid.ClearSelection();
                bool currentCellAssigned = false;
                foreach (DataGridViewRow gridRow in grid.Rows)
                {
                    if (gridRow.Tag is not BulkMaterialEditRow row || !predicate(row))
                        continue;

                    foreach (DataGridViewCell cell in gridRow.Cells)
                        cell.Selected = true;

                    if (!currentCellAssigned)
                    {
                        grid.CurrentCell = gridRow.Cells[GetPathColumnIndex()];
                        currentCellAssigned = true;
                    }
                }
            });

            RefreshSummary();
        }

        private void SetSelectionAnchor(int rowIndex, int columnIndex)
        {
            selectionAnchorRowIndex = rowIndex;
            selectionAnchorColumnIndex = columnIndex;
        }

        private void EnsureSelectionAnchor()
        {
            if (selectionAnchorRowIndex >= 0 && selectionAnchorColumnIndex >= 0)
                return;

            if (grid.CurrentCell != null)
                SetSelectionAnchor(grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex);
        }

        private void SetCurrentCellSafe(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count || columnIndex < 0 || columnIndex >= grid.Columns.Count)
                return;

            grid.CurrentCell = grid.Rows[rowIndex].Cells[columnIndex];
            CenterCurrentCellColumn();
        }

        private void SetCurrentCellPreservingSelection(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count || columnIndex < 0 || columnIndex >= grid.Columns.Count)
                return;

            List<(int RowIndex, int ColumnIndex)> selectedCells = grid.SelectedCells
                .Cast<DataGridViewCell>()
                .Select(cell => (cell.RowIndex, cell.ColumnIndex))
                .ToList();

            PerformSelectionMutation(() =>
            {
                grid.CurrentCell = grid.Rows[rowIndex].Cells[columnIndex];
                foreach ((int selectedRowIndex, int selectedColumnIndex) in selectedCells)
                {
                    if (selectedRowIndex < 0
                        || selectedRowIndex >= grid.Rows.Count
                        || selectedColumnIndex < 0
                        || selectedColumnIndex >= grid.Columns.Count)
                    {
                        continue;
                    }

                    grid.Rows[selectedRowIndex].Cells[selectedColumnIndex].Selected = true;
                }
            });

            CenterCurrentCellColumn();
        }

        private bool IsRowSelected(int rowIndex)
        {
            return grid.SelectedCells.Cast<DataGridViewCell>().Any(cell => cell.RowIndex == rowIndex);
        }

        private bool IsMetaColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= grid.Columns.Count)
                return false;

            string name = grid.Columns[columnIndex].Name;
            return name is DirtyColumnName or VersionColumnName or PathColumnName;
        }

        private bool IsPathColumn(int columnIndex)
        {
            return columnIndex >= 0
                && columnIndex < grid.Columns.Count
                && string.Equals(grid.Columns[columnIndex].Name, PathColumnName, StringComparison.Ordinal);
        }

        private bool IsFieldColumn(int columnIndex)
        {
            return columnIndex >= 0
                && columnIndex < grid.Columns.Count
                && TryGetFieldDescriptor(grid.Columns[columnIndex].Name) != null;
        }

        private int GetPathColumnIndex()
        {
            return grid.Columns.Contains(PathColumnName)
                ? grid.Columns[PathColumnName].Index
                : Math.Min(2, Math.Max(0, grid.Columns.Count - 1));
        }

        private (string FilePath, string ColumnName)? BuildCellReference(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count || columnIndex < 0 || columnIndex >= grid.Columns.Count)
                return null;

            return grid.Rows[rowIndex].Tag is BulkMaterialEditRow row
                ? (row.FilePath, grid.Columns[columnIndex].Name)
                : null;
        }

        private (string FilePath, string ColumnName, string Value)? BuildPasteTarget(int rowIndex, int columnIndex, string value)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count || columnIndex < 0 || columnIndex >= grid.Columns.Count)
                return null;

            return grid.Rows[rowIndex].Tag is BulkMaterialEditRow row
                ? (row.FilePath, grid.Columns[columnIndex].Name, value)
                : null;
        }
    }
}
