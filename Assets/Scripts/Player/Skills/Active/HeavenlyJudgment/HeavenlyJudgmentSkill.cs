using System.Collections;
using System.EffectSystem;
using Agents.CombatSystem;
using JWLib.ObjectPool.Runtime;
using UnityEngine;

namespace Player.Skills.Active
{
    // 발동형 — strikeCount회에 걸쳐 순차적으로 성검을 낙하시킨다.
    // 각 회차는 주변 적 위치(없으면 플레이어 주변 랜덤 위치)에 범위 데미지를 가한다.
    public class HeavenlyJudgmentSkill : AbstractTriggeredSkill<HeavenlyJudgmentLevelData>
    {
        private const int MaxHitBuffer = 16;

        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private float searchRange = 10f;
        [SerializeField] private float randomRange = 5f;

        private readonly Collider[] _hitBuffer = new Collider[MaxHitBuffer];

        // 발동 시점에 CurrentLevelData를 그대로 사용하므로 캐시할 상태가 없음
        protected override void OnLevelDataChanged(HeavenlyJudgmentLevelData levelData) { }

        protected override void Activate() => StartCoroutine(JudgmentRoutine(CurrentLevelData));

        private IEnumerator JudgmentRoutine(HeavenlyJudgmentLevelData levelData)
        {
            for (int i = 0; i < levelData.strikeCount; i++)
            {
                Strike(PickStrikePoint(), levelData);

                if (levelData.strikeInterval > 0f)
                    yield return new WaitForSeconds(levelData.strikeInterval);
            }
        }

        private Vector3 PickStrikePoint()
        {
            Vector3 origin = Player.transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, searchRange, _hitBuffer, targetLayer);

            if (count > 0)
                return _hitBuffer[Random.Range(0, count)].transform.position;

            Vector2 offset = Random.insideUnitCircle * randomRange;
            return origin + new Vector3(offset.x, 0f, offset.y);
        }

        private void Strike(Vector3 point, HeavenlyJudgmentLevelData levelData)
        {
            PlayStrikeVfx(point);

            int count = Physics.OverlapSphereNonAlloc(point, levelData.radius, _hitBuffer, targetLayer);
            for (int i = 0; i < count; i++)
            {
                if (_hitBuffer[i].TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(new DamageData(levelData.damage, point, Vector3.up, Player, false, HitType.Light));
            }
        }

        private void PlayStrikeVfx(Vector3 position)
        {
            if (PopVisual() is not PoolableVFX vfx) return;
            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(position, Quaternion.identity);
        }

        private void HandleVfxEnd(PoolableVFX vfx)
        {
            vfx.OnVfxEnd -= HandleVfxEnd;
            PushVisual(vfx);
        }
    }
}
