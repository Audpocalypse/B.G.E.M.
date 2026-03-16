using Material_Editor.Models;
using Material_Editor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal sealed partial class BulkMaterialEditorView
    {
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

        private void RefreshVisibleDescriptorsAndGrid(bool refreshSummary = true)
        {
            selectedDescriptors = ResolveVisibleDescriptors(session, selectedLabelPreferences);
            RebuildGrid();
            if (refreshSummary)
                RefreshSummary();
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
    }
}
