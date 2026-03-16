using System;
using System.Windows.Forms;

namespace Material_Editor.Theming
{
    internal abstract class ThemeAwareForm : Form
    {
        private bool isSubscribed;

        protected AppearanceDefinition ActiveAppearance => AppearanceService.CurrentAppearance;
        protected ThemeDefinition ActiveTheme => ActiveAppearance.Theme;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SubscribeToAppearanceChanges();
            ApplyCurrentAppearance();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnsubscribeFromAppearanceChanges();
            base.OnHandleDestroyed(e);
        }

        protected void ApplyCurrentAppearance()
        {
            if (!AppearanceService.IsInitialized)
                return;

            ApplyAppearance(AppearanceService.CurrentAppearance);
        }

        protected abstract void ApplyAppearance(AppearanceDefinition appearance);

        private void SubscribeToAppearanceChanges()
        {
            if (isSubscribed)
                return;

            AppearanceService.AppearanceChanged += AppearanceService_AppearanceChanged;
            isSubscribed = true;
        }

        private void UnsubscribeFromAppearanceChanges()
        {
            if (!isSubscribed)
                return;

            AppearanceService.AppearanceChanged -= AppearanceService_AppearanceChanged;
            isSubscribed = false;
        }

        private void AppearanceService_AppearanceChanged(object sender, AppearanceChangedEventArgs e)
        {
            SuspendLayout();
            try
            {
                ApplyAppearance(e.Appearance);
            }
            finally
            {
                ResumeLayout(true);
            }
        }
    }
}
