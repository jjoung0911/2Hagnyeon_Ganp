using System.Collections;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace System.EffectSystem
{
    public class PoolableVFX : AbstractMonoPoolable
    {
        [SerializeField] private GameObject effectObject;
        private IPlayableVFX _playableVFX;

        public event Action<PoolableVFX> OnVfxEnd;

        private void Awake()
        {
            if (effectObject == null) return;
            _playableVFX = effectObject.GetComponent<IPlayableVFX>();
            if (_playableVFX == null) effectObject = null;
        }

        public override void ResetItem()
        {
            base.ResetItem();
            _playableVFX.Stop();
        }

        public void PlayVfx(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            StartCoroutine(PlayVfxCoroutine(position, rotation));
        }

        private IEnumerator PlayVfxCoroutine(Vector3 position, Quaternion rotation)
        {
            _playableVFX.Play();
            yield return new WaitForSeconds(_playableVFX.VfxDuration);
            OnVfxEnd?.Invoke(this);
        }

        public void PlayVfx() => _playableVFX.Play();
    }
}