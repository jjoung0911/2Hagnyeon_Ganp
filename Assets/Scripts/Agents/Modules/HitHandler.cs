using System;
using System.Collections;
using Agents.CombatSystem;
using csiimnida.CSILib.SoundManager.RunTime;
using JWLib.AnimationSystem;
using JWLib.EventChannelSystem;
using JWLib.ObjectPool.Runtime;
using JWLib.ObjectPool.Runtime.Events;
using UnityEngine;
using UnityEngine.AI;

namespace Agents.Modules
{
    public abstract class AbstractHitHandler : MonoBehaviour, IModule, IHitHandler, IPoiseHandler
    {
        [Header("피격 애니메이션")]
        [SerializeField] protected AnimParamSO hitStateParam;
        [SerializeField] protected AnimParamSO hitLevelParam;
        
        [Header("사망 애니메이션")]
        [SerializeField] private AnimParamSO deathStateParam;
        [SerializeField] private EventChannelSO createEvent;
        [SerializeField] private PoolItemSO deadParticle;

        [Header("피격 VFX — 피격 지점에 출혈 연출(공격 종류 무관 공통)")]
        [SerializeField] private PoolItemSO hitBloodParticle;
        
        [Header("경직 지속 시간 (초)")]
        [SerializeField] private float heavyStaggerDuration = 0.7f;
        [SerializeField] private float launchStaggerDuration = 1.2f;

        [Header("넉백 거리 (m)")]
        [SerializeField] private float heavyKnockback = 2.5f;
        [SerializeField] private float launchKnockback = 3.0f;

        [SerializeField] private float launchUpRatio = 0.8f;

        [Header("강인도 (Poise) 시스템")]
        [Tooltip("강인도 최대값 — 이 값을 초과하면 경직 발생")]
        [SerializeField] private float poiseMax = 100f;

        [Tooltip("HitType별 강인도 데미지")]
        [SerializeField] private float lightPoiseDamage = 20f;
        [SerializeField] private float mediumPoiseDamage = 35f;
        [SerializeField] private float heavyPoiseDamage = 60f;
        [SerializeField] private float launchPoiseDamage = 100f;

        [Tooltip("마지막 피격 후 이 시간(초)이 지나면 강인도 게이지 초기화")]
        [SerializeField] private float poiseResetDelay = 4f;

        [Header("사운드")]
        [Tooltip("피격 시 재생할 SFX (비워두면 재생하지 않음)")]
        [SerializeField] private SoundSo hitSfx;
        [Tooltip("사망 시 재생할 SFX (비워두면 재생하지 않음)")]
        [SerializeField] private SoundSo deathSfx;

        protected Agent _owner;
        protected IRenderer _renderer;
        protected AudioModule _audio;
        private NavMeshAgent _navAgent;
        private Coroutine _staggerCoroutine;
        private Coroutine _poiseResetCoroutine;

        public bool IsStaggered { get; private set; }
        public float PoiseCurrent { get; private set; }
        public float PoiseMax => poiseMax;
        public event Action OnStaggerBegin;

        protected HitType CurrentHitType => _owner.ActionData?.HitType ?? HitType.Light;
        protected Vector3 CurrentHitNormal => _owner.ActionData?.HitNormal ?? Vector3.back;
        protected Vector3 CurrentHitPoint => _owner.ActionData?.HitPoint ?? _owner.transform.position;

        // HitType 강도를 피격 애니메이션 블렌드용 0~1 레벨로 변환
        protected static float HitTypeToLevel(HitType type) => type switch
        {
            HitType.Light  => 0f,
            HitType.Medium => 0.33f,
            HitType.Heavy  => 0.67f,
            HitType.Launch => 1f,
            _              => 0f
        };

        protected void PlayHitReaction(HitType type, float crossFadeDuration, float normalizedTime, int layer = -1)
        {
            if (hitStateParam == null) return;
            if (hitLevelParam != null) _renderer?.SetFloat(hitLevelParam, HitTypeToLevel(type));
            _renderer?.PlayClip(hitStateParam.ParamHash, crossFadeDuration, normalizedTime, layer);
        }

        // 사망 SFX 재생 — HandleDeath()를 오버라이드해 base를 호출하지 않는 파생 클래스에서도 사용
        protected void PlayDeathSfx() => _audio?.PlaySfx(deathSfx);

        public virtual void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            if (_owner == null)
            {
                Debug.LogError($"[{GetType().Name}] owner({owner?.GetType().Name})가 Agent가 아닙니다 — 구독 불가");
                return;
            }
            _renderer = owner.GetModule<IRenderer>();
            _audio = owner.GetModule<AudioModule>();
            _navAgent = owner.GetComponent<NavMeshAgent>();
            _owner.OnHit.AddListener(HandleHit);
            _owner.OnDeath.AddListener(HandleDeath);
        }

        protected virtual void HandleDeath()
        {
            PlayDeathSfx();
            _renderer.PlayClip(deathStateParam.ParamHash, 0.1f, 0, 1);
        }

        public virtual void HandleHit()
        {
            _audio?.PlaySfx(hitSfx);
            SpawnHitBlood();
            HitType type = CurrentHitType;
            Vector3 hitNormal = CurrentHitNormal;

            float poiseDamage = type switch
            {
                HitType.Light  => lightPoiseDamage,
                HitType.Medium => mediumPoiseDamage,
                HitType.Heavy  => heavyPoiseDamage,
                HitType.Launch => launchPoiseDamage,
                _              => lightPoiseDamage
            };

            PoiseCurrent += poiseDamage;

            if (_poiseResetCoroutine != null)
                StopCoroutine(_poiseResetCoroutine);
            _poiseResetCoroutine = StartCoroutine(PoiseResetCoroutine());

            if (PoiseCurrent < poiseMax)
                return;

            PoiseCurrent = 0f;

            HitType staggerType = type == HitType.Launch ? HitType.Launch : HitType.Heavy;
            float duration     = staggerType == HitType.Launch ? launchStaggerDuration : heavyStaggerDuration;
            float knockbackDist = staggerType == HitType.Launch ? launchKnockback : heavyKnockback;

            Vector3 knockDir = -hitNormal;
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude < 0.01f) knockDir = Vector3.back;
            knockDir.Normalize();

            if (staggerType == HitType.Launch)
                knockDir = Vector3.Lerp(knockDir, Vector3.up, launchUpRatio).normalized;

            PlayHitReaction(staggerType, 0.05f, 0.1f);

            if (_staggerCoroutine != null)
                StopCoroutine(_staggerCoroutine);
            OnStaggerBegin?.Invoke();
            _staggerCoroutine = StartCoroutine(StaggerCoroutine(duration, knockDir, knockbackDist));
        }

        // 피격 지점에 출혈 VFX를 띄운다. 공격자(스킬)별 HitVFX와 별개로,
        // 피격당하는 쪽에서 공통으로 재생하는 연출이라 모든 공격에 일괄 적용된다.
        protected void SpawnHitBlood()
        {
            if (hitBloodParticle == null || createEvent == null) return;

            Vector3 normal = CurrentHitNormal;
            Quaternion rot = normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal)
                : Quaternion.identity;
            createEvent.RaiseEvent(CreateEvents.ShowPoolingVfx.InitData(hitBloodParticle, CurrentHitPoint, rot));
        }

        private IEnumerator PoiseResetCoroutine()
        {
            yield return new WaitForSeconds(poiseResetDelay);
            PoiseCurrent = 0f;
            _poiseResetCoroutine = null;
        }

        private IEnumerator StaggerCoroutine(float duration, Vector3 knockDir, float knockDist)
        {
            IsStaggered = true;

            if (_navAgent != null)
                _navAgent.enabled = false;

            float elapsed = 0f;
            float speed = knockDist / Mathf.Max(duration * 0.4f, 0.05f);
            float startY = _owner.transform.position.y;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (elapsed < duration * 0.4f)
                {
                    float t = 1f - (elapsed / (duration * 0.4f));
                    _owner.transform.position += knockDir * (speed * t * Time.deltaTime);
                }

                Vector3 pos = _owner.transform.position;
                pos.y = startY;
                _owner.transform.position = pos;

                yield return null;
            }

            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                _navAgent.Warp(_owner.transform.position);
            }

            IsStaggered = false;
            _staggerCoroutine = null;
        }

        protected virtual void OnDestroy()
        {
            if (_owner != null)
                _owner.OnHit.RemoveListener(HandleHit);
        }
    }
}
