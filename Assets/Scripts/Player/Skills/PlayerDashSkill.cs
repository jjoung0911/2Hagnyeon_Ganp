using System;
using Agents.CombatSystem;
using Agents.Modules;
using Agents.Modules.Movement;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;

namespace Player.Skills
{
    public class PlayerDashSkill : AbstractPlayerSkill
    {
        [SerializeField] private AnimParamSO dashAnimParam;
        [SerializeField] private PoolItemSO dashVfxItemSo;
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private float iFrameDuration = 0.4f;
        // 대쉬 직후 공격 데미지 보너스 (절댓값) — 0이면 비활성화
        [SerializeField] private float dashAttackBonus = 5f;
        // 보너스 유효 시간 (초) — 이 시간 내에 공격하면 적용
        [SerializeField] private float dashAttackBonusDuration = 0.4f;

        [Header("대쉬 충전")]
        // 동시에 보유 가능한 최대 충전 수 — 쿨타임이 지날 때마다 충전이 하나씩 채워짐
        [SerializeField] private int maxCharges = 1;

        // 업그레이드로 추가되는 최대 충전 수 — SkillUpgradeSO.targetFields(리플렉션)로 세팅
        private int _maxChargesBonus;
        // 이미 충전으로 환산해 지급한 보너스 — 업그레이드 시 늘어난 만큼만 즉시 충전하기 위한 추적값
        private int _appliedChargeBonus;

        [Header("충돌 무시")]
        [SerializeField] private LayerMask enemyLayer;

        private VFXModule _vfxModule;
        private HealthModule _healthModule;
        private PlayerAttackBuffModule _buffModule;

        private int _playerLayer;
        private int _currentCharges;
        // 다음 충전이 완료될 때까지 남은 시간 — 충전이 가득 찼으면 0
        private float _rechargeRemaining;

        private float CurrentCooldown => SkillData.cooldown * CooldownMultiplier;

        public override float NormalizedCooldown
        {
            get
            {
                if (_currentCharges >= MaxCharges) return 1f;
                float cd = CurrentCooldown;
                return Mathf.Approximately(cd, 0f) ? 1f : Mathf.Clamp01(1f - _rechargeRemaining / cd);
            }
        }

        public override bool IsReady => _currentCharges > 0;
        public override int MaxCharges => maxCharges + _maxChargesBonus;
        public override int CurrentCharges => _currentCharges;

        public override void Initialize(ISkillModule module)
        {
            base.Initialize(module);
            Debug.Assert(_player != null, "PlayerDashSkill can only be used with a Player agent.");
            _vfxModule = _player.GetModule<VFXModule>();
            _healthModule = _player.GetModule<HealthModule>();
            _buffModule = _player.GetModule<PlayerAttackBuffModule>();
            _playerLayer = _player.gameObject.layer;
            _currentCharges = MaxCharges;
        }

        protected override void OnUpgraded(int level, SkillUpgradeSO data)
        {
            // _maxChargesBonus는 targetFields(리플렉션)로 이미 세팅됨 — 늘어난 충전 수만큼 즉시 채워준다
            _currentCharges += _maxChargesBonus - _appliedChargeBonus;
            _appliedChargeBonus = _maxChargesBonus;
        }

        private void Update()
        {
            if (_currentCharges >= MaxCharges) return;

            _rechargeRemaining -= Time.deltaTime;
            if (_rechargeRemaining <= 0f)
            {
                _currentCharges++;
                _rechargeRemaining = _currentCharges < MaxCharges ? CurrentCooldown : 0f;
            }
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return _currentCharges > 0 && !IsUsing;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

            // 충전이 가득 찬 상태에서 사용하면 다음 충전 타이머를 새로 시작
            if (_currentCharges >= MaxCharges)
                _rechargeRemaining = CurrentCooldown;
            _currentCharges--;

            // 입력 방향이 있으면 해당 방향으로 대쉬, 없으면 현재 전방
            Vector3 dashDir = _moveData.TargetMoveDir;
            dashDir.y = 0f;
            if (dashDir.magnitude < 0.1f)
                dashDir = _player.transform.forward;
            dashDir.Normalize();
            _player.transform.rotation = Quaternion.LookRotation(dashDir);

            SetEnemyCollision(false);

            LockMovement();
            // _healthModule?.SetInvincible(iFrameDuration);
            _healthModule.IsInvincible = true;
            if (dashVfxItemSo != null) createChannel.RaiseEvent(CreateEvents.ShowPoolingVfx.
                InitData(dashVfxItemSo, transform.position, Quaternion.LookRotation(dashDir)));
            _rootMotion?.Begin(_player.transform.forward, 2.6f);
            _renderer.PlayClip(dashAnimParam.ParamHash, 0, 0.08f);
            SubscribeAutoStopOnAnimationEnd();
        }

        public override void StopSkill()
        {
            SetEnemyCollision(true);
            UnlockMovement();
            _rootMotion?.End();
            UnsubscribeAutoStopOnAnimationEnd();
            _healthModule.IsInvincible = false;

            // 대쉬 직후 공격 강화 — 0.4초 이내 공격 시 데미지 보너스
            if (dashAttackBonus > 0f)
                _buffModule?.ApplyDashBonus(dashAttackBonus, dashAttackBonusDuration);

            base.StopSkill();
        }

        // LayerMask의 각 레이어에 대해 플레이어 ↔ 해당 레이어 충돌을 켜거나 끈다
        private void SetEnemyCollision(bool enabled)
        {
            int mask = enemyLayer.value;
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                    Physics.IgnoreLayerCollision(_playerLayer, i, !enabled);
            }
        }
    }
}
