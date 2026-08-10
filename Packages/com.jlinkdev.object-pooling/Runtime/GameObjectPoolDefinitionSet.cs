using UnityEngine;

namespace jlinkdev.UnityUtilities.ObjectPooling
{
    /// <summary>
    /// Reusable asset containing prefab-backed pool definitions for a registry.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameObject Pool Definition Set",
        menuName = "jlinkdev/Unity Utilities/Object Pooling/GameObject Pool Definition Set")]
    public sealed class GameObjectPoolDefinitionSet : ScriptableObject
    {
        [SerializeField] [Tooltip("Prefab pool definitions provided by this asset.")]
        private GameObjectPoolDefinition[] _definitions;

        /// <summary>
        /// Gets the prefab pool definitions provided by this asset.
        /// </summary>
        public GameObjectPoolDefinition[] Definitions => _definitions;
    }
}
