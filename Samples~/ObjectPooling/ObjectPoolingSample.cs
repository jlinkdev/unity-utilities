using UnityEngine;
using jlinkdev.UnityUtilities.ObjectPooling;

namespace jlinkdev.UnityUtilities.Samples.ObjectPooling
{
    /// <summary>
    /// Small example that spawns pooled objects on a timer and returns old ones.
    /// </summary>
    public sealed class ObjectPoolingSample : MonoBehaviour
    {
        [SerializeField] private GameObjectPoolHandle _poolHandle;
        [SerializeField] [Min(0.05f)] private float _spawnInterval = 0.5f;
        [SerializeField] [Min(0.1f)] private float _lifetime = 2f;
        [SerializeField] private int _maxLiveObjects = 12;

        private float _timer;

        private void Update()
        {
            if (_poolHandle == null)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < _spawnInterval)
            {
                return;
            }

            _timer = 0f;

            GameObject spawned = _poolHandle.Spawn(transform.position + Random.insideUnitSphere * 2f, Quaternion.identity);
            if (spawned == null)
            {
                return;
            }

            AutoReturn autoReturn = spawned.GetComponent<AutoReturn>();
            if (autoReturn == null)
            {
                autoReturn = spawned.AddComponent<AutoReturn>();
            }

            autoReturn.Initialize(_poolHandle, _lifetime);

            if (_poolHandle.CountActive > _maxLiveObjects)
            {
                _poolHandle.ClearInactive();
            }
        }

        private sealed class AutoReturn : MonoBehaviour
        {
            private GameObjectPoolHandle _owner;
            private float _returnAt;

            public void Initialize(GameObjectPoolHandle owner, float lifetime)
            {
                _owner = owner;
                _returnAt = Time.time + lifetime;
            }

            private void Update()
            {
                if (_owner == null || Time.time < _returnAt)
                {
                    return;
                }

                _owner.Despawn(gameObject);
            }
        }
    }
}
