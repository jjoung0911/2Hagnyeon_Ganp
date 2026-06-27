using System.Collections;
using System.Collections.Generic;
using Agents.CombatSystem;
using Agents.Modules;
using csiimnida.CSILib.SoundManager.RunTime;
using Enemy.Boss;
using JWLib.ObjectPool.Runtime;
using System.EffectSystem;
using System.Managers;
using System.StatSystem;
using Enemy;
using UnityEngine;

namespace Player.Skills
{
    // 연환참 — 주변 적들을 순서대로 연속 타격하는 순간이동형 연속 공격 스킬.
    // 스킬 사용 중 항상 무적. 사용 시 탐색 범위(업그레이드로 확장)를 보여주는 링 VFX를 표시한다.
    // Lv8 진화("무영난무")에서는 슬로우 모션 시전 연출, 플레이어 메시 숨김과 함께
    // 슬로우모션 동안 범위 내 적을 스냅샷해 maxTargets 상한 없이 전원을 순차 타격한다.
    public class PlayerChainSlashSkill : AbstractPlayerSkill
    {
        [Header("스탯")]
        [SerializeField] private StatSO atkStatSo;

        [Header("타격 판정")]
        [SerializeField] private OverlapDamageCaster caster;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private HitType hitType = HitType.Medium;
        [SerializeField] private int baseMaxTargets = 5;
        [SerializeField] private float baseRange = 8f;
        [SerializeField] private float strikeInterval = 0.15f;
        [SerializeField] private float teleportOffset = 1.5f;

        [Header("연출 — Trail")]
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private Color normalTrailStartColor = Color.white;
        [SerializeField] private Color evolvedTrailStartColor = Color.red;

        [Header("연출 — VFX 풀")]
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO hitVfxItem;
        // 탐색 범위를 시각화하는 링 VFX — range에 맞춰 스케일
        [SerializeField] private PoolItemSO rangeVfxItem;
        // 링 VFX가 로컬 스케일 1에서 덮는 시각적 반경. 탐색 range / 이 값 = 적용 스케일.
        // (AuraVisual과 동일한 단위 반경 컨벤션. 플레이모드에서 범위에 맞게 미세조정)
        [Tooltip("링 VFX가 스케일 1에서 덮는 반경. 탐색 range / 이 값 = 적용 스케일.")]
        [SerializeField] private float rangeVfxUnitRadius = 1.3f;

        [Header("사운드")]
        [SerializeField] private SoundSo slashSfx;
        [SerializeField] private SoundSo finalSfx;

        [Header("연출 — 카메라 쉐이크 / 히트스톱")]
        [SerializeField] private float hitShakeForce = 0.2f;
        [SerializeField] private float hitStopScale = 0.001f;
        [SerializeField] private float hitStopDuration = 0.06f;
        [SerializeField] private float finalShakeForce = 0.9f;
        [SerializeField] private float finalHitStopDuration = 0.16f;

        [Header("무영난무 — 슬로우 모션")]
        [SerializeField] private float slowMotionScale = 0.1f;
        [SerializeField] private float slowMotionDuration = 0.2f;

        private const int MaxHitBuffer = 16;

        private readonly Collider[] _hitBuffer = new Collider[MaxHitBuffer];
        private readonly HashSet<Collider> _struck = new();
        // 무영난무 — VFX 표시 시점에 확정한 범위 내 적 스냅샷
        private readonly List<Collider> _snapshot = new();
        private PoolableVFX _rangeVfx;

        private IAgentStat _stat;
        private HealthModule _healthModule;
        private PlayerCameraModule _cameraModule;

        private int _maxTargetsBonus;
        private float _rangeBonus;
        private int _bossExtraHits;
        private float _strikeIntervalReduction;
        private float _finalDamageMultiplier;
        private float _vfxScaleMultiplier = 1f;
        private bool _isShadowlessMassacre;

        private Coroutine _chainRoutine;

        private void Reset() => startUnlocked = false;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _stat = _player.GetModule<IAgentStat>();
            _healthModule = _player.GetModule<HealthModule>();
            _cameraModule = _player.GetModule<PlayerCameraModule>();
        }

        public override bool CanUseSkill(GameObject target = null) => DefaultCanUse();

        protected override void OnUpgraded(int level, SkillUpgradeSO data)
        {
            // 수치/플래그는 targetFields(리플렉션)로 이미 세팅됨 — VFX 스케일 기본값(미설정 0 → 1)만 보정
            if (_vfxScaleMultiplier <= 0f) _vfxScaleMultiplier = 1f;
        }

        private float GetAtk() => _stat?.GetStat(atkStatSo.Index)?.Value ?? 0f;

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _rootMotion?.End();
            LockMovement();
            _healthModule.IsInvincible = true;
            _renderer.SetVisible(false);

            if (trail != null)
            {
                trail.startColor = _isShadowlessMassacre ? evolvedTrailStartColor : normalTrailStartColor;
                Color endColor = trail.startColor;
                endColor.a = 0f;
                trail.endColor = endColor;
                trail.enabled = true;
            }

            float damage = GetAtk() * (SkillData.damage / 100f) * DamageMultiplier;
            int maxTargets = Mathf.Max(1, baseMaxTargets + _maxTargetsBonus);
            float range = baseRange + _rangeBonus;
            float interval = Mathf.Max(0.02f, strikeInterval - _strikeIntervalReduction);

            _chainRoutine = StartCoroutine(ChainRoutine(damage, maxTargets, range, interval));
        }

        private IEnumerator ChainRoutine(float damage, int maxTargets, float range, float interval)
        {
            _struck.Clear();
            _snapshot.Clear();
            Vector3 origin = _player.transform.position;
            Collider lastTarget = null;

            // 탐색 범위 프리뷰 VFX — 기본/진화 공통. 사용 시 즉시 범위를 시각화
            SpawnRangeVfx(origin, range);

            // 진화 — 슬로우모션 텔레그래프 동안 범위 내 적 전원을 스냅샷
            int iterations = maxTargets;
            if (_isShadowlessMassacre)
            {
                TimeManager.Instance.HitStop(slowMotionDuration, slowMotionScale);
                BuildSnapshot(origin, range);
                yield return new WaitForSecondsRealtime(slowMotionDuration);
                iterations = _snapshot.Count;
            }

            for (int i = 0; i < iterations; i++)
            {
                // 진화는 스냅샷 리스트 내에서, 기본은 매번 범위를 재탐색해 가장 가까운 적 선택
                Collider target = _isShadowlessMassacre
                    ? FindNearestInSnapshot(origin)
                    : FindNearestTarget(origin, range);
                if (target == null) break;

                // 죽은 적은 동선에서 제외하되 재선택되지 않도록 처리 후 건너뜀
                var enemy = target.GetComponentInParent<AbstractEnemy>();
                _struck.Add(target);
                if (enemy == null || enemy.IsDead) continue;

                lastTarget = target;

                yield return StrikeTarget(target, damage);

                // 플레이어가 순간이동한 실제 위치를 기준으로 다음 적을 탐색
                // — 적이 타격 후 파괴되더라도 안전하게 체인 유지
                origin = _player.transform.position;

                int extraHits = enemy is IBoss ? _bossExtraHits : 0;
                for (int h = 0; h < extraHits; h++)
                {
                    yield return new WaitForSecondsRealtime(interval * 0.5f);
                    if (target != null)
                        yield return StrikeTarget(target, damage);
                }

                if (i < iterations - 1)
                    yield return new WaitForSecondsRealtime(interval);
            }

            if (lastTarget != null)
                FinalStrike(lastTarget, damage);

            StopSkill();
        }

        private IEnumerator StrikeTarget(Collider target, float damage)
        {
            Vector3 toTarget = target.transform.position - _player.transform.position;
            toTarget.y = 0f;
            Vector3 dir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : _player.transform.forward;

            _moveData.Warp(target.transform.position - dir * teleportOffset);
            _player.transform.rotation = Quaternion.LookRotation(dir);

            yield return null;

            caster.CurrentHitType = hitType;
            caster.SetDamageOverride(damage);
            caster.CastOnce();

            // 텔레포트 방향을 쉐이크 방향으로 사용 — 체인마다 다른 방향 임펄스 전달
            TimeManager.Instance.HitStop(hitStopDuration, hitStopScale);
            _cameraModule?.ShakeCamera(hitShakeForce, dir);
            SpawnHitVfx(target.transform.position);
            _audio?.PlaySfx(slashSfx);
        }

        // 마지막 적에 대한 피날레 — 폭발 AOE는 제거하고 묵직한 타격감 연출만 유지.
        // 강화 데미지(_finalDamageMultiplier)는 설정된 경우에만 추가하고, 히트스톱·쉐이크·VFX·SFX는 항상 재생
        private void FinalStrike(Collider lastTarget, float damage)
        {
            Vector3 point = lastTarget.transform.position;

            if (_finalDamageMultiplier > 0f && lastTarget.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(new DamageData(damage * (1f + _finalDamageMultiplier),
                    point, Vector3.up, _player, false, hitType));

            // 최종타 강화 연출 — 강한 히트스톱 + 방향성 쉐이크 + VFX
            Vector3 finalDir = (_player.transform.position - point).normalized;
            TimeManager.Instance.HitStop(finalHitStopDuration, hitStopScale);
            _cameraModule?.ShakeCamera(finalShakeForce, finalDir);
            SpawnHitVfx(point);

            _audio?.PlaySfx(finalSfx);
        }

        private Collider FindNearestTarget(Vector3 origin, float range)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, range, _hitBuffer, targetLayer);
            Collider nearest = null;
            float minSqDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = _hitBuffer[i];
                if (_struck.Contains(col)) continue;

                float sqDist = (col.transform.position - origin).sqrMagnitude;
                if (sqDist < minSqDist)
                {
                    minSqDist = sqDist;
                    nearest = col;
                }
            }

            return nearest;
        }

        // VFX 표시 시점에 범위 내 적을 리스트로 확정 — 이후 범위를 벗어나도 타격 대상으로 유지
        private void BuildSnapshot(Vector3 origin, float range)
        {
            _snapshot.Clear();
            int count = Physics.OverlapSphereNonAlloc(origin, range, _hitBuffer, targetLayer);
            for (int i = 0; i < count; i++)
                _snapshot.Add(_hitBuffer[i]);
        }

        // 스냅샷 리스트 내에서 현재 위치 기준 가장 가까운, 아직 안 친 적을 선택
        private Collider FindNearestInSnapshot(Vector3 origin)
        {
            Collider nearest = null;
            float minSqDist = float.MaxValue;

            foreach (Collider col in _snapshot)
            {
                if (col == null || _struck.Contains(col)) continue;

                float sqDist = (col.transform.position - origin).sqrMagnitude;
                if (sqDist < minSqDist)
                {
                    minSqDist = sqDist;
                    nearest = col;
                }
            }

            return nearest;
        }

        private void SpawnHitVfx(Vector3 position)
        {
            if (poolManager == null || hitVfxItem == null) return;
            var vfx = poolManager.Pop<PoolableVFX>(hitVfxItem);
            if (vfx == null) return;

            vfx.transform.localScale = Vector3.one * _vfxScaleMultiplier;
            vfx.OnVfxEnd += HandleHitVfxEnd;
            vfx.PlayVfx(position, Quaternion.identity);
        }

        private void HandleHitVfxEnd(PoolableVFX vfx)
        {
            vfx.OnVfxEnd -= HandleHitVfxEnd;
            poolManager?.Push(vfx);
        }

        // 탐색 범위 링 VFX 생성 — 단위 반경 프리팹을 range에 맞춰 스케일해 플레이어 위치에 표시.
        // 스킬 종료(StopSkill) 시점까지 유지되며 ReturnRangeVfx로 반납된다
        private void SpawnRangeVfx(Vector3 origin, float range)
        {
            if (poolManager == null || rangeVfxItem == null) return;
            var vfx = poolManager.Pop<PoolableVFX>(rangeVfxItem);
            if (vfx == null) return;

            _rangeVfx = vfx;
            vfx.transform.localScale = Vector3.one * (range / Mathf.Max(0.01f, rangeVfxUnitRadius));
            vfx.PlayVfx(origin, Quaternion.identity);
        }

        private void ReturnRangeVfx()
        {
            if (_rangeVfx == null) return;
            poolManager?.Push(_rangeVfx);
            _rangeVfx = null;
        }

        public override void StopSkill()
        {
            UnlockMovement();
            _healthModule.IsInvincible = false;

            if (trail != null) trail.enabled = false;
            _renderer.SetVisible(true);
            ReturnRangeVfx();

            if (_chainRoutine != null)
            {
                StopCoroutine(_chainRoutine);
                _chainRoutine = null;
            }

            base.StopSkill();
        }

#if UNITY_EDITOR
        // 탐색 범위 시각화용 — 링 VFX(rangeVfxUnitRadius)를 이 원에 맞춰 보정한다 (지면 평면)
        private void OnDrawGizmosSelected()
        {
            Vector3 center = _player != null ? _player.transform.position : transform.position;
            UnityEditor.Handles.color = new Color(0f, 0.6f, 1f, 0.9f);
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, baseRange + _rangeBonus);
        }
#endif
    }
}
