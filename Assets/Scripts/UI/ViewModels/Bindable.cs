using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace UI.ViewModels
{
    // INotifyBindablePropertyChanged 구현 보일러플레이트를 줄이는 헬퍼.
    // ScriptableObject(StatViewModel)와 순수 클래스(SkillCooldownEntryViewModel)가
    // 상속 구조 없이 동일한 알림 방식을 공유하기 위해 정적 메서드로 분리했다.
    public static class Bindable
    {
        public static void Raise(EventHandler<BindablePropertyChangedEventArgs> handler, object sender,
            [CallerMemberName] string propertyName = "")
        {
            handler?.Invoke(sender, new BindablePropertyChangedEventArgs(propertyName));
        }
    }
}
