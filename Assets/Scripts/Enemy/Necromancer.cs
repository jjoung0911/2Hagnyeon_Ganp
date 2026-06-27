namespace Enemy
{
    // 소환형 엘리트 몬스터 — 직접 전투보다 전장 유지에 집중한다.
    // 기본 공격(Shadow Bolt)은 EnemyProjectileSkill, 소환은 NecromancerSummonSkill로 처리되며
    // 두 스킬 모두 RangedEnemy의 COMBAT 상태(UseSkillAction)에서 우선순위에 따라 자동 선택된다.
    public class Necromancer : RangedEnemy
    {
    }
}
