# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 3D 액션 게임 프로젝트 (프로젝트명: 2학년 감프). Player, Enemy, 각종 게임 시스템으로 구성된다.

## 코딩 규칙

1. 모든 답변과 코드 주석은 **한글**로 작성
2. `Update()` 내부에서 `GetComponent`, `Find`, `Instantiate` 사용 금지 — GC 부하 방지
3. 컴포넌트 참조는 `[SerializeField] private`를 기본으로 사용
4. UI는 레거시 UI 대신 **TextMeshProUGUI(TMP)** 사용
5. 에디터 직접 조작이 필요한 작업은 `EditorWindow` 스크립트로 자동화

## 설계 원칙

### SOLID 원칙

모든 코드는 SOLID 원칙을 준수한다.

- **SRP (단일 책임)** — 클래스 하나는 한 가지 역할만. 이동은 `AgentMoveData`, 렌더링은 `AgentRenderer`, 전투 감지는 `PlayerCombatModule`처럼 기능별로 분리
- **OCP (개방-폐쇄)** — 새 스킬은 `AbstractPlayerSkill` 상속으로 추가. 기존 `PlayerSkillModule` 수정 없이 확장
- **LSP (리스코프 치환)** — `AbstractPlayerSkill`, `AgentMoveData` 등 추상 타입으로 참조. 구현체 교체 시 동작이 보장되어야 함
- **ISP (인터페이스 분리)** — `IModule`, `IRenderer`, `IMoveData`, `ISkillModule` 등 역할별 인터페이스로 분리. 모듈은 필요한 인터페이스만 구현
- **DIP (의존성 역전)** — 모듈 간 참조는 구체 클래스가 아닌 인터페이스로. `GetModule<IMoveData>()`처럼 인터페이스 타입으로 접근

### Modular Architecture

이 프로젝트의 모든 캐릭터 기능은 **독립적인 모듈**로 분리되어 `ModuleOwner`에 조립된다.

- **모듈은 서로 직접 참조하지 않는다** — 다른 모듈이 필요하면 `owner.GetModule<T>()`로 `Initialize()` 또는 `AfterInit()` 시점에 한 번만 가져와 캐싱
- **모듈은 MonoBehaviour 컴포넌트다** — Unity Inspector에서 조립하고, `ModuleOwner.Awake()`가 자동으로 수집·초기화
- **기능 추가 = 새 모듈 추가** — 기존 모듈을 수정해 기능을 끼워 넣지 않는다. 새 `IModule` 구현체를 만들어 GameObject에 부착
- **모듈 간 통신은 이벤트/채널로** — 직접 메서드 호출 대신 `EventChannelSO` 또는 C# 이벤트(`Action`)로 결합도를 낮춤
- **스킬도 모듈이다** — `AbstractPlayerSkill`은 `PlayerSkillModule` 하위 GameObject에 컴포넌트로 부착. 새 스킬은 기존 코드 수정 없이 추가

## 아키텍처 개요

### 핵심 패턴: Module System (`Agents/`)

모든 게임 캐릭터(Agent)는 `ModuleOwner` 기반의 모듈 시스템으로 동작한다.

```
ModuleOwner (MonoBehaviour)
  └─ Agent (abstract)
       ├─ Player
       └─ AbstractEnemy
```

- `ModuleOwner.Awake()`에서 자식 오브젝트의 `IModule` 구현체를 자동 수집 및 초기화
- 모듈 접근: `GetModule<T>()` — 정확한 타입으로 먼저 검색, 없으면 인터페이스로 폴백
- 초기화 순서: `Initialize()` → `AfterInit()` (의존성 있는 모듈은 `IAfterInit` 구현)
- 새 모듈 추가 시: `IModule` 구현, `Initialize(ModuleOwner owner)` 작성, Agent 하위 GameObject에 컴포넌트로 부착

주요 모듈:
- `IRenderer` / `AgentRenderer` — Animator 래핑, 애니메이션 재생. `PlayClip(hash, fadeDuration, normalizedTime, layer)`
- `IAgentStat` / `AgentStat` — 스탯 관리
- `INavMovement` / `NavMovement` — NavMeshAgent 이동 (적 전용)
- `ISensor` / `Sensor` — OverlapSphere 기반 감지, `ColliderResults` 배열 제공
- `ISkillModule` / `PlayerSkillModule` — 스킬 실행 + 내부 스킬 버퍼(0.3초) 관리
- `IAnimationTrigger` / `AgentRenderer` — 애니메이션 이벤트 콜백. `EndTrigger()`, `AttackStart()`, `AttackEnd()`, `DrawEnd()`, `SheatheEnd()` 등을 Unity Animation Event에서 호출
- `PlayerCombatModule` — 전투 스탠스(`CombatStance`) 관리, 적 감지 주기적 폴링
- `VFXModule` — 비주얼 이펙트 재생

### 이동 시스템 (`Agents/Modules/Movement/`)

`AgentMoveData` → `PlayerMoveData` (상속) 구조.

- `CanManualMove` — `false`이면 수평 이동 속도가 0이 됨. 스킬 실행 중 이동 잠금에 사용
- `IsRootMotionActive` — `true`이면 `AgentMover.FixedUpdate()`가 `MoveCharacter()` 스킵
- `IsNearGround()` — `CharacterController.isGrounded` 보조용 SphereCast. `groundMask`는 **적 레이어를 제외한 지면 레이어만** 설정해야 함 (CharacterController를 가진 오브젝트는 자동 제외)
- `PlayerMoveData.SetRunCondition(bool)` — 달리기 전환. 달리기 시작 시 0.5초 가속 코루틴 실행

이동 파이프라인:
```
PlayerMoveData.FixedUpdate() → SetMoveDir()
AgentMoveData.Update() → Lerp → CalculateFinalVelocity()
AgentMover.FixedUpdate() → _controller.Move()
```

### 스킬 시스템 (`Player/Skills/`)

모든 플레이어 스킬은 `AbstractPlayerSkill`을 상속한다.

```
AbstractPlayerSkill (MonoBehaviour, ISkill)
  ├─ PlayerAttackCombo
  ├─ PlayerDashSkill
  ├─ PlayerJumpModule
  ├─ PlayerJumpAttackSkill
  ├─ PlayerParryingSkill
  └─ PlayerSlideModule
```

**스킬 생명주기:**
1. `PlayerSkillModule.TryUseSkill(index)` — 현재 스킬 실행 중이면 0.3초 버퍼에 저장
2. `AbstractPlayerSkill.UseSkill()` — `IsUsing = true`, `_lastUseTime` 기록
3. `AbstractPlayerSkill.StopSkill()` — `IsUsing = false`, `OnSkillEnd?.Invoke()`
4. `PlayerSkillModule.HandleSkillEnd()` — 버퍼된 스킬 연결 또는 `combatIdleClip` 재생

**중요한 패턴 — `CanManualMove` 복원:**
- `StopSkill()` 오버라이드 시 `_moveData.CanManualMove = true`를 **반드시 첫 번째 줄**에 작성
- 이후 코드에서 예외가 발생해도 이동이 영구적으로 잠기지 않도록 방어
- `StopSkill()`은 `OnAnimationEnd` 이벤트에서만 호출되므로, 애니메이션이 중단되면 복원이 누락될 수 있음

**`CanUseInCombat` vs `CanUseAnyTime`:**
- `canUseInCombat` (SerializeField) — 전투 스탠스 한정 스킬
- `CanUseAnyTime` (SerializeField) — 스탠스 무관하게 사용 가능

**이벤트 구독 규칙:**
- `OnOnDetectEnd` 등 외부 이벤트는 `StopSkill()`에서도 **반드시 구독 해제** (애니메이션 이벤트 누락으로 `HandleAttackEndEffect`가 호출되지 않을 경우 대비)

### 전투 히트 감지 (`Player/PlayerSwordSensor.cs`)

검이 각 콜라이더와 **분리되는 순간** 이벤트로 히트 정보를 발화한다.

- `StartCollision()` — 충돌 감지 시작. 이전 프레임 위치 초기화
- `StopCollision()` — 종료 및 아직 접촉 중인 콜라이더를 즉시 `OnOnDetectEnd`로 처리
- `OnOnDetectEnd` (event) — `HitInfo` (Col, CutPoint, CutNormal) 발화. 구독자가 절단 처리
- 매 FixedUpdate마다 `sweepSamples`(기본 2)개의 보간 위치를 `OverlapBoxNonAlloc`으로 체크 → 빠른 회전 시 히트 누락 방지
- 같은 FixedUpdate 내 여러 샘플에서 동일 적에게 중복 데미지 방지 (`_damagedThisFrame`)

사용 패턴:
```csharp
// HandleAttackStart()에서
_sensor.OnOnDetectEnd += HandleSlice;
_sensor.StartCollision();

// HandleAttackEndEffect()에서
_sensor.StopCollision();       // 남은 접촉 처리 후 정리
_sensor.OnOnDetectEnd -= HandleSlice;

// StopSkill()에서도 방어적 해제
_sensor.OnOnDetectEnd -= HandleSlice;
```

### 전투 스탠스 (`Player/PlayerCombatModule.cs`)

- `CombatStance`: `Normal` → `Drawing` → `Combat` → `Sheathing` → `Normal`
- `BeginDraw()` / `BeginSheathe()` — `CanManualMove = false` 설정 후 애니메이션 이벤트(`OnDrawEnd`, `OnSheatheEnd`)로 복원
- 애니메이션 이벤트 누락 시 `CanManualMove = false` 고착 가능성 있음 (기존 알려진 이슈)
- `DetectEnemies()` — 0.2초 간격으로 `detectRadius` 내 적 감지. `combatExitDelay`(3초) 후 `BeginSheathe()`

### FSM (`Agents/FSM/`)

플레이어용 계층적 FSM. ScriptableObject로 상태를 데이터 드리븐 방식으로 구성한다.

- `AgentLayeredStateMachine` — 레이어별(`int Layer`) 독립 FSM을 관리
- `AgentStateMachine` — 단일 레이어. `StateSO.className`에서 리플렉션으로 상태 인스턴스 생성
- `AgentState` — 상태 기반 클래스. 생성자에서 `owner.GetModule<T>()`로 필요 모듈 참조
- `StateSO` — 상태 하나를 정의하는 SO (className, stateIndex, AnimParamSO)
- `ParentStateSO` — 레이어 하나를 정의 (Layer 번호 + SubStates 목록)
- 새 상태 추가: `AgentState` 상속 → `StateSO` 에셋 생성 후 `className`에 **전체 네임스페이스 포함** 클래스명 입력

### 적 AI (`Enemy/`)

`AbstractEnemy`는 Unity Behavior (BehaviorGraphAgent)를 사용한 BT(Behavior Tree) 기반 AI.

- BT 블랙보드 변수 접근: `SetVariableValue<T>()` / `GetVariable<T>()` 헬퍼 사용
- `BtVars` 클래스에 블랙보드 변수 이름 상수 관리
- `StateChannel` — BT와 코드 간 상태 이벤트 통신용 SO
- `AbstractEnemy.Start()`에서 블랙보드의 `StateChannel` 변수를 읽고 `this`를 `BtVars.Enemy`에 등록

### 스탯 시스템 (`System/StatSystem/`)

- `StatSO` (`IndexedAsset`) — 스탯 하나를 정의하는 SO. `int Index`로 식별
- `StatOverride` — Inspector에서 기본값 오버라이드 가능한 래퍼
- `AgentStat` — `StatOverride[]`를 받아 Clone된 개별 StatSO 인스턴스 딕셔너리로 관리
- 스탯 조회: `GetStat(assetIndex)`, 구독: `SubscribeStat(index, handler, defaultVal)`
- 새 스탯 추가: `StatSO` 에셋 생성 → 고유 `Index` 부여 → `AgentStat.statOverrides`에 등록

### 에셋 인덱싱 (`System/`)

- `IndexedAsset` — `int Index` 필드를 가진 SO 기반 클래스. 스탯, 스킬 등 모든 데이터 SO가 상속
- `AssetTableSO` — `IndexedAsset[]`을 묶어 테이블로 관리
- `AssetNameSO` — 에셋 이름 상수 관리용 (VFX 등 이름 기반 풀 접근 시 사용)

### 이벤트 채널 (`JWLib/EventChannelSystem/`)

ScriptableObject 기반 타입 안전 이벤트 버스. 씬 간 의존성 없이 통신 가능.

```csharp
channel.AddListener<MyEvent>(handler);
channel.RemoveListener<MyEvent>(handler);
channel.RaiseEvent(new MyEvent { ... });
```

- `GameEvent` 상속으로 새 이벤트 타입 정의
- `EventChannelSO`는 `[CreateAssetMenu]` SO이므로 Project 창에서 에셋으로 생성 후 Inspector에서 주입

### 의존성 주입 (`JWLib/DISystems/`)

리플렉션 기반 DI. `DependencyInjector` MonoBehaviour가 씬 내 모든 Provider/Injectable을 Awake 시 처리.

- `[Provide]` — 의존성 제공. 메서드 또는 클래스에 부착
- `[Inject]` — 의존성 주입 요청. 필드 또는 메서드에 부착
- `IDependencyProvider` 구현 클래스가 Provider 역할
- `DefaultExecutionOrder(-10)` 설정으로 다른 컴포넌트보다 먼저 실행됨

### 오브젝트 풀 (`JWLib/ObjectPool/`)

- `IPoolable` — 풀 대상 컴포넌트가 구현하는 인터페이스. `ResetItem()` 호출로 상태 초기화
- `Pool` — 내부 Stack 기반 풀. `Pop()` 시 비어 있으면 자동 Instantiate
- `PoolManagerSO` — 에셋으로 풀 설정을 관리. `PoolInitializer`가 씬 로드 시 초기화
- VFX 이펙트 등 반복 생성 객체에 사용; `PoolableVFX` / `AbstractMonoPoolable` 상속으로 추가

### 입력 버퍼 (`System/Core/InputBuffer`)

- 싱글톤(`InputBuffer.Instance`). 기본 유효 시간 0.15초(`bufferWindow`)
- `Register(action)` → `Consume(action)` (소비) / `Peek(action)` (비소비 확인)
- `InputBufferAction` enum에 버퍼링할 액션을 추가해 사용

### 이펙트 시스템 (`System/EffectSystem/`)

- `IPlayableVFX` — 이펙트 재생 인터페이스
- `PoolableVFX` — 풀에서 꺼낸 VFX. 재생 완료 후 자동 풀 반납
- `PlayParticleVfx` / `PlayGraphVfx` — ParticleSystem / VFX Graph 구현체

### 제스처 인식 (`GestureScripts/`)

$P Point-Cloud 알고리즘 기반 터치/마우스 제스처 인식.

- `GestureDataSO` / `GestureDataListSO` — 제스처 데이터를 SO로 저장
- `PointCloudRecognizer` — 인식 알고리즘 코어

### 애니메이션 파라미터 (`JWLib/AnimationSystem/`)

- `AnimParamSO` — Animator 파라미터 이름과 Hash를 SO로 관리. `ParamHash`로 접근
- 하드코딩된 파라미터 문자열 대신 `AnimParamSO` 에셋 사용

## 어셈블리 구조

`Scripts/` 폴더(Agents, Player, Enemy, System 등)는 별도 `.asmdef` 없이 기본 `Assembly-CSharp`을 사용한다.

| 어셈블리 | 경로 |
|---|---|
| `DI_System_Assembly` | `JWLib/DISystems/` |
| `EventChannel_Assembly` | `JWLib/EventChannelSystem/` |
| `ObjectPool.Runtime.Assembly` | `JWLib/ObjectPool/Runtime/` |
| `ObjectPool.Editor.Assembly` | `JWLib/ObjectPool/Editor/` |

## 씬 및 에셋 경로

- `Work Scene.unity` — 작업용 메인 씬
- `SampleScene.unity` — 샘플/테스트 씬
- `Assets/!!.GameModules/` — 런타임에 사용되는 SO 에셋 모음 (AnimParamSO, StatSO, FSM 상태, 이벤트 채널 등)

## 알려진 패턴 및 주의사항

### Animation Event 의존성
`EndTrigger()`, `DrawEnd()`, `SheatheEnd()` 등은 Unity Animator의 Animation Event로 호출된다. 애니메이션이 `CrossFadeInFixedTime`으로 중단되면 이벤트가 발화되지 않아 `CanManualMove = false` 고착이 발생할 수 있다.

### 새 스킬 작성 체크리스트
1. `AbstractPlayerSkill` 상속
2. `Initialize()` 오버라이드 시 `base.Initialize(module)` 호출
3. `StopSkill()` — `_moveData.CanManualMove = true`를 첫 줄에 작성
4. `StopSkill()` — 구독한 모든 이벤트(`_trigger.OnAnimationEnd`, `_sensor.OnOnDetectEnd` 등) 해제
5. `SkillDataSO` 에셋 생성 → 고유 `skillIndex` 부여 → `SkillTable`에 등록
6. Player GameObject의 Skills 하위에 컴포넌트로 부착

### `groundMask` 설정
`AgentMoveData.groundMask`는 지면 전용 레이어만 포함해야 한다. `-1`(전체 레이어) 사용 시 적 위에 올라갔다 내려올 때 비정상 지면 감지가 발생한다.

## Agent skills

### Issue tracker

이슈와 PRD는 `.scratch/<기능>/` 아래 로컬 마크다운 파일로 관리한다. See `docs/agents/issue-tracker.md`.

### Triage labels

다섯 가지 표준 트리아지 역할(`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`)을 기본 문자열 그대로 사용한다. See `docs/agents/triage-labels.md`.

### Domain docs

단일 컨텍스트(single-context) — 루트의 `CONTEXT.md` + `docs/adr/`. See `docs/agents/domain.md`.
