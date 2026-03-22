using MaterialLib;
using System;
using System.Drawing;
using Material_Editor.Controls;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private void BuildMaterialSectionControls()
        {
            CreateMaterialSpecularSurfaceControls();
            CreateMaterialLightingControls();
            CreateMaterialEmittanceControls();
            CreateMaterialAdaptiveEmissiveControls();
            CreateMaterialWetnessControls();
            CreateMaterialRenderingFlagsControls();
            CreateMaterialShaderFeatureControls();
            CreateMaterialHairControls();
            CreateMaterialTessellationControls();
            CreateMaterialTerrainControls();
        }

        private void BuildEffectSectionControls()
        {
            CreateEffectSurfaceColorControls();
            CreateEffectFalloffControls();
            CreateEffectSoftnessControls();
            CreateEffectEnvironmentMappingControls();
            CreateEffectGlassControls();
            CreateEffectFeatureControls();
        }

        private void CreateGeneralControls(BaseMaterialFile file)
        {
            ControlFactory.CreateControl(layoutGeneral, ControlNames.TileU, file.TileU);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.TileV, file.TileV);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.OffsetU, file.UOffset);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.OffsetV, file.VOffset);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ScaleU, file.UScale);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ScaleV, file.VScale);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.Alpha, file.Alpha);

            int alphaBlendMode = (int)file.AlphaBlendMode;
            if (alphaBlendMode < 0 || alphaBlendMode > 4)
                alphaBlendMode = 0;

            ControlFactory.CreateDropdownControl(layoutGeneral, ControlNames.AlphaBlendMode,
                ["Unknown", "None", "Standard", "Additive", "Multiplicative"], alphaBlendMode);

            ControlFactory.CreateControl(layoutGeneral, ControlNames.AlphaTestReference, file.AlphaTestRef);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.AlphaTest, file.AlphaTest);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ZBufferWrite, file.ZBufferWrite);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ZBufferTest, file.ZBufferTest);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ScreenSpaceReflections, file.ScreenSpaceReflections);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.WetnessControlSSR, file.WetnessControlScreenSpaceReflections);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.Decal, file.Decal);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.TwoSided, file.TwoSided);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.DecalNoFade, file.DecalNoFade);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.NonOccluder, file.NonOccluder);

            ControlFactory.CreateControl(layoutGeneral, ControlNames.Refraction, file.Refraction, null, _ =>
            {
                ControlFactory.UpdateVisibility(
                    ControlNames.RefractionFalloff,
                    ControlNames.RefractionPower);
                OnChanged();
            });
            ControlFactory.CreateControl(layoutGeneral, ControlNames.RefractionFalloff, file.RefractionFalloff, RefractionVisibility);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.RefractionPower, file.RefractionPower, RefractionVisibility);

            ControlFactory.CreateControl(layoutGeneral, ControlNames.EnvironmentMapping, file.EnvironmentMapping, _ => file.Version < 10, _ =>
            {
                ControlFactory.UpdateVisibility(ControlNames.EnvironmentMaskScale);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutGeneral, ControlNames.EnvironmentMaskScale, file.EnvironmentMappingMaskScale, _ =>
            {
                if (!ControlFactory.GetProperty(ControlNames.EnvironmentMapping, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 10;
            });
            ControlFactory.CreateControl(layoutGeneral, ControlNames.DepthBias, file.DepthBias, _ => file.Version >= 10);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.GrayscaleToPaletteColor, file.GrayscaleToPaletteColor);
            ControlFactory.CreateFlagControl(layoutGeneral, ControlNames.MaskWrites, Enum.GetNames(typeof(BaseMaterialFile.MaskWriteFlags)), (int)file.MaskWrites, _ => file.Version >= 6);
        }

        private void CreateMaterialPathControls(BGSM bgsm, Font fileFont)
        {
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Diffuse, fileFont, FileControl.FileType.Texture, bgsm.DiffuseTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Normal, fileFont, FileControl.FileType.Texture, bgsm.NormalTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.SmoothSpec, fileFont, FileControl.FileType.Texture, bgsm.SmoothSpecTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Greyscale, fileFont, FileControl.FileType.Texture, bgsm.GreyscaleTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Environment, fileFont, FileControl.FileType.Texture, bgsm.EnvmapTexture, _ => currentMaterial.Version <= 2);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Glow, fileFont, FileControl.FileType.Texture, bgsm.GlowTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.InnerLayer, fileFont, FileControl.FileType.Texture, bgsm.InnerLayerTexture, _ => currentMaterial.Version <= 2);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Wrinkles, fileFont, FileControl.FileType.Texture, bgsm.WrinklesTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Displacement, fileFont, FileControl.FileType.Texture, bgsm.DisplacementTexture, _ => currentMaterial.Version <= 2);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Specular, fileFont, FileControl.FileType.Texture, bgsm.SpecularTexture, _ => currentMaterial.Version > 2);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Lighting, fileFont, FileControl.FileType.Texture, bgsm.LightingTexture, _ => currentMaterial.Version > 2);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Flow, fileFont, FileControl.FileType.Texture, bgsm.FlowTexture, _ => currentMaterial.Version > 2);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.DistanceFieldAlpha, fileFont, FileControl.FileType.Texture, bgsm.DistanceFieldAlphaTexture, _ => currentMaterial.Version > 2);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.RootMaterialPath, fileFont, FileControl.FileType.Material, bgsm.RootMaterialPath);
        }

        private void CreateEffectPathControls(BGEM bgem, Font fileFont)
        {
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.BaseTexture, fileFont, FileControl.FileType.Texture, bgem.BaseTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GrayscaleTexture, fileFont, FileControl.FileType.Texture, bgem.GrayscaleTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.EnvmapTexture, fileFont, FileControl.FileType.Texture, bgem.EnvmapTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.NormalTexture, fileFont, FileControl.FileType.Texture, bgem.NormalTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.EnvmapMaskTexture, fileFont, FileControl.FileType.Texture, bgem.EnvmapMaskTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.SpecularTexture, fileFont, FileControl.FileType.Texture, bgem.SpecularTexture, _ => currentMaterial.Version >= 11);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.LightingTexture, fileFont, FileControl.FileType.Texture, bgem.LightingTexture, _ => currentMaterial.Version >= 11);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlowTexture, fileFont, FileControl.FileType.Texture, bgem.GlowTexture, _ => currentMaterial.Version >= 11);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlassRoughnessScratch, fileFont, FileControl.FileType.Texture, bgem.GlassRoughnessScratch, _ => currentMaterial.Version >= 21);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlassDirtOverlay, fileFont, FileControl.FileType.Texture, bgem.GlassDirtOverlay, _ => currentMaterial.Version >= 21);
        }

        private void CreateMaterialSpecularSurfaceControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.SpecularEnabled, bgsm.SpecularEnabled, null, _ =>
            {
                ControlFactory.UpdateVisibility(
                    ControlNames.SpecularColor,
                    ControlNames.SpecularMultiplier);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.SpecularEnabled,
                () => IsControlEnabled(ControlNames.SpecularEnabled),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.SpecularColor, UIntToColor(bgsm.SpecularColor), SpecularColorAndMultiplierVisibility);
                    ControlFactory.CreateDetachedControl(ControlNames.SpecularMultiplier, bgsm.SpecularMult, SpecularColorAndMultiplierVisibility);
                },
                ControlNames.SpecularColor,
                ControlNames.SpecularMultiplier);
            ControlFactory.CreateDetachedControl(ControlNames.Smoothness, bgsm.Smoothness);
            ControlFactory.CreateDetachedControl(ControlNames.FresnelPower, bgsm.FresnelPower);
            ControlFactory.CreateDetachedControl(ControlNames.AnisoLighting, bgsm.AnisoLighting);
            ControlFactory.CreateDetachedControl(ControlNames.GrayscaleToPaletteScale, bgsm.GrayscaleToPaletteScale);
            ControlFactory.CreateDetachedControl(ControlNames.SkewSpecularAlpha, bgsm.SkewSpecularAlpha, _ => currentMaterial.Version >= 1);
        }

        private void CreateMaterialLightingControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.RimLighting, bgsm.RimLighting, _ => currentMaterial.Version < 8, _ =>
            {
                ControlFactory.UpdateVisibility(ControlNames.RimPower);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.RimLighting,
                () => IsControlEnabled(ControlNames.RimLighting),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.RimPower, bgsm.RimPower, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.RimLighting, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version < 8;
                    });
                },
                ControlNames.RimPower);
            ControlFactory.CreateDetachedControl(ControlNames.BackLighting, bgsm.BackLighting, _ => currentMaterial.Version < 8);
            ControlFactory.CreateDetachedControl(ControlNames.BacklightPower, bgsm.BackLightPower, _ => currentMaterial.Version < 8);
            ControlFactory.CreateDetachedControl(ControlNames.SubsurfaceLighting, bgsm.SubsurfaceLighting, _ => currentMaterial.Version < 8, _ =>
            {
                ControlFactory.UpdateVisibility(ControlNames.SubsurfaceLightingRolloff);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.SubsurfaceLighting,
                () => IsControlEnabled(ControlNames.SubsurfaceLighting),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.SubsurfaceLightingRolloff, bgsm.SubsurfaceLightingRolloff, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.SubsurfaceLighting, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version < 8;
                    });
                },
                ControlNames.SubsurfaceLightingRolloff);
            ControlFactory.CreateDetachedControl(ControlNames.Translucency, bgsm.Translucency, _ => currentMaterial.Version >= 8);
            ControlFactory.CreateDetachedControl(ControlNames.TranslucencyThickObject, bgsm.TranslucencyThickObject, _ => currentMaterial.Version >= 8);
            ControlFactory.CreateDetachedControl(ControlNames.TranslucencyAlbSubsurfColor, bgsm.TranslucencyMixAlbedoWithSubsurfaceColor, _ => currentMaterial.Version >= 8);
            ControlFactory.CreateDetachedControl(ControlNames.TranslucencySubsurfaceColor, UIntToColor(bgsm.TranslucencySubsurfaceColor), _ => currentMaterial.Version >= 8);
            ControlFactory.CreateDetachedControl(ControlNames.TranslucencyTransmissiveScale, bgsm.TranslucencyTransmissiveScale, _ => currentMaterial.Version >= 8);
            ControlFactory.CreateDetachedControl(ControlNames.TranslucencyTurbulence, bgsm.TranslucencyTurbulence, _ => currentMaterial.Version >= 8);
        }

        private void CreateMaterialEmittanceControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.EmittanceEnabled, bgsm.EmitEnabled, null, _ =>
            {
                ControlFactory.UpdateVisibility(
                    ControlNames.EmittanceColor,
                    ControlNames.EmittanceMultiplier);
                OnChanged();
            });
            ControlFactory.CreateDetachedControl(ControlNames.ExternalEmittance, bgsm.ExternalEmittance);
            RegisterDeferredControllerGroup(ControlNames.EmittanceEnabled,
                () => IsControlEnabled(ControlNames.EmittanceEnabled),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.EmittanceColor, UIntToColor(bgsm.EmittanceColor), EmittanceColorAndMultiplierVisibility);
                    ControlFactory.CreateDetachedControl(ControlNames.EmittanceMultiplier, bgsm.EmittanceMult, EmittanceColorAndMultiplierVisibility);
                },
                ControlNames.EmittanceColor,
                ControlNames.EmittanceMultiplier);
            ControlFactory.CreateDetachedControl(ControlNames.LumEmittance, bgsm.LumEmittance, _ => currentMaterial.Version >= 12);
        }

        private void CreateMaterialAdaptiveEmissiveControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.AdaptativeEmissive, bgsm.UseAdaptativeEmissive, _ => currentMaterial.Version >= 13, _ =>
            {
                ControlFactory.UpdateVisibility(
                    ControlNames.AdaptEmissiveExposureOffset,
                    ControlNames.AdaptEmissiveFinalExposureMin,
                    ControlNames.AdaptEmissiveFinalExposureMax);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.AdaptativeEmissive,
                () => IsControlEnabled(ControlNames.AdaptativeEmissive),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.AdaptEmissiveExposureOffset, bgsm.AdaptativeEmissive_ExposureOffset, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.AdaptativeEmissive, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 13;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.AdaptEmissiveFinalExposureMin, bgsm.AdaptativeEmissive_FinalExposureMin, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.AdaptativeEmissive, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 13;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.AdaptEmissiveFinalExposureMax, bgsm.AdaptativeEmissive_FinalExposureMax, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.AdaptativeEmissive, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 13;
                    });
                },
                ControlNames.AdaptEmissiveExposureOffset,
                ControlNames.AdaptEmissiveFinalExposureMin,
                ControlNames.AdaptEmissiveFinalExposureMax);
        }

        private void CreateMaterialWetnessControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.WetSpecScale, bgsm.WetnessControlSpecScale);
            ControlFactory.CreateDetachedControl(ControlNames.WetSpecPowerScale, bgsm.WetnessControlSpecPowerScale);
            ControlFactory.CreateDetachedControl(ControlNames.WetSpecMinVar, bgsm.WetnessControlSpecMinvar);
            ControlFactory.CreateDetachedControl(ControlNames.WetEnvMapScale, bgsm.WetnessControlEnvMapScale, _ => currentMaterial.Version < 10);
            ControlFactory.CreateDetachedControl(ControlNames.WetFresnelPower, bgsm.WetnessControlFresnelPower);
            ControlFactory.CreateDetachedControl(ControlNames.WetMetalness, bgsm.WetnessControlMetalness);
        }

        private void CreateMaterialRenderingFlagsControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.EnableEditorAlphaRef, bgsm.EnableEditorAlphaRef);
            ControlFactory.CreateDetachedControl(ControlNames.ModelSpaceNormals, bgsm.ModelSpaceNormals);
            ControlFactory.CreateDetachedControl(ControlNames.ReceiveShadows, bgsm.ReceiveShadows);
            ControlFactory.CreateDetachedControl(ControlNames.CastShadows, bgsm.CastShadows);
            ControlFactory.CreateDetachedControl(ControlNames.AssumeShadowmask, bgsm.AssumeShadowmask);
            ControlFactory.CreateDetachedControl(ControlNames.HideSecret, bgsm.HideSecret);
            ControlFactory.CreateDetachedControl(ControlNames.DissolveFade, bgsm.DissolveFade);
            ControlFactory.CreateDetachedControl(ControlNames.Glowmap, bgsm.Glowmap);
        }

        private void CreateMaterialShaderFeatureControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.Facegen, bgsm.Facegen);
            ControlFactory.CreateDetachedControl(ControlNames.SkinTint, bgsm.SkinTint);
            ControlFactory.CreateDetachedControl(ControlNames.Tree, bgsm.Tree);
            ControlFactory.CreateDetachedControl(ControlNames.EnvironmentMapWindow, bgsm.EnvironmentMappingWindow, _ => currentMaterial.Version < 7);
            ControlFactory.CreateDetachedControl(ControlNames.EnvironmentMapEye, bgsm.EnvironmentMappingEye, _ => currentMaterial.Version < 7);
            ControlFactory.CreateDetachedControl(ControlNames.PBR, bgsm.PBR, _ => currentMaterial.Version > 2);
            ControlFactory.CreateDetachedControl(ControlNames.CustomPorosity, bgsm.CustomPorosity, _ => currentMaterial.Version >= 9);
            ControlFactory.CreateDetachedControl(ControlNames.PorosityValue, bgsm.PorosityValue, _ => currentMaterial.Version >= 9);
        }

        private void CreateMaterialHairControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.Hair, bgsm.Hair, null, _ =>
            {
                ControlFactory.UpdateVisibility(ControlNames.HairTintColor);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.Hair,
                () => IsControlEnabled(ControlNames.Hair),
                () => ControlFactory.CreateDetachedControl(ControlNames.HairTintColor, UIntToColor(bgsm.HairTintColor), HairTintColorVisibility),
                ControlNames.HairTintColor);
        }

        private void CreateMaterialTessellationControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.Tessellate, bgsm.Tessellate, null, _ =>
            {
                ControlFactory.UpdateVisibility(
                    ControlNames.DisplacementTexBias,
                    ControlNames.DisplacementTexScale,
                    ControlNames.TessellationPNScale,
                    ControlNames.TessellationBaseFactor,
                    ControlNames.TessellationFadeDistance);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.Tessellate,
                () => IsControlEnabled(ControlNames.Tessellate),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.DisplacementTexBias, bgsm.DisplacementTextureBias, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Tessellate, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version < 3;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.DisplacementTexScale, bgsm.DisplacementTextureScale, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Tessellate, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version < 3;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.TessellationPNScale, bgsm.TessellationPnScale, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Tessellate, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version < 3;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.TessellationBaseFactor, bgsm.TessellationBaseFactor, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Tessellate, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version < 3;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.TessellationFadeDistance, bgsm.TessellationFadeDistance, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Tessellate, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version < 3;
                    });
                },
                ControlNames.DisplacementTexBias,
                ControlNames.DisplacementTexScale,
                ControlNames.TessellationPNScale,
                ControlNames.TessellationBaseFactor,
                ControlNames.TessellationFadeDistance);
        }

        private void CreateMaterialTerrainControls()
        {
            BGSM bgsm = (BGSM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.Terrain, bgsm.Terrain, _ => currentMaterial.Version >= 3, _ =>
            {
                ControlFactory.UpdateVisibility(
                    ControlNames.UnkInt1BGSM,
                    ControlNames.TerrainThresholdFalloff,
                    ControlNames.TerrainTilingDistance,
                    ControlNames.TerrainRotationAngle);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.Terrain,
                () => IsControlEnabled(ControlNames.Terrain),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.UnkInt1BGSM, bgsm.UnkInt1, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Terrain, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version == 3;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.TerrainThresholdFalloff, bgsm.TerrainThresholdFalloff, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Terrain, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 3;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.TerrainTilingDistance, bgsm.TerrainTilingDistance, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Terrain, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 3;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.TerrainRotationAngle, bgsm.TerrainRotationAngle, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.Terrain, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 3;
                    });
                },
                ControlNames.UnkInt1BGSM,
                ControlNames.TerrainThresholdFalloff,
                ControlNames.TerrainTilingDistance,
                ControlNames.TerrainRotationAngle);
        }

        private void CreateEffectSurfaceColorControls()
        {
            BGEM bgem = (BGEM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.BaseColor, UIntToColor(bgem.BaseColor));
            ControlFactory.CreateDetachedControl(ControlNames.BaseColorScale, bgem.BaseColorScale);
            ControlFactory.CreateDetachedControl(ControlNames.EmitColor, UIntToColor(bgem.EmittanceColor), _ => currentMaterial.Version >= 11);
            ControlFactory.CreateDetachedControl(ControlNames.LightingInfluence, bgem.LightingInfluence);
            ControlFactory.CreateDetachedControl(ControlNames.EnvmapMinLOD, bgem.EnvmapMinLOD);
        }

        private void CreateEffectFalloffControls()
        {
            BGEM bgem = (BGEM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.FalloffEnabled, bgem.FalloffEnabled, null, _ =>
            {
                ControlFactory.UpdateVisibility(
                    ControlNames.FalloffStartAngle,
                    ControlNames.FalloffStopAngle,
                    ControlNames.FalloffStartOpacity,
                    ControlNames.FalloffStopOpacity);
                OnChanged();
            });
            ControlFactory.CreateDetachedControl(ControlNames.FalloffColorEnabled, bgem.FalloffColorEnabled);
            RegisterDeferredControllerGroup(ControlNames.FalloffEnabled,
                () => IsControlEnabled(ControlNames.FalloffEnabled),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.FalloffStartAngle, bgem.FalloffStartAngle, FalloffVisibility);
                    ControlFactory.CreateDetachedControl(ControlNames.FalloffStopAngle, bgem.FalloffStopAngle, FalloffVisibility);
                    ControlFactory.CreateDetachedControl(ControlNames.FalloffStartOpacity, bgem.FalloffStartOpacity, FalloffVisibility);
                    ControlFactory.CreateDetachedControl(ControlNames.FalloffStopOpacity, bgem.FalloffStopOpacity, FalloffVisibility);
                },
                ControlNames.FalloffStartAngle,
                ControlNames.FalloffStopAngle,
                ControlNames.FalloffStartOpacity,
                ControlNames.FalloffStopOpacity);
        }

        private void CreateEffectSoftnessControls()
        {
            BGEM bgem = (BGEM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.SoftEnabled, bgem.SoftEnabled, null, _ =>
            {
                ControlFactory.UpdateVisibility(ControlNames.SoftDepth);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.SoftEnabled,
                () => IsControlEnabled(ControlNames.SoftEnabled),
                () => ControlFactory.CreateDetachedControl(ControlNames.SoftDepth, bgem.SoftDepth, SoftDepthVisibility),
                ControlNames.SoftDepth);
        }

        private void CreateEffectEnvironmentMappingControls()
        {
            BGEM bgem = (BGEM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.EnvMapping, bgem.EnvironmentMapping, _ => currentMaterial.Version >= 10);
            ControlFactory.CreateDetachedControl(ControlNames.EnvMappingMaskScale, bgem.EnvironmentMappingMaskScale, _ => currentMaterial.Version >= 10);
        }

        private void CreateEffectGlassControls()
        {
            BGEM bgem = (BGEM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.GlassEnabled, bgem.GlassEnabled, _ => currentMaterial.Version >= 21, _ =>
            {
                ControlFactory.UpdateVisibility(
                    ControlNames.GlassFresnelColor,
                    ControlNames.GlassBlurScaleBase,
                    ControlNames.GlassBlurScaleFactor,
                    ControlNames.GlassRefractionScaleBase);
                OnChanged();
            });
            RegisterDeferredControllerGroup(ControlNames.GlassEnabled,
                () => IsControlEnabled(ControlNames.GlassEnabled),
                () =>
                {
                    ControlFactory.CreateDetachedControl(ControlNames.GlassFresnelColor, UIntToColor(bgem.GlassFresnelColor), _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.GlassEnabled, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 21;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.GlassBlurScaleBase, bgem.GlassBlurScaleBase, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.GlassEnabled, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 21;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.GlassBlurScaleFactor, bgem.GlassBlurScaleFactor, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.GlassEnabled, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 22;
                    });
                    ControlFactory.CreateDetachedControl(ControlNames.GlassRefractionScaleBase, bgem.GlassRefractionScaleBase, _ =>
                    {
                        if (!ControlFactory.GetProperty(ControlNames.GlassEnabled, out var property))
                            return false;

                        return Convert.ToBoolean(property) && currentMaterial.Version >= 21;
                    });
                },
                ControlNames.GlassFresnelColor,
                ControlNames.GlassBlurScaleBase,
                ControlNames.GlassBlurScaleFactor,
                ControlNames.GlassRefractionScaleBase);
        }

        private void CreateEffectFeatureControls()
        {
            BGEM bgem = (BGEM)currentMaterial;
            ControlFactory.CreateDetachedControl(ControlNames.BloodEnabled, bgem.BloodEnabled);
            ControlFactory.CreateDetachedControl(ControlNames.EffectLightingEnabled, bgem.EffectLightingEnabled);
            ControlFactory.CreateDetachedControl(ControlNames.GrayscaleToPaletteAlpha, bgem.GrayscaleToPaletteAlpha);
            ControlFactory.CreateDetachedControl(ControlNames.EffectGlowmap, bgem.Glowmap, _ => currentMaterial.Version >= 16);
            ControlFactory.CreateDetachedControl(ControlNames.EffectPBRSpecular, bgem.EffectPbrSpecular, _ => currentMaterial.Version >= 20);
            ControlFactory.CreateDetachedControl(ControlNames.AdaptativeEmissiveExposureOffset, bgem.AdaptativeEmissive_ExposureOffset, _ => currentMaterial.Version >= 15);
            ControlFactory.CreateDetachedControl(ControlNames.AdaptativeEmissiveFinalExposureMin, bgem.AdaptativeEmissive_FinalExposureMin, _ => currentMaterial.Version >= 15);
            ControlFactory.CreateDetachedControl(ControlNames.AdaptativeEmissiveFinalExposureMax, bgem.AdaptativeEmissive_FinalExposureMax, _ => currentMaterial.Version >= 15);
        }
    }
}
