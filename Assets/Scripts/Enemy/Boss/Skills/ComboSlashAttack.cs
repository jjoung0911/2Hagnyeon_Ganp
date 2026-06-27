using Agents.CombatSystem;
using Agents.Modules;
using Enemy.Skills;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy.Boss.Skills
{
    // 패턴 2: 3연속 콤보 (좌측 베기 -> 우측 베기 -> 강한 내려찍기).
    // 하나의 콤보 애니메이션 안에서 Attack 이벤트(OnAttackStart)가 3회 발화되며,
    // 발화된 순서대로 1, 2타는 Light(경직 없음), 3타는 Heavy(짧은 경직 유발) 판정한다.
    // 3타가 적중하면 카메라 쉐이크 + 충격파 VFX를 추가로 재생한다.
    public class ComboSlashAttack : AbstractEnemySkill, ISkillCondition
    {
        [Header("사거리")]
        [SerializeField] private float maxRange = 4f;

        [Header("콤보 애니메이션 (Attack 이벤트 3회 발화)")]
        [SerializeField] private AnimParamSO comboClip;
        [SerializeField] private float crossDuration = 0.1f;

        [Header("바닥 텔레그래프")]
        [SerializeField] private AttackTelegraphView telegraphPrefab;
        [SerializeField] private float telegraphRadius = 4f;
        [SerializeField] private float telegraphAngle = 120f;
        [SerializeField] private float telegraphDuration = 0.5f;

        [Header("타수별 히트 타입 (0: 좌, 1: 우, 2: 내려찍기)")]
        [SerializeField] private HitType[] hitTypes = { HitType.Light, HitType.Light, HitType.Heavy };

        [Header("3타 적중 연출")]
        [SerializeField] private PoolItemSO finisherVfx;
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private EventChannelSO bossEventChannel;
        [SerializeField] private float finisherShakeForce = 1f;

        private const int FinisherIndex = 2;

        private BossPatternController _patternController;
        private IAnimationTrigger _trigger;
        private IOneShotCaster _caster;
        private GameObject _target;
        private int _comboIndex;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _patternController = _enemy?.GetModule<BossPatternController>();
            _trigger = _enemy?.GetModule<IAnimationTrigger>();
            _caster = GetComponentInChildren<IOneShotCaster>();

            if (_caster != null)
                _caster.OnHit += HandleCasterHit;
        }

        public bool CheckCondition(GameObject target)
        {
            if (target == null) return false;
            float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
            return dist <= maxRange
                   && (_patternController == null || _patternController.CanUsePattern(BossPatternType.ComboSlash));
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _patternController?.RecordPatternUsed(BossPatternType.ComboSlash);

            _comboIndex = 0;
            _target = target;

            if (telegraphPrefab != null)
            {
                var telegraph = Instantiate(telegraphPrefab, transform.position, transform.rotation);
                telegraph.SetAngle(telegraphAngle);
                telegraph.Show(telegraphRadius, telegraphDuration);
            }

            if (comboClip != null)
                _renderer?.PlayClip(comboClip.ParamHash, crossDuration);

            if (_trigger != null)
            {
                _trigger.OnAttackStart += HandleAttackStart;
                _trigger.OnAnimationEnd += HandleAnimationEnd;
            }
        }

        public override void StopSkill()
        {
            if (_trigger != null)
            {
                _trigger.OnAttackStart -= HandleAttackStart;
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
            }

            base.StopSkill();
        }

        private void OnDestroy()
        {
            if (_caster != null)
                _caster.OnHit -= HandleCasterHit;
        }

        private void HandleAttackStart()
        {
            if (_caster == null) return;
            _caster.CurrentHitType = _comboIndex < hitTypes.Length ? hitTypes[_comboIndex] : HitType.Light;
            _caster.SetDamageOverride(SkillData.damage);
            _caster.CastOnce();
            _comboIndex++;
        }

        private void HandleCasterHit(DamageData data)
        {
            if (_comboIndex - 1 != FinisherIndex) return;

            if (finisherVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    finisherVfx, data.HitPoint, Quaternion.identity));

            bossEventChannel?.RaiseEvent(BossEvents.CameraShakeRequest.Init(finisherShakeForce));
        }

        private void HandleAnimationEnd()
        {
            StopSkill();
        }
    }
}
