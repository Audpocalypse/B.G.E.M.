using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Material_Editor.Controls;
using Material_Editor.Models;
using Material_Editor.Theming;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private static Color UIntToColor(uint value)
        {
            return Color.FromArgb(255,
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF));
        }

        private bool RefractionVisibility(CustomControl _)
        {
            if (ControlFactory.GetProperty(ControlNames.Refraction, out var property))
                return Convert.ToBoolean(property);
            else
                return true;
        }

        private bool SpecularColorAndMultiplierVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.SpecularEnabled, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private bool EmittanceColorAndMultiplierVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.EmittanceEnabled, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private bool HairTintColorVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.Hair, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private bool FalloffVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.FalloffEnabled, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private bool SoftDepthVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.SoftEnabled, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private static bool IsControlEnabled(string controlName)
        {
            return ControlFactory.GetProperty(controlName, out var property) && Convert.ToBoolean(property);
        }

        private void PrepareFlatEditorLayout(TableLayoutPanel layout)
        {
            layout.SuspendLayout();
            layout.Controls.Clear();
            layout.RowStyles.Clear();
            layout.ColumnStyles.Clear();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Dock = DockStyle.Top;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(8);
            layout.RowCount = 0;
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle());
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle());
            layout.ResumeLayout();
        }

        private void PrepareHostLayout(TableLayoutPanel layout)
        {
            layout.SuspendLayout();
            layout.Controls.Clear();
            layout.RowStyles.Clear();
            layout.ColumnStyles.Clear();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Dock = DockStyle.Top;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(8);
            layout.RowCount = 0;
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ResumeLayout();
        }

        private TableLayoutPanel CreateSectionColumn()
        {
            var column = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 0
            };
            column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return column;
        }

        private TableLayoutPanel CreateTwoColumnHost()
        {
            var host = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return host;
        }

        private static void AddControlRow(TableLayoutPanel parent, Control control)
        {
            int row = parent.RowCount++;
            parent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            parent.Controls.Add(control, 0, row);
        }

        private void UpdateSectionVisibility()
        {
            foreach (var entry in sectionVisibilityMap)
            {
                bool hasVisibleContent = entry.Value.Any(name =>
                {
                    if (string.IsNullOrEmpty(name))
                        return false;

                    var control = ControlFactory.Find(name);
                    return control?.ShouldBeVisible() == true;
                });

                entry.Key.Visible = hasVisibleContent;
            }
        }

        private void InitializePageSections()
        {
            contentHostLayout.SuspendLayout();
            contentHostLayout.Controls.Clear();
            contentHostLayout.RowStyles.Clear();
            contentHostLayout.RowCount = 0;

            generalPageSection = CreateTopLevelPageSection("General", layoutGeneral);
            materialPageSection = CreateTopLevelPageSection("Material", layoutMaterial);
            effectPageSection = CreateTopLevelPageSection("Effect", layoutEffect);

            AddControlRow(contentHostLayout, generalPageSection);
            AddControlRow(contentHostLayout, materialPageSection);
            AddControlRow(contentHostLayout, effectPageSection);

            contentHostLayout.ResumeLayout();
            UpdateTopLevelSectionVisibility();
        }

        private static CollapsibleGroupBox CreateTopLevelPageSection(string title, TableLayoutPanel layout)
        {
            var section = new CollapsibleGroupBox(title, collapsible: true, collapsedByDefault: false)
            {
                Margin = new Padding(0, 0, 0, 8)
            };
            section.ContentLayout.Controls.Add(layout, 0, 0);
            return section;
        }

        private void UpdateTopLevelSectionVisibility()
        {
            if (generalPageSection != null)
                generalPageSection.Visible = true;

            if (materialPageSection != null)
                materialPageSection.Visible = CurrentMaterialType == MaterialType.Material;

            if (effectPageSection != null)
                effectPageSection.Visible = CurrentMaterialType == MaterialType.Effect;
        }

        private void RelayoutSingleEditorForAppearance()
        {
            if (!IsSingleMode)
                return;

            bool originalAutoScroll = contentScrollPanel.AutoScroll;
            contentScrollPanel.SuspendLayout();
            contentHostLayout.SuspendLayout();
            layoutGeneral.SuspendLayout();
            layoutMaterial.SuspendLayout();
            layoutEffect.SuspendLayout();

            try
            {
                contentScrollPanel.AutoScroll = false;
                layoutGeneral.PerformLayout();
                layoutMaterial.PerformLayout();
                layoutEffect.PerformLayout();
                contentHostLayout.PerformLayout();
                UpdateSectionVisibility();
                UpdateTopLevelSectionVisibility();
            }
            finally
            {
                layoutEffect.ResumeLayout(true);
                layoutMaterial.ResumeLayout(true);
                layoutGeneral.ResumeLayout(true);
                contentHostLayout.ResumeLayout(true);
                contentScrollPanel.AutoScroll = originalAutoScroll;
                contentScrollPanel.ResumeLayout(true);
            }
        }

        private void RebuildGeneralLayout()
        {
            var leftSections = new[]
            {
                new SectionDefinition("Texture Coordinates", 2, false, false,
                    ControlNames.TileU,
                    ControlNames.TileV,
                    ControlNames.OffsetU,
                    ControlNames.OffsetV,
                    ControlNames.ScaleU,
                    ControlNames.ScaleV),
                new SectionDefinition("Alpha / Refraction", 2, false, false,
                    ControlNames.Alpha,
                    ControlNames.AlphaBlendMode,
                    ControlNames.AlphaTestReference,
                    ControlNames.AlphaTest,
                    ControlNames.Refraction,
                    ControlNames.RefractionFalloff,
                    ControlNames.RefractionPower),
            };

            var rightSections = new[]
            {
                new SectionDefinition("Rendering / Environment", 2, false, false,
                    ControlNames.ZBufferWrite,
                    ControlNames.ZBufferTest,
                    ControlNames.ScreenSpaceReflections,
                    ControlNames.WetnessControlSSR,
                    ControlNames.EnvironmentMapping,
                    ControlNames.EnvironmentMaskScale,
                    ControlNames.DepthBias,
                    ControlNames.Decal,
                    ControlNames.TwoSided,
                    ControlNames.DecalNoFade,
                    ControlNames.NonOccluder,
                    ControlNames.GrayscaleToPaletteColor),
                new SectionDefinition("Advanced Masking", 1, false, false,
                    ControlNames.MaskWrites),
            };

            PrepareHostLayout(layoutGeneral);
            BuildSectionColumns(layoutGeneral, leftSections, rightSections);
        }

        private void RebuildMaterialLayout()
        {
            PrepareHostLayout(layoutMaterial);

            var pathSection = CreatePathsSection("Paths / Textures", new[]
            {
                ControlNames.Diffuse,
                ControlNames.Normal,
                ControlNames.SmoothSpec,
                ControlNames.Greyscale,
                ControlNames.Environment,
                ControlNames.Glow,
                ControlNames.Wrinkles,
                ControlNames.InnerLayer,
                ControlNames.Displacement,
                ControlNames.RootMaterialPath,
                ControlNames.Specular,
                ControlNames.Lighting,
                ControlNames.Flow,
                ControlNames.DistanceFieldAlpha,
            }, null);
            AddControlRow(layoutMaterial, pathSection);

            var leftSections = new[]
            {
                new SectionDefinition("Specular / Surface", 2, true, true,
                    ControlNames.SpecularEnabled,
                    ControlNames.SpecularColor,
                    ControlNames.SpecularMultiplier,
                    ControlNames.Smoothness,
                    ControlNames.FresnelPower,
                    ControlNames.AnisoLighting,
                    ControlNames.GrayscaleToPaletteScale,
                    ControlNames.SkewSpecularAlpha),
                new SectionDefinition("Lighting", 2, true, true,
                    ControlNames.RimLighting,
                    ControlNames.RimPower,
                    ControlNames.BackLighting,
                    ControlNames.BacklightPower,
                    ControlNames.SubsurfaceLighting,
                    ControlNames.SubsurfaceLightingRolloff,
                    ControlNames.Translucency,
                    ControlNames.TranslucencyThickObject,
                    ControlNames.TranslucencyAlbSubsurfColor,
                    ControlNames.TranslucencySubsurfaceColor,
                    ControlNames.TranslucencyTransmissiveScale,
                    ControlNames.TranslucencyTurbulence),
                new SectionDefinition("Emittance", 2, true, true,
                    ControlNames.EmittanceEnabled,
                    ControlNames.ExternalEmittance,
                    ControlNames.EmittanceColor,
                    ControlNames.EmittanceMultiplier,
                    ControlNames.LumEmittance),
                new SectionDefinition("Adaptive Emissive", 2, true, true,
                    ControlNames.AdaptativeEmissive,
                    ControlNames.AdaptEmissiveExposureOffset,
                    ControlNames.AdaptEmissiveFinalExposureMin,
                    ControlNames.AdaptEmissiveFinalExposureMax),
            };

            var rightSections = new[]
            {
                new SectionDefinition("Wetness", 2, true, true,
                    ControlNames.WetSpecScale,
                    ControlNames.WetSpecPowerScale,
                    ControlNames.WetSpecMinVar,
                    ControlNames.WetEnvMapScale,
                    ControlNames.WetFresnelPower,
                    ControlNames.WetMetalness),
                new SectionDefinition("Rendering Flags", 2, true, true,
                    ControlNames.EnableEditorAlphaRef,
                    ControlNames.ModelSpaceNormals,
                    ControlNames.ReceiveShadows,
                    ControlNames.CastShadows,
                    ControlNames.AssumeShadowmask,
                    ControlNames.HideSecret,
                    ControlNames.DissolveFade,
                    ControlNames.Glowmap),
                new SectionDefinition("Shader Features", 2, true, true,
                    ControlNames.Facegen,
                    ControlNames.SkinTint,
                    ControlNames.Tree,
                    ControlNames.EnvironmentMapWindow,
                    ControlNames.EnvironmentMapEye,
                    ControlNames.PBR,
                    ControlNames.CustomPorosity,
                    ControlNames.PorosityValue),
                new SectionDefinition("Hair", 2, true, true,
                    ControlNames.Hair,
                    ControlNames.HairTintColor),
                new SectionDefinition("Tessellation", 2, true, true,
                    ControlNames.Tessellate,
                    ControlNames.DisplacementTexBias,
                    ControlNames.DisplacementTexScale,
                    ControlNames.TessellationPNScale,
                    ControlNames.TessellationBaseFactor,
                    ControlNames.TessellationFadeDistance),
                new SectionDefinition("Terrain", 2, true, true,
                    ControlNames.Terrain,
                    ControlNames.UnkInt1BGSM,
                    ControlNames.TerrainThresholdFalloff,
                    ControlNames.TerrainTilingDistance,
                    ControlNames.TerrainRotationAngle),
            };

            BuildSectionColumns(layoutMaterial, leftSections, rightSections);
        }

        private void RebuildEffectLayout()
        {
            PrepareHostLayout(layoutEffect);

            var pathSection = CreatePathsSection("Paths / Textures", new[]
            {
                ControlNames.BaseTexture,
                ControlNames.NormalTexture,
                ControlNames.GrayscaleTexture,
                ControlNames.EnvmapTexture,
                ControlNames.EnvmapMaskTexture,
                ControlNames.GlowTexture,
                ControlNames.SpecularTexture,
                ControlNames.LightingTexture,
                ControlNames.GlassRoughnessScratch,
                ControlNames.GlassDirtOverlay,
            }, null);
            AddControlRow(layoutEffect, pathSection);

            var leftSections = new[]
            {
                new SectionDefinition("Surface / Color", 2, true, true,
                    ControlNames.BaseColor,
                    ControlNames.BaseColorScale,
                    ControlNames.EmitColor,
                    ControlNames.LightingInfluence,
                    ControlNames.EnvmapMinLOD),
                new SectionDefinition("Falloff", 2, true, true,
                    ControlNames.FalloffEnabled,
                    ControlNames.FalloffColorEnabled,
                    ControlNames.FalloffStartAngle,
                    ControlNames.FalloffStopAngle,
                    ControlNames.FalloffStartOpacity,
                    ControlNames.FalloffStopOpacity),
                new SectionDefinition("Softness", 2, true, true,
                    ControlNames.SoftEnabled,
                    ControlNames.SoftDepth),
            };

            var rightSections = new[]
            {
                new SectionDefinition("Environment Mapping", 2, true, true,
                    ControlNames.EnvMapping,
                    ControlNames.EnvMappingMaskScale),
                new SectionDefinition("Glass", 2, true, true,
                    ControlNames.GlassEnabled,
                    ControlNames.GlassFresnelColor,
                    ControlNames.GlassBlurScaleBase,
                    ControlNames.GlassBlurScaleFactor,
                    ControlNames.GlassRefractionScaleBase),
                new SectionDefinition("Effect Features", 2, true, true,
                    ControlNames.BloodEnabled,
                    ControlNames.EffectLightingEnabled,
                    ControlNames.GrayscaleToPaletteAlpha,
                    ControlNames.EffectGlowmap,
                    ControlNames.EffectPBRSpecular,
                    ControlNames.AdaptativeEmissiveExposureOffset,
                    ControlNames.AdaptativeEmissiveFinalExposureMin,
                    ControlNames.AdaptativeEmissiveFinalExposureMax),
            };

            BuildSectionColumns(layoutEffect, leftSections, rightSections);
        }

        private void BuildSectionColumns(TableLayoutPanel host, IReadOnlyList<SectionDefinition> leftSections, IReadOnlyList<SectionDefinition> rightSections)
        {
            var sectionHost = CreateTwoColumnHost();
            var leftColumn = CreateSectionColumn();
            var rightColumn = CreateSectionColumn();

            sectionHost.Controls.Add(leftColumn, 0, 0);
            sectionHost.Controls.Add(rightColumn, 1, 0);

            foreach (var section in leftSections)
                AddControlRow(leftColumn, CreateSection(section, true));

            foreach (var section in rightSections)
                AddControlRow(rightColumn, CreateSection(section, false));

            AddControlRow(host, sectionHost);
        }

        private Control CreateSection(SectionDefinition definition, bool leftColumn)
        {
            var group = new CollapsibleGroupBox(definition.Title, definition.Collapsible, definition.CollapsedByDefault)
            {
                Margin = leftColumn ? new Padding(0, 0, 5, 8) : new Padding(5, 0, 0, 8)
            };

            var grid = CreatePropertyGrid(definition.PairsPerRow);
            PopulatePropertyGrid(grid, definition.Controls, definition.PairsPerRow);
            group.ContentLayout.Controls.Add(grid, 0, 0);
            sectionVisibilityMap[group] = definition.Controls;
            return group;
        }

        private static void RegisterDeferredControllerGroup(string controllerName, Func<bool> shouldBuildImmediately, Action buildControls, params string[] controlNames)
        {
            buildControls?.Invoke();
        }

        private CollapsibleGroupBox CreatePathsSection(string title, IReadOnlyList<string> controlNames, string fullWidthControlName)
        {
            var group = new CollapsibleGroupBox(title)
            {
                Margin = new Padding(0, 0, 0, 8)
            };

            var grid = CreateFileGrid();
            PopulateFileGrid(grid, controlNames, fullWidthControlName);
            group.ContentLayout.Controls.Add(grid, 0, 0);
            return group;
        }

        private TableLayoutPanel CreatePropertyGrid(int pairsPerRow)
        {
            var grid = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = Math.Max(1, pairsPerRow * 2 + Math.Max(0, pairsPerRow - 1)),
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 0
            };

            for (int pairIndex = 0; pairIndex < pairsPerRow; pairIndex++)
            {
                grid.ColumnStyles.Add(new ColumnStyle());
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / pairsPerRow));

                if (pairIndex < pairsPerRow - 1)
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
            }

            return grid;
        }

        private TableLayoutPanel CreateFileGrid()
        {
            var grid = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 7,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 0
            };

            grid.ColumnStyles.Add(new ColumnStyle());
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle());
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
            grid.ColumnStyles.Add(new ColumnStyle());
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle());

            return grid;
        }

        private void PopulatePropertyGrid(TableLayoutPanel grid, IReadOnlyList<string> controlNames, int pairsPerRow)
        {
            int currentRow = -1;
            int pairIndex = 0;

            foreach (var controlName in controlNames)
            {
                var control = ControlFactory.Find(controlName);
                if (control == null)
                    continue;

                ApplyCompactFieldLayout(control);

                if (NeedsFullWidthRow(control))
                {
                    if (pairIndex != 0)
                        pairIndex = 0;

                    AddFullWidthFieldRow(grid, control);
                    currentRow = -1;
                    continue;
                }

                if (pairIndex == 0)
                {
                    currentRow = grid.RowCount++;
                    grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                }

                int column = pairIndex * 3;
                grid.Controls.Add(control.LabelControl, column, currentRow);
                grid.Controls.Add(control.Control, column + 1, currentRow);

                pairIndex++;
                if (pairIndex >= pairsPerRow)
                    pairIndex = 0;
            }
        }

        private void PopulateFileGrid(TableLayoutPanel grid, IReadOnlyList<string> controlNames, string fullWidthControlName)
        {
            for (int i = 0; i < controlNames.Count; i += 2)
            {
                int row = grid.RowCount++;
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                AddFileFieldPair(grid, row, controlNames[i], 0);

                if (i + 1 < controlNames.Count)
                    AddFileFieldPair(grid, row, controlNames[i + 1], 4);
            }

            if (!string.IsNullOrEmpty(fullWidthControlName))
                AddFullWidthFieldRow(grid, ControlFactory.Find(fullWidthControlName));
        }

        private void AddFileFieldPair(TableLayoutPanel grid, int row, string controlName, int columnOffset)
        {
            if (string.IsNullOrEmpty(controlName))
                return;

            var control = ControlFactory.Find(controlName);
            if (control == null)
                return;

            ApplyCompactFieldLayout(control, true);

            grid.Controls.Add(control.LabelControl, columnOffset, row);
            grid.Controls.Add(control.Control, columnOffset + 1, row);

            if (control.ExtraControl != null)
                grid.Controls.Add(control.ExtraControl, columnOffset + 2, row);
        }

        private void AddFullWidthFieldRow(TableLayoutPanel grid, CustomControl control)
        {
            if (control == null)
                return;

            ApplyCompactFieldLayout(control, true);

            var rowLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = control.ExtraControl != null ? 3 : 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            rowLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rowLayout.ColumnStyles.Add(new ColumnStyle());
            rowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            if (control.ExtraControl != null)
                rowLayout.ColumnStyles.Add(new ColumnStyle());

            rowLayout.Controls.Add(control.LabelControl, 0, 0);
            rowLayout.Controls.Add(control.Control, 1, 0);

            if (control.ExtraControl != null)
                rowLayout.Controls.Add(control.ExtraControl, 2, 0);

            int row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(rowLayout, 0, row);
            grid.SetColumnSpan(rowLayout, grid.ColumnCount);
        }

        private static bool NeedsFullWidthRow(CustomControl control)
        {
            return control.Control is CheckedListBox || control.ExtraControl != null;
        }

        private static void ApplyCompactFieldLayout(CustomControl control, bool pathStyle = false)
        {
            if (control?.LabelControl == null || control.Control == null)
                return;

            control.LabelControl.Anchor = AnchorStyles.Left;
            control.LabelControl.AutoSize = true;
            control.LabelControl.Margin = new Padding(0, 4, 8, 4);

            switch (control.Control)
            {
                case ColorToggleCheckBox colorToggleCheckBox:
                    colorToggleCheckBox.AutoSize = true;
                    colorToggleCheckBox.Anchor = AnchorStyles.Left;
                    colorToggleCheckBox.Margin = new Padding(0, 2, 0, 2);
                    break;
                case CheckBox checkBox:
                    checkBox.AutoSize = true;
                    checkBox.Anchor = AnchorStyles.Left;
                    checkBox.Margin = new Padding(0, 2, 0, 2);
                    break;
                case RadioButton radioButton:
                    radioButton.AutoSize = true;
                    radioButton.Anchor = AnchorStyles.Left;
                    radioButton.Margin = new Padding(0, 2, 0, 2);
                    break;
                case CheckedListBox checkedListBox:
                    checkedListBox.Dock = DockStyle.Fill;
                    checkedListBox.Margin = new Padding(0, 1, 0, 1);
                    checkedListBox.Height = Math.Max(checkedListBox.Height, 60);
                    break;
                default:
                    control.Control.Dock = DockStyle.Fill;
                    control.Control.Margin = new Padding(0, 1, 0, 1);
                    break;
            }

            if (control.ExtraControl != null)
            {
                control.ExtraControl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                control.ExtraControl.Margin = pathStyle ? new Padding(6, 1, 0, 1) : new Padding(6, 1, 0, 1);
            }
        }
    }
}
