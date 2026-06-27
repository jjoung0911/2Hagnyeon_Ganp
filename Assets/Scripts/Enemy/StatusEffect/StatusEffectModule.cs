using Agents.Modules;
using UnityEngine;

namespace Enemy
{
    // 기절/둔화 디버프 타이머를 관리하고 INavMovement에 반영하는 모듈.
    public class StatusEffectModule : MonoBehaviour, IModule, IStatusEffectModule
    {
        private INavMovement _navMovement;
        private HealthModule _health;
        private float _baseSpeed;

        private float _stunTimer;
        private float _slowTimer;
        private float _slowRatio;
        private bool _wasStunned;

        private float _bleedTimer;
        private float _bleedTickTimer;
        private float _bleedTickInterval;
        private float _bleedDamagePerTick;

        public bool IsStunned => _stunTimer > 0f;
        public bool IsBleeding => _bleedTimer > 0f;
        public float SpeedMultiplier { get; private set; } = 1f;

        public void Initialize(ModuleOwner owner)
        {
            _navMovement = owner.GetModule<INavMovement>();
            _health = owner.GetModule<HealthModule>();
            if (_navMovement != null)
                _baseSpeed = _navMovement.Speed;
        }

        public void ApplyStun(float duration)
        {
            _stunTimer = Mathf.Max(_stunTimer, duration);
        }

        public void ApplySlow(float ratio, float duration)
        {
            _slowRatio = Mathf.Max(_slowRatio, ratio);
            _slowTimer = Mathf.Max(_slowTimer, duration);
        }

        // 출혈 — 같은 대상에게 중첩 적용 시 더 강한 효과로 갱신 (덮어쓰기)
        public void ApplyBleed(float damagePerTick, float tickInterval, float duration)
        {
            _bleedDamagePerTick = Mathf.Max(_bleedDamagePerTick, damagePerTick);
            _bleedTickInterval = tickInterval;
            if (_bleedTimer <= 0f) _bleedTickTimer = tickInterval;
            _bleedTimer = Mathf.Max(_bleedTimer, duration);
        }

        public void ResetEffects()
        {
            _stunTimer = 0f;
            _slowTimer = 0f;
            _slowRatio = 0f;
            _bleedTimer = 0f;
            _bleedDamagePerTick = 0f;
            SpeedMultiplier = 1f;
            _wasStunned = false;

            if (_navMovement == null) return;
            _navMovement.IsStopped = false;
            _navMovement.Speed = _baseSpeed;
        }

        private void Update()
        {
            if (_stunTimer > 0f) _stunTimer -= Time.deltaTime;
            if (_slowTimer > 0f) _slowTimer -= Time.deltaTime;
            UpdateBleed();

            bool isStunnedNow = IsStunned;
            if (isStunnedNow != _wasStunned)
            {
                if (_navMovement != null) _navMovement.IsStopped = isStunnedNow;
                _wasStunned = isStunnedNow;
            }

            if (_navMovement == null) return;

            if (isStunnedNow)
            {
                SpeedMultiplier = 0f;
                return;
            }

            SpeedMultiplier = _slowTimer > 0f ? Mathf.Clamp01(1f - _slowRatio) : 1f;
            _navMovement.Speed = _baseSpeed * SpeedMultiplier;
        }

        private void UpdateBleed()
        {
            if (_bleedTimer <= 0f) return;

            _bleedTimer -= Time.deltaTime;
            _bleedTickTimer -= Time.deltaTime;
            if (_bleedTickTimer <= 0f)
            {
                _health?.ApplyDamage(_bleedDamagePerTick);
                _bleedTickTimer = _bleedTickInterval;
            }

            if (_bleedTimer <= 0f) _bleedDamagePerTick = 0f;
        }
    }
}
