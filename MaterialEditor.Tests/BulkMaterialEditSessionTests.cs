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
            Session_PreservesInsertionOrderAndFlagsMixedTypes();
            Descriptor_ParsesAndFormatsSpecialTypes();
            Session_TracksUnsupportedVersionSpecificFields();
            Session_TracksDirtyStateForParentBooleanFields();
            Session_RefreshDynamicStatePreservesDirtyBaseline();
            Session_UndoRestoresRecordedCellValue();
            Session_ApplyWritesDirtyRowsAndCreatesBackups();
            Session_ApplyCreatesMultipleTimestampedBackupsForSameFile();
            Session_ApplySelectedWritesOnlySelectedDirtyRows();
            Session_ApplySelectedClearsUndoForSavedRows();
            Session_AddFilesKeepsDirtyStateAndDedupes();
            Session_ApplyFailureLeavesRowDirty();
            Session_ApplyValidationFailureReportsFieldDetails();
            Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata();
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

        private static void Session_PreservesInsertionOrderAndFlagsMixedTypes()
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
                    MaterialFilePersistence.NormalizePath(aPath),
                    MaterialFilePersistence.NormalizePath(wrongPath),
                    MaterialFilePersistence.NormalizePath(bPath)
                },
                session.Rows.Select(row => row.FilePath).ToArray(),
                nameof(Session_PreservesInsertionOrderAndFlagsMixedTypes));

            AssertEqual(0, session.Rows[0].InsertionIndex, nameof(Session_PreservesInsertionOrderAndFlagsMixedTypes));
            AssertEqual(1, session.Rows[1].InsertionIndex, nameof(Session_PreservesInsertionOrderAndFlagsMixedTypes));
            AssertEqual(2, session.Rows[2].InsertionIndex, nameof(Session_PreservesInsertionOrderAndFlagsMixedTypes));
            AssertTrue(!session.Rows[0].HasLoadError, nameof(Session_PreservesInsertionOrderAndFlagsMixedTypes));
            AssertTrue(session.Rows[1].HasLoadError, nameof(Session_PreservesInsertionOrderAndFlagsMixedTypes));
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

        private static void Session_RefreshDynamicStatePreservesDirtyBaseline()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "refresh.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });
            var row = session.Rows.Single();
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);

            AssertTrue(session.TrySetCellValue(row, diffuseDescriptor, "textures\\changed.dds", out string errorMessage), nameof(Session_RefreshDynamicStatePreservesDirtyBaseline));
            AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(Session_RefreshDynamicStatePreservesDirtyBaseline));
            AssertTrue(row.IsDirty, nameof(Session_RefreshDynamicStatePreservesDirtyBaseline));

            row.RefreshDynamicState();

            AssertEqual("textures\\original.dds", Convert.ToString(row.GetCell(diffuseDescriptor).OriginalValue), nameof(Session_RefreshDynamicStatePreservesDirtyBaseline));
            AssertEqual("textures\\changed.dds", Convert.ToString(row.GetCell(diffuseDescriptor).CurrentValue), nameof(Session_RefreshDynamicStatePreservesDirtyBaseline));
            AssertTrue(row.IsDirty, nameof(Session_RefreshDynamicStatePreservesDirtyBaseline));
        }

        private static void Session_UndoRestoresRecordedCellValue()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "undo.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });
            var row = session.Rows.Single();
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);

            AssertTrue(session.TrySetCellValue(row, diffuseDescriptor, "textures\\changed.dds", out string errorMessage, recordUndo: true), nameof(Session_UndoRestoresRecordedCellValue));
            AssertEqual(string.Empty, errorMessage ?? string.Empty, nameof(Session_UndoRestoresRecordedCellValue));
            AssertTrue(session.CanUndo, nameof(Session_UndoRestoresRecordedCellValue));
            AssertTrue(!session.CanRedo, nameof(Session_UndoRestoresRecordedCellValue));

            IReadOnlyList<BulkMaterialEditUndoChange> undone = session.UndoLastChange();

            AssertEqual(1, undone.Count, nameof(Session_UndoRestoresRecordedCellValue));
            AssertEqual("textures\\original.dds", Convert.ToString(row.GetCell(diffuseDescriptor).CurrentValue), nameof(Session_UndoRestoresRecordedCellValue));
            AssertTrue(!session.CanUndo, nameof(Session_UndoRestoresRecordedCellValue));
            AssertTrue(session.CanRedo, nameof(Session_UndoRestoresRecordedCellValue));

            IReadOnlyList<BulkMaterialEditUndoChange> redone = session.RedoLastChange();

            AssertEqual(1, redone.Count, nameof(Session_UndoRestoresRecordedCellValue));
            AssertEqual("textures\\changed.dds", Convert.ToString(row.GetCell(diffuseDescriptor).CurrentValue), nameof(Session_UndoRestoresRecordedCellValue));
            AssertTrue(session.CanUndo, nameof(Session_UndoRestoresRecordedCellValue));
            AssertTrue(!session.CanRedo, nameof(Session_UndoRestoresRecordedCellValue));
        }

        private static void Session_ApplyWritesDirtyRowsAndCreatesBackups()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
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
            string[] backupFiles = GetOrderedBackupFiles(aPath);
            AssertEqual(1, backupFiles.Length, nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
            AssertTrue(IsTimestampedBackupPath(backupFiles[0], aPath), nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
            AssertEqual("textures\\updated.dds", TestFileSupport.LoadBgsm(aPath).DiffuseTexture, nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
            AssertTrue(!session.Rows[0].IsDirty, nameof(Session_ApplyWritesDirtyRowsAndCreatesBackups));
        }

        private static void Session_ApplyCreatesMultipleTimestampedBackupsForSameFile()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "repeat.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });
            var row = session.Rows.Single();
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);

            AssertTrue(session.TrySetCellValue(row, diffuseDescriptor, "textures\\first.dds", out _), nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
            IReadOnlyList<FieldCopyResult> firstResults = session.ApplyChanges(backupBeforeWrite: true);
            AssertEqual(FieldCopyStatus.Success, firstResults.Single().Status, nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));

            AssertTrue(session.TrySetCellValue(row, diffuseDescriptor, "textures\\second.dds", out _), nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
            IReadOnlyList<FieldCopyResult> secondResults = session.ApplyChanges(backupBeforeWrite: true);
            AssertEqual(FieldCopyStatus.Success, secondResults.Single().Status, nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));

            string[] backupFiles = GetOrderedBackupFiles(path);
            AssertEqual(2, backupFiles.Length, nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
            AssertTrue(IsTimestampedBackupPath(backupFiles[0], path), nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
            AssertTrue(IsTimestampedBackupPath(backupFiles[1], path), nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
            AssertTrue(!string.Equals(Path.GetFileName(backupFiles[0]), Path.GetFileName(backupFiles[1]), StringComparison.OrdinalIgnoreCase), nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
            AssertEqual("textures\\original.dds", TestFileSupport.LoadBgsm(backupFiles[0]).DiffuseTexture, nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
            AssertEqual("textures\\first.dds", TestFileSupport.LoadBgsm(backupFiles[1]).DiffuseTexture, nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
            AssertEqual("textures\\second.dds", TestFileSupport.LoadBgsm(path).DiffuseTexture, nameof(Session_ApplyCreatesMultipleTimestampedBackupsForSameFile));
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

        private static void Session_ApplyValidationFailureReportsFieldDetails()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "invalid-save.bgsm", material =>
            {
                material.Version = 2;
                material.GrayscaleToPaletteScale = 1.0f;
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });
            var row = session.Rows.Single();
            var scaleDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.GrayscaleToPaletteScale);

            AssertEqual(false, session.TrySetCellValue(row, scaleDescriptor, string.Empty, out string errorMessage), nameof(Session_ApplyValidationFailureReportsFieldDetails));
            AssertTrue(!string.IsNullOrWhiteSpace(errorMessage), nameof(Session_ApplyValidationFailureReportsFieldDetails));
            AssertTrue(row.HasValidationErrors, nameof(Session_ApplyValidationFailureReportsFieldDetails));

            IReadOnlyList<FieldCopyResult> results = session.ApplySelectedChanges(new[] { row }, backupBeforeWrite: false);

            AssertEqual(1, results.Count, nameof(Session_ApplyValidationFailureReportsFieldDetails));
            AssertEqual(FieldCopyStatus.Failed, results[0].Status, nameof(Session_ApplyValidationFailureReportsFieldDetails));
            AssertTrue((results[0].Message ?? string.Empty).Contains(ControlNames.GrayscaleToPaletteScale, StringComparison.Ordinal), nameof(Session_ApplyValidationFailureReportsFieldDetails));
        }

        private static void Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string materialsDirectory = Path.Combine(outputDirectory.Path, "Materials", "Armor");
            Directory.CreateDirectory(materialsDirectory);
            string path = TestFileSupport.CreateBgsm(materialsDirectory, "reload.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });
            DateTime initialCreationTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            DateTime initialLastWriteTime = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            File.SetCreationTimeUtc(path, initialCreationTime);
            File.SetLastWriteTimeUtc(path, initialLastWriteTime);

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);
            BulkMaterialEditRow originalRow = session.Rows[0];
            AssertEqual("reload.bgsm", originalRow.FileName, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertEqual("Armor\\reload.bgsm", originalRow.DisplayPath, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertEqual("Armor", originalRow.DisplayFolder, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertEqual(initialCreationTime, originalRow.CreationTimeUtc, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertEqual(initialLastWriteTime, originalRow.LastWriteTimeUtc, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertTrue(session.TrySetCellValue(originalRow, diffuseDescriptor, "textures\\edited.dds", out _), nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertTrue(originalRow.IsDirty, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));

            DateTime updatedCreationTime = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
            DateTime updatedLastWriteTime = new DateTime(2024, 6, 7, 8, 9, 10, DateTimeKind.Utc);
            TestFileSupport.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\original.dds"
            });
            File.SetCreationTimeUtc(path, updatedCreationTime);
            File.SetLastWriteTimeUtc(path, updatedLastWriteTime);

            IReadOnlyList<BulkMaterialEditRow> reloaded = session.ReloadRows(new[] { session.Rows[0] });

            AssertEqual(1, reloaded.Count, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertEqual(originalRow.InsertionIndex, reloaded[0].InsertionIndex, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertEqual("textures\\original.dds", Convert.ToString(reloaded[0].GetCell(diffuseDescriptor).CurrentValue), nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertEqual(updatedCreationTime, reloaded[0].CreationTimeUtc, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertEqual(updatedLastWriteTime, reloaded[0].LastWriteTimeUtc, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
            AssertTrue(!reloaded[0].IsDirty, nameof(Session_ReloadRowsRestoresDiskStateAndRefreshesMetadata));
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

        private static void Session_ApplySelectedClearsUndoForSavedRows()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "saved.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\saved.dds";
            });

            var session = BulkMaterialEditSession.Create(MaterialType.Material, new[] { path });
            var row = session.Rows.Single();
            var diffuseDescriptor = session.AllDescriptors.Single(descriptor => descriptor.Label == ControlNames.Diffuse);

            AssertTrue(session.TrySetCellValue(row, diffuseDescriptor, "textures\\saved_changed.dds", out _, recordUndo: true), nameof(Session_ApplySelectedClearsUndoForSavedRows));
            AssertTrue(session.CanUndo, nameof(Session_ApplySelectedClearsUndoForSavedRows));

            IReadOnlyList<FieldCopyResult> results = session.ApplySelectedChanges(new[] { row }, backupBeforeWrite: false);

            AssertEqual(FieldCopyStatus.Success, results.Single().Status, nameof(Session_ApplySelectedClearsUndoForSavedRows));
            AssertTrue(!session.CanUndo, nameof(Session_ApplySelectedClearsUndoForSavedRows));
            AssertTrue(!session.CanRedo, nameof(Session_ApplySelectedClearsUndoForSavedRows));
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

        private static string[] GetOrderedBackupFiles(string sourcePath)
        {
            return MaterialBackupService.GetAvailableBackups(sourcePath)
                .Where(entry => !entry.IsLegacy && !entry.IsRetainedOriginal)
                .OrderBy(entry => entry.DisplayLabel, StringComparer.OrdinalIgnoreCase)
                .Select(entry => entry.BackupPath)
                .ToArray();
        }

        private static bool IsTimestampedBackupPath(string backupPath, string sourcePath)
        {
            string expectedDirectory = MaterialBackupService.BackupDirectoryPathValue;
            if (!string.Equals(Path.GetDirectoryName(backupPath), expectedDirectory, StringComparison.OrdinalIgnoreCase))
                return false;

            string fileName = Path.GetFileName(backupPath);
            string expectedSuffix = $"_{Path.GetFileName(sourcePath)}.bak";
            if (!fileName.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase))
                return false;

            int separatorIndex = fileName.IndexOf('_');
            if (separatorIndex != 21)
                return false;

            string prefix = fileName[..separatorIndex];
            return char.IsDigit(prefix[0])
                && char.IsDigit(prefix[1])
                && char.IsDigit(prefix[2])
                && char.IsDigit(prefix[3])
                && char.IsDigit(prefix[4])
                && char.IsDigit(prefix[5])
                && char.IsDigit(prefix[6])
                && char.IsDigit(prefix[7])
                && prefix[8] == '-'
                && char.IsDigit(prefix[9])
                && char.IsDigit(prefix[10])
                && char.IsDigit(prefix[11])
                && char.IsDigit(prefix[12])
                && char.IsDigit(prefix[13])
                && char.IsDigit(prefix[14])
                && char.IsDigit(prefix[15])
                && char.IsDigit(prefix[16])
                && char.IsDigit(prefix[17])
                && prefix[18] == '-'
                && char.IsDigit(prefix[19])
                && char.IsDigit(prefix[20]);
        }
    }
}
