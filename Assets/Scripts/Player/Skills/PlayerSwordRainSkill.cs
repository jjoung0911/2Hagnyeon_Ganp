using Agents.CombatSystem;
using Agents.Modules;
using JWLib.ObjectPool.Runtime;
using System.EffectSystem;
using System.StatSystem;
using UnityEngine;

namespace Player.Skills
{
    // 검우 — 플레이어 주변에 검의 비를 소환하는 범위 공격 스킬.
    // 시전 즉시(이동 잠금 없음) 플레이어 위치(앵커)에 VFX를 풀에서 꺼내 반경에 맞춰 스케일 재생하고,
    // 반경 내 모든 적에게 범위 피해를 1회 입힌다.
    // 진화("천검우")에서는 PlayerHeavenlySwordRainSkill로 교체된다.
    public class PlayerSwordRainSkill : AbstractPlayerSkill
    {
        [Header("범위 공격")]
        [SerializeField] protected LayerMask enemyLayer;
        [SerializeField] protected float baseAreaRadius = 4f;
        [SerializeField] protected HitType hitType = HitType.Medium;

        [Header("VFX (풀링)")]
        [SerializeField] protected PoolManagerSO poolManagerAsset;
        [SerializeField] protected PoolItemSO areaVfxItem;
        // VFX가 로컬 스케일 1에서 덮는 시각적 반경 — 타격 반경을 이 값에 맞춰 스케일링한다.
        // (AuraVisual과 동일한 단위 반경 컨벤션. 플레이모드에서 범위에 맞게 미세조정)
        [Tooltip("이 VFX가 스케일 1에서 덮는 반경. 타격 반경 / 이 값 = 적용 스케일.")]
        [SerializeField] protected float vfxUnitRadius = 3f;

        [Header("범위 링 인디케이터 (ChainSlash와 동일 방식)")]
        // 타격 경계를 명확히 보여주는 링 VFX. ChainSlash의 range 링 프리팹을 재사용 가능.
        [SerializeField] protected PoolItemSO areaRingVfxItem;
        [Tooltip("링 VFX가 스케일 1에서 덮는 반경. 타격 반경 / 이 값 = 적용 스케일.")]
        [SerializeField] protected float ringUnitRadius = 1.3f;

        [Header("진화")]
        [SerializeField] private PlayerHeavenlySwordRainSkill evolutionSkill;

        [Header("스탯")]
        [SerializeField] protected StatSO atkStatSo;

        protected const int HitBuffer = 16;
        protected readonly Collider[] _hitBuffer = new Collider[HitBuffer];

        protected IAgentStat _stat;
        // 타격 반경 보너스 — SkillUpgradeSO.targetFields(리플렉션)로 세팅
        protected float _areaRadiusBonus;
        // 천검우 진화 여부 — targetFields(리플렉션)로 세팅. true가 되면 OnUpgraded에서 진화 처리
        private bool _evolve;

        private void Reset() => startUnlocked = false;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _stat = _player.GetModule<IAgentStat>();
        }

        public override bool CanUseSkill(GameObject target = null) => DefaultCanUse();

        protected override void OnUpgraded(int level, SkillUpgradeSO data)
        {
            // _areaRadiusBonus/_evolve는 targetFields(리플렉션)로 이미 세팅됨 — 진화 부수효과만 처리
            if (_evolve && evolutionSkill != null)
                _skillModule.EvolveSkill(this, evolutionSkill);
        }

        protected float GetAtk() => _stat?.GetStat(atkStatSo.Index)?.Value ?? 0f;

        protected float CurrentAreaRadius => baseAreaRadius + _areaRadiusBonus;

        protected float GetSkillDamage() => GetAtk() * (SkillData.damage / 100f) * DamageMultiplier;

        public override void UseSkill(GameObject target = null)
        {
            BeginSkill();

            Vector3 center = _player.transform.position;
            float radius = CurrentAreaRadius;

            PlayAreaVfx(center, radius);
            ApplyAreaDamage(center, radius, GetSkillDamage());

            StopSkill();
        }

        // 반경 내 모든 적에게 1회 범위 피해
        protected void ApplyAreaDamage(Vector3 center, float radius, float damage)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, _hitBuffer, enemyLayer);
            for (int i = 0; i < count; i++)
            {
                if (!_hitBuffer[i].TryGetComponent<IDamageable>(out var damageable)) continue;
                Vector3 hitPoint = _hitBuffer[i].ClosestPoint(center);
                damageable.TakeDamage(new DamageData(damage, hitPoint, Vector3.up, _player, false, hitType));
            }
        }

        // 시전 위치(앵커)에 검의 비 VFX + 타격 경계 링을 함께 재생. 둘 다 종료 시 풀로 반환.
        protected void PlayAreaVfx(Vector3 center, float radius)
        {
            SpawnScaledVfx(areaVfxItem, center, radius, vfxUnitRadius);
            SpawnScaledVfx(areaRingVfxItem, center, radius, ringUnitRadius);
        }

        // 풀에서 VFX를 꺼내 (radius / unitRadius) 스케일로 center에 재생. 종료 시 풀로 반환.
        private void SpawnScaledVfx(PoolItemSO item, Vector3 center, float radius, float unitRadius)
        {
            if (poolManagerAsset == null || item == null) return;

            var vfx = poolManagerAsset.Pop<PoolableVFX>(item);
            if (vfx == null) return;

            vfx.transform.localScale = Vector3.one * (radius / Mathf.Max(0.01f, unitRadius));
            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(center, Quaternion.identity);
        }

        private void HandleVfxEnd(PoolableVFX vfx)
        {
            vfx.OnVfxEnd -= HandleVfxEnd;
            poolManagerAsset.Push(vfx);
        }

#if UNITY_EDITOR
        // 타격 반경 시각화용 — VFX(vfxUnitRadius)를 이 원에 맞춰 보정한다. 진화형은 radiusBonus만큼 더 넓다
        protected virtual float GizmoRadius => CurrentAreaRadius;

        // 선택 시 시전 중심에 타격 반경 원을 표시 (지면 평면)
        protected virtual void OnDrawGizmosSelected()
        {
            Vector3 center = _player != null ? _player.transform.position : transform.position;
            UnityEditor.Handles.color = new Color(1f, 0.4f, 0f, 0.9f);
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, GizmoRadius);
        }
#endif
    }
}
