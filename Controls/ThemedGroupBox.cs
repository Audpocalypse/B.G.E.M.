using Material_Editor.Theming;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    internal sealed class ThemedGroupBox : GroupBox
    {
        public ThemedGroupBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (ThemeApplicator.TryGetGroupBoxTheme(this, out ThemeDefinition theme))
            {
                ThemeApplicator.DrawGroupBox(e.Graphics, this, theme);
                return;
            }

            if (ThemeService.IsInitialized)
            {
                ThemeApplicator.DrawGroupBox(e.Graphics, this, ThemeService.CurrentTheme);
                return;
            }

            base.OnPaint(e);
        }
    }
}
