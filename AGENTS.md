# DemonLord_v2 Unity 개발 규칙

이 저장소를 수정하는 모든 개발 에이전트는 작업 전에 이 파일과 `docs/architecture` 문서를 읽는다. 현재 범위는 전투 구현이 아니라, 실행부터 세이브 데이터에 따른 게임 진입까지의 프런트엔드 수직 슬라이스다.

## 작업 시작과 Git

1. 먼저 `git status --short`, `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`을 확인한다.
2. 기존 사용자 변경을 되돌리거나 스테이징하지 않는다. 관계없는 파일은 수정하지 않는다.
3. Unity 버전은 `6000.4.10f1`로 유지한다. 업그레이드/다운그레이드는 금지한다.
4. `Library`, `Temp`, `Logs`, `UserSettings`, IDE 생성 파일, 빌드 산출물은 커밋하지 않는다.
5. `.meta`, `.unity`, `.prefab`, `Assets`, `Packages`, `ProjectSettings` 변경은 누락 없이 함께 커밋한다.
6. 새 패키지는 추가하지 않는다. 현재 승인된 UI/입력/테스트 의존성은 Input System, UGUI, Test Framework다.

## 목표 사용자 흐름

```text
게임 실행
→ 로고/주의 문구
→ 타이틀 인트로
→ 메인 화면
→ 게임 시작 방식 선택
→ 세이브 슬롯 선택
→ 새 게임 설정
→ 세이브 생성 또는 로드
→ 세이브의 EntryId/CheckpointId에 따라 게임 진입
```

이어하기는 `StartMode → SaveSlots(Load) → 검증 → Entry 해석 → GameShell`이다.

새 게임은 `StartMode → SaveSlots(New) → (사용 중 슬롯이면 덮어쓰기 확인) → NewGameSetup → 원자적 저장 → Entry 해석 → GameShell`이다.

## 구조 경계

목표 폴더 구조:

```text
Assets/_Project/
  Scenes/                 # 00_Boot, 10_Frontend, 90_GameShell
  Prefabs/
  ScriptableObjects/
  Scripts/
    Domain/               # UnityEngine 미참조 순수 데이터/규칙
    Application/          # 유스케이스와 인터페이스
    Infrastructure/       # 파일, 직렬화, 씬 로딩 구현
    Presentation/         # MonoBehaviour, UGUI View/Presenter
    Bootstrap/            # 명시적 composition root
  Tests/EditMode/
  Tests/PlayMode/
```

- `Domain`과 `Application`은 가능하면 `UnityEngine`에 의존하지 않는다.
- UI View는 입력 전달과 렌더링만 담당한다. 파일 저장, 세이브 검증, 씬 결정은 Presenter/Coordinator/UseCase가 담당한다.
- `AppRoot`만 앱 수명주기 서비스에 대해 `DontDestroyOnLoad`를 사용할 수 있다.
- 전역 mutable static, 범용 Service Locator, `FindObjectOfType`, `GameObject.Find`, `PlayerPrefs` 기반 세이브를 사용하지 않는다.
- 씬 이름 문자열과 `SceneManager` 호출을 여러 화면에 흩뿌리지 않는다. `ISceneFlowService`와 `IEntryPointResolver` 경계로 모은다.

## 프런트엔드 상태 규칙

`FrontendCoordinator`만 화면 전이를 결정한다. 허용 상태는 다음과 같다.

| 상태 | 역할 | 뒤로 가기 |
|---|---|---|
| `LogoNotice` | 로고/주의 문구, 최소 노출 후 스킵 | 없음 |
| `TitleIntro` | 타이틀 인트로, 입력 또는 시간 초과로 완료 | 없음 |
| `MainMenu` | 게임 시작/설정/종료 | 없음 |
| `StartMode` | 이어하기/새 게임 | MainMenu |
| `SaveSlots.Load` | 유효 세이브 로드 | StartMode |
| `SaveSlots.New` | 새 게임 슬롯 선택 | StartMode |
| `NewGameSetup` | 프로필/난이도/튜토리얼 설정 | SaveSlots.New |
| `Busy` | 저장·로드·씬 전환 중 입력 잠금 | 작업별 정책 |
| `ErrorDialog` | 복구 가능한 실패 표시 | 안전한 이전 상태 |

- 로고/인트로는 애니메이션 이벤트가 누락돼도 최대 시간 후 진행해야 한다.
- 비동기 요청 중에는 중복 클릭과 중복 씬 로드를 막는다.
- Back/Cancel은 항상 결정적이며, 화면 재진입 시 이벤트 구독이 중복되지 않아야 한다.
- Continue는 `Valid`이면서 호환 가능한 슬롯이 하나 이상일 때만 활성화한다.

## 세이브 규칙

- 슬롯 ID는 `slot-01`, `slot-02`, `slot-03`으로 고정한다. 표시 이름을 파일 경로로 쓰지 않는다.
- 저장 위치는 `Application.persistentDataPath/Saves/<slotId>/save.json`이다.
- 세이브는 `schemaVersion`, `saveId`, `slotId`, UTC 생성/수정 시각, `buildVersion`, payload, `payloadSha256`을 가진다.
- 실제 재개 위치는 씬 이름이 아니라 안정적인 `entryId`와 `checkpointId`로 기록한다.
- 첫 구현의 새 게임 진입값은 `entryId=prologue_start`, `checkpointId=start`다.
- 신규 세이브 설정값은 `profileName`, `difficultyId`, `tutorialEnabled`다. 난이도 ID는 `story`, `normal`, `hard`만 허용한다.
- `profileName`은 trim 후 1~16자이며 제어 문자와 파일 경로 문자를 거부한다.
- 저장은 `save.tmp`에 완전 작성·검증 후 최종 파일을 교체하고, 기존 정상 파일을 `save.bak`으로 남긴다.
- 로드 실패를 빈 슬롯으로 표시하지 않는다. `Empty`, `Valid`, `Corrupt`, `Incompatible`을 구분한다.
- 본 파일이 손상되면 백업을 검사하고, 성공 시 UI와 로그에 복구 사실을 남긴다.
- 미래 스키마 세이브는 덮어쓰지 않고 `Incompatible`으로 표시한다. 과거 버전은 명시적인 순차 마이그레이션만 허용한다.
- 체크섬은 손상 탐지용이며 보안·치트 방지 기능이라고 표현하지 않는다.

## 진입점 규칙

세이브의 `EntryId`는 `IEntryPointResolver`가 런타임 씬/스폰 지점으로 변환한다.

```text
저장: entryId="prologue_start", checkpointId="start"
  ↓
Resolver: sceneKey="90_GameShell", spawnKey="start"
```

알 수 없거나 구현되지 않은 EntryId를 기본 씬으로 조용히 대체하지 않는다. 세션을 비우고 오류를 표시한 뒤 슬롯 화면으로 복귀한다.

## 테스트와 완료 보고

- EditMode: 상태 전이/Back, 입력 검증, 슬롯 분류, 체크섬 오류, 백업 복구, 미래 버전 거부, Entry 해석 실패, 새 게임 초기 데이터 생성.
- PlayMode: 첫 실행 새 게임, 재실행 이어하기, 덮어쓰기 취소/확인, 손상 슬롯, 빠른 연속 클릭을 스모크 테스트한다.
- Unity 재컴파일과 테스트를 확인하고, 컴파일 오류·콘솔 예외·Missing Script/Reference가 0개여야 한다.
- 완료 보고에는 변경 파일, 실행한 테스트와 결과, 수동 확인, 미완료 위험, 다음 한 작업을 적는다.
