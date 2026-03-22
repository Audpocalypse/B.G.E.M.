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
            UiConstruction_MainBuildsOnlyActiveSingleEditorControls();
            UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues();
            UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen();
            UiBehavior_MainLazyBuildsCollapsedSingleEditorSectionsOnExpand();
            UiBehavior_MainHidesUnsupportedLazySectionsBeforeExpand();
            UiBehavior_MainDefersControllerDependentFieldsUntilNeeded();
            UiBehavior_MainBuildsControllerSubgroupsIndependently();
            UiBehavior_MainAppliesCurrentAppearanceWhenLazySectionBuildsAfterThemeChange();
            UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally();
            UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits();
            UiBehavior_MainSettingsApplyThemeAndFontTogether();
            UiBehavior_MainPreservesUnexpandedLazySectionValuesOnCapture();
            UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState();
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

        private static void UiConstruction_MainBuildsOnlyActiveSingleEditorControls()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiConstruction_MainBuildsOnlyActiveSingleEditorControls));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, DiffuseTexture = "textures\\a.dds" } });
                AssertEqual(true, ControlFactory.Find(ControlNames.Diffuse) != null, nameof(UiConstruction_MainBuildsOnlyActiveSingleEditorControls));
                AssertEqual(true, ControlFactory.Find(ControlNames.BaseTexture) == null, nameof(UiConstruction_MainBuildsOnlyActiveSingleEditorControls));

                createControls.Invoke(form, new object[] { new BGEM { Version = 2, BaseTexture = "textures\\b.dds" } });
                AssertEqual(true, ControlFactory.Find(ControlNames.BaseTexture) != null, nameof(UiConstruction_MainBuildsOnlyActiveSingleEditorControls));
                AssertEqual(true, ControlFactory.Find(ControlNames.Diffuse) == null, nameof(UiConstruction_MainBuildsOnlyActiveSingleEditorControls));
            });
        }

        private static void UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo workspaceModeField = typeof(Main).GetField("workspaceMode", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo effectRadioField = typeof(Main).GetField("rbTypeEffect", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo currentMaterialField = typeof(Main).GetField("currentMaterial", BindingFlags.Instance | BindingFlags.NonPublic);

                AssertEqual(true, createControls != null, nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));
                AssertEqual(true, workspaceModeField != null, nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));
                AssertEqual(true, effectRadioField != null, nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));
                AssertEqual(true, currentMaterialField != null, nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, TileU = false, Alpha = 0.5f, DiffuseTexture = "textures\\a.dds" } });

                object singleMode = workspaceModeField.FieldType.GetField("Single").GetValue(null);
                workspaceModeField.SetValue(form, singleMode);

                var effectRadio = (RadioButton)effectRadioField.GetValue(form);
                effectRadio.Checked = true;

                AssertEqual(true, ControlFactory.Find(ControlNames.BaseTexture) != null, nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));
                AssertEqual(true, ControlFactory.Find(ControlNames.Diffuse) == null, nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));
                AssertEqual(false, Convert.ToBoolean(ControlFactory.Find(ControlNames.TileU)?.GetProperty()), nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));
                AssertEqual(0.5m, (decimal)ControlFactory.Find(ControlNames.Alpha).GetProperty(), nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));
                AssertEqual(true, currentMaterialField.GetValue(form) is BGEM, nameof(UiBehavior_MainTypeToggleRebuildsSingleEditorAndPreservesSharedValues));
            });
        }

        private static void UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo currentMaterialField = typeof(Main).GetField("currentMaterial", BindingFlags.Instance | BindingFlags.NonPublic);

                AssertEqual(true, createControls != null, nameof(UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen));
                AssertEqual(true, currentMaterialField != null, nameof(UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, TileU = true, DiffuseTexture = "textures\\first.dds" } });

                CustomControl firstTileControl = ControlFactory.Find(ControlNames.TileU);
                CustomControl firstDiffuseControl = ControlFactory.Find(ControlNames.Diffuse);
                object firstMaterialInstance = currentMaterialField.GetValue(form);

                createControls.Invoke(form, new object[] { new BGSM { Version = 21, TileU = false, DiffuseTexture = "textures\\second.dds" } });

                AssertEqual(true, ReferenceEquals(firstTileControl, ControlFactory.Find(ControlNames.TileU)), nameof(UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen));
                AssertEqual(true, ReferenceEquals(firstDiffuseControl, ControlFactory.Find(ControlNames.Diffuse)), nameof(UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen));
                AssertEqual(true, ReferenceEquals(firstMaterialInstance, currentMaterialField.GetValue(form)), nameof(UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen));
                AssertEqual(false, Convert.ToBoolean(ControlFactory.Find(ControlNames.TileU)?.GetProperty()), nameof(UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen));
                AssertEqual("textures\\second.dds", Convert.ToString(ControlFactory.Find(ControlNames.Diffuse)?.GetProperty()), nameof(UiBehavior_MainReusesSingleEditorControlsForSameTypeOpen));
            });
        }

        private static void UiBehavior_MainLazyBuildsCollapsedSingleEditorSectionsOnExpand()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainLazyBuildsCollapsedSingleEditorSectionsOnExpand));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, SpecularEnabled = true, SpecularMult = 4.5f } });
                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularEnabled) != null, nameof(UiBehavior_MainLazyBuildsCollapsedSingleEditorSectionsOnExpand));

                Control specularGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Specular / Surface");
                specularGroup.GetType().GetMethod("SetCollapsed")?.Invoke(specularGroup, new object[] { false });

                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularEnabled) != null, nameof(UiBehavior_MainLazyBuildsCollapsedSingleEditorSectionsOnExpand));
                AssertEqual(true, Convert.ToBoolean(ControlFactory.Find(ControlNames.SpecularEnabled)?.GetProperty()), nameof(UiBehavior_MainLazyBuildsCollapsedSingleEditorSectionsOnExpand));
            });
        }

        private static void UiBehavior_MainHidesUnsupportedLazySectionsBeforeExpand()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainHidesUnsupportedLazySectionsBeforeExpand));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2 } });

                Control terrainGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Terrain");

                AssertEqual(false, terrainGroup.Visible, nameof(UiBehavior_MainHidesUnsupportedLazySectionsBeforeExpand));
                AssertEqual(true, ControlFactory.Find(ControlNames.Terrain) != null, nameof(UiBehavior_MainHidesUnsupportedLazySectionsBeforeExpand));
                AssertEqual(false, ControlFactory.Find(ControlNames.Terrain).ShouldBeVisible(), nameof(UiBehavior_MainHidesUnsupportedLazySectionsBeforeExpand));
            });
        }

        private static void UiBehavior_MainDefersControllerDependentFieldsUntilNeeded()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, SpecularEnabled = false, SpecularMult = 4.5f } });
                Control specularGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Specular / Surface");
                specularGroup.GetType().GetMethod("SetCollapsed")?.Invoke(specularGroup, new object[] { false });

                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularEnabled) != null, nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularColor) != null, nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularMultiplier) != null, nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
                AssertEqual(false, ControlFactory.Find(ControlNames.SpecularColor).ShouldBeVisible(), nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
                AssertEqual(false, ControlFactory.Find(ControlNames.SpecularMultiplier).ShouldBeVisible(), nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));

                var specularToggle = ControlFactory.Find(ControlNames.SpecularEnabled)?.Control as CheckBox;
                AssertEqual(true, specularToggle != null, nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
                specularToggle.Checked = true;

                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularColor) != null, nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularMultiplier) != null, nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularColor).ShouldBeVisible(), nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularMultiplier).ShouldBeVisible(), nameof(UiBehavior_MainDefersControllerDependentFieldsUntilNeeded));
            });
        }

        private static void UiBehavior_MainBuildsControllerSubgroupsIndependently()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainBuildsControllerSubgroupsIndependently));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, Hair = true, HairTintColor = 0x00AA33u, Tessellate = false } });
                Control hairGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Hair");
                Control tessellationGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Tessellation");
                hairGroup.GetType().GetMethod("SetCollapsed")?.Invoke(hairGroup, new object[] { false });
                tessellationGroup.GetType().GetMethod("SetCollapsed")?.Invoke(tessellationGroup, new object[] { false });

                AssertEqual(true, ControlFactory.Find(ControlNames.Hair) != null, nameof(UiBehavior_MainBuildsControllerSubgroupsIndependently));
                AssertEqual(true, ControlFactory.Find(ControlNames.HairTintColor) != null, nameof(UiBehavior_MainBuildsControllerSubgroupsIndependently));
                AssertEqual(true, Convert.ToBoolean(ControlFactory.Find(ControlNames.Hair)?.GetProperty()), nameof(UiBehavior_MainBuildsControllerSubgroupsIndependently));
                AssertEqual(true, ControlFactory.Find(ControlNames.Tessellate) != null, nameof(UiBehavior_MainBuildsControllerSubgroupsIndependently));
                AssertEqual(true, ControlFactory.Find(ControlNames.HairTintColor).ShouldBeVisible(), nameof(UiBehavior_MainBuildsControllerSubgroupsIndependently));
                AssertEqual(false, ControlFactory.Find(ControlNames.DisplacementTexBias).ShouldBeVisible(), nameof(UiBehavior_MainBuildsControllerSubgroupsIndependently));
            });
        }

        private static void UiBehavior_MainAppliesCurrentAppearanceWhenLazySectionBuildsAfterThemeChange()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                CreateThemeFile(directory, "alt", "Alt Theme", controlBackground: "#5A2B1D", foreground: "#F4E2A7");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainAppliesCurrentAppearanceWhenLazySectionBuildsAfterThemeChange));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, SpecularEnabled = true, SpecularMult = 4.5f } });

                AssertEqual(true, AppearanceService.SetCurrentTheme("alt"), nameof(UiBehavior_MainAppliesCurrentAppearanceWhenLazySectionBuildsAfterThemeChange));

                var specularToggle = ControlFactory.Find(ControlNames.SpecularEnabled)?.Control as ColorToggleCheckBox;
                AssertEqual(true, specularToggle != null, nameof(UiBehavior_MainAppliesCurrentAppearanceWhenLazySectionBuildsAfterThemeChange));
                AssertEqual(ColorTranslator.FromHtml("#5A2B1D"), specularToggle.BackColor, nameof(UiBehavior_MainAppliesCurrentAppearanceWhenLazySectionBuildsAfterThemeChange));
                AssertEqual(ColorTranslator.FromHtml("#F4E2A7"), specularToggle.ForeColor, nameof(UiBehavior_MainAppliesCurrentAppearanceWhenLazySectionBuildsAfterThemeChange));
            });
        }

        private static void UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally));
                AssertEqual(true, typeof(Main).GetMethod("TryPrewarmNextLazySection", BindingFlags.Instance | BindingFlags.NonPublic) == null, nameof(UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, SpecularEnabled = true, WetnessControlSpecScale = 2.5f } });

                Control specularGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Specular / Surface");
                PropertyInfo isCollapsedProperty = specularGroup.GetType().GetProperty("IsCollapsed");
                AssertEqual(true, isCollapsedProperty != null, nameof(UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally));

                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularEnabled) != null, nameof(UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally));
                AssertEqual(true, ControlFactory.Find(ControlNames.RimLighting) != null, nameof(UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally));
                AssertEqual(true, ControlFactory.Find(ControlNames.WetSpecScale) != null, nameof(UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally));
                AssertEqual(true, (bool)isCollapsedProperty.GetValue(specularGroup), nameof(UiBehavior_MainPrewarmsCollapsedSingleEditorSectionsIncrementally));
            });
        }

        private static void UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                using var nextFont = new Font("Consolas", 13f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo captureState = typeof(Main).GetMethod("CaptureCurrentSingleEditorState", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo workspaceModeField = typeof(Main).GetField("workspaceMode", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo pendingRebuildField = typeof(Main).GetField("pendingSingleEditorAppearanceRebuildMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));
                AssertEqual(true, captureState != null, nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));
                AssertEqual(true, workspaceModeField != null, nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));
                AssertEqual(true, pendingRebuildField != null, nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, SpecularEnabled = true, SpecularMult = 4.5f } });
                object singleMode = workspaceModeField.FieldType.GetField("Single").GetValue(null);
                workspaceModeField.SetValue(form, singleMode);

                Control specularGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Specular / Surface");
                specularGroup.GetType().GetMethod("SetCollapsed")?.Invoke(specularGroup, new object[] { false });

                ControlFactory.SetProperty(ControlNames.SpecularMultiplier, 9.5m);
                pendingRebuildField.SetValue(form, captureState.Invoke(form, Array.Empty<object>()));

                AppearanceService.SetFont(nextFont);

                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularEnabled) != null, nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));
                AssertNearlyEqual(13f, form.Font.SizeInPoints, nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));

                Control materialPage = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Material");
                PropertyInfo isCollapsedProperty = materialPage.GetType().GetProperty("IsCollapsed");
                AssertEqual(true, isCollapsedProperty != null, nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));
                AssertEqual(true, (bool)isCollapsedProperty.GetValue(materialPage), nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));

                materialPage.GetType().GetMethod("SetCollapsed")?.Invoke(materialPage, new object[] { false });
                specularGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Specular / Surface");
                specularGroup.GetType().GetMethod("SetCollapsed")?.Invoke(specularGroup, new object[] { false });

                AssertEqual(9.5m, (decimal)ControlFactory.Find(ControlNames.SpecularMultiplier).GetProperty(), nameof(UiBehavior_MainFontChangeRebuildsCollapsedLazyTreeAndPreservesEdits));
            });
        }

        private static void UiBehavior_MainSettingsApplyThemeAndFontTogether()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                CreateThemeFile(
                    directory,
                    "alt",
                    "Alt Theme",
                    formBackground: "#203040",
                    controlBackground: "#5A2B1D",
                    foreground: "#F4E2A7");
                using var font = new Font("Segoe UI", 11f);
                using var nextFont = new Font("Consolas", 13f);
                AppearanceService.InitializeForTesting("default", font, directory);

                Config config = CreateConfig(font);
                using var form = new Main(config);
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applySettingsSelections = typeof(Main).GetMethod("ApplySettingsSelections", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo workspaceModeField = typeof(Main).GetField("workspaceMode", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(true, applySettingsSelections != null, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(true, workspaceModeField != null, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, SpecularEnabled = true, SpecularMult = 4.5f } });
                object singleMode = workspaceModeField.FieldType.GetField("Single").GetValue(null);
                workspaceModeField.SetValue(form, singleMode);

                applySettingsSelections.Invoke(form, new object[] { "alt", nextFont, false, BulkDirtyRemoveBehavior.Save });

                AssertNearlyEqual(13f, form.Font.SizeInPoints, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual("alt", config.ThemeId, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(false, config.ShowSplashAnimation, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(BulkDirtyRemoveBehavior.Save, config.BulkDirtyRemoveBehavior, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(ColorTranslator.FromHtml("#203040"), form.BackColor, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual("alt", AppearanceService.CurrentAppearance.Theme.Id, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertNearlyEqual(13f, AppearanceService.CurrentAppearance.Font.SizeInPoints, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
            });
        }

        private static void UiBehavior_MainPreservesUnexpandedLazySectionValuesOnCapture()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo getMaterialValues = typeof(Main).GetMethod("GetMaterialValues", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, createControls != null, nameof(UiBehavior_MainPreservesUnexpandedLazySectionValuesOnCapture));
                AssertEqual(true, getMaterialValues != null, nameof(UiBehavior_MainPreservesUnexpandedLazySectionValuesOnCapture));

                createControls.Invoke(form, new object[] { new BGSM { Version = 2, SpecularEnabled = true, SpecularMult = 7.25f } });
                AssertEqual(true, ControlFactory.Find(ControlNames.SpecularEnabled) != null, nameof(UiBehavior_MainPreservesUnexpandedLazySectionValuesOnCapture));

                var snapshot = new BGSM();
                getMaterialValues.Invoke(form, new object[] { snapshot });

                AssertEqual(true, snapshot.SpecularEnabled, nameof(UiBehavior_MainPreservesUnexpandedLazySectionValuesOnCapture));
                AssertNearlyEqual(7.25f, snapshot.SpecularMult, nameof(UiBehavior_MainPreservesUnexpandedLazySectionValuesOnCapture));
            });
        }

        private static void UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                Config config = CreateConfig(font);
                config.GameVersion = Game.FO76;

                using var form = new Main(config);
                IntPtr _ = form.Handle;

                MethodInfo createControls = typeof(Main).GetMethod("CreateMaterialControls", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo fillVersionDropdown = typeof(Main).GetMethod("FillVersionDropdown", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo setGameSelection = typeof(Main).GetMethod("SetGameSelection", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo workspaceModeField = typeof(Main).GetField("workspaceMode", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo workFilePathField = typeof(Main).GetField("workFilePath", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo changedField = typeof(Main).GetField("changed", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo currentMaterialField = typeof(Main).GetField("currentMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo fo4RadioField = typeof(Main).GetField("rbGameFO4", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo listVersionField = typeof(Main).GetField("listVersion", BindingFlags.Instance | BindingFlags.NonPublic);

                AssertEqual(true, createControls != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, fillVersionDropdown != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, setGameSelection != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, workspaceModeField != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, workFilePathField != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, changedField != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, currentMaterialField != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, fo4RadioField != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, listVersionField != null, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));

                createControls.Invoke(form, new object[] { new BGSM { Version = 21, DiffuseTexture = "textures\\f76.dds", DistanceFieldAlphaTexture = "textures\\dfa.dds" } });

                object singleMode = workspaceModeField.FieldType.GetField("Single").GetValue(null);
                workspaceModeField.SetValue(form, singleMode);
                workFilePathField.SetValue(form, "sample.bgsm");

                setGameSelection.Invoke(form, new object[] { Game.FO76 });
                fillVersionDropdown.Invoke(form, null);
                changedField.SetValue(form, false);

                var fo4Radio = (RadioButton)fo4RadioField.GetValue(form);
                var versionList = (ComboBox)listVersionField.GetValue(form);

                fo4Radio.Checked = true;

                var currentMaterial = (BGSM)currentMaterialField.GetValue(form);

                AssertEqual((uint)21, currentMaterial.Version, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(false, (bool)changedField.GetValue(form), nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(Game.FO4, config.GameVersion, nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual(true, versionList.Items.Cast<object>().Any(item => Convert.ToUInt32(item) == 21u), nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
                AssertEqual((uint)21, Convert.ToUInt32(versionList.SelectedItem), nameof(UiBehavior_MainGameTogglePreservesOpenMaterialVersionAndCleanState));
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

        private static void CreateThemeFile(string directory, string id, string displayName, string formBackground = "#111111", string controlBackground = "#222222", string foreground = "#EEEEEE")
        {
            File.WriteAllText(
                Path.Combine(directory, id + ".xml"),
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<theme id=""{id}"" name=""{displayName}"">
  <palette formBackground=""{formBackground}"" controlBackground=""{controlBackground}"" panelBackground=""#333333"" menuBackground=""#444444"" foreground=""{foreground}"" accent=""#88AA44"" />
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
