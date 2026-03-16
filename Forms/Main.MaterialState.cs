using MaterialLib;
using Material_Editor.Models;
using Material_Editor.Services;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private BaseMaterialFile CaptureCurrentMaterialState()
        {
            if (currentMaterial == null)
                return null;

            BaseMaterialFile snapshot = CurrentMaterialType switch
            {
                MaterialType.Effect => new BGEM(),
                _ => new BGSM(),
            };

            GetMaterialValues(snapshot);
            return snapshot;
        }

        private BaseMaterialFile CloneMaterial(BaseMaterialFile source)
        {
            return MaterialFileCloner.Clone(source);
        }
    }
}
