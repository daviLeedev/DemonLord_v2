# 지역 확장·실내외 전환·전체 지도·실시간 미니맵 구현 프롬프트

## 1. 역할과 최종 목표

당신은 `C:\workspace\unity\DemonLord_v2`를 수정하는 시니어 Unity 클라이언트 개발자다.

현재 세계조정국 연구실 탐험 슬라이스를 다음 구조로 확장한다.

```text
90_GameShell 영속 유지
├─ 세계조정국 연구실 내부 Additive 지역
├─ 세계조정국 청사 외부 Additive 지역
├─ 문/출입구를 통한 안전한 왕복 전환
├─ M: 현재 지역 전체 지도
└─ 좌측 상단 위치 HUD 아래: 실시간 미니맵
```

완성 목표는 플레이어가 연구실 내부에서 출입구에 F로 상호작용해 청사 외곽으로 나가고, 외부에서 다시 내부로 돌아오며, 두 지역 모두에서 M 지도와 미니맵으로 현재 위치·방향·층을 확인하는 것이다.

커밋과 푸시는 하지 않는다. 전체 아트 QA는 나중에 별도로 진행하되 기능·입력·씬 수명주기·명백한 UI 잘림은 이번 작업에서 검증한다.

---

## 2. UTF-8로 끝까지 읽을 문서

구현 전에 다음 문서를 UTF-8로 끝까지 읽는다.

1. `AGENTS.md`
2. `MASTER_IMPLEMENTATION_PROMPT.md`
3. `docs/architecture/AREA_TRANSITION_AND_MAP_SYSTEM.md`
4. `docs/architecture/EXPLORATION_PROTOTYPE.md`
5. `docs/architecture/INGAME_HUD_PAUSE_MENU.md`
6. `docs/architecture/INGAME_PAUSE_CORE_ACTIONS.md`
7. `docs/architecture/BOOT_SAVE_FLOW.md`
8. `docs/architecture/SAVE_DATA.md`
9. `docs/agent/WORLD_ADJUSTMENT_LAB_VERTICAL_SLICE_PROMPT.md`
10. `docs/agent/INGAME_HUD_PAUSE_MENU_PROMPT.md`

이 작업 범위에서 충돌하면 `docs/architecture/AREA_TRANSITION_AND_MAP_SYSTEM.md`를 우선한다. 기존 사용자 변경은 보존한다.

---

## 3. 작업 전 점검

아래를 먼저 확인하고 기준선을 간단히 기록한다.

- `git status --short`
- `ProjectSettings/ProjectVersion.txt`가 `6000.4.10f1`인지 확인
- `Packages/manifest.json`
- Build Settings와 현재 `90_GameShell.unity`
- `GameShellRoot`, `UnitySceneFlowService`, `EntryPointResolver`, `EntryDestination`
- `ExplorationInputReader`, `ExplorationInputGate`, `InGameUiCoordinator`
- `LocationTracker`, `LocationVolume`, `InGameHudState`, `InGameHudView`
- `InteractionSensor`, `LabDoorController`, `QuarterViewCameraRig`, `PlayerMotor`
- `GameSave`, DTO/Mapper, 마이그레이션 파이프라인, `SaveGameProgressUseCase`
- `WorldAdjustmentLabSceneBuilder`와 기존 EditMode/PlayMode 테스트

새 패키지를 추가하지 않는다. Input System, UGUI, Test Framework와 현재 승인된 패키지만 사용한다.

---

## 4. 반드시 지킬 확정 설계

### 4.1 씬 구성

- `90_GameShell`: 플레이어, 카메라, 입력, HUD, 대화, ESC 메뉴, 지도, 지역 전환 서비스를 소유하고 Single로 한 번만 로드한다.
- `91_LabInterior`: 현재 세계조정국 연구실 환경을 옮긴 Additive 실내 지역이다.
- `92_BureauCourtyard`: 기능 검증용 청사 외부 Additive 지역이다.
- 지역 씬마다 `AreaRoot`는 정확히 하나다.
- 지역 씬 안에 별도 Canvas, EventSystem, AudioListener, 플레이어, 메인 카메라를 만들지 않는다.
- `AppRoot` 외 `DontDestroyOnLoad`를 추가하지 않는다.

초기 안정 ID:

```text
areaId: world_adjustment_lab_interior
  defaultSpawnId: reception_start
  returnSpawnId: courtyard_entrance

areaId: bureau_courtyard
  defaultSpawnId: lab_exit
```

### 4.2 전환 UX

- 지역 이동은 출입구에 접근해 F로 상호작용한다.
- 페이드 out 0.25초, in 0.35초를 기본값으로 사용한다.
- 전환 중 이동·대시·상호작용·카메라·대화 입력을 잠근다.
- 다음 지역을 로드·검증·배치한 후 이전 지역을 언로드한다.
- 전환 실패 시 이전 지역과 위치를 유지하고 `지역을 이동하지 못했습니다. 다시 시도해 주세요.`를 표시한다.
- 잠긴 출입구는 지역 로드를 시작하지 않고 기존 잠김 알림 정책을 사용한다.

### 4.3 지도 UX

- Keyboard M / Gamepad Select: 현재 지역 지도 열기·닫기.
- 지도 활성 중 Escape / Gamepad B: 지도만 닫기.
- 대화 중 M은 무시한다.
- 지역 전환 중 M, Escape, Start는 무시한다.
- 전체 지도는 게임을 일시정지하고 원래 timeScale을 보관·복구한다.
- 미니맵은 북쪽 고정이며 플레이어 화살표만 마지막 facing으로 회전한다.
- 카메라 Q/E 회전은 미니맵을 회전시키지 않는다.
- 미니맵은 현재 위치 텍스트 바로 아래 Safe Area 안에 배치한다.
- M 전체 지도는 현재 지역·현재 층을 먼저 보여준다.
- 초기 아이콘은 플레이어, 현재 위치, 지역 출입구만 사용한다. 가짜 NPC·퀘스트·상점 정보를 만들지 않는다.

### 4.4 저장

- 위치 재개를 위해 스키마를 v3으로 올린다.
- `entryId/checkpointId`와 별도로 `areaId/spawnId`를 저장한다.
- 임의 좌표·회전·씬 이름은 저장하지 않는다.
- v2 → v3 마이그레이션은 기존 checkpoint를 보존하고 연구실 내부 `reception_start`로 안전하게 변환한다.
- 알 수 없는 area/spawn을 기본값으로 조용히 대체하지 않는다.

---

## 5. 0~14단계 구현 순서

아래 단계를 순서대로 수행한다. 각 단계의 컴파일 오류를 다음 단계로 넘기지 않는다.

### 0단계: 기준선과 변경 경계 확인

- 현재 씬과 사용자 변경을 확인하고 관계없는 변경을 건드리지 않는다.
- 기존 연구실 빌더의 소유 루트와 수동 배치 오브젝트를 구분한다.
- 현재 플레이 진입, ESC 메뉴, 저장, 위치 HUD 관련 테스트를 실행 가능한 범위에서 확인한다.
- 기존 미해결 오류가 있으면 새 작업 오류와 분리해서 기록한다.

### 1단계: 순수 지역 데이터와 검증 규칙

다음 안정 ID/상태 모델을 Unity 의존 없는 계층 또는 최소 의존 계층에 만든다.

```text
AreaId
SpawnId
ExplorationLocation(areaId, spawnId)
AreaTransitionState
AreaTransitionRequest
AreaTransitionResult
```

- stable ID는 소문자 영문, 숫자, `_`, `-`만 허용하고 길이를 제한한다.
- null/빈 문자열/제어 문자를 거부한다.
- 지역 전환 상태에서 Busy 중 중복 요청을 거부한다.
- 상태 전이와 ID 검증 EditMode 테스트를 먼저 작성한다.

### 2단계: AreaDefinition과 AreaRegistry

- `AreaDefinition` ScriptableObject를 구현한다.
- `areaId`, `sceneKey`, 표시명, 종류, default spawn, map definition을 보유한다.
- `AreaRegistry`는 명시적으로 주입된 정의 배열만 사용한다.
- areaId/sceneKey 중복, null definition, default spawn 누락을 검증한다.
- `Resources.LoadAll`, 씬 이름 추론, `FindObjectOfType`, `GameObject.Find`, Service Locator를 사용하지 않는다.
- EditMode 테스트로 성공/중복/누락/알 수 없는 ID를 검증한다.

### 3단계: 영속 GameShell과 Additive 로더

- `90_GameShell`에서 지역 환경을 분리할 수 있도록 `GameShellRoot` 책임을 정리한다.
- 테스트 가능한 `IAreaSceneLoader` 경계를 만들고 Unity 구현은 `SceneManager.LoadSceneAsync(..., Additive)`와 `UnloadSceneAsync`를 캡슐화한다.
- GameShell 초기화는 셸 로드 후 목적지 지역을 Additive로 로드한 다음 탐험 입력을 활성화한다.
- 지역 씬이 완전히 준비되기 전에는 플레이어 이동을 허용하지 않는다.
- 현재 셸을 `SceneManager.LoadSceneMode.Single`로 다시 로드하지 않는다.

### 4단계: AreaRoot와 AreaSpawnPoint

- 지역 씬마다 `AreaRoot` 하나와 `AreaSpawnPoint[]`를 구현한다.
- AreaRoot의 areaId와 로드 요청 areaId가 일치해야 한다.
- spawnId 중복과 대상 spawn 누락은 실패한다.
- 배치 시 CharacterController 비활성 → Pose 설정 → 활성 → `Physics.SyncTransforms` 순서를 지킨다.
- 배치 후 `PlayerMotor.ResetMotion`, 카메라 `SnapImmediate`를 호출한다.
- 이전 지역의 파괴된 LocationVolume/상호작용 후보를 정리한다.

### 5단계: AreaTransitionCoordinator

다음 상태를 단일 코디네이터가 소유한다.

```text
Idle
FadingOut
Loading
Validating
Positioning
UnloadingPrevious
FadingIn
RollingBack
```

- 요청 즉시 중복을 거부한다.
- Gate 토큰을 한 번 얻고 모든 종료 경로에서 한 번 해제한다.
- 새 지역 검증 성공 전까지 이전 지역을 언로드하지 않는다.
- 실패 시 후보 지역만 정리하고 플레이어 원래 Pose·현재 지역 상태·카메라·입력을 복구한다.
- 비활성화/파괴/예외/Unity AsyncOperation 실패도 동일한 복구 경로를 사용한다.
- 페이드는 `unscaledDeltaTime`을 사용한다.
- 상태 전이 EditMode 테스트와 실패 복구 PlayMode 테스트를 추가한다.

### 6단계: AreaPortal과 상호작용 연결

- `AreaPortal`을 기존 `InteractionSensor`가 선택 가능한 상호작용 대상으로 만든다.
- 표시 문구는 `F 나가기`, `F 들어가기`처럼 데이터로 설정한다.
- 목적지는 `targetAreaId/targetSpawnId`로만 지정한다.
- 잠긴 포털은 기존 AccessPolicy/NotificationView를 재사용한다.
- 왕복 포털의 목적지 유효성을 에디터 검증에서 확인한다.
- F 연타가 중복 전환을 만들지 않는 PlayMode 테스트를 추가한다.

### 7단계: 세이브 v3 및 마이그레이션

- `SaveSchema.CurrentVersion`을 3으로 올린다.
- 진행 DTO와 도메인 저장 객체에 `areaId/spawnId`를 추가한다.
- entry/checkpoint와 지역 위치의 책임을 분리한다.
- 신규 게임 기본 위치는 `world_adjustment_lab_interior/reception_start`다.
- v2 → v3 마이그레이터를 순차 마이그레이션 파이프라인에 추가한다.
- v2 fixture, v3 왕복 직렬화, checksum, future schema 거부 테스트를 갱신한다.
- 수동 저장은 `ExplorationLocationState`의 마지막 안전 area/spawn을 저장한다.
- 전환 성공은 런타임 위치 상태만 갱신하며 임의 자동 저장을 추가하지 않는다.
- 로드 시 알 수 없는 area/spawn은 복구 가능한 명시적 오류로 처리한다.

### 8단계: M 입력과 UI 상태 통합

- `ExplorationInputReader`에 별도 Map one-shot 액션을 추가한다.
- Keyboard M과 Gamepad Select를 연결한다.
- `ClearPendingMenuInput` 또는 별도 지도 입력 정리 경계를 명확히 한다.
- `InGameUiCoordinator` 또는 그와 협력하는 단일 코디네이터가 지도 상태를 소유한다.
- 대화 → 전환 → 지도 → ESC 하위 메뉴 → ESC 루트 → 탐험 순서로 우선순위를 구현한다.
- 지도 열기 전 timeScale을 보관하고 닫기/비활성화/파괴 시 원래 값으로 복구한다.
- ESC 메뉴와 지도가 동시에 활성화되지 않게 한다.
- 입력 우선순위 EditMode/PlayMode 테스트를 추가한다.

### 9단계: MapDefinition과 순수 MapProjection

- 지역별 `AreaMapDefinition`과 층별 `MapFloorDefinition`을 만든다.
- `floorId`, 표시명, 배경 Sprite, origin, 두 축, world size, 미니맵 뷰 크기를 보관한다.
- `MapProjection` 순수 계산을 구현한다.
- 월드→정규화, 전체 지도 마커 좌표, 미니맵 배경 offset/uvRect, 가장자리 clamp를 지원한다.
- 회전 축과 잘못된 크기를 처리한다.
- 대표 좌표(네 모서리, 중앙, 영역 밖, 회전된 맵) EditMode 테스트를 작성한다.

### 10단계: 실시간 MiniMapView

- 기존 좌측 상단 위치 HUD 아래에 배치한다.
- 권장 크기는 1920×1080 기준 300×210, 1280×720에서는 240×168 이상을 유지한다.
- 배경, 마스크, 테두리, N 표식, 층 라벨, 플레이어 화살표를 둔다.
- 플레이어 위치·마지막 facing·현재 floor만 실시간 반영한다.
- 북쪽 고정을 유지하고 카메라 회전에 영향받지 않는다.
- 할당 없는 갱신을 사용하고 값이 변하지 않으면 UI 속성을 다시 쓰지 않는다.
- 모든 HUD Graphic은 `raycastTarget=false`다.
- 현재 지도 데이터가 없으면 미니맵을 숨기고 콘솔 경고를 한 번만 남긴다. 가짜 지도를 표시하지 않는다.

### 11단계: M AreaMapView

- 전체 화면 딤과 중앙 지도 프레임을 구현한다.
- 지역명, 방명, 층명, 지도 이미지, 플레이어 마커, 출입구 마커, 범례를 표시한다.
- 열 때 현재 층과 현재 위치를 선택한다.
- M/Escape/B로 닫는다.
- 휠로 제한된 확대/축소를 제공한다.
- 다층 데이터가 존재할 때 Q/E 또는 패드 shoulder로 층을 변경한다.
- 다른 층을 보고 있으면 플레이어 마커를 숨기고 현재 실제 층을 텍스트로 알린다.
- View는 입력 Gate, timeScale, 씬 로드, 저장을 직접 수행하지 않는다.
- 실행 시 버튼/이벤트 구독은 런타임에 연결하고 에디터 빌더 시점 람다에 의존하지 않는다.

### 12단계: 위치 HUD와 층 추적 확장

- 기존 `LocationTracker` 우선순위와 마지막 유효 위치 유지 규칙을 보존한다.
- stable `areaId/roomId/floorId`와 표시명을 분리한다.
- `MapFloorVolume` 또는 동등한 명시적 층 추적을 추가한다.
- 지역 전환 직후 기본 위치를 먼저 게시해 HUD가 빈 값으로 깜빡이지 않게 한다.
- 이전 지역 언로드 시 파괴된 볼륨 참조를 제거한다.
- HUD 문자열은 위치 변경 이벤트에서만 갱신한다.

### 13단계: 지역 씬과 idempotent 빌더

- 현재 연구실 환경을 `91_LabInterior` Additive 지역으로 분리한다.
- `92_BureauCourtyard`에는 기능 검증 가능한 외부 블록아웃을 만든다.
- 외부에는 바닥, 건물 외벽, 연구실 출입구, 충돌 경계, 조명, LocationVolume, 지도 데이터를 포함한다.
- 기존 프로젝트 TextureKit과 색상 토큰을 재사용하고 새 이미지가 없어도 단순 도형 지도 이미지로 기능을 완성한다.
- 빌더는 자기 소유 생성 루트만 교체하고 사용자의 수동 오브젝트를 삭제하지 않는다.
- 반복 실행해도 AreaRoot, Portal, Spawn, LocationVolume, 지도 데이터가 중복되지 않는다.
- Build Settings에 셸과 두 지역 씬을 누락 없이 등록한다.

### 14단계: 종합 검증과 문서 갱신

- `docs/architecture/EXPLORATION_PROTOTYPE.md`, `SAVE_DATA.md`, `BOOT_SAVE_FLOW.md`를 실제 구현과 일치하게 갱신한다.
- EditMode와 PlayMode 테스트를 실행한다.
- 씬 검증기로 중복 Canvas/EventSystem/AudioListener, AreaRoot 중복, stable ID 중복, Missing Script/Reference를 확인한다.
- 컴파일 오류와 콘솔 예외를 0개로 만든다.
- 수동 검증 항목과 자동 검증 항목을 구분해 보고한다.

---

## 6. 필수 테스트

### EditMode

- Area/Spawn stable ID 검증
- AreaRegistry 중복과 unknown ID 처리
- 전환 상태 머신의 허용/거부 전이
- Busy 중 중복 요청 거부
- MapProjection 중앙·모서리·영역 밖·회전 좌표
- LocationVolume priority와 floor 판정
- 지도/ESC/대화 입력 우선순위
- v2 → v3 마이그레이션
- v3 DTO 왕복과 checksum
- 알 수 없는 area/spawn 로드 거부

### PlayMode

- 세이브 진입 시 GameShell → 연구실 지역 Additive 초기화
- 연구실 내부 → 외부 → 내부 왕복
- 왕복 뒤 플레이어, 카메라, HUD, EventSystem, AudioListener가 각각 하나
- F 연타 중복 전환 방지
- 잘못된 목적지 전환의 이전 지역 롤백
- 전환 중 이동·대시·카메라·상호작용 잠금
- M 열기/닫기 및 timeScale/Gate 복구
- 대화 중 M 무시, Escape는 대화만 종료
- 지도 중 Escape는 지도만 종료
- 지도와 ESC 메뉴 동시 활성 방지
- 미니맵 플레이어 위치·방향·층 변경
- Q/E 카메라 회전 후 미니맵 북쪽 고정
- 포털과 HUD가 레이캐스트/클릭을 방해하지 않음
- 1280×720, 1920×1080, 2560×1440, 3440×1440 레이아웃

---

## 7. 시각 기준

`AGENTS.md`의 색상 토큰만 사용한다.

- 미니맵: `Surface #171C22`, 알파 0.88~0.94
- 테두리/N 표식: `AccentGold #B99A59`
- 플레이어/현재 위치: `Secondary #5CAECC`
- 지도 선/비활성 요소: `TextMuted #9AA6AF`
- 지역명/방명/층명: `TextMain #EEE6D5`
- 잠긴 출입구: `AccentRed #7D1827` + 자물쇠 형태
- M 지도 전체 딤: 검정 알파 0.72

임시 AURA 코드, 지역 번호, 디버그 좌표를 사용자 UI에 표시하지 않는다. 선택·잠김·현재 위치를 색상만으로 전달하지 않는다.

---

## 8. 금지사항

- 새 패키지 설치
- `90_GameShell`을 지역 이동마다 재로드
- 지역마다 플레이어·메인 카메라·HUD·EventSystem 생성
- `DontDestroyOnLoad` 남발
- `GameObject.Find`, `FindObjectOfType`, Service Locator
- View에서 SceneManager, 파일 IO, timeScale, 입력 Gate 직접 조작
- 지역/스폰 누락을 임의 기본값으로 조용히 대체
- 월드 좌표·씬 이름을 저장 데이터에 기록
- 별도 탑다운 카메라와 RenderTexture 미니맵을 첫 구현에 추가
- 가짜 NPC/퀘스트/재화/시간 데이터 표시
- 에디터 씬 빌더 시점의 비직렬화 람다만으로 버튼 이벤트 연결
- 기존 사용자 변경 되돌리기
- 테스트를 통과시키기 위한 기능 삭제
- 커밋 또는 푸시

---

## 9. 완료 보고 형식

완료 시 다음 순서로 보고한다.

1. 구현된 실내/외 왕복 흐름
2. GameShell과 Additive 지역 씬 수명주기
3. M 지도와 미니맵 입력/표시 규칙
4. 저장 v3과 v2 마이그레이션 결과
5. 변경 파일 목록
6. 실행한 EditMode/PlayMode 테스트와 결과
7. 컴파일·콘솔·Missing Reference 검증 결과
8. 자동화하지 못한 수동 확인 항목
9. 남은 위험과 다음 한 작업
