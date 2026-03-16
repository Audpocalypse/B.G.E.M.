using Material_Editor.Theming;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class StartupSplashDialog : ThemeAwareForm
    {
        private const int OuterPadding = 8;
        private const int ScreenMargin = 48;

        private readonly SplashAnimationResource animationResource;
        private readonly PictureBox animationPictureBox;
        private readonly Timer closeTimer;
        private bool closeRequested;

        private StartupSplashDialog(SplashAnimationResource animationResource)
        {
            this.animationResource = animationResource ?? throw new ArgumentNullException(nameof(animationResource));

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            KeyPreview = true;
            Padding = new Padding(OuterPadding);

            animationPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = animationResource.Image,
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false
            };
            animationPictureBox.Click += DismissSplash;
            Controls.Add(animationPictureBox);

            ClientSize = CalculateClientSize(animationResource.Image.Size);

            closeTimer = new Timer
            {
                Interval = animationResource.DurationMilliseconds
            };
            closeTimer.Tick += CloseTimer_Tick;

            Click += DismissSplash;
            Shown += StartupSplashDialog_Shown;
        }

        public static bool TryCreate(out StartupSplashDialog dialog)
        {
            dialog = null;

            SplashAnimationResource animationResource = SplashAnimationProvider.Load();
            if (animationResource == null)
                return false;

            try
            {
                dialog = new StartupSplashDialog(animationResource);
                return true;
            }
            catch
            {
                animationResource.Dispose();
                return false;
            }
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            BackColor = appearance.Theme.Palette.FormBackground;
            animationPictureBox.BackColor = appearance.Theme.Palette.FormBackground;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                CloseSplash();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                closeTimer.Dispose();
                animationPictureBox.Image = null;
                animationResource.Dispose();
            }

            base.Dispose(disposing);
        }

        private static Size CalculateClientSize(Size imageSize)
        {
            Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, imageSize.Width, imageSize.Height);
            int maxWidth = Math.Max(1, workingArea.Width - ScreenMargin);
            int maxHeight = Math.Max(1, workingArea.Height - ScreenMargin);

            float scale = Math.Min(1f, Math.Min((float)maxWidth / imageSize.Width, (float)maxHeight / imageSize.Height));
            int width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
            int height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));

            return new Size(width + (OuterPadding * 2), height + (OuterPadding * 2));
        }

        private void StartupSplashDialog_Shown(object sender, EventArgs e)
        {
            Activate();
            closeTimer.Start();
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            CloseSplash();
        }

        private void DismissSplash(object sender, EventArgs e)
        {
            CloseSplash();
        }

        private void CloseSplash()
        {
            if (closeRequested || IsDisposed)
                return;

            closeRequested = true;
            closeTimer.Stop();
            Close();
        }
    }
}
