using Material_Editor.Controls;
using Material_Editor.AdvancedVariant;
using Material_Editor.Dialogs;
using Material_Editor.Forms;
using Material_Editor.Models;
using Material_Editor.Services;
using Material_Editor.Theming;
using MaterialLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MaterialEditor.Tests
{
    internal static class StressTests
    {
        public static void RunAll()
        {
            RunScenario(nameof(AdvancedVariant_ResolveMaximumSupportedContextSet), AdvancedVariant_ResolveMaximumSupportedContextSet);
            RunScenario(nameof(VariationGenerator_GeneratesLargeAdvancedBatch), VariationGenerator_GeneratesLargeAdvancedBatch);
            RunScenario(nameof(BulkSession_AppliesChangesAcrossLargeFileSet), BulkSession_AppliesChangesAcrossLargeFileSet);
            RunScenario(nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet), BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet);
            RunScenario(nameof(BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState), BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState);
            RunScenario(nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet), FieldOverwrite_IterativeModeProcessesLargeTargetSet);
            RunScenario(nameof(UiLifecycle_RepeatedMainFormConstructionKeepsResourceGrowthBounded), UiLifecycle_RepeatedMainFormConstructionKeepsResourceGrowthBounded);
            RunScenario(nameof(UiLifecycle_RepeatedBulkEditorProjectionChurnKeepsResourceGrowthBounded), UiLifecycle_RepeatedBulkEditorProjectionChurnKeepsResourceGrowthBounded);
            RunScenario(nameof(UiLifecycle_RepeatedSearchAndRecoveryDialogConstructionKeepsResourceGrowthBounded), UiLifecycle_RepeatedSearchAndRecoveryDialogConstructionKeepsResourceGrowthBounded);
            RunScenario(nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable), UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable);
            RunScenario(nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable), UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable);
            RunScenario(nameof(UiGrid_ScrollAndPaintChurnKeepsResourceGrowthBounded), UiGrid_ScrollAndPaintChurnKeepsResourceGrowthBounded);
        }

        public static void RunSoakAll()
        {
            RunScenario(nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded), Soak_BulkEditorWorkflowLoopKeepsResourcesBounded);
            RunScenario(nameof(Soak_BackupRecoveryLoopPreservesRecoverability), Soak_BackupRecoveryLoopPreservesRecoverability);
            RunScenario(nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable), Soak_GenerateEditOverwriteRoundTripRemainsStable);
        }

        private static void AdvancedVariant_ResolveMaximumSupportedContextSet()
        {
            var options = new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Layer1", Enumerable.Range(1, 10).ToArray()),
                    new AdvancedVariantLayerDefinition("Layer2", Enumerable.Range(1, 10).ToArray()),
                    new AdvancedVariantLayerDefinition("Layer3", Enumerable.Range(1, 10).ToArray()),
                    new AdvancedVariantLayerDefinition("Layer4", Enumerable.Range(1, 10).ToArray()),
                }
            };

            AdvancedVariantResolvedContext[] contexts = AdvancedVariantEngine.Resolve(options).ToArray();

            AssertEqual(10000, contexts.Length, nameof(AdvancedVariant_ResolveMaximumSupportedContextSet));
            AssertEqual("1111", contexts[0].Index, nameof(AdvancedVariant_ResolveMaximumSupportedContextSet));
            AssertEqual("01010101", contexts[0].IndexNN, nameof(AdvancedVariant_ResolveMaximumSupportedContextSet));
            AssertEqual("10101010", contexts[^1].Index, nameof(AdvancedVariant_ResolveMaximumSupportedContextSet));
            AssertEqual("10101010", contexts[^1].IndexNN, nameof(AdvancedVariant_ResolveMaximumSupportedContextSet));
        }

        private static void VariationGenerator_GeneratesLargeAdvancedBatch()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            var template = new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\template_{indexNN}.dds",
                NormalTexture = "textures\\template_n.dds"
            };

            MaterialFieldDescriptor diffuseDescriptor = GetBgsmDescriptor(ControlNames.Diffuse);
            var options = new MaterialVariationOptions
            {
                OutputPattern = Path.Combine(outputPath, "variant_{indexNN}_{indexTok}.bgsm"),
                Fields = new[]
                {
                    new MaterialVariationFieldAssignment(diffuseDescriptor, "textures\\stress_{indexNN}_{indexTokLayer1}_{indexTokLayer2}.dds")
                },
                AdvancedVariant = new AdvancedVariantOptions
                {
                    Layers = new[]
                    {
                        new AdvancedVariantLayerDefinition("Layer1", Enumerable.Range(1, 20).ToArray()),
                        new AdvancedVariantLayerDefinition("Layer2", Enumerable.Range(1, 20).ToArray()),
                    },
                    Rules = new[]
                    {
                        new AdvancedVariantRule { IndexToken = "stress", SourceOrder = 0 }
                    }
                }
            };

            IReadOnlyList<FieldCopyResult> results = MaterialVariationGenerator.Generate(template, options, serializeAsJson: true);

            AssertEqual(400, results.Count, nameof(VariationGenerator_GeneratesLargeAdvancedBatch));
            AssertEqual(400, results.Count(result => result.Status == FieldCopyStatus.Success), nameof(VariationGenerator_GeneratesLargeAdvancedBatch));
            AssertEqual(400, Directory.GetFiles(outputPath, "*.bgsm").Length, nameof(VariationGenerator_GeneratesLargeAdvancedBatch));

            string firstText = File.ReadAllText(Path.Combine(outputPath, "variant_0101_stress.bgsm"));
            string lastText = File.ReadAllText(Path.Combine(outputPath, "variant_2020_stress.bgsm"));

            AssertContains(firstText, "textures\\\\stress_0101_1_1.dds", nameof(VariationGenerator_GeneratesLargeAdvancedBatch));
            AssertContains(lastText, "textures\\\\stress_2020_20_20.dds", nameof(VariationGenerator_GeneratesLargeAdvancedBatch));
        }

        private static void BulkSession_AppliesChangesAcrossLargeFileSet()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string directory = outputDirectory.Path;
            const int fileCount = 150;
            var paths = new List<string>(fileCount);

            for (int i = 0; i < fileCount; i++)
            {
                string indexText = i.ToString("000", CultureInfo.InvariantCulture);
                paths.Add(TestFileSupport.CreateBgsm(directory, $"session_{indexText}.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = $"textures\\original_{indexText}.dds";
                    material.NormalTexture = "textures\\normal.dds";
                }));
            }

            BulkMaterialEditSession session = BulkMaterialEditSession.Create(MaterialType.Material, paths);
            MaterialFieldDescriptor diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);

            foreach (BulkMaterialEditRow row in session.Rows)
            {
                AssertTrue(session.TrySetCellValue(row, diffuseDescriptor, "textures\\release_stress.dds", out string errorMessage), nameof(BulkSession_AppliesChangesAcrossLargeFileSet));
                AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(BulkSession_AppliesChangesAcrossLargeFileSet));
            }

            IReadOnlyList<FieldCopyResult> results = session.ApplyChanges(backupBeforeWrite: false);

            AssertEqual(fileCount, results.Count, nameof(BulkSession_AppliesChangesAcrossLargeFileSet));
            AssertEqual(fileCount, results.Count(result => result.Status == FieldCopyStatus.Success), nameof(BulkSession_AppliesChangesAcrossLargeFileSet));
            AssertTrue(!session.HasDirtyRows, nameof(BulkSession_AppliesChangesAcrossLargeFileSet));
            AssertEqual("textures\\release_stress.dds", TestFileSupport.LoadBgsm(paths[0]).DiffuseTexture, nameof(BulkSession_AppliesChangesAcrossLargeFileSet));
            AssertEqual("textures\\release_stress.dds", TestFileSupport.LoadBgsm(paths[^1]).DiffuseTexture, nameof(BulkSession_AppliesChangesAcrossLargeFileSet));
        }

        private static void BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string directory = outputDirectory.Path;
            const int fileCount = 48;
            var paths = new List<string>(fileCount);

            for (int i = 0; i < fileCount; i++)
            {
                string indexText = i.ToString("000", CultureInfo.InvariantCulture);
                paths.Add(TestFileSupport.CreateBgsm(directory, $"backup_{indexText}.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = $"textures\\initial_{indexText}.dds";
                    material.NormalTexture = "textures\\normal.dds";
                }));
            }

            var config = new Config
            {
                CreateBackupsByDefault = true,
                MaxBackupsPerFile = 0,
                MaxBackupFolderMegabytes = 0
            };

            BulkMaterialEditSession firstSession = BulkMaterialEditSession.Create(MaterialType.Material, paths);
            MaterialFieldDescriptor diffuseDescriptor = firstSession.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
            for (int i = 0; i < firstSession.Rows.Count; i++)
            {
                string nextValue = $"textures\\pass1_{i.ToString("000", CultureInfo.InvariantCulture)}.dds";
                AssertTrue(firstSession.TrySetCellValue(firstSession.Rows[i], diffuseDescriptor, nextValue, out string errorMessage), nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
                AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
            }

            IReadOnlyList<FieldCopyResult> firstResults = firstSession.ApplyChanges(backupBeforeWrite: true, config);
            AssertEqual(fileCount, firstResults.Count(result => result.Status == FieldCopyStatus.Success), nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
            AssertEqual(1, GetBackupFiles(paths[0]).Length, nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
            AssertEqual(fileCount, Directory.GetFiles(MaterialBackupService.BackupDirectoryPathValue, "*.bak").Length, nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));

            BulkMaterialEditSession secondSession = BulkMaterialEditSession.Create(MaterialType.Material, paths);
            MaterialFieldDescriptor secondDiffuseDescriptor = secondSession.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
            for (int i = 0; i < secondSession.Rows.Count; i++)
            {
                string nextValue = $"textures\\pass2_{i.ToString("000", CultureInfo.InvariantCulture)}.dds";
                AssertTrue(secondSession.TrySetCellValue(secondSession.Rows[i], secondDiffuseDescriptor, nextValue, out string errorMessage), nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
                AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
            }

            IReadOnlyList<FieldCopyResult> secondResults = secondSession.ApplyChanges(backupBeforeWrite: true, config);
            AssertEqual(fileCount, secondResults.Count(result => result.Status == FieldCopyStatus.Success), nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
            AssertEqual(2, GetBackupFiles(paths[0]).Length, nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
            AssertEqual(fileCount * 2, Directory.GetFiles(MaterialBackupService.BackupDirectoryPathValue, "*.bak").Length, nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
            AssertEqual("textures\\pass2_000.dds", TestFileSupport.LoadBgsm(paths[0]).DiffuseTexture, nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
            AssertEqual("textures\\pass1_000.dds", TestFileSupport.LoadBgsm(GetBackupFiles(paths[0]).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Last()).DiffuseTexture, nameof(BulkSession_BackupHeavyApplyCreatesTimestampedBackupsForLargeSet));
        }

        private static void BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string directory = outputDirectory.Path;
            const int normalCount = 18;
            const int lockedCount = 3;
            const int malformedCount = 3;
            const int wrongTypeCount = 3;

            var allPaths = new List<string>(normalCount + lockedCount + malformedCount + wrongTypeCount);
            var lockedPaths = new List<string>(lockedCount);

            for (int i = 0; i < normalCount; i++)
            {
                string path = TestFileSupport.CreateBgsm(directory, $"normal_{i.ToString("000", CultureInfo.InvariantCulture)}.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = $"textures\\normal_{i.ToString("000", CultureInfo.InvariantCulture)}.dds";
                });
                allPaths.Add(path);
            }

            for (int i = 0; i < lockedCount; i++)
            {
                string path = TestFileSupport.CreateBgsm(directory, $"locked_{i.ToString("000", CultureInfo.InvariantCulture)}.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = $"textures\\locked_{i.ToString("000", CultureInfo.InvariantCulture)}.dds";
                });
                allPaths.Add(path);
                lockedPaths.Add(path);
            }

            for (int i = 0; i < malformedCount; i++)
            {
                string path = Path.Combine(directory, $"malformed_{i.ToString("000", CultureInfo.InvariantCulture)}.bgsm");
                File.WriteAllText(path, "not a valid material");
                allPaths.Add(path);
            }

            for (int i = 0; i < wrongTypeCount; i++)
            {
                string path = TestFileSupport.CreateBgem(directory, $"wrong_{i.ToString("000", CultureInfo.InvariantCulture)}.bgem", material =>
                {
                    material.Version = 11;
                    material.BaseTexture = $"textures\\wrong_{i.ToString("000", CultureInfo.InvariantCulture)}.dds";
                });
                allPaths.Add(path);
            }

            BulkMaterialEditSession session = BulkMaterialEditSession.Create(MaterialType.Material, allPaths);
            MaterialFieldDescriptor diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
            foreach (BulkMaterialEditRow row in session.Rows.Where(row => !row.HasLoadError))
                AssertTrue(session.TrySetCellValue(row, diffuseDescriptor, "textures\\mixed_batch.dds", out _), nameof(BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState));

            using var lockStreamA = new FileStream(lockedPaths[0], FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using var lockStreamB = new FileStream(lockedPaths[1], FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using var lockStreamC = new FileStream(lockedPaths[2], FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            IReadOnlyList<FieldCopyResult> results = session.ApplyChanges(backupBeforeWrite: false);

            AssertEqual(normalCount, results.Count(result => result.Status == FieldCopyStatus.Success), nameof(BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState));
            AssertEqual(lockedCount + malformedCount + wrongTypeCount, results.Count(result => result.Status == FieldCopyStatus.Failed), nameof(BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState));

            foreach (string lockedPath in lockedPaths)
            {
                BulkMaterialEditRow lockedRow = session.Rows.Single(row => string.Equals(row.FilePath, MaterialFilePersistence.NormalizePath(lockedPath), StringComparison.OrdinalIgnoreCase));
                AssertTrue(lockedRow.IsDirty, nameof(BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState));
            }

            BulkMaterialEditRow successRow = session.Rows.First(row => !row.HasLoadError && !lockedPaths.Contains(row.FilePath, StringComparer.OrdinalIgnoreCase));
            AssertTrue(!successRow.IsDirty, nameof(BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState));
            AssertEqual("textures\\mixed_batch.dds", TestFileSupport.LoadBgsm(successRow.FilePath).DiffuseTexture, nameof(BulkSession_MixedFailureBatchReportsPartialFailuresWithoutLosingDirtyState));
        }

        private static void FieldOverwrite_IterativeModeProcessesLargeTargetSet()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string directory = outputDirectory.Path;
            const int materialCount = 120;
            const int effectCount = 5;
            var targetFiles = new List<string>(materialCount + effectCount);

            for (int i = 0; i < materialCount; i++)
            {
                string indexText = i.ToString("000", CultureInfo.InvariantCulture);
                targetFiles.Add(TestFileSupport.CreateBgsm(directory, $"target_{indexText}.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = $"textures\\orig_{indexText}.dds";
                    material.GrayscaleToPaletteScale = 0.25f;
                }));
            }

            for (int i = 0; i < effectCount; i++)
            {
                string indexText = i.ToString("000", CultureInfo.InvariantCulture);
                targetFiles.Add(TestFileSupport.CreateBgem(directory, $"effect_{indexText}.bgem", material =>
                {
                    material.Version = 11;
                    material.BaseTexture = $"textures\\effect_{indexText}.dds";
                }));
            }

            MaterialFieldDescriptor diffuseDescriptor = GetBgsmDescriptor(ControlNames.Diffuse);
            MaterialFieldDescriptor grayscaleDescriptor = GetBgsmDescriptor(ControlNames.GrayscaleToPaletteScale);
            var tool = new FieldOverwriteTool();
            var sourceState = new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\source.dds",
                GrayscaleToPaletteScale = 1.0f
            };

            IReadOnlyList<FieldCopyResult> results = tool.Run(sourceState, new FieldOverwriteOptions
            {
                Descriptors = new[] { diffuseDescriptor, grayscaleDescriptor },
                TargetFiles = targetFiles,
                BackupBeforeWrite = false,
                IterativeOptions = new IterativeFieldOverwriteOptions
                {
                    StartIndex = 100,
                    Count = 60,
                    Step = 2,
                    Assignments = new IterativeFieldAssignment[]
                    {
                        new(diffuseDescriptor, "textures\\stress_{indexTok}_{index:000}.dds"),
                        new(grayscaleDescriptor, 1.0f, 0.5f)
                    }
                }
            });

            AssertEqual(materialCount + effectCount, results.Count, nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet));
            AssertEqual(60, results.Count(result => result.Status == FieldCopyStatus.Success), nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet));
            AssertEqual(65, results.Count(result => result.Status == FieldCopyStatus.Skipped), nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet));

            BGSM firstSelected = TestFileSupport.LoadBgsm(Path.Combine(directory, "target_000.bgsm"));
            BGSM firstSkipped = TestFileSupport.LoadBgsm(Path.Combine(directory, "target_001.bgsm"));
            BGSM lastSelected = TestFileSupport.LoadBgsm(Path.Combine(directory, "target_118.bgsm"));

            AssertEqual("textures\\stress_100_100.dds", firstSelected.DiffuseTexture, nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet));
            AssertEqual(1.0f, firstSelected.GrayscaleToPaletteScale, nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet));
            AssertEqual("textures\\orig_001.dds", firstSkipped.DiffuseTexture, nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet));
            AssertEqual("textures\\stress_218_218.dds", lastSelected.DiffuseTexture, nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet));
            AssertEqual(30.5f, lastSelected.GrayscaleToPaletteScale, nameof(FieldOverwrite_IterativeModeProcessesLargeTargetSet));
        }

        private static void UiLifecycle_RepeatedMainFormConstructionKeepsResourceGrowthBounded()
        {
            TestFileSupport.RunInTempDirectory("material-editor-ui-stress-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                // Warm up JIT and one-time static allocations before taking the baseline.
                CreateAndDisposeMainForm(font);

                UiResourceSnapshot baseline = CaptureUiResources();
                const int iterations = 16;
                for (int i = 0; i < iterations; i++)
                {
                    CreateAndDisposeMainForm(font);
                    if ((i + 1) % 4 == 0)
                        ForceFullCollection();
                }

                UiResourceSnapshot after = CaptureUiResources();
                AssertResourceGrowthWithinThreshold(
                    baseline,
                    after,
                    new UiResourceThresholds(4 * 1024 * 1024, 32 * 1024 * 1024, 25, 12, 12),
                    nameof(UiLifecycle_RepeatedMainFormConstructionKeepsResourceGrowthBounded));
            });
        }

        private static void UiLifecycle_RepeatedBulkEditorProjectionChurnKeepsResourceGrowthBounded()
        {
            TestFileSupport.RunInTempDirectories("material-editor-ui-stress-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = CreateBulkStressFiles(materialDirectory, 96);

                // Warm up grid creation and projection rebuilding before measuring.
                CreateAndDisposeBulkEditor(font, paths);

                UiResourceSnapshot baseline = CaptureUiResources();
                const int iterations = 10;
                for (int i = 0; i < iterations; i++)
                {
                    CreateAndDisposeBulkEditor(font, paths);
                    if ((i + 1) % 2 == 0)
                        ForceFullCollection();
                }

                UiResourceSnapshot after = CaptureUiResources();
                AssertResourceGrowthWithinThreshold(
                    baseline,
                    after,
                    new UiResourceThresholds(8 * 1024 * 1024, 48 * 1024 * 1024, 35, 18, 18),
                    nameof(UiLifecycle_RepeatedBulkEditorProjectionChurnKeepsResourceGrowthBounded));
            });
        }

        private static void UiLifecycle_RepeatedSearchAndRecoveryDialogConstructionKeepsResourceGrowthBounded()
        {
            TestFileSupport.RunInTempDirectory("material-editor-ui-stress-tests", directory =>
            {
                CreateThemeFile(directory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, directory);

                IReadOnlyList<MaterialFieldDescriptor> descriptors = MaterialFieldRegistry.GetDescriptors(new BGSM());
                MaterialBackupEntry[] backups = CreateSyntheticBackupEntries(180);

                CreateAndDisposeSearchDialog(descriptors, replaceMode: false);
                CreateAndDisposeSearchDialog(descriptors, replaceMode: true);
                CreateAndDisposeBackupRecoveryDialog(backups);

                UiResourceSnapshot baseline = CaptureUiResources();
                const int iterations = 12;
                for (int i = 0; i < iterations; i++)
                {
                    CreateAndDisposeSearchDialog(descriptors, replaceMode: i % 2 == 0);
                    CreateAndDisposeBackupRecoveryDialog(backups);
                    if ((i + 1) % 3 == 0)
                        ForceFullCollection();
                }

                UiResourceSnapshot after = CaptureUiResources();
                AssertResourceGrowthWithinThreshold(
                    baseline,
                    after,
                    new UiResourceThresholds(8 * 1024 * 1024, 40 * 1024 * 1024, 40, 18, 18),
                    nameof(UiLifecycle_RepeatedSearchAndRecoveryDialogConstructionKeepsResourceGrowthBounded));
            });
        }

        private static void UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable()
        {
            TestFileSupport.RunInTempDirectories("material-editor-ui-stress-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = CreateBulkStressFiles(materialDirectory, 32);
                const int cycleCount = 5;

                for (int cycle = 0; cycle < cycleCount; cycle++)
                {
                    BulkMaterialEditSession session = BulkMaterialEditSession.Create(MaterialType.Material, paths);
                    MaterialFieldDescriptor diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);

                    using var host = new Form();
                    using var view = new BulkMaterialEditorView();
                    host.Controls.Add(view);
                    IntPtr hostHandle = host.Handle;
                    IntPtr viewHandle = view.Handle;

                    view.Initialize(session, CreateConfig(font), backupBeforeWrite: cycle % 2 == 0);
                    AssertEqual(paths.Length, view.VisibleRows.Count, nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));

                    for (int rowIndex = 0; rowIndex < 12; rowIndex++)
                    {
                        string nextValue = $"textures\\cycle_{cycle}_{rowIndex.ToString("00", CultureInfo.InvariantCulture)}.dds";
                        AssertTrue(session.TrySetCellValue(session.Rows[rowIndex], diffuseDescriptor, nextValue, out string errorMessage), nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                        AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                    }

                    AssertEqual(12, session.Rows.Count(row => row.IsDirty), nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                    view.NotifySessionChanged();
                    AssertEqual(12, session.Rows.Count(row => row.IsDirty), nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                    AssertEqual(12, view.DirtyRowCount, nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                    IReadOnlyList<FieldCopyResult> results = session.ApplyChanges(backupBeforeWrite: cycle % 2 == 0, CreateConfig(font));
                    view.NotifySessionChanged();

                    AssertEqual(12, results.Count(result => result.Status == FieldCopyStatus.Success), nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                    AssertEqual(paths.Length - 12, results.Count(result => result.Status == FieldCopyStatus.Skipped), nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));

                    TestFileSupport.SaveMaterial(paths[0], new BGSM
                    {
                        Version = 2,
                        DiffuseTexture = $"textures\\external_{cycle}.dds",
                        NormalTexture = "textures\\bulk_n.dds"
                    });

                    IReadOnlyList<BulkMaterialEditRow> reloaded = session.ReloadRows(new[] { session.Rows[0] });
                    view.NotifySessionChanged();
                    AssertEqual(1, reloaded.Count, nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                    AssertEqual($"textures\\external_{cycle}.dds", Convert.ToString(reloaded[0].GetCell(diffuseDescriptor).CurrentValue), nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                    AssertTrue(!reloaded[0].IsDirty, nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                }

                BulkMaterialEditSession finalSession = BulkMaterialEditSession.Create(MaterialType.Material, paths);
                MaterialFieldDescriptor finalDiffuseDescriptor = finalSession.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                AssertEqual($"textures\\external_{cycleCount - 1}.dds", Convert.ToString(finalSession.Rows[0].GetCell(finalDiffuseDescriptor).CurrentValue), nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
                AssertEqual($"textures\\cycle_{cycleCount - 1}_11.dds", Convert.ToString(finalSession.Rows[11].GetCell(finalDiffuseDescriptor).CurrentValue), nameof(UiWorkflow_RepeatedBulkEditorOpenSaveReloadCyclesStayStable));
            });
        }

        private static void UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable()
        {
            TestFileSupport.RunInTempDirectories("material-editor-ui-stress-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = CreateBulkStressFiles(materialDirectory, 54);
                BulkMaterialEditSession session = BulkMaterialEditSession.Create(MaterialType.Material, paths);
                MaterialFieldDescriptor diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                MaterialFieldDescriptor alphaDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Alpha);
                MaterialFieldDescriptor twoSidedDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.TwoSided);

                using var host = new Form
                {
                    Width = 1280,
                    Height = 900
                };
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr hostHandle = host.Handle;
                IntPtr viewHandle = view.Handle;

                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                MethodInfo applyFilterMethod = typeof(BulkMaterialEditorView).GetMethod("ApplyRequestAsFilter", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo replaceMethod = typeof(BulkMaterialEditorView).GetMethod("ExecuteReplaceRequest", BindingFlags.Instance | BindingFlags.NonPublic);
                AssertEqual(true, applyFilterMethod != null, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                AssertEqual(true, replaceMethod != null, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));

                for (int cycle = 0; cycle < 6; cycle++)
                {
                    int textBaseIndex = cycle * 9;
                    int boolBaseIndex = textBaseIndex + 4;
                    int numberBaseIndex = textBaseIndex + 6;
                    string textNeedle = $"needle_{cycle}";
                    string textReplacement = $"replaced_{cycle}";

                    for (int offset = 0; offset < 4; offset++)
                    {
                        string nextValue = $"textures\\{textNeedle}_{offset.ToString("00", CultureInfo.InvariantCulture)}.dds";
                        AssertTrue(session.TrySetCellValue(session.Rows[textBaseIndex + offset], diffuseDescriptor, nextValue, out string errorMessage), nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                        AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                    }

                    for (int offset = 0; offset < 2; offset++)
                    {
                        AssertTrue(session.TrySetCellValue(session.Rows[boolBaseIndex + offset], twoSidedDescriptor, true, out string errorMessage), nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                        AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                    }

                    for (int offset = 0; offset < 3; offset++)
                    {
                        AssertTrue(session.TrySetCellValue(session.Rows[numberBaseIndex + offset], alphaDescriptor, 0.25f, out string errorMessage), nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                        AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                    }

                    view.NotifySessionChanged();

                    var textRequest = new BulkFindReplaceRequest
                    {
                        Scope = BulkFindScope.ByField,
                        ValueType = BulkFindValueType.Text,
                        FindText = textNeedle,
                        ReplaceText = textReplacement,
                        FieldLabels = new List<string> { ControlNames.Diffuse }
                    };
                    applyFilterMethod.Invoke(view, new object[] { textRequest });
                    AssertEqual(BulkMaterialRowFilter.CustomFiles, view.ActiveFilter, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                    AssertEqual("Custom Search Results", view.CustomFilterName, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                    AssertEqual(4, view.VisibleRows.Count, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));

                    replaceMethod.Invoke(view, new object[] { textRequest });
                    for (int offset = 0; offset < 4; offset++)
                    {
                        string actual = Convert.ToString(session.Rows[textBaseIndex + offset].GetCell(diffuseDescriptor).CurrentValue);
                        AssertContains(actual, textReplacement, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                    }
                    AssertEqual(4, view.VisibleRows.Count, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));

                    view.SetRowFilter(BulkMaterialRowFilter.AllFiles);

                    var booleanRequest = new BulkFindReplaceRequest
                    {
                        Scope = BulkFindScope.ByField,
                        ValueType = BulkFindValueType.Boolean,
                        FindBooleanValue = true,
                        ReplaceBooleanValue = false,
                        FieldLabels = new List<string> { ControlNames.TwoSided }
                    };
                    replaceMethod.Invoke(view, new object[] { booleanRequest });
                    for (int offset = 0; offset < 2; offset++)
                        AssertEqual(false, Convert.ToBoolean(session.Rows[boolBaseIndex + offset].GetCell(twoSidedDescriptor).CurrentValue), nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));

                    var numberRequest = new BulkFindReplaceRequest
                    {
                        Scope = BulkFindScope.ByField,
                        ValueType = BulkFindValueType.Number,
                        FindText = "0.25",
                        ReplaceText = "0.5",
                        FieldLabels = new List<string> { ControlNames.Alpha }
                    };
                    replaceMethod.Invoke(view, new object[] { numberRequest });
                    for (int offset = 0; offset < 3; offset++)
                        AssertEqual(0.5f, Convert.ToSingle(session.Rows[numberBaseIndex + offset].GetCell(alphaDescriptor).CurrentValue), nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));

                    view.SetSortKey(cycle % 2 == 0 ? BulkMaterialSortKey.LastModified : BulkMaterialSortKey.ByAddition);
                    view.SetSortDirection(cycle % 2 == 0 ? BulkMaterialSortDirection.Descending : BulkMaterialSortDirection.Ascending);
                    view.SetGroupByFolder(cycle % 2 == 0);
                    view.SetRowFilter(BulkMaterialRowFilter.DirtyFiles);
                    AssertEqual(7 * (cycle + 1), view.VisibleRows.Count, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                    view.SetRowFilter(BulkMaterialRowFilter.AllFiles);
                }

                IReadOnlyList<FieldCopyResult> results = session.ApplyChanges(backupBeforeWrite: false);
                view.NotifySessionChanged();
                AssertEqual(42, results.Count(result => result.Status == FieldCopyStatus.Success), nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
                AssertTrue(!session.HasDirtyRows, nameof(UiWorkflow_RepeatedFindReplaceAndCustomFilterCyclesStayStable));
            });
        }

        private static void UiGrid_ScrollAndPaintChurnKeepsResourceGrowthBounded()
        {
            TestFileSupport.RunInTempDirectories("material-editor-ui-stress-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = CreateBulkStressFiles(materialDirectory, 140);

                using var host = new Form
                {
                    Width = 1200,
                    Height = 900
                };
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr hostHandle = host.Handle;
                IntPtr viewHandle = view.Handle;

                BulkMaterialEditSession session = BulkMaterialEditSession.Create(MaterialType.Material, paths);
                view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);

                DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                int pathColumnIndex = grid.Columns["__path"].Index;
                int scrollableColumnIndex = grid.Columns
                    .Cast<DataGridViewColumn>()
                    .Where(column => column.Visible && !column.Frozen)
                    .Select(column => column.Index)
                    .FirstOrDefault(pathColumnIndex);

                grid.CurrentCell = grid.Rows[0].Cells[scrollableColumnIndex];
                grid.Invalidate();
                grid.Update();
                Application.DoEvents();

                UiResourceSnapshot baseline = CaptureUiResources();
                const int iterations = 60;
                for (int i = 0; i < iterations; i++)
                {
                    int rowIndex = (i * 7) % grid.Rows.Count;
                    grid.CurrentCell = grid.Rows[rowIndex].Cells[scrollableColumnIndex];
                    if (rowIndex < grid.Rows.Count - 1)
                        grid.FirstDisplayedScrollingRowIndex = rowIndex;

                    if (scrollableColumnIndex >= 0 && scrollableColumnIndex < grid.Columns.Count && !grid.Columns[scrollableColumnIndex].Frozen)
                        grid.FirstDisplayedScrollingColumnIndex = scrollableColumnIndex;

                    if (i % 10 == 0)
                    {
                        view.SetSortDirection(i % 20 == 0 ? BulkMaterialSortDirection.Descending : BulkMaterialSortDirection.Ascending);
                        view.SetRowFilter(i % 20 == 0 ? BulkMaterialRowFilter.AllFiles : BulkMaterialRowFilter.DirtyFiles);
                        view.SetRowFilter(BulkMaterialRowFilter.AllFiles);
                    }

                    grid.Invalidate();
                    grid.Update();
                    host.Update();
                    Application.DoEvents();
                }

                UiResourceSnapshot after = CaptureUiResources();
                AssertResourceGrowthWithinThreshold(
                    baseline,
                    after,
                    new UiResourceThresholds(4 * 1024 * 1024, 24 * 1024 * 1024, 15, 10, 10),
                    nameof(UiGrid_ScrollAndPaintChurnKeepsResourceGrowthBounded));
            });
        }

        private static void Soak_BulkEditorWorkflowLoopKeepsResourcesBounded()
        {
            TestFileSupport.RunInTempDirectories("material-editor-ui-soak-tests", (themeDirectory, materialDirectory) =>
            {
                CreateThemeFile(themeDirectory, "default", "Default");
                using var font = new Font("Segoe UI", 11f);
                AppearanceService.InitializeForTesting("default", font, themeDirectory);

                string[] paths = CreateBulkStressFiles(materialDirectory, 72);
                using var host = new Form
                {
                    Width = 1280,
                    Height = 900
                };
                using var view = new BulkMaterialEditorView();
                host.Controls.Add(view);
                IntPtr hostHandle = host.Handle;
                IntPtr viewHandle = view.Handle;

                UiResourceSnapshot baseline = CaptureUiResources();

                const int cycleCount = 12;
                for (int cycle = 0; cycle < cycleCount; cycle++)
                {
                    BulkMaterialEditSession session = BulkMaterialEditSession.Create(MaterialType.Material, paths);
                    view.Initialize(session, CreateConfig(font), backupBeforeWrite: cycle % 2 == 0);

                    MaterialFieldDescriptor diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
                    int changedRowCount = 18;
                    int startIndex = (cycle * 3) % 12;
                    for (int offset = 0; offset < changedRowCount; offset++)
                    {
                        int rowIndex = (startIndex + offset) % session.Rows.Count;
                        string nextValue = $"textures\\soak_cycle_{cycle}_{rowIndex.ToString("00", CultureInfo.InvariantCulture)}.dds";
                        AssertTrue(session.TrySetCellValue(session.Rows[rowIndex], diffuseDescriptor, nextValue, out string errorMessage), nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded));
                        AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded));
                    }

                    view.NotifySessionChanged();
                    AssertEqual(changedRowCount, view.DirtyRowCount, nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded));

                    view.SetRowFilter(BulkMaterialRowFilter.DirtyFiles);
                    AssertEqual(changedRowCount, view.VisibleRows.Count, nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded));
                    view.SetRowFilter(BulkMaterialRowFilter.AllFiles);
                    view.SetSortKey(cycle % 2 == 0 ? BulkMaterialSortKey.LastModified : BulkMaterialSortKey.ByAddition);
                    view.SetSortDirection(cycle % 3 == 0 ? BulkMaterialSortDirection.Descending : BulkMaterialSortDirection.Ascending);
                    view.SetGroupByFolder(cycle % 2 == 0);

                    DataGridView grid = GetDescendants(host).OfType<DataGridView>().Single();
                    int firstEditableColumn = grid.Columns
                        .Cast<DataGridViewColumn>()
                        .Where(column => column.Visible && !column.Frozen)
                        .Select(column => column.Index)
                        .First();
                    for (int scrollPass = 0; scrollPass < 20; scrollPass++)
                    {
                        int rowIndex = (scrollPass * 5) % grid.Rows.Count;
                        grid.CurrentCell = grid.Rows[rowIndex].Cells[firstEditableColumn];
                        if (rowIndex < grid.Rows.Count - 1)
                            grid.FirstDisplayedScrollingRowIndex = rowIndex;
                        grid.Invalidate();
                        grid.Update();
                        host.Update();
                        Application.DoEvents();
                    }

                    IReadOnlyList<FieldCopyResult> saveResults = session.ApplyChanges(backupBeforeWrite: cycle % 2 == 0, CreateConfig(font));
                    view.NotifySessionChanged();
                    AssertEqual(changedRowCount, saveResults.Count(result => result.Status == FieldCopyStatus.Success), nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded));

                    int externallyChangedRow = (cycle * 7) % paths.Length;
                    TestFileSupport.SaveMaterial(paths[externallyChangedRow], new BGSM
                    {
                        Version = 2,
                        DiffuseTexture = $"textures\\external_soak_{cycle}.dds",
                        NormalTexture = "textures\\bulk_n.dds"
                    });
                    IReadOnlyList<BulkMaterialEditRow> reloadedRows = session.ReloadRows(new[] { session.Rows[externallyChangedRow] });
                    view.NotifySessionChanged();
                    AssertEqual(1, reloadedRows.Count, nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded));
                    AssertEqual($"textures\\external_soak_{cycle}.dds", Convert.ToString(reloadedRows[0].GetCell(diffuseDescriptor).CurrentValue), nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded));

                    view.ClearSession();
                    Application.DoEvents();
                }

                ForceFullCollection();
                Application.DoEvents();
                UiResourceSnapshot after = CaptureUiResources();
                AssertResourceGrowthWithinThreshold(
                    baseline,
                    after,
                    new UiResourceThresholds(16 * 1024 * 1024, 64 * 1024 * 1024, 80, 12, 12),
                    nameof(Soak_BulkEditorWorkflowLoopKeepsResourcesBounded));
            });
        }

        private static void Soak_BackupRecoveryLoopPreservesRecoverability()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "recovery.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\version_000.dds";
                material.NormalTexture = "textures\\base_n.dds";
            });

            var config = new Config
            {
                CreateBackupsByDefault = true,
                MaxBackupsPerFile = 0,
                MaxBackupFolderMegabytes = 0
            };

            const int saveCycles = 16;
            for (int cycle = 1; cycle <= saveCycles; cycle++)
            {
                MaterialFilePersistence.SaveMaterial(path, new BGSM
                {
                    Version = 2,
                    DiffuseTexture = $"textures\\version_{cycle.ToString("000", CultureInfo.InvariantCulture)}.dds",
                    NormalTexture = "textures\\base_n.dds"
                }, asJson: false, backupExisting: true, config);
            }

            IReadOnlyList<MaterialBackupEntry> backups = MaterialBackupService.GetAvailableBackups(path);
            AssertEqual(saveCycles, backups.Count, nameof(Soak_BackupRecoveryLoopPreservesRecoverability));

            for (int cycle = 0; cycle < 8; cycle++)
            {
                FieldCopyResult oldest = MaterialBackupService.RestoreOldest(path, config, backupCurrentBeforeRestore: false);
                AssertEqual(FieldCopyStatus.Success, oldest.Status, nameof(Soak_BackupRecoveryLoopPreservesRecoverability));
                AssertEqual("textures\\version_000.dds", TestFileSupport.LoadBgsm(path).DiffuseTexture, nameof(Soak_BackupRecoveryLoopPreservesRecoverability));

                FieldCopyResult newest = MaterialBackupService.RestoreNewest(path, config, backupCurrentBeforeRestore: false);
                AssertEqual(FieldCopyStatus.Success, newest.Status, nameof(Soak_BackupRecoveryLoopPreservesRecoverability));
                AssertEqual($"textures\\version_{(saveCycles - 1).ToString("000", CultureInfo.InvariantCulture)}.dds", TestFileSupport.LoadBgsm(path).DiffuseTexture, nameof(Soak_BackupRecoveryLoopPreservesRecoverability));
            }
        }

        private static void Soak_GenerateEditOverwriteRoundTripRemainsStable()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string root = outputDirectory.Path;
            MaterialFieldDescriptor diffuseDescriptor = GetBgsmDescriptor(ControlNames.Diffuse);
            MaterialFieldDescriptor grayscaleDescriptor = GetBgsmDescriptor(ControlNames.GrayscaleToPaletteScale);
            var overwriteTool = new FieldOverwriteTool();
            var sourceState = new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\overwrite_source.dds",
                GrayscaleToPaletteScale = 2.0f
            };

            const int cycleCount = 8;
            for (int cycle = 0; cycle < cycleCount; cycle++)
            {
                string cycleDirectory = Path.Combine(root, $"cycle_{cycle.ToString("00", CultureInfo.InvariantCulture)}");
                Directory.CreateDirectory(cycleDirectory);
                var template = new BGSM
                {
                    Version = 2,
                    DiffuseTexture = "textures\\template_{indexNN}.dds",
                    NormalTexture = "textures\\template_n.dds",
                    GrayscaleToPaletteScale = 0.5f
                };

                var generationOptions = new MaterialVariationOptions
                {
                    OutputPattern = Path.Combine(cycleDirectory, "variant_{indexNN}.bgsm"),
                    Fields = new[]
                    {
                        new MaterialVariationFieldAssignment(diffuseDescriptor, $"textures\\generated_{cycle}_{{indexNN}}.dds")
                    },
                    AdvancedVariant = new AdvancedVariantOptions
                    {
                        Layers = new[]
                        {
                            new AdvancedVariantLayerDefinition("Layer1", Enumerable.Range(1, 6).ToArray()),
                            new AdvancedVariantLayerDefinition("Layer2", Enumerable.Range(1, 6).ToArray())
                        }
                    }
                };

                IReadOnlyList<FieldCopyResult> generationResults = MaterialVariationGenerator.Generate(template, generationOptions, serializeAsJson: true);
                AssertEqual(36, generationResults.Count(result => result.Status == FieldCopyStatus.Success), nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable));

                string[] generatedPaths = Directory.GetFiles(cycleDirectory, "*.bgsm").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
                BulkMaterialEditSession session = BulkMaterialEditSession.Create(MaterialType.Material, generatedPaths);
                for (int rowIndex = 0; rowIndex < 10; rowIndex++)
                {
                    string nextValue = $"textures\\edited_{cycle}_{rowIndex.ToString("00", CultureInfo.InvariantCulture)}.dds";
                    AssertTrue(session.TrySetCellValue(session.Rows[rowIndex], diffuseDescriptor, nextValue, out string errorMessage), nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable));
                    AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable));
                }

                IReadOnlyList<FieldCopyResult> editResults = session.ApplyChanges(backupBeforeWrite: false);
                AssertEqual(10, editResults.Count(result => result.Status == FieldCopyStatus.Success), nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable));

                IReadOnlyList<FieldCopyResult> overwriteResults = overwriteTool.Run(sourceState, new FieldOverwriteOptions
                {
                    Descriptors = new[] { diffuseDescriptor, grayscaleDescriptor },
                    TargetFiles = generatedPaths,
                    BackupBeforeWrite = false,
                    IterativeOptions = new IterativeFieldOverwriteOptions
                    {
                        StartIndex = 200 + cycle,
                        Count = 12,
                        Step = 1,
                        Assignments = new IterativeFieldAssignment[]
                        {
                            new(diffuseDescriptor, $"textures\\overwrite_{cycle}_{{index:000}}.dds"),
                            new(grayscaleDescriptor, 2.0f, 0.25f)
                        }
                    }
                });

                AssertEqual(12, overwriteResults.Count(result => result.Status == FieldCopyStatus.Success), nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable));
                AssertEqual(generatedPaths.Length - 12, overwriteResults.Count(result => result.Status == FieldCopyStatus.Skipped), nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable));

                AssertContains(File.ReadAllText(generatedPaths[0]), $"overwrite_{cycle}_", nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable));
                AssertContains(File.ReadAllText(generatedPaths[^1]), $"generated_{cycle}_", nameof(Soak_GenerateEditOverwriteRoundTripRemainsStable));
            }
        }

        private static MaterialFieldDescriptor GetBgsmDescriptor(string label)
        {
            return MaterialFieldRegistry.GetDescriptors(new BGSM()).Single(descriptor => descriptor.Label == label);
        }

        private static void CreateAndDisposeMainForm(Font font)
        {
            using var form = new Main(CreateConfig(font));
            IntPtr formHandle = form.Handle;
            form.PerformLayout();
        }

        private static void CreateAndDisposeSearchDialog(IReadOnlyList<MaterialFieldDescriptor> descriptors, bool replaceMode)
        {
            BulkFindReplaceAction capturedAction = BulkFindReplaceAction.FindNext;
            BulkFindReplaceRequest capturedRequest = null;

            using var dialog = new FindReplaceDialog(
                descriptors,
                new BulkFindReplaceRequest
                {
                    Scope = BulkFindScope.ByField,
                    ValueType = BulkFindValueType.Text,
                    FindText = "bulk",
                    ReplaceText = replaceMode ? "stable" : string.Empty,
                    FieldLabels = new List<string> { ControlNames.Diffuse }
                },
                replaceMode,
                (action, request) =>
                {
                    capturedAction = action;
                    capturedRequest = request?.Clone();
                });
            IntPtr handle = dialog.Handle;
            dialog.Show();
            Application.DoEvents();

            RadioButton numberButton = GetDescendants(dialog).OfType<RadioButton>().Single(button => button.Text == "Number");
            RadioButton checkboxButton = GetDescendants(dialog).OfType<RadioButton>().Single(button => button.Text == "Checkbox");
            RadioButton textButton = GetDescendants(dialog).OfType<RadioButton>().Single(button => button.Text == "Text");
            numberButton.Checked = true;
            checkboxButton.Checked = true;
            textButton.Checked = true;

            CheckedListBox fieldList = GetDescendants(dialog).OfType<CheckedListBox>().Single();
            if (fieldList.Items.Count > 0)
                fieldList.SetItemChecked(0, true);

            TextBox[] textBoxes = GetDescendants(dialog).OfType<TextBox>().ToArray();
            textBoxes[0].Text = "bulk";
            if (replaceMode && textBoxes.Length > 1)
                textBoxes[1].Text = "stable";

            Button actionButton = GetDescendants(dialog).OfType<Button>().Single(button => button.Text == (replaceMode ? "Replace All" : "Find Next"));
            actionButton.PerformClick();
            Application.DoEvents();

            AssertEqual(replaceMode ? BulkFindReplaceAction.ReplaceAll : BulkFindReplaceAction.FindNext, capturedAction, nameof(CreateAndDisposeSearchDialog));
            AssertTrue(capturedRequest != null, nameof(CreateAndDisposeSearchDialog));
            dialog.Close();
        }

        private static void CreateAndDisposeBackupRecoveryDialog(IReadOnlyList<MaterialBackupEntry> backups)
        {
            using var dialog = new BackupRecoveryDialog(backups, "Recovery Stress");
            IntPtr handle = dialog.Handle;
            dialog.Show();
            Application.DoEvents();

            DataGridView grid = GetDescendants(dialog).OfType<DataGridView>().Single();
            grid.ClearSelection();
            grid.Rows[0].Selected = true;
            grid.CurrentCell = grid.Rows[0].Cells[0];
            Application.DoEvents();

            AssertTrue(dialog.SelectedBackup != null, nameof(CreateAndDisposeBackupRecoveryDialog));
            Button recoverButton = GetDescendants(dialog).OfType<Button>().Single(button => button.Text == "Recover Selected");
            recoverButton.PerformClick();
            Application.DoEvents();
        }

        private static void CreateAndDisposeBulkEditor(Font font, IReadOnlyList<string> paths)
        {
            BulkMaterialEditSession session = BulkMaterialEditSession.Create(MaterialType.Material, paths);
            MaterialFieldDescriptor diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
            session.TrySetCellValue(session.Rows[0], diffuseDescriptor, "textures\\dirty_stress.dds", out _);

            using var host = new Form();
            using var view = new BulkMaterialEditorView();
            host.Controls.Add(view);
            IntPtr hostHandle = host.Handle;
            IntPtr viewHandle = view.Handle;

            view.Initialize(session, CreateConfig(font), backupBeforeWrite: true);
            view.SetSortKey(BulkMaterialSortKey.LastModified);
            view.SetSortDirection(BulkMaterialSortDirection.Descending);
            view.SetGroupByFolder(true);
            view.SetRowFilter(BulkMaterialRowFilter.DirtyFiles);
            view.SetRowFilter(BulkMaterialRowFilter.AllFiles);
            view.SetSortKey(BulkMaterialSortKey.ByAddition);
            view.SetSortDirection(BulkMaterialSortDirection.Ascending);
            view.SetGroupByFolder(false);
            view.ClearSession();
        }

        private static string[] CreateBulkStressFiles(string rootDirectory, int fileCount)
        {
            string materialsRoot = Path.Combine(rootDirectory, "Materials");
            string alphaDirectory = Path.Combine(materialsRoot, "Alpha");
            string betaDirectory = Path.Combine(materialsRoot, "Beta");
            Directory.CreateDirectory(alphaDirectory);
            Directory.CreateDirectory(betaDirectory);

            var paths = new List<string>(fileCount);
            for (int i = 0; i < fileCount; i++)
            {
                string indexText = i.ToString("000", CultureInfo.InvariantCulture);
                string directory = i % 2 == 0 ? alphaDirectory : betaDirectory;
                string path = TestFileSupport.CreateBgsm(directory, $"bulk_{indexText}.bgsm", material =>
                {
                    material.Version = 2;
                    material.DiffuseTexture = $"textures\\bulk_{indexText}.dds";
                    material.NormalTexture = "textures\\bulk_n.dds";
                });

                File.SetLastWriteTimeUtc(path, new DateTime(2024, 1, 1).AddMinutes(i));
                paths.Add(path);
            }

            return paths.ToArray();
        }

        private static MaterialBackupEntry[] CreateSyntheticBackupEntries(int count)
        {
            return Enumerable.Range(0, count)
                .Select(index => new MaterialBackupEntry(
                    $"materials\\folder_{index % 6}\\item_{index.ToString("000", CultureInfo.InvariantCulture)}.bgsm",
                    $"backup\\{index.ToString("000", CultureInfo.InvariantCulture)}.bak",
                    $"{index.ToString("000", CultureInfo.InvariantCulture)}.bak",
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(index),
                    2048 + index,
                    isLegacy: index % 9 == 0,
                    isRetainedOriginal: index % 13 == 0))
                .ToArray();
        }

        private static string[] GetBackupFiles(string sourcePath)
        {
            return MaterialBackupService.GetAvailableBackups(sourcePath)
                .Where(entry => !entry.IsLegacy && !entry.IsRetainedOriginal)
                .Select(entry => entry.BackupPath)
                .ToArray();
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

        private static UiResourceSnapshot CaptureUiResources()
        {
            ForceFullCollection();
            using Process process = Process.GetCurrentProcess();
            process.Refresh();
            return new UiResourceSnapshot(
                GC.GetTotalMemory(forceFullCollection: true),
                process.PrivateMemorySize64,
                process.HandleCount,
                NativeMethods.GetGuiResources(process.Handle, NativeMethods.GdiObjects),
                NativeMethods.GetGuiResources(process.Handle, NativeMethods.UserObjects));
        }

        private static void ForceFullCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static void AssertResourceGrowthWithinThreshold(
            UiResourceSnapshot baseline,
            UiResourceSnapshot after,
            UiResourceThresholds thresholds,
            string testName)
        {
            long managedDelta = after.ManagedHeapBytes - baseline.ManagedHeapBytes;
            long privateDelta = after.PrivateBytes - baseline.PrivateBytes;
            int handleDelta = after.HandleCount - baseline.HandleCount;
            int gdiDelta = after.GdiObjectCount - baseline.GdiObjectCount;
            int userDelta = after.UserObjectCount - baseline.UserObjectCount;

            AssertLessThanOrEqual(thresholds.ManagedHeapBytes, managedDelta, $"{testName} managed heap delta");
            AssertLessThanOrEqual(thresholds.PrivateBytes, privateDelta, $"{testName} private bytes delta");
            AssertLessThanOrEqual(thresholds.HandleCount, handleDelta, $"{testName} handle delta");
            AssertLessThanOrEqual(thresholds.GdiObjectCount, gdiDelta, $"{testName} GDI object delta");
            AssertLessThanOrEqual(thresholds.UserObjectCount, userDelta, $"{testName} USER object delta");

            Console.WriteLine(
                $"{testName} resources: managed={managedDelta / 1024} KB, private={privateDelta / 1024} KB, handles={handleDelta}, gdi={gdiDelta}, user={userDelta}.");
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

        private static void RunScenario(string name, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            Console.WriteLine($"{name} passed in {stopwatch.ElapsedMilliseconds} ms.");
        }

        private static void AssertContains(string value, string expectedSubstring, string testName)
        {
            if (value == null || value.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException($"{testName} failed: expected '{expectedSubstring}' to be present.");
        }

        private static void AssertEqual<T>(T expected, T actual, string testName)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException($"{testName} failed: expected '{expected}', got '{actual}'.");
        }

        private static void AssertLessThanOrEqual(long expectedMaximum, long actualValue, string testName)
        {
            if (actualValue > expectedMaximum)
                throw new InvalidOperationException($"{testName} failed: expected <= '{expectedMaximum}', got '{actualValue}'.");
        }

        private static void AssertTrue(bool value, string testName)
        {
            if (!value)
                throw new InvalidOperationException($"{testName} failed.");
        }

        private readonly record struct UiResourceSnapshot(
            long ManagedHeapBytes,
            long PrivateBytes,
            int HandleCount,
            int GdiObjectCount,
            int UserObjectCount);

        private readonly record struct UiResourceThresholds(
            long ManagedHeapBytes,
            long PrivateBytes,
            int HandleCount,
            int GdiObjectCount,
            int UserObjectCount);

        private static class NativeMethods
        {
            public const uint GdiObjects = 0;
            public const uint UserObjects = 1;

            [DllImport("user32.dll", SetLastError = true)]
            public static extern int GetGuiResources(IntPtr hProcess, uint uiFlags);
        }
    }
}
