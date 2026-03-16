using Material_Editor.Controls;
using Material_Editor.Dialogs;
using Material_Editor.Forms;
using Material_Editor.Models;
using Material_Editor.Services;
using Material_Editor.Theming;
using MaterialLib;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MaterialEditor.Tests
{
    internal static class AppearanceTests
    {
        public static void RunAll()
        {
            AppearanceService_InitializesWithThemeAndFont();
            AppearanceService_SetFontRaisesOneAppearanceUpdateAndPreservesTheme();
            AppearanceService_SetThemeRaisesOneAppearanceUpdateAndPreservesFont();
            AppearanceDefinition_DerivedFontsTrackConfiguredSize();
            UiConstruction_MainUsesAppearanceFontAndFontAutoscaling();
            UiConstruction_FieldSelectionDialogUsesAppearanceFont();
            UiConstruction_FieldSelectionDialogUsesColorToggleGrid();
            UiConstruction_SettingsDialogReflectsConfigValues();
            UiConstruction_SettingsDialogCommitsSplashAnimationSelection();
            ConfigLoad_DefaultsSplashAnimationToTrueWhenUnset();
            ConfigLoad_ReadsPersistedSplashAnimationSetting();
            UiConstruction_ThemeDesignerDialogBuildsPreview();
            UiConstruction_SettingsDialogRefreshesThemeAfterDesignSave();
            UiConstruction_VariationGeneratorDialogUsesAppearanceFont();
            UiConstruction_BulkMaterialEditorViewUsesAppearanceFont();
            UiConstruction_BulkMaterialEditorViewInitializesBooleanColumnsByName();
            UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath();
            UiConstruction_BulkMaterialEditorViewRevealsDependentFieldsWhenControllerTurnsOn();
            UiConstruction_BulkMaterialEditorViewToggleMarksParentBooleanDirty();
        }

        private static void AppearanceService_InitializesWithThemeAndFont()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);

                AppearanceInitializationResult result = AppearanceService.InitializeForTesting("default", font, directory);

                AssertEqual("default", result.ActiveAppearance.Theme.Id, nameof(AppearanceService_InitializesWithThemeAndFont));
                AssertNearlyEqual(11f, result.ActiveAppearance.Font.SizeInPoints, nameof(AppearanceService_InitializesWithThemeAndFont));
            });
        }

        private static void AppearanceService_SetFontRaisesOneAppearanceUpdateAndPreservesTheme()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var initialFont = new Font("Segoe UI", 9f);
                using var nextFont = new Font("Segoe UI", 12f);
                AppearanceService.InitializeForTesting("default", initialFont, directory);

                int eventCount = 0;
                AppearanceDefinition latestAppearance = null;
                EventHandler<AppearanceChangedEventArgs> handler = (s, e) =>
                {
                    eventCount++;
                    latestAppearance = e.Appearance;
                };

                AppearanceService.AppearanceChanged += handler;
                try
                {
                    AppearanceService.SetFont(nextFont);
                }
                finally
                {
                    AppearanceService.AppearanceChanged -= handler;
                }

                AssertEqual(1, eventCount, nameof(AppearanceService_SetFontRaisesOneAppearanceUpdateAndPreservesTheme));
                AssertEqual("default", latestAppearance.Theme.Id, nameof(AppearanceService_SetFontRaisesOneAppearanceUpdateAndPreservesTheme));
                AssertNearlyEqual(12f, latestAppearance.Font.SizeInPoints, nameof(AppearanceService_SetFontRaisesOneAppearanceUpdateAndPreservesTheme));
            });
        }

        private static void AppearanceService_SetThemeRaisesOneAppearanceUpdateAndPreservesFont()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                CreateThemeFile(directory, "alt", "Alt Theme");
                using var font = new Font("Segoe UI", 10f);
                AppearanceService.InitializeForTesting("default", font, directory);

                int eventCount = 0;
                AppearanceDefinition latestAppearance = null;
                EventHandler<AppearanceChangedEventArgs> handler = (s, e) =>
                {
                    eventCount++;
                    latestAppearance = e.Appearance;
                };

                AppearanceService.AppearanceChanged += handler;
                try
                {
                    AppearanceService.SetCurrentTheme("alt");
                }
                finally
                {
                    AppearanceService.AppearanceChanged -= handler;
                }

                AssertEqual(1, eventCount, nameof(AppearanceService_SetThemeRaisesOneAppearanceUpdateAndPreservesFont));
                AssertEqual("alt", latestAppearance.Theme.Id, nameof(AppearanceService_SetThemeRaisesOneAppearanceUpdateAndPreservesFont));
                AssertNearlyEqual(10f, latestAppearance.Font.SizeInPoints, nameof(AppearanceService_SetThemeRaisesOneAppearanceUpdateAndPreservesFont));
            });
        }

        private static void AppearanceDefinition_DerivedFontsTrackConfiguredSize()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 13.5f);
                AppearanceInitializationResult result = AppearanceService.InitializeForTesting("default", font, directory);

                AssertNearlyEqual(13.5f, result.ActiveAppearance.BoldFont.SizeInPoints, nameof(AppearanceDefinition_DerivedFontsTrackConfiguredSize));
                AssertNearlyEqual(13.5f, result.ActiveAppearance.MonospaceFont.SizeInPoints, nameof(AppearanceDefinition_DerivedFontsTrackConfiguredSize));
                AssertEqual(FontStyle.Bold, result.ActiveAppearance.BoldFont.Style, nameof(AppearanceDefinition_DerivedFontsTrackConfiguredSize));
            });
        }

        private static void UiConstruction_MainUsesAppearanceFontAndFontAutoscaling()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                AssertEqual(AutoScaleMode.Font, form.AutoScaleMode, nameof(UiConstruction_MainUsesAppearanceFontAndFontAutoscaling));
                AssertNearlyEqual(11f, form.Font.SizeInPoints, nameof(UiConstruction_MainUsesAppearanceFontAndFontAutoscaling));
            });
        }

        private static void UiConstruction_FieldSelectionDialogUsesAppearanceFont()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var dialog = new FieldSelectionDialog(Array.Empty<MaterialFieldDescriptor>());
                IntPtr _ = dialog.Handle;

                AssertEqual(AutoScaleMode.Font, dialog.AutoScaleMode, nameof(UiConstruction_FieldSelectionDialogUsesAppearanceFont));
                AssertNearlyEqual(11f, dialog.Font.SizeInPoints, nameof(UiConstruction_FieldSelectionDialogUsesAppearanceFont));
            });
        }

        private static void UiConstruction_FieldSelectionDialogUsesColorToggleGrid()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var dialog = new FieldSelectionDialog(MaterialFieldRegistry.GetDescriptors(MaterialType.Material).Take(2).ToArray());
                IntPtr _ = dialog.Handle;

                var grid = GetDescendants(dialog)
                    .OfType<DataGridView>()
                    .Single();

                AssertEqual(true, grid.Columns[0] is DataGridViewCheckBoxColumn, nameof(UiConstruction_FieldSelectionDialogUsesColorToggleGrid));
                AssertEqual(typeof(ColorToggleCheckBoxCell), grid.Columns[0].CellTemplate.GetType(), nameof(UiConstruction_FieldSelectionDialogUsesColorToggleGrid));
            });
        }

        private static void UiConstruction_SettingsDialogReflectsConfigValues()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                CreateThemeFile(directory, "alt", "Alt");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                var config = CreateConfig(font);
                config.ThemeId = "alt";
                config.ShowSplashAnimation = false;
                config.BulkDirtyRemoveBehavior = BulkDirtyRemoveBehavior.Save;

                using var dialog = new SettingsDialog(config);
                IntPtr _ = dialog.Handle;

                AssertEqual(AutoScaleMode.Font, dialog.AutoScaleMode, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual("alt", dialog.SelectedThemeId, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual(false, dialog.SelectedShowSplashAnimation, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual(BulkDirtyRemoveBehavior.Save, dialog.SelectedBulkDirtyRemoveBehavior, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertNearlyEqual(11f, dialog.SelectedFont.SizeInPoints, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
            });
        }

        private static void UiConstruction_SettingsDialogCommitsSplashAnimationSelection()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                var config = CreateConfig(font);
                using var dialog = new SettingsDialog(config);
                IntPtr _ = dialog.Handle;

                dialog.SplashAnimationChecked = false;
                dialog.CommitSelections();

                AssertEqual(false, dialog.SelectedShowSplashAnimation, nameof(UiConstruction_SettingsDialogCommitsSplashAnimationSelection));
            });
        }

        private static void ConfigLoad_DefaultsSplashAnimationToTrueWhenUnset()
        {
            Config config = Main.LoadConfig(new NameValueCollection());
            AssertEqual(true, config.ShowSplashAnimation, nameof(ConfigLoad_DefaultsSplashAnimationToTrueWhenUnset));
        }

        private static void ConfigLoad_ReadsPersistedSplashAnimationSetting()
        {
            Config config = Main.LoadConfig(new NameValueCollection
            {
                ["ShowSplashAnimation"] = "false"
            });
            AssertEqual(false, config.ShowSplashAnimation, nameof(ConfigLoad_ReadsPersistedSplashAnimationSetting));
        }

        private static void UiConstruction_ThemeDesignerDialogBuildsPreview()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);
                ThemeDefinition theme = ThemeService.AvailableThemes.Single(themeDefinition => themeDefinition.Id == "default");

                using var dialog = new ThemeDesignerDialog(theme);
                IntPtr _ = dialog.Handle;

                AssertEqual(AutoScaleMode.Font, dialog.AutoScaleMode, nameof(UiConstruction_ThemeDesignerDialogBuildsPreview));
                AssertEqual(true, GetDescendants(dialog).OfType<MenuStrip>().Any(), nameof(UiConstruction_ThemeDesignerDialogBuildsPreview));
                AssertEqual(true, GetDescendants(dialog).OfType<DataGridView>().Any(), nameof(UiConstruction_ThemeDesignerDialogBuildsPreview));
                AssertEqual(true, GetDescendants(dialog).OfType<ListView>().Any(), nameof(UiConstruction_ThemeDesignerDialogBuildsPreview));
                AssertEqual(true, GetDescendants(dialog).OfType<ColorToggleCheckBox>().Any(), nameof(UiConstruction_ThemeDesignerDialogBuildsPreview));
            });
        }

        private static void UiConstruction_SettingsDialogRefreshesThemeAfterDesignSave()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                ThemeCatalog.SaveThemeToDirectory(
                    new ThemeDefinition(
                        "designer-copy",
                        "Designer Copy",
                        new ThemePalette(Color.Black, Color.Black, Color.Black, Color.Black, Color.White, Color.Empty, Color.White, Color.White, Color.Black, Color.White, Color.White, Color.White),
                        new ThemeSemanticColors(Color.Green, Color.Yellow, Color.Red, Color.Blue, Color.Gray, Color.Maroon, Color.Orange, Color.Purple, Color.Silver)),
                    directory);
                ThemeService.ReloadThemesForTesting(directory);

                var config = CreateConfig(font);
                using var dialog = new SettingsDialog(config);
                IntPtr _ = dialog.Handle;

                dialog.HandleThemeDesigned("designer-copy");

                AssertEqual("designer-copy", dialog.SelectedThemeId, nameof(UiConstruction_SettingsDialogRefreshesThemeAfterDesignSave));
            });
        }

        private static void UiConstruction_VariationGeneratorDialogUsesAppearanceFont()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var dialog = new VariationGeneratorDialog(Array.Empty<MaterialFieldDescriptor>(), Array.Empty<string>(), "test_{index}");
                IntPtr _ = dialog.Handle;

                AssertEqual(AutoScaleMode.Font, dialog.AutoScaleMode, nameof(UiConstruction_VariationGeneratorDialogUsesAppearanceFont));
                AssertNearlyEqual(11f, dialog.Font.SizeInPoints, nameof(UiConstruction_VariationGeneratorDialogUsesAppearanceFont));
            });
        }

        private static void UiConstruction_BulkMaterialEditorViewUsesAppearanceFont()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                AssertNearlyEqual(11f, view.Font.SizeInPoints, nameof(UiConstruction_BulkMaterialEditorViewUsesAppearanceFont));
            });
        }

        private static void UiConstruction_BulkMaterialEditorViewInitializesBooleanColumnsByName()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialPath = TestFileSupport.CreateBgsm(materialDirectory, "test.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\a.dds";
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { materialPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();

                AssertEqual(true, grid.Columns.Contains("field::" + ControlNames.TileU), nameof(UiConstruction_BulkMaterialEditorViewInitializesBooleanColumnsByName));
            });
        }

        private static void UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialsFolder = Path.Combine(materialDirectory, "Materials", "Armor");
                Directory.CreateDirectory(materialsFolder);
                string materialPath = TestFileSupport.CreateBgsm(materialsFolder, "vault.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\a.dds";
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { materialPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                AssertEqual(true, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.TileU), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.Glow), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.RefractionFalloff), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.EnvironmentMaskScale), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.DepthBias), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.PBR), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.CustomPorosity), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.AdaptativeEmissive), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.Terrain), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();

                AssertEqual("Armor\\vault.bgsm", Convert.ToString(grid.Rows[0].Cells["__path"].Value), nameof(UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath));
            });
        }

        private static void UiConstruction_BulkMaterialEditorViewRevealsDependentFieldsWhenControllerTurnsOn()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialPath = TestFileSupport.CreateBgsm(materialDirectory, "test.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\a.dds";
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { materialPath });
                MaterialFieldDescriptor refractionDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Refraction);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                AssertEqual(false, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.RefractionFalloff), nameof(UiConstruction_BulkMaterialEditorViewRevealsDependentFieldsWhenControllerTurnsOn));

                AssertEqual(true, session.TrySetCellValue(session.Rows[0], refractionDescriptor, true, out string errorMessage), nameof(UiConstruction_BulkMaterialEditorViewRevealsDependentFieldsWhenControllerTurnsOn));
                AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(UiConstruction_BulkMaterialEditorViewRevealsDependentFieldsWhenControllerTurnsOn));

                view.NotifySessionChanged();

                AssertEqual(true, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.RefractionFalloff), nameof(UiConstruction_BulkMaterialEditorViewRevealsDependentFieldsWhenControllerTurnsOn));
                AssertEqual(true, view.SelectedDescriptors.Any(descriptor => descriptor.Label == ControlNames.RefractionPower), nameof(UiConstruction_BulkMaterialEditorViewRevealsDependentFieldsWhenControllerTurnsOn));
            });
        }

        private static void UiConstruction_BulkMaterialEditorViewToggleMarksParentBooleanDirty()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialPath = TestFileSupport.CreateBgsm(materialDirectory, "test.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\a.dds";
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { materialPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                int columnIndex = grid.Columns["field::" + ControlNames.EnvironmentMapping].Index;

                MethodInfo toggleMethod = typeof(BulkMaterialEditorView).GetMethod("TryToggleBooleanCell", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, toggleMethod != null, nameof(UiConstruction_BulkMaterialEditorViewToggleMarksParentBooleanDirty));
                AssertEqual(true, (bool)toggleMethod.Invoke(view, new object[] { 0, columnIndex, null }), nameof(UiConstruction_BulkMaterialEditorViewToggleMarksParentBooleanDirty));
                AssertEqual(true, view.HasDirtyRows, nameof(UiConstruction_BulkMaterialEditorViewToggleMarksParentBooleanDirty));
                AssertEqual(true, view.IsDirtyRow(session.Rows[0]), nameof(UiConstruction_BulkMaterialEditorViewToggleMarksParentBooleanDirty));
            });
        }

        private static Config CreateConfig(Font font)
        {
            return new Config
            {
                GameVersion = Game.FO4,
                Font = font,
                ThemeId = "default",
                ShowSplashAnimation = true,
                BulkDirtyRemoveBehavior = BulkDirtyRemoveBehavior.Ask,
                BulkFieldPresets = new List<BulkFieldPreset>()
            };
        }

        private static void CreateThemeFile(string directory, string id, string displayName)
        {
            File.WriteAllText(
                Path.Combine(directory, id + ".xml"),
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<theme id=""{id}"" name=""{displayName}"">
  <palette formBackground=""#111111"" controlBackground=""#222222"" panelBackground=""#333333"" menuBackground=""#444444"" foreground=""#EEEEEE"" accent=""#88AA44"" />
  <semantic success=""#55AA55"" warning=""#CCAA44"" error=""#CC5555"" dirty=""#996600"" readOnly=""#666666"" loadError=""#884444"" validation=""#AA7733"" checkboxOff=""#777777"" selectedToggle=""#445566"" />
</theme>");
        }

        private static IEnumerable<Control> GetDescendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;

                foreach (Control descendant in GetDescendants(child))
                    yield return descendant;
            }
        }

        private static void AssertNearlyEqual(float expected, float actual, string testName)
        {
            if (Math.Abs(expected - actual) > 0.2f)
                throw new InvalidOperationException($"{testName} failed. Expected '{expected}', got '{actual}'.");
        }

        private static void AssertEqual<T>(T expected, T actual, string testName)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException($"{testName} failed. Expected '{expected}', got '{actual}'.");
        }
    }
}
