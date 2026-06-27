using System;
using JWLib.AnimationSystem;
using UnityEngine;

namespace Agents.Modules
{
    public class AgentRenderer : MonoBehaviour, IModule, IRenderer, IAnimationTrigger
    {
        protected Agent _owner;
        protected Animator _animator;
        public Animator Animator => _animator;
        public event Action OnAnimationEnd;
        public event Action OnAttack;
        public event Action OnAttackEnd;
        public event Action OnAttackStart;
        public event Action OnFootStep;

        private Renderer[] _skinnedMeshRenderers;

        public virtual void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            _animator = owner.GetComponentInChildren<Animator>();
            _skinnedMeshRenderers = owner.GetComponentsInChildren<Renderer>();
        }

        public void PlayClip(int clipHash, float crossedFadeDuration, float normalizedTime = 0, int layer = 1)
        {
            if (_animator == null) return;
            // layer=-1: CrossFadeInFixedTime은 모든 레이어에 적용, Play는 첫 번째 레이어만 적용
            // → layer=-1일 때 Play를 쓰면 lowerBodyLayer가 스킵될 수 있으므로 항상 CrossFade 사용
            if (layer >= 0 && _animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == clipHash)
                _animator.Play(clipHash, layer, normalizedTime);
            else
                _animator.CrossFadeInFixedTime(clipHash, crossedFadeDuration, layer, normalizedTime);
        }

        public void SetBool(AnimParamSO param, bool value)
            => _animator.SetBool(param.ParamHash, value);

        public void SetFloat(AnimParamSO param, float value)
            => _animator.SetFloat(param.ParamHash, value);

        public void SetVector2(AnimParamSO xParam, AnimParamSO yParam, Vector2 value)
        {
            _animator.SetFloat(xParam.ParamHash, value.x);
            _animator.SetFloat(yParam.ParamHash, value.y);
        }
        
        public void SetInt(AnimParamSO param, int value)
            => _animator.SetInteger(param.ParamHash, value);
        public void SetTrigger(AnimParamSO param)
            => _animator.SetTrigger(param.ParamHash);

        public void SetAnimatorSpeed(float speed)
            => _animator.speed = speed;

        public void SetVisible(bool visible)
        {
            foreach (var r in _skinnedMeshRenderers)
                r.enabled = visible;
        }

        public void EndTrigger() => OnAnimationEnd?.Invoke();
        public void Attack() => OnAttack?.Invoke();
        public void AttackEnd() => OnAttackEnd?.Invoke();
        public void AttackStart() => OnAttackStart?.Invoke();
        public void FootStep() => OnFootStep?.Invoke();
    }
}