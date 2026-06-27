using System.Collections;
using Agents.CombatSystem;
using UnityEngine;

namespace Player.Skills
{
    // 천검우 — 검우의 진화형 궁극기.
    // 더 넓은 반경과 높은 피해로 1회 강타한 뒤, 잔류 영역을 남긴다.
    // 잔류 영역은 지속 피해(DoT)를 입히고, 영역 안의 플레이어에게 공격력 % 버프를 부여한다.
    // 영역을 벗어나도 grace 시간 동안 버프가 유지된다.
    public class PlayerHeavenlySwordRainSkill : PlayerSwordRainSkill
    {
        [Header("천검우 — 강화 수치")]
        [SerializeField] private float radiusBonus = 2f;   // 기본형 대비 추가 반경
        [SerializeField] private float damageBonus = 0.5f; // 강타 피해 배율 추가 (0.5 = +50%)

        [Header("천검우 — 잔류 장판(DoT)")]
        [SerializeField] private float fieldDuration = 5f;
        [SerializeField] private float fieldTickInterval = 0.5f;
        [SerializeField] private float fieldDpsRatio = 0.3f; // 틱당 피해 = 강타 피해 * 이 비율

        [Header("천검우 — 공격력 버프")]
        [SerializeField, Range(0f, 3f)] private float atkBuffPercent = 0.3f; // 기본 공격력 대비 % (0.3 = +30%)
        [SerializeField] private float buffGrace = 1.5f; // 영역 이탈 후 버프 유지 시간(초)

        private bool _buffApplied;

        // 진화 전용 수치는 Inspector에서 직접 관리 — 기본형 인덱스 적용을 막기 위해 비워 둔다
        protected override void OnUpgraded(int level, SkillUpgradeSO data) { }

        public override void UseSkill(GameObject target = null)
        {
            BeginSkill();

            Vector3 center = _player.transform.position;
            float radius = CurrentAreaRadius + radiusBonus;
            float damage = GetSkillDamage() * (1f + damageBonus);

            PlayAreaVfx(center, radius);
            ApplyAreaDamage(center, radius, damage);

            // 잔류 영역은 스킬 사용 상태와 독립적으로 동작하므로 즉시 종료해 쿨다운/재사용을 막지 않는다
            StartCoroutine(LingeringFieldRoutine(center, radius, damage));
            StopSkill();
        }

        // 잔류 영역 — DoT 틱 + 플레이어 ATK 버프(영역 안 판정, grace 유지)
        private IEnumerator LingeringFieldRoutine(Vector3 center, float radius, float strikeDamage)
        {
            float tickDamage = strikeDamage * fieldDpsRatio;
            float elapsed = 0f;
            float tickTimer = fieldTickInterval;
            float lastInZoneTime = 0f;
            float sqRadius = radius * radius;

            while (elapsed < fieldDuration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // 지속 피해 틱
                tickTimer -= dt;
                if (tickTimer <= 0f)
                {
                    tickTimer = fieldTickInterval;
                    ApplyAreaDamage(center, radius, tickDamage);
                }

                // 플레이어 영역 내 판정 → 버프 적용/유지
                bool inside = (_player.transform.position - center).sqrMagnitude <= sqRadius;
                if (inside)
                {
                    lastInZoneTime = elapsed;
                    ApplyAtkBuff();
                }
                else if (_buffApplied && elapsed - lastInZoneTime >= buffGrace)
                {
                    RemoveAtkBuff();
                }

                yield return null;
            }

            RemoveAtkBuff();
        }

        private void ApplyAtkBuff()
        {
            if (_buffApplied || _stat == null || atkStatSo == null) return;

            float buffValue = (_stat.GetStat(atkStatSo.Index)?.BaseValue ?? 0f) * atkBuffPercent;
            _stat.AddModifier(atkStatSo.Index, this, buffValue);
            _buffApplied = true;
        }

        private void RemoveAtkBuff()
        {
            if (!_buffApplied) return;

            _stat?.RemoveModifier(atkStatSo.Index, this);
            _buffApplied = false;
        }

        // 영역 활성 중 비활성화되어도 버프가 영구 고착되지 않도록 정리
        private void OnDisable() => RemoveAtkBuff();

#if UNITY_EDITOR
        // 천검우는 기본 반경 + radiusBonus가 실제 타격 반경
        protected override float GizmoRadius => CurrentAreaRadius + radiusBonus;
#endif
    }
}
