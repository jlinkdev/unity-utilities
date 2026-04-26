using UnityEngine;
using UnityUtilities.Pooling;

namespace UnityUtilities.Samples.Pooling
{
    /// <summary>
    /// Minimal example showing spawn and timed despawn with <see cref="GameObjectPoolBehaviour"/>.
    /// </summary>
    public sealed class PoolingExample : MonoBehaviour
    {
        [SerializeField] private GameObjectPoolBehaviour pool;
        [SerializeField] private float spawnIntervalSeconds = 0.5f;
        [SerializeField] private float lifetimeSeconds = 2f;
        [SerializeField] private Transform spawnPoint;

        private float _elapsed;

        private void Update()
        {
            if (pool == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < spawnIntervalSeconds)
            {
                return;
            }

            _elapsed = 0f;
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject instance = pool.Spawn(spawnPosition, Quaternion.identity);
            if (instance != null)
            {
                StartCoroutine(DespawnAfterDelay(instance));
            }
        }

        private System.Collections.IEnumerator DespawnAfterDelay(GameObject instance)
        {
            yield return new WaitForSeconds(lifetimeSeconds);

            if (instance != null)
            {
                pool.Despawn(instance);
            }
        }
    }
}
