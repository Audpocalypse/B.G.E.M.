using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Material_Editor.Infrastructure
{
    internal static class SplashAnimationProvider
    {
        private const string ResourceName = "Material_Editor.assets.B.gif";
        private const int FrameDelayPropertyId = 0x5100;
        private const int DefaultDurationMilliseconds = 4500;

        public static SplashAnimationResource Load()
        {
            using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (resourceStream == null)
                return null;

            MemoryStream buffer = null;
            try
            {
                buffer = new MemoryStream();
                resourceStream.CopyTo(buffer);
                buffer.Position = 0;

                Image image = Image.FromStream(buffer);
                int durationMilliseconds = GetAnimationDurationMilliseconds(image);
                return new SplashAnimationResource(buffer, image, durationMilliseconds);
            }
            catch
            {
                buffer?.Dispose();
                return null;
            }
        }

        private static int GetAnimationDurationMilliseconds(Image image)
        {
            if (image == null || !image.PropertyIdList.Contains(FrameDelayPropertyId))
                return DefaultDurationMilliseconds;

            try
            {
                PropertyItem frameDelayProperty = image.GetPropertyItem(FrameDelayPropertyId);
                if (frameDelayProperty?.Value == null || frameDelayProperty.Len < sizeof(int))
                    return DefaultDurationMilliseconds;

                int totalCentiseconds = 0;
                for (int offset = 0; offset <= frameDelayProperty.Len - sizeof(int); offset += sizeof(int))
                    totalCentiseconds += BitConverter.ToInt32(frameDelayProperty.Value, offset);

                return totalCentiseconds > 0
                    ? totalCentiseconds * 10
                    : DefaultDurationMilliseconds;
            }
            catch
            {
                return DefaultDurationMilliseconds;
            }
        }
    }

    internal sealed class SplashAnimationResource : IDisposable
    {
        private readonly Stream backingStream;

        public SplashAnimationResource(Stream backingStream, Image image, int durationMilliseconds)
        {
            this.backingStream = backingStream ?? throw new ArgumentNullException(nameof(backingStream));
            Image = image ?? throw new ArgumentNullException(nameof(image));
            DurationMilliseconds = durationMilliseconds > 0 ? durationMilliseconds : 4500;
        }

        public Image Image { get; }

        public int DurationMilliseconds { get; }

        public void Dispose()
        {
            Image.Dispose();
            backingStream.Dispose();
        }
    }
}
