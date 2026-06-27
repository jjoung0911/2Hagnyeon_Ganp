namespace Player.Skills.Active

{
    // 지속형(Persistent) 스킬 — 매 프레임 OnFrameUpdate 실행, TickInterval > 0이면
    // 일정 간격마다 OnTick 추가 실행 (예: 오라 데미지 틱).
    public abstract class AbstractPersistentSkill<TLevel> : AbstractActiveSkill where TLevel : struct
    {
        [UnityEngine.SerializeField] private PersistentSkillDataSO<TLevel> data;

        protected TLevel CurrentLevelData => data.GetLevel(Level);
        protected float TickInterval => data.GetTickInterval(Level);

        private float _tickTimer;

        public sealed override void Tick(float dt)
        {
            OnFrameUpdate(dt);

            float interval = TickInterval;
            if (interval <= 0f) return;

            _tickTimer -= dt;
            if (_tickTimer > 0f) return;

            _tickTimer += interval;
            OnTick();
        }

        // 매 프레임 실행 — 비주얼 추적/회전 등
        protected virtual void OnFrameUpdate(float dt) { }

        // TickInterval마다 실행 — 데미지 적용 등
        protected virtual void OnTick() { }

        protected sealed override void OnLevelChanged(int level)
        {
            _tickTimer = 0f;
            if (level <= 0) return;
            OnLevelDataChanged(data.GetLevel(level));
        }

        // 레벨 변경 시 비주얼/캐시 갱신
        protected abstract void OnLevelDataChanged(TLevel levelData);
    }
}
