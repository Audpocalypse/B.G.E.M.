using MaterialLib;
using System;
using System.IO;

namespace Material_Editor.Services
{
    internal static class MaterialFileCloner
    {
        public static BaseMaterialFile Clone(BaseMaterialFile source)
        {
            if (source == null)
                return null;

            string tempPath = null;
            try
            {
                tempPath = Path.GetTempFileName();
                using var stream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                if (!source.Save(stream))
                    return source;

                stream.Position = 0;
                var clone = (BaseMaterialFile)Activator.CreateInstance(source.GetType());
                if (clone == null)
                    return source;

                if (!clone.Open(stream))
                    return source;

                return clone;
            }
            catch
            {
                return source;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
