# 개발 에이전트 구현 프롬프트 — 인게임 저장·환경 설정·타이틀 복귀

## 실행 지시

`C:\workspace\unity\DemonLord_v2`에서 작업한다.

`AGENTS.md`와 아래 문서를 UTF-8로 끝까지 읽은 뒤 0~12단계를 순서대로 구현한다.

- `docs/architecture/INGAME_PAUSE_CORE_ACTIONS.md`
- `docs/architecture/INGAME_HUD_PAUSE_MENU.md`
- `docs/architecture/BOOT_SAVE_FLOW.md`
- `docs/architecture/SAVE_DATA.md`
- `docs/architecture/EXPLORATION_PROTOTYPE.md`

기존 사용자 변경을 보존하고 관계없는 파일을 수정하지 않는다. 커밋·푸시는 하지 않는다. Unity 버전과 패키지는 변경하지 않는다. 전체 시각 QA나 UI 재디자인은 이번 작업 범위가 아니다.

## 최종 목표

`90_GameShell`의 일시정지 메뉴에서 다음 기능을 실제 서비스와 연결하고 성공·실패·취소 경로를 자동 테스트한다.

1. 저장하기
2. 환경 설정
3. 타이틀로 돌아가기

기존 코드에 이미 있는 기능은 재작성하지 말고 보완한다. 특히 `InGameUiCoordinator`, `PauseMenuView`, `SaveGameProgressUseCase`, `SettingsService`, `UnitySceneFlowService`, `FrontendCoordinator`를 우선 재사용한다.

## 금지 사항

- `PlayerPrefs` 저장
- View에서 파일 저장, `SceneManager`, Repository 호출
- 플레이어 Transform, 문 상태, 대화 중간 상태를 세이브 DTO에 임의 추가
- `GameObject.Find`, `FindObjectOfType`, Service Locator, 새 전역 mutable static
- 부팅과 타이틀 복귀를 구분하지 않은 채 Frontend의 기존 `Busy` 상태 재사용
- 실패를 성공으로 표시하거나 예외를 삼키기
- 새 UI 이미지 생성 또는 기존 메뉴의 시각 전면 재작업
- 사용자 변경 되돌리기, Git reset/checkout, 커밋, 푸시

## 0단계 — 기준선 확인

다음을 먼저 기록한다.

- `git status --short`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- 현재 컴파일 상태
- 관련 EditMode/PlayMode 테스트 목록

기존 변경이 많으므로 이번 작업 파일과 사용자 변경 파일을 구분한다.

## 1단계 — 현재 구현 감사

다음 코드를 끝까지 읽고 설계 문서와 차이를 짧게 정리한다.

- `InGameUiCoordinator`
- `PauseMenuView`
- `InGameMenuStateMachine`
- `SaveGameProgressUseCase`
- `LabProgressController`
- `SettingsService`
- `UnityGameSettingsRuntimeApplier`
- `ISceneFlowService` / `UnitySceneFlowService`
- `FrontendCoordinator` / `FrontendView`
- `GameShellRoot` / `AppRoot`

현재 확인된 핵심 결함은 GameShell 진입 전 남아 있는 `FrontendScreen.Busy`를 타이틀 복귀 시 그대로 재사용할 가능성이다. 이 결함을 반드시 해결한다.

## 2단계 — Frontend 진입 모드 계약

Application 계층에 순수 enum `FrontendEntryMode`를 추가한다.

```text
Opening
MainMenu
```

`ISceneFlowService.LoadFrontendAsync`가 진입 모드를 명시적으로 받도록 변경한다. 기본 인자를 사용해 호출 의도를 숨기지 않는다.

- `AppRoot.Start`는 `Opening`을 전달한다.
- 인게임 타이틀 복귀는 `MainMenu`를 전달한다.
- 모든 fake/mock/test 구현도 갱신한다.

`FrontendCoordinator`에 진입 모드를 반영하는 명시적 초기화 메서드를 추가한다.

- Opening: `LogoNotice`, 오류·선택 상태 초기화
- MainMenu: `MainMenu`, 오류·선택 상태 초기화, 슬롯 목록 갱신 가능 상태

`FrontendView.Initialize`는 현재 진입 모드가 Opening일 때만 로고/주의 문구 코루틴을 시작한다. MainMenu이면 즉시 메인 메뉴를 렌더링하고 첫 활성 버튼에 포커스를 둔다.

## 3단계 — 수동 저장 계약 확정

메뉴 저장은 현재 세션의 `CurrentSave.Progress.CheckpointId`를 `SaveGameProgressUseCase`에 전달한다.

- 현재 체크포인트는 `LabProgressController`가 성공적으로 저장한 마지막 안정 체크포인트다.
- 새로운 위치 좌표 필드나 임시 세이브 필드를 만들지 않는다.
- 활성 세이브가 없으면 버튼 비활성 및 명령 거부다.
- 성공 시 `IPlayerSession.CurrentSave`가 유스케이스 결과로 갱신되어야 한다.
- 실패 시 기존 세션은 동일해야 한다.

필요하면 `SaveGameProgressUseCase`의 결과를 테스트하기 쉽게 보완하되 Repository의 원자적 저장 규칙은 변경하지 않는다.

## 4단계 — 저장 버튼 완성

`InGameUiCoordinator`에서 다음 상태 흐름을 보장한다.

```text
Root → Busy → Root
```

- 시작 즉시 루트 버튼 비활성
- 중복 요청 거부
- 성공: `기록을 저장했습니다.`, `ui_save_complete_01.wav`
- 실패: `기록을 저장하지 못했습니다. 다시 시도해 주세요.`
- 성공/실패 모두 메뉴와 일시정지는 유지
- 예외는 로그에 남기고 실패 UI로 복구

`PauseMenuView`는 저장 성공/실패 상태를 문구로 표시하고, Busy 동안 포커스가 사라져도 복귀 후 첫 활성 항목을 다시 선택한다.

## 5단계 — 설정 편집 수명주기 완성

환경 설정 진입 시 반드시 `SettingsService.BeginEdit()`을 한 번 호출한다.

- 값 변경: `SetWorking`
- 기본값: `ResetWorking`
- 적용: `SaveWorking`
- 취소 버튼, Escape, Gamepad East: `CancelEdit`

적용 성공은 Root로 돌아가고 성공 문구를 표시한다. 저장 실패는 Settings 화면에 남아 오류 문구를 표시하며 Persisted를 변경하지 않는다. 실패 후 취소하면 기존 Persisted가 런타임에 다시 적용되어야 한다.

화면을 반복 진입해도 이벤트가 중복 구독되지 않게 한다.

## 6단계 — 설정의 실제 런타임 반영

현재 존재하는 설정 소비 경로를 연결한다.

- 화면 모드, 해상도, VSync, 품질: `UnityGameSettingsRuntimeApplier`
- 인게임 UI 효과음: `masterVolume × sfxVolume`
- GameShell BGM AudioSource가 명시적으로 주입되어 있을 때만 `masterVolume × bgmVolume`
- UI 크기: 일시정지 메뉴 기준 루트에 적용하되 중앙 앵커와 Safe Area를 깨지 않음

화면 흔들림·섬광·전환 효과가 실제로 존재하지 않으면 새 가짜 시스템을 만들지 않는다. 값의 저장과 취소 복구만 보장하고 미적용 범위를 완료 보고에 명시한다.

## 7단계 — 타이틀 복귀 확인 흐름

Root에서 바로 씬을 전환하지 않는다.

```text
Root → ConfirmReturnToTitle
```

확인 문구에는 세션 종료와 저장되지 않은 진행 유실 가능성을 명시한다. 취소 또는 뒤로 가기는 Root로 돌아간다.

확인 시:

1. `Busy`로 전환하고 중복 입력을 막는다.
2. 현재 `GameSave`를 로컬 변수에 보관한다.
3. `timeScale`과 탐험 입력 잠금을 정상 복구한다.
4. 세션을 비운다.
5. `LoadFrontendAsync(FrontendEntryMode.MainMenu)`를 await한다.

암묵적인 수동 저장은 실행하지 않는다.

## 8단계 — 타이틀 전환 실패 복구

씬 전환이 예외 또는 실패로 끝나면:

- 보관한 원본 `GameSave`를 세션에 복구
- 메뉴 상태를 Root로 강제 복구
- 기존 일시정지 `timeScale` 다시 적용
- 탐험 입력 잠금 다시 획득
- `타이틀 화면으로 이동하지 못했습니다.` 표시
- 버튼 포커스 복구

잠금 토큰을 중복 Dispose하거나 `timeScale=1`로 무조건 덮어쓰지 않는다.

## 9단계 — 입력과 포커스 회귀 방지

- 대화 중 Escape는 대화만 닫는다.
- Root Escape는 메뉴를 닫는다.
- Settings/확인창 Escape는 Root로 돌아간다.
- Busy에서는 Escape, Submit, 클릭을 무시한다.
- 저장/설정/타이틀 실패 후 키보드·마우스·게임패드 포커스를 복구한다.
- 메뉴를 닫으면 이동·대시·상호작용·카메라 입력이 정확히 복구된다.

## 10단계 — EditMode 테스트

최소 다음 테스트를 추가한다.

- 수동 저장 성공: 슬롯/Entry 유지, 체크포인트 유지, 수정 시각과 세션 갱신
- 수동 저장 실패: 세션 불변
- 설정 취소: Persisted 런타임 재적용
- 설정 저장 실패: Persisted 불변
- Frontend Opening 초기화
- Frontend MainMenu 복귀 초기화
- Busy 중 중복 명령 거부
- 확인 취소 상태 전이

테스트는 임시 디렉터리 또는 in-memory fake를 사용하며 실제 사용자 세이브를 건드리지 않는다.

## 11단계 — PlayMode 통합 테스트

기존 `ExplorationPrototypePlayModeTests`의 패턴을 재사용해 다음을 검증한다.

- 메뉴 저장 성공/실패 UI 및 일시정지 유지
- 설정 변경 후 취소 복구
- 설정 적용 성공 후 재진입 값 유지
- 타이틀 복귀 확인 취소
- 타이틀 복귀 성공 후 Frontend MainMenu 직접 진입
- 타이틀 복귀 실패 후 세션/Root/timeScale/입력 잠금 복구
- 빠른 연속 입력에도 저장과 씬 로드 호출 횟수 1회

테스트용 `ISceneFlowService`는 완료 시점을 제어할 수 있는 fake를 사용한다. production 코드에 테스트 전용 분기를 넣지 않는다.

## 12단계 — 검증 및 완료 보고

다음을 순서대로 확인한다.

1. Runtime/Editor/EditModeTests/PlayModeTests 컴파일
2. 관련 EditMode 테스트
3. 관련 PlayMode 테스트
4. `90_GameShell` 씬 참조 검증
5. Missing Script/Reference와 콘솔 예외 0개

전체 시각 QA는 하지 않는다. 기능 검증에 필요한 화면 상태와 포커스만 확인한다.

완료 보고에는 다음을 포함한다.

- 변경 파일
- 저장하기 동작과 저장 범위
- 환경 설정 적용/취소/실패 동작
- 타이틀 복귀 성공/실패 동작
- 실행한 테스트와 정확한 결과
- 아직 실제 소비자가 없어 저장만 되는 설정 항목
- 커밋·푸시하지 않았다는 확인
