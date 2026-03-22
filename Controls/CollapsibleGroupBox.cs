using System;
using System.Drawing;
using System.Windows.Forms;
using Material_Editor.Theming;

namespace Material_Editor.Controls
{
    internal sealed class CollapsibleGroupBox : GroupBox
    {
        private readonly ToggleButton toggleButton;
        private bool collapsed;

        public TableLayoutPanel ContentLayout { get; }
        public bool IsCollapsed => collapsed;
        public event EventHandler CollapsedStateChanged;

        public CollapsibleGroupBox(string title, bool collapsible = false, bool collapsedByDefault = false)
        {
            Text = title;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Dock = DockStyle.Top;
            Margin = new Padding(0, 0, 0, 6);
            Padding = new Padding(6, 16, 6, 6);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();

            ContentLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            ContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Controls.Add(ContentLayout);

            if (collapsible)
            {
                toggleButton = new ToggleButton
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Margin = new Padding(0),
                    Padding = new Padding(3, 0, 3, 0),
                    CausesValidation = false
                };
                toggleButton.Click += ToggleButton_Click;
                Controls.Add(toggleButton);
                Resize += (_, _) => PositionToggleButton();
            }

            SetCollapsed(collapsedByDefault);
        }

        public void SetCollapsed(bool value)
        {
            SetCollapsedState(value);
        }

        private void ToggleButton_Click(object sender, EventArgs e)
        {
            SetCollapsedState(!collapsed);
        }

        private void SetCollapsedState(bool value)
        {
            bool stateChanged = collapsed != value;

            Control parent = null;
            if (stateChanged)
            {
                parent = Parent;
                parent?.SuspendLayout();
                SuspendLayout();
                ContentLayout.SuspendLayout();
            }

            collapsed = value;
            ContentLayout.Visible = !collapsed;

            if (toggleButton != null)
            {
                toggleButton.Text = collapsed ? "Show" : "Hide";
                PositionToggleButton();
            }

            if (!stateChanged)
                return;

            ContentLayout.ResumeLayout(true);
            ResumeLayout(true);
            parent?.ResumeLayout(true);
            CollapsedStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PositionToggleButton()
        {
            if (toggleButton == null)
                return;

            int x = Math.Max(6, ClientSize.Width - toggleButton.Width - 6);
            toggleButton.Location = new Point(x, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!ThemeService.IsInitialized)
            {
                base.OnPaint(e);
                return;
            }

            ThemeApplicator.DrawGroupBox(e.Graphics, this, ThemeService.CurrentTheme);
        }

        private sealed class ToggleButton : Button
        {
            public ToggleButton()
            {
                SetStyle(ControlStyles.Selectable, false);
                TabStop = false;
            }

            protected override bool ShowFocusCues => false;
        }
    }
}
