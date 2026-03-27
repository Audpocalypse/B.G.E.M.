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
            UiConstruction_MainStartsAtDefaultEditorSize();
            UiConstruction_FieldSelectionDialogUsesAppearanceFont();
            UiConstruction_FieldSelectionDialogUsesColorToggleGrid();
            UiConstruction_SettingsDialogReflectsConfigValues();
            UiConstruction_SettingsDialogStartsSizedToAvoidScrolling();
            UiBehavior_BackupRecoveryDialogSupportsSelectAllAndSelectNone();
            UiBehavior_FieldSelectionDialogSupportsSelectAllShortcutAndContextSelectNone();
            UiConstruction_SettingsDialogCommitsSplashAnimationSelection();
            ConfigLoad_DefaultsSplashAnimationToTrueWhenUnset();
            ConfigLoad_ReadsPersistedSplashAnimationSetting();
            UiConstruction_ThemeDesignerDialogBuildsPreview();
            UiConstruction_SettingsDialogRefreshesThemeAfterDesignSave();
            UiConstruction_VariationGeneratorDialogUsesAppearanceFont();
            UiConstruction_VariationGeneratorDialogShowsBulkReturnOptionWhenEnabled();
            UiConstruction_TargetFileSelectionDialogShowsBulkReturnOptionWhenEnabled();
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
            UiBehavior_MainUndoRestoresPreviousSingleEditorState();
            UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges();
            UiBehavior_MainUndoPreservesOpenSingleEditorSections();
            UiBehavior_MainUndoOpensClosedAffectedSections();
            UiConstruction_BulkMaterialEditorViewUsesAppearanceFont();
            UiConstruction_BulkMaterialEditorViewInitializesBooleanColumnsByName();
            UiConstruction_BulkMaterialEditorViewOmitsEmptyFieldsAndTrimsDisplayedPath();
            UiConstruction_BulkMaterialEditorViewRevealsDependentFieldsWhenControllerTurnsOn();
            UiConstruction_BulkMaterialEditorViewToggleMarksParentBooleanDirty();
            UiConstruction_BulkMaterialEditorViewUsesCellSelectionMode();
            UiBehavior_BulkMaterialEditorViewSelectedRowsTrackCellSelection();
            UiBehavior_BulkMaterialEditorViewShiftPageDownExtendsSelection();
            UiBehavior_BulkMaterialEditorViewRowSelectorClickSelectsEntireRow();
            UiBehavior_BulkMaterialEditorViewRowDragSelectsMultipleFiles();
            UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiRowSelection();
            UiBehavior_BulkMaterialEditorViewSelectingBooleanCellDoesNotToggleValue();
            UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiFieldSelection();
            UiBehavior_BulkMaterialEditorViewSelectingNumericCellDoesNotEnterEditMode();
            UiBehavior_BulkMaterialEditorViewShiftArrowExtendsSelection();
            UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection();
            UiBehavior_BulkMaterialEditorViewReplaceAllMatchesInColumnUpdatesTextCells();
            UiBehavior_BulkMaterialEditorViewFindCanCreateCustomFilter();
            UiBehavior_BulkMaterialEditorViewFindPreviousWrapsAroundLoadedFiles();
            UiBehavior_BulkMaterialEditorViewReplaceUpdatesCurrentMatchOnly();
            UiBehavior_BulkMaterialEditorViewReplaceBooleanMatchesUpdatesMatchingFiles();
            UiBehavior_BulkMaterialEditorViewReplaceNumericMatchesUpdatesMatchingFiles();
            UiConstruction_MainBulkEditMenuExposesRowClipboardShortcuts();
            UiConstruction_MainMenuBindingsMatchRequestedShortcuts();
            UiBehavior_BulkMaterialEditorViewCtrlShiftCUsesRowClipboard();
            UiBehavior_BulkMaterialEditorViewCtrlShiftVPastesRows();
            UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows();
            UiBehavior_BulkMaterialEditorViewCtrlXCutsFields();
            UiBehavior_BulkMaterialEditorViewDeleteClearsFields();
            UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors();
            UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors();
            UiBehavior_BulkMaterialEditorViewCtrlEnterSelectsCurrentRow();
            UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows();
            UiBehavior_BulkMaterialEditorViewCtrlTCyclesFilters();
            UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows();
            UiBehavior_BulkMaterialEditorViewCopyRowsWritesSelectedDescriptorMatrixToClipboard();
            UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection();
            UiBehavior_BulkMaterialEditorViewCopyFieldsPreservesSparseRectangularSelection();
            UiBehavior_BulkMaterialEditorViewPasteFieldsSingleValueFillsSparseSelection();
            UiBehavior_BulkMaterialEditorViewFillUpCopiesBottomValue();
            UiBehavior_BulkMaterialEditorViewFillDownCopiesTopValue();
            UiBehavior_BulkMaterialEditorViewDefaultsToAlphabeticalAscendingProjection();
            UiBehavior_BulkMaterialEditorViewSortByAdditionRestoresSessionOrder();
            UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary();
            UiBehavior_BulkMaterialEditorViewGroupsByFolderAndSortsWithinFolder();
            UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation();
            UiConstruction_MainToolsMenuIncludesFilterAndSortMenus();
            UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus();
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

        private static void UiConstruction_MainStartsAtDefaultEditorSize()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                AssertEqual(1280, form.ClientSize.Width, nameof(UiConstruction_MainStartsAtDefaultEditorSize));
                AssertEqual(860, form.ClientSize.Height, nameof(UiConstruction_MainStartsAtDefaultEditorSize));
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
                config.CreateBackupsByDefault = false;
                config.RetainOriginalBackup = true;
                config.MaxBackupsPerFile = 3;
                config.MaxBackupFolderMegabytes = 128;

                using var dialog = new SettingsDialog(config);
                IntPtr _ = dialog.Handle;

                AssertEqual(AutoScaleMode.Font, dialog.AutoScaleMode, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual("alt", dialog.SelectedThemeId, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual(false, dialog.SelectedShowSplashAnimation, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual(BulkDirtyRemoveBehavior.Save, dialog.SelectedBulkDirtyRemoveBehavior, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual(false, dialog.SelectedCreateBackupsByDefault, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual(true, dialog.SelectedRetainOriginalBackup, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual(3, dialog.SelectedMaxBackupsPerFile, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertEqual(128L, dialog.SelectedMaxBackupFolderMegabytes, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
                AssertNearlyEqual(11f, dialog.SelectedFont.SizeInPoints, nameof(UiConstruction_SettingsDialogReflectsConfigValues));
            });
        }

        private static void UiConstruction_SettingsDialogStartsSizedToAvoidScrolling()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var dialog = new SettingsDialog(CreateConfig(font));
                IntPtr _ = dialog.Handle;
                dialog.PerformLayout();

                FieldInfo contentPanelField = typeof(SettingsDialog).GetField("contentPanel", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, contentPanelField != null, nameof(UiConstruction_SettingsDialogStartsSizedToAvoidScrolling));

                var contentPanel = (Panel)contentPanelField.GetValue(dialog);
                AssertTrue(dialog.ClientSize.Width >= 740, nameof(UiConstruction_SettingsDialogStartsSizedToAvoidScrolling));
                AssertTrue(dialog.ClientSize.Height >= 470, nameof(UiConstruction_SettingsDialogStartsSizedToAvoidScrolling));
                AssertEqual(false, contentPanel.VerticalScroll.Visible, nameof(UiConstruction_SettingsDialogStartsSizedToAvoidScrolling));
            });
        }

        private static void UiBehavior_BackupRecoveryDialogSupportsSelectAllAndSelectNone()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                var backups = new[]
                {
                    new MaterialBackupEntry("materials\\first.bgsm", "backup\\first_1.bak", "first_1.bak", DateTime.UtcNow.AddMinutes(-2), 1024, isLegacy: false, isRetainedOriginal: false),
                    new MaterialBackupEntry("materials\\second.bgsm", "backup\\second_1.bak", "second_1.bak", DateTime.UtcNow.AddMinutes(-1), 2048, isLegacy: false, isRetainedOriginal: false)
                };

                using var dialog = new BackupRecoveryDialog(backups, "Available Backups");
                IntPtr _ = dialog.Handle;
                dialog.Show();
                Application.DoEvents();

                DataGridView grid = GetDescendants(dialog).OfType<DataGridView>().Single();
                ContextMenuStrip menu = grid.ContextMenuStrip;
                AssertEqual(true, menu != null, nameof(UiBehavior_BackupRecoveryDialogSupportsSelectAllAndSelectNone));

                ToolStripMenuItem selectAllItem = menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Single(item => item.Text == "Select All");
                selectAllItem.PerformClick();
                AssertEqual(2, grid.SelectedRows.Count, nameof(UiBehavior_BackupRecoveryDialogSupportsSelectAllAndSelectNone));

                ToolStripMenuItem selectNoneItem = menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Single(item => item.Text == "Select None");
                selectNoneItem.PerformClick();
                AssertEqual(0, grid.SelectedRows.Count, nameof(UiBehavior_BackupRecoveryDialogSupportsSelectAllAndSelectNone));
            });
        }

        private static void UiBehavior_FieldSelectionDialogSupportsSelectAllShortcutAndContextSelectNone()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var dialog = new FieldSelectionDialog(MaterialFieldRegistry.GetDescriptors(MaterialType.Material).Take(3).ToArray());
                IntPtr _ = dialog.Handle;
                dialog.Show();
                Application.DoEvents();

                DataGridView grid = GetDescendants(dialog).OfType<DataGridView>().Single();
                ContextMenuStrip menu = grid.ContextMenuStrip;
                AssertEqual(true, menu != null, nameof(UiBehavior_FieldSelectionDialogSupportsSelectAllShortcutAndContextSelectNone));

                ToolStripMenuItem selectAllItem = menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Single(item => item.Text == "Select All");
                selectAllItem.PerformClick();
                AssertTrue(grid.Rows.Cast<DataGridViewRow>().All(row => Convert.ToBoolean(row.Cells[0].Value)), nameof(UiBehavior_FieldSelectionDialogSupportsSelectAllShortcutAndContextSelectNone));

                ToolStripMenuItem selectNoneItem = menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Single(item => item.Text == "Select None");
                selectNoneItem.PerformClick();
                AssertTrue(grid.Rows.Cast<DataGridViewRow>().All(row => !Convert.ToBoolean(row.Cells[0].Value)), nameof(UiBehavior_FieldSelectionDialogSupportsSelectAllShortcutAndContextSelectNone));
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
                dialog.CreateBackupsByDefaultChecked = false;
                dialog.RetainOriginalBackupChecked = true;
                dialog.MaxBackupsPerFileValue = 4;
                dialog.MaxBackupFolderMegabytesValue = 256;
                dialog.CommitSelections();

                AssertEqual(false, dialog.SelectedShowSplashAnimation, nameof(UiConstruction_SettingsDialogCommitsSplashAnimationSelection));
                AssertEqual(false, dialog.SelectedCreateBackupsByDefault, nameof(UiConstruction_SettingsDialogCommitsSplashAnimationSelection));
                AssertEqual(true, dialog.SelectedRetainOriginalBackup, nameof(UiConstruction_SettingsDialogCommitsSplashAnimationSelection));
                AssertEqual(4, dialog.SelectedMaxBackupsPerFile, nameof(UiConstruction_SettingsDialogCommitsSplashAnimationSelection));
                AssertEqual(256L, dialog.SelectedMaxBackupFolderMegabytes, nameof(UiConstruction_SettingsDialogCommitsSplashAnimationSelection));
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
                ["ShowSplashAnimation"] = "false",
                ["CreateBackupsByDefault"] = "false",
                ["RetainOriginalBackup"] = "true",
                ["MaxBackupsPerFile"] = "5",
                ["MaxBackupFolderMegabytes"] = "64"
            });
            AssertEqual(false, config.ShowSplashAnimation, nameof(ConfigLoad_ReadsPersistedSplashAnimationSetting));
            AssertEqual(false, config.CreateBackupsByDefault, nameof(ConfigLoad_ReadsPersistedSplashAnimationSetting));
            AssertEqual(true, config.RetainOriginalBackup, nameof(ConfigLoad_ReadsPersistedSplashAnimationSetting));
            AssertEqual(5, config.MaxBackupsPerFile, nameof(ConfigLoad_ReadsPersistedSplashAnimationSetting));
            AssertEqual(64L, config.MaxBackupFolderMegabytes, nameof(ConfigLoad_ReadsPersistedSplashAnimationSetting));
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

        private static void UiConstruction_VariationGeneratorDialogShowsBulkReturnOptionWhenEnabled()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var dialog = new VariationGeneratorDialog(Array.Empty<MaterialFieldDescriptor>(), Array.Empty<string>(), "test_{index}", allowReturnToBulkEditor: true);
                IntPtr _ = dialog.Handle;

                ColorToggleCheckBox bulkReturnCheckBox = GetDescendants(dialog)
                    .OfType<ColorToggleCheckBox>()
                    .FirstOrDefault(checkBox => checkBox.Text.Contains("current bulk editor", StringComparison.OrdinalIgnoreCase));

                AssertEqual(true, bulkReturnCheckBox != null && bulkReturnCheckBox.Checked, nameof(UiConstruction_VariationGeneratorDialogShowsBulkReturnOptionWhenEnabled));
                AssertEqual(true, dialog.AddResultsToCurrentBulkEditor, nameof(UiConstruction_VariationGeneratorDialogShowsBulkReturnOptionWhenEnabled));
            });
        }

        private static void UiConstruction_TargetFileSelectionDialogShowsBulkReturnOptionWhenEnabled()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var dialog = new TargetFileSelectionDialog(MaterialType.Material, allowReturnToBulkEditor: true);
                IntPtr _ = dialog.Handle;

                ColorToggleCheckBox bulkReturnCheckBox = GetDescendants(dialog)
                    .OfType<ColorToggleCheckBox>()
                    .FirstOrDefault(checkBox => checkBox.Text.Contains("current bulk editor", StringComparison.OrdinalIgnoreCase));

                AssertEqual(true, bulkReturnCheckBox != null && bulkReturnCheckBox.Checked, nameof(UiConstruction_TargetFileSelectionDialogShowsBulkReturnOptionWhenEnabled));
                AssertEqual(true, dialog.AddResultsToCurrentBulkEditor, nameof(UiConstruction_TargetFileSelectionDialogShowsBulkReturnOptionWhenEnabled));
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

                applySettingsSelections.Invoke(form, new object[] { "alt", nextFont, false, BulkDirtyRemoveBehavior.Save, false, true, 2, 48L });

                AssertNearlyEqual(13f, form.Font.SizeInPoints, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual("alt", config.ThemeId, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(false, config.ShowSplashAnimation, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(BulkDirtyRemoveBehavior.Save, config.BulkDirtyRemoveBehavior, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(false, config.CreateBackupsByDefault, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(true, config.RetainOriginalBackup, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(2, config.MaxBackupsPerFile, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
                AssertEqual(48L, config.MaxBackupFolderMegabytes, nameof(UiBehavior_MainSettingsApplyThemeAndFontTogether));
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

        private static void UiBehavior_MainUndoRestoresPreviousSingleEditorState()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo openMaterialState = typeof(Main).GetMethod("OpenMaterialState", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo undoMenuField = typeof(Main).GetField("editUndoToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo redoMenuField = typeof(Main).GetField("editRedoToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo changedField = typeof(Main).GetField("changed", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, openMaterialState != null, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(true, undoMenuField != null, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(true, redoMenuField != null, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(true, changedField != null, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));

                var material = new BGSM
                {
                    Version = 2,
                    DiffuseTexture = "textures\\original.dds"
                };

                openMaterialState.Invoke(form, new object[] { "c:\\temp\\undo.bgsm", material, material, false, "Opening material..." });

                var diffuseTextBox = ControlFactory.Find(ControlNames.Diffuse)?.Control as TextBox;
                var undoMenu = (ToolStripMenuItem)undoMenuField.GetValue(form);
                var redoMenu = (ToolStripMenuItem)redoMenuField.GetValue(form);
                AssertEqual(true, diffuseTextBox != null, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(true, undoMenu != null, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(true, redoMenu != null, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));

                diffuseTextBox.Text = "textures\\changed.dds";

                AssertEqual(true, undoMenu.Enabled, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(false, redoMenu.Enabled, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(true, (bool)changedField.GetValue(form), nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));

                undoMenu.PerformClick();

                AssertEqual("textures\\original.dds", diffuseTextBox.Text, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(false, undoMenu.Enabled, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(true, redoMenu.Enabled, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(false, (bool)changedField.GetValue(form), nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));

                redoMenu.PerformClick();

                AssertEqual("textures\\changed.dds", diffuseTextBox.Text, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(true, undoMenu.Enabled, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
                AssertEqual(false, redoMenu.Enabled, nameof(UiBehavior_MainUndoRestoresPreviousSingleEditorState));
            });
        }

        private static void UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo openMaterialState = typeof(Main).GetMethod("OpenMaterialState", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo undoMenuField = typeof(Main).GetField("editUndoToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo redoMenuField = typeof(Main).GetField("editRedoToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, openMaterialState != null, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(true, undoMenuField != null, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(true, redoMenuField != null, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));

                var material = new BGSM
                {
                    Version = 2,
                    DiffuseTexture = "textures\\original.dds",
                    Alpha = 1f
                };

                openMaterialState.Invoke(form, new object[] { "c:\\temp\\undo-multi.bgsm", material, material, false, "Opening material..." });

                var diffuseTextBox = ControlFactory.Find(ControlNames.Diffuse)?.Control as TextBox;
                var alphaControl = ControlFactory.Find(ControlNames.Alpha)?.Control as NumericUpDown;
                var undoMenu = (ToolStripMenuItem)undoMenuField.GetValue(form);
                var redoMenu = (ToolStripMenuItem)redoMenuField.GetValue(form);
                AssertEqual(true, diffuseTextBox != null, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(true, alphaControl != null, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(true, undoMenu != null, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(true, redoMenu != null, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));

                diffuseTextBox.Text = "textures\\first-edit.dds";
                alphaControl.Value = 2m;

                AssertEqual(true, undoMenu.Enabled, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(false, redoMenu.Enabled, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));

                undoMenu.PerformClick();
                AssertEqual("textures\\first-edit.dds", diffuseTextBox.Text, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(1m, alphaControl.Value, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));

                undoMenu.PerformClick();
                AssertEqual("textures\\original.dds", diffuseTextBox.Text, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(1m, alphaControl.Value, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));

                redoMenu.PerformClick();
                AssertEqual("textures\\first-edit.dds", diffuseTextBox.Text, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(1m, alphaControl.Value, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));

                redoMenu.PerformClick();
                AssertEqual("textures\\first-edit.dds", diffuseTextBox.Text, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
                AssertEqual(2m, alphaControl.Value, nameof(UiBehavior_MainUndoRestoresFirstEditAfterMultipleSingleEditorChanges));
            });
        }

        private static void UiBehavior_MainUndoPreservesOpenSingleEditorSections()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo openMaterialState = typeof(Main).GetMethod("OpenMaterialState", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo undoMenuField = typeof(Main).GetField("editUndoToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo redoMenuField = typeof(Main).GetField("editRedoToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, openMaterialState != null, nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
                AssertEqual(true, undoMenuField != null, nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
                AssertEqual(true, redoMenuField != null, nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));

                var material = new BGSM
                {
                    Version = 2,
                    SpecularEnabled = true,
                    SpecularMult = 4.5f,
                    Alpha = 1f
                };

                openMaterialState.Invoke(form, new object[] { "c:\\temp\\undo-sections.bgsm", material, material, false, "Opening material..." });

                Control generalPage = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "General");
                Control materialPage = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Material");
                Control specularGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Specular / Surface");
                PropertyInfo isCollapsedProperty = generalPage.GetType().GetProperty("IsCollapsed");
                AssertEqual(true, isCollapsedProperty != null, nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));

                generalPage.GetType().GetMethod("SetCollapsed")?.Invoke(generalPage, new object[] { false });
                materialPage.GetType().GetMethod("SetCollapsed")?.Invoke(materialPage, new object[] { false });
                specularGroup.GetType().GetMethod("SetCollapsed")?.Invoke(specularGroup, new object[] { false });

                var alphaControl = ControlFactory.Find(ControlNames.Alpha)?.Control as NumericUpDown;
                var undoMenu = (ToolStripMenuItem)undoMenuField.GetValue(form);
                var redoMenu = (ToolStripMenuItem)redoMenuField.GetValue(form);
                AssertEqual(true, alphaControl != null, nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
                AssertEqual(true, undoMenu != null, nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
                AssertEqual(true, redoMenu != null, nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));

                alphaControl.Value = 2m;
                undoMenu.PerformClick();

                AssertEqual(false, (bool)isCollapsedProperty.GetValue(generalPage), nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
                AssertEqual(false, (bool)isCollapsedProperty.GetValue(materialPage), nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
                AssertEqual(false, (bool)isCollapsedProperty.GetValue(specularGroup), nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));

                redoMenu.PerformClick();

                AssertEqual(false, (bool)isCollapsedProperty.GetValue(generalPage), nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
                AssertEqual(false, (bool)isCollapsedProperty.GetValue(materialPage), nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
                AssertEqual(false, (bool)isCollapsedProperty.GetValue(specularGroup), nameof(UiBehavior_MainUndoPreservesOpenSingleEditorSections));
            });
        }

        private static void UiBehavior_MainUndoOpensClosedAffectedSections()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                MethodInfo openMaterialState = typeof(Main).GetMethod("OpenMaterialState", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo undoMenuField = typeof(Main).GetField("editUndoToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo redoMenuField = typeof(Main).GetField("editRedoToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, openMaterialState != null, nameof(UiBehavior_MainUndoOpensClosedAffectedSections));
                AssertEqual(true, undoMenuField != null, nameof(UiBehavior_MainUndoOpensClosedAffectedSections));
                AssertEqual(true, redoMenuField != null, nameof(UiBehavior_MainUndoOpensClosedAffectedSections));

                var material = new BGSM
                {
                    Version = 2,
                    SpecularEnabled = true,
                    SpecularMult = 4.5f
                };

                openMaterialState.Invoke(form, new object[] { "c:\\temp\\undo-closed-sections.bgsm", material, material, false, "Opening material..." });

                Control materialPage = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Material");
                Control specularGroup = GetDescendants(form)
                    .First(control => control.GetType().Name == "CollapsibleGroupBox" && control.Text == "Specular / Surface");
                PropertyInfo isCollapsedProperty = materialPage.GetType().GetProperty("IsCollapsed");
                AssertEqual(true, isCollapsedProperty != null, nameof(UiBehavior_MainUndoOpensClosedAffectedSections));

                materialPage.GetType().GetMethod("SetCollapsed")?.Invoke(materialPage, new object[] { true });
                specularGroup.GetType().GetMethod("SetCollapsed")?.Invoke(specularGroup, new object[] { true });

                var specularMultiplierControl = ControlFactory.Find(ControlNames.SpecularMultiplier)?.Control as NumericUpDown;
                var undoMenu = (ToolStripMenuItem)undoMenuField.GetValue(form);
                var redoMenu = (ToolStripMenuItem)redoMenuField.GetValue(form);
                AssertEqual(true, specularMultiplierControl != null, nameof(UiBehavior_MainUndoOpensClosedAffectedSections));
                AssertEqual(true, undoMenu != null, nameof(UiBehavior_MainUndoOpensClosedAffectedSections));
                AssertEqual(true, redoMenu != null, nameof(UiBehavior_MainUndoOpensClosedAffectedSections));

                specularMultiplierControl.Value = 9.5m;
                undoMenu.PerformClick();

                AssertEqual(false, (bool)isCollapsedProperty.GetValue(materialPage), nameof(UiBehavior_MainUndoOpensClosedAffectedSections));
                AssertEqual(false, (bool)isCollapsedProperty.GetValue(specularGroup), nameof(UiBehavior_MainUndoOpensClosedAffectedSections));

                materialPage.GetType().GetMethod("SetCollapsed")?.Invoke(materialPage, new object[] { true });
                specularGroup.GetType().GetMethod("SetCollapsed")?.Invoke(specularGroup, new object[] { true });

                redoMenu.PerformClick();

                AssertEqual(false, (bool)isCollapsedProperty.GetValue(materialPage), nameof(UiBehavior_MainUndoOpensClosedAffectedSections));
                AssertEqual(false, (bool)isCollapsedProperty.GetValue(specularGroup), nameof(UiBehavior_MainUndoOpensClosedAffectedSections));
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

        private static void UiConstruction_BulkMaterialEditorViewUsesCellSelectionMode()
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

                AssertEqual(DataGridViewSelectionMode.CellSelect, grid.SelectionMode, nameof(UiConstruction_BulkMaterialEditorViewUsesCellSelectionMode));
                AssertEqual(DataGridViewEditMode.EditProgrammatically, grid.EditMode, nameof(UiConstruction_BulkMaterialEditorViewUsesCellSelectionMode));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewSelectedRowsTrackCellSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string firstPath = TestFileSupport.CreateBgsm(materialDirectory, "a.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\a.dds";
                });
                string secondPath = TestFileSupport.CreateBgsm(materialDirectory, "b.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\b.dds";
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { firstPath, secondPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                DataGridViewCell firstCell = grid.Rows[0].Cells["field::" + ControlNames.Diffuse];
                DataGridViewCell secondCell = grid.Rows[1].Cells["field::" + ControlNames.Diffuse];

                grid.ClearSelection();
                grid.CurrentCell = firstCell;
                firstCell.Selected = true;
                secondCell.Selected = true;

                AssertEqual(2, view.SelectedRows.Count, nameof(UiBehavior_BulkMaterialEditorViewSelectedRowsTrackCellSelection));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewShiftPageDownExtendsSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 4)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"test{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\{index}.dds";
                    }))
                    .ToArray();
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form { Width = 900, Height = 320 };
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                DataGridViewCell firstCell = grid.Rows[0].Cells["field::" + ControlNames.Diffuse];
                grid.ClearSelection();
                grid.CurrentCell = firstCell;
                firstCell.Selected = true;

                MethodInfo shortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridSelectionShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, shortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewShiftPageDownExtendsSelection));

                var args = new KeyEventArgs(Keys.Shift | Keys.End);
                AssertEqual(true, (bool)shortcutMethod.Invoke(view, new object[] { args }), nameof(UiBehavior_BulkMaterialEditorViewShiftPageDownExtendsSelection));
                AssertTrue(grid.CurrentCell.ColumnIndex > firstCell.ColumnIndex, nameof(UiBehavior_BulkMaterialEditorViewShiftPageDownExtendsSelection));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewRowDragSelectsMultipleFiles()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 3)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"row{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\row{index}.dds";
                    }))
                    .ToArray();
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                int pathColumnIndex = grid.Columns["__path"].Index;

                MethodInfo mouseDownMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo mouseEnterMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseEnter", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo mouseUpMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, mouseDownMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRowDragSelectsMultipleFiles));
                AssertEqual(true, mouseEnterMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRowDragSelectsMultipleFiles));
                AssertEqual(true, mouseUpMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRowDragSelectsMultipleFiles));

                var downArgs = new DataGridViewCellMouseEventArgs(pathColumnIndex, 0, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                var enterArgs = new DataGridViewCellEventArgs(pathColumnIndex, 2);
                var upArgs = new DataGridViewCellMouseEventArgs(pathColumnIndex, 2, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

                mouseDownMethod.Invoke(view, new object[] { downArgs });
                mouseEnterMethod.Invoke(view, new object[] { enterArgs });
                mouseUpMethod.Invoke(view, new object[] { upArgs });

                AssertEqual(3, view.SelectedRows.Count, nameof(UiBehavior_BulkMaterialEditorViewRowDragSelectsMultipleFiles));
                AssertTrue(grid.Rows.Cast<DataGridViewRow>().Take(3).All(row => row.Cells.Cast<DataGridViewCell>().All(cell => cell.Selected)), nameof(UiBehavior_BulkMaterialEditorViewRowDragSelectsMultipleFiles));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewRowSelectorClickSelectsEntireRow()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"single-row{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\single-row{index}.dds";
                    }))
                    .ToArray();
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                int pathColumnIndex = grid.Columns["__path"].Index;

                MethodInfo mouseDownMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo mouseUpMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, mouseDownMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRowSelectorClickSelectsEntireRow));
                AssertEqual(true, mouseUpMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRowSelectorClickSelectsEntireRow));

                var downArgs = new DataGridViewCellMouseEventArgs(pathColumnIndex, 1, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                var upArgs = new DataGridViewCellMouseEventArgs(pathColumnIndex, 1, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                mouseDownMethod.Invoke(view, new object[] { downArgs });
                mouseUpMethod.Invoke(view, new object[] { upArgs });

                AssertEqual(1, view.SelectedRows.Count, nameof(UiBehavior_BulkMaterialEditorViewRowSelectorClickSelectsEntireRow));
                AssertTrue(grid.Rows[1].Cells.Cast<DataGridViewCell>().All(cell => cell.Selected), nameof(UiBehavior_BulkMaterialEditorViewRowSelectorClickSelectsEntireRow));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewSelectingBooleanCellDoesNotToggleValue()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialPath = TestFileSupport.CreateBgsm(materialDirectory, "toggle.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\toggle.dds";
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

                MethodInfo mouseDownMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo mouseUpMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, mouseDownMethod != null, nameof(UiBehavior_BulkMaterialEditorViewSelectingBooleanCellDoesNotToggleValue));
                AssertEqual(true, mouseUpMethod != null, nameof(UiBehavior_BulkMaterialEditorViewSelectingBooleanCellDoesNotToggleValue));

                var downArgs = new DataGridViewCellMouseEventArgs(columnIndex, 0, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                var upArgs = new DataGridViewCellMouseEventArgs(columnIndex, 0, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                mouseDownMethod.Invoke(view, new object[] { downArgs });
                mouseUpMethod.Invoke(view, new object[] { upArgs });

                AssertEqual(false, view.HasDirtyRows, nameof(UiBehavior_BulkMaterialEditorViewSelectingBooleanCellDoesNotToggleValue));
                AssertEqual(false, view.IsDirtyRow(session.Rows[0]), nameof(UiBehavior_BulkMaterialEditorViewSelectingBooleanCellDoesNotToggleValue));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiRowSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"row-right-click{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\row-right-click{index}.dds";
                    }))
                    .ToArray();
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                int pathColumnIndex = grid.Columns["__path"].Index;

                MethodInfo selectRowRangeMethod = typeof(BulkMaterialEditorView).GetMethod("SelectRowRange", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo rightClickMethod = typeof(BulkMaterialEditorView).GetMethod("HandleRowSelectorRightClick", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectRowRangeMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiRowSelection));
                AssertEqual(true, rightClickMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiRowSelection));

                selectRowRangeMethod.Invoke(view, new object[] { 0, 1, true });
                rightClickMethod.Invoke(view, new object[] { 1, pathColumnIndex });

                AssertEqual(2, view.SelectedRows.Count, nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiRowSelection));
                AssertTrue(grid.Rows[0].Cells.Cast<DataGridViewCell>().All(cell => cell.Selected), nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiRowSelection));
                AssertTrue(grid.Rows[1].Cells.Cast<DataGridViewCell>().All(cell => cell.Selected), nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiRowSelection));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiFieldSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"field-right-click{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\field-right-click{index}.dds";
                    }))
                    .ToArray();
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;

                MethodInfo selectRectangleMethod = typeof(BulkMaterialEditorView).GetMethod("SelectCellRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo rightClickMethod = typeof(BulkMaterialEditorView).GetMethod("HandleFieldRightClick", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectRectangleMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiFieldSelection));
                AssertEqual(true, rightClickMethod != null, nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiFieldSelection));

                selectRectangleMethod.Invoke(view, new object[] { 0, diffuseColumnIndex, 1, diffuseColumnIndex, true });
                rightClickMethod.Invoke(view, new object[] { 1, diffuseColumnIndex });

                AssertEqual(2, view.SelectedRows.Count, nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiFieldSelection));
                AssertEqual(true, grid.Rows[0].Cells[diffuseColumnIndex].Selected, nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiFieldSelection));
                AssertEqual(true, grid.Rows[1].Cells[diffuseColumnIndex].Selected, nameof(UiBehavior_BulkMaterialEditorViewRightClickPreservesMultiFieldSelection));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewSelectingNumericCellDoesNotEnterEditMode()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialPath = TestFileSupport.CreateBgsm(materialDirectory, "numeric-select.bgsm", material =>
                {
                    material.Version = 2;
                    material.Alpha = 0.5f;
                    material.DiffuseTexture = "textures\\numeric.dds";
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
                int columnIndex = grid.Columns["field::" + ControlNames.Alpha].Index;

                MethodInfo mouseDownMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo mouseUpMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridCellMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, mouseDownMethod != null, nameof(UiBehavior_BulkMaterialEditorViewSelectingNumericCellDoesNotEnterEditMode));
                AssertEqual(true, mouseUpMethod != null, nameof(UiBehavior_BulkMaterialEditorViewSelectingNumericCellDoesNotEnterEditMode));

                var downArgs = new DataGridViewCellMouseEventArgs(columnIndex, 0, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                var upArgs = new DataGridViewCellMouseEventArgs(columnIndex, 0, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                mouseDownMethod.Invoke(view, new object[] { downArgs });
                mouseUpMethod.Invoke(view, new object[] { upArgs });

                AssertEqual(grid.Rows[0].Cells[columnIndex], grid.CurrentCell, nameof(UiBehavior_BulkMaterialEditorViewSelectingNumericCellDoesNotEnterEditMode));
                AssertEqual(false, grid.IsCurrentCellInEditMode, nameof(UiBehavior_BulkMaterialEditorViewSelectingNumericCellDoesNotEnterEditMode));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewShiftArrowExtendsSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialPath = TestFileSupport.CreateBgsm(materialDirectory, "shift-arrow.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\a.dds";
                    material.NormalTexture = "textures\\n.dds";
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
                DataGridViewCell firstCell = grid.Rows[0].Cells["field::" + ControlNames.Diffuse];
                grid.ClearSelection();
                grid.CurrentCell = firstCell;
                firstCell.Selected = true;

                MethodInfo shortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridSelectionShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, shortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewShiftArrowExtendsSelection));

                var args = new KeyEventArgs(Keys.Shift | Keys.Right);
                AssertEqual(true, (bool)shortcutMethod.Invoke(view, new object[] { args }), nameof(UiBehavior_BulkMaterialEditorViewShiftArrowExtendsSelection));
                AssertTrue(grid.CurrentCell.ColumnIndex > firstCell.ColumnIndex, nameof(UiBehavior_BulkMaterialEditorViewShiftArrowExtendsSelection));
            });
        }

        private static void UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialPath = TestFileSupport.CreateBgsm(materialDirectory, "find_enabled.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\enabled.dds";
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { materialPath });

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                FieldInfo workspaceModeField = typeof(Main).GetField("workspaceMode", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo bulkSessionField = typeof(Main).GetField("bulkSession", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo bulkEditorViewField = typeof(Main).GetField("bulkEditorView", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo findMenuField = typeof(Main).GetField("findToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo findReplaceMenuField = typeof(Main).GetField("findReplaceToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo updateStateMethod = typeof(Main).GetMethod("UpdateWorkspaceCommandState", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, workspaceModeField != null, nameof(UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection));
                AssertEqual(true, bulkSessionField != null, nameof(UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection));
                AssertEqual(true, bulkEditorViewField != null, nameof(UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection));
                AssertEqual(true, findMenuField != null, nameof(UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection));
                AssertEqual(true, findReplaceMenuField != null, nameof(UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection));
                AssertEqual(true, updateStateMethod != null, nameof(UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection));

                var bulkView = (BulkMaterialEditorView)bulkEditorViewField.GetValue(form);
                bulkView.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                object bulkMode = workspaceModeField.FieldType.GetField("Bulk").GetValue(null);
                workspaceModeField.SetValue(form, bulkMode);
                bulkSessionField.SetValue(form, session);

                DataGridView grid = GetDescendants(bulkView).OfType<DataGridView>().Single();
                grid.ClearSelection();

                updateStateMethod.Invoke(form, null);

                var findMenu = (ToolStripMenuItem)findMenuField.GetValue(form);
                var findReplaceMenu = (ToolStripMenuItem)findReplaceMenuField.GetValue(form);
                AssertEqual(true, findMenu.Enabled, nameof(UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection));
                AssertEqual(true, findReplaceMenu.Enabled, nameof(UiBehavior_MainBulkFindMenusStayEnabledWithoutSelection));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewReplaceAllMatchesInColumnUpdatesTextCells()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"replace{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\group_{index}.dds";
                    }))
                    .ToArray();
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                int columnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(d => d.Label == ControlNames.Diffuse);

                MethodInfo replaceMethod = typeof(BulkMaterialEditorView).GetMethod("ReplaceAllMatchesInColumn", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, replaceMethod != null, nameof(UiBehavior_BulkMaterialEditorViewReplaceAllMatchesInColumnUpdatesTextCells));

                int replacedCount = (int)replaceMethod.Invoke(view, new object[] { columnIndex, diffuse, "group", "set" });
                AssertEqual(2, replacedCount, nameof(UiBehavior_BulkMaterialEditorViewReplaceAllMatchesInColumnUpdatesTextCells));
                AssertEqual("textures\\set_0.dds", Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewReplaceAllMatchesInColumnUpdatesTextCells));
                AssertEqual("textures\\set_1.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewReplaceAllMatchesInColumnUpdatesTextCells));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewFindCanCreateCustomFilter()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = new[]
                {
                    TestFileSupport.CreateBgsm(materialDirectory, "match0.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = "textures\\needle_a.dds";
                    }),
                    TestFileSupport.CreateBgsm(materialDirectory, "skip.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = "textures\\haystack.dds";
                    }),
                    TestFileSupport.CreateBgsm(materialDirectory, "match1.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = "textures\\needle_b.dds";
                    })
                };
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                MethodInfo filterMethod = typeof(BulkMaterialEditorView).GetMethod("ApplyRequestAsFilter", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, filterMethod != null, nameof(UiBehavior_BulkMaterialEditorViewFindCanCreateCustomFilter));

                filterMethod.Invoke(view, new object[]
                {
                    new BulkFindReplaceRequest
                    {
                        Scope = BulkFindScope.All,
                        ValueType = BulkFindValueType.Text,
                        FindText = "needle"
                    }
                });

                AssertEqual(BulkMaterialRowFilter.CustomFiles, view.ActiveFilter, nameof(UiBehavior_BulkMaterialEditorViewFindCanCreateCustomFilter));
                AssertSequenceEqual(
                    new[] { "match0.bgsm", "match1.bgsm" },
                    view.VisibleRows.Select(row => row.FileName).ToArray(),
                    nameof(UiBehavior_BulkMaterialEditorViewFindCanCreateCustomFilter));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewFindPreviousWrapsAroundLoadedFiles()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = new[]
                {
                    TestFileSupport.CreateBgsm(materialDirectory, "first.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = "textures\\needle_first.dds";
                    }),
                    TestFileSupport.CreateBgsm(materialDirectory, "second.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = "textures\\needle_second.dds";
                    })
                };
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                DataGridViewCell firstCell = grid.Rows[0].Cells["field::" + ControlNames.Diffuse];
                grid.ClearSelection();
                grid.CurrentCell = firstCell;
                firstCell.Selected = true;

                MethodInfo findPreviousMethod = typeof(BulkMaterialEditorView)
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(method => method.Name == "ExecuteFindRequest" && method.GetParameters().Length == 2);
                AssertEqual(true, findPreviousMethod != null, nameof(UiBehavior_BulkMaterialEditorViewFindPreviousWrapsAroundLoadedFiles));

                object backwardDirection = findPreviousMethod.GetParameters()[1].ParameterType.GetField("Backward", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                findPreviousMethod.Invoke(view, new object[]
                {
                    new BulkFindReplaceRequest
                    {
                        Scope = BulkFindScope.All,
                        ValueType = BulkFindValueType.Text,
                        FindText = "needle"
                    },
                    backwardDirection
                });

                AssertEqual("second.bgsm", ((BulkMaterialEditRow)grid.CurrentCell.OwningRow.Tag).FileName, nameof(UiBehavior_BulkMaterialEditorViewFindPreviousWrapsAroundLoadedFiles));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewReplaceUpdatesCurrentMatchOnly()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"replace_one_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\group_{index}.dds";
                    }))
                    .ToArray();
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                DataGridViewCell secondCell = grid.Rows[1].Cells["field::" + ControlNames.Diffuse];
                grid.ClearSelection();
                grid.CurrentCell = secondCell;
                secondCell.Selected = true;

                MethodInfo replaceSingleMethod = typeof(BulkMaterialEditorView).GetMethod("ExecuteReplaceSingleRequest", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, replaceSingleMethod != null, nameof(UiBehavior_BulkMaterialEditorViewReplaceUpdatesCurrentMatchOnly));

                replaceSingleMethod.Invoke(view, new object[]
                {
                    new BulkFindReplaceRequest
                    {
                        Scope = BulkFindScope.ByField,
                        ValueType = BulkFindValueType.Text,
                        FindText = "group",
                        ReplaceText = "single",
                        FieldLabels = new List<string> { ControlNames.Diffuse }
                    }
                });

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(d => d.Label == ControlNames.Diffuse);
                AssertEqual("textures\\group_0.dds", Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewReplaceUpdatesCurrentMatchOnly));
                AssertEqual("textures\\single_1.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewReplaceUpdatesCurrentMatchOnly));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewReplaceBooleanMatchesUpdatesMatchingFiles()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string enabledPath = TestFileSupport.CreateBgsm(materialDirectory, "enabled.bgsm", material =>
                {
                    material.Version = 2;
                    material.TwoSided = true;
                });
                string disabledPath = TestFileSupport.CreateBgsm(materialDirectory, "disabled.bgsm", material =>
                {
                    material.Version = 2;
                    material.TwoSided = false;
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { enabledPath, disabledPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                MethodInfo replaceMethod = typeof(BulkMaterialEditorView).GetMethod("ExecuteReplaceRequest", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, replaceMethod != null, nameof(UiBehavior_BulkMaterialEditorViewReplaceBooleanMatchesUpdatesMatchingFiles));

                replaceMethod.Invoke(view, new object[]
                {
                    new BulkFindReplaceRequest
                    {
                        Scope = BulkFindScope.ByField,
                        ValueType = BulkFindValueType.Boolean,
                        FindBooleanValue = true,
                        ReplaceBooleanValue = false,
                        FieldLabels = new List<string> { ControlNames.TwoSided }
                    }
                });

                MaterialFieldDescriptor twoSided = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.TwoSided);
                AssertEqual(false, Convert.ToBoolean(session.Rows[0].GetCell(twoSided).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewReplaceBooleanMatchesUpdatesMatchingFiles));
                AssertEqual(false, Convert.ToBoolean(session.Rows[1].GetCell(twoSided).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewReplaceBooleanMatchesUpdatesMatchingFiles));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewReplaceNumericMatchesUpdatesMatchingFiles()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string firstPath = TestFileSupport.CreateBgsm(materialDirectory, "alpha_match.bgsm", material =>
                {
                    material.Version = 2;
                    material.Alpha = 0.5f;
                });
                string secondPath = TestFileSupport.CreateBgsm(materialDirectory, "alpha_skip.bgsm", material =>
                {
                    material.Version = 2;
                    material.Alpha = 0.75f;
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { firstPath, secondPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                MethodInfo replaceMethod = typeof(BulkMaterialEditorView).GetMethod("ExecuteReplaceRequest", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, replaceMethod != null, nameof(UiBehavior_BulkMaterialEditorViewReplaceNumericMatchesUpdatesMatchingFiles));

                replaceMethod.Invoke(view, new object[]
                {
                    new BulkFindReplaceRequest
                    {
                        Scope = BulkFindScope.ByField,
                        ValueType = BulkFindValueType.Number,
                        FindText = "0.5",
                        ReplaceText = "1.25",
                        FieldLabels = new List<string> { ControlNames.Alpha }
                    }
                });

                MaterialFieldDescriptor alpha = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Alpha);
                AssertNearlyEqual(1.25f, Convert.ToSingle(session.Rows[0].GetCell(alpha).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewReplaceNumericMatchesUpdatesMatchingFiles));
                AssertNearlyEqual(0.75f, Convert.ToSingle(session.Rows[1].GetCell(alpha).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewReplaceNumericMatchesUpdatesMatchingFiles));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCopyRowsWritesSelectedDescriptorMatrixToClipboard()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string firstPath = TestFileSupport.CreateBgsm(materialDirectory, "copy_rows_0.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\copy_row_0.dds";
                    material.NormalTexture = "textures\\copy_row_n0.dds";
                });
                string secondPath = TestFileSupport.CreateBgsm(materialDirectory, "copy_rows_1.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\copy_row_1.dds";
                    material.NormalTexture = "textures\\copy_row_n1.dds";
                });

                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { firstPath, secondPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse, ControlNames.Normal);

                MethodInfo selectRowRange = typeof(BulkMaterialEditorView).GetMethod("SelectRowRange", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo copyRowsMethod = typeof(BulkMaterialEditorView).GetMethod("CopySelectedRowsToClipboard", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectRowRange != null, nameof(UiBehavior_BulkMaterialEditorViewCopyRowsWritesSelectedDescriptorMatrixToClipboard));
                AssertEqual(true, copyRowsMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCopyRowsWritesSelectedDescriptorMatrixToClipboard));

                selectRowRange.Invoke(view, new object[] { 0, 1, true });

                WithClipboardPreserved(() =>
                {
                    copyRowsMethod.Invoke(view, null);
                    AssertEqual(
                        "textures\\copy_row_0.dds\ttextures\\copy_row_n0.dds" + Environment.NewLine
                        + "textures\\copy_row_1.dds\ttextures\\copy_row_n1.dds",
                        Clipboard.GetText(),
                        nameof(UiBehavior_BulkMaterialEditorViewCopyRowsWritesSelectedDescriptorMatrixToClipboard));
                });
            });
        }

        private static void UiConstruction_MainBulkEditMenuExposesRowClipboardShortcuts()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", themeDirectory =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                FieldInfo cutRowsField = typeof(Main).GetField("editCutRowsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo copyRowsField = typeof(Main).GetField("editCopyRowsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo pasteRowsField = typeof(Main).GetField("editPasteRowsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, cutRowsField != null, nameof(UiConstruction_MainBulkEditMenuExposesRowClipboardShortcuts));
                AssertEqual(true, copyRowsField != null, nameof(UiConstruction_MainBulkEditMenuExposesRowClipboardShortcuts));
                AssertEqual(true, pasteRowsField != null, nameof(UiConstruction_MainBulkEditMenuExposesRowClipboardShortcuts));

                var cutRowsMenu = (ToolStripMenuItem)cutRowsField.GetValue(form);
                var copyRowsMenu = (ToolStripMenuItem)copyRowsField.GetValue(form);
                var pasteRowsMenu = (ToolStripMenuItem)pasteRowsField.GetValue(form);

                AssertEqual(Keys.Control | Keys.Shift | Keys.X, cutRowsMenu.ShortcutKeys, nameof(UiConstruction_MainBulkEditMenuExposesRowClipboardShortcuts));
                AssertEqual(Keys.Control | Keys.Shift | Keys.C, copyRowsMenu.ShortcutKeys, nameof(UiConstruction_MainBulkEditMenuExposesRowClipboardShortcuts));
                AssertEqual(Keys.Control | Keys.Shift | Keys.V, pasteRowsMenu.ShortcutKeys, nameof(UiConstruction_MainBulkEditMenuExposesRowClipboardShortcuts));
            });
        }

        private static void UiConstruction_MainMenuBindingsMatchRequestedShortcuts()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", themeDirectory =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                FieldInfo fileMenuField = typeof(Main).GetField("fileToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo editMenuField = typeof(Main).GetField("editToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo closeField = typeof(Main).GetField("closeToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo saveAsField = typeof(Main).GetField("saveAsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo openFolderField = typeof(Main).GetField("openFolderToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo addFilesField = typeof(Main).GetField("addFilesToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo addFolderField = typeof(Main).GetField("addFolderToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo removeSelectedField = typeof(Main).GetField("removeSelectedFilesToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo revealField = typeof(Main).GetField("editRevealInExplorerToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo reloadField = typeof(Main).GetField("editReloadFromDiskToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo browseBackupsField = typeof(Main).GetField("browseBackupsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo sendToSingleField = typeof(Main).GetField("editSendToSingleEditorToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo cutField = typeof(Main).GetField("editCutToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo clearField = typeof(Main).GetField("editClearToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo selectAllField = typeof(Main).GetField("editSelectAllToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo selectRowField = typeof(Main).GetField("editSelectRowToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo selectPageAboveField = typeof(Main).GetField("editSelectPageAboveToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo selectPageBelowField = typeof(Main).GetField("editSelectPageBelowToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo selectAllAboveField = typeof(Main).GetField("editSelectAllAboveToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo selectAllBelowField = typeof(Main).GetField("editSelectAllBelowToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo selectDirtyField = typeof(Main).GetField("editSelectDirtyRowsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo selectErrorField = typeof(Main).GetField("editSelectErrorRowsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo findReplaceField = typeof(Main).GetField("findReplaceToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo generateField = typeof(Main).GetField("generateVariationsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo overwriteField = typeof(Main).GetField("overwriteFilesByFieldToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);

                AssertEqual(true, fileMenuField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, editMenuField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, closeField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, saveAsField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, openFolderField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, addFilesField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, addFolderField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, removeSelectedField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, revealField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, reloadField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, browseBackupsField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, sendToSingleField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, cutField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, clearField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, selectAllField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, selectRowField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, selectPageAboveField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, selectPageBelowField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, selectAllAboveField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, selectAllBelowField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, selectDirtyField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, selectErrorField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, findReplaceField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, generateField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(true, overwriteField != null, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));

                var fileMenu = (ToolStripMenuItem)fileMenuField.GetValue(form);
                var editMenu = (ToolStripMenuItem)editMenuField.GetValue(form);
                var closeMenu = (ToolStripMenuItem)closeField.GetValue(form);
                var saveAsMenu = (ToolStripMenuItem)saveAsField.GetValue(form);
                var openFolderMenu = (ToolStripMenuItem)openFolderField.GetValue(form);
                var addFilesMenu = (ToolStripMenuItem)addFilesField.GetValue(form);
                var addFolderMenu = (ToolStripMenuItem)addFolderField.GetValue(form);
                var removeSelectedMenu = (ToolStripMenuItem)removeSelectedField.GetValue(form);
                var revealMenu = (ToolStripMenuItem)revealField.GetValue(form);
                var reloadMenu = (ToolStripMenuItem)reloadField.GetValue(form);
                var browseBackupsMenu = (ToolStripMenuItem)browseBackupsField.GetValue(form);
                var sendToSingleMenu = (ToolStripMenuItem)sendToSingleField.GetValue(form);
                var cutMenu = (ToolStripMenuItem)cutField.GetValue(form);
                var clearMenu = (ToolStripMenuItem)clearField.GetValue(form);
                var selectAllMenu = (ToolStripMenuItem)selectAllField.GetValue(form);
                var selectRowMenu = (ToolStripMenuItem)selectRowField.GetValue(form);
                var selectPageAboveMenu = (ToolStripMenuItem)selectPageAboveField.GetValue(form);
                var selectPageBelowMenu = (ToolStripMenuItem)selectPageBelowField.GetValue(form);
                var selectAllAboveMenu = (ToolStripMenuItem)selectAllAboveField.GetValue(form);
                var selectAllBelowMenu = (ToolStripMenuItem)selectAllBelowField.GetValue(form);
                var selectDirtyMenu = (ToolStripMenuItem)selectDirtyField.GetValue(form);
                var selectErrorMenu = (ToolStripMenuItem)selectErrorField.GetValue(form);
                var findReplaceMenu = (ToolStripMenuItem)findReplaceField.GetValue(form);
                var generateMenu = (ToolStripMenuItem)generateField.GetValue(form);
                var overwriteMenu = (ToolStripMenuItem)overwriteField.GetValue(form);

                AssertEqual(Keys.Control | Keys.Shift | Keys.O, openFolderMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Oemplus, addFilesMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Shift | Keys.Oemplus, addFolderMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual("Ctrl++", addFilesMenu.ShortcutKeyDisplayString, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual("Ctrl+Shift++", addFolderMenu.ShortcutKeyDisplayString, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Delete, removeSelectedMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.E, revealMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.R, reloadMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.B, browseBackupsMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.F4, closeMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.W, sendToSingleMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.X, cutMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Delete, clearMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.A, selectAllMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Enter, selectRowMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual("Shift+PgUp", selectPageAboveMenu.ShortcutKeyDisplayString, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual("Shift+PgDn", selectPageBelowMenu.ShortcutKeyDisplayString, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Shift | Keys.PageUp, selectAllAboveMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Shift | Keys.PageDown, selectAllBelowMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Q, selectDirtyMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Shift | Keys.Q, selectErrorMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Shift | Keys.F, findReplaceMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.G, generateMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(Keys.Control | Keys.Shift | Keys.G, overwriteMenu.ShortcutKeys, nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));

                AssertEqual(fileMenu.DropDownItems.IndexOf(saveAsMenu) + 1, fileMenu.DropDownItems.IndexOf(addFilesMenu), nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(fileMenu.DropDownItems.IndexOf(removeSelectedMenu) + 1, fileMenu.DropDownItems.IndexOf(revealMenu), nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(fileMenu.DropDownItems.IndexOf(revealMenu) + 1, fileMenu.DropDownItems.IndexOf(reloadMenu), nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(fileMenu.DropDownItems.IndexOf(reloadMenu) + 1, fileMenu.DropDownItems.IndexOf(closeMenu), nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(-1, editMenu.DropDownItems.IndexOf(revealMenu), nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
                AssertEqual(-1, editMenu.DropDownItems.IndexOf(reloadMenu), nameof(UiConstruction_MainMenuBindingsMatchRequestedShortcuts));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCtrlShiftCUsesRowClipboard()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"shortcut_copy_rows_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\shortcut_copy_{index}.dds";
                        material.NormalTexture = $"textures\\shortcut_copy_n{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse, ControlNames.Normal);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                MethodInfo shortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridClipboardShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, shortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftCUsesRowClipboard));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[diffuseColumnIndex];
                    grid.Rows[0].Cells[diffuseColumnIndex].Selected = true;
                    grid.Rows[1].Cells[diffuseColumnIndex].Selected = true;
                });

                WithClipboardPreserved(() =>
                {
                    var keyArgs = new KeyEventArgs(Keys.Control | Keys.Shift | Keys.C);
                    bool handled = (bool)shortcutMethod.Invoke(view, new object[] { keyArgs });

                    AssertEqual(true, handled, nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftCUsesRowClipboard));
                    AssertEqual(
                        "textures\\shortcut_copy_0.dds\ttextures\\shortcut_copy_n0.dds" + Environment.NewLine
                        + "textures\\shortcut_copy_1.dds\ttextures\\shortcut_copy_n1.dds",
                        Clipboard.GetText(),
                        nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftCUsesRowClipboard));
                });
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCtrlShiftVPastesRows()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"shortcut_paste_rows_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\shortcut_original_{index}.dds";
                        material.NormalTexture = $"textures\\shortcut_original_n{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse, ControlNames.Normal);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                MethodInfo shortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridClipboardShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, shortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftVPastesRows));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[diffuseColumnIndex];
                    grid.Rows[0].Cells[diffuseColumnIndex].Selected = true;
                    grid.Rows[1].Cells[diffuseColumnIndex].Selected = true;
                });

                WithClipboardPreserved(() =>
                {
                    Clipboard.SetText(
                        "textures\\shortcut_paste_0.dds\ttextures\\shortcut_paste_n0.dds" + Environment.NewLine
                        + "textures\\shortcut_paste_1.dds\ttextures\\shortcut_paste_n1.dds");

                    var keyArgs = new KeyEventArgs(Keys.Control | Keys.Shift | Keys.V);
                    bool handled = (bool)shortcutMethod.Invoke(view, new object[] { keyArgs });

                    AssertEqual(true, handled, nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftVPastesRows));
                });

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                MaterialFieldDescriptor normal = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Normal);
                AssertEqual("textures\\shortcut_paste_0.dds", Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftVPastesRows));
                AssertEqual("textures\\shortcut_paste_n0.dds", Convert.ToString(session.Rows[0].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftVPastesRows));
                AssertEqual("textures\\shortcut_paste_1.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftVPastesRows));
                AssertEqual("textures\\shortcut_paste_n1.dds", Convert.ToString(session.Rows[1].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftVPastesRows));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"shortcut_cut_rows_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\shortcut_cut_{index}.dds";
                        material.NormalTexture = $"textures\\shortcut_cut_n{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse, ControlNames.Normal);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                MethodInfo shortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridClipboardShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, shortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[diffuseColumnIndex];
                    grid.Rows[0].Cells[diffuseColumnIndex].Selected = true;
                    grid.Rows[1].Cells[diffuseColumnIndex].Selected = true;
                });

                WithClipboardPreserved(() =>
                {
                    var keyArgs = new KeyEventArgs(Keys.Control | Keys.Shift | Keys.X);
                    bool handled = (bool)shortcutMethod.Invoke(view, new object[] { keyArgs });

                    AssertEqual(true, handled, nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows));
                    AssertEqual(
                        "textures\\shortcut_cut_0.dds\ttextures\\shortcut_cut_n0.dds" + Environment.NewLine
                        + "textures\\shortcut_cut_1.dds\ttextures\\shortcut_cut_n1.dds",
                        Clipboard.GetText(),
                        nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows));
                });

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                MaterialFieldDescriptor normal = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Normal);
                AssertEqual(string.Empty, Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows));
                AssertEqual(string.Empty, Convert.ToString(session.Rows[0].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows));
                AssertEqual(string.Empty, Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows));
                AssertEqual(string.Empty, Convert.ToString(session.Rows[1].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows));
                AssertEqual(true, view.CanUndo, nameof(UiBehavior_BulkMaterialEditorViewCtrlShiftXCutsRows));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCtrlXCutsFields()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"shortcut_cut_fields_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\shortcut_field_{index}.dds";
                        material.NormalTexture = $"textures\\shortcut_field_n{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse, ControlNames.Normal);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                MethodInfo shortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridClipboardShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, shortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCtrlXCutsFields));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[diffuseColumnIndex];
                    grid.Rows[0].Cells[diffuseColumnIndex].Selected = true;
                    grid.Rows[1].Cells[diffuseColumnIndex].Selected = true;
                });

                WithClipboardPreserved(() =>
                {
                    var keyArgs = new KeyEventArgs(Keys.Control | Keys.X);
                    bool handled = (bool)shortcutMethod.Invoke(view, new object[] { keyArgs });

                    AssertEqual(true, handled, nameof(UiBehavior_BulkMaterialEditorViewCtrlXCutsFields));
                    AssertEqual(
                        "textures\\shortcut_field_0.dds" + Environment.NewLine
                        + "textures\\shortcut_field_1.dds",
                        Clipboard.GetText(),
                        nameof(UiBehavior_BulkMaterialEditorViewCtrlXCutsFields));
                });

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                MaterialFieldDescriptor normal = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Normal);
                AssertEqual(string.Empty, Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlXCutsFields));
                AssertEqual(string.Empty, Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlXCutsFields));
                AssertEqual("textures\\shortcut_field_n0.dds", Convert.ToString(session.Rows[0].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlXCutsFields));
                AssertEqual("textures\\shortcut_field_n1.dds", Convert.ToString(session.Rows[1].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewCtrlXCutsFields));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewDeleteClearsFields()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"shortcut_clear_fields_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\shortcut_clear_{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                MethodInfo shortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridEditShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, shortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewDeleteClearsFields));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[diffuseColumnIndex];
                    grid.Rows[0].Cells[diffuseColumnIndex].Selected = true;
                    grid.Rows[1].Cells[diffuseColumnIndex].Selected = true;
                });

                var keyArgs = new KeyEventArgs(Keys.Delete);
                bool handled = (bool)shortcutMethod.Invoke(view, new object[] { keyArgs });

                AssertEqual(true, handled, nameof(UiBehavior_BulkMaterialEditorViewDeleteClearsFields));

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                AssertEqual(string.Empty, Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewDeleteClearsFields));
                AssertEqual(string.Empty, Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewDeleteClearsFields));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string path = TestFileSupport.CreateBgsm(materialDirectory, "shortcut_clear_row_undo.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\shortcut_clear_row.dds";
                    material.EnvironmentMapping = true;
                    material.EnvironmentMappingMaskScale = 1.5f;
                });

                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.EnvironmentMapping, ControlNames.EnvironmentMaskScale);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int environmentColumnIndex = grid.Columns["field::" + ControlNames.EnvironmentMapping].Index;
                MethodInfo selectCurrentRowMethod = typeof(BulkMaterialEditorView).GetMethod("SelectCurrentRow", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectCurrentRowMethod != null, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[environmentColumnIndex];
                    grid.Rows[0].Cells[environmentColumnIndex].Selected = true;
                });
                selectCurrentRowMethod.Invoke(view, null);

                view.ExecuteClearSelection();

                MaterialFieldDescriptor environmentMapping = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.EnvironmentMapping);
                MaterialFieldDescriptor environmentMaskScale = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.EnvironmentMaskScale);
                AssertEqual(false, Convert.ToBoolean(session.Rows[0].GetCell(environmentMapping).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertNearlyEqual(0f, Convert.ToSingle(session.Rows[0].GetCell(environmentMaskScale).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertEqual(false, session.Rows[0].HasValidationErrors, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertEqual(false, session.HasValidationErrors, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));

                AssertEqual(true, view.ExecuteUndo(), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertEqual(true, Convert.ToBoolean(session.Rows[0].GetCell(environmentMapping).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertNearlyEqual(1.5f, Convert.ToSingle(session.Rows[0].GetCell(environmentMaskScale).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertEqual(false, session.Rows[0].HasValidationErrors, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertEqual(false, session.HasValidationErrors, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertEqual(string.Empty, GetBulkValidationLabelText(view), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));

                IReadOnlyList<FieldCopyResult> results = view.SaveSelectedChanges();
                AssertEqual(1, results.Count, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
                AssertEqual(FieldCopyStatus.Skipped, results[0].Status, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterRowClearDoesNotLeaveValidationErrors));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string path = TestFileSupport.CreateBgem(materialDirectory, "shortcut_clear_effect_row_undo.bgem", material =>
                {
                    material.Version = 10;
                    material.BaseTexture = "textures\\shortcut_clear_effect_row.dds";
                    material.EnvironmentMapping = true;
                    material.EnvironmentMappingMaskScale = 2.5f;
                });

                var session = BulkMaterialEditSession.Create(MaterialType.Effect, new[] { path });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.EnvMapping, ControlNames.EnvMappingMaskScale);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int envMappingColumnIndex = grid.Columns["field::" + ControlNames.EnvMapping].Index;
                MethodInfo selectCurrentRowMethod = typeof(BulkMaterialEditorView).GetMethod("SelectCurrentRow", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectCurrentRowMethod != null, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[envMappingColumnIndex];
                    grid.Rows[0].Cells[envMappingColumnIndex].Selected = true;
                });
                selectCurrentRowMethod.Invoke(view, null);

                view.ExecuteClearSelection();

                MaterialFieldDescriptor envMapping = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.EnvMapping);
                MaterialFieldDescriptor envMappingMaskScale = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.EnvMappingMaskScale);
                AssertEqual(false, Convert.ToBoolean(session.Rows[0].GetCell(envMapping).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertNearlyEqual(0f, Convert.ToSingle(session.Rows[0].GetCell(envMappingMaskScale).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertEqual(false, session.Rows[0].HasValidationErrors, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertEqual(false, session.HasValidationErrors, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));

                AssertEqual(true, view.ExecuteUndo(), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertEqual(true, Convert.ToBoolean(session.Rows[0].GetCell(envMapping).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertNearlyEqual(2.5f, Convert.ToSingle(session.Rows[0].GetCell(envMappingMaskScale).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertEqual(false, session.Rows[0].HasValidationErrors, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertEqual(false, session.HasValidationErrors, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertEqual(string.Empty, GetBulkValidationLabelText(view), nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));

                IReadOnlyList<FieldCopyResult> results = view.SaveSelectedChanges();
                AssertEqual(1, results.Count, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
                AssertEqual(FieldCopyStatus.Skipped, results[0].Status, nameof(UiBehavior_BulkMaterialEditorViewUndoAfterEffectRowClearDoesNotLeaveValidationErrors));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCtrlEnterSelectsCurrentRow()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"shortcut_select_row_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\shortcut_select_{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                MethodInfo shortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridEditShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, shortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCtrlEnterSelectsCurrentRow));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[1].Cells[diffuseColumnIndex];
                    grid.Rows[1].Cells[diffuseColumnIndex].Selected = true;
                });

                var keyArgs = new KeyEventArgs(Keys.Control | Keys.Enter);
                bool handled = (bool)shortcutMethod.Invoke(view, new object[] { keyArgs });

                AssertEqual(true, handled, nameof(UiBehavior_BulkMaterialEditorViewCtrlEnterSelectsCurrentRow));
                AssertEqual(1, view.SelectedRows.Count, nameof(UiBehavior_BulkMaterialEditorViewCtrlEnterSelectsCurrentRow));
                AssertEqual(paths[1], view.SelectedRows[0].FilePath, nameof(UiBehavior_BulkMaterialEditorViewCtrlEnterSelectsCurrentRow));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string validPath = TestFileSupport.CreateBgsm(materialDirectory, "shortcut_select_dirty.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\shortcut_select_dirty.dds";
                });
                string missingPath = Path.Combine(materialDirectory, "shortcut_select_missing.bgsm");
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { validPath, missingPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                MethodInfo editShortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridEditShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, editShortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[diffuseColumnIndex];
                    grid.Rows[0].Cells[diffuseColumnIndex].Selected = true;
                });

                bool deleteHandled = (bool)editShortcutMethod.Invoke(view, new object[] { new KeyEventArgs(Keys.Delete) });
                AssertEqual(true, deleteHandled, nameof(UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows));

                bool dirtyHandled = (bool)editShortcutMethod.Invoke(view, new object[] { new KeyEventArgs(Keys.Control | Keys.Q) });
                AssertEqual(true, dirtyHandled, nameof(UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows));
                AssertEqual(1, view.SelectedRows.Count, nameof(UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows));
                AssertEqual(validPath, view.SelectedRows[0].FilePath, nameof(UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows));

                bool errorHandled = (bool)editShortcutMethod.Invoke(view, new object[] { new KeyEventArgs(Keys.Control | Keys.Shift | Keys.Q) });
                AssertEqual(true, errorHandled, nameof(UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows));
                AssertEqual(1, view.SelectedRows.Count, nameof(UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows));
                AssertEqual(missingPath, view.SelectedRows[0].FilePath, nameof(UiBehavior_BulkMaterialEditorViewCtrlQSelectsDirtyRowsAndCtrlShiftQSelectsErroredRows));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCtrlTCyclesFilters()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialsRoot = Path.Combine(materialDirectory, "Materials");
                Directory.CreateDirectory(Path.Combine(materialsRoot, "SetA"));
                Directory.CreateDirectory(Path.Combine(materialsRoot, "SetB"));
                Directory.CreateDirectory(Path.Combine(materialsRoot, "SetC"));

                string duplicateAPath = TestFileSupport.CreateBgsm(Path.Combine(materialsRoot, "SetA"), "shared.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\cycle_shared_a.dds";
                });
                string duplicateBPath = TestFileSupport.CreateBgsm(Path.Combine(materialsRoot, "SetB"), "shared.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\cycle_shared_b.dds";
                });
                string uniquePath = TestFileSupport.CreateBgsm(Path.Combine(materialsRoot, "SetC"), "unique.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\cycle_unique.dds";
                });

                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { duplicateAPath, duplicateBPath, uniquePath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                int uniqueRowIndex = grid.Rows
                    .Cast<DataGridViewRow>()
                    .Single(row => string.Equals(Convert.ToString(row.Cells["__path"].Value), "SetC\\unique.bgsm", StringComparison.OrdinalIgnoreCase))
                    .Index;
                MethodInfo editShortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridEditShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, editShortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFilters));

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[uniqueRowIndex].Cells[diffuseColumnIndex];
                    grid.Rows[uniqueRowIndex].Cells[diffuseColumnIndex].Selected = true;
                });

                bool forwardHandled = (bool)editShortcutMethod.Invoke(view, new object[] { new KeyEventArgs(Keys.Control | Keys.T) });
                AssertEqual(true, forwardHandled, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFilters));
                AssertEqual(BulkMaterialRowFilter.UniqueFilesOnly, view.ActiveFilter, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFilters));

                bool backwardHandled = (bool)editShortcutMethod.Invoke(view, new object[] { new KeyEventArgs(Keys.Control | Keys.Shift | Keys.T) });
                AssertEqual(true, backwardHandled, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFilters));
                AssertEqual(BulkMaterialRowFilter.AllFiles, view.ActiveFilter, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFilters));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string firstPath = TestFileSupport.CreateBgsm(materialDirectory, "first.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\first.dds";
                });
                string secondPath = TestFileSupport.CreateBgsm(materialDirectory, "second.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\second.dds";
                });

                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { firstPath, secondPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                MethodInfo editShortcutMethod = typeof(BulkMaterialEditorView).GetMethod("HandleGridEditShortcut", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, editShortcutMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows));

                view.SetRowFilter(BulkMaterialRowFilter.DirtyFiles);
                AssertEqual(0, grid.Rows.Count, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows));
                AssertEqual(null, grid.CurrentCell, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows));

                bool forwardHandled = (bool)editShortcutMethod.Invoke(view, new object[] { new KeyEventArgs(Keys.Control | Keys.T) });
                AssertEqual(true, forwardHandled, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows));
                AssertEqual(BulkMaterialRowFilter.ErroredFiles, view.ActiveFilter, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows));

                bool backwardHandled = (bool)editShortcutMethod.Invoke(view, new object[] { new KeyEventArgs(Keys.Control | Keys.Shift | Keys.T) });
                AssertEqual(true, backwardHandled, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows));
                AssertEqual(BulkMaterialRowFilter.DirtyFiles, view.ActiveFilter, nameof(UiBehavior_BulkMaterialEditorViewCtrlTCyclesFiltersWhenCurrentFilterShowsNoRows));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 3)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"paste_rows_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\original_row_{index}.dds";
                        material.NormalTexture = $"textures\\original_row_n{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse, ControlNames.Normal);

                MethodInfo selectRowRange = typeof(BulkMaterialEditorView).GetMethod("SelectRowRange", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo pasteRowsMethod = typeof(BulkMaterialEditorView).GetMethod("PasteRowsFromClipboard", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectRowRange != null, nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));
                AssertEqual(true, pasteRowsMethod != null, nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));

                selectRowRange.Invoke(view, new object[] { 0, 2, true });

                WithClipboardPreserved(() =>
                {
                    Clipboard.SetText(
                        "textures\\paste_row_0.dds\ttextures\\paste_row_n0.dds" + Environment.NewLine
                        + "textures\\paste_row_1.dds\ttextures\\paste_row_n1.dds");

                    pasteRowsMethod.Invoke(view, null);
                });

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                MaterialFieldDescriptor normal = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Normal);

                AssertEqual("textures\\paste_row_0.dds", Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));
                AssertEqual("textures\\paste_row_n0.dds", Convert.ToString(session.Rows[0].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));
                AssertEqual("textures\\paste_row_1.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));
                AssertEqual("textures\\paste_row_n1.dds", Convert.ToString(session.Rows[1].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));
                AssertEqual("textures\\paste_row_1.dds", Convert.ToString(session.Rows[2].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));
                AssertEqual("textures\\paste_row_n1.dds", Convert.ToString(session.Rows[2].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));
                AssertEqual(true, view.CanUndo, nameof(UiBehavior_BulkMaterialEditorViewPasteRowsRepeatsLastClipboardRowAcrossSelection));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewCopyFieldsPreservesSparseRectangularSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"copy_fields_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\field_{index}.dds";
                        material.NormalTexture = $"textures\\field_n{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse, ControlNames.Normal);

                var grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                int normalColumnIndex = grid.Columns["field::" + ControlNames.Normal].Index;

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[diffuseColumnIndex];
                    grid.Rows[0].Cells[diffuseColumnIndex].Selected = true;
                    grid.Rows[1].Cells[normalColumnIndex].Selected = true;
                });

                MethodInfo copyFieldsMethod = typeof(BulkMaterialEditorView).GetMethod("CopySelectedFieldsToClipboard", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, copyFieldsMethod != null, nameof(UiBehavior_BulkMaterialEditorViewCopyFieldsPreservesSparseRectangularSelection));

                WithClipboardPreserved(() =>
                {
                    copyFieldsMethod.Invoke(view, null);
                    AssertEqual(
                        "textures\\field_0.dds\t" + Environment.NewLine
                        + "\ttextures\\field_n1.dds",
                        Clipboard.GetText(),
                        nameof(UiBehavior_BulkMaterialEditorViewCopyFieldsPreservesSparseRectangularSelection));
                });
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewPasteFieldsSingleValueFillsSparseSelection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"paste_fields_{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\paste_field_{index}.dds";
                        material.NormalTexture = $"textures\\paste_field_n{index}.dds";
                    }))
                    .ToArray();

                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                ConfigureBulkEditorVisibleFields(view, ControlNames.Diffuse, ControlNames.Normal);

                var grid = GetDescendants(host).OfType<DataGridView>().Single();
                int diffuseColumnIndex = grid.Columns["field::" + ControlNames.Diffuse].Index;
                int normalColumnIndex = grid.Columns["field::" + ControlNames.Normal].Index;

                PerformGridSelection(grid, () =>
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid.Rows[0].Cells[diffuseColumnIndex];
                    grid.Rows[0].Cells[diffuseColumnIndex].Selected = true;
                    grid.Rows[1].Cells[normalColumnIndex].Selected = true;
                });

                MethodInfo pasteFieldsMethod = typeof(BulkMaterialEditorView).GetMethod("PasteFieldsFromClipboard", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, pasteFieldsMethod != null, nameof(UiBehavior_BulkMaterialEditorViewPasteFieldsSingleValueFillsSparseSelection));

                WithClipboardPreserved(() =>
                {
                    Clipboard.SetText("textures\\filled.dds");
                    pasteFieldsMethod.Invoke(view, null);
                });

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                MaterialFieldDescriptor normal = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Normal);

                AssertEqual("textures\\filled.dds", Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteFieldsSingleValueFillsSparseSelection));
                AssertEqual("textures\\paste_field_n0.dds", Convert.ToString(session.Rows[0].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteFieldsSingleValueFillsSparseSelection));
                AssertEqual("textures\\paste_field_1.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteFieldsSingleValueFillsSparseSelection));
                AssertEqual("textures\\filled.dds", Convert.ToString(session.Rows[1].GetCell(normal).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewPasteFieldsSingleValueFillsSparseSelection));
                AssertEqual(true, view.CanUndo, nameof(UiBehavior_BulkMaterialEditorViewPasteFieldsSingleValueFillsSparseSelection));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewFillUpCopiesBottomValue()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = Enumerable.Range(0, 2)
                    .Select(index => TestFileSupport.CreateBgsm(materialDirectory, $"clear{index}.bgsm", material =>
                    {
                        material.Version = 2;
                        material.DiffuseTexture = $"textures\\clear{index}.dds";
                    }))
                    .ToArray();
                var session = BulkMaterialEditSession.Create(MaterialType.Material, paths);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                DataGridViewCell firstCell = grid.Rows[0].Cells["field::" + ControlNames.Diffuse];
                MethodInfo selectRectangle = typeof(BulkMaterialEditorView).GetMethod("SelectCellRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectRectangle != null, nameof(UiBehavior_BulkMaterialEditorViewFillUpCopiesBottomValue));
                selectRectangle.Invoke(view, new object[] { 0, firstCell.ColumnIndex, 1, firstCell.ColumnIndex, true });

                MethodInfo fillUpMethod = typeof(BulkMaterialEditorView).GetMethod("FillUpSelectedFields", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, fillUpMethod != null, nameof(UiBehavior_BulkMaterialEditorViewFillUpCopiesBottomValue));
                fillUpMethod.Invoke(view, null);

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(d => d.Label == ControlNames.Diffuse);
                AssertEqual("textures\\clear1.dds", Convert.ToString(session.Rows[0].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewFillUpCopiesBottomValue));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewFillDownCopiesTopValue()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string firstPath = TestFileSupport.CreateBgsm(materialDirectory, "fill0.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\source.dds";
                });
                string secondPath = TestFileSupport.CreateBgsm(materialDirectory, "fill1.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\target.dds";
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { firstPath, secondPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                var grid = GetDescendants(host)
                    .OfType<DataGridView>()
                    .Single();
                DataGridViewCell firstCell = grid.Rows[0].Cells["field::" + ControlNames.Diffuse];
                MethodInfo selectRectangle = typeof(BulkMaterialEditorView).GetMethod("SelectCellRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectRectangle != null, nameof(UiBehavior_BulkMaterialEditorViewFillDownCopiesTopValue));
                selectRectangle.Invoke(view, new object[] { 0, firstCell.ColumnIndex, 1, firstCell.ColumnIndex, true });

                MethodInfo fillDownMethod = typeof(BulkMaterialEditorView).GetMethod("FillDownSelectedFields", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, fillDownMethod != null, nameof(UiBehavior_BulkMaterialEditorViewFillDownCopiesTopValue));
                fillDownMethod.Invoke(view, null);

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(d => d.Label == ControlNames.Diffuse);
                AssertEqual("textures\\source.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewFillDownCopiesTopValue));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewDefaultsToAlphabeticalAscendingProjection()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialsRoot = Path.Combine(materialDirectory, "Materials");
                string betaDirectory = Path.Combine(materialsRoot, "Beta");
                string alphaDirectory = Path.Combine(materialsRoot, "Alpha");
                Directory.CreateDirectory(betaDirectory);
                Directory.CreateDirectory(alphaDirectory);

                string betaPath = TestFileSupport.CreateBgsm(betaDirectory, "beta.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\beta.dds";
                });
                string alphaPath = TestFileSupport.CreateBgsm(alphaDirectory, "alpha.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\alpha.dds";
                });

                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { betaPath, alphaPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                AssertSequenceEqual(
                    new[] { "Alpha\\alpha.bgsm", "Beta\\beta.bgsm" },
                    GetGridPathValues(grid),
                    nameof(UiBehavior_BulkMaterialEditorViewDefaultsToAlphabeticalAscendingProjection));
                AssertSequenceEqual(
                    new[] { "Beta\\beta.bgsm", "Alpha\\alpha.bgsm" },
                    session.Rows.Select(row => row.DisplayPath).ToArray(),
                    nameof(UiBehavior_BulkMaterialEditorViewDefaultsToAlphabeticalAscendingProjection));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewSortByAdditionRestoresSessionOrder()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialsRoot = Path.Combine(materialDirectory, "Materials");
                string firstDirectory = Path.Combine(materialsRoot, "First");
                string secondDirectory = Path.Combine(materialsRoot, "Second");
                Directory.CreateDirectory(firstDirectory);
                Directory.CreateDirectory(secondDirectory);

                string firstPath = TestFileSupport.CreateBgsm(secondDirectory, "b.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\b.dds";
                });
                string secondPath = TestFileSupport.CreateBgsm(firstDirectory, "a.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\a.dds";
                });

                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { firstPath, secondPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                view.SetSortKey(BulkMaterialSortKey.ByAddition);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                AssertSequenceEqual(
                    new[] { "Second\\b.bgsm", "First\\a.bgsm" },
                    GetGridPathValues(grid),
                    nameof(UiBehavior_BulkMaterialEditorViewSortByAdditionRestoresSessionOrder));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialsRoot = Path.Combine(materialDirectory, "Materials");
                string duplicateADirectory = Path.Combine(materialsRoot, "SetA");
                string duplicateBDirectory = Path.Combine(materialsRoot, "SetB");
                string uniqueDirectory = Path.Combine(materialsRoot, "SetC");
                Directory.CreateDirectory(duplicateADirectory);
                Directory.CreateDirectory(duplicateBDirectory);
                Directory.CreateDirectory(uniqueDirectory);

                string duplicateAPath = TestFileSupport.CreateBgsm(duplicateADirectory, "shared.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\shared_a.dds";
                });
                string uniquePath = TestFileSupport.CreateBgsm(uniqueDirectory, "unique.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\unique.dds";
                });
                string erroredPath = TestFileSupport.CreateBgem(materialDirectory, "broken.bgem", material =>
                {
                    material.Version = 11;
                    material.BaseTexture = "textures\\broken.dds";
                });
                string duplicateBPath = TestFileSupport.CreateBgsm(duplicateBDirectory, "shared.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\shared_b.dds";
                });

                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { duplicateAPath, uniquePath, erroredPath, duplicateBPath });
                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                BulkMaterialEditRow dirtyRow = session.Rows.Single(row => string.Equals(row.FilePath, MaterialFilePersistence.NormalizePath(uniquePath), StringComparison.OrdinalIgnoreCase));
                AssertEqual(true, session.TrySetCellValue(dirtyRow, diffuse, "textures\\unique_dirty.dds", out string dirtyError), nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));
                AssertEqual(string.Empty, dirtyError ?? string.Empty, nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();

                view.SetRowFilter(BulkMaterialRowFilter.DuplicateFiles);
                AssertSequenceEqual(
                    new[] { "SetA\\shared.bgsm", "SetB\\shared.bgsm" },
                    GetGridPathValues(grid),
                    nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));

                view.SetRowFilter(BulkMaterialRowFilter.UniqueFilesOnly);
                AssertEqual(2, grid.Rows.Count, nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));

                view.SetRowFilter(BulkMaterialRowFilter.DirtyFiles);
                AssertSequenceEqual(
                    new[] { "SetC\\unique.bgsm" },
                    GetGridPathValues(grid),
                    nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));

                Label summaryLabel = GetDescendants(host)
                    .OfType<Label>()
                    .First(label => label.Text.Contains("shown of", StringComparison.OrdinalIgnoreCase));
                AssertTrue(summaryLabel.Text.Contains("1 shown of 4", StringComparison.OrdinalIgnoreCase), nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));
                AssertTrue(summaryLabel.Text.Contains("Filter: Dirty Files", StringComparison.OrdinalIgnoreCase), nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));

                view.SetRowFilter(BulkMaterialRowFilter.ErroredFiles);
                AssertEqual(1, grid.Rows.Count, nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));
                AssertEqual(true, view.VisibleRows[0].HasLoadError, nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));
                AssertTrue(summaryLabel.Text.Contains("Filter: Errored Files", StringComparison.OrdinalIgnoreCase), nameof(UiBehavior_BulkMaterialEditorViewFiltersRowsAndUpdatesSummary));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewGroupsByFolderAndSortsWithinFolder()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string materialsRoot = Path.Combine(materialDirectory, "Materials");
                string alphaDirectory = Path.Combine(materialsRoot, "Alpha");
                string betaDirectory = Path.Combine(materialsRoot, "Beta");
                Directory.CreateDirectory(alphaDirectory);
                Directory.CreateDirectory(betaDirectory);

                string alphaOldPath = TestFileSupport.CreateBgsm(alphaDirectory, "old.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\alpha_old.dds";
                });
                string betaNewPath = TestFileSupport.CreateBgsm(betaDirectory, "new.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\beta_new.dds";
                });
                string alphaNewPath = TestFileSupport.CreateBgsm(alphaDirectory, "new.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\alpha_new.dds";
                });
                string betaOldPath = TestFileSupport.CreateBgsm(betaDirectory, "old.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\beta_old.dds";
                });

                File.SetLastWriteTimeUtc(alphaOldPath, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                File.SetLastWriteTimeUtc(alphaNewPath, new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));
                File.SetLastWriteTimeUtc(betaOldPath, new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));
                File.SetLastWriteTimeUtc(betaNewPath, new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc));

                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { betaNewPath, alphaOldPath, betaOldPath, alphaNewPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
                view.SetSortKey(BulkMaterialSortKey.LastModified);
                view.SetSortDirection(BulkMaterialSortDirection.Descending);
                view.SetGroupByFolder(true);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                AssertSequenceEqual(
                    new[] { "Alpha\\new.bgsm", "Alpha\\old.bgsm", "Beta\\new.bgsm", "Beta\\old.bgsm" },
                    GetGridPathValues(grid),
                    nameof(UiBehavior_BulkMaterialEditorViewGroupsByFolderAndSortsWithinFolder));
            });
        }

        private static void UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation()
        {
            TestFileSupport.RunInTempDirectories("material-editor-appearance-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string firstPath = TestFileSupport.CreateBgsm(materialDirectory, "undo0.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\source.dds";
                });
                string secondPath = TestFileSupport.CreateBgsm(materialDirectory, "undo1.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = "textures\\target.dds";
                });
                var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { firstPath, secondPath });

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                DataGridViewCell firstCell = grid.Rows[0].Cells["field::" + ControlNames.Diffuse];
                MethodInfo selectRectangle = typeof(BulkMaterialEditorView).GetMethod("SelectCellRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo fillDownMethod = typeof(BulkMaterialEditorView).GetMethod("FillDownSelectedFields", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, selectRectangle != null, nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual(true, fillDownMethod != null, nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));

                selectRectangle.Invoke(view, new object[] { 0, firstCell.ColumnIndex, 1, firstCell.ColumnIndex, true });
                fillDownMethod.Invoke(view, null);

                MaterialFieldDescriptor diffuse = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                AssertEqual("textures\\source.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual(true, view.CanUndo, nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual(false, view.CanRedo, nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));

                AssertEqual(true, view.ExecuteUndo(), nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual("textures\\target.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual(false, view.CanUndo, nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual(true, view.CanRedo, nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));

                AssertEqual(true, view.ExecuteRedo(), nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual("textures\\source.dds", Convert.ToString(session.Rows[1].GetCell(diffuse).CurrentValue), nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual(true, view.CanUndo, nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
                AssertEqual(false, view.CanRedo, nameof(UiBehavior_BulkMaterialEditorViewUndoRevertsFillDownOperation));
            });
        }

        private static void UiConstruction_MainToolsMenuIncludesFilterAndSortMenus()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", themeDirectory =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                using var form = new Main(CreateConfig(font));
                IntPtr _ = form.Handle;

                FieldInfo toolsMenuField = typeof(Main).GetField("toolsToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo filtersMenuField = typeof(Main).GetField("editFiltersToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo sortMenuField = typeof(Main).GetField("editSortToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo recoveryMenuField = typeof(Main).GetField("recoveryToolStripMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, toolsMenuField != null, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual(true, filtersMenuField != null, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual(true, sortMenuField != null, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual(true, recoveryMenuField != null, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));

                var toolsMenu = (ToolStripMenuItem)toolsMenuField.GetValue(form);
                var filtersMenu = (ToolStripMenuItem)filtersMenuField.GetValue(form);
                var sortMenu = (ToolStripMenuItem)sortMenuField.GetValue(form);
                var recoveryMenu = (ToolStripMenuItem)recoveryMenuField.GetValue(form);
                var allFilesFilterMenu = filtersMenu.DropDownItems
                    .Cast<ToolStripItem>()
                    .OfType<ToolStripMenuItem>()
                    .Single(item => item.Text == "All Files");

                AssertEqual("Tools", toolsMenu.Text, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual("Filters", filtersMenu.Text, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual("Sort", sortMenu.Text, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual("Recovery", recoveryMenu.Text, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual("Ctrl+T <> Ctrl+Shift+T", filtersMenu.ShortcutKeyDisplayString, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual(Keys.Control | Keys.Shift | Keys.A, allFilesFilterMenu.ShortcutKeys, nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual(filtersMenu, toolsMenu.DropDownItems[0], nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertEqual(sortMenu, toolsMenu.DropDownItems[1], nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertTrue(filtersMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Duplicate Files"), nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertTrue(filtersMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Custom Search Results"), nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertTrue(sortMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "By Addition"), nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertTrue(sortMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Group by Folder"), nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertTrue(recoveryMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Recover Most Recent Backup"), nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
                AssertTrue(recoveryMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Browse Backups..."), nameof(UiConstruction_MainToolsMenuIncludesFilterAndSortMenus));
            });
        }

        private static void UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus()
        {
            TestFileSupport.RunInTempDirectory("material-editor-appearance-tests", themeDirectory =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                using var host = new Form();
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr _ = host.Handle;
                IntPtr __ = view.Handle;

                FieldInfo fileFiltersField = typeof(BulkMaterialEditorView).GetField("fileFiltersMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo fileSortField = typeof(BulkMaterialEditorView).GetField("fileSortMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo fieldFiltersField = typeof(BulkMaterialEditorView).GetField("fieldFiltersMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo fieldSortField = typeof(BulkMaterialEditorView).GetField("fieldSortMenuItem", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, fileFiltersField != null, nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertEqual(true, fileSortField != null, nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertEqual(true, fieldFiltersField != null, nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertEqual(true, fieldSortField != null, nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));

                var fileFiltersMenu = (ToolStripMenuItem)fileFiltersField.GetValue(view);
                var fileSortMenu = (ToolStripMenuItem)fileSortField.GetValue(view);
                var fieldFiltersMenu = (ToolStripMenuItem)fieldFiltersField.GetValue(view);
                var fieldSortMenu = (ToolStripMenuItem)fieldSortField.GetValue(view);

                AssertEqual("Filters", fileFiltersMenu.Text, nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertEqual("Sort", fileSortMenu.Text, nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertEqual("Filters", fieldFiltersMenu.Text, nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertEqual("Sort", fieldSortMenu.Text, nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertTrue(fileFiltersMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Duplicate Files"), nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertTrue(fileFiltersMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Custom Search Results"), nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertTrue(fileSortMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "By Addition"), nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertTrue(fieldFiltersMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Errored Files"), nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertTrue(fieldFiltersMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Custom Search Results"), nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
                AssertTrue(fieldSortMenu.DropDownItems.Cast<ToolStripItem>().Any(item => item.Text == "Group by Folder"), nameof(UiConstruction_BulkMaterialEditorViewContextMenusIncludeFilterAndSortMenus));
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
                CreateBackupsByDefault = true,
                RetainOriginalBackup = false,
                MaxBackupsPerFile = 0,
                MaxBackupFolderMegabytes = 0,
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

        private static string GetBulkValidationLabelText(BulkMaterialEditorView view)
        {
            FieldInfo validationLabelField = typeof(BulkMaterialEditorView).GetField("validationLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, validationLabelField != null, nameof(GetBulkValidationLabelText));
            Label validationLabel = validationLabelField.GetValue(view) as Label;
            return validationLabel?.Text ?? string.Empty;
        }

        private static string[] GetGridPathValues(DataGridView grid)
        {
            return grid.Rows
                .Cast<DataGridViewRow>()
                .Select(row => Convert.ToString(row.Cells["__path"].Value) ?? string.Empty)
                .ToArray();
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

        private static void AssertTrue(bool condition, string testName)
        {
            if (!condition)
                throw new InvalidOperationException($"{testName} failed.");
        }

        private static void AssertSequenceEqual(string[] expected, string[] actual, string testName)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException($"{testName} failed. Expected '{string.Join(", ", expected)}', got '{string.Join(", ", actual)}'.");
        }

        private static void ConfigureBulkEditorVisibleFields(BulkMaterialEditorView view, params string[] labels)
        {
            FieldInfo selectedLabelPreferencesField = typeof(BulkMaterialEditorView).GetField("selectedLabelPreferences", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo refreshVisibleDescriptorsAndGridMethod = typeof(BulkMaterialEditorView).GetMethod("RefreshVisibleDescriptorsAndGrid", BindingFlags.Instance | BindingFlags.NonPublic);
            AssertEqual(true, selectedLabelPreferencesField != null, nameof(ConfigureBulkEditorVisibleFields));
            AssertEqual(true, refreshVisibleDescriptorsAndGridMethod != null, nameof(ConfigureBulkEditorVisibleFields));

            selectedLabelPreferencesField.SetValue(
                view,
                new HashSet<string>(labels ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase));
            refreshVisibleDescriptorsAndGridMethod.Invoke(view, new object[] { true });
        }

        private static void WithClipboardPreserved(Action action)
        {
            string originalText = null;
            bool hadOriginalText = false;

            if (Clipboard.ContainsText())
            {
                originalText = Clipboard.GetText();
                hadOriginalText = true;
            }

            try
            {
                action();
            }
            finally
            {
                if (hadOriginalText)
                    Clipboard.SetText(originalText ?? string.Empty);
                else
                    Clipboard.Clear();
            }
        }

        private static void PerformGridSelection(DataGridView grid, Action action)
        {
            if (grid == null || action == null)
                return;

            grid.SuspendLayout();
            try
            {
                action();
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
    }
}
