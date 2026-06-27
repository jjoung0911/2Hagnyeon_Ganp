using Agents.CombatSystem;
using Agents.Modules;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Enemy.Skills
{
    // 투사체를 발사하는 원거리 스킬.
    // ISkillCondition: 최소~최대 사거리 안에 있을 때만 사용.
    // AttackStart 애니메이션 이벤트 시점에 투사체를 생성·발사한다.
    public class EnemyProjectileSkill : AbstractEnemySkill, ISkillCondition
    {
        [Header("애니메이션")]
        [SerializeField] private AnimParamSO attackClip;
        [SerializeField] private float crossDuration = 0.1f;

        [Header("투사체")]
        [SerializeField] private PoolManagerSO poolManagerAsset;
        [SerializeField] private PoolItemSO projectileItem;
        [SerializeField] private Transform firePoint;       // null이면 스킬 오브젝트 기준
        [SerializeField] private int projectileCount = 1;  // 동시 발사 수
        [SerializeField] private float spreadAngle = 20f;  // 다중 발사 시 퍼짐 각도
        [SerializeField] private HitType hitType = HitType.Light;

        [Header("사거리")]
        [SerializeField] private float minRange = 4f;
        [SerializeField] private float maxRange = 15f;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO fireVfx;
        [SerializeField] private EventChannelSO createChannel;

        private IAnimationTrigger _trigger;
        private GameObject _target;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            _trigger = _enemy?.GetModule<IAnimationTrigger>();
        }

        public bool CheckCondition(GameObject target)
        {
            if (target == null) return false;
            float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
            return dist >= minRange && dist <= maxRange;
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return !IsUsing && NormalizedCooldown >= 1f && CheckCondition(target);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _target = target;

            if (_trigger != null)
            {
                _trigger.OnAttack += HandleAttack;
                _trigger.OnAnimationEnd += HandleAnimationEnd;
            }

            if (attackClip != null)
                _renderer?.PlayClip(attackClip.ParamHash, crossDuration);
        }

        public override void StopSkill()
        {
            if (_trigger != null)
            {
                _trigger.OnAttack -= HandleAttack;
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
            }

            base.StopSkill();
        }

        private void HandleAttack()
        {
            _trigger.OnAttack -= HandleAttack;
            if (projectileItem == null || poolManagerAsset == null) return;

            Transform origin = firePoint != null ? firePoint : transform;

            // 타겟 가슴 높이 조준
            Quaternion baseRot = origin.rotation;
            if (_target != null)
            {
                Vector3 aimPos = _target.transform.position + Vector3.up;
                Vector3 dir = (aimPos - origin.position).normalized;
                if (dir.sqrMagnitude > 0.001f)
                    baseRot = Quaternion.LookRotation(dir);
            }

            // 발사 VFX
            if (fireVfx != null)
                createChannel?.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(
                    fireVfx, origin.position, baseRot));

            if (projectileCount <= 1)
            {
                Fire(origin.position, baseRot);
                return;
            }

            // 다중 투사체: 좌우 균등 분산
            float half = spreadAngle * (projectileCount - 1) * 0.5f;
            for (int i = 0; i < projectileCount; i++)
            {
                float yaw = -half + spreadAngle * i;
                Fire(origin.position, baseRot * Quaternion.Euler(0f, yaw, 0f));
            }
        }

        private void Fire(Vector3 position, Quaternion rotation)
        {
            EnemyProjectile proj = poolManagerAsset.Pop<EnemyProjectile>(projectileItem);
            proj.transform.SetPositionAndRotation(position, rotation);
            proj.Launch(_enemy, SkillData.damage, hitType);
        }

        private void HandleAnimationEnd() => StopSkill();
    }
}
