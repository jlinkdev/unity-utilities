using JLink.Plants.Studio;
using UnityEditor;

namespace JLink.Plants.Editor
{
    [InitializeOnLoad]
    static class StudioBakeBridge
    {
        static StudioBakeBridge()
        {
            PlantStudio.BakeRequested += PlantMeshBaker.ChooseDestination;
            WindowsillWorkshop.PrefabRequested += PlantPrefabBaker.Choose;
        }
    }
}
