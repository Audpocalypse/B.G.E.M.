using MaterialLib;
using System.Drawing;
using Material_Editor.Controls;
using Material_Editor.Models;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        #region Material
        private void CreateMaterialControls(BaseMaterialFile file = null)
        {
            if (file == null)
                file = CreateMaterialForType(MaterialType.Material);

            if (!forceSingleEditorControlRebuild && TryReuseMaterialControls(file))
                return;

            currentMaterial = file;
            sectionVisibilityMap.Clear();

            Font fileFont = AppearanceService.CurrentAppearance.MonospaceFont;
            bool buildMaterialControls = file is BGSM;
            bool buildEffectControls = file is BGEM;

            contentScrollPanel.SuspendLayout();
            contentHostLayout.SuspendLayout();
            layoutGeneral.SuspendLayout();
            layoutMaterial.SuspendLayout();
            layoutEffect.SuspendLayout();

            try
            {
                ControlFactory.ClearControls();
                PrepareFlatEditorLayout(layoutGeneral);
                PrepareFlatEditorLayout(layoutMaterial);
                PrepareFlatEditorLayout(layoutEffect);
                ControlFactory.DefaultChangedCallback = (control) => OnChanged();

                CreateGeneralControls(file);

                if (buildMaterialControls)
                {
                    CreateMaterialPathControls((BGSM)file, fileFont);
                    BuildMaterialSectionControls();
                }

                if (buildEffectControls)
                {
                    CreateEffectPathControls((BGEM)file, fileFont);
                    BuildEffectSectionControls();
                }

                RebuildGeneralLayout();
                if (buildMaterialControls)
                    RebuildMaterialLayout();
                if (buildEffectControls)
                    RebuildEffectLayout();

                CreateTooltips();
                ControlFactory.UpdateVisibility();
                ApplyCurrentAppearance();
                originalMaterial = CloneMaterial(currentMaterial);
            }
            finally
            {
                layoutEffect.ResumeLayout(true);
                layoutMaterial.ResumeLayout(true);
                layoutGeneral.ResumeLayout(true);
                contentHostLayout.ResumeLayout(true);
                contentScrollPanel.ResumeLayout(true);
            }
        }

        #endregion
    }
}
