using MaterialLib;
using Material_Editor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace Material_Editor.Services
{
    internal static class MaterialFilePersistence
    {
        public static bool TryLoadMaterial(string filePath, out BaseMaterialFile material, out bool isJson, out string errorMessage)
        {
            material = null;
            isJson = false;
            errorMessage = null;

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                int start = stream.ReadByte();
                if (start == -1)
                {
                    errorMessage = "Target file is empty.";
                    return false;
                }

                stream.Position = 0;

                BaseMaterialFile candidate = Path.GetExtension(filePath).Equals(".bgem", StringComparison.OrdinalIgnoreCase)
                    ? new BGEM()
                    : new BGSM();

                if (start == '{' || start == '[')
                {
                    isJson = true;
                    var serializer = new DataContractJsonSerializer(candidate.GetType(), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                    material = (BaseMaterialFile)serializer.ReadObject(stream);
                }
                else
                {
                    stream.Position = 0;
                    if (!candidate.Open(stream))
                    {
                        errorMessage = "Failed to read binary material.";
                        return false;
                    }

                    material = candidate;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static bool TrySaveMaterial(string filePath, BaseMaterialFile material, bool asJson, out string errorMessage)
        {
            errorMessage = null;
            var previousCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                EnsureParentDirectoryExists(filePath);
                using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                if (asJson)
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    using var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  ");
                    var serializer = new DataContractJsonSerializer(material.GetType(), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                    serializer.WriteObject(writer, material);
                    writer.Flush();
                }
                else if (!material.Save(stream))
                {
                    errorMessage = "Failed to write binary material.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        public static void SaveMaterial(string filePath, BaseMaterialFile material, bool asJson)
        {
            if (!TrySaveMaterial(filePath, material, asJson, out string errorMessage))
                throw new IOException(errorMessage ?? "Failed to save material.");
        }

        public static void SaveMaterial(string filePath, BaseMaterialFile material, bool asJson, bool backupExisting)
        {
            SaveMaterial(filePath, material, asJson, backupExisting, config: null);
        }

        public static void SaveMaterial(string filePath, BaseMaterialFile material, bool asJson, bool backupExisting, Config config)
        {
            if (!MaterialBackupService.TryCreateBackup(filePath, backupExisting, config, out _, out string backupError))
                throw new IOException(backupError ?? "Failed to create backup.");

            EnsureParentDirectoryExists(filePath);
            SaveMaterial(filePath, material, asJson);
        }

        public static FieldCopyResult SaveMaterialResult(
            string filePath,
            BaseMaterialFile material,
            bool asJson,
            string successMessage,
            bool backupExisting = false,
            Config config = null,
            string failureMessagePrefix = null)
        {
            try
            {
                SaveMaterial(filePath, material, asJson, backupExisting, config);
                return new FieldCopyResult(filePath, FieldCopyStatus.Success, successMessage);
            }
            catch (Exception ex)
            {
                string message = string.IsNullOrEmpty(failureMessagePrefix)
                    ? ex.Message
                    : $"{failureMessagePrefix}{ex.Message}";
                return new FieldCopyResult(filePath, FieldCopyStatus.Failed, message);
            }
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            try
            {
                return Path.GetFullPath(path.Trim());
            }
            catch
            {
                return path.Trim();
            }
        }

        public static bool TryNormalizeOutputPath(string path, out string normalizedPath, out string errorMessage)
        {
            normalizedPath = string.Empty;
            errorMessage = ValidateOutputPath(path);
            if (!string.IsNullOrEmpty(errorMessage))
                return false;

            try
            {
                normalizedPath = Path.GetFullPath(path.Trim());
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Invalid output path: {ex.Message}";
                return false;
            }
        }

        public static void FinalizeOutputPaths<TItem>(
            IEnumerable<TItem> items,
            Func<TItem, string> targetPathSelector,
            Func<TItem, string> validationErrorSelector,
            Action<TItem, string> normalizedPathSetter,
            Action<TItem, string> validationErrorSetter,
            Func<TItem, string> normalizedPathSelector,
            string duplicateMessage)
        {
            foreach (TItem item in items ?? Array.Empty<TItem>())
            {
                if (!string.IsNullOrWhiteSpace(validationErrorSelector(item)))
                    continue;

                if (!TryNormalizeOutputPath(targetPathSelector(item), out string normalizedPath, out string errorMessage))
                {
                    validationErrorSetter(item, errorMessage);
                    continue;
                }

                normalizedPathSetter(item, normalizedPath);
            }

            var duplicateGroups = (items ?? Array.Empty<TItem>())
                .Where(item => string.IsNullOrWhiteSpace(validationErrorSelector(item)))
                .GroupBy(normalizedPathSelector, StringComparer.OrdinalIgnoreCase)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

            foreach (IGrouping<string, TItem> duplicateGroup in duplicateGroups)
            {
                foreach (TItem item in duplicateGroup)
                    validationErrorSetter(item, duplicateMessage);
            }
        }

        public static string ValidateOutputPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Output path resolved to an empty value.";

            string fileName = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(fileName))
                return "Generated output path is missing a file name.";

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return $"Generated file name '{fileName}' contains invalid characters.";

            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                return null;

            char[] separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            foreach (string segment in directory.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment.EndsWith(":", StringComparison.Ordinal))
                    continue;

                if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return $"Generated directory segment '{segment}' contains invalid characters.";
            }

            return null;
        }

        private static void EnsureParentDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
