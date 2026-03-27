using Material_Editor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Material_Editor.Services
{
    internal sealed class MaterialBackupEntry
    {
        public MaterialBackupEntry(string sourcePath, string backupPath, string displayLabel, DateTime createdUtc, long sizeBytes, bool isLegacy, bool isRetainedOriginal)
        {
            SourcePath = sourcePath ?? string.Empty;
            BackupPath = backupPath ?? string.Empty;
            DisplayLabel = displayLabel ?? string.Empty;
            CreatedUtc = createdUtc;
            SizeBytes = sizeBytes;
            IsLegacy = isLegacy;
            IsRetainedOriginal = isRetainedOriginal;
        }

        public string SourcePath { get; }
        public string BackupPath { get; }
        public string DisplayLabel { get; }
        public DateTime CreatedUtc { get; }
        public long SizeBytes { get; }
        public bool IsLegacy { get; }
        public bool IsRetainedOriginal { get; }
    }

    internal static class MaterialBackupService
    {
        private const string BackupFolderName = "backup";
        private const string BackupTimestampFormat = "yyyyMMdd-HHmmssfff";
        private const string LegacyBackupExtension = ".bak";
        private const string RetainedOriginalPrefix = "original_";

        public static string BackupFolderNameValue => BackupFolderName;
        internal static string BackupDirectoryPathValue => GetBackupDirectory();
        internal static string BackupRootDirectoryOverride { get; set; }

        public static IReadOnlyList<MaterialBackupEntry> GetAvailableBackups(string sourcePath)
        {
            string normalizedSourcePath = MaterialFilePersistence.NormalizePath(sourcePath);
            if (string.IsNullOrWhiteSpace(normalizedSourcePath))
                return Array.Empty<MaterialBackupEntry>();

            var backups = new List<MaterialBackupEntry>();
            string sourceFileName = Path.GetFileName(normalizedSourcePath) ?? string.Empty;
            string sourceKey = GetSourceKey(normalizedSourcePath);
            string backupDirectory = GetBackupDirectory();

            if (Directory.Exists(backupDirectory))
            {
                string pattern = $"*_{sourceKey}_{sourceFileName}{LegacyBackupExtension}";
                foreach (string backupPath in Directory.GetFiles(backupDirectory, pattern))
                {
                    if (TryCreateBackupEntry(normalizedSourcePath, backupPath, isLegacy: false, out MaterialBackupEntry entry))
                        backups.Add(entry);
                }
            }

            string legacyBackupDirectory = GetLegacyBackupDirectory(normalizedSourcePath);
            if (!string.Equals(legacyBackupDirectory, backupDirectory, StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(legacyBackupDirectory))
            {
                string pattern = $"*_{sourceFileName}{LegacyBackupExtension}";
                foreach (string backupPath in Directory.GetFiles(legacyBackupDirectory, pattern))
                {
                    if (TryCreateBackupEntry(normalizedSourcePath, backupPath, isLegacy: false, out MaterialBackupEntry entry))
                        backups.Add(entry);
                }
            }

            string legacyBackupPath = normalizedSourcePath + LegacyBackupExtension;
            if (File.Exists(legacyBackupPath)
                && TryCreateBackupEntry(normalizedSourcePath, legacyBackupPath, isLegacy: true, out MaterialBackupEntry legacyEntry))
            {
                backups.Add(legacyEntry);
            }

            return backups
                .OrderByDescending(entry => entry.CreatedUtc)
                .ThenByDescending(entry => entry.DisplayLabel, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static IReadOnlyList<MaterialBackupEntry> GetAvailableBackups(IEnumerable<string> sourcePaths)
        {
            return (sourcePaths ?? Array.Empty<string>())
                .Select(MaterialFilePersistence.NormalizePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(GetAvailableBackups)
                .OrderBy(entry => entry.SourcePath, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(entry => entry.CreatedUtc)
                .ThenByDescending(entry => entry.DisplayLabel, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static bool TryCreateBackup(string filePath, bool backupExisting, Config config, out string backupPath, out string errorMessage)
        {
            backupPath = string.Empty;
            errorMessage = null;

            string normalizedPath = MaterialFilePersistence.NormalizePath(filePath);
            if (!backupExisting || string.IsNullOrWhiteSpace(normalizedPath) || !File.Exists(normalizedPath))
                return true;

            try
            {
                PrepareBackupCapacity(normalizedPath, config);
                backupPath = GetNextBackupPath(normalizedPath, config);
                File.Copy(normalizedPath, backupPath);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static FieldCopyResult RestoreNewest(string sourcePath, Config config, bool backupCurrentBeforeRestore)
        {
            IReadOnlyList<MaterialBackupEntry> backups = GetAvailableBackups(sourcePath);
            MaterialBackupEntry latestBackup = backups.FirstOrDefault();
            if (latestBackup == null)
                return new FieldCopyResult(sourcePath, FieldCopyStatus.Failed, "No backups are available for this file.");

            return RestoreBackup(latestBackup, config, backupCurrentBeforeRestore);
        }

        public static FieldCopyResult RestoreOldest(string sourcePath, Config config, bool backupCurrentBeforeRestore)
        {
            IReadOnlyList<MaterialBackupEntry> backups = GetAvailableBackups(sourcePath);
            MaterialBackupEntry oldestBackup = backups.LastOrDefault();
            if (oldestBackup == null)
                return new FieldCopyResult(sourcePath, FieldCopyStatus.Failed, "No backups are available for this file.");

            return RestoreBackup(oldestBackup, config, backupCurrentBeforeRestore);
        }

        public static FieldCopyResult RestoreBackup(MaterialBackupEntry backup, Config config, bool backupCurrentBeforeRestore)
        {
            if (backup == null)
                return new FieldCopyResult(string.Empty, FieldCopyStatus.Failed, "No backup was selected.");

            string sourcePath = MaterialFilePersistence.NormalizePath(backup.SourcePath);
            string backupPath = MaterialFilePersistence.NormalizePath(backup.BackupPath);

            try
            {
                if (!File.Exists(backupPath))
                    return new FieldCopyResult(sourcePath, FieldCopyStatus.Failed, "The selected backup file no longer exists.");

                if (backupCurrentBeforeRestore
                    && File.Exists(sourcePath)
                    && !TryCreateBackup(sourcePath, backupExisting: true, config, out _, out string backupError))
                {
                    return new FieldCopyResult(sourcePath, FieldCopyStatus.Failed, $"Failed to create a backup of the current file before restore: {backupError}");
                }

                string directory = Path.GetDirectoryName(sourcePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.Copy(backupPath, sourcePath, overwrite: true);
                return new FieldCopyResult(sourcePath, FieldCopyStatus.Success, $"Recovered from '{backup.DisplayLabel}'.");
            }
            catch (Exception ex)
            {
                return new FieldCopyResult(sourcePath, FieldCopyStatus.Failed, ex.Message);
            }
        }

        private static void PrepareBackupCapacity(string sourcePath, Config config)
        {
            int maxBackupsPerFile = Math.Max(0, config?.MaxBackupsPerFile ?? 0);
            long maxBackupFolderBytes = GetMaxBackupFolderBytes(config);
            long incomingBackupBytes = new FileInfo(sourcePath).Length;
            bool incomingBackupIsRetainedOriginal = WillCreateRetainedOriginalBackup(sourcePath, config);

            IReadOnlyList<MaterialBackupEntry> existingBackups = GetManagedBackups(sourcePath)
                .OrderBy(entry => entry.CreatedUtc)
                .ThenBy(entry => entry.DisplayLabel, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (maxBackupsPerFile > 0)
            {
                MaterialBackupEntry[] prunableBackups = existingBackups
                    .Where(entry => !entry.IsRetainedOriginal)
                    .ToArray();

                int backupsToDelete = prunableBackups.Length - maxBackupsPerFile + (incomingBackupIsRetainedOriginal ? 0 : 1);
                for (int index = 0; index < backupsToDelete; index++)
                    DeleteBackupFile(prunableBackups[index].BackupPath);
            }

            if (maxBackupFolderBytes <= 0)
                return;

            if (incomingBackupBytes > maxBackupFolderBytes)
            {
                throw new IOException(
                    $"Backup failed because the file size ({FormatBytes(incomingBackupBytes)}) exceeds the configured backup folder limit ({FormatBytes(maxBackupFolderBytes)}).");
            }

            string backupDirectory = GetBackupDirectory();
            Directory.CreateDirectory(backupDirectory);

            List<FileInfo> folderBackups = Directory.GetFiles(backupDirectory, "*" + LegacyBackupExtension)
                .Select(path => new FileInfo(path))
                .ToList();
            List<FileInfo> prunableFolderBackups = folderBackups
                .Where(info => !IsRetainedOriginalBackupPath(info.FullName))
                .OrderBy(info => GetBackupSortTimeUtc(info.FullName))
                .ThenBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            long currentFolderBytes = folderBackups.Sum(info => info.Length);
            int deleteIndex = 0;
            while (currentFolderBytes + incomingBackupBytes > maxBackupFolderBytes && deleteIndex < prunableFolderBackups.Count)
            {
                FileInfo info = prunableFolderBackups[deleteIndex++];
                currentFolderBytes -= info.Length;
                DeleteBackupFile(info.FullName);
            }

            if (currentFolderBytes + incomingBackupBytes > maxBackupFolderBytes)
            {
                throw new IOException(
                    $"Backup failed because the configured backup folder limit ({FormatBytes(maxBackupFolderBytes)}) was reached and no more backups could be pruned.");
            }
        }

        private static bool TryCreateBackupEntry(string sourcePath, string backupPath, bool isLegacy, out MaterialBackupEntry entry)
        {
            entry = null;

            try
            {
                if (!File.Exists(backupPath))
                    return false;

                var fileInfo = new FileInfo(backupPath);
                entry = new MaterialBackupEntry(
                    sourcePath,
                    backupPath,
                    Path.GetFileName(backupPath) ?? string.Empty,
                    GetBackupSortTimeUtc(backupPath),
                    fileInfo.Length,
                    isLegacy,
                    isRetainedOriginal: IsRetainedOriginalBackupPath(backupPath));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static DateTime GetBackupSortTimeUtc(string backupPath)
        {
            if (TryGetTimestampedBackupUtc(backupPath, out DateTime timestampUtc))
                return timestampUtc;

            try
            {
                return File.GetLastWriteTimeUtc(backupPath);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static bool TryGetTimestampedBackupUtc(string backupPath, out DateTime timestampUtc)
        {
            timestampUtc = DateTime.MinValue;
            string fileName = Path.GetFileName(backupPath) ?? string.Empty;
            if (fileName.Length < 22)
                return false;

            string timestampText = fileName[..18];
            return DateTime.TryParseExact(
                timestampText,
                BackupTimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out timestampUtc)
                && (timestampUtc = timestampUtc.ToUniversalTime()) != DateTime.MinValue;
        }

        private static string GetNextBackupPath(string filePath, Config config)
        {
            string originalFileName = Path.GetFileName(filePath) ?? string.Empty;
            string sourceKey = GetSourceKey(filePath);
            string backupDirectory = GetBackupDirectory();
            Directory.CreateDirectory(backupDirectory);

            if (WillCreateRetainedOriginalBackup(filePath, config))
            {
                string retainedOriginalPath = GetRetainedOriginalBackupPath(filePath);
                return retainedOriginalPath;
            }

            string timestamp = DateTime.Now.ToString(BackupTimestampFormat, CultureInfo.InvariantCulture);
            for (int sequence = 0; ; sequence++)
            {
                string backupFileName = $"{timestamp}-{sequence:00}_{sourceKey}_{originalFileName}{LegacyBackupExtension}";
                string backupPath = Path.Combine(backupDirectory, backupFileName);
                if (!File.Exists(backupPath))
                    return backupPath;
            }
        }

        private static IReadOnlyList<MaterialBackupEntry> GetManagedBackups(string sourcePath)
        {
            string normalizedSourcePath = MaterialFilePersistence.NormalizePath(sourcePath);
            if (string.IsNullOrWhiteSpace(normalizedSourcePath))
                return Array.Empty<MaterialBackupEntry>();

            string backupDirectory = GetBackupDirectory();
            if (!Directory.Exists(backupDirectory))
                return Array.Empty<MaterialBackupEntry>();

            string sourceFileName = Path.GetFileName(normalizedSourcePath) ?? string.Empty;
            string sourceKey = GetSourceKey(normalizedSourcePath);
            string pattern = $"*_{sourceKey}_{sourceFileName}{LegacyBackupExtension}";
            var backups = new List<MaterialBackupEntry>();
            foreach (string backupPath in Directory.GetFiles(backupDirectory, pattern))
            {
                if (TryCreateBackupEntry(normalizedSourcePath, backupPath, isLegacy: false, out MaterialBackupEntry entry))
                    backups.Add(entry);
            }

            return backups;
        }

        private static string GetBackupDirectory()
        {
            string rootDirectory = BackupRootDirectoryOverride;
            if (string.IsNullOrWhiteSpace(rootDirectory))
                rootDirectory = AppContext.BaseDirectory;

            return Path.Combine(rootDirectory, BackupFolderName);
        }

        private static string GetLegacyBackupDirectory(string filePath)
        {
            string sourceDirectory = Path.GetDirectoryName(filePath);
            return string.IsNullOrWhiteSpace(sourceDirectory)
                ? BackupFolderName
                : Path.Combine(sourceDirectory, BackupFolderName);
        }

        private static string GetRetainedOriginalBackupPath(string filePath)
        {
            string backupDirectory = GetBackupDirectory();
            string sourceKey = GetSourceKey(filePath);
            string originalFileName = Path.GetFileName(filePath) ?? string.Empty;
            return Path.Combine(backupDirectory, $"{RetainedOriginalPrefix}{sourceKey}_{originalFileName}{LegacyBackupExtension}");
        }

        private static bool WillCreateRetainedOriginalBackup(string filePath, Config config)
        {
            return config?.RetainOriginalBackup == true
                && !File.Exists(GetRetainedOriginalBackupPath(filePath));
        }

        private static bool IsRetainedOriginalBackupPath(string backupPath)
        {
            string fileName = Path.GetFileName(backupPath) ?? string.Empty;
            return fileName.StartsWith(RetainedOriginalPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSourceKey(string filePath)
        {
            string normalizedPath = MaterialFilePersistence.NormalizePath(filePath) ?? string.Empty;
            byte[] pathBytes = Encoding.UTF8.GetBytes(normalizedPath);
            return Convert.ToHexString(SHA256.HashData(pathBytes));
        }

        private static long GetMaxBackupFolderBytes(Config config)
        {
            long megabytes = Math.Max(0L, config?.MaxBackupFolderMegabytes ?? 0L);
            if (megabytes == 0)
                return 0;

            return megabytes > long.MaxValue / (1024L * 1024L)
                ? long.MaxValue
                : megabytes * 1024L * 1024L;
        }

        private static void DeleteBackupFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static string FormatBytes(long value)
        {
            if (value < 1024)
                return $"{value} B";

            double size = value;
            string[] units = { "KB", "MB", "GB", "TB" };
            int unitIndex = -1;
            do
            {
                size /= 1024d;
                unitIndex++;
            }
            while (size >= 1024d && unitIndex < units.Length - 1);

            return $"{size:0.#} {units[unitIndex]}";
        }
    }
}
