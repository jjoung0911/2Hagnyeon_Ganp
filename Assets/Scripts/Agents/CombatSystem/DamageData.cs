using Agents.Modules;
using UnityEngine;

namespace Agents.CombatSystem
{
    // 피격 강도 — HitHandler가 읽어 경직/넉백 세기를 결정
    public enum HitType { Light, Medium, Heavy, Launch }

    public struct DamageData
    {
        public float DamageAmount;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public ModuleOwner Attacker;
        public bool IsCritical;
        public HitType HitType;

        public DamageData(float damageAmount, Vector3 hitPoint, Vector3 hitNormal,
            ModuleOwner attacker, bool isCritical, HitType hitType = HitType.Light)
        {
            DamageAmount = damageAmount;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Attacker = attacker;
            IsCritical = isCritical;
            HitType = hitType;
        }
    }
}
