using UnityEngine;

namespace JLink.Frames
{
    [CreateAssetMenu(menuName = "JLink/Picture Frame Design")]
    public sealed class FrameDesign : ScriptableObject
    {
        public FrameRecipe recipe = new FrameRecipe();
    }
}
