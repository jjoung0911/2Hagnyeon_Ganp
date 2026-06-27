using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace __.GameModules.PlayerData
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "SO/InputReader", order = 0)]
    public class InputReader : ScriptableObject, Controls.IPlayerActions
    {
        private Controls _input;
        public event Action OnDashKeyPressed;
        public event Action<bool> OnRunKeyPressed;
        // (skillIndex, isPressed) — 눌릴 때 true, 뗄 때 false
        public event Action<int, bool> OnSkillKeyStateChanged;
        public Vector2 MoveDir { get; private set; }

        public event Action<bool> OnInputPerformed;

        private bool _inputEnabled = false;
        private bool _isRunning = false;
        private int _lastSkillIndex;

        private void OnEnable()
        {
            _input ??= new Controls();

            _input.Player.SetCallbacks(this);
            _input.Enable();
            _inputEnabled = true;
        }

        // 입력 전체를 켜고 끈다. 사망 시 완전 잠금에 사용.
        // 잠글 때는 잔여 이동/달리기 상태를 초기화해 캐릭터가 그 자리에 멈추도록 한다.
        public void SetInputEnabled(bool enabled)
        {
            _input ??= new Controls();
            _input.Player.SetCallbacks(this);

            if (enabled)
            {
                _input.Enable();
            }
            else
            {
                _input.Disable();
                MoveDir = Vector2.zero;
                if (_isRunning)
                {
                    _isRunning = false;
                    OnRunKeyPressed?.Invoke(false);
                }
            }

            _inputEnabled = enabled;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveDir = context.ReadValue<Vector2>();

            if(Mathf.Approximately(MoveDir.sqrMagnitude, 0))
                MoveDir = Vector2.zero;
            // if(context.performed)
        
            // bool before = _inputEnabled;
            // _inputEnabled = !Mathf.Approximately(MoveDir.sqrMagnitude, 0);
            // if(before != _inputEnabled)
            //     OnInputPerformed?.Invoke(_inputEnabled);
        }

        public void OnAim(InputAction.CallbackContext context)
        {
        }
        
        public void OnDash(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnDashKeyPressed?.Invoke();
        }

        public void OnRun(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnRunKeyPressed?.Invoke(true);
            }
            if(context.canceled)
                OnRunKeyPressed?.Invoke(false);
        }

        public void OnSkill(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                _lastSkillIndex = (int)context.ReadValue<float>();
                OnSkillKeyStateChanged?.Invoke(_lastSkillIndex, true);
            }
            else if (context.canceled)
            {
                OnSkillKeyStateChanged?.Invoke(_lastSkillIndex, false);
            }
        }


        private void OnDisable()
        {
            _input.Disable(); 
        }
    }
}
