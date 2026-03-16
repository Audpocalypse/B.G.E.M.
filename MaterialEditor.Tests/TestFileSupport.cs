using Material_Editor.Services;
using MaterialLib;
using System;
using System.IO;

namespace MaterialEditor.Tests
{
    internal static class TestFileSupport
    {
        public static void RunInTempDirectory(string rootName, Action<string> action)
        {
            using var directory = CreateTempDirectoryScope(rootName);
            action(directory.Path);
        }

        public static void RunInTempDirectories(string rootName, Action<string, string> action)
        {
            using var firstDirectory = CreateTempDirectoryScope(rootName);
            using var secondDirectory = CreateTempDirectoryScope(rootName);
            action(firstDirectory.Path, secondDirectory.Path);
        }

        public static TemporaryDirectoryScope CreateTempDirectoryScope(string rootName = "MaterialEditorTests")
        {
            return new TemporaryDirectoryScope(CreateTempDirectory(rootName));
        }

        private static string CreateTempDirectory(string rootName = "MaterialEditorTests")
        {
            string path = Path.Combine(Path.GetTempPath(), rootName, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }

        public static string CreateBgsm(string directory, string fileName, Action<BGSM> configure)
        {
            string path = Path.Combine(directory, fileName);
            var material = new BGSM();
            configure?.Invoke(material);
            SaveMaterial(path, material);
            return path;
        }

        public static string CreateBgem(string directory, string fileName, Action<BGEM> configure)
        {
            string path = Path.Combine(directory, fileName);
            var material = new BGEM();
            configure?.Invoke(material);
            SaveMaterial(path, material);
            return path;
        }

        public static void SaveMaterial(string path, BaseMaterialFile material)
        {
            MaterialFilePersistence.SaveMaterial(path, material, asJson: false);
        }

        public static BGSM LoadBgsm(string path)
        {
            if (!MaterialFilePersistence.TryLoadMaterial(path, out BaseMaterialFile material, out _, out string errorMessage) || material is not BGSM bgsm)
                throw new InvalidOperationException($"Failed to load material '{path}': {errorMessage}");

            return bgsm;
        }

        internal sealed class TemporaryDirectoryScope : IDisposable
        {
            public TemporaryDirectoryScope(string path)
            {
                Path = path;
            }

            public string Path { get; }

            public void Dispose()
            {
                DeleteDirectory(Path);
            }
        }
    }
}
