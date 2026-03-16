using Material_Editor.Theming;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal static class ColorToggleDataGridView
    {
        internal const int GridIndicatorSize = 14;

        public static DataGridViewCheckBoxColumn CreateColumn(string dataPropertyName, string headerText, string name = null, float fillWeight = 0f, int width = 60, bool autoSizeNone = false)
        {
            var column = new DataGridViewCheckBoxColumn
            {
                Name = name ?? dataPropertyName ?? headerText,
                DataPropertyName = dataPropertyName,
                HeaderText = headerText,
                Width = width,
                FlatStyle = FlatStyle.Flat,
                CellTemplate = new ColorToggleCheckBoxCell()
            };

            if (fillWeight > 0f)
                column.FillWeight = fillWeight;

            if (autoSizeNone)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.Resizable = DataGridViewTriState.False;
            }

            return column;
        }

        public static bool TryHandleCellMouseUp(DataGridView grid, DataGridViewCellMouseEventArgs e)
        {
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0)
                return false;

            return TryToggleCellValue(grid, e.ColumnIndex, e.RowIndex);
        }

        public static bool TryHandleSpaceKey(DataGridView grid, KeyEventArgs e)
        {
            if (grid?.CurrentCell == null || e == null || e.KeyCode != Keys.Space || e.Modifiers != Keys.None)
                return false;

            if (!TryToggleCellValue(grid, grid.CurrentCell.ColumnIndex, grid.CurrentCell.RowIndex))
                return false;

            e.Handled = true;
            e.SuppressKeyPress = true;
            return true;
        }

        private static bool TryToggleCellValue(DataGridView grid, int columnIndex, int rowIndex)
        {
            if (grid.Columns[columnIndex] is not DataGridViewCheckBoxColumn
                || grid.Rows[rowIndex].Cells[columnIndex] is not ColorToggleCheckBoxCell cell
                || cell.ReadOnly)
            {
                return false;
            }

            bool currentValue = cell.Value is bool boolValue && boolValue;
            grid.CurrentCell = cell;
            cell.Value = !currentValue;
            grid.NotifyCurrentCellDirty(true);
            grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            return true;
        }
    }

    internal sealed class ColorToggleCheckBoxCell : DataGridViewCheckBoxCell
    {
        protected override void Paint(
            System.Drawing.Graphics graphics,
            System.Drawing.Rectangle clipBounds,
            System.Drawing.Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates cellState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            if (DataGridView == null || graphics == null)
                return;

            bool isSelected = (cellState & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
            bool isChecked = false;
            if (formattedValue is bool boolValue)
                isChecked = boolValue;
            else if (value is bool checkedValue)
                isChecked = checkedValue;

            System.Drawing.Color background = isSelected
                ? cellStyle.SelectionBackColor
                : cellStyle.BackColor;

            using var backgroundBrush = new System.Drawing.SolidBrush(background);
            graphics.FillRectangle(backgroundBrush, cellBounds);

            if ((paintParts & DataGridViewPaintParts.Border) == DataGridViewPaintParts.Border)
                PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);

            ThemeDefinition theme = ThemeService.IsInitialized ? ThemeService.CurrentTheme : null;
            var contentBounds = cellBounds;
            contentBounds.Inflate(-4, -2);
            ColorToggleRenderer.DrawCheckbox(graphics, contentBounds, isChecked, theme, background, clearBackground: false, indicatorSize: ColorToggleDataGridView.GridIndicatorSize);

            if ((paintParts & DataGridViewPaintParts.Focus) == DataGridViewPaintParts.Focus
                && DataGridView.CurrentCellAddress.X == ColumnIndex
                && DataGridView.CurrentCellAddress.Y == rowIndex
                && DataGridView.Focused)
            {
                var focusBounds = cellBounds;
                focusBounds.Inflate(-2, -2);
                ControlPaint.DrawFocusRectangle(graphics, focusBounds, cellStyle.ForeColor, background);
            }
        }
    }
}
