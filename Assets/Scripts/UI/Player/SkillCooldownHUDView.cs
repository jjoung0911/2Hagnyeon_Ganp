using System.Collections.Generic;
using JWLib.EventChannelSystem;
using UI.Events;
using UnityEngine;

namespace UI.Player
{
    // 하단 스킬 쿨타임 HUD. PlayerUIRelayModule이 발행하는 SkillCooldownChangedEvent를 구독해
    // 슬롯을 동적으로 생성하고 아이콘/쿨타임/잠금 상태를 업데이트한다.
    // PlayerUIRelayModule.AfterInit()의 초기 발행보다 먼저 구독되도록 우선 실행한다
    // (PlayerUIBridge와 동일한 컨벤션 — EventChannelSO의 리플레이 캐시는 보조 방어선).
    [DefaultExecutionOrder(-10)]
    public class SkillCooldownHUDView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private SkillSlotUI slotTemplate;
        [SerializeField] private Transform slotsContainer;

        // slotIndex → 슬롯 인스턴스
        private readonly Dictionary<int, SkillSlotUI> _slots = new();

        private void Awake()
        {
            slotTemplate.gameObject.SetActive(false);

            // 에디터에서 레이아웃 미리보기로 배치해 둔 디자인용 슬롯을 런타임 시작 시 제거한다.
            // (동적 생성 슬롯과 중복되지 않도록. 템플릿이 컨테이너 자식인 경우는 보존)
            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = slotsContainer.GetChild(i);
                if (child != slotTemplate.transform)
                    Destroy(child.gameObject);
            }

            playerChannel.AddListener<SkillCooldownChangedEvent>(HandleSkillCooldownChanged);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<SkillCooldownChangedEvent>(HandleSkillCooldownChanged);
        }

        private void HandleSkillCooldownChanged(SkillCooldownChangedEvent evt)
        {
            if (!_slots.TryGetValue(evt.SlotIndex, out var slot))
            {
                slot = Instantiate(slotTemplate, slotsContainer);
                slot.gameObject.SetActive(true);
                // 이벤트 캐시 리플레이는 도착 순서가 보장되지 않으므로(EventChannelSO 참고),
                // 생성 순서가 아닌 SlotIndex로 형제 인덱스를 고정해 슬롯 표시 순서를 보장한다.
                slot.transform.SetSiblingIndex(evt.SlotIndex);
                _slots[evt.SlotIndex] = slot;
            }

            slot.SetData(evt.Icon, evt.IsUnlocked, evt.NormalizedCooldown, evt.RemainingSeconds);
        }
    }
}
