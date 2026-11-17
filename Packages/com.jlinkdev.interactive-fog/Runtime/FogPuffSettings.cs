using UnityEngine;

namespace jlinkdev.UnityUtilities.InteractiveFog
{
    [System.Serializable]
    public struct FogPuffSettings
    {
        [Tooltip("World-space cell spacing. The effective spacing is at least 1.7 times maximum radius to bound neighboring-cell work.")]
        [Min(.2f)] public float spacing;
        [Tooltip("Chance that a procedural cell cycle contains a billow; controls statistical coverage, not an exact count.")]
        [Range(0,1)] public float coverage;
        public int fieldSeed;
        [Tooltip("Minimum and maximum procedural rise duration, in seconds.")]
        public Vector2 growthRange;
        [Tooltip("Minimum and maximum procedural settling duration, in seconds.")]
        public Vector2 settlingRange;
        [Tooltip("Minimum and maximum empty interval between a cell's cycles, in seconds.")]
        public Vector2 quietRange;
        [Tooltip("Minimum and maximum reference radius in metres (0.1 to 2). The footprint widens during settling.")]
        public Vector2 radiusRange;
        [Range(0,5)] public float density;
        [Tooltip("Density-volume emitted as mist per cubic metre of mature radius. Zero disables density transfer.")]
        [Range(0,6)] public float mistAmount;
        [Tooltip("Outward stirring during collapse, in metres per second.")]
        [Range(0,3)] public float spreadSpeed;
        public static FogPuffSettings Default => new()
        {
            spacing=2.8f,coverage=.45f,fieldSeed=173,growthRange=new Vector2(4,7),settlingRange=new Vector2(5,8),quietRange=new Vector2(2,5),
            radiusRange=new Vector2(.6f,1.2f), density=1.4f, mistAmount=1.2f, spreadSpeed=.4f
        };
    }

}
