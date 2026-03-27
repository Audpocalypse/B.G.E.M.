using Material_Editor.Models;
using Material_Editor.Services;
using MaterialLib;
using System;
using System.IO;
using System.Linq;

namespace MaterialEditor.Tests
{
    internal static class MaterialBackupServiceTests
    {
        public static void RunAll()
        {
            Save_PrunesOlderBackupsWhenPerFileLimitIsReached();
            Save_RetainedOriginalSurvivesPerFileCleanup();
            BackupService_PrunesBackupFolderToConfiguredSize();
            BackupService_RetainedOriginalSurvivesFolderCleanup();
            BackupService_FailsWhenSingleBackupExceedsConfiguredFolderLimit();
            BackupService_RestoresOldestAndNewestBackups();
            BackupService_UsesSingleManagedFolderWithoutFilenameCollisions();
            BackupService_EnumeratesLegacySiblingBackups();
        }

        private static void Save_PrunesOlderBackupsWhenPerFileLimitIsReached()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "limited.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });
            Config config = CreateBackupConfig(maxBackupsPerFile: 1);

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\first.dds"
            }, asJson: false, backupExisting: true, config);

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\second.dds"
            }, asJson: false, backupExisting: true, config);

            var backups = MaterialBackupService.GetAvailableBackups(path);
            AssertEqual(1, backups.Count, nameof(Save_PrunesOlderBackupsWhenPerFileLimitIsReached));
            AssertEqual("textures\\first.dds", TestFileSupport.LoadBgsm(backups.Single().BackupPath).DiffuseTexture, nameof(Save_PrunesOlderBackupsWhenPerFileLimitIsReached));
            AssertEqual("textures\\second.dds", TestFileSupport.LoadBgsm(path).DiffuseTexture, nameof(Save_PrunesOlderBackupsWhenPerFileLimitIsReached));
        }

        private static void BackupService_PrunesBackupFolderToConfiguredSize()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string path = Path.Combine(outputDirectory.Path, "folderlimit.bin");
            File.WriteAllBytes(path, new byte[700 * 1024]);
            Config config = CreateBackupConfig(maxBackupFolderMegabytes: 1);

            AssertTrue(MaterialBackupService.TryCreateBackup(path, backupExisting: true, config, out string firstBackupPath, out string firstError), nameof(BackupService_PrunesBackupFolderToConfiguredSize));
            AssertEqual(string.Empty, firstError ?? string.Empty, nameof(BackupService_PrunesBackupFolderToConfiguredSize));
            AssertTrue(File.Exists(firstBackupPath), nameof(BackupService_PrunesBackupFolderToConfiguredSize));

            File.WriteAllBytes(path, new byte[700 * 1024]);
            AssertTrue(MaterialBackupService.TryCreateBackup(path, backupExisting: true, config, out string secondBackupPath, out string secondError), nameof(BackupService_PrunesBackupFolderToConfiguredSize));
            AssertEqual(string.Empty, secondError ?? string.Empty, nameof(BackupService_PrunesBackupFolderToConfiguredSize));

            string backupDirectory = MaterialBackupService.BackupDirectoryPathValue;
            string[] backupFiles = Directory.GetFiles(backupDirectory, "*.bak");
            AssertEqual(1, backupFiles.Length, nameof(BackupService_PrunesBackupFolderToConfiguredSize));
            AssertEqual(MaterialFilePersistence.NormalizePath(secondBackupPath), MaterialFilePersistence.NormalizePath(backupFiles[0]), nameof(BackupService_PrunesBackupFolderToConfiguredSize));
        }

        private static void Save_RetainedOriginalSurvivesPerFileCleanup()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "retain-per-file.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });
            Config config = CreateBackupConfig(maxBackupsPerFile: 1, retainOriginalBackup: true);

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\first.dds"
            }, asJson: false, backupExisting: true, config);

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\second.dds"
            }, asJson: false, backupExisting: true, config);

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\third.dds"
            }, asJson: false, backupExisting: true, config);

            var backups = MaterialBackupService.GetAvailableBackups(path)
                .OrderBy(entry => entry.CreatedUtc)
                .ThenBy(entry => entry.DisplayLabel, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            AssertEqual(2, backups.Length, nameof(Save_RetainedOriginalSurvivesPerFileCleanup));
            AssertEqual(true, backups[0].IsRetainedOriginal, nameof(Save_RetainedOriginalSurvivesPerFileCleanup));
            AssertEqual("textures\\original.dds", TestFileSupport.LoadBgsm(backups[0].BackupPath).DiffuseTexture, nameof(Save_RetainedOriginalSurvivesPerFileCleanup));
            AssertEqual(false, backups[1].IsRetainedOriginal, nameof(Save_RetainedOriginalSurvivesPerFileCleanup));
            AssertEqual("textures\\second.dds", TestFileSupport.LoadBgsm(backups[1].BackupPath).DiffuseTexture, nameof(Save_RetainedOriginalSurvivesPerFileCleanup));
        }

        private static void BackupService_RetainedOriginalSurvivesFolderCleanup()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string path = Path.Combine(outputDirectory.Path, "retain-folder.bin");
            File.WriteAllBytes(path, new byte[700 * 1024]);
            Config config = CreateBackupConfig(maxBackupFolderMegabytes: 1, retainOriginalBackup: true);

            AssertTrue(MaterialBackupService.TryCreateBackup(path, backupExisting: true, config, out string originalBackupPath, out string firstError), nameof(BackupService_RetainedOriginalSurvivesFolderCleanup));
            AssertEqual(string.Empty, firstError ?? string.Empty, nameof(BackupService_RetainedOriginalSurvivesFolderCleanup));

            File.WriteAllBytes(path, new byte[700 * 1024]);
            bool success = MaterialBackupService.TryCreateBackup(path, backupExisting: true, config, out _, out string secondError);

            AssertEqual(false, success, nameof(BackupService_RetainedOriginalSurvivesFolderCleanup));
            AssertTrue((secondError ?? string.Empty).Contains("no more backups could be pruned", StringComparison.OrdinalIgnoreCase), nameof(BackupService_RetainedOriginalSurvivesFolderCleanup));

            var backups = MaterialBackupService.GetAvailableBackups(path);
            AssertEqual(1, backups.Count, nameof(BackupService_RetainedOriginalSurvivesFolderCleanup));
            AssertEqual(true, backups.Single().IsRetainedOriginal, nameof(BackupService_RetainedOriginalSurvivesFolderCleanup));
            AssertEqual(MaterialFilePersistence.NormalizePath(originalBackupPath), MaterialFilePersistence.NormalizePath(backups.Single().BackupPath), nameof(BackupService_RetainedOriginalSurvivesFolderCleanup));
        }

        private static void BackupService_FailsWhenSingleBackupExceedsConfiguredFolderLimit()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string path = Path.Combine(outputDirectory.Path, "toolarge.bin");
            File.WriteAllBytes(path, new byte[2 * 1024 * 1024]);
            Config config = CreateBackupConfig(maxBackupFolderMegabytes: 1);

            bool success = MaterialBackupService.TryCreateBackup(path, backupExisting: true, config, out _, out string errorMessage);

            AssertEqual(false, success, nameof(BackupService_FailsWhenSingleBackupExceedsConfiguredFolderLimit));
            AssertTrue((errorMessage ?? string.Empty).Contains("exceeds the configured backup folder limit", StringComparison.OrdinalIgnoreCase), nameof(BackupService_FailsWhenSingleBackupExceedsConfiguredFolderLimit));
        }

        private static void BackupService_RestoresOldestAndNewestBackups()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "restore.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });
            Config config = CreateBackupConfig();

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\first.dds"
            }, asJson: false, backupExisting: true, config);

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\second.dds"
            }, asJson: false, backupExisting: true, config);

            FieldCopyResult oldestResult = MaterialBackupService.RestoreOldest(path, config, backupCurrentBeforeRestore: false);
            AssertEqual(FieldCopyStatus.Success, oldestResult.Status, nameof(BackupService_RestoresOldestAndNewestBackups));
            AssertEqual("textures\\original.dds", TestFileSupport.LoadBgsm(path).DiffuseTexture, nameof(BackupService_RestoresOldestAndNewestBackups));

            FieldCopyResult newestResult = MaterialBackupService.RestoreNewest(path, config, backupCurrentBeforeRestore: false);
            AssertEqual(FieldCopyStatus.Success, newestResult.Status, nameof(BackupService_RestoresOldestAndNewestBackups));
            AssertEqual("textures\\first.dds", TestFileSupport.LoadBgsm(path).DiffuseTexture, nameof(BackupService_RestoresOldestAndNewestBackups));
        }

        private static void BackupService_UsesSingleManagedFolderWithoutFilenameCollisions()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string alphaDirectory = Path.Combine(outputDirectory.Path, "Alpha");
            string betaDirectory = Path.Combine(outputDirectory.Path, "Beta");
            Directory.CreateDirectory(alphaDirectory);
            Directory.CreateDirectory(betaDirectory);

            string alphaPath = TestFileSupport.CreateBgsm(alphaDirectory, "shared.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\alpha.dds";
            });
            string betaPath = TestFileSupport.CreateBgsm(betaDirectory, "shared.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\beta.dds";
            });

            Config config = CreateBackupConfig();
            AssertTrue(MaterialBackupService.TryCreateBackup(alphaPath, backupExisting: true, config, out string alphaBackupPath, out string alphaError), nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));
            AssertEqual(string.Empty, alphaError ?? string.Empty, nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));
            AssertTrue(MaterialBackupService.TryCreateBackup(betaPath, backupExisting: true, config, out string betaBackupPath, out string betaError), nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));
            AssertEqual(string.Empty, betaError ?? string.Empty, nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));

            AssertEqual(MaterialBackupService.BackupDirectoryPathValue, Path.GetDirectoryName(alphaBackupPath), nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));
            AssertEqual(MaterialBackupService.BackupDirectoryPathValue, Path.GetDirectoryName(betaBackupPath), nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));
            AssertTrue(!string.Equals(Path.GetFileName(alphaBackupPath), Path.GetFileName(betaBackupPath), StringComparison.OrdinalIgnoreCase), nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));
            AssertEqual(1, MaterialBackupService.GetAvailableBackups(alphaPath).Count, nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));
            AssertEqual(1, MaterialBackupService.GetAvailableBackups(betaPath).Count, nameof(BackupService_UsesSingleManagedFolderWithoutFilenameCollisions));
        }

        private static void BackupService_EnumeratesLegacySiblingBackups()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            using var backupRoot = TestFileSupport.PushBackupRoot(outputDirectory.Path);
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "legacy.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\current.dds";
            });

            string legacyBackupPath = path + ".bak";
            TestFileSupport.SaveMaterial(legacyBackupPath, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\legacy.dds"
            });

            var backups = MaterialBackupService.GetAvailableBackups(path);
            AssertEqual(1, backups.Count, nameof(BackupService_EnumeratesLegacySiblingBackups));
            AssertEqual(true, backups.Single().IsLegacy, nameof(BackupService_EnumeratesLegacySiblingBackups));
            AssertEqual(MaterialFilePersistence.NormalizePath(legacyBackupPath), MaterialFilePersistence.NormalizePath(backups.Single().BackupPath), nameof(BackupService_EnumeratesLegacySiblingBackups));
        }

        private static Config CreateBackupConfig(int maxBackupsPerFile = 0, long maxBackupFolderMegabytes = 0, bool retainOriginalBackup = false)
        {
            return new Config
            {
                CreateBackupsByDefault = true,
                RetainOriginalBackup = retainOriginalBackup,
                MaxBackupsPerFile = maxBackupsPerFile,
                MaxBackupFolderMegabytes = maxBackupFolderMegabytes
            };
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
    }
}
