using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal partial class AboutDialog : ThemeAwareForm
    {
        public AboutDialog()
        {
            InitializeComponent();
            aboutText.Text = BuildAboutText();
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
        }

        private void AboutText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string BuildAboutText()
        {
            Assembly assembly = typeof(AboutDialog).Assembly;
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? Application.ProductVersion;
            return $"B.G.E.M.{Environment.NewLine}Version {version}{Environment.NewLine}by Audpocalypse{Environment.NewLine}https://github.com/Audpocalypse/B.G.E.M./";
        }
    }
}
