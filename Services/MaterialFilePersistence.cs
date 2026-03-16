using MaterialLib;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

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

        public static void SaveMaterial(string filePath, BaseMaterialFile material, bool asJson)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            if (asJson)
            {
                using var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  ");
                var serializer = new DataContractJsonSerializer(material.GetType(), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                serializer.WriteObject(writer, material);
                writer.Flush();
            }
            else if (!material.Save(stream))
            {
                throw new IOException("Failed to write binary material.");
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
    }
}
