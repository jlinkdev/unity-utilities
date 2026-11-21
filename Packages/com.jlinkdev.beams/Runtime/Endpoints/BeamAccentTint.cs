using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    [ExecuteAlways, RequireComponent(typeof(MeshRenderer))]
    public sealed class BeamAccentTint : MonoBehaviour
    {
        [ColorUsage(true, true)] public Color tint = Color.white;
        public bool surface;
        private MaterialPropertyBlock block;
        private void OnEnable() => Apply();
        public void Apply()
        {
            block ??= new MaterialPropertyBlock();
            var renderer = GetComponent<MeshRenderer>();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Tint", tint);
            block.SetFloat("_Surface", surface ? 1 : 0);
            block.SetFloat("_Facing", surface ? 0 : 1);
            renderer.SetPropertyBlock(block);
        }
    }
}
