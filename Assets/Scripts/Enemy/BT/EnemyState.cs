using Unity.Behavior;

namespace _00.Scripts.Enemy.BT
{
    [BlackboardEnum]
    public enum EnemyState
    {
        IDLE,MOVE,COMBAT,HIT,DEATH
    }
}