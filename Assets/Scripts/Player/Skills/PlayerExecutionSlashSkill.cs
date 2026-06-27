using System.Collections;
using System.EffectSystem;
using JWLib.AnimationSystem;
using System.Managers;
using Agents;
using Agents.CombatSystem;
using Agents.Modules;
using Enemy.Boss;
using JWLib.ObjectPool.Runtime;
using System.StatSystem;
using UnityEngine;

namespace Player.Skills
{
    // 처형참 — 가장 가까운 적에게 대검의 모든 힘을 집중한 단격.
    // Lv4 보스 추가 피해 / Lv5 방어력 관통 / Lv6 저체력 추가 피해 / Lv7 처치 시 쿨다운 환급.
    // 진화(일섬): 슬로우 모션 시전 → 적 후방 텔레포트 → X자 참격 → 체력 20% 이하 즉사.
    public class PlayerExecutionSlashSkill : AbstractPlayerSkill
    {
        [Header("스탯")]
        [SerializeField] private StatSO atkStatSo;

        [Header("판정")]
        [SerializeField] private RayDamageCaster caster;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private float searchRange = 8f;

        [Header("애니메이션")]
        [SerializeField] private AnimParamSO executionAnimParam;
        [SerializeField] private float dashScale = 1.5f;

        [Header("일반 참격 연출")]
        [SerializeField] private float hitStopDuration  = 0.15f;
        [SerializeField] private float hitStopScale     = 0.05f;
        [SerializeField] private float cameraShakeForce = 0.6f;

        [Header("일섬 (진화) 피해")]
        [SerializeField] private float evoMultiplier           = 12f;
        [SerializeField] private float evoArmorPenRatio        = 0.5f;
        [SerializeField] private float evoBossDamageBonus      = 0.3f;
        [SerializeField] private float evoInstantKillThreshold = 0.2f;

        [Header("일섬 (진화) 연출")]
        [SerializeField] private float evoSlowMotionScale    = 0.1f;
        [SerializeField] private float evoSlowMotionDuration = 0.2f;
        [SerializeField] private float evoTeleportOffset     = 1.5f;
        [SerializeField] private float evoHitStopDuration    = 0.18f;
        [SerializeField] private float evoHitStopScale       = 0.02f;
        [SerializeField] private float evoCameraShakeForce   = 1.4f;

        [Header("VFX 풀")]
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO windupVfxItem;
        [SerializeField] private PoolItemSO slashVfxItem;
        [SerializeField] private PoolItemSO hitMarkVfxItem;
        [SerializeField] private PoolItemSO xSlashVfxItem;

        // ── 모듈 캐시 ─────────────────────────────────────────────────
        private IAgentStat _stat;
        private HealthModule _playerHealth;
        private PlayerCameraModule _cameraModule;

        // ── 업그레이드 티어 캐시 ──────────────────────────────────────
        private float _bossDamageBonus;
        private float _armorPenRatio;
        private float _lowHpBonusMultiplier;
        private float _cooldownRefund;
        private bool  _isOneFlash;

        // ── 런타임 상태 ───────────────────────────────────────────────
        private readonly Collider[] _hitBuffer = new Collider[8];
        private Agent    _currentTarget;
        private float    _currentDamage;
        private Coroutine _routine;

        private void Reset() => startUnlocked = false;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _stat         = _player.GetModule<IAgentStat>();
            _playerHealth = _player.GetModule<HealthModule>();
            _cameraModule = _player.GetModule<PlayerCameraModule>();
            caster.InitCaster(_player);
            caster.OnKill += HandleKill;
        }

        private void OnDestroy()
        {
            if (caster != null) caster.OnKill -= HandleKill;
        }

        public override bool CanUseSkill(GameObject target = null) => DefaultCanUse();

        // 티어 수치(_bossDamageBonus 등)와 플래그(_isOneFlash)는 모두
        // SkillUpgradeSO.targetFields(리플렉션)로 직접 세팅되므로 별도 OnUpgraded 처리가 없다.

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _currentTarget = FindNearestTarget();
            if (_currentTarget == null) { StopSkill(); return; }

            FaceTarget(_currentTarget.transform);
            LockMovement();

            _trigger.OnAttackStart += HandleAttackStart;
            _trigger.OnAttack      += HandleAttack;

            if (_isOneFlash)
            {
                _routine = StartCoroutine(OneFlashSetupRoutine());
            }
            else
            {
                _currentDamage = CalculateDamage(_currentTarget);
                PlayVfxAt(windupVfxItem, _player.transform.position, _player.transform.rotation);
                _renderer.PlayClip(executionAnimParam.ParamHash, 0f, 0f);
                SubscribeAutoStopOnAnimationEnd();
            }
        }

        // ── 애니메이션 이벤트 핸들러 ─────────────────────────────────

        private void HandleAttackStart()
        {
            if (_isOneFlash) return;
            _playerHealth.IsInvincible = true;
            _rootMotion?.Begin(_player.transform.forward, dashScale);
        }

        private void HandleAttack()
        {
            if (_isOneFlash)
                HandleEvoAttack();
            else
            {
                _rootMotion?.End();
                HandleNormalAttack();
            }
        }

        private void HandleNormalAttack()
        {
            if (_currentTarget == null || _currentTarget.IsDead) return;

            PlayVfxAt(slashVfxItem, _player.transform.position, _player.transform.rotation);
            PlayVfxAt(hitMarkVfxItem, _currentTarget.transform.position, Quaternion.identity);

            caster.CurrentHitType = HitType.Heavy;
            caster.SetDamageOverride(_currentDamage);
            caster.CastOnce();

            // 참격 방향 반대 = 적에서 밀려나는 느낌의 방향성 임펄스
            Vector3 shakeDir = -_player.transform.forward;
            TimeManager.Instance.HitStop(hitStopDuration, hitStopScale);
            _cameraModule?.ShakeCamera(cameraShakeForce, shakeDir);
        }

        private void HandleEvoAttack()
        {
            if (_currentTarget == null || _currentTarget.IsDead) return;

            PlayVfxAt(xSlashVfxItem, _currentTarget.transform.position, Quaternion.identity);

            caster.CurrentHitType = HitType.Heavy;
            caster.SetDamageOverride(CalculateEvoDamage(_currentTarget));
            caster.CastOnce();

            // 적 등 뒤에서 찌르는 방향: 플레이어 → 적의 반대 = 타격이 적을 관통하는 느낌
            Vector3 shakeDir = (_player.transform.position - _currentTarget.transform.position).normalized;
            TimeManager.Instance.HitStop(evoHitStopDuration, evoHitStopScale);
            _cameraModule?.ShakeCamera(evoCameraShakeForce, shakeDir);
        }

        // ── 일섬 사전 연출 코루틴 ────────────────────────────────────

        private IEnumerator OneFlashSetupRoutine()
        {
            _playerHealth.IsInvincible = true;

            // 세상이 멈추는 순간 — 슬로우모션을 먼저 시작하고 플레이어가 사라짐
            PlayVfxAt(windupVfxItem, _player.transform.position, _player.transform.rotation);
            TimeManager.Instance.SetTimeScale(evoSlowMotionScale);
            _renderer.SetVisible(false);

            yield return new WaitForSecondsRealtime(evoSlowMotionDuration);
            TimeManager.Instance.SetTimeScale(1f);

            if (_currentTarget == null || _currentTarget.IsDead)
            {
                _renderer.SetVisible(true);
                _routine = null;
                StopSkill();
                yield break;
            }

            // 적 등 뒤에 순간이동 후 나타남 — 슬래시 VFX로 등장 연출
            _moveData.Warp(_currentTarget.transform.position - _currentTarget.transform.forward * evoTeleportOffset);
            FaceTarget(_currentTarget.transform);
            _renderer.SetVisible(true);
            PlayVfxAt(slashVfxItem, _player.transform.position, _player.transform.rotation);

            _renderer.PlayClip(executionAnimParam.ParamHash, 0f, 0f);
            SubscribeAutoStopOnAnimationEnd();
            _routine = null;
        }

        // ── 피해 계산 ─────────────────────────────────────────────────

        private float GetAtk() => _stat?.GetStat(atkStatSo.Index)?.Value ?? 0f;

        private float CalculateDamage(Agent target)
        {
            float dmg = GetAtk() * (SkillData.damage / 100f) * DamageMultiplier;

            if (_bossDamageBonus > 0f && target.GetComponent<IBoss>() != null)
                dmg *= 1f + _bossDamageBonus;

            if (_armorPenRatio > 0f)
                dmg *= 1f + _armorPenRatio;

            if (_lowHpBonusMultiplier > 0f)
            {
                var health = target.GetModule<HealthModule>();
                if (health != null && health.MaxHp > 0f)
                    dmg *= 1f + _lowHpBonusMultiplier * (1f - health.CurrentHp / health.MaxHp);
            }

            return dmg;
        }

        // 즉사 판정(일반 몬스터 체력 20% 이하)은 float.MaxValue로 처리 — 보스 제외
        private float CalculateEvoDamage(Agent target)
        {
            bool isBoss = target.GetComponent<IBoss>() != null;

            if (!isBoss && evoInstantKillThreshold > 0f)
            {
                var health = target.GetModule<HealthModule>();
                if (health != null && health.MaxHp > 0f &&
                    health.CurrentHp / health.MaxHp <= evoInstantKillThreshold)
                    return float.MaxValue;
            }

            float dmg = GetAtk() * evoMultiplier;
            if (isBoss) dmg *= 1f + evoBossDamageBonus;
            dmg         *= 1f + evoArmorPenRatio;
            return dmg;
        }

        // ── 킬 이벤트 — Lv7 쿨다운 환급 ─────────────────────────────

        private void HandleKill(Collider col, DamageData data)
        {
            if (_cooldownRefund > 0f)
                _lastUseTime -= _cooldownRefund;
        }

        // ── 탐색 / 방향 ───────────────────────────────────────────────

        private Agent FindNearestTarget()
        {
            int   count     = Physics.OverlapSphereNonAlloc(_player.transform.position, searchRange, _hitBuffer, targetLayer);
            Agent nearest   = null;
            float minSqDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var agent = _hitBuffer[i].GetComponentInParent<Agent>();
                if (agent == null || agent.IsDead) continue;

                float sq = (_player.transform.position - agent.transform.position).sqrMagnitude;
                if (sq < minSqDist) { minSqDist = sq; nearest = agent; }
            }

            return nearest;
        }

        private void FaceTarget(Transform target)
        {
            Vector3 look = target.position - _player.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                _player.transform.rotation = Quaternion.LookRotation(look);
        }

        // ── VFX 헬퍼 ─────────────────────────────────────────────────

        private void PlayVfxAt(PoolItemSO item, Vector3 pos, Quaternion rot)
        {
            if (poolManager == null || item == null) return;
            var vfx = poolManager.Pop<PoolableVFX>(item);
            if (vfx == null) return;
            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(pos, rot);
        }

        private void HandleVfxEnd(PoolableVFX vfx)
        {
            vfx.OnVfxEnd -= HandleVfxEnd;
            poolManager?.Push(vfx);
        }

        // ── 스킬 종료 ─────────────────────────────────────────────────

        public override void StopSkill()
        {
            UnlockMovement();
            _playerHealth.IsInvincible = false;
            _renderer.SetVisible(true); // 코루틴 조기 종료 시에도 메시 복원
            _rootMotion?.End();
            TimeManager.Instance.SetTimeScale(1f);

            _trigger.OnAttackStart -= HandleAttackStart;
            _trigger.OnAttack      -= HandleAttack;
            UnsubscribeAutoStopOnAnimationEnd();

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _currentTarget = null;
            base.StopSkill();
        }
    }
}
