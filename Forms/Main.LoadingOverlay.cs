using System;
using System.Drawing;
using System.Windows.Forms;
using Material_Editor.Theming;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private Panel singleEditorLoadingOverlay;
        private TableLayoutPanel singleEditorLoadingLayout;
        private Label singleEditorLoadingLabel;
        private ProgressBar singleEditorLoadingProgress;
        private int singleEditorLoadingDepth;

        private void InitializeSingleEditorLoadingOverlay()
        {
            singleEditorLoadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(24)
            };

            singleEditorLoadingLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0),
                Padding = new Padding(20)
            };
            singleEditorLoadingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            singleEditorLoadingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            singleEditorLoadingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            singleEditorLoadingLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Text = "Building editor..."
            };
            AppearanceApplicator.SetFontRole(singleEditorLoadingLabel, AppearanceFontRole.Bold);

            singleEditorLoadingProgress = new ProgressBar
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 12, 0, 0),
                MarqueeAnimationSpeed = 20,
                Style = ProgressBarStyle.Marquee,
                Width = 260
            };

            singleEditorLoadingLayout.Controls.Add(singleEditorLoadingLabel, 0, 0);
            singleEditorLoadingLayout.Controls.Add(singleEditorLoadingProgress, 0, 1);
            singleEditorLoadingOverlay.Controls.Add(singleEditorLoadingLayout);
            contentScrollPanel.Controls.Add(singleEditorLoadingOverlay);
            ApplyAppearanceToSingleEditorLoadingOverlay();
            CenterSingleEditorLoadingOverlay();
            singleEditorLoadingOverlay.Resize += (_, _) => CenterSingleEditorLoadingOverlay();
        }

        private void RunWithSingleEditorLoadingOverlay(string message, Action action)
        {
            if (action == null)
                return;

            ShowSingleEditorLoadingOverlay(message);
            try
            {
                action();
            }
            finally
            {
                HideSingleEditorLoadingOverlay();
            }
        }

        private void ShowSingleEditorLoadingOverlay(string message)
        {
            if (singleEditorLoadingOverlay == null || IsDisposed)
                return;

            singleEditorLoadingDepth++;
            if (!string.IsNullOrWhiteSpace(message))
                singleEditorLoadingLabel.Text = message;

            ApplyAppearanceToSingleEditorLoadingOverlay();
            CenterSingleEditorLoadingOverlay();

            if (singleEditorLoadingDepth > 1)
                return;

            UseWaitCursor = true;
            singleEditorLoadingOverlay.Visible = true;
            singleEditorLoadingOverlay.BringToFront();
            singleEditorLoadingOverlay.Update();
            contentScrollPanel.Update();
            Update();
        }

        private void HideSingleEditorLoadingOverlay()
        {
            if (singleEditorLoadingOverlay == null || singleEditorLoadingDepth == 0)
                return;

            singleEditorLoadingDepth--;
            if (singleEditorLoadingDepth > 0)
                return;

            singleEditorLoadingOverlay.Visible = false;
            UseWaitCursor = false;
        }

        private void ApplyAppearanceToSingleEditorLoadingOverlay()
        {
            if (singleEditorLoadingOverlay == null || !AppearanceService.IsInitialized)
                return;

            AppearanceDefinition appearance = AppearanceService.CurrentAppearance;
            AppearanceApplicator.ApplyToContainer(
                singleEditorLoadingOverlay,
                appearance,
                appearance.Theme.Palette.PanelBackground);
            singleEditorLoadingLayout.BackColor = appearance.Theme.Palette.ControlBackground;
            singleEditorLoadingLayout.ForeColor = appearance.Theme.Palette.Foreground;
            singleEditorLoadingLabel.ForeColor = appearance.Theme.Palette.Foreground;
        }

        private void CenterSingleEditorLoadingOverlay()
        {
            if (singleEditorLoadingOverlay == null || singleEditorLoadingLayout == null)
                return;

            Size preferredSize = singleEditorLoadingLayout.GetPreferredSize(singleEditorLoadingOverlay.ClientSize);
            int x = Math.Max(0, (singleEditorLoadingOverlay.ClientSize.Width - preferredSize.Width) / 2);
            int y = Math.Max(0, (singleEditorLoadingOverlay.ClientSize.Height - preferredSize.Height) / 2);
            singleEditorLoadingLayout.Location = new Point(x, y);
        }
    }
}
