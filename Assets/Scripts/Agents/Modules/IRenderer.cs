using JWLib.AnimationSystem;
using UnityEngine;

namespace Agents.Modules
{
    public interface IRenderer
    {
        void PlayClip(int clipHash, float crossedFadeDuration, float normalizedTime = 0, int layer = -1);
        void SetBool(AnimParamSO param, bool value);
        void SetFloat(AnimParamSO param, float value);
        void SetVector2(AnimParamSO xParam, AnimParamSO yParam, Vector2 value);
        void SetInt(AnimParamSO param, int value);
        void SetTrigger(AnimParamSO param);
        void SetAnimatorSpeed(float speed);
        void SetVisible(bool visible);
    }
}