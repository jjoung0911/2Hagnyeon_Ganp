using System;
using DG.Tweening;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player.LevelSystem
{
    // 경험치 오브 - 드롭 애니메이션이 끝나면 회전하며 대기하다가, 플레이어가 감지 범위에 들어오면
    // 자력으로 끌려가 충분히 가까워지면 흡수된다 (OnCollected 발행)
    public class ExperienceOrb : AbstractMonoPoolable
    {
        [Header("Drop")]
        [SerializeField] private float dropHeight = 1.5f;
        [SerializeField] private float dropDuration = 0.5f;
        [SerializeField] private float dropRadius = 1f;

        [Header("Magnet")]
        [SerializeField] private float magnetRadius = 4f;
        [SerializeField] private float magnetSpeed = 10f;
        [SerializeField] private float collectDistance = 0.4f;
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private LayerMask playerLayer;

        public event Action<ExperienceOrb, int> OnCollected;

        private readonly Collider[] _detectResults = new Collider[1];
        private int _expAmount;
        private Transform _target;
        private bool _isDropping;

        // 풀에서 꺼낸 직후 위치와 경험치량을 설정하고 드롭 애니메이션을 재생한다
        public void Init(Vector3 position, int expAmount)
        {
            _expAmount = expAmount;
            PlayDropAnimation(position);
        }

        private void PlayDropAnimation(Vector3 dropPosition)
        {
            _isDropping = true;
            _target = null;

            transform.position = dropPosition;

            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * dropRadius;
            Vector3 landPosition = dropPosition + new Vector3(randomOffset.x, 0f, randomOffset.y);

            transform.DOJump(landPosition, dropHeight, 1, dropDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _isDropping = false);
        }

        public override void ResetItem()
        {
            base.ResetItem();
            transform.DOKill();
            _target = null;
            _isDropping = false;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

            if (_isDropping)
                return;

            if (_target == null)
            {
                if (Physics.OverlapSphereNonAlloc(transform.position, magnetRadius, _detectResults, playerLayer) > 0)
                    _target = _detectResults[0].transform;
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, _target.position, magnetSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _target.position) <= collectDistance)
                OnCollected?.Invoke(this, _expAmount);
        }
    }
}
