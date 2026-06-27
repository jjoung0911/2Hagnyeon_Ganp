using System.Collections.Generic;
using UnityEngine;

namespace Player.Skills.Active
{
    // 지속형 — 시작 시 슬롯 수만큼 검을 생성해 플레이어 주위를 떠다니게 한다.
    // 플레이어 주변에 적이 감지되면 검들이 발사되어 스스로 적을 호밍·관통 타격하고,
    // 적이 모두 사라지면 다시 플레이어 곁으로 복귀해 부유한다.
    // 슬롯별로 activeDuration만큼 활동하면 사라지고, cooldownDuration이 지나면 다시 등장한다.
    public class OrbitSwordSkill : AbstractPersistentSkill<OrbitSwordLevelData>
    {
        private const int MaxDetectBuffer = 8;
        private const float FanAngleDegrees = 140f; // 플레이어 등 뒤로 펼쳐지는 부채꼴 각도
        private const float SwayAmplitudeDegrees = 10f; // 부유 중 좌우로 흔들리는 각도 범위
        [SerializeField] private float positionSmoothTime = 0.15f; // 플레이어 이동을 따라갈 때의 위치 보정(Damping) 시간

        [SerializeField] private LayerMask targetLayer;

        private enum SwordState { Empty, Floating, Launched, Cooldown }

        private class Slot
        {
            public SwordState State;
            public OrbitSwordVisual Visual;
            public Vector3 Velocity;
            public float ActiveTimer; // Floating/Launched 상태로 누적된 시간 — activeDuration 도달 시 사라짐
            public float CooldownTimer; // Cooldown 상태에서 재등장까지 남은 시간
        }

        private readonly List<Slot> _slots = new();
        private readonly Collider[] _detectBuffer = new Collider[MaxDetectBuffer];
        private float _angle;

        // 보유 슬롯 개수를 레벨에 맞춰 동기화한다. 줄어들면 활성 검을 즉시 풀에 반납.
        protected override void OnLevelDataChanged(OrbitSwordLevelData levelData)
        {
            while (_slots.Count < levelData.swordCount)
                _slots.Add(new Slot { State = SwordState.Empty });

            while (_slots.Count > levelData.swordCount)
            {
                var last = _slots[^1];
                if (last.Visual != null)
                    PushVisual(last.Visual);
                _slots.RemoveAt(_slots.Count - 1);
            }
        }

        // 슬롯 상태에 따라 최초 생성, 부유 위치 갱신, 적 감지 시 발사/복귀를 처리
        protected override void OnFrameUpdate(float dt)
        {
            if (_slots.Count == 0) return;

            var levelData = CurrentLevelData;
            _angle += levelData.floatSpeed * dt;

            Vector3 center = Player.transform.position + Vector3.up * levelData.floatHeight;
            bool hasEnemy = HasEnemy(center, levelData.detectRadius);

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];

                switch (slot.State)
                {
                    case SwordState.Empty:
                        TrySpawn(slot, i, center, levelData);
                        break;

                    case SwordState.Floating:
                        if (TryExpire(slot, dt, levelData)) break;
                        if (hasEnemy) LaunchSlot(slot, levelData);
                        else UpdateFloating(slot, i, center, levelData);
                        break;

                    case SwordState.Launched:
                        if (TryExpire(slot, dt, levelData)) break;
                        // 검이 스스로 호밍/타격. 적이 사라지면 부유로 복귀.
                        if (!hasEnemy) ReturnSlot(slot);
                        break;

                    case SwordState.Cooldown:
                        TickCooldown(slot, dt);
                        break;
                }
            }
        }

        // 활동 시간을 누적시키고, activeDuration을 초과하면 검을 거둬들여 쿨다운 상태로 전환한다 (전환 시 true)
        private bool TryExpire(Slot slot, float dt, OrbitSwordLevelData levelData)
        {
            if (levelData.activeDuration <= 0f) return false;

            slot.ActiveTimer += dt;
            if (slot.ActiveTimer < levelData.activeDuration) return false;

            slot.Visual.StopAttack();
            PushVisual(slot.Visual);
            slot.Visual = null;
            slot.ActiveTimer = 0f;
            slot.CooldownTimer = levelData.cooldownDuration;
            slot.State = SwordState.Cooldown;
            return true;
        }

        // 쿨다운이 끝나면 다시 생성 가능하도록 Empty로 전환
        private void TickCooldown(Slot slot, float dt)
        {
            slot.CooldownTimer -= dt;
            if (slot.CooldownTimer <= 0f)
                slot.State = SwordState.Empty;
        }

        // 풀에서 검을 꺼내 부유 상태로 최초 1회 생성
        private void TrySpawn(Slot slot, int index, Vector3 center, OrbitSwordLevelData levelData)
        {
            if (PopVisual() is not OrbitSwordVisual sword) return;

            // 플레이어(부모)를 따라 즉시 이동하지 않도록 부모에서 분리 — 위치는 SmoothDamp로만 갱신
            sword.GameObject.transform.SetParent(null, true);
            sword.GameObject.transform.position = CalculateFloatTarget(index, center, levelData);
            sword.GameObject.transform.rotation = Player.transform.rotation;
            slot.Visual = sword;
            slot.Velocity = Vector3.zero;
            slot.State = SwordState.Floating;
        }

        // 검을 발사 — 이후 검이 스스로 탐색/호밍/관통 타격
        private void LaunchSlot(Slot slot, OrbitSwordLevelData levelData)
        {
            slot.State = SwordState.Launched;
            slot.Visual.Launch(Player, levelData.launchSpeed, levelData.damage, levelData.detectRadius,
                levelData.reHitInterval, levelData.maxDistance, targetLayer);
        }

        // 적이 사라져 부유로 복귀 — 검 공격 중단, 현재 위치에서 부유 지점으로 SmoothDamp 재개
        private void ReturnSlot(Slot slot)
        {
            slot.Visual.StopAttack();
            slot.Velocity = Vector3.zero;
            slot.State = SwordState.Floating;
        }

        // 부유 위치를 갱신. 검들은 플레이어 등 뒤 부채꼴(FanAngleDegrees)에 펼쳐진 채 좌우로 살짝 흔들린다.
        private void UpdateFloating(Slot slot, int index, Vector3 center, OrbitSwordLevelData levelData)
        {
            Vector3 targetPosition = CalculateFloatTarget(index, center, levelData);
            slot.Visual.GameObject.transform.position = Vector3.SmoothDamp(
                slot.Visual.GameObject.transform.position, targetPosition, ref slot.Velocity, positionSmoothTime);
            slot.Visual.GameObject.transform.rotation = Player.transform.rotation;
        }

        // 부채꼴 배치 + 좌우 흔들림을 적용한 목표 부유 위치를 계산
        private Vector3 CalculateFloatTarget(int index, Vector3 center, OrbitSwordLevelData levelData)
        {
            float spread = _slots.Count > 1 ? FanAngleDegrees : 0f;
            float angleStep = _slots.Count > 1 ? spread / (_slots.Count - 1) : 0f;
            float baseAngle = -90f - spread * 0.5f + angleStep * index; // -90도 = 플레이어 정면 기준 등 뒤 방향
            float angleDeg = baseAngle + Mathf.Sin(_angle * Mathf.Deg2Rad) * SwayAmplitudeDegrees;
            float rad = angleDeg * Mathf.Deg2Rad;

            Transform playerTransform = Player.transform;
            Vector3 offset = (playerTransform.right * Mathf.Cos(rad) + playerTransform.forward * Mathf.Sin(rad)) * levelData.floatRadius;
            return center + offset;
        }

        private bool HasEnemy(Vector3 origin, float radius)
            => Physics.OverlapSphereNonAlloc(origin, radius, _detectBuffer, targetLayer) > 0;
    }
}
