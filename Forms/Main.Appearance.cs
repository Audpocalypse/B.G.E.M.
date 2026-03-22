using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MaterialLib;
using Material_Editor.Controls;
using Material_Editor.Dialogs;
using Material_Editor.Models;
using Material_Editor.Services;
using Material_Editor.Theming;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new SettingsDialog(config);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplySettingsSelections(
                dialog.SelectedThemeId,
                dialog.SelectedFont,
                dialog.SelectedShowSplashAnimation,
                dialog.SelectedBulkDirtyRemoveBehavior);
        }

        private void ApplySettingsSelections(string selectedThemeId, Font selectedFont, bool selectedShowSplashAnimation, BulkDirtyRemoveBehavior selectedBulkDirtyRemoveBehavior)
        {
            string nextThemeId = string.IsNullOrWhiteSpace(selectedThemeId)
                ? ThemeIds.Default
                : ThemeService.NormalizeThemeId(selectedThemeId);
            Font nextFont = selectedFont ?? new Font(DefaultAppFont, FontStyle.Regular);

            bool themeChanged = !string.Equals(config.ThemeId, nextThemeId, StringComparison.OrdinalIgnoreCase);
            bool fontChanged = config.Font == null
                || !string.Equals(config.Font.Name, nextFont.Name, StringComparison.OrdinalIgnoreCase)
                || Math.Abs(config.Font.SizeInPoints - nextFont.SizeInPoints) > 0.01f
                || config.Font.Style != nextFont.Style;

            config.ThemeId = nextThemeId;
            config.Font = nextFont;
            config.ShowSplashAnimation = selectedShowSplashAnimation;
            config.BulkDirtyRemoveBehavior = selectedBulkDirtyRemoveBehavior;

            if (fontChanged && IsSingleMode && currentMaterial != null)
                pendingSingleEditorAppearanceRebuildMaterial = CaptureCurrentSingleEditorState();

            if (themeChanged)
                AppearanceService.SetCurrentTheme(nextThemeId);

            if (fontChanged)
                AppearanceService.SetFont(nextFont);
        }

        internal static Config LoadConfig()
        {
            return LoadConfig(ConfigurationManager.AppSettings);
        }

        internal static Config LoadConfig(NameValueCollection appSettings)
        {
            var loadedConfig = new Config
            {
                GameVersion = Game.FO4,
                Font = new Font(DefaultAppFont, FontStyle.Regular),
                ThemeId = ThemeIds.Default,
                ShowSplashAnimation = true,
                BulkDirtyRemoveBehavior = BulkDirtyRemoveBehavior.Ask,
                BulkFieldPresets = new List<BulkFieldPreset>()
            };

            try
            {
                var gameVersion = appSettings["GameVersion"];
                if (gameVersion != null && Enum.TryParse(gameVersion, out Game parsedGame))
                {
                    loadedConfig.GameVersion = parsedGame;
                }

                var fontName = appSettings["FontName"];
                var fontSizeStr = appSettings["FontSize"];
                var themeIdValue = appSettings["ThemeId"];
                var themeValue = appSettings["Theme"];
                var showSplashAnimation = appSettings["ShowSplashAnimation"];
                var bulkRemoveBehavior = appSettings["BulkDirtyRemoveBehavior"];
                var bulkFieldPresets = appSettings["BulkFieldPresets"];
                if (!string.IsNullOrEmpty(fontName) && !string.IsNullOrEmpty(fontSizeStr))
                {
                    if (!float.TryParse(fontSizeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float fontSize))
                        fontSize = 10.0f;

                    try
                    {
                        loadedConfig.Font = new Font(fontName, fontSize);
                    }
                    catch
                    {
                        loadedConfig.Font = new Font(DefaultAppFont, FontStyle.Regular);
                    }
                }

                string persistedThemeId = !string.IsNullOrWhiteSpace(themeIdValue) ? themeIdValue : themeValue;
                loadedConfig.ThemeId = ThemeService.NormalizeThemeId(persistedThemeId);

                if (!string.IsNullOrEmpty(showSplashAnimation) && bool.TryParse(showSplashAnimation, out bool parsedShowSplashAnimation))
                {
                    loadedConfig.ShowSplashAnimation = parsedShowSplashAnimation;
                }

                if (!string.IsNullOrEmpty(bulkRemoveBehavior) && Enum.TryParse(bulkRemoveBehavior, true, out BulkDirtyRemoveBehavior parsedBulkRemoveBehavior))
                {
                    loadedConfig.BulkDirtyRemoveBehavior = parsedBulkRemoveBehavior;
                }

                loadedConfig.BulkFieldPresets = BulkEditorPreferencesService.DeserializePresets(bulkFieldPresets);
            }
            catch
            {
            }

            if (loadedConfig.Font == null)
                loadedConfig.Font = new Font(DefaultAppFont, FontStyle.Regular);

            return loadedConfig;
        }

        private void WriteSettings()
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                var gameVersion = configFile.AppSettings.Settings["GameVersion"];
                if (gameVersion != null)
                    gameVersion.Value = Convert.ToString(config.GameVersion);
                else
                    configFile.AppSettings.Settings.Add("GameVersion", Convert.ToString(config.GameVersion));

                var fontName = configFile.AppSettings.Settings["FontName"];
                if (fontName != null)
                    fontName.Value = config.Font.Name;
                else
                    configFile.AppSettings.Settings.Add("FontName", config.Font.Name);

                var fontSize = configFile.AppSettings.Settings["FontSize"];
                if (fontSize != null)
                    fontSize.Value = config.Font.Size.ToString(CultureInfo.InvariantCulture);
                else
                    configFile.AppSettings.Settings.Add("FontSize", config.Font.Size.ToString(CultureInfo.InvariantCulture));

                var themeSetting = configFile.AppSettings.Settings["ThemeId"];
                if (themeSetting != null)
                    themeSetting.Value = config.ThemeId;
                else
                    configFile.AppSettings.Settings.Add("ThemeId", config.ThemeId);

                if (configFile.AppSettings.Settings["Theme"] != null)
                    configFile.AppSettings.Settings.Remove("Theme");

                var showSplashAnimation = configFile.AppSettings.Settings["ShowSplashAnimation"];
                if (showSplashAnimation != null)
                    showSplashAnimation.Value = config.ShowSplashAnimation.ToString();
                else
                    configFile.AppSettings.Settings.Add("ShowSplashAnimation", config.ShowSplashAnimation.ToString());

                var bulkRemoveBehavior = configFile.AppSettings.Settings["BulkDirtyRemoveBehavior"];
                if (bulkRemoveBehavior != null)
                    bulkRemoveBehavior.Value = config.BulkDirtyRemoveBehavior.ToString();
                else
                    configFile.AppSettings.Settings.Add("BulkDirtyRemoveBehavior", config.BulkDirtyRemoveBehavior.ToString());

                string serializedPresets = BulkEditorPreferencesService.SerializePresets(config.BulkFieldPresets);
                var bulkFieldPresets = configFile.AppSettings.Settings["BulkFieldPresets"];
                if (bulkFieldPresets != null)
                    bulkFieldPresets.Value = serializedPresets;
                else
                    configFile.AppSettings.Settings.Add("BulkFieldPresets", serializedPresets);

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch
            {
            }
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            if (TryHandleSingleEditorAppearanceRebuild())
                return;

            config.Font = appearance.Font;
            config.ThemeId = appearance.Theme.Id;

            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(topControlsLayout, appearance, appearance.Theme.Palette.FormBackground);
            AppearanceApplicator.ApplyToContainer(contentScrollPanel, appearance, appearance.Theme.Palette.FormBackground);
            AppearanceApplicator.ApplyToContainer(contentHostLayout, appearance, appearance.Theme.Palette.FormBackground);
            AppearanceApplicator.ApplyToContainer(layoutGeneral, appearance, appearance.Theme.Palette.PanelBackground);
            AppearanceApplicator.ApplyToContainer(layoutMaterial, appearance, appearance.Theme.Palette.PanelBackground);
            AppearanceApplicator.ApplyToContainer(layoutEffect, appearance, appearance.Theme.Palette.PanelBackground);
            AppearanceApplicator.ApplyToToolStrip(menuStrip, appearance);
            AppearanceApplicator.ApplyToComboBox(listVersion, appearance, appearance.Theme.Palette.ControlBackground);
            ControlFactory.ApplyAppearance(appearance);
            ApplyAppearanceToSingleEditorLoadingOverlay();
            ApplyDropdownTheme(ControlNames.AlphaBlendMode, appearance);
            RelayoutSingleEditorForAppearance();
        }

        private void ApplyDropdownTheme(string controlName, AppearanceDefinition appearance)
        {
            var combo = ControlFactory.Find(controlName)?.Control as ComboBox;
            if (combo != null)
                AppearanceApplicator.ApplyToComboBox(combo, appearance, appearance.Theme.Palette.ControlBackground);
        }

        private bool TryHandleSingleEditorAppearanceRebuild()
        {
            if (!IsSingleMode || pendingSingleEditorAppearanceRebuildMaterial == null)
                return false;

            BaseMaterialFile snapshot = pendingSingleEditorAppearanceRebuildMaterial;
            pendingSingleEditorAppearanceRebuildMaterial = null;

            RunWithSingleEditorLoadingOverlay("Rebuilding editor...", () =>
            {
                SuspendAll();
                try
                {
                    forceSingleEditorControlRebuild = true;
                    CreateMaterialControls(snapshot);
                    generalPageSection?.SetCollapsed(true);
                    materialPageSection?.SetCollapsed(true);
                    effectPageSection?.SetCollapsed(true);
                    UpdateTopLevelSectionVisibility();
                }
                finally
                {
                    forceSingleEditorControlRebuild = false;
                    ResumeAll();
                }
            });

            return true;
        }

        private void Main_Closing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmCanReplaceWorkspace())
            {
                e.Cancel = true;
                return;
            }

            WriteSettings();
        }
    }
}
