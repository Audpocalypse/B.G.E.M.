using System;
using System.Drawing;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    public partial class CheckControl : CustomControl
    {
        private Label lbLabel;
        private ColorToggleCheckBox check;
        private bool suppressChanged;

        public override Label LabelControl
        {
            get { return lbLabel; }
        }

        public override Control Control
        {
            get { return check; }
        }

        public CheckControl(string label, Func<CustomControl, bool> visibilityCallback, Action<CustomControl> changedCallback, bool initialChecked = false) : base(label)
        {
            lbLabel.Text = label;
            check.Checked = initialChecked;

            VisibilityCallback = visibilityCallback;
            ChangedCallback = changedCallback;
        }

        public override void CreateControls()
        {
            lbLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                AutoSize = true,
                Name = "lbLabel",
                Text = "Label",
                Tag = this
            };

            check = new ColorToggleCheckBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Name = "check",
                TabIndex = 0,
                Text = string.Empty,
                Tag = this
            };
            check.CheckedChanged += new EventHandler(Check_CheckedChanged);
            UpdateCheckVisual(check);
        }

        private void Check_CheckedChanged(object sender, EventArgs e)
        {
            var check = sender as ColorToggleCheckBox;
            UpdateCheckVisual(check);

            if (suppressChanged)
                return;

            InvokeChangedCallback();
        }

        internal static void UpdateCheckVisual(ColorToggleCheckBox check)
        {
            if (check == null)
                return;

            ThemeDefinition theme = ThemeService.IsInitialized ? ThemeService.CurrentTheme : null;
            check.ApplyTheme(theme, check.Parent?.BackColor ?? SystemColors.Control);
        }

        public override object GetProperty()
        {
            return check.Checked;
        }

        public override void SetProperty(object value)
        {
            suppressChanged = true;
            try
            {
                check.Checked = value != null && Convert.ToBoolean(value);
                UpdateCheckVisual(check);
            }
            finally
            {
                suppressChanged = false;
            }
        }
    }
}
