using System;
using UnityEngine;

namespace JLink.Plants
{
    public static class PlantInstances
    {
        public static ProceduralPlant Living(ProceduralPlant source, Transform parent=null) => Living(source,parent,null);

        /// <summary>Create an independent living instance with the source appearance and the supplied growth record.</summary>
        public static ProceduralPlant Living(ProceduralPlant source, Transform parent, PlantGrowth structure)
        {
            if (!source) throw new ArgumentNullException(nameof(source));
            var go=new GameObject(source.name+" (living)");
            go.SetActive(false);
            go.transform.SetParent(parent,false);
            var plant=go.AddComponent<ProceduralPlant>();
            source.CopyTo(plant,structure);
            go.SetActive(true);
            return plant;
        }

        public static GameObject Frozen(ProceduralPlant source, Transform parent=null)
        {
            if (!source || !source.CurrentMesh) throw new ArgumentException("An enabled plant is required.");
            var go=new GameObject(source.name+" (static)");
            go.transform.SetParent(parent,false);
            var mesh=UnityEngine.Object.Instantiate(source.CurrentMesh);
            mesh.name=source.Growth.Recipe.species+" mesh";
            mesh.hideFlags=HideFlags.DontSave;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;
            go.AddComponent<PlantMeshOwner>().Own(mesh);
            return go;
        }
    }
}
