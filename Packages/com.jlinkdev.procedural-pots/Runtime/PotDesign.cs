using UnityEngine;

namespace JLink.Pots
{
    [CreateAssetMenu(menuName = "Pottery/Pot Design")]
    public sealed class PotDesign : ScriptableObject
    {
        public PotRecipe recipe = new PotRecipe();
    }
}
