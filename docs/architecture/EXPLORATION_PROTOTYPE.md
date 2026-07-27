# 쿼터뷰 탐색 이동 프로토타입

## 목적과 범위

`90_GameShell`은 세이브 진입 확인용 화면을 넘어, 정사영 쿼터뷰 3D 공간을 직접 이동하고 NPC와 상호작용하는 첫 게임플레이 프로토타입이다. 전투, 인벤토리, 퀘스트, 점프와 전투 전용 씬은 이 단계에 포함하지 않는다.

```text
00_Boot
→ 10_Frontend에서 새 게임 생성 또는 세이브 로드
→ IEntryPointResolver
→ EntryDestination(sceneKey="90_GameShell", areaId, spawnId)
→ UnitySceneFlowService가 활성 씬의 GameShellRoot 하나를 초기화
→ 90은 persistent shell로 유지하고, areaId 지역 씬을 additive 로드
→ area의 spawnId SpawnPoint 배치와 카메라 snap
→ 탐색 입력 활성화
```

알 수 없는 씬 또는 스폰 키, 활성 세이브 누락, 중복 composition root와 직렬화 참조 누락은 기본 위치로 대체하지 않는다. 초기화를 실패시키고 탐색 입력을 계속 비활성 상태로 둔다.

## 런타임 책임

| 구성 요소 | 책임 |
|---|---|
| `GameShellRoot` | persistent shell composition root, 세션·목적지 검증, 지역 초기화·컴포넌트 연결과 입력 활성화 |
| `AreaTransitionCoordinator` | 중복 전환을 거부하고 입력/상호작용을 잠근 뒤 fade-out → additive 로드 → AreaRoot 검증 → spawn 배치 → 이전 지역 해제 → fade-in을 직렬 수행 |
| `AreaRegistry` / `AreaDefinition` | 안정적인 areaId, 지역 씬 키, 기본 spawn, 지도 정의를 보관하는 ScriptableObject 레지스트리 |
| `AreaRoot` | 지역이 소유한 spawn·portal·room/floor volume·카메라 zone 및 지역 콘텐츠 바인딩의 경계 |
| `AreaPortal` | F 상호작용으로 targetAreaId/targetSpawnId 전환을 요청하는 안정 ID 포털 |
| `LocationTracker` | area/room/floor의 안정 ID를 HUD·저장·지도에 발행하는 단일 위치 원천 |
| `MapCoordinator` | M 전체 지도와 상시 미니맵을 갱신하며 지도와 pause가 동시에 열리지 않도록 소유권을 조정 |
| `SpawnPoint` | ordinal 비교를 사용하는 안정적인 `spawnKey`와 시작 Pose 제공 |
| `ExplorationInputReader` | Exploration InputActionMap 수명주기와 모든 키보드·마우스 입력의 단일 진입점 |
| `ExplorationInputGate` | 이동·대시·상호작용·카메라 채널별 소유권 토큰 잠금 |
| `PlayerMotor` | CharacterController 걷기·달리기·중력·거리 예산형 대시·충돌 |
| `PlayerFacing` | 마지막 유효 이동을 45도 단위 8방향으로 유지하고 별도 visual root에 표현 |
| `QuarterViewCameraRig` | 정사영 추적, 정수 quarter index 회전, 줌, zone 및 대화 override |
| `CameraZone` | 플레이어 Collider만 감지하는 BoxCollider trigger와 카메라 profile 수명주기 |
| `InteractionSensor` | 고정 버퍼 후보 수집, 반경·cone·LOS 검사, 현재 대상 하나 선택 |
| `PrototypeInteractable` | 표시명, 행동명, focus/marker/대화 anchor와 임시 대사 제공 |
| `InteractionPromptView` | 선택 marker와 `F 대화`/`F 조사` UGUI 표시 |
| `DialogueFocusController` | 대화 잠금, 상호 facing, 대화 카메라와 UI, 모든 종료 경로 복원 |

Domain/Application에는 새 UnityEngine 의존성을 추가하지 않았다. 물리·입력·카메라 결합 코드는 `Presentation/Exploration`에 한정한다.

## 입력과 기본 수치

| 입력 | 동작 |
|---|---|
| WASD | 현재 카메라의 XZ forward/right 기준 자유 이동 |
| Left Shift hold | 달리기 |
| Space press | 현재 이동 방향, 입력이 없으면 마지막 facing 방향으로 대시 |
| Q / E press | 정확한 90도 단위 카메라 좌·우 회전 |
| Mouse Wheel | 정사영 확대·축소 |
| M press / Gamepad Select | 전체 지도 열기·닫기 |
| Q / E / shoulder | 전체 지도에서 floor 전환 (2층 이상 지역만) |
| F press | 현재 선택 대상과 상호작용, 대화 중 다음 줄 |
| Enter | 대화 다음 줄 |
| Escape | 대화 종료 |

Inspector 기본값은 걷기 `3.0m/s`, 달리기 `5.5m/s`, 대시 `3.0m / 0.18s / cooldown 0.65s`, slope limit `45°`, step offset `0.3m`다. 카메라는 yaw `45°`, pitch 약 `35°`, 회전 전환 약 `0.25s`, orthographic size `8`, 제한 `6~12`, 휠 줌 감도 `0.03`을 사용한다. 상호작용 반경은 `2.2m`, 전체 전방 cone은 약 `100°`다.

## 결정적 선택 정책

상호작용 후보는 활성/가능 여부, 반경, 마지막 facing 기준 전방 cone, LOS 순으로 거른다. 남은 후보는 정렬된 alignment 내림차순, 거리 제곱 오름차순, stable ID ordinal 오름차순으로 하나만 선택한다.

CameraZone이 겹치면 다음 순서를 적용한다.

1. 높은 `priority`
2. 같은 priority면 가장 최근에 진입한 zone
3. 진입 순서도 같으면 `stableId` ordinal 오름차순

zone 안의 Q/E quarter index와 zoom offset은 허용된다. 퇴장하면 이전 기본 profile로 복귀한다. 대화 override는 현재 카메라 상태 위에 임시로 적용하며, handle 해제 후 zone/기본 profile과 quarter/zoom 상태로 돌아간다.

## 씬과 authoring

`90_GameShell.unity`는 persistent shell이며 플레이어, 카메라, HUD, Dialogue/UI와 area transition/map coordinator만 소유한다. 실제 월드 콘텐츠는 다음 additive 지역 씬에 있다.

| 지역 씬 | areaId | 기본 진입 | 현재 역할 |
|---|---|---|---|
| `91_LabInterior` | `world-adjustment-lab-interior` | `reception-start` | 세계조정국 연구실 내부와 대화/잠긴 문 |
| `92_BureauCourtyard` | `bureau-courtyard` | `lab-exit` | 중앙 청사 앞마당 blockout 및 연구실 귀환 포털 |

각 지역은 정확히 하나의 `AreaRoot`를 가지며 런타임 절차 생성에 의존하지 않는다. `DemonLord/Exploration/Build Area Transition And Map System`은 `__AreaSystem...Generated` 소유 루트와 지도 UI만 idempotent하게 재생성한다.

연구실 지역에는 다음 콘텐츠가 포함된다.

- `GameShellRoot`, `start` SpawnPoint, CharacterController 플레이어
- 정사영 카메라와 Directional Light
- 평지, 외벽/모서리, 대시 충돌 벽, 완만한 경사와 플랫폼, 5단 짧은 계단
- NPC 2명, 조사 대상 1개, CameraZone 1개
- Interaction Prompt와 placeholder Dialogue Canvas

`DemonLord/Exploration/Build Prototype Scene`은 `__ExplorationPrototypeGenerated` 범위만 다시 만들고 프리팹·머티리얼·씬을 저장하는 idempotent authoring 명령이다. `Validate Prototype Scene`은 composition root·스폰·카메라·Missing Script·상호작용 대상·zone 구성을 검사한다. 런타임에서는 테스트 월드를 절차 생성하지 않는다.

## 최종 아트 교체 경계

- 3D 모델: `PrototypePlayer`/NPC의 `VisualRoot` 하위 placeholder renderer를 모델과 Animator로 바꾼다. `PlayerMotor`, CharacterController와 논리 루트는 유지한다.
- 8방향 2D: `PlayerFacing.CurrentDirection`을 읽는 sprite presenter를 `VisualRoot`에 추가하고 8방향 sprite/animation을 선택한다. 이동·카메라 코드는 수정하지 않는다.
- 2.5D: 3D 지형·Collider·CameraRig를 유지하고 visual adapter만 billboard/8방향 sprite 방식으로 교체한다.
- 최종 프롬프트·대화 UI: `InteractionPromptView`와 `DialogueFocusController`가 가진 View 참조를 교체하되 선택·잠금 계약은 유지한다.

## 검증

- 순수 계산과 상태 규칙: `Assets/_Project/Tests/EditMode`
- 직렬화 씬, 이동/충돌/경사·계단, 카메라, zone, 상호작용·대화: `Assets/_Project/Tests/PlayMode/ExplorationPrototypePlayModeTests.cs`
- Unity 실행 메뉴: `DemonLord/Exploration/Run PlayMode Tests`
- 결과 XML: `Application.persistentDataPath/ExplorationPlayModeResults.xml`

PlayMode 테스트 러너는 수동 플레이를 위한 Boot 시작 씬 override를 테스트 동안만 해제하고, 도메인 리로드 이후 callback을 재등록한 뒤 종료 시 원래 설정을 복원한다.

## 세계조정국 연구실 세로 슬라이스

`__WorldAdjustmentLabGenerated`는 연구실 지역의 복제 원본으로만 남고, 실제 플레이는 `91_LabInterior`의 area 전용 생성 루트에서 이루어진다. 빌더는 자신이 소유한 루트만 재생성하며, 이전 `__ExplorationPrototypeGenerated`와 수동 배치 오브젝트는 삭제하지 않는다.

맵은 중앙 접수실을 허브로 사용한다. 세무관 업무실, 세계선 분석실, 기록 보관실, 격리 연구실이 복도와 실제 문으로 연결된다. 업무실의 장부와 분석실의 장치는 조사할 수 있으며, 분석실에는 `worldline-researcher` 안정 ID를 가진 세계선 분석 연구원이 있다. 격리 연구실 문은 기본 잠김이며 `접근 권한이 없습니다. 문이 잠겨 있습니다.` 토스트만 표시하고 이동을 잠그지 않는다.

### 추가 런타임 책임

| 구성 요소 | 책임 |
|---|---|
| `DirectionalAnimationSet` | Idle/Walk/Run/Dash 각각 8방향 클립과 FPS·루프 여부를 직렬화 |
| `DirectionalSpritePresenter` | 월드 facing을 현재 카메라 yaw 기준 스프라이트 방향으로 바꾸고 billboard 표시 |
| `LabDoorController` | Closed/Opening/Open/Closing/Locked 상태, 0.45초 문 애니메이션, 통행 collider와 끼임 방지 |
| `DoorAccessPolicy` | 향후 진행 플래그로 대체 가능한 잠금 판정과 잠김 메시지 |
| `DialogueSequence` | 화자 ID·표시명·초상화·화자 측·본문을 가진 대화 데이터 |
| `DialogueView` | 좌측 세무관/우측 상대 초상화, 화자 강조, 이름·본문·계속 안내 표시 |
| `DialogueTheme` | 대화 폰트·색·크기를 보관하는 교체 가능한 ScriptableObject |
| `NotificationView` | 대화와 분리된 1.8초 잠금/일반 토스트 |
| `WorldAdjustmentLabSceneBuilder` | 연구실의 프리팹, ScriptableObject, 재질, 전용 루트 생성과 검증 |

### 1회성 입력 소비 정책

`Confirm`, `Cancel`, `Interact`, `Dash`는 `ExplorationInputReader`가 한 번만 소비하는 edge 입력이다. 이동·달리기·줌은 held/value 입력으로 유지한다. 대화는 시작 직전에 이미 남아 있던 `Confirm`/`Interact`/`Cancel`을 비워 시작 F 또는 이전 Escape가 첫 줄 진행·즉시 종료로 번지지 않게 한다. 대화 중의 새 F/Enter는 한 줄만 진행하고, 새 Escape만 현재 대화를 종료한다. 대화 UI, CanvasGroup, NPC 또는 컨트롤러가 비활성화·파괴되면 종료 경로는 같은 disposable 입력 잠금·카메라 override handle을 정확히 한 번 해제한다.

### 아트 교체 가이드

- 세무관의 임시 32방향 상태 이미지는 `Assets/_Project/Art/Characters/TaxOfficer/Placeholder/`에 있다. 같은 파일명·방향 순서를 유지하거나 `TaxOfficerPlaceholderDirectionalAnimationSet.asset`의 Sprite 참조만 교체하면 코드 변경 없이 새 원화를 연결할 수 있다.
- 세무관과 연구원 기본 초상화는 각각 `Assets/_Project/Art/Characters/TaxOfficer/Portraits/`와 `Assets/_Project/Art/Characters/WorldlineResearcher/Portraits/`에 있다. 표정 변형을 추가할 때는 `DialogueSequence`의 participant/line 데이터에 참조를 확장하고, 화자 이름 문자열로 이미지를 추론하지 않는다.
- 연구실 블록아웃 재질과 세계조정국 표식은 `Assets/_Project/Art/Prototype/WorldAdjustmentLab/` 아래에 있다. `TextureKit/`에는 슬레이트·황동/현무암·청색/버건디 대리석 바닥, 석재·황동/기록보관실 벽돌/격리실 금속 벽, 일반/기록실 양문/격리실 보안문, 세 종류의 계단 마감 원본을 둔다. 빌더가 이 원본에서 반복·클램프 임포트 설정과 재질 참조를 구성하므로 파일명은 유지한다. 최종 모델·타일·데칼로 바꿀 때에도 `CharacterController`, 벽/문/가구의 collider, 카메라 zone과 빌더의 전용 루트 계약은 유지한다.
- 대화 패널의 글꼴·색·크기는 `Assets/_Project/ScriptableObjects/Exploration/WorldAdjustmentLabDialogueTheme.asset`에서 바꾼다. 한국어 글리프가 들어 있는 라이선스 확인 폰트를 연결한 뒤 빌더 검증을 다시 실행한다.

### 에디터 제작과 검증

`DemonLord/Exploration/Build World Adjustment Lab`은 전용 생성 루트를 재생성하고 저장한 뒤 검증한다. `DemonLord/Exploration/Validate World Adjustment Lab`은 GameShellRoot와 start SpawnPoint 수, 5개 문과 잠긴 문, 4×8 스프라이트 세트, 대화 데이터·초상화·폰트, 활성 AudioListener/EventSystem 수를 검사한다. 생성/검증 실패는 조용히 보정하지 않고 Unity Console에 원인을 남긴다.

### 지역 전환·미니맵 규칙

- 전환 중에는 `AreaTransitionCoordinator.IsTransitioning`이 true이며, 포털·M·pause·save 입력을 즉시 거부한다. 실패하면 기존 지역과 카메라/입력 상태를 복구하고 사용자에게 재시도 가능한 알림만 표시한다.
- mini map은 현재 floor의 지도 원본과 플레이어 marker만 표시한다. 전체 지도는 현재 area의 지도, room/floor 이름, 포털 marker, 도움말을 표시하고 M/Escape로 닫는다.
- `LocationTracker`가 area 진입 시 기본 room/floor를 먼저 발행하고 이후 trigger volume 우선순위로 현재 room/floor를 갱신한다. UI 문자열은 저장하지 않고 stable ID와 definition을 통해 표시한다.
- 체크포인트 저장은 `areaId`와 `spawnId`를 함께 저장한다. 잘못된 area/spawn은 임의의 원점으로 대체하지 않고 안전한 기본 진입으로만 명시적으로 해석한다.
