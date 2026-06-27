using JWLib.EventChannelSystem;
using UnityEngine;

namespace System.Events
{
    public static class CameraEvents
    {
        public static readonly RunningEvent onRunningStateChanged = new RunningEvent();
    }
    
    public class RunningEvent: GameEvent
    {
        public bool IsRunning;
        public RunningEvent Init(bool isRunning)
        {
            IsRunning = isRunning;
            return this;
        }
    }
}