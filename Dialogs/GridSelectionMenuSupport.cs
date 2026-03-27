using System;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal static class GridSelectionMenuSupport
    {
        public static ContextMenuStrip Attach(
            DataGridView grid,
            Action selectAll,
            Action selectNone,
            Func<bool> canSelectAll = null,
            Func<bool> canSelectNone = null)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));
            if (selectAll == null)
                throw new ArgumentNullException(nameof(selectAll));
            if (selectNone == null)
                throw new ArgumentNullException(nameof(selectNone));

            var menu = new ContextMenuStrip();
            var selectAllItem = new ToolStripMenuItem("Select All");
            var selectNoneItem = new ToolStripMenuItem("Select None");

            selectAllItem.Click += (_, _) => selectAll();
            selectNoneItem.Click += (_, _) => selectNone();
            menu.Items.Add(selectAllItem);
            menu.Items.Add(selectNoneItem);
            menu.Opening += (_, _) =>
            {
                selectAllItem.Enabled = canSelectAll?.Invoke() ?? true;
                selectNoneItem.Enabled = canSelectNone?.Invoke() ?? true;
            };

            grid.ContextMenuStrip = menu;
            grid.KeyDown += (_, e) =>
            {
                if (!e.Control || e.KeyCode != Keys.A)
                    return;

                selectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
            grid.CellMouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                DataGridViewCell targetCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (grid.SelectionMode == DataGridViewSelectionMode.FullRowSelect)
                {
                    if (!grid.Rows[e.RowIndex].Selected)
                    {
                        grid.ClearSelection();
                        grid.Rows[e.RowIndex].Selected = true;
                    }
                }
                else if (!targetCell.Selected)
                {
                    grid.ClearSelection();
                    targetCell.Selected = true;
                }

                grid.CurrentCell = targetCell;
            };

            return menu;
        }
    }
}
