using Material_Editor.Services;
using MaterialLib;
using System;
using System.IO;
using System.Linq;

namespace MaterialEditor.Tests
{
    internal static class FieldOverwriteAdvancedTests
    {
        public static void RunAll()
        {
            Planner_SortsTargetsAndAppliesStep();
            Planner_BlankIndexTokFallsBackToNumericIndex();
            Planner_ManualDisableSkipsSelectedTarget();
            Run_AdvancedModeUpdatesOnlySteppedTargets();
            Run_LegacyModeStillCopiesSelectedFields();
        }

        private static void Planner_SortsTargetsAndAppliesStep()
        {
            var contexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialType.Material,
                new[]
                {
                    @"C:\temp\d.bgsm",
                    @"C:\temp\b.bgsm",
                    @"C:\temp\a.bgsm",
                    @"C:\temp\wrong.bgem",
                    @"C:\temp\c.bgsm"
                },
                new IterativeFieldOverwriteOptions
                {
                    StartIndex = 5,
                    Count = 2,
                    Step = 2,
                    Targets = new[]
                    {
                        new IterativeTargetOverride(@"C:\temp\a.bgsm", "first"),
                        new IterativeTargetOverride(@"C:\temp\c.bgsm", "third")
                    }
                });

            AssertSequenceEqual(
                new[] { @"C:\temp\a.bgsm", @"C:\temp\b.bgsm", @"C:\temp\c.bgsm", @"C:\temp\d.bgsm", @"C:\temp\wrong.bgem" },
                contexts.Select(context => context.TargetPath).ToArray(),
                nameof(Planner_SortsTargetsAndAppliesStep));
            AssertSequenceEqual(
                new[] { "True", "False", "True", "False", "False" },
                contexts.Select(context => context.WillApply.ToString()).ToArray(),
                nameof(Planner_SortsTargetsAndAppliesStep));
            AssertSequenceEqual(
                new[] { "5", "", "7", "", "" },
                contexts.Select(context => context.Index).ToArray(),
                nameof(Planner_SortsTargetsAndAppliesStep));
        }

        private static void Planner_BlankIndexTokFallsBackToNumericIndex()
        {
            var contexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialType.Material,
                new[] { @"C:\temp\a.bgsm", @"C:\temp\b.bgsm" },
                new IterativeFieldOverwriteOptions
                {
                    StartIndex = 3,
                    Count = 2,
                    Step = 1,
                    Targets = new[]
                    {
                        new IterativeTargetOverride(@"C:\temp\b.bgsm", "named")
                    }
                });

            AssertEqual("3", contexts[0].IndexTok, nameof(Planner_BlankIndexTokFallsBackToNumericIndex));
            AssertTrue(contexts[0].IndexTokFallback, nameof(Planner_BlankIndexTokFallsBackToNumericIndex));
            AssertEqual("named", contexts[1].IndexTok, nameof(Planner_BlankIndexTokFallsBackToNumericIndex));
            AssertTrue(!contexts[1].IndexTokFallback, nameof(Planner_BlankIndexTokFallsBackToNumericIndex));
        }

        private static void Planner_ManualDisableSkipsSelectedTarget()
        {
            var contexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialType.Material,
                new[] { @"C:\temp\a.bgsm", @"C:\temp\b.bgsm", @"C:\temp\c.bgsm" },
                new IterativeFieldOverwriteOptions
                {
                    StartIndex = 1,
                    Count = 3,
                    Step = 1,
                    Targets = new[]
                    {
                        new IterativeTargetOverride(@"C:\temp\b.bgsm", string.Empty, isEnabled: false)
                    }
                });

            AssertSequenceEqual(
                new[] { "True", "False", "True" },
                contexts.Select(context => context.WillApply.ToString()).ToArray(),
                nameof(Planner_ManualDisableSkipsSelectedTarget));
            AssertTrue(contexts[1].SelectedByIteration, nameof(Planner_ManualDisableSkipsSelectedTarget));
            AssertTrue(!contexts[1].ManuallyEnabled, nameof(Planner_ManualDisableSkipsSelectedTarget));
            AssertEqual("2", contexts[1].Index, nameof(Planner_ManualDisableSkipsSelectedTarget));
        }

        private static void Run_AdvancedModeUpdatesOnlySteppedTargets()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            string aPath = TestFileSupport.CreateBgsm(outputPath, "a.bgsm", material =>
            {
                material.DiffuseTexture = "textures\\orig_a.dds";
                material.GrayscaleToPaletteScale = 0.5f;
            });
            string bPath = TestFileSupport.CreateBgsm(outputPath, "b.bgsm", material =>
            {
                material.DiffuseTexture = "textures\\orig_b.dds";
                material.GrayscaleToPaletteScale = 0.5f;
            });
            string cPath = TestFileSupport.CreateBgsm(outputPath, "c.bgsm", material =>
            {
                material.DiffuseTexture = "textures\\orig_c.dds";
                material.GrayscaleToPaletteScale = 0.5f;
            });
            string dPath = TestFileSupport.CreateBgsm(outputPath, "d.bgsm", material =>
            {
                material.DiffuseTexture = "textures\\orig_d.dds";
                material.GrayscaleToPaletteScale = 0.5f;
            });
            string wrongPath = TestFileSupport.CreateBgem(outputPath, "wrong.bgem", material => material.BaseTexture = "textures\\effect.dds");

            var diffuseDescriptor = GetBgsmDescriptor(ControlNames.Diffuse);
            var grayscaleDescriptor = GetBgsmDescriptor(ControlNames.GrayscaleToPaletteScale);
            var tool = new FieldOverwriteTool();
            var sourceState = new BGSM
            {
                DiffuseTexture = "textures\\source.dds",
                GrayscaleToPaletteScale = 1.0f
            };

            var results = tool.Run(sourceState, new FieldOverwriteOptions
            {
                Descriptors = new[] { diffuseDescriptor, grayscaleDescriptor },
                TargetFiles = new[] { dPath, wrongPath, bPath, aPath, cPath },
                IterativeOptions = new IterativeFieldOverwriteOptions
                {
                    StartIndex = 5,
                    Count = 2,
                    Step = 2,
                    Assignments = new IterativeFieldAssignment[]
                    {
                        new(diffuseDescriptor, "textures\\iter_{indexTok}_{index:00}.dds"),
                        new(grayscaleDescriptor, 1.5f, 0.25f)
                    },
                    Targets = new[]
                    {
                        new IterativeTargetOverride(aPath, "first"),
                        new IterativeTargetOverride(cPath, "third")
                    }
                }
            });

            AssertEqual(5, results.Count, nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));
            AssertEqual(2, results.Count(result => result.Status == FieldCopyStatus.Success), nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));
            AssertEqual(3, results.Count(result => result.Status == FieldCopyStatus.Skipped), nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));

            var aMaterial = TestFileSupport.LoadBgsm(aPath);
            var bMaterial = TestFileSupport.LoadBgsm(bPath);
            var cMaterial = TestFileSupport.LoadBgsm(cPath);
            var dMaterial = TestFileSupport.LoadBgsm(dPath);

            AssertEqual("textures\\iter_first_05.dds", aMaterial.DiffuseTexture, nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));
            AssertEqual(1.5f, aMaterial.GrayscaleToPaletteScale, nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));
            AssertEqual("textures\\orig_b.dds", bMaterial.DiffuseTexture, nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));
            AssertEqual("textures\\iter_third_07.dds", cMaterial.DiffuseTexture, nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));
            AssertEqual(1.75f, cMaterial.GrayscaleToPaletteScale, nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));
            AssertEqual("textures\\orig_d.dds", dMaterial.DiffuseTexture, nameof(Run_AdvancedModeUpdatesOnlySteppedTargets));
        }

        private static void Run_LegacyModeStillCopiesSelectedFields()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            string targetPath = TestFileSupport.CreateBgsm(outputPath, "target.bgsm", material =>
            {
                material.DiffuseTexture = "textures\\before.dds";
                material.GrayscaleToPaletteScale = 0.5f;
            });
            var diffuseDescriptor = GetBgsmDescriptor(ControlNames.Diffuse);
            var tool = new FieldOverwriteTool();
            var sourceState = new BGSM
            {
                DiffuseTexture = "textures\\after.dds"
            };

            var results = tool.Run(sourceState, new[] { diffuseDescriptor }, new[] { targetPath }, backupBeforeWrite: false);

            AssertEqual(FieldCopyStatus.Success, results.Single().Status, nameof(Run_LegacyModeStillCopiesSelectedFields));
            AssertEqual("textures\\after.dds", TestFileSupport.LoadBgsm(targetPath).DiffuseTexture, nameof(Run_LegacyModeStillCopiesSelectedFields));
        }

        private static MaterialFieldDescriptor GetBgsmDescriptor(string label)
        {
            return MaterialFieldRegistry.GetDescriptors(new BGSM()).Single(descriptor => descriptor.Label == label);
        }

        private static void AssertEqual<T>(T expected, T actual, string testName)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException($"{testName} failed: expected '{expected}', got '{actual}'.");
        }

        private static void AssertTrue(bool value, string testName)
        {
            if (!value)
                throw new InvalidOperationException($"{testName} failed.");
        }

        private static void AssertSequenceEqual(string[] expected, string[] actual, string testName)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException($"{testName} failed: sequences did not match.");
        }
    }
}
