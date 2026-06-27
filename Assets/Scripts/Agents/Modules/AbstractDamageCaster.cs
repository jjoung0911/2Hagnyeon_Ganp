using System;
using Agents.CombatSystem;
using UnityEngine;

namespace Agents.Modules
{
    public abstract class AbstractDamageCaster : MonoBehaviour
    {
        public HitType CurrentHitType { get; set; } = HitType.Light;

        // 최종 데미지에 곱해지는 배율 (기본 1). 웨이브 난이도 스케일링 등 외부에서 설정.
        // 플레이어 캐스터는 설정하지 않으므로 항상 1로 동작한다.
        public float DamageMultiplier { get; set; } = 1f;

        public event Action<DamageData> OnHit;
        // 명중한 콜라이더 정보가 필요한 구독자용 (출혈 등 상태이상 적용)
        public event Action<Collider, DamageData> OnHitTarget;
        public event Action<Collider, DamageData> OnKill;

        protected Agent _owner;
        private float _damageOverride = -1f;

        public void InitCaster(ModuleOwner owner)
        {
            _owner = owner as Agent;
            OnInitialize();
        }

        protected virtual void OnInitialize(){}

        public void SetDamageOverride(float amount) => _damageOverride = amount;
        protected float GetEffectiveDamage(float baseDamage)
        {
            return (_damageOverride > 0f ? _damageOverride : baseDamage) * DamageMultiplier;
        }

        protected void ResetDamageOverride() => _damageOverride = -1f;

        protected void ApplyDamage(Collider col, IDamageable target, DamageData data)
        {
            var killable = col.GetComponentInParent<IKillable>();
            if (killable != null && killable.IsDead)
                return;

            target.TakeDamage(data);
            OnHit?.Invoke(data);
            OnHitTarget?.Invoke(col, data);

            if (killable != null && killable.IsDead)
                OnKill?.Invoke(col, data);
        }
    }
}
