using Material_Editor.Services;
using MaterialLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MaterialEditor.Tests
{
    internal static class BulkMaterialEditSessionTests
    {
        public static void RunAll()
        {
            Registry_TypeDescriptorsIncludeVersionSpecificFields();
            Session_SortsDistinctRowsAndFlagsMixedTypes();
            Descriptor_ParsesAndFormatsSpecialTypes();
            Session_TracksUnsupportedVersionSpecificFields();
            Session_TracksDirtyStateForParentBooleanFields();
            Session_ApplyWritesDirtyRowsAndCreatesBackups();
            Session_ApplySelectedWritesOnlySelectedDirtyRows();
            Session_AddFilesKeepsDirtyStateAndDedupes();
            Session_ApplyFailureLeavesRowDirty();
            SelectionService_SplitsMixedTypes();
            Preferences_SerializeAndResolvePresets();
            Export_UsesPatternTokensForCurrentRows();
        }

        private static void Registry_TypeDescriptorsIncludeVersionSpecificFields()
        {
            var materialDescriptors = MaterialFieldRegistry.GetDescriptors(MaterialType.Material);
            AssertTrue(materialDescriptors.Any(descriptor => descriptor.Label == ControlNames.LumEmittance), nameof(Registry_TypeDescriptorsIncludeVersionSpecificFields));
            AssertTrue(materialDescriptors.Any(descriptor => descriptor.Label == ControlNames.Environment), nameof(Registry_TypeDescriptorsIncludeVersionSpecificFields));

            var effectDescriptors = MaterialFieldRegistry.GetDescriptors(MaterialType.Effect);
            AssertTrue(effectDescriptors.Any(descriptor => descriptor.Label == ControlNames.GlassBlurScaleFactor), nameof(Registry_TypeDescriptorsIncludeVersionSpecificFields));
        }

        private static void Session_SortsDistinctRowsAndFlagsMixedTypes()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            string aPath = TestFileSupport.CreateBgsm(outputPath, "b.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\a.dds";
            });
            string bPath = TestFileSupport.CreateBgsm(outputPath, "a.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\b.dds";
            });
            string wrongPath = TestFileSupport.CreateBgem(outputPath, "wrong.bgem", material =>
            {
                material.Version = 11;
                material.BaseTexture = "textures\\wrong.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { aPath, wrongPath, bPath, aPath });

            AssertSequenceEqual(
                new[]
                {
                    MaterialFilePersistence.NormalizePath(bPath),
                    MaterialFilePersistence.NormalizePath(aPath),
                    MaterialFilePersistence.NormalizePath(wrongPath)
                },
                session.Rows.Select(row => row.FilePath).ToArray(),
                nameof(Session_SortsDistinctRowsAndFlagsMixedTypes));

            AssertTrue(!session.Rows[0].HasLoadError, nameof(Session_SortsDistinctRowsAndFlagsMixedTypes));
            AssertTrue(session.Rows[2].HasLoadError, nameof(Session_SortsDistinctRowsAndFlagsMixedTypes));
        }

        private static void Descriptor_ParsesAndFormatsSpecialTypes()
        {
            var materialDescriptors = MaterialFieldRegistry.GetDescriptors(MaterialType.Material);

            var colorDescriptor = materialDescriptors.Single(descriptor => descriptor.Label == ControlNames.SpecularColor);
            AssertTrue(colorDescriptor.TryParseTextValue("#112233", out object colorValue, out string colorError), nameof(Descriptor_ParsesAndFormatsSpecialTypes));
            AssertEqual(string.Empty, colorError ?? string.Empty, nameof(Descriptor_ParsesAndFormatsSpecialTypes));
            AssertEqual("#112233", colorDescriptor.FormatValue(colorValue), nameof(Descriptor_ParsesAndFormatsSpecialTypes));

            var flagsDescriptor = materialDescriptors.Single(descriptor => descriptor.Label == ControlNames.MaskWrites);
            AssertTrue(flagsDescriptor.TryParseTextValue("ALBEDO, GLOSS", out object flagsValue, out string flagsError), nameof(Descriptor_ParsesAndFormatsSpecialTypes));
            AssertEqual(string.Empty, flagsError ?? string.Empty, nameof(Descriptor_ParsesAndFormatsSpecialTypes));
            AssertEqual("ALBEDO, GLOSS", flagsDescriptor.FormatValue(flagsValue), nameof(Descriptor_ParsesAndFormatsSpecialTypes));

            var numberDescriptor = materialDescriptors.Single(descriptor => descriptor.Label == ControlNames.Alpha);
            AssertTrue(numberDescriptor.TryParseTextValue("1.25", out object numberValue, out string numberError), nameof(Descriptor_ParsesAndFormatsSpecialTypes));
            AssertEqual(string.Empty, numberError ?? string.Empty, nameof(Descriptor_ParsesAndFormatsSpecialTypes));
            AssertEqual("1.25", numberDescriptor.FormatValue(numberValue), nameof(Descriptor_ParsesAndFormatsSpecialTypes));
        }

        private static void Session_TracksUnsupportedVersionSpecificFields()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            string oldPath = TestFileSupport.CreateBgsm(outputPath, "old.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\old.dds";
            });
            string newPath = TestFileSupport.CreateBgsm(outputPath, "new.bgsm", material =>
            {
                material.Version = 13;
                material.DiffuseTexture = "textures\\new.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { oldPath, newPath });
            var lumDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.LumEmittance);
            var oldRow = session.Rows.Single(row => row.FilePath == MaterialFilePersistence.NormalizePath(oldPath));
            var newRow = session.Rows.Single(row => row.FilePath == MaterialFilePersistence.NormalizePath(newPath));

            AssertEqual(false, oldRow.GetCell(lumDescriptor).IsSupported, nameof(Session_TracksUnsupportedVersionSpecificFields));
            AssertEqual(true, newRow.GetCell(lumDescriptor).IsSupported, nameof(Session_TracksUnsupportedVersionSpecificFields));
        }

        private static void Session_TracksDirtyStateForParentBooleanFields()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "env.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\env.dds";
            });
            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });
            var row = session.Rows.Single();
            var descriptor = session.AllDescriptors.Single(field => field.Label == ControlNames.EnvironmentMapping);

            AssertEqual(false, row.IsDirty, nameof(Session_TracksDirtyStateForParentBooleanFields));
            AssertTrue(session.TrySetCellValue(row, descriptor, true, out string errorMessage), nameof(Session_TracksDirtyStateForParentBooleanFields));
            AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(Session_TracksDirtyStateForParentBooleanFields));
            AssertEqual(true, row.GetCell(descriptor).CurrentValue, nameof(Session_TracksDirtyStateForParentBooleanFields));
            AssertTrue(row.IsDirty, nameof(Session_TracksDirtyStateForParentBooleanFields));
        }

        private static void Session_ApplyWritesDirtyRowsAndCreatesBackups()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            string aPath = TestFileSupport.CreateBgsm(outputPath, "a.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\a.dds";
            });
            string bPath = TestFileSupport.CreateBgsm(outputPath, "b.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\b.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { aPath, bPath });
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);

            AssertTrue(session.TrySetCellValue(session.Rows[0], diffuseDescriptor, "textures\\updated.dds", out string errorMessage), nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
            AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));

            var results = session.ApplyChanges(backupBeforeWrite: true);

            AssertEqual(FieldCopyStatus.Success, results.Single(result => result.TargetPath == MaterialFilePersistence.NormalizePath(aPath)).Status, nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
            AssertEqual(FieldCopyStatus.Skipped, results.Single(result => result.TargetPath == MaterialFilePersistence.NormalizePath(bPath)).Status, nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
            AssertTrue(File.Exists($"{aPath}.bak"), nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
            AssertEqual("textures\\updated.dds", TestFileSupport.LoadBgsm(aPath).DiffuseTexture, nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
            AssertTrue(!session.Rows[0].IsDirty, nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
        }

        private static void Session_ApplyFailureLeavesRowDirty()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "locked.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\locked.dds";
            });
            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
            AssertTrue(session.TrySetCellValue(session.Rows[0], diffuseDescriptor, "textures\\changed.dds", out _), nameof(Session_ApplyFailureLeavesRowDirty));

            using var lockStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            var results = session.ApplyChanges(backupBeforeWrite: false);

            AssertEqual(FieldCopyStatus.Failed, results.Single().Status, nameof(Session_ApplyFailureLeavesRowDirty));
            AssertTrue(session.Rows[0].IsDirty, nameof(Session_ApplyFailureLeavesRowDirty));
        }

        private static void Session_ApplySelectedWritesOnlySelectedDirtyRows()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            string aPath = TestFileSupport.CreateBgsm(outputPath, "a.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\a.dds";
            });
            string bPath = TestFileSupport.CreateBgsm(outputPath, "b.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\b.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { aPath, bPath });
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
            AssertTrue(session.TrySetCellValue(session.Rows[0], diffuseDescriptor, "textures\\a_changed.dds", out _), nameof(Session_ApplySelectedWritesOnlySelectedDirtyRows));
            AssertTrue(session.TrySetCellValue(session.Rows[1], diffuseDescriptor, "textures\\b_changed.dds", out _), nameof(Session_ApplySelectedWritesOnlySelectedDirtyRows));

            var results = session.ApplySelectedChanges(new[] { session.Rows[1] }, backupBeforeWrite: false);

            AssertEqual(1, results.Count, nameof(Session_ApplySelectedWritesOnlySelectedDirtyRows));
            AssertEqual(FieldCopyStatus.Success, results.Single().Status, nameof(Session_ApplySelectedWritesOnlySelectedDirtyRows));
            AssertEqual("textures\\a.dds", TestFileSupport.LoadBgsm(aPath).DiffuseTexture, nameof(Session_ApplySelectedWritesOnlySelectedDirtyRows));
            AssertEqual("textures\\b_changed.dds", TestFileSupport.LoadBgsm(bPath).DiffuseTexture, nameof(Session_ApplySelectedWritesOnlySelectedDirtyRows));
            AssertTrue(session.Rows[0].IsDirty, nameof(Session_ApplySelectedWritesOnlySelectedDirtyRows));
            AssertTrue(!session.Rows[1].IsDirty, nameof(Session_ApplySelectedWritesOnlySelectedDirtyRows));
        }

        private static void Session_AddFilesKeepsDirtyStateAndDedupes()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            string aPath = TestFileSupport.CreateBgsm(outputPath, "a.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\a.dds";
            });
            string bPath = TestFileSupport.CreateBgsm(outputPath, "b.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\b.dds";
            });
            string cPath = TestFileSupport.CreateBgsm(outputPath, "c.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\c.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { aPath, bPath });
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
            AssertTrue(session.TrySetCellValue(session.Rows[0], diffuseDescriptor, "textures\\updated.dds", out _), nameof(Session_AddFilesKeepsDirtyStateAndDedupes));

            BulkMaterialSessionAddResult addResult = session.AddFiles(new[] { bPath, cPath });

            AssertEqual(1, addResult.DuplicateCount, nameof(Session_AddFilesKeepsDirtyStateAndDedupes));
            AssertEqual(3, session.Rows.Count, nameof(Session_AddFilesKeepsDirtyStateAndDedupes));
            AssertTrue(session.Rows.Any(row => row.FilePath == MaterialFilePersistence.NormalizePath(cPath)), nameof(Session_AddFilesKeepsDirtyStateAndDedupes));
            AssertTrue(session.Rows.Any(row => row.IsDirty), nameof(Session_AddFilesKeepsDirtyStateAndDedupes));
        }

        private static void SelectionService_SplitsMixedTypes()
        {
            var summary = MaterialFileSelectionService.Analyze(new[]
            {
                "c:\\temp\\a.bgsm",
                "c:\\temp\\b.bgem",
                "c:\\temp\\c.txt",
                "c:\\temp\\a.bgsm"
            });

            AssertEqual(1, summary.MaterialFiles.Count, nameof(SelectionService_SplitsMixedTypes));
            AssertEqual(1, summary.EffectFiles.Count, nameof(SelectionService_SplitsMixedTypes));
            AssertEqual(1, summary.UnsupportedFiles.Count, nameof(SelectionService_SplitsMixedTypes));
            AssertTrue(summary.HasMixedMaterialTypes, nameof(SelectionService_SplitsMixedTypes));
        }

        private static void Preferences_SerializeAndResolvePresets()
        {
            var config = new Config();
            var descriptors = MaterialFieldRegistry.GetDescriptors(MaterialType.Material);
            var selectedDescriptors = new[]
            {
                descriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse),
                descriptors.Single(descriptor => descriptor.Label == ControlNames.Normal)
            };

            BulkEditorPreferencesService.SavePreset(config, MaterialType.Material, "Textures", selectedDescriptors);
            string serialized = BulkEditorPreferencesService.SerializePresets(config.BulkFieldPresets);
            List<BulkFieldPreset> reloaded = BulkEditorPreferencesService.DeserializePresets(serialized);
            BulkFieldPreset preset = reloaded.Single();
            IReadOnlyList<MaterialFieldDescriptor> resolved = BulkEditorPreferencesService.ResolvePresetFields(descriptors, preset);

            AssertEqual("Textures", preset.Name, nameof(Preferences_SerializeAndResolvePresets));
            AssertSequenceEqual(
                selectedDescriptors.Select(descriptor => descriptor.Label).OrderBy(label => label, StringComparer.OrdinalIgnoreCase).ToArray(),
                resolved.Select(descriptor => descriptor.Label).OrderBy(label => label, StringComparer.OrdinalIgnoreCase).ToArray(),
                nameof(Preferences_SerializeAndResolvePresets));
        }

        private static void Export_UsesPatternTokensForCurrentRows()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            string sourcePath = TestFileSupport.CreateBgem(outputPath, "effect.bgem", material =>
            {
                material.Version = 11;
                material.BaseTexture = "textures\\effect.dds";
            });
            var session = BulkMaterialEditSession.Create(MaterialType.Effect, new[] { sourcePath });
            string exportPattern = Path.Combine(outputPath, "exports", "{name}_{indexNN}{ext}");

            IReadOnlyList<string> preview = BulkMaterialExportService.PreviewOutputPaths(session.Rows, exportPattern);
            IReadOnlyList<FieldCopyResult> results = BulkMaterialExportService.Export(session.Rows, exportPattern, serializeAsJson: false);

            string expectedPath = Path.Combine(outputPath, "exports", "effect_01.bgem");
            FieldCopyResult exportResult = results.Single();
            AssertEqual(expectedPath, preview.Single(), nameof(Export_UsesPatternTokensForCurrentRows));
            AssertEqual(FieldCopyStatus.Success, exportResult.Status, $"{nameof(Export_UsesPatternTokensForCurrentRows)} [{exportResult.Message}]");
            AssertTrue(File.Exists(expectedPath), nameof(Export_UsesPatternTokensForCurrentRows));
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
