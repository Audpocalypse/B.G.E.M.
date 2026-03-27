using MaterialLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

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
                    return CloneByPropertyCopy(source);

                stream.Position = 0;
                var clone = (BaseMaterialFile)Activator.CreateInstance(source.GetType());
                if (clone == null)
                    return CloneByPropertyCopy(source);

                if (!clone.Open(stream))
                    return CloneByPropertyCopy(source);

                return clone;
            }
            catch
            {
                return CloneByPropertyCopy(source);
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

        private static BaseMaterialFile CloneByPropertyCopy(BaseMaterialFile source)
        {
            if (source == null)
                return null;

            try
            {
                var clone = (BaseMaterialFile)Activator.CreateInstance(source.GetType());
                if (clone == null)
                    return source;

                PropertyInfo[] properties = source.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                    .ToArray();

                foreach (PropertyInfo property in properties)
                    property.SetValue(clone, property.GetValue(source));

                return clone;
            }
            catch
            {
                return source;
            }
        }
    }
}
