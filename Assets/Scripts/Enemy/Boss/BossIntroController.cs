using Agents.Modules;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using System.Managers;
using Unity.Behavior;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace Enemy.Boss
{
    // 보스가 DifficultyManager에 의해 생성되면 AfterInit()에서 곧바로 등장 연출(Timeline)을 재생한다.
    // 연출이 끝나기 전까지 보스의 BT(BehaviorGraphAgent)는 비활성화 상태로 유지되며,
    // Timeline 재생이 끝나면 다시 활성화된다.
    // Timeline의 Signal Track이 PlayRoar(), RequestCameraShake(), StartSlowMotion()을 호출한다.
    public class BossIntroController : MonoBehaviour, IModule, IAfterInit
    {
        [SerializeField] private EventChannelSO bossEventChannel;

        [Header("연출 Timeline")]
        [SerializeField] private PlayableDirector director;

        [Header("카메라 컷씬")]
        [SerializeField] private CinemachineCamera introCam;
        [SerializeField] private int introCamPriority = 100;

        [Header("포효 연출")]
        [SerializeField] private AnimParamSO roarClip;
        [SerializeField] private float roarCrossDuration = 0.1f;
        [SerializeField] private float shakeForce = 1.5f;

        [Header("슬로우모션")]
        [SerializeField] private float slowMotionScale = 0.2f;
        [SerializeField] private float slowMotionDuration = 1.5f;

        private IRenderer _renderer;
        private BehaviorGraphAgent _btAgent;

        public void Initialize(ModuleOwner owner)
        {
            _renderer = owner.GetModule<IRenderer>();
            _btAgent = owner.GetComponent<BehaviorGraphAgent>();

            // 연출이 끝나기 전까지 보스 BT 비활성화
            if (_btAgent != null)
                _btAgent.enabled = false;

            if (introCam != null)
            {
                introCam.Priority.Value = introCamPriority;
                introCam.gameObject.SetActive(false);
            }
        }

        public void AfterInit()
        {
            if (director != null)
                director.stopped += HandleTimelineStopped;

            // DifficultyManager가 이 컴포넌트를 생성했다는 것 자체가 "지금 등장해야 함"을 의미하므로
            // 별도 트리거 없이 곧바로 등장 연출을 시작한다
            bossEventChannel?.RaiseEvent(BossEvents.BossIntroStarted);
            director?.Play();
        }

        private void OnDestroy()
        {
            if (director != null)
                director.stopped -= HandleTimelineStopped;
        }

        // Timeline Signal Track에서 호출 — 포효 애니메이션 재생
        public void PlayRoar()
        {
            if (roarClip != null)
                _renderer?.PlayClip(roarClip.ParamHash, roarCrossDuration);
        }

        // Timeline Signal Track에서 호출 — 화면 흔들림 요청
        public void RequestCameraShake()
        {
            bossEventChannel?.RaiseEvent(BossEvents.CameraShakeRequest.Init(shakeForce));
        }

        // Timeline Signal Track에서 호출 — 슬로우모션 진입
        public void StartSlowMotion()
        {
            TimeManager.Instance.HandleTimeScale(slowMotionScale, slowMotionDuration);
        }

        // Timeline 재생 종료 시 호출 — 보스 BT 재활성화 및 연출 종료 이벤트 발행
        private void HandleTimelineStopped(PlayableDirector _)
        {
            if (_btAgent != null)
                _btAgent.enabled = true;

            bossEventChannel?.RaiseEvent(BossEvents.BossIntroEnded);
        }
    }
}
