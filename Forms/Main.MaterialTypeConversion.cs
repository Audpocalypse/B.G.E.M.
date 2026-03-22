using MaterialLib;
using Material_Editor.Models;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private BaseMaterialFile CreateMaterialForType(MaterialType type, uint? version = null)
        {
            BaseMaterialFile material = type switch
            {
                MaterialType.Effect => new BGEM(),
                _ => new BGSM(),
            };

            material.Version = version ?? (uint)(config.GameVersion == Game.FO76 ? DefaultVersionFO76 : DefaultVersionFO4);
            return material;
        }

        private void RebuildSingleEditorForMaterialType(MaterialType targetType)
        {
            BaseMaterialFile sourceState = CaptureCurrentSingleEditorState();
            BaseMaterialFile targetMaterial = CreateMaterialForType(targetType, sourceState?.Version);

            if (sourceState != null)
                CopySharedMaterialValues(sourceState, targetMaterial);

            RunWithSingleEditorLoadingOverlay("Switching editor type...", () =>
            {
                SuspendAll();
                try
                {
                    CreateMaterialControls(targetMaterial);
                }
                finally
                {
                    ResumeAll();
                }
            });
        }

        private BaseMaterialFile CaptureCurrentSingleEditorState()
        {
            if (currentMaterial == null)
                return null;

            BaseMaterialFile snapshot = currentMaterial is BGEM
                ? new BGEM()
                : new BGSM();

            GetMaterialValues(snapshot);
            return snapshot;
        }

        private static void CopySharedMaterialValues(BaseMaterialFile source, BaseMaterialFile target)
        {
            if (source == null || target == null)
                return;

            target.Version = source.Version;
            target.TileU = source.TileU;
            target.TileV = source.TileV;
            target.UOffset = source.UOffset;
            target.VOffset = source.VOffset;
            target.UScale = source.UScale;
            target.VScale = source.VScale;
            target.Alpha = source.Alpha;
            target.AlphaBlendMode = source.AlphaBlendMode;
            target.AlphaTestRef = source.AlphaTestRef;
            target.AlphaTest = source.AlphaTest;
            target.ZBufferWrite = source.ZBufferWrite;
            target.ZBufferTest = source.ZBufferTest;
            target.ScreenSpaceReflections = source.ScreenSpaceReflections;
            target.WetnessControlScreenSpaceReflections = source.WetnessControlScreenSpaceReflections;
            target.Decal = source.Decal;
            target.TwoSided = source.TwoSided;
            target.DecalNoFade = source.DecalNoFade;
            target.NonOccluder = source.NonOccluder;
            target.Refraction = source.Refraction;
            target.RefractionFalloff = source.RefractionFalloff;
            target.RefractionPower = source.RefractionPower;
            target.EnvironmentMapping = source.EnvironmentMapping;
            target.EnvironmentMappingMaskScale = source.EnvironmentMappingMaskScale;
            target.DepthBias = source.DepthBias;
            target.GrayscaleToPaletteColor = source.GrayscaleToPaletteColor;
            target.MaskWrites = source.MaskWrites;
        }
    }
}
