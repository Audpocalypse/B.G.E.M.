using System;

namespace Material_Editor.Infrastructure
{
    internal static class ContentPathHelper
    {
        public static bool TryGetRelativePath(string path, string rootFolderName, out string relativePath)
        {
            relativePath = string.Empty;
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(rootFolderName))
                return false;

            string normalizedPath = NormalizeSeparators(path);
            string normalizedRoot = rootFolderName.Trim().Trim('/', '\\');
            string token = normalizedRoot + "/";

            if (normalizedPath.StartsWith(token, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = normalizedPath[token.Length..];
                return !string.IsNullOrWhiteSpace(relativePath);
            }

            int index = normalizedPath.IndexOf("/" + token, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return false;

            int startIndex = index + token.Length + 1;
            if (startIndex >= normalizedPath.Length)
                return false;

            relativePath = normalizedPath[startIndex..];
            return !string.IsNullOrWhiteSpace(relativePath);
        }

        public static string NormalizeSeparators(string path)
        {
            return (path ?? string.Empty).Trim().Replace('\\', '/');
        }
    }
}
