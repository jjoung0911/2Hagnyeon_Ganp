using System;
using System.Collections;
using System.Core;
using UnityEngine;

namespace System.Managers
{
    // 타임스케일 통합 관리 모듈 — Player(히트스톱/슬로우모션)와 UI(일시정지)가 공통으로 사용한다.
    // 최종 Time.timeScale = (일시정지 중이면 0) : (이펙트 타임스케일)
    public class TimeManager : MonoSingleton<TimeManager>
    {
        private const float BaseFixedDeltaTime = 0.02f;

        [Header("HandleTimeScale 전환 설정")]
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private float minTimeScale = 0.0001f;
        [SerializeField] private float maxTimeScale = 2f;

        private float _effectTimeScale = 1f;
        private int _pauseCount;
        private Coroutine _effectCoroutine;

        public bool IsPaused => _pauseCount > 0;

        protected override void Awake()
        {
            base.Awake();
            ApplyTimeScale();
        }

        // UI에서 호출 — 일시정지 요청. 여러 곳에서 중복 호출돼도 Resume 횟수가 맞아야 해제됨
        public void Pause()
        {
            _pauseCount++;
            ApplyTimeScale();
        }

        // UI에서 호출 — 일시정지 해제 요청
        public void Resume()
        {
            _pauseCount = Mathf.Max(0, _pauseCount - 1);
            ApplyTimeScale();
        }

        // Player에서 호출 — 즉시 타임스케일 변경 (지속 효과)
        public void SetTimeScale(float scale)
        {
            if (_effectCoroutine != null)
            {
                StopCoroutine(_effectCoroutine);
                _effectCoroutine = null;
            }
            _effectTimeScale = scale;
            ApplyTimeScale();
        }

        // Player에서 호출 — duration 동안 timeScale을 적용했다가 1로 자동 복구 (히트스톱)
        public void HitStop(float duration, float timeScale)
        {
            if (_effectCoroutine != null)
                StopCoroutine(_effectCoroutine);
            _effectCoroutine = StartCoroutine(HitStopRoutine(duration, timeScale));
        }

        // Player에서 호출 — 곡선 기반으로 destScale까지 전환 후 onPeak, 다시 1로 복구 후 onComplete
        public void HandleTimeScale(float destScale, float duration, Action onPeak = null, Action onComplete = null)
        {
            if (_effectCoroutine != null)
            {
                StopCoroutine(_effectCoroutine);
                _effectCoroutine = null;
                // 이전 코루틴의 콜백은 버리되, 현재 타임스케일 값은 그대로 유지해 끊김 없이 새 전환으로 이어감
            }
            _effectCoroutine = StartCoroutine(TimeTransitionRoutine(destScale, duration, transitionCurve, minTimeScale, maxTimeScale, onPeak, onComplete));
        }

        private IEnumerator HitStopRoutine(float duration, float timeScale)
        {
            _effectTimeScale = timeScale;
            ApplyTimeScale();

            yield return new WaitForSecondsRealtime(duration);

            _effectTimeScale = 1f;
            ApplyTimeScale();
            _effectCoroutine = null;
        }

        private IEnumerator TimeTransitionRoutine(float destScale, float duration, AnimationCurve curve,
            float minScale, float maxScale, Action onPeak, Action onComplete)
        {
            float targetScale = Mathf.Clamp(destScale, minScale, maxScale);
            float startScale = _effectTimeScale;

            float reachDuration = duration / 3f * 2f;
            float t = 0f;
            while (t < reachDuration)
            {
                t += Time.unscaledDeltaTime;
                float curveVal = curve.Evaluate(t / reachDuration);
                _effectTimeScale = Mathf.Lerp(startScale, targetScale, curveVal);
                ApplyTimeScale();
                yield return null;
            }

            onPeak?.Invoke();

            float recoveryDuration = duration / 3f;
            float peakScale = _effectTimeScale;
            t = 0f;
            while (t < recoveryDuration)
            {
                t += Time.unscaledDeltaTime;
                float curveVal = curve.Evaluate(t / recoveryDuration);
                _effectTimeScale = Mathf.Lerp(peakScale, 1f, curveVal);
                ApplyTimeScale();
                yield return null;
            }

            _effectTimeScale = 1f;
            ApplyTimeScale();
            onComplete?.Invoke();
            _effectCoroutine = null;
        }

        private void ApplyTimeScale()
        {
            float scale = _pauseCount > 0 ? 0f : _effectTimeScale;
            Time.timeScale = scale;
            Time.fixedDeltaTime = BaseFixedDeltaTime * scale;
        }
    }
}
