# 지역 전환·전체 지도·실시간 미니맵 설계

## 1. 목표와 확정 방향

현재 `90_GameShell`의 세계조정국 연구실 탐험 구조를 다음 단계로 확장한다.

```text
90_GameShell (플레이어·카메라·HUD·입력·서비스가 유지되는 영속 셸)
├─ 91_LabInterior (세계조정국 연구실 내부, Additive)
├─ 92_BureauCourtyard (세계조정국 청사 외부, Additive)
└─ 이후 지역 씬 (Additive)
```

- 건물 내부와 외부는 각각 독립된 지역 씬으로 관리한다.
- 지역의 정적 시각 표현은 고정 쿼터뷰 이미지 원화가 담당하고, 이동 가능 여부는 보이지 않는 Collider·Trigger가 담당한다.
- 지역 사이 이동은 문·출입구·게이트의 `AreaPortal`을 통해 수행한다.
- `90_GameShell`은 Single로 한 번만 로드하고 지역 씬만 Additive로 교체한다.
- `M`을 누르면 현재 지역의 전체 지도를 연다.
- 좌측 상단 현재 위치 HUD 바로 아래에 실시간 미니맵을 둔다.
- 미니맵은 북쪽 고정(North-up)이며 플레이어 화살표만 마지막 바라보는 방향으로 회전한다.
- 전투, 빠른 이동, 경로 탐색, 안개 걷기, 월드맵 전체 지역 선택은 이번 범위가 아니다.

초기 구현 지역 ID는 다음과 같이 고정한다. 안정 ID는 저장 데이터와 테스트에서 사용하므로 출시 후 이름을 바꾸지 않는다.

| `areaId` | 씬 | 표시명 | 종류 |
|---|---|---|---|
| `world_adjustment_lab_interior` | `91_LabInterior` | 세계조정국 연구실 | 실내 |
| `bureau_courtyard` | `92_BureauCourtyard` | 세계조정국 청사 외곽 | 실외 |

## 2. 씬 수명주기

### 2.1 영속 셸

`90_GameShell`이 소유하는 항목:

- `GameShellRoot`
- 플레이어 논리 루트, `CharacterController`, 모델과 Animator
- `ExplorationInputReader`, `PlayerMotor`, `PlayerFacing`
- 게임 카메라와 `QuarterViewCameraRig`
- 상호작용 센서
- HUD, 대화 UI, ESC 메뉴, 지도 오버레이, 로딩 페이드
- `AreaTransitionCoordinator`, 현재 지역 상태, 지도 상태

지역 씬이 소유하는 항목:

- 바닥·벽·건물·실외 지형과 Collider
- 지역 조명과 환경 효과
- NPC와 조사 대상
- 문과 `AreaPortal`
- `AreaSpawnPoint`, `LocationVolume`, `MapFloorVolume`
- 지역별 `AreaRoot` 하나

`AppRoot` 외에는 `DontDestroyOnLoad`를 추가하지 않는다. `90_GameShell` 자체가 로드된 상태를 유지하므로 플레이어와 HUD는 별도 전역 오브젝트가 될 필요가 없다.

### 2.2 초기 진입

```text
세이브 로드
→ IEntryPointResolver
→ EntryDestination(shellSceneKey, areaId, spawnId)
→ 90_GameShell Single 로드
→ GameShellRoot 기본 서비스 초기화
→ 대상 지역 Additive 로드
→ AreaRoot/areaId/spawnId 검증
→ 플레이어 배치
→ Physics.SyncTransforms + 카메라 SnapImmediate
→ 탐험 입력 활성화
```

초기 지역을 찾을 수 없거나 `AreaRoot`가 중복되거나 스폰이 없으면 임의 위치로 대체하지 않는다. 초기화를 실패시키고 프런트엔드의 복구 가능한 오류 흐름으로 돌아간다.

### 2.3 런타임 지역 전환

지역 전환 상태는 다음과 같다.

```text
Idle → FadingOut → Loading → Validating → Positioning → UnloadingPrevious → FadingIn → Idle
                                      └─ 실패 → RollingBack → Idle
```

전환 규칙:

1. `AreaPortal`은 `InteractionSensor`의 기존 F 상호작용을 재사용한다.
2. 전환 요청과 동시에 중복 요청을 거부하고 이동·대시·상호작용·카메라·대화 입력을 Gate 토큰으로 잠근다.
3. 페이드는 `unscaledDeltaTime`을 사용하며 권장 시간은 out `0.25초`, in `0.35초`다.
4. 다음 지역을 먼저 Additive로 로드하고 완전 검증한 뒤 이전 지역을 언로드한다.
5. 플레이어 배치 때 `CharacterController`를 잠시 끄고 Pose 설정 후 다시 켠다.
6. `PlayerMotor.ResetMotion`, `Physics.SyncTransforms`, `QuarterViewCameraRig.SnapImmediate`를 호출한다.
7. 성공한 뒤에만 현재 `areaId`와 안전한 `spawnId`를 갱신한다.
8. 실패하면 새로 로드한 후보 씬만 언로드하고 이전 지역·플레이어 위치·입력 상태를 복구한 뒤 사용자용 알림을 표시한다.
9. 모든 성공·실패·비활성화·파괴 경로에서 Gate 토큰과 페이드가 정확히 한 번 복구되어야 한다.

같은 지역 안의 일반 문은 기존 `LabDoorController`를 사용한다. Additive 씬 전환은 다른 `areaId`로 이동할 때만 수행한다.

## 3. 데이터 계약

### 3.1 AreaDefinition

지역별 ScriptableObject 또는 동등한 명시적 데이터는 다음을 가진다.

```text
AreaDefinition
  areaId              # 소문자 stable ID
  sceneKey            # Build Settings의 실제 씬 키
  displayNameKey      # 최종 현지화 교체 경계
  fallbackDisplayName # 현재 한국어 표시명
  areaKind             # Interior | Exterior
  defaultSpawnId
  mapDefinition
```

`AreaRegistry`는 모든 `AreaDefinition`을 명시적 배열로 보유한다. `Resources.LoadAll`, 씬 이름 추론, `GameObject.Find`, Service Locator를 사용하지 않는다. `areaId`와 `sceneKey`의 중복은 검증 단계에서 실패시킨다.

### 3.2 AreaRoot와 SpawnPoint

각 지역 씬에는 활성 `AreaRoot`가 정확히 하나 있어야 한다.

```text
AreaRoot
  areaDefinition
  spawnPoints[]
  portals[]
  locationVolumes[]
  floorVolumes[]
```

`AreaSpawnPoint`는 `spawnId`, Pose, `floorId`를 가진다. `spawnId`는 지역 내부에서 고유하며 저장 데이터에 쓰일 수 있으므로 이름 변경 금지다.

초기 스폰 ID:

| 지역 | `spawnId` | 용도 |
|---|---|---|
| 연구실 내부 | `reception_start` | 신규/기존 세이브 기본 진입 |
| 연구실 내부 | `courtyard_entrance` | 외부에서 안으로 복귀 |
| 청사 외곽 | `lab_exit` | 연구실에서 밖으로 나옴 |

### 3.3 AreaPortal

```text
AreaPortal
  portalId
  interactionLabel
  targetAreaId
  targetSpawnId
  accessPolicy
  confirmationPolicy  # 기본 None
```

- 출입구는 기본적으로 F 상호작용 방식이다.
- 잠긴 포털은 기존 잠긴 문과 동일하게 알림만 표시하고 지역 전환을 요청하지 않는다.
- 목적지 `areaId` 또는 `spawnId`가 유효하지 않으면 에디터 검증과 런타임 전환 모두 실패한다.
- 양방향 연결은 각 씬에 독립 포털 두 개를 두되, 검증기가 역방향 대상 존재 여부를 검사한다.

## 4. 세이브 위치 확장

현재 `entryId/checkpointId`는 이야기 진입점과 진행 체크포인트다. 이를 지역·스폰 위치로 오용하지 않는다. 정확한 지역 재개를 위해 저장 스키마를 v3으로 올린다.

```json
{
  "progress": {
    "entryId": "prologue_start",
    "checkpointId": "start",
    "areaId": "world_adjustment_lab_interior",
    "spawnId": "reception_start",
    "playTimeSeconds": 0
  }
}
```

- `entryId`: 전체 게임 흐름의 진입 종류
- `checkpointId`: 이야기/진행 체크포인트
- `areaId`: 재개할 지역
- `spawnId`: 재개할 안전한 스폰

임의 월드 좌표·회전·씬 이름은 저장하지 않는다. 현재 지역에 도착하면 런타임 위치 상태만 갱신하고, 수동 저장 또는 승인된 자동 저장 시 안정 ID를 파일에 기록한다.

v2 → v3 마이그레이션:

- 모든 기존 연구실 체크포인트는 `areaId=world_adjustment_lab_interior`로 변환한다.
- 기존 `checkpointId=start`는 `spawnId=reception_start`로 변환한다.
- 기존 진행 체크포인트도 안전한 시작 위치 `reception_start`로 변환한다. 진행 상태는 `checkpointId`에 그대로 유지한다.
- 원본 v2 파일을 즉시 덮어쓰지 않고 기존 마이그레이션 정책과 체크섬 검증을 유지한다.
- 알 수 없는 `areaId/spawnId`는 기본값으로 조용히 대체하지 않고 명시적 로드 실패로 처리한다.

## 5. 입력과 UI 상태 우선순위

`ExplorationInputReader`에 Map 액션을 추가한다.

| 입력 | 동작 |
|---|---|
| Keyboard M | 현재 지역 지도 열기/닫기 |
| Gamepad Select | 현재 지역 지도 열기/닫기 |
| Escape / Gamepad B | 열린 지도를 닫기 |
| Gamepad Start | 기존 ESC 일시정지 메뉴 |

지도 입력은 별도 one-shot으로 소비하고 화면 진입·종료 시 pending 값을 비운다.

우선순위:

```text
대화 활성          → M 무시, Escape는 대화만 종료
지역 전환 중       → M/Escape/Start 무시
전체 지도 활성     → M/Escape/B로 지도만 닫기
ESC 하위 메뉴 활성 → 기존 Back 규칙
ESC 루트 활성      → 기존 메뉴 닫기
일반 탐험          → M은 지도, Escape/Start는 메뉴
```

전체 지도는 탐험을 일시정지한다. 열기 전 `Time.timeScale`을 보관하고 `0`으로 설정하며 닫을 때 원래 값을 복구한다. 기존 ESC 메뉴와 공통 `InGameUiCoordinator`가 상태와 Gate 토큰을 소유해야 하며, 지도 View가 직접 timeScale이나 입력을 조작하면 안 된다.

## 6. 실시간 미니맵

### 6.1 UX

- 좌측 상단 현재 위치 텍스트 아래에 배치한다.
- 1920×1080 기준 권장 크기 `300×210`, 최소 해상도에서는 `240×168`까지 축소한다.
- Safe Area 왼쪽·위 여백은 기존 위치 HUD와 동일하게 `32~48px`을 사용한다.
- 북쪽 고정이며 우측 상단에 작은 `N` 표식을 표시한다.
- 플레이어는 중앙 부근의 `Secondary #5CAECC` 화살표로 표시하고 마지막 바라보는 방향으로 회전한다.
- 현재 층의 지도만 표시한다. 층이 바뀌면 배경과 층 라벨을 즉시 교체한다.
- 대화와 ESC 메뉴 중에는 뒤에 유지하되 딤 아래에 놓인다.
- 실내/실외 모두 동일한 View를 사용하고 데이터만 교체한다.
- HUD 요소는 모두 `raycastTarget=false`여야 한다.

### 6.2 지도 투영

별도 탑다운 카메라와 RenderTexture를 만들지 않는다. 각 층의 정적 지도 이미지와 월드 좌표 투영 데이터를 사용한다.

```text
MapFloorDefinition
  floorId
  displayName
  backgroundSprite
  worldOrigin
  worldAxisX
  worldAxisY        # 쿼터뷰 월드의 보통 XZ 평면
  worldSize
  minimapViewportWorldSize
```

월드 위치를 정규화 좌표로 변환한다.

```text
u = dot(worldPosition - origin, normalizedAxisX) / worldSize.x
v = dot(worldPosition - origin, normalizedAxisY) / worldSize.y
```

`MapProjection`은 Unity UI와 분리 가능한 순수 계산으로 작성하고 다음을 처리한다.

- 월드 → 정규화 좌표
- 전체 지도 RectTransform상의 마커 좌표
- 미니맵 `uvRect` 또는 배경 오프셋
- 지도 가장자리에서의 clamp
- 회전된 축을 가진 지역
- 0 또는 음수 크기 거부

미니맵 갱신은 매 프레임 가능하지만 배열·문자열·LINQ 할당을 만들지 않는다. 플레이어 위치, 방향 또는 층이 변했을 때만 실제 UI 속성을 갱신한다.

## 7. M 전체 지도

- 현재 지역과 현재 층의 지도를 중앙 대형 패널로 표시한다.
- 헤더: 지역명, 현재 방, 층 이름.
- 현재 위치 화살표와 `현재 위치` 범례를 표시한다.
- 실내는 방 윤곽과 출입구, 실외는 통행 가능한 구역과 건물 출입구를 표시한다.
- 마우스 휠과 패드 shoulder로 제한된 확대·축소를 제공한다. 초기 구현에서는 드래그 이동이 없어도 된다.
- 다층 지역은 `Q/E` 또는 패드 shoulder로 층을 바꿀 수 있지만, 열 때는 항상 현재 층을 선택한다.
- 현재 층이 아닌 층을 보는 동안 플레이어 마커를 숨기고 `현재 위치: <층 이름>`을 표시한다.
- M/Escape/B로 닫고 이전 탐험 상태를 정확히 복구한다.
- 지도 View는 씬 로드, 세이브, 입력 Gate를 직접 수행하지 않고 렌더링과 사용자 이벤트 전달만 담당한다.

초기 범위에서 표시하는 아이콘은 플레이어, 현재 위치, 지역 출입구다. NPC, 퀘스트, 상점, 빠른 이동 아이콘은 확장 슬롯만 두고 가짜 데이터를 표시하지 않는다.

## 8. 기존 위치 HUD 통합

현재 `LocationTracker`와 `LocationVolume`의 우선순위 규칙을 유지하되 상태에 stable ID와 `floorId`를 추가한다.

```text
CurrentLocation
  areaId
  areaDisplayName
  roomId
  roomDisplayName
  floorId
```

- `InGameHudState`에 지도 렌더링용 Unity 오브젝트를 넣지 않는다.
- 지역 전환 성공 직후 새 지역 기본 위치를 먼저 게시해 빈 위치가 깜빡이지 않게 한다.
- 방 Trigger가 겹치면 기존 priority → stable ID ordinal 규칙을 유지한다.
- `LocationTracker`가 `AreaRoot` 교체 시 이전 씬의 파괴된 Volume 참조를 정리한다.
- 표시 문자열은 위치 변경 이벤트에서만 갱신하고 매 프레임 다시 만들지 않는다.

## 9. 책임 구조

```text
GameShellRoot
├─ AreaRegistry
├─ AreaTransitionCoordinator
│  ├─ IAreaSceneLoader
│  ├─ ExplorationInputGate
│  ├─ ScreenFadeView
│  └─ ExplorationLocationState
├─ MapCoordinator
│  ├─ AreaMapView (M 전체 지도)
│  ├─ MiniMapView
│  ├─ MapProjection
│  └─ LocationTracker
├─ InGameUiCoordinator
├─ Player/Camera/Interaction
└─ HUD/Dialogue/Pause
```

- `AreaTransitionCoordinator`: 전환 상태와 롤백의 단일 소유자
- `IAreaSceneLoader`: Additive load/unload의 테스트 가능한 경계
- `ExplorationLocationState`: 현재 area/spawn/floor의 런타임 읽기 모델
- `MapCoordinator`: M 입력 우선순위, 지도 열기/닫기, 현재 MapDefinition 선택
- `MiniMapView`, `AreaMapView`: 렌더링과 이벤트 전달만 담당
- `AreaRegistry`: 안정 ID에서 지역 데이터로의 결정적 해석

## 10. 시각 규칙

기존 `AGENTS.md` 토큰을 그대로 사용한다.

- 미니맵 배경: `Surface #171C22`, 알파 `0.88~0.94`
- 테두리·북쪽 표식: `AccentGold #B99A59`
- 플레이어/현재 위치: `Secondary #5CAECC`
- 잠긴 출입구: `AccentRed #7D1827`과 자물쇠 모양을 함께 사용
- 기본 지도 선: `TextMuted #9AA6AF`
- 방명·지역명: `TextMain #EEE6D5`
- 전체 지도 딤: 검정 알파 `0.72`

실내 지도는 행정 도면·청사진 느낌, 실외 지도는 어두운 도시 평면도 느낌으로 통일한다. 기능 구현 단계에서는 단순한 도형 기반 지도 이미지를 사용할 수 있으며 최종 지도 원화는 별도 아트 작업으로 교체한다.

## 11. 검증 기준

- `90_GameShell`은 지역을 바꿔도 다시 로드되지 않는다.
- 실내 → 외부 → 실내 왕복 후 플레이어·카메라·HUD·메뉴가 한 개씩만 존재한다.
- 전환 중 버튼 연타가 중복 씬 로드나 중복 스폰을 만들지 않는다.
- 실패한 전환은 이전 지역과 플레이어 상태를 유지한다.
- M 지도가 대화·ESC 메뉴·지역 전환과 입력 경쟁을 일으키지 않는다.
- 미니맵이 실시간 위치·방향·층을 정확히 표시한다.
- 탐험 중 Q/E가 고정 카메라 투영을 바꾸지 않고 미니맵은 계속 북쪽 고정이다.
- 1280×720, 1920×1080, 2560×1440, 3440×1440에서 Safe Area를 벗어나지 않는다.
- v2 세이브가 v3 런타임 객체로 마이그레이션되고 기존 체크포인트가 유지된다.
- 알 수 없는 지역·스폰·중복 stable ID는 조용히 대체되지 않는다.
- 컴파일 오류, 콘솔 예외, Missing Script/Reference가 0개다.
