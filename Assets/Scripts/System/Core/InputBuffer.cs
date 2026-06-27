// ============================================================
//  InputBuffer.cs
//  입력 버퍼 시스템 (싱글톤)
//
//  [사용법]
//  1. 씬 내 임의의 GameObject에 InputBuffer 컴포넌트를 추가하거나
//     Awake()에서 자동 생성됩니다.
//
//  2. 입력을 등록할 때 (예: InputSystem / LegacyInput 래퍼에서):
//       InputBuffer.Instance.Register(InputBufferAction.Jump);
//
//  3. 실제 처리 시점에 버퍼를 소비:
//       if (InputBuffer.Instance.Consume(InputBufferAction.Jump))
//           DoJump();
//
//  [설계 포인트]
//  - 각 액션당 하나의 버퍼 슬롯을 유지합니다.
//  - bufferWindow 시간(초) 이내의 입력만 유효로 간주합니다.
//  - Consume()은 유효한 버퍼가 있을 때 true를 반환하고 즉시 소거합니다.
//  - Peek()으로 소거 없이 유효 여부만 확인할 수 있습니다.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace System.Core
{
    public class InputBuffer : MonoBehaviour
    {
        // ── 인스펙터 ──────────────────────────────────────────
        [Tooltip("입력이 유효한 시간 (초). 이 시간이 지나면 버퍼가 자동으로 만료됩니다.")]
        [SerializeField] private float bufferWindow = 0.15f;

        // ── 싱글톤 ────────────────────────────────────────────
        public static InputBuffer Instance { get; private set; }

        // ── 내부 데이터 ───────────────────────────────────────
        // 각 액션별 마지막 등록 시각 (Time.unscaledTime 기준)
        private readonly Dictionary<InputBufferAction, float> _buffer =
            new Dictionary<InputBufferAction, float>();

        // ── 공개 프로퍼티 ─────────────────────────────────────
        /// <summary>현재 설정된 버퍼 유효 시간(초).</summary>
        public float BufferWindow
        {
            get => bufferWindow;
            set => bufferWindow = Mathf.Max(0f, value);
        }

        // ─────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitBuffer();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region Public API

        /// <summary>
        /// 액션을 버퍼에 등록합니다.
        /// 입력 시스템(래퍼)에서 버튼 누름 이벤트 발생 시 호출하세요.
        /// </summary>
        public void Register(InputBufferAction action)
        {
            _buffer[action] = Time.unscaledTime;
        }

        /// <summary>
        /// 버퍼를 소비합니다.
        /// 유효한 버퍼가 있으면 true를 반환하고 해당 버퍼를 즉시 제거합니다.
        /// </summary>
        public bool Consume(InputBufferAction action)
        {
            if (!IsValid(action)) return false;

            _buffer[action] = float.NegativeInfinity; // 즉시 만료
            return true;
        }

        /// <summary>
        /// 버퍼를 소비하지 않고 유효 여부만 확인합니다.
        /// </summary>
        public bool Peek(InputBufferAction action) => IsValid(action);

        /// <summary>
        /// 특정 액션의 버퍼를 수동으로 초기화합니다.
        /// </summary>
        public void Clear(InputBufferAction action)
        {
            _buffer[action] = float.NegativeInfinity;
        }

        /// <summary>
        /// 모든 액션의 버퍼를 초기화합니다.
        /// </summary>
        public void ClearAll()
        {
            var actions = Enum.GetValues(typeof(InputBufferAction));
            foreach (InputBufferAction action in actions)
                _buffer[action] = float.NegativeInfinity;
        }

        /// <summary>
        /// 특정 액션의 남은 유효 시간(초)을 반환합니다. (만료 시 0 이하)
        /// </summary>
        public float GetRemainingTime(InputBufferAction action)
        {
            if (!_buffer.TryGetValue(action, out float registeredTime))
                return 0f;
            return bufferWindow - (Time.unscaledTime - registeredTime);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region Private Helpers

        private void InitBuffer()
        {
            var actions = Enum.GetValues(typeof(InputBufferAction));
            foreach (InputBufferAction action in actions)
                _buffer[action] = float.NegativeInfinity;
        }

        private bool IsValid(InputBufferAction action)
        {
            if (!_buffer.TryGetValue(action, out float registeredTime))
                return false;
            return (Time.unscaledTime - registeredTime) <= bufferWindow;
        }

        #endregion

#if UNITY_EDITOR
        // ─────────────────────────────────────────────────────
        #region Editor Debug

        [Header("── Debug (Editor Only) ──")]
        [SerializeField] private bool showDebugGUI = false;

        private void OnGUI()
        {
            if (!showDebugGUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 260, 200));
            GUILayout.Label("[ InputBuffer Debug ]");

            foreach (InputBufferAction action in Enum.GetValues(typeof(InputBufferAction)))
            {
                float remaining = GetRemainingTime(action);
                bool valid = remaining > 0f;
                string status = valid ? $"<color=lime>{remaining * 1000f:F0} ms</color>" : "<color=grey>--</color>";
                GUILayout.Label($"{action,-14}: {status}", new GUIStyle(GUI.skin.label) { richText = true });
            }

            GUILayout.EndArea();
        }

        #endregion
#endif
    }
}
