using Material_Editor.Controls;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private void CreateTooltips()
        {
            toolTip.RemoveAll();

            ControlFactory.SetTooltip(ControlNames.TileU, toolTip, "Tile the U texture coordinate (wrapping/repeating the texture).");
            ControlFactory.SetTooltip(ControlNames.TileV, toolTip, "Tile the V texture coordinate (wrapping/repeating the texture).");
            ControlFactory.SetTooltip(ControlNames.OffsetU, toolTip, "Offset the U texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.OffsetV, toolTip, "Offset the V texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.ScaleU, toolTip, "Scale the U texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.ScaleV, toolTip, "Scale the V texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.Alpha, toolTip, "Fixed alpha value that applies to the entire mesh (unrelated to texture alpha).");
            ControlFactory.SetTooltip(ControlNames.AlphaBlendMode, toolTip, "Defines the mode at which alpha is blended into other meshes.");
            ControlFactory.SetTooltip(ControlNames.AlphaTestReference, toolTip, "Reference value to do alpha testing for. Transparency happens when alpha is below or exceeds the reference value (depending on modes).");
            ControlFactory.SetTooltip(ControlNames.AlphaTest, toolTip, "Toggle alpha testing using the reference value.");
            ControlFactory.SetTooltip(ControlNames.ZBufferWrite, toolTip, "The mesh writes to the z-buffer to make others aware of its depth.");
            ControlFactory.SetTooltip(ControlNames.ZBufferTest, toolTip, "The mesh tests the z-buffer to take note of other meshes depth.");
            ControlFactory.SetTooltip(ControlNames.ScreenSpaceReflections, toolTip, "Toggle screen space reflections.");
            ControlFactory.SetTooltip(ControlNames.WetnessControlSSR, toolTip, "Toggle wetness control for screen space reflections.");
            ControlFactory.SetTooltip(ControlNames.Decal, toolTip, "Toggle decal rendering.");
            ControlFactory.SetTooltip(ControlNames.TwoSided, toolTip, "Renders both sides of all faces of the mesh (double sided).");
            ControlFactory.SetTooltip(ControlNames.DecalNoFade, toolTip, "Toggle decal rendering without fade.");
            ControlFactory.SetTooltip(ControlNames.NonOccluder, toolTip, "Don't perform occlusion (line-of-sight).");
            ControlFactory.SetTooltip(ControlNames.Refraction, toolTip, "Toggle refraction of light.");
            ControlFactory.SetTooltip(ControlNames.RefractionFalloff, toolTip, "Toggles refraction falloff.");
            ControlFactory.SetTooltip(ControlNames.RefractionPower, toolTip, "Power of the refraction.");
            ControlFactory.SetTooltip(ControlNames.EnvironmentMapping, toolTip, "Toggle environment mapping.");
            ControlFactory.SetTooltip(ControlNames.EnvironmentMaskScale, toolTip, "Scale for the environment mask.");
            ControlFactory.SetTooltip(ControlNames.DepthBias, toolTip, "Toggle depth bias to prevent z-fighting.");
            ControlFactory.SetTooltip(ControlNames.GrayscaleToPaletteColor, toolTip, "Toggle mapping of grayscale to palette colors.");
            ControlFactory.SetTooltip(ControlNames.MaskWrites, toolTip, "Masks writing of certain lighting properties.");

            ControlFactory.SetTooltip(ControlNames.Diffuse, toolTip, "Diffuse texture slot.");
            ControlFactory.SetTooltip(ControlNames.Normal, toolTip, "Normal map slot.");
            ControlFactory.SetTooltip(ControlNames.SmoothSpec, toolTip, "Smoothness/specular mask slot.");
            ControlFactory.SetTooltip(ControlNames.Greyscale, toolTip, "Greyscale (palette/lookup/heightmap) texture slot.");
            ControlFactory.SetTooltip(ControlNames.Environment, toolTip, "Environment map slot.");
            ControlFactory.SetTooltip(ControlNames.Glow, toolTip, "Glow map or other specialty slot.");
            ControlFactory.SetTooltip(ControlNames.InnerLayer, toolTip, "Inner layer mask slot.");
            ControlFactory.SetTooltip(ControlNames.Wrinkles, toolTip, "Wrinkles texture slot.");
            ControlFactory.SetTooltip(ControlNames.Displacement, toolTip, "Displacement texture slot.");
            ControlFactory.SetTooltip(ControlNames.Specular, toolTip, "PBR specular texture slot.");
            ControlFactory.SetTooltip(ControlNames.Lighting, toolTip, "PBR lighting texture slot.");
            ControlFactory.SetTooltip(ControlNames.Flow, toolTip, "PBR flow texture slot.");
            ControlFactory.SetTooltip(ControlNames.DistanceFieldAlpha, toolTip, "Distance field alpha texture slot.");
            ControlFactory.SetTooltip(ControlNames.EnableEditorAlphaRef, toolTip, "Toggle editor alpha testing reference.");
            ControlFactory.SetTooltip(ControlNames.RimLighting, toolTip, "Toggle rim lighting effect.");
            ControlFactory.SetTooltip(ControlNames.RimPower, toolTip, "Power of the rim lighting.");
            ControlFactory.SetTooltip(ControlNames.BacklightPower, toolTip, "Power of the back lighting.");
            ControlFactory.SetTooltip(ControlNames.SubsurfaceLighting, toolTip, "Toggle subsurface lighting effect.");
            ControlFactory.SetTooltip(ControlNames.SubsurfaceLightingRolloff, toolTip, "Rolloff of the subsurface lighting.");
            ControlFactory.SetTooltip(ControlNames.Translucency, toolTip, "Toggle translucency simulation.");
            ControlFactory.SetTooltip(ControlNames.TranslucencyThickObject, toolTip, "Object on which the material is applied is thick (or a billboard if not). Used for correct shadowing.");
            ControlFactory.SetTooltip(ControlNames.TranslucencyAlbSubsurfColor, toolTip, "Multiply the subsurface color with the albedo instead of just outputting the specified color.");
            ControlFactory.SetTooltip(ControlNames.TranslucencySubsurfaceColor, toolTip, "Color tint of the subsurface matter.");
            ControlFactory.SetTooltip(ControlNames.TranslucencyTransmissiveScale, toolTip, "Simulate amount of light collision inside the material.");
            ControlFactory.SetTooltip(ControlNames.TranslucencyTurbulence, toolTip, "Turbulence for translucency.");
            ControlFactory.SetTooltip(ControlNames.SpecularEnabled, toolTip, "Toggle specular effect.");
            ControlFactory.SetTooltip(ControlNames.SpecularColor, toolTip, "Color for the specular effect.");
            ControlFactory.SetTooltip(ControlNames.SpecularMultiplier, toolTip, "Multiplier for the specular effect.");
            ControlFactory.SetTooltip(ControlNames.Smoothness, toolTip, "Smoothness of the specular effect.");
            ControlFactory.SetTooltip(ControlNames.FresnelPower, toolTip, "Power of the fresnel reflection and transmission (specular).");
            ControlFactory.SetTooltip(ControlNames.WetSpecScale, toolTip, "Scale of the wetness specular.");
            ControlFactory.SetTooltip(ControlNames.WetSpecPowerScale, toolTip, "Power scale of the wetness specular.");
            ControlFactory.SetTooltip(ControlNames.WetSpecMinVar, toolTip, "Minimum variance of the wetness specular.");
            ControlFactory.SetTooltip(ControlNames.WetEnvMapScale, toolTip, "Environment map scale of the wetness effect.");
            ControlFactory.SetTooltip(ControlNames.WetFresnelPower, toolTip, "Fresnel power of the wetness effect.");
            ControlFactory.SetTooltip(ControlNames.WetMetalness, toolTip, "Metalness of the wetness effect.");
            ControlFactory.SetTooltip(ControlNames.PBR, toolTip, "Enables native PBR rendering. Requires diffuse, normal, specular and lighting texture (flow optional).");
            ControlFactory.SetTooltip(ControlNames.CustomPorosity, toolTip, "Toggle custom porosity for PBR.");
            ControlFactory.SetTooltip(ControlNames.PorosityValue, toolTip, "Custom porosity value for PBR.");
            ControlFactory.SetTooltip(ControlNames.RootMaterialPath, toolTip, "Template/root file of the current material.");
            ControlFactory.SetTooltip(ControlNames.AnisoLighting, toolTip, "Toggle anisotropic lighting.");
            ControlFactory.SetTooltip(ControlNames.EmittanceEnabled, toolTip, "Toggle emittance effect.");
            ControlFactory.SetTooltip(ControlNames.EmittanceColor, toolTip, "Color for the emittance effect.");
            ControlFactory.SetTooltip(ControlNames.EmittanceMultiplier, toolTip, "Multiplier for the emittance effect.");
            ControlFactory.SetTooltip(ControlNames.ModelSpaceNormals, toolTip, "Toggle model space normals rendering.");
            ControlFactory.SetTooltip(ControlNames.ExternalEmittance, toolTip, "Toggle external emittance effect.");
            ControlFactory.SetTooltip(ControlNames.LumEmittance, toolTip, "Luminous emittance value (in Lux) of the luminous flux emitted from the surface.");
            ControlFactory.SetTooltip(ControlNames.AdaptativeEmissive, toolTip, "Use stable emissive over physically based emittance. If unchecked, uses luminous emittance.");
            ControlFactory.SetTooltip(ControlNames.AdaptEmissiveExposureOffset, toolTip, "Exposure offset applied while exposing the emissive object.");
            ControlFactory.SetTooltip(ControlNames.AdaptEmissiveFinalExposureMin, toolTip, "Minimum amount of exposure on the emissive object.");
            ControlFactory.SetTooltip(ControlNames.AdaptEmissiveFinalExposureMax, toolTip, "Maximum amount of exposure on the emissive object.");
            ControlFactory.SetTooltip(ControlNames.BackLighting, toolTip, "Toggle back lighting effect.");
            ControlFactory.SetTooltip(ControlNames.ReceiveShadows, toolTip, "Toggle if this mesh receives shadows.");
            ControlFactory.SetTooltip(ControlNames.HideSecret, toolTip, "Toggle hide secret.");
            ControlFactory.SetTooltip(ControlNames.CastShadows, toolTip, "Toggle shadow casting for this mesh.");
            ControlFactory.SetTooltip(ControlNames.DissolveFade, toolTip, "Toggle dissolve fade.");
            ControlFactory.SetTooltip(ControlNames.AssumeShadowmask, toolTip, "Toggle assuming shadowmask.");
            ControlFactory.SetTooltip(ControlNames.Glowmap, toolTip, "Toggle making use of a glowmap for emittance.");
            ControlFactory.SetTooltip(ControlNames.EnvironmentMapWindow, toolTip, "Toggle environment map window.");
            ControlFactory.SetTooltip(ControlNames.EnvironmentMapEye, toolTip, "Toggle environment map eye.");
            ControlFactory.SetTooltip(ControlNames.Hair, toolTip, "Toggle hair rendering.");
            ControlFactory.SetTooltip(ControlNames.HairTintColor, toolTip, "Color for the hair tinting.");
            ControlFactory.SetTooltip(ControlNames.Tree, toolTip, "Toggle tree rendering.");
            ControlFactory.SetTooltip(ControlNames.Facegen, toolTip, "Toggle facegen rendering.");
            ControlFactory.SetTooltip(ControlNames.SkinTint, toolTip, "Toggle skin tint rendering.");
            ControlFactory.SetTooltip(ControlNames.Tessellate, toolTip, "Toggle tessellation effect.");
            ControlFactory.SetTooltip(ControlNames.DisplacementTexBias, toolTip, "Bias for the displacement texture.");
            ControlFactory.SetTooltip(ControlNames.DisplacementTexScale, toolTip, "Scale for the displacement texture.");
            ControlFactory.SetTooltip(ControlNames.TessellationPNScale, toolTip, "PN (point normal) scale for the tessellation effect.");
            ControlFactory.SetTooltip(ControlNames.TessellationBaseFactor, toolTip, "Base factor for the tessellation effect.");
            ControlFactory.SetTooltip(ControlNames.TessellationFadeDistance, toolTip, "Fade distance for the tessellation effect.");
            ControlFactory.SetTooltip(ControlNames.GrayscaleToPaletteScale, toolTip, "Scale for the grayscale to palette mapping.");
            ControlFactory.SetTooltip(ControlNames.SkewSpecularAlpha, toolTip, "Toggle skew specular alpha.");
            ControlFactory.SetTooltip(ControlNames.Terrain, toolTip, "Toggle terrain rendering.");
            ControlFactory.SetTooltip(ControlNames.UnkInt1BGSM, toolTip, "Unknown value.");
            ControlFactory.SetTooltip(ControlNames.TerrainThresholdFalloff, toolTip, "Softness of the terrain blending.");
            ControlFactory.SetTooltip(ControlNames.TerrainTilingDistance, toolTip, "Tiling distance of the terrain.");
            ControlFactory.SetTooltip(ControlNames.TerrainRotationAngle, toolTip, "Rotation angle of the terrain.");

            ControlFactory.SetTooltip(ControlNames.BaseTexture, toolTip, "Base texture slot.");
            ControlFactory.SetTooltip(ControlNames.GrayscaleTexture, toolTip, "Grayscale texture slot.");
            ControlFactory.SetTooltip(ControlNames.EnvmapTexture, toolTip, "Environment map slot.");
            ControlFactory.SetTooltip(ControlNames.NormalTexture, toolTip, "Normal map slot.");
            ControlFactory.SetTooltip(ControlNames.EnvmapMaskTexture, toolTip, "Environment map mask slot.");
            ControlFactory.SetTooltip(ControlNames.SpecularTexture, toolTip, "PBR specular texture slot.");
            ControlFactory.SetTooltip(ControlNames.LightingTexture, toolTip, "PBR lighting texture slot.");
            ControlFactory.SetTooltip(ControlNames.GlowTexture, toolTip, "PBR emissive palette slot.");
            ControlFactory.SetTooltip(ControlNames.EnvMapping, toolTip, "Toggle environment mapping effect.");
            ControlFactory.SetTooltip(ControlNames.EnvMappingMaskScale, toolTip, "Scale for the environment mapping mask.");
            ControlFactory.SetTooltip(ControlNames.BloodEnabled, toolTip, "Toggle blood rendering.");
            ControlFactory.SetTooltip(ControlNames.EffectLightingEnabled, toolTip, "Toggle effect lighting.");
            ControlFactory.SetTooltip(ControlNames.FalloffEnabled, toolTip, "Toggle falloff settings driving alpha.");
            ControlFactory.SetTooltip(ControlNames.FalloffColorEnabled, toolTip, "Toggle falloff settings driving color.");
            ControlFactory.SetTooltip(ControlNames.GrayscaleToPaletteAlpha, toolTip, "Toggle grayscale to palette alpha mapping.");
            ControlFactory.SetTooltip(ControlNames.SoftEnabled, toolTip, "Toggle softness effect.");
            ControlFactory.SetTooltip(ControlNames.BaseColor, toolTip, "Base color of the effect.");
            ControlFactory.SetTooltip(ControlNames.BaseColorScale, toolTip, "Scale of the base color.");
            ControlFactory.SetTooltip(ControlNames.FalloffStartAngle, toolTip, "Start angle of the falloff.");
            ControlFactory.SetTooltip(ControlNames.FalloffStopAngle, toolTip, "Stop angle of the falloff.");
            ControlFactory.SetTooltip(ControlNames.FalloffStartOpacity, toolTip, "Start opacity of the falloff.");
            ControlFactory.SetTooltip(ControlNames.FalloffStopOpacity, toolTip, "Stop opacity of the falloff.");
            ControlFactory.SetTooltip(ControlNames.LightingInfluence, toolTip, "Lighting influence value of the material.");
            ControlFactory.SetTooltip(ControlNames.EnvmapMinLOD, toolTip, "Minimum LOD for environment mapping.");
            ControlFactory.SetTooltip(ControlNames.SoftDepth, toolTip, "Softness depth value of the material.");
            ControlFactory.SetTooltip(ControlNames.EmitColor, toolTip, "Color for the PBR emittance effect.");
            ControlFactory.SetTooltip(ControlNames.AdaptativeEmissiveExposureOffset, toolTip, "Exposure offset applied while exposing the emissive object.");
            ControlFactory.SetTooltip(ControlNames.AdaptativeEmissiveFinalExposureMin, toolTip, "Minimum amount of exposure on the emissive object.");
            ControlFactory.SetTooltip(ControlNames.AdaptativeEmissiveFinalExposureMax, toolTip, "Maximum amount of exposure on the emissive object.");
            ControlFactory.SetTooltip(ControlNames.EffectGlowmap, toolTip, "Toggle glowmap.");
            ControlFactory.SetTooltip(ControlNames.EffectPBRSpecular, toolTip, "Toggle PBR specular effect.");

            ControlFactory.SetTooltip(ControlNames.GlassRoughnessScratch, toolTip, "Path to the roughness(R) and scratch(G) texture.");
            ControlFactory.SetTooltip(ControlNames.GlassDirtOverlay, toolTip, "Path to the dirt overlay texture.");

            ControlFactory.SetTooltip(ControlNames.GlassEnabled, toolTip, "Glass rendering enabled");
            ControlFactory.SetTooltip(ControlNames.GlassFresnelColor, toolTip, "Glass fresnel color.");
            ControlFactory.SetTooltip(ControlNames.GlassBlurScaleBase, toolTip, "Possibly glass blur scale base. Might be a different property.");
            ControlFactory.SetTooltip(ControlNames.GlassBlurScaleFactor, toolTip, "Possibly glass blur scale factor. Might be a different property.");
            ControlFactory.SetTooltip(ControlNames.GlassRefractionScaleBase, toolTip, "Possibly glass refraction scale base. Might be a different property.");
        }
    }
}
