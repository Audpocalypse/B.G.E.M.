using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material_Editor.Theming;

namespace Material_Editor.Controls
{
    internal sealed class CollapsibleGroupBox : GroupBox
    {
        private const int WM_SETREDRAW = 0x000B;
        private readonly Button toggleButton;
        private bool collapsed;

        public TableLayoutPanel ContentLayout { get; }

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
                toggleButton = new Button
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Margin = new Padding(0),
                    Padding = new Padding(3, 0, 3, 0),
                    TabStop = false
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
            var redrawHost = FindRedrawHost();

            var parent = Parent;
            if (stateChanged)
            {
                SetRedraw(redrawHost, false);
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
            PerformLayout();
            parent?.PerformLayout();
            SetRedraw(redrawHost, true);
            redrawHost?.Invalidate(true);
            redrawHost?.Update();
        }

        private Control FindRedrawHost()
        {
            Control current = this;
            Control redrawHost = this;

            while (current?.Parent != null)
            {
                current = current.Parent;
                if (current is ScrollableControl)
                    redrawHost = current;
            }

            return redrawHost;
        }

        private void PositionToggleButton()
        {
            if (toggleButton == null)
                return;

            int x = Math.Max(6, ClientSize.Width - toggleButton.Width - 6);
            toggleButton.Location = new Point(x, 0);
        }

        private static void SetRedraw(Control control, bool enable)
        {
            if (control == null || !control.IsHandleCreated)
                return;

            SendMessage(control.Handle, WM_SETREDRAW, enable ? new IntPtr(1) : IntPtr.Zero, IntPtr.Zero);
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

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
