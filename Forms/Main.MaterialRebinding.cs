using MaterialLib;
using System;
using System.Linq;
using System.Reflection;
using Material_Editor.Controls;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private bool TryReuseMaterialControls(BaseMaterialFile file)
        {
            if (file == null
                || currentMaterial == null
                || currentMaterial.GetType() != file.GetType()
                || !HasReusableControlTree(file))
            {
                return false;
            }

            CopyMaterialState(file, currentMaterial);
            ApplyMaterialToExistingControls(currentMaterial);
            ControlFactory.UpdateVisibility();
            ApplyCurrentAppearance();
            originalMaterial = CloneMaterial(currentMaterial);
            return true;
        }

        private static bool HasReusableControlTree(BaseMaterialFile file)
        {
            if (file == null
                || ControlFactory.Find(ControlNames.TileU) == null
                || ControlFactory.Find(ControlNames.MaskWrites) == null)
            {
                return false;
            }

            return file is BGEM
                ? ControlFactory.Find(ControlNames.BaseTexture) != null
                : ControlFactory.Find(ControlNames.Diffuse) != null;
        }

        private static void CopyMaterialState(BaseMaterialFile source, BaseMaterialFile target)
        {
            if (source == null || target == null || source.GetType() != target.GetType())
                return;

            PropertyInfo[] properties = source.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                .ToArray();

            foreach (PropertyInfo property in properties)
                property.SetValue(target, property.GetValue(source));
        }

        private void ApplyMaterialToExistingControls(BaseMaterialFile file)
        {
            if (file == null)
                return;

            int alphaBlendMode = (int)file.AlphaBlendMode;
            if (alphaBlendMode < 0 || alphaBlendMode > 4)
                alphaBlendMode = 0;

            ControlFactory.SetProperty(ControlNames.TileU, file.TileU);
            ControlFactory.SetProperty(ControlNames.TileV, file.TileV);
            ControlFactory.SetProperty(ControlNames.OffsetU, file.UOffset);
            ControlFactory.SetProperty(ControlNames.OffsetV, file.VOffset);
            ControlFactory.SetProperty(ControlNames.ScaleU, file.UScale);
            ControlFactory.SetProperty(ControlNames.ScaleV, file.VScale);
            ControlFactory.SetProperty(ControlNames.Alpha, file.Alpha);
            ControlFactory.SetProperty(ControlNames.AlphaBlendMode, alphaBlendMode);
            ControlFactory.SetProperty(ControlNames.AlphaTestReference, file.AlphaTestRef);
            ControlFactory.SetProperty(ControlNames.AlphaTest, file.AlphaTest);
            ControlFactory.SetProperty(ControlNames.ZBufferWrite, file.ZBufferWrite);
            ControlFactory.SetProperty(ControlNames.ZBufferTest, file.ZBufferTest);
            ControlFactory.SetProperty(ControlNames.ScreenSpaceReflections, file.ScreenSpaceReflections);
            ControlFactory.SetProperty(ControlNames.WetnessControlSSR, file.WetnessControlScreenSpaceReflections);
            ControlFactory.SetProperty(ControlNames.Decal, file.Decal);
            ControlFactory.SetProperty(ControlNames.TwoSided, file.TwoSided);
            ControlFactory.SetProperty(ControlNames.DecalNoFade, file.DecalNoFade);
            ControlFactory.SetProperty(ControlNames.NonOccluder, file.NonOccluder);
            ControlFactory.SetProperty(ControlNames.Refraction, file.Refraction);
            ControlFactory.SetProperty(ControlNames.RefractionFalloff, file.RefractionFalloff);
            ControlFactory.SetProperty(ControlNames.RefractionPower, file.RefractionPower);
            ControlFactory.SetProperty(ControlNames.EnvironmentMapping, file.EnvironmentMapping);
            ControlFactory.SetProperty(ControlNames.EnvironmentMaskScale, file.EnvironmentMappingMaskScale);
            ControlFactory.SetProperty(ControlNames.DepthBias, file.DepthBias);
            ControlFactory.SetProperty(ControlNames.GrayscaleToPaletteColor, file.GrayscaleToPaletteColor);
            ControlFactory.SetProperty(ControlNames.MaskWrites, (int)file.MaskWrites);

            if (file is BGSM bgsm)
            {
                ControlFactory.SetProperty(ControlNames.Diffuse, bgsm.DiffuseTexture);
                ControlFactory.SetProperty(ControlNames.Normal, bgsm.NormalTexture);
                ControlFactory.SetProperty(ControlNames.SmoothSpec, bgsm.SmoothSpecTexture);
                ControlFactory.SetProperty(ControlNames.Greyscale, bgsm.GreyscaleTexture);
                ControlFactory.SetProperty(ControlNames.Environment, bgsm.EnvmapTexture);
                ControlFactory.SetProperty(ControlNames.Glow, bgsm.GlowTexture);
                ControlFactory.SetProperty(ControlNames.InnerLayer, bgsm.InnerLayerTexture);
                ControlFactory.SetProperty(ControlNames.Wrinkles, bgsm.WrinklesTexture);
                ControlFactory.SetProperty(ControlNames.Displacement, bgsm.DisplacementTexture);
                ControlFactory.SetProperty(ControlNames.Specular, bgsm.SpecularTexture);
                ControlFactory.SetProperty(ControlNames.Lighting, bgsm.LightingTexture);
                ControlFactory.SetProperty(ControlNames.Flow, bgsm.FlowTexture);
                ControlFactory.SetProperty(ControlNames.DistanceFieldAlpha, bgsm.DistanceFieldAlphaTexture);
                ControlFactory.SetProperty(ControlNames.EnableEditorAlphaRef, bgsm.EnableEditorAlphaRef);
                ControlFactory.SetProperty(ControlNames.Translucency, bgsm.Translucency);
                ControlFactory.SetProperty(ControlNames.TranslucencyThickObject, bgsm.TranslucencyThickObject);
                ControlFactory.SetProperty(ControlNames.TranslucencyAlbSubsurfColor, bgsm.TranslucencyMixAlbedoWithSubsurfaceColor);
                ControlFactory.SetProperty(ControlNames.TranslucencySubsurfaceColor, UIntToColor(bgsm.TranslucencySubsurfaceColor));
                ControlFactory.SetProperty(ControlNames.TranslucencyTransmissiveScale, bgsm.TranslucencyTransmissiveScale);
                ControlFactory.SetProperty(ControlNames.TranslucencyTurbulence, bgsm.TranslucencyTurbulence);
                ControlFactory.SetProperty(ControlNames.RimLighting, bgsm.RimLighting);
                ControlFactory.SetProperty(ControlNames.RimPower, bgsm.RimPower);
                ControlFactory.SetProperty(ControlNames.BacklightPower, bgsm.BackLightPower);
                ControlFactory.SetProperty(ControlNames.SubsurfaceLighting, bgsm.SubsurfaceLighting);
                ControlFactory.SetProperty(ControlNames.SubsurfaceLightingRolloff, bgsm.SubsurfaceLightingRolloff);
                ControlFactory.SetProperty(ControlNames.SpecularEnabled, bgsm.SpecularEnabled);
                ControlFactory.SetProperty(ControlNames.SpecularColor, UIntToColor(bgsm.SpecularColor));
                ControlFactory.SetProperty(ControlNames.SpecularMultiplier, bgsm.SpecularMult);
                ControlFactory.SetProperty(ControlNames.Smoothness, bgsm.Smoothness);
                ControlFactory.SetProperty(ControlNames.FresnelPower, bgsm.FresnelPower);
                ControlFactory.SetProperty(ControlNames.WetSpecScale, bgsm.WetnessControlSpecScale);
                ControlFactory.SetProperty(ControlNames.WetSpecPowerScale, bgsm.WetnessControlSpecPowerScale);
                ControlFactory.SetProperty(ControlNames.WetSpecMinVar, bgsm.WetnessControlSpecMinvar);
                ControlFactory.SetProperty(ControlNames.WetEnvMapScale, bgsm.WetnessControlEnvMapScale);
                ControlFactory.SetProperty(ControlNames.WetFresnelPower, bgsm.WetnessControlFresnelPower);
                ControlFactory.SetProperty(ControlNames.WetMetalness, bgsm.WetnessControlMetalness);
                ControlFactory.SetProperty(ControlNames.PBR, bgsm.PBR);
                ControlFactory.SetProperty(ControlNames.CustomPorosity, bgsm.CustomPorosity);
                ControlFactory.SetProperty(ControlNames.PorosityValue, bgsm.PorosityValue);
                ControlFactory.SetProperty(ControlNames.RootMaterialPath, bgsm.RootMaterialPath);
                ControlFactory.SetProperty(ControlNames.AnisoLighting, bgsm.AnisoLighting);
                ControlFactory.SetProperty(ControlNames.EmittanceEnabled, bgsm.EmitEnabled);
                ControlFactory.SetProperty(ControlNames.EmittanceColor, UIntToColor(bgsm.EmittanceColor));
                ControlFactory.SetProperty(ControlNames.EmittanceMultiplier, bgsm.EmittanceMult);
                ControlFactory.SetProperty(ControlNames.ModelSpaceNormals, bgsm.ModelSpaceNormals);
                ControlFactory.SetProperty(ControlNames.ExternalEmittance, bgsm.ExternalEmittance);
                ControlFactory.SetProperty(ControlNames.LumEmittance, bgsm.LumEmittance);
                ControlFactory.SetProperty(ControlNames.AdaptativeEmissive, bgsm.UseAdaptativeEmissive);
                ControlFactory.SetProperty(ControlNames.AdaptEmissiveExposureOffset, bgsm.AdaptativeEmissive_ExposureOffset);
                ControlFactory.SetProperty(ControlNames.AdaptEmissiveFinalExposureMin, bgsm.AdaptativeEmissive_FinalExposureMin);
                ControlFactory.SetProperty(ControlNames.AdaptEmissiveFinalExposureMax, bgsm.AdaptativeEmissive_FinalExposureMax);
                ControlFactory.SetProperty(ControlNames.BackLighting, bgsm.BackLighting);
                ControlFactory.SetProperty(ControlNames.ReceiveShadows, bgsm.ReceiveShadows);
                ControlFactory.SetProperty(ControlNames.HideSecret, bgsm.HideSecret);
                ControlFactory.SetProperty(ControlNames.CastShadows, bgsm.CastShadows);
                ControlFactory.SetProperty(ControlNames.DissolveFade, bgsm.DissolveFade);
                ControlFactory.SetProperty(ControlNames.AssumeShadowmask, bgsm.AssumeShadowmask);
                ControlFactory.SetProperty(ControlNames.Glowmap, bgsm.Glowmap);
                ControlFactory.SetProperty(ControlNames.EnvironmentMapWindow, bgsm.EnvironmentMappingWindow);
                ControlFactory.SetProperty(ControlNames.EnvironmentMapEye, bgsm.EnvironmentMappingEye);
                ControlFactory.SetProperty(ControlNames.Hair, bgsm.Hair);
                ControlFactory.SetProperty(ControlNames.HairTintColor, UIntToColor(bgsm.HairTintColor));
                ControlFactory.SetProperty(ControlNames.Tree, bgsm.Tree);
                ControlFactory.SetProperty(ControlNames.Facegen, bgsm.Facegen);
                ControlFactory.SetProperty(ControlNames.SkinTint, bgsm.SkinTint);
                ControlFactory.SetProperty(ControlNames.Tessellate, bgsm.Tessellate);
                ControlFactory.SetProperty(ControlNames.DisplacementTexBias, bgsm.DisplacementTextureBias);
                ControlFactory.SetProperty(ControlNames.DisplacementTexScale, bgsm.DisplacementTextureScale);
                ControlFactory.SetProperty(ControlNames.TessellationPNScale, bgsm.TessellationPnScale);
                ControlFactory.SetProperty(ControlNames.TessellationBaseFactor, bgsm.TessellationBaseFactor);
                ControlFactory.SetProperty(ControlNames.TessellationFadeDistance, bgsm.TessellationFadeDistance);
                ControlFactory.SetProperty(ControlNames.GrayscaleToPaletteScale, bgsm.GrayscaleToPaletteScale);
                ControlFactory.SetProperty(ControlNames.SkewSpecularAlpha, bgsm.SkewSpecularAlpha);
                ControlFactory.SetProperty(ControlNames.Terrain, bgsm.Terrain);
                ControlFactory.SetProperty(ControlNames.UnkInt1BGSM, bgsm.UnkInt1);
                ControlFactory.SetProperty(ControlNames.TerrainThresholdFalloff, bgsm.TerrainThresholdFalloff);
                ControlFactory.SetProperty(ControlNames.TerrainTilingDistance, bgsm.TerrainTilingDistance);
                ControlFactory.SetProperty(ControlNames.TerrainRotationAngle, bgsm.TerrainRotationAngle);
            }
            else if (file is BGEM bgem)
            {
                ControlFactory.SetProperty(ControlNames.BaseTexture, bgem.BaseTexture);
                ControlFactory.SetProperty(ControlNames.GrayscaleTexture, bgem.GrayscaleTexture);
                ControlFactory.SetProperty(ControlNames.EnvmapTexture, bgem.EnvmapTexture);
                ControlFactory.SetProperty(ControlNames.NormalTexture, bgem.NormalTexture);
                ControlFactory.SetProperty(ControlNames.EnvmapMaskTexture, bgem.EnvmapMaskTexture);
                ControlFactory.SetProperty(ControlNames.SpecularTexture, bgem.SpecularTexture);
                ControlFactory.SetProperty(ControlNames.LightingTexture, bgem.LightingTexture);
                ControlFactory.SetProperty(ControlNames.GlowTexture, bgem.GlowTexture);
                ControlFactory.SetProperty(ControlNames.GlassRoughnessScratch, bgem.GlassRoughnessScratch);
                ControlFactory.SetProperty(ControlNames.GlassDirtOverlay, bgem.GlassDirtOverlay);
                ControlFactory.SetProperty(ControlNames.GlassEnabled, bgem.GlassEnabled);
                ControlFactory.SetProperty(ControlNames.GlassFresnelColor, UIntToColor(bgem.GlassFresnelColor));
                ControlFactory.SetProperty(ControlNames.GlassBlurScaleBase, bgem.GlassBlurScaleBase);
                ControlFactory.SetProperty(ControlNames.GlassBlurScaleFactor, bgem.GlassBlurScaleFactor);
                ControlFactory.SetProperty(ControlNames.GlassRefractionScaleBase, bgem.GlassRefractionScaleBase);
                ControlFactory.SetProperty(ControlNames.EnvMapping, bgem.EnvironmentMapping);
                ControlFactory.SetProperty(ControlNames.EnvMappingMaskScale, bgem.EnvironmentMappingMaskScale);
                ControlFactory.SetProperty(ControlNames.BloodEnabled, bgem.BloodEnabled);
                ControlFactory.SetProperty(ControlNames.EffectLightingEnabled, bgem.EffectLightingEnabled);
                ControlFactory.SetProperty(ControlNames.FalloffEnabled, bgem.FalloffEnabled);
                ControlFactory.SetProperty(ControlNames.FalloffColorEnabled, bgem.FalloffColorEnabled);
                ControlFactory.SetProperty(ControlNames.GrayscaleToPaletteAlpha, bgem.GrayscaleToPaletteAlpha);
                ControlFactory.SetProperty(ControlNames.SoftEnabled, bgem.SoftEnabled);
                ControlFactory.SetProperty(ControlNames.BaseColor, UIntToColor(bgem.BaseColor));
                ControlFactory.SetProperty(ControlNames.BaseColorScale, bgem.BaseColorScale);
                ControlFactory.SetProperty(ControlNames.FalloffStartAngle, bgem.FalloffStartAngle);
                ControlFactory.SetProperty(ControlNames.FalloffStopAngle, bgem.FalloffStopAngle);
                ControlFactory.SetProperty(ControlNames.FalloffStartOpacity, bgem.FalloffStartOpacity);
                ControlFactory.SetProperty(ControlNames.FalloffStopOpacity, bgem.FalloffStopOpacity);
                ControlFactory.SetProperty(ControlNames.LightingInfluence, bgem.LightingInfluence);
                ControlFactory.SetProperty(ControlNames.EnvmapMinLOD, bgem.EnvmapMinLOD);
                ControlFactory.SetProperty(ControlNames.SoftDepth, bgem.SoftDepth);
                ControlFactory.SetProperty(ControlNames.EmitColor, UIntToColor(bgem.EmittanceColor));
                ControlFactory.SetProperty(ControlNames.AdaptativeEmissiveExposureOffset, bgem.AdaptativeEmissive_ExposureOffset);
                ControlFactory.SetProperty(ControlNames.AdaptativeEmissiveFinalExposureMin, bgem.AdaptativeEmissive_FinalExposureMin);
                ControlFactory.SetProperty(ControlNames.AdaptativeEmissiveFinalExposureMax, bgem.AdaptativeEmissive_FinalExposureMax);
                ControlFactory.SetProperty(ControlNames.EffectGlowmap, bgem.Glowmap);
                ControlFactory.SetProperty(ControlNames.EffectPBRSpecular, bgem.EffectPbrSpecular);
            }
        }
    }
}
