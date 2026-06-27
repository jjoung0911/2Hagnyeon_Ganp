using System;
using Agents.CombatSystem;
using Agents.Modules;
using Enemy;
using System.EffectSystem;
using UnityEngine;

namespace Player.Skills.Active.Aura
{
    // 지속형 — cooldown마다 플레이어 위치에 VFX를 켜고, activeDuration 동안
    // TickInterval마다 범위 내 전체에 tickDamage를 준 뒤 VFX를 끈다(버스트 방식).
    public class AuraSkill : AbstractPersistentSkill<AuraLevelData>
    {
        private const int MaxHitBuffer = 16;

        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private AssetNameSO vfxNameSo;

        private readonly Collider[] _hitBuffer = new Collider[MaxHitBuffer];
        private VFXModule _vfxModule;
        private PlayParticleVfx _auraVfx;
        private bool _isBursting;
        // 대기 중엔 다음 버스트까지 남은 시간, 버스트 중엔 종료까지 남은 시간
        private float _stateTimer;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _vfxModule = Player.GetModule<VFXModule>();
        }

        // 레벨이 바뀌면 진행 중인 버스트를 정리하고 cooldown 대기부터 다시 시작
        protected override void OnLevelDataChanged(AuraLevelData levelData)
        {
            _vfxModule.StopVFX(vfxNameSo.AssetHash);
            _isBursting = false;
            _stateTimer = levelData.cooldown;
        }

        // 버스트 중에는 VFX 위치를 플레이어 위치로 추적하고 지속시간을 소진,
        // 대기 중에는 cooldown을 소진해 다음 버스트 시점을 판단
        protected override void OnFrameUpdate(float dt)
        {
            var levelData = CurrentLevelData;
            _stateTimer -= dt;

            if (_isBursting)
            {
                // 위치는 부모(Player)를 따라가되, 회전은 플레이어 회전과 무관하게 고정
                if (_auraVfx != null)
                    _auraVfx.transform.rotation = Quaternion.identity;

                if (_stateTimer <= 0f)
                    EndBurst(levelData);
            }
            else if (_stateTimer <= 0f)
            {
                BeginBurst(levelData);
            }
        }

        // TickInterval마다 — 버스트 중일 때만 VFX 반경(levelData.radius)에 맞춰 범위 내 전체에 tickDamage
        protected override void OnTick()
        {
            if (!_isBursting) return;

            var levelData = CurrentLevelData;
            int count = Physics.OverlapSphereNonAlloc(Player.transform.position, levelData.radius, _hitBuffer, targetLayer);

            for (int i = 0; i < count; i++)
            {
                if (_hitBuffer[i].TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(new DamageData(levelData.tickDamage, _hitBuffer[i].transform.position
                        , Vector3.up, Player, false, HitType.Light));

                if (levelData.slowDuration > 0f
                    && _hitBuffer[i].GetComponentInParent<IStatusEffectModule>() is { } statusEffect)
                    statusEffect.ApplySlow(levelData.slowPercent, levelData.slowDuration);
            }
        }

        // VFX를 Pop해 레벨 반경에 맞춰 표시 — 이후 activeDuration 동안 OnTick이 피해를 준다
        private void BeginBurst(AuraLevelData levelData)
        {
            if(_auraVfx == null)
                _vfxModule.TryGetVFX(vfxNameSo.AssetHash, out _auraVfx);
                
            _isBursting = true;
            _stateTimer = levelData.activeDuration;

            // 비주얼 크기를 OnTick의 OverlapSphereNonAlloc 판정 반경과 동일하게 맞춤
            _auraVfx?.SetRadius(levelData.radius);
            _vfxModule.PlayVFX(vfxNameSo.AssetHash);
            Audio?.PlaySfx(activateSfx);
        }

        // VFX를 정지하고 Push해 사라지게 한 뒤 다음 버스트까지 cooldown 대기로 전환
        private void EndBurst(AuraLevelData levelData)
        {
            _isBursting = false;
            _stateTimer = levelData.cooldown;
            
            _vfxModule.StopVFX(vfxNameSo.AssetHash);
            if (_auraVfx != null)
                _auraVfx.Stop();
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, 5);
        }
    }
}
