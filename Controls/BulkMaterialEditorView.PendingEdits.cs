using Material_Editor.Models;
using Material_Editor.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal sealed partial class BulkMaterialEditorView
    {
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
            var affectedRows = new HashSet<BulkMaterialEditRow>();

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
                    affectedRows.Add(row);
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

            ClearStaleValidationErrors(affectedRows);

            if (visibilityChanged)
            {
                RefreshVisibleDescriptorsAndGrid(refreshSummary: false);
            }
            else
            {
                RefreshAllGridRows();
            }

            RefreshSummary();
        }

        private bool TryApplyCellValue(
            BulkMaterialEditRow row,
            MaterialFieldDescriptor descriptor,
            DataGridViewCell gridCell,
            object inputValue,
            string invalidText,
            Dictionary<(string FilePath, string Label), object> originalValues = null)
        {
            if (originalValues != null)
                CaptureOriginalCellValue(originalValues, row, descriptor);

            bool recordUndo = originalValues == null;
            if (!session.TrySetCellValue(row, descriptor, inputValue, out string errorMessage, recordUndo))
            {
                invalidCellTexts[(row.FilePath, descriptor.Label)] = invalidText;
                gridCell.ErrorText = errorMessage ?? "Invalid value.";
                return false;
            }

            suppressGridEvents = true;
            try
            {
                invalidCellTexts.Remove((row.FilePath, descriptor.Label));
                gridCell.Value = row.GetCell(descriptor).GetGridValue();
                gridCell.ErrorText = string.Empty;
            }
            finally
            {
                suppressGridEvents = false;
            }

            return true;
        }

        private static void CaptureOriginalCellValue(
            IDictionary<(string FilePath, string Label), object> originalValues,
            BulkMaterialEditRow row,
            MaterialFieldDescriptor descriptor)
        {
            if (originalValues == null || row == null || descriptor == null)
                return;

            (string FilePath, string Label) key = (row.FilePath, descriptor.Label);
            if (!originalValues.ContainsKey(key))
                originalValues[key] = row.GetCell(descriptor).CurrentValue;
        }

        private void RecordUndoBatch(Dictionary<(string FilePath, string Label), object> originalValues)
        {
            if (session == null || originalValues == null || originalValues.Count == 0)
                return;

            var changes = new List<BulkMaterialEditUndoChange>();
            foreach (((string filePath, string label) key, object previousValue) in originalValues)
            {
                BulkMaterialEditRow row = session.Rows.FirstOrDefault(candidate => string.Equals(candidate.FilePath, key.filePath, StringComparison.OrdinalIgnoreCase));
                MaterialFieldDescriptor descriptor = session.FindDescriptor(key.label);
                if (row == null || descriptor == null)
                    continue;

                object currentValue = row.GetCell(descriptor).CurrentValue;
                if (BulkMaterialEditValueComparer.ValuesEqual(previousValue, currentValue))
                    continue;

                changes.Add(new BulkMaterialEditUndoChange(row.FilePath, descriptor.Label, previousValue, currentValue));
            }

            session.RecordUndoEntry(changes);
        }

        private bool UndoLastChange()
        {
            return ApplyHistoryChange(() => session?.UndoLastChange() ?? Array.Empty<BulkMaterialEditUndoChange>());
        }

        private bool RedoLastChange()
        {
            return ApplyHistoryChange(() => session?.RedoLastChange() ?? Array.Empty<BulkMaterialEditUndoChange>());
        }

        private bool ApplyHistoryChange(Func<IReadOnlyList<BulkMaterialEditUndoChange>> changeAction)
        {
            if (session == null || changeAction == null)
                return false;

            CommitPendingEdits();

            List<(string FilePath, string ColumnName)> selectedCells = grid.SelectedCells
                .Cast<DataGridViewCell>()
                .Select(cell => BuildCellReference(cell.RowIndex, cell.ColumnIndex))
                .Where(reference => reference.HasValue)
                .Select(reference => reference.Value)
                .Distinct()
                .ToList();
            string[] selectedRows = SelectedRows
                .Select(row => row.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            (string FilePath, string ColumnName)? currentCellReference = grid.CurrentCell == null
                ? null
                : BuildCellReference(grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex);

            IReadOnlyList<BulkMaterialEditUndoChange> appliedChanges = changeAction();
            if (appliedChanges.Count == 0)
            {
                RefreshSummary();
                return false;
            }

            bool visibilityChanged = false;
            var affectedRows = new HashSet<BulkMaterialEditRow>();
            foreach (BulkMaterialEditUndoChange change in appliedChanges)
            {
                invalidCellTexts.Remove((change.FilePath, change.DescriptorLabel));
                pendingBooleanOverrides.Remove((change.FilePath, change.DescriptorLabel));
                pendingDirtyRowPaths.Remove(change.FilePath);

                BulkMaterialEditRow row = session.Rows.FirstOrDefault(candidate => string.Equals(candidate.FilePath, change.FilePath, StringComparison.OrdinalIgnoreCase));
                MaterialFieldDescriptor descriptor = session.FindDescriptor(change.DescriptorLabel);
                if (row == null || descriptor == null)
                    continue;

                affectedRows.Add(row);
                visibilityChanged |= VisibilityDrivenFieldMap.ContainsKey(descriptor.Label);
            }

            foreach (BulkMaterialEditRow row in affectedRows)
                row.RefreshDynamicState();

            ClearStaleValidationErrors(affectedRows);

            bool rebuilt = visibilityChanged && TryRefreshVisibilityDrivenColumns(
                selectedRows,
                currentCellReference?.FilePath ?? string.Empty,
                currentCellReference?.ColumnName ?? string.Empty);

            if (!rebuilt)
                RefreshAllGridRows();

            if (selectedCells.Count > 0)
                ReselectCells(selectedCells);
            else if (selectedRows.Length > 0)
                ReselectRows(selectedRows);

            if (currentCellReference.HasValue)
                RestoreCurrentCell(currentCellReference.Value.FilePath, currentCellReference.Value.ColumnName);

            RefreshSummary();
            return true;
        }

        private bool HandlePostEditRefresh(
            int rowIndex,
            BulkMaterialEditRow row,
            MaterialFieldDescriptor descriptor,
            IReadOnlyCollection<string> selectedPaths,
            string focusFilePath,
            string focusColumnName)
        {
            ClearStaleValidationErrors(new[] { row });
            RefreshGridRowState(grid.Rows[rowIndex], row);
            RefreshSummary();

            if (!VisibilityDrivenFieldMap.ContainsKey(descriptor.Label))
                return false;

            row.RefreshDynamicState();
            if (TryRefreshVisibilityDrivenColumns(selectedPaths, focusFilePath, focusColumnName))
                return true;

            RefreshGridRow(rowIndex, row);
            return false;
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

        private void RefreshAllGridRows()
        {
            for (int rowIndex = 0; rowIndex < grid.Rows.Count; rowIndex++)
            {
                if (grid.Rows[rowIndex].Tag is BulkMaterialEditRow row)
                    RefreshGridRow(rowIndex, row);
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
                if (!TryApplyCellValue(row, descriptor, gridCell, nextValue, Convert.ToString(nextValue) ?? string.Empty))
                    return false;
            }
            finally
            {
                suppressGridEvents = false;
            }

            if (HandlePostEditRefresh(rowIndex, row, descriptor, selectedPaths, focusFilePath, focusColumnName))
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
    }
}
