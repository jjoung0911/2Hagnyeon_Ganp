// ============================================================
//  InputBufferAction.cs
//  버퍼링할 수 있는 입력 액션 목록.
//  새로운 액션이 필요하면 여기에 항목을 추가하세요.
// ============================================================

namespace System.Core
{
    public enum InputBufferAction
    {
        Jump,
        Attack,
        Dodge,
        Interact,
        Dash,
        SpecialAttack,
    }
}
