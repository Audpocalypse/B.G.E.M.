using System;
using System.Windows.Forms;

namespace Material_Editor.Infrastructure
{
    internal sealed class StartupApplicationContext : ApplicationContext
    {
        private readonly Config config;
        private StartupSplashDialog splashDialog;

        public StartupApplicationContext(Config config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            ShowInitialForm();
        }

        private void ShowInitialForm()
        {
            if (config.ShowSplashAnimation && StartupSplashDialog.TryCreate(out splashDialog))
            {
                splashDialog.FormClosed += SplashDialog_FormClosed;
                splashDialog.Show();
                return;
            }

            ShowMainForm();
        }

        private void SplashDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            splashDialog.FormClosed -= SplashDialog_FormClosed;
            splashDialog = null;
            ShowMainForm();
        }

        private void ShowMainForm()
        {
            var mainForm = new Material_Editor.Forms.Main(config);
            mainForm.FormClosed += MainForm_FormClosed;
            MainForm = mainForm;
            mainForm.Show();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender is Form form)
                form.FormClosed -= MainForm_FormClosed;

            ExitThread();
        }
    }
}
