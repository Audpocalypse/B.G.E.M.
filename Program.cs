using System;
using System.Windows.Forms;

namespace Material_Editor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            try
            {
                ApplicationConfiguration.Initialize();
                var config = Material_Editor.Forms.Main.LoadConfig();
                AppearanceInitializationResult appearanceInitialization = AppearanceService.Initialize(config.ThemeId, config.Font);
                config.ThemeId = appearanceInitialization.ActiveAppearance.Theme.Id;
                config.Font = appearanceInitialization.ActiveAppearance.Font;
                Application.SetDefaultFont(config.Font);

                if (!string.IsNullOrWhiteSpace(appearanceInitialization.StartupWarning))
                {
                    MessageBox.Show(
                        appearanceInitialization.StartupWarning,
                        "Theme Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                Application.Run(new Material_Editor.Forms.Main(config));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unhandled startup exception:{Environment.NewLine}{ex}", "Startup Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
