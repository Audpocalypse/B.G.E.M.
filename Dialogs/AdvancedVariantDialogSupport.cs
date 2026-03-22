using Material_Editor.AdvancedVariant;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal static class AdvancedVariantDialogSupport
    {
        public static IReadOnlyList<AdvancedVariantLayerDefinition> BuildLayers(NumericUpDown[] countControls, string layerNameFormat)
        {
            var layers = new List<AdvancedVariantLayerDefinition>(countControls.Length);
            for (int index = 0; index < countControls.Length; index++)
            {
                int count = (int)countControls[index].Value;
                if (count <= 0)
                    continue;

                layers.Add(new AdvancedVariantLayerDefinition(
                    string.Format(CultureInfo.InvariantCulture, layerNameFormat, index + 1),
                    Enumerable.Range(1, count).ToArray()));
            }

            return layers;
        }

        public static (FlowLayoutPanel Layout, NumericUpDown[] Controls) CreateLayerCountLayout(
            int layerCount,
            Func<int, decimal> defaultValueFactory,
            EventHandler valueChangedHandler,
            Padding layoutMargin,
            Padding itemMargin,
            Padding labelMargin,
            int numericWidth,
            Padding numericMargin,
            HorizontalAlignment textAlign = HorizontalAlignment.Left)
        {
            var countsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = layoutMargin
            };

            var controls = new NumericUpDown[layerCount];
            for (int index = 0; index < controls.Length; index++)
            {
                var countItemPanel = new FlowLayoutPanel
                {
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.LeftToRight,
                    Margin = itemMargin
                };

                countItemPanel.Controls.Add(DialogLayoutSupport.CreateInlineLabel($"Layer {index + 1} count:", labelMargin));

                decimal defaultValue = defaultValueFactory?.Invoke(index) ?? 0m;
                var layerCountControl = DialogLayoutSupport.CreateNumericInput(
                    0,
                    AdvancedVariantEngine.MaxLayerCountValue,
                    defaultValue,
                    numericWidth,
                    numericMargin,
                    textAlign: textAlign);
                if (valueChangedHandler != null)
                    layerCountControl.ValueChanged += valueChangedHandler;

                controls[index] = layerCountControl;
                countItemPanel.Controls.Add(layerCountControl);
                countsLayout.Controls.Add(countItemPanel);
            }

            return (countsLayout, controls);
        }

        public static void UpdateLayerColumnChoices(DataGridViewComboBoxColumn[] columns, NumericUpDown[] countControls)
        {
            for (int index = 0; index < columns.Length; index++)
            {
                int count = index < countControls.Length
                    ? (int)countControls[index].Value
                    : 0;

                string[] values = new[] { string.Empty }
                    .Concat(Enumerable.Range(1, count).Select(value => value.ToString(CultureInfo.InvariantCulture)))
                    .ToArray();

                SetComboColumnChoices(columns[index], values);
            }
        }

        private static void SetComboColumnChoices(DataGridViewComboBoxColumn column, IEnumerable<string> values)
        {
            column.Items.Clear();
            foreach (string value in values)
                column.Items.Add(value);
        }

        public static DataGridViewComboBoxColumn[] AddRuleColumns(
            DataGridView grid,
            int layerCount,
            float layerFillWeight,
            float indexTokenFillWeight)
        {
            var columns = new DataGridViewComboBoxColumn[layerCount];
            for (int index = 0; index < columns.Length; index++)
            {
                var column = new DataGridViewComboBoxColumn
                {
                    DataPropertyName = BuildLayerMatchPropertyName(index),
                    HeaderText = $"Layer {index + 1}",
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                    FlatStyle = FlatStyle.Flat,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = layerFillWeight
                };
                columns[index] = column;
                grid.Columns.Add(column);
            }

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AdvancedRuleRow.IndexTok),
                HeaderText = "indexTok",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = indexTokenFillWeight
            });

            return columns;
        }

        private static string NormalizeLayerMatchText(NumericUpDown[] countControls, string value, int layerIndex)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            int count = layerIndex < countControls.Length
                ? (int)countControls[layerIndex].Value
                : 0;

            if (count <= 0)
                return string.Empty;

            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
                && parsed >= 1
                && parsed <= count
                ? parsed.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string GetLayerColumnTooltipText(NumericUpDown[] countControls, int zeroBasedLayerIndex)
        {
            int count = zeroBasedLayerIndex < countControls.Length
                ? (int)countControls[zeroBasedLayerIndex].Value
                : 0;

            return count > 0
                ? $"Choose 1-{count}, or leave blank to match any Layer {zeroBasedLayerIndex + 1} value."
                : $"Leave blank while Layer {zeroBasedLayerIndex + 1} count is 0.";
        }

        private static string BuildLayerDescription(AdvancedVariantResolvedContext context)
        {
            return string.Join(", ", context.LayerIndices.Select((value, index) => $"Layer{index + 1}={value}"));
        }

        public static IReadOnlyList<string> BuildFallbackTooltipLines(
            IReadOnlyList<AdvancedVariantResolvedContext> contexts,
            bool includeLayerTokenFallbacks,
            out int fallbackContextCount)
        {
            var tooltipLines = new List<string>();
            fallbackContextCount = 0;

            foreach (var context in contexts)
            {
                bool anyFallback = false;
                if (context.IndexTokFallback)
                {
                    anyFallback = true;
                    tooltipLines.Add($"{BuildLayerDescription(context)} -> {{indexTok}}={context.IndexTok}");
                }

                if (includeLayerTokenFallbacks)
                {
                    for (int layerIndex = 0; layerIndex < context.LayerIndices.Count && layerIndex < 4; layerIndex++)
                    {
                        if (!context.IsLayerIndexTokenFallback(layerIndex))
                            continue;

                        anyFallback = true;
                        tooltipLines.Add($"{BuildLayerDescription(context)} -> {{indexTokLayer{layerIndex + 1}}}={context.GetLayerIndexToken(layerIndex)}");
                    }
                }

                if (anyFallback)
                    fallbackContextCount++;
            }

            return tooltipLines;
        }

        public static void NormalizeLayerMatchTexts(BindingList<AdvancedRuleRow> rows, NumericUpDown[] countControls)
        {
            foreach (var row in rows)
            {
                row.Layer1MatchText = NormalizeLayerMatchText(countControls, row.Layer1MatchText, 0);
                row.Layer2MatchText = NormalizeLayerMatchText(countControls, row.Layer2MatchText, 1);
                row.Layer3MatchText = NormalizeLayerMatchText(countControls, row.Layer3MatchText, 2);
                row.Layer4MatchText = NormalizeLayerMatchText(countControls, row.Layer4MatchText, 3);
            }
        }

        public static AdvancedRuleRow AddRule(BindingList<AdvancedRuleRow> rows, PropertyChangedEventHandler propertyChangedHandler)
        {
            var row = new AdvancedRuleRow();
            if (propertyChangedHandler != null)
                row.PropertyChanged += propertyChangedHandler;

            rows.Add(row);
            return row;
        }

        public static bool EnsureAtLeastOneRule(
            BindingList<AdvancedRuleRow> rows,
            int layerCount,
            PropertyChangedEventHandler propertyChangedHandler)
        {
            if (rows.Count > 0 || layerCount == 0)
                return false;

            AddRule(rows, propertyChangedHandler);
            return true;
        }

        public static int DeleteSelectedRule(
            BindingList<AdvancedRuleRow> rows,
            int selectedIndex,
            PropertyChangedEventHandler propertyChangedHandler,
            bool ensureAtLeastOneRule)
        {
            if (selectedIndex < 0 || selectedIndex >= rows.Count)
                return -1;

            if (propertyChangedHandler != null)
                rows[selectedIndex].PropertyChanged -= propertyChangedHandler;

            rows.RemoveAt(selectedIndex);

            if (ensureAtLeastOneRule && rows.Count == 0)
                AddRule(rows, propertyChangedHandler);

            return Math.Min(selectedIndex, rows.Count - 1);
        }

        public static AdvancedVariantRule[] BuildRules(BindingList<AdvancedRuleRow> rows, bool includeDefaultRuleWhenEmpty)
        {
            AdvancedVariantRule[] rules = rows
                .Where(row => row.HasMeaningfulContent)
                .Select((row, index) => row.ToRule(index))
                .ToArray();

            if (rules.Length == 0 && includeDefaultRuleWhenEmpty)
                rules = new[] { new AdvancedVariantRule { SourceOrder = 0 } };

            return rules;
        }

        public static void CommitComboBoxEditIfDirty(DataGridView grid)
        {
            if (grid?.IsCurrentCellDirty == true && grid.CurrentCell is DataGridViewComboBoxCell)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        public static void SuppressGridDataError(DataGridViewDataErrorEventArgs e)
        {
            if (e != null)
                e.ThrowException = false;
        }

        public static string GetRuleCellTooltipText(
            AdvancedRuleRow row,
            int columnIndex,
            NumericUpDown[] countControls,
            string indexTokenTooltip = "")
        {
            if (columnIndex < 0)
                return string.Empty;

            return columnIndex switch
            {
                0 => GetLayerColumnTooltipText(countControls, 0),
                1 => GetLayerColumnTooltipText(countControls, 1),
                2 => GetLayerColumnTooltipText(countControls, 2),
                3 => GetLayerColumnTooltipText(countControls, 3),
                4 => indexTokenTooltip ?? string.Empty,
                _ => string.Empty
            };
        }

        public static void SelectGridRow(DataGridView grid, int rowIndex)
        {
            if (grid == null || rowIndex < 0 || rowIndex >= grid.Rows.Count)
                return;

            grid.ClearSelection();
            grid.Rows[rowIndex].Selected = true;
            grid.CurrentCell = grid.Rows[rowIndex].Cells[0];
        }

        public static int MoveSelectedRule(BindingList<AdvancedRuleRow> rows, int selectedIndex, int direction)
        {
            int targetIndex = selectedIndex + direction;
            if (selectedIndex < 0 || targetIndex < 0 || targetIndex >= rows.Count)
                return -1;

            var row = rows[selectedIndex];
            rows.RemoveAt(selectedIndex);
            rows.Insert(targetIndex, row);
            return targetIndex;
        }

        public static void UpdateRuleButtonState(
            Button addButton,
            Button deleteButton,
            Button moveUpButton,
            Button moveDownButton,
            int selectedIndex,
            int rowCount,
            bool canModifyRules)
        {
            bool hasSelection = selectedIndex >= 0 && selectedIndex < rowCount;

            addButton.Enabled = canModifyRules;
            deleteButton.Enabled = canModifyRules && hasSelection && rowCount > 1;
            moveUpButton.Enabled = canModifyRules && hasSelection && selectedIndex > 0;
            moveDownButton.Enabled = canModifyRules && hasSelection && selectedIndex >= 0 && selectedIndex < rowCount - 1;
        }

        private static string BuildLayerMatchPropertyName(int zeroBasedLayerIndex)
        {
            return nameof(AdvancedRuleRow.Layer1MatchText)
                .Replace("1", (zeroBasedLayerIndex + 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }
    }
}
