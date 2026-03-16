using MaterialLib;
using System;

namespace Material_Editor.Services
{
    internal static class MaterialFileTypeHelper
    {
        public static MaterialType GetMaterialType(BaseMaterialFile material)
        {
            return material is BGEM ? MaterialType.Effect : MaterialType.Material;
        }

        public static string GetExpectedExtension(MaterialType materialType)
        {
            return materialType == MaterialType.Effect ? ".bgem" : ".bgsm";
        }

        public static string GetFileDialogFilter(MaterialType materialType)
        {
            return materialType == MaterialType.Effect
                ? "Effect Files (.bgem)|*.bgem"
                : "Material Files (.bgsm)|*.bgsm";
        }

        public static string GetDisplayName(MaterialType materialType)
        {
            return materialType == MaterialType.Effect ? "BGEM effect" : "BGSM material";
        }
    }
}
