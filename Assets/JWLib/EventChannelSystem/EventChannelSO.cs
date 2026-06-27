using System;
using System.Collections.Generic;
using UnityEngine;

namespace JWLib.EventChannelSystem
{
    [CreateAssetMenu(fileName = "Event Channel", menuName = "Lib/EventChannel", order = 0)]
    public class EventChannelSO : ScriptableObject
    {
        private Dictionary<Type, Action<GameEvent>> _events = new();
        private Dictionary<Delegate, Action<GameEvent>> _lookUp = new(); // 중복 구독 방지를 위한 테이블

        // 늦게 구독한 리스너에게 마지막 값을 즉시 전달하기 위한 캐시.
        // 타입 하나당 GameEvent.CacheKey별로 따로 저장한다 — 스킬 슬롯처럼 같은 이벤트 타입을
        // 여러 대상(슬롯 인덱스 등)에 대해 연속 발행하는 경우, 타입만으로 캐시하면
        // 마지막 슬롯 값으로 덮어써져 나머지 슬롯의 초기값이 사라지는 문제가 있었다.
        private static readonly object DefaultCacheKey = new();
        private Dictionary<Type, Dictionary<object, GameEvent>> _lastEvents = new();

        // EventChannelSO는 에셋(SO)이라 씬 리로드 후에도 살아남는다.
        // 이전 씬에서 마지막으로 발행된 이벤트(_lastEvents)가 남아 있으면,
        // 새 씬의 구독자가 AddListener 하는 순간 죽은 이벤트가 리플레이되어
        // 잘못된 로직 재발동(예: 묵은 EnemyKilledEvent로 경험치 누적→레벨업, 묵은 선택창 이벤트로 업그레이드 UI 표시)
        // 또는 NullReferenceException(멀티캐스트 체인 중단)을 일으킨다.
        //
        // sceneUnloaded만으로는 "새 씬 Awake보다 늦게 불릴 수 있는" 타이밍 위험이 있어,
        // 씬 전환 코드가 LoadScene 직전 ClearAllCaches()를 호출해 확실히 비우도록 한다(타이밍 비의존).
        private static readonly HashSet<EventChannelSO> _allChannels = new();

        private void OnEnable()
        {
            _allChannels.Add(this);
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void OnDisable()
        {
            _allChannels.Remove(this);
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        private void HandleSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            _lastEvents.Clear();
        }

        // 마지막 이벤트 캐시만 비운다. 살아있는 리스너(_events)는 보존 — 씬 전환 직전 호출용.
        public void ClearCache() => _lastEvents.Clear();

        // 씬 전환 직전에 호출해 모든 채널의 묵은 이벤트 캐시를 비운다(다음 씬으로 리플레이 방지).
        public static void ClearAllCaches()
        {
            foreach (var channel in _allChannels)
                channel._lastEvents.Clear();
        }
        
        //MethodInfo: Class 내부의 메서드
        //Delegate는 인스턴스 마다의 메서드를 저장함.
        
        //handler : 핸들따라하지마
        
        public void AddListener<T>(Action<T> handler) where T : GameEvent
        {
            if (_lookUp.ContainsKey(handler)) return;
            
            //Action<GameEvent>는 GameEvent를 가지고 있으니까 GameEvent를 원하는 클래스로 캐스팅해서 보내야함
            Action<GameEvent> castHandler = (evt) => handler(evt as T); // 타입캐스트 콜 핸들러
            _lookUp[handler] = castHandler;
            Type eventType = typeof(T);
            if (!_events.TryAdd(eventType, castHandler))
            {
                _events[eventType] += castHandler;
            }

            // 구독 시점에 이미 발행된 값이 있다면 즉시 전달 (초기값 누락 방지)
            // CacheKey별로 여러 개가 캐시되어 있을 수 있으므로(예: 스킬 슬롯별 쿨다운) 전부 리플레이한다.
            if (_lastEvents.TryGetValue(eventType, out var keyedEvents))
            {
                foreach (GameEvent cached in keyedEvents.Values)
                    handler((T)cached);
            }
        }

        // public class 따라하지마 : GameEvent
        // {
        //     
        // }
        // void 따라쓰지마메서드()
        // {
        //     AddListener<따라하지마>(핸들따라하지마);
        // }
        //
        // private void 핸들따라하지마(따라하지마 이벤트)
        // {
        //     
        // }
        public void RemoveListener<T>(Action<T> handler) where T : GameEvent
        {
            Type eventType = typeof(T);
            if (_lookUp.TryGetValue(handler, out Action<GameEvent> action))
            {
                if (_events.TryGetValue(eventType, out Action<GameEvent> internalAction))
                {
                    internalAction -= action;
                    if (internalAction == null) // 구독을 해제하고 없었다면 딕셔너리에서 action제거
                    {
                        _events.Remove(eventType);
                    }
                    else // 남아있다면 다시 넣어줌
                    {
                        _events[eventType] = internalAction;
                    }
                }

                _lookUp.Remove(handler);
            }

        }

        public void RaiseEvent(GameEvent evt)
        {
            Type eventType = evt.GetType();
            if (!_lastEvents.TryGetValue(eventType, out var keyedEvents))
            {
                keyedEvents = new Dictionary<object, GameEvent>();
                _lastEvents[eventType] = keyedEvents;
            }
            keyedEvents[evt.CacheKey ?? DefaultCacheKey] = evt;

            if (_events.TryGetValue(eventType, out Action<GameEvent> action))
            {
                action?.Invoke(evt);
            }
        }

        public void Clear()
        {
            _events.Clear();
            _lookUp.Clear();
            _lastEvents.Clear();
        }
    }
}