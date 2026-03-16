using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Material_Editor.Infrastructure
{
    internal static class AppIconProvider
    {
        private const string ResourceName = "Material_Editor.assets.bgem.ico";
        private static Icon cachedIcon;

        public static void Apply(Form form)
        {
            if (form == null)
                return;

            var icon = GetIcon();
            if (icon != null)
                form.Icon = icon;
        }

        private static Icon GetIcon()
        {
            if (cachedIcon != null)
                return (Icon)cachedIcon.Clone();

            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream == null)
                return null;

            cachedIcon = new Icon(stream);
            return (Icon)cachedIcon.Clone();
        }
    }
}
