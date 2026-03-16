using MaterialLib;
using System;
using System.Drawing;
using Material_Editor.Controls;
using Material_Editor.Models;
using Material_Editor.Services;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        #region Material
        private void CreateMaterialControls(BaseMaterialFile file = null)
        {
            if (file == null)
            {
                file = new BGSM();

                switch (config.GameVersion)
                {
                    case Game.FO4:
                        file.Version = DefaultVersionFO4;
                        break;

                    case Game.FO76:
                        file.Version = DefaultVersionFO76;
                        break;
                }
            }

            currentMaterial = file;
            sectionVisibilityMap.Clear();

            Font fileFont = AppearanceService.CurrentAppearance.MonospaceFont;

            ControlFactory.ClearControls();
            PrepareFlatEditorLayout(layoutGeneral);
            PrepareFlatEditorLayout(layoutMaterial);
            PrepareFlatEditorLayout(layoutEffect);
            ControlFactory.DefaultChangedCallback = (control) => OnChanged();

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

            ControlFactory.CreateControl(layoutGeneral, ControlNames.Refraction, file.Refraction, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.RefractionFalloff);
                ControlFactory.UpdateVisibility(ControlNames.RefractionPower);
                OnChanged();
            });
            ControlFactory.CreateControl(layoutGeneral, ControlNames.RefractionFalloff, file.RefractionFalloff, RefractionVisibility);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.RefractionPower, file.RefractionPower, RefractionVisibility);

            ControlFactory.CreateControl(layoutGeneral, ControlNames.EnvironmentMapping, file.EnvironmentMapping, (control) => { return file.Version < 10; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.EnvironmentMaskScale);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutGeneral, ControlNames.EnvironmentMaskScale, file.EnvironmentMappingMaskScale, (control) =>
            {
                if (!ControlFactory.GetProperty(ControlNames.EnvironmentMapping, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 10;
            });
            ControlFactory.CreateControl(layoutGeneral, ControlNames.DepthBias, file.DepthBias, (control) => { return file.Version >= 10; });
            ControlFactory.CreateControl(layoutGeneral, ControlNames.GrayscaleToPaletteColor, file.GrayscaleToPaletteColor);
            ControlFactory.CreateFlagControl(layoutGeneral, ControlNames.MaskWrites, Enum.GetNames(typeof(BaseMaterialFile.MaskWriteFlags)), (int)file.MaskWrites, (control) => { return file.Version >= 6; });

            if (file is not BGSM bgsm)
            {
                bgsm = new BGSM();

                switch (config.GameVersion)
                {
                    case Game.FO4:
                        bgsm.Version = DefaultVersionFO4;
                        break;

                    case Game.FO76:
                        bgsm.Version = DefaultVersionFO76;
                        break;
                }
            }

            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Diffuse, fileFont, FileControl.FileType.Texture, bgsm.DiffuseTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Normal, fileFont, FileControl.FileType.Texture, bgsm.NormalTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.SmoothSpec, fileFont, FileControl.FileType.Texture, bgsm.SmoothSpecTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Greyscale, fileFont, FileControl.FileType.Texture, bgsm.GreyscaleTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Environment, fileFont, FileControl.FileType.Texture, bgsm.EnvmapTexture, (control) => { return file.Version <= 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Glow, fileFont, FileControl.FileType.Texture, bgsm.GlowTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.InnerLayer, fileFont, FileControl.FileType.Texture, bgsm.InnerLayerTexture, (control) => { return file.Version <= 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Wrinkles, fileFont, FileControl.FileType.Texture, bgsm.WrinklesTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Displacement, fileFont, FileControl.FileType.Texture, bgsm.DisplacementTexture, (control) => { return file.Version <= 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Specular, fileFont, FileControl.FileType.Texture, bgsm.SpecularTexture, (control) => { return file.Version > 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Lighting, fileFont, FileControl.FileType.Texture, bgsm.LightingTexture, (control) => { return file.Version > 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Flow, fileFont, FileControl.FileType.Texture, bgsm.FlowTexture, (control) => { return file.Version > 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.DistanceFieldAlpha, fileFont, FileControl.FileType.Texture, bgsm.DistanceFieldAlphaTexture, (control) => { return file.Version > 2; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.EnableEditorAlphaRef, bgsm.EnableEditorAlphaRef);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.RimLighting, bgsm.RimLighting, (control) => { return file.Version < 8; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.RimPower);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.RimPower, bgsm.RimPower, (control) =>
            {
                if (!ControlFactory.GetProperty(ControlNames.RimLighting, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 8;
            });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.BacklightPower, bgsm.BackLightPower, (control) => { return file.Version < 8; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.SubsurfaceLighting, bgsm.SubsurfaceLighting, (control) => { return file.Version < 8; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.SubsurfaceLightingRolloff);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.SubsurfaceLightingRolloff, bgsm.SubsurfaceLightingRolloff, (control) =>
            {
                if (!ControlFactory.GetProperty(ControlNames.SubsurfaceLighting, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 8;
            });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Translucency, bgsm.Translucency, (control) => { return file.Version >= 8; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencyThickObject, bgsm.TranslucencyThickObject, (control) => { return file.Version >= 8; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencyAlbSubsurfColor, bgsm.TranslucencyMixAlbedoWithSubsurfaceColor, (control) => { return file.Version >= 8; });

            var translucencySubsurfaceColor = UIntToColor(bgsm.TranslucencySubsurfaceColor);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencySubsurfaceColor, translucencySubsurfaceColor, (control) => { return file.Version >= 8; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencyTransmissiveScale, bgsm.TranslucencyTransmissiveScale, (control) => { return file.Version >= 8; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencyTurbulence, bgsm.TranslucencyTurbulence, (control) => { return file.Version >= 8; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.SpecularEnabled, bgsm.SpecularEnabled, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.SpecularColor);
                ControlFactory.UpdateVisibility(ControlNames.SpecularMultiplier);
                OnChanged();
            });

            var specularColor = UIntToColor(bgsm.SpecularColor);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.SpecularColor, specularColor, SpecularColorAndMultiplierVisibility);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.SpecularMultiplier, bgsm.SpecularMult, SpecularColorAndMultiplierVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.Smoothness, bgsm.Smoothness);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.FresnelPower, bgsm.FresnelPower);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetSpecScale, bgsm.WetnessControlSpecScale);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetSpecPowerScale, bgsm.WetnessControlSpecPowerScale);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetSpecMinVar, bgsm.WetnessControlSpecMinvar);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetEnvMapScale, bgsm.WetnessControlEnvMapScale, (control) => { return file.Version < 10; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetFresnelPower, bgsm.WetnessControlFresnelPower);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetMetalness, bgsm.WetnessControlMetalness);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.PBR, bgsm.PBR, (control) => { return file.Version > 2; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.CustomPorosity, bgsm.CustomPorosity, (control) => { return file.Version >= 9; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.PorosityValue, bgsm.PorosityValue, (control) => { return file.Version >= 9; });

            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.RootMaterialPath, fileFont, FileControl.FileType.Material, bgsm.RootMaterialPath);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.AnisoLighting, bgsm.AnisoLighting);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.EmittanceEnabled, bgsm.EmitEnabled, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.EmittanceColor);
                ControlFactory.UpdateVisibility(ControlNames.EmittanceMultiplier);
                OnChanged();
            });

            var emittanceColor = UIntToColor(bgsm.EmittanceColor);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.EmittanceColor, emittanceColor, EmittanceColorAndMultiplierVisibility);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.EmittanceMultiplier, bgsm.EmittanceMult, EmittanceColorAndMultiplierVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.ModelSpaceNormals, bgsm.ModelSpaceNormals);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.ExternalEmittance, bgsm.ExternalEmittance);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.LumEmittance, bgsm.LumEmittance, (control) => { return file.Version >= 12; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.AdaptativeEmissive, bgsm.UseAdaptativeEmissive, (control) => { return file.Version >= 13; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.AdaptEmissiveExposureOffset);
                ControlFactory.UpdateVisibility(ControlNames.AdaptEmissiveFinalExposureMin);
                ControlFactory.UpdateVisibility(ControlNames.AdaptEmissiveFinalExposureMax);
                OnChanged();
            });

            bool AdaptativeEmissiveVisibility(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.AdaptativeEmissive, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version >= 13;
            }

            ControlFactory.CreateControl(layoutMaterial, ControlNames.AdaptEmissiveExposureOffset, bgsm.AdaptativeEmissive_ExposureOffset, AdaptativeEmissiveVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.AdaptEmissiveFinalExposureMin, bgsm.AdaptativeEmissive_FinalExposureMin, AdaptativeEmissiveVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.AdaptEmissiveFinalExposureMax, bgsm.AdaptativeEmissive_FinalExposureMax, AdaptativeEmissiveVisibility);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.BackLighting, bgsm.BackLighting, (control) => { return file.Version < 8; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.ReceiveShadows, bgsm.ReceiveShadows);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.HideSecret, bgsm.HideSecret);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.CastShadows, bgsm.CastShadows);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.DissolveFade, bgsm.DissolveFade);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.AssumeShadowmask, bgsm.AssumeShadowmask);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.Glowmap, bgsm.Glowmap);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.EnvironmentMapWindow, bgsm.EnvironmentMappingWindow, (control) => { return file.Version < 7; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.EnvironmentMapEye, bgsm.EnvironmentMappingEye, (control) => { return file.Version < 7; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Hair, bgsm.Hair, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.HairTintColor);
                OnChanged();
            });

            var hairTintColor = UIntToColor(bgsm.HairTintColor);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.HairTintColor, hairTintColor, HairTintColorVisibility);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Tree, bgsm.Tree);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.Facegen, bgsm.Facegen);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.SkinTint, bgsm.SkinTint);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Tessellate, bgsm.Tessellate, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.DisplacementTexBias);
                ControlFactory.UpdateVisibility(ControlNames.DisplacementTexScale);
                ControlFactory.UpdateVisibility(ControlNames.TessellationPNScale);
                ControlFactory.UpdateVisibility(ControlNames.TessellationBaseFactor);
                ControlFactory.UpdateVisibility(ControlNames.TessellationFadeDistance);
                OnChanged();
            });

            bool TessellateVisibility(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.Tessellate, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 3;
            }

            ControlFactory.CreateControl(layoutMaterial, ControlNames.DisplacementTexBias, bgsm.DisplacementTextureBias, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.DisplacementTexScale, bgsm.DisplacementTextureScale, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TessellationPNScale, bgsm.TessellationPnScale, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TessellationBaseFactor, bgsm.TessellationBaseFactor, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TessellationFadeDistance, bgsm.TessellationFadeDistance, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.GrayscaleToPaletteScale, bgsm.GrayscaleToPaletteScale);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.SkewSpecularAlpha, bgsm.SkewSpecularAlpha, (control) => { return file.Version >= 1; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Terrain, bgsm.Terrain, (control) => { return file.Version >= 3; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.UnkInt1BGSM);
                ControlFactory.UpdateVisibility(ControlNames.TerrainThresholdFalloff);
                ControlFactory.UpdateVisibility(ControlNames.TerrainTilingDistance);
                ControlFactory.UpdateVisibility(ControlNames.TerrainRotationAngle);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.UnkInt1BGSM, bgsm.UnkInt1, (control) =>
            {
                if (!ControlFactory.GetProperty(ControlNames.Terrain, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version == 3;
            });

            bool TerrainVisibility(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.Terrain, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version >= 3;
            }

            ControlFactory.CreateControl(layoutMaterial, ControlNames.TerrainThresholdFalloff, bgsm.TerrainThresholdFalloff, TerrainVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TerrainTilingDistance, bgsm.TerrainTilingDistance, TerrainVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TerrainRotationAngle, bgsm.TerrainRotationAngle, TerrainVisibility);

            if (file is not BGEM bgem)
            {
                bgem = new BGEM();

                switch (config.GameVersion)
                {
                    case Game.FO4:
                        bgem.Version = DefaultVersionFO4;
                        break;

                    case Game.FO76:
                        bgem.Version = DefaultVersionFO76;
                        break;
                }
            }

            ControlFactory.CreateFileControl(layoutEffect, ControlNames.BaseTexture, fileFont, FileControl.FileType.Texture, bgem.BaseTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GrayscaleTexture, fileFont, FileControl.FileType.Texture, bgem.GrayscaleTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.EnvmapTexture, fileFont, FileControl.FileType.Texture, bgem.EnvmapTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.NormalTexture, fileFont, FileControl.FileType.Texture, bgem.NormalTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.EnvmapMaskTexture, fileFont, FileControl.FileType.Texture, bgem.EnvmapMaskTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.SpecularTexture, fileFont, FileControl.FileType.Texture, bgem.SpecularTexture, (control) => { return file.Version >= 11; });
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.LightingTexture, fileFont, FileControl.FileType.Texture, bgem.LightingTexture, (control) => { return file.Version >= 11; });
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlowTexture, fileFont, FileControl.FileType.Texture, bgem.GlowTexture, (control) => { return file.Version >= 11; });

            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlassRoughnessScratch, fileFont, FileControl.FileType.Texture, bgem.GlassRoughnessScratch, (control) => { return file.Version >= 21; });
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlassDirtOverlay, fileFont, FileControl.FileType.Texture, bgem.GlassDirtOverlay, (control) => { return file.Version >= 21; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassEnabled, bgem.GlassEnabled, (control) => { return file.Version >= 21; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.GlassFresnelColor);
                ControlFactory.UpdateVisibility(ControlNames.GlassBlurScaleBase);
                ControlFactory.UpdateVisibility(ControlNames.GlassBlurScaleFactor);
                ControlFactory.UpdateVisibility(ControlNames.GlassRefractionScaleBase);
                OnChanged();
            });

            bool GlassVisibilityV21(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.GlassEnabled, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version >= 21;
            }

            bool GlassVisibilityV22(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.GlassEnabled, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version >= 22;
            }

            var glassFresnelColor = UIntToColor(bgem.GlassFresnelColor);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassFresnelColor, glassFresnelColor, GlassVisibilityV21);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassBlurScaleBase, bgem.GlassBlurScaleBase, GlassVisibilityV21);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassBlurScaleFactor, bgem.GlassBlurScaleFactor, GlassVisibilityV22);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassRefractionScaleBase, bgem.GlassRefractionScaleBase, GlassVisibilityV21);

            ControlFactory.CreateControl(layoutEffect, ControlNames.EnvMapping, bgem.EnvironmentMapping, (control) => { return file.Version >= 10; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.EnvMappingMaskScale, bgem.EnvironmentMappingMaskScale, (control) => { return file.Version >= 10; });

            ControlFactory.CreateControl(layoutEffect, ControlNames.BloodEnabled, bgem.BloodEnabled);
            ControlFactory.CreateControl(layoutEffect, ControlNames.EffectLightingEnabled, bgem.EffectLightingEnabled);

            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffEnabled, bgem.FalloffEnabled, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.FalloffStartAngle);
                ControlFactory.UpdateVisibility(ControlNames.FalloffStopAngle);
                ControlFactory.UpdateVisibility(ControlNames.FalloffStartOpacity);
                ControlFactory.UpdateVisibility(ControlNames.FalloffStopOpacity);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffColorEnabled, bgem.FalloffColorEnabled);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GrayscaleToPaletteAlpha, bgem.GrayscaleToPaletteAlpha);

            ControlFactory.CreateControl(layoutEffect, ControlNames.SoftEnabled, bgem.SoftEnabled, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.SoftDepth);
                OnChanged();
            });

            var baseColor = UIntToColor(bgem.BaseColor);
            ControlFactory.CreateControl(layoutEffect, ControlNames.BaseColor, baseColor);

            ControlFactory.CreateControl(layoutEffect, ControlNames.BaseColorScale, bgem.BaseColorScale);
            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffStartAngle, bgem.FalloffStartAngle, FalloffVisibility);
            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffStopAngle, bgem.FalloffStopAngle, FalloffVisibility);
            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffStartOpacity, bgem.FalloffStartOpacity, FalloffVisibility);
            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffStopOpacity, bgem.FalloffStopOpacity, FalloffVisibility);
            ControlFactory.CreateControl(layoutEffect, ControlNames.LightingInfluence, bgem.LightingInfluence);
            ControlFactory.CreateControl(layoutEffect, ControlNames.EnvmapMinLOD, bgem.EnvmapMinLOD);
            ControlFactory.CreateControl(layoutEffect, ControlNames.SoftDepth, bgem.SoftDepth, SoftDepthVisibility);

            var emitColor = UIntToColor(bgem.EmittanceColor);
            ControlFactory.CreateControl(layoutEffect, ControlNames.EmitColor, emitColor, (control) => { return file.Version >= 11; });

            ControlFactory.CreateControl(layoutEffect, ControlNames.AdaptativeEmissiveExposureOffset, bgem.AdaptativeEmissive_ExposureOffset, (control) => { return file.Version >= 15; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.AdaptativeEmissiveFinalExposureMin, bgem.AdaptativeEmissive_FinalExposureMin, (control) => { return file.Version >= 15; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.AdaptativeEmissiveFinalExposureMax, bgem.AdaptativeEmissive_FinalExposureMax, (control) => { return file.Version >= 15; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.EffectGlowmap, bgem.Glowmap, (control) => { return file.Version >= 16; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.EffectPBRSpecular, bgem.EffectPbrSpecular, (control) => { return file.Version >= 20; });

            RebuildGeneralLayout();
            RebuildMaterialLayout();
            RebuildEffectLayout();
            CreateTooltips();
            ControlFactory.UpdateVisibility();
            ApplyCurrentAppearance();
            originalMaterial = CloneMaterial(currentMaterial);
        }

        #endregion
    }
}
