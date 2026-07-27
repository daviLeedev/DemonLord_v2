# 인게임 HUD 및 일시정지 메뉴 설계

## 1. 목표

`90_GameShell` 탐험 화면에 다음 UI를 추가한다.

- 좌측 상단: 현재 지역과 현재 방 표시
- 우측 상단: 향후 재화와 게임 시간을 표시할 상태 영역
- 화면 중앙: `Escape` 또는 게임패드 Start로 여는 일시정지 메뉴
- 메뉴 하위 화면: 환경 설정, 조작 안내, 타이틀 복귀 확인, 게임 종료 확인

이번 단계에서는 재화 경제와 게임 시간 시뮬레이션 자체를 구현하지 않는다. HUD가 가짜 값을 소유하지 않도록 읽기 전용 상태 계약과 빈 값 표현만 먼저 만든다.

## 2. 화면 구성

기준 해상도는 `1920 x 1080`, `CanvasScaler.ScaleWithScreenSize`, `matchWidthOrHeight=0.5`다. 모든 고정 HUD는 Safe Area 내부에 배치한다.

```text
┌──────────────────────────────────────────────────────────────────────┐
│ [세계조정국 연구실]                         [재화  —] [시간  --:--] │
│ [현재 방: 중앙 접수실]                                             │
│                                                                      │
│                         ┌────────────────┐                           │
│                         │      메뉴      │                           │
│                         │                │                           │
│                         │   계속하기     │                           │
│                         │   저장하기     │                           │
│                         │   환경 설정    │                           │
│                         │   조작 안내    │                           │
│                         │   타이틀로     │                           │
│                         │   게임 종료    │                           │
│                         └────────────────┘                           │
└──────────────────────────────────────────────────────────────────────┘
```

### 좌측 상단 위치 패널

- 첫 줄: 상위 지역 `세계조정국 연구실`
- 둘째 줄: 현재 방 이름
- 위치 ID와 표시 이름은 분리한다.
- 방 진입 Trigger가 `LocationVolume`에 등록된 안정 ID를 상태 소스에 전달한다.
- 초기 방은 `reception`, 표시 이름은 `중앙 접수실`이다.
- 권장 방 ID:
  - `reception`: 중앙 접수실
  - `tax_office`: 세무 집행실
  - `analysis_lab`: 세계선 분석실
  - `archive`: 기록보관실
  - `archive_annex`: 기록보관실 별관
  - `restricted`: 격리실

### 우측 상단 상태 패널

- 재화와 게임 시간은 서로 독립된 행 또는 칩으로 구성한다.
- 이번 단계의 데이터 소스가 없으면 `—`, `--:--`를 표시한다. 임의로 `9999`, `Day 1` 같은 가짜 데이터를 넣지 않는다.
- 뷰가 재화나 시간을 계산하지 않는다.
- 향후 `IWalletReadModel`, `IGameTimeReadModel`과 연결할 수 있는 읽기 전용 계약을 둔다.
- 실제 값이 없더라도 아이콘 슬롯, 값 라벨, 접근성용 텍스트 라벨은 유지한다.
- 시간 표기 기본 형식은 `HH:mm`, 날짜가 생기면 `D{day}  HH:mm`으로 확장한다.

## 3. 일시정지 메뉴 상태

`InGameMenuState`는 최소 다음 상태를 갖는다.

```text
Closed
Root
Settings
Controls
ConfirmReturnToTitle
ConfirmQuit
Busy
```

- `Closed`: 탐험 HUD만 표시한다.
- `Root`: 중앙 메뉴 목록을 표시한다.
- `Settings`: 기존 `SettingsService`의 작업 복사본을 편집한다.
- `Controls`: 키보드/마우스/게임패드 조작표를 표시한다.
- 확인 화면은 취소하면 반드시 `Root`로 돌아간다.
- `Busy`: 저장 또는 씬 전환 중 중복 입력을 차단한다.

## 4. Escape 입력 우선순위

Escape는 여러 컴포넌트가 경쟁해서 소비하지 않는다. `InGameUiCoordinator`가 한 곳에서 다음 순서로 처리한다.

1. 대화가 열려 있으면 대화만 닫는다.
2. 확인창·설정·조작 안내가 열려 있으면 `Root`로 돌아간다.
3. 루트 메뉴가 열려 있으면 메뉴를 닫고 게임을 재개한다.
4. 탐험 중이면 루트 메뉴를 연다.

입력 시스템에는 별도 `Pause` 액션을 추가한다.

- Keyboard: `Escape`
- Gamepad: `Start`
- 게임패드 `B/East`: 하위 화면 취소
- `Escape`를 대화 컨트롤러와 일시정지 컨트롤러가 동시에 소비하지 않게 한다.
- 메뉴가 열린 동안 `Movement`, `Dash`, `Interaction`, `Camera`, `Dialogue` 채널을 토큰으로 잠근다.
- 메뉴 토글 입력은 잠금 밖의 별도 채널이어야 한다.
- 메뉴 종료, 씬 전환, 비활성화, 예외 발생 경로에서 잠금 토큰을 정확히 한 번 해제한다.

## 5. 실제 일시정지 규칙

- 루트 메뉴 또는 하위 메뉴가 열리면 `Time.timeScale=0`으로 탐험 시뮬레이션을 멈춘다.
- 열기 직전 값을 보관하고 닫을 때 복원한다. 무조건 `1`로 덮어쓰지 않는다.
- UI 애니메이션과 메뉴 입력은 unscaled time을 사용한다.
- `OnDisable`, `OnDestroy`, 타이틀 전환 실패에서도 시간 배율과 입력 잠금을 복구한다.
- 대화만 닫는 Escape에서는 일시정지 메뉴를 열지 않는다.

## 6. 메뉴 명령

### 계속하기

- 메뉴를 닫고 이전 `timeScale` 및 입력 상태를 복원한다.
- 기본 선택 항목이다.

### 저장하기

- 현재 세션의 저장 슬롯과 체크포인트를 유지한 채 `SaveGameProgressUseCase`를 호출한다.
- 저장 중에는 `Busy`로 전환하고 버튼 연타를 막는다.
- 성공/실패는 기존 알림 UI 또는 메뉴 내부 상태 라벨로 명확히 표시한다.
- 현재 저장 포맷에 없는 임의의 위치, 재화, 시간 데이터를 추가하지 않는다.

### 환경 설정

- 기존 `SettingsService.BeginEdit`, `SetWorking`, `SaveWorking`, `CancelEdit`를 사용한다.
- 프런트엔드 설정 코드를 복사해서 별도 규칙을 만들지 않는다.
- 설정 저장 성공 후 `Root`로 돌아간다.
- 취소는 런타임 설정을 `Persisted` 값으로 복구한다.

### 조작 안내

- 현재 실제 바인딩만 표시한다.
- WASD 이동, Shift 달리기, Space 대시, F 상호작용/대화, Q/E 회전, 휠 확대/축소, Escape 메뉴, 게임패드 대응을 포함한다.

### 타이틀로 돌아가기

- 확인창을 거친다.
- 확인 시 현재 세션을 정리하고 `ISceneFlowService.LoadFrontendAsync()`를 호출한다.
- 씬 전환 실패 시 메뉴를 복구하고 오류를 표시한다.

### 게임 종료

- 확인창을 거친다.
- Player 빌드에서는 `Application.Quit()`을 호출한다.
- Editor에서는 Play Mode를 강제로 조작하지 않고 로그 또는 테스트 가능한 종료 어댑터를 사용한다.

## 7. 시각 규칙

- 기존 `AGENTS.md`의 비주얼 색상 시스템을 따른다.
- 화면 전체 딤: 검정 알파 `0.68~0.76`.
- 중앙 표면: `Surface #171C22`, 불투명도 약 `0.96`.
- 기본 테두리: `AccentGold #B99A59`.
- 선택/포커스: `Secondary #5CAECC`와 외곽선 또는 커서 표시를 함께 사용한다.
- 본문: `TextMain #EEE6D5`, 보조: `TextMuted #9AA6AF`.
- 위험 확인 버튼에만 `AccentRed #7D1827`을 사용한다.
- 코드명, 지역 번호, AURA 식별자 같은 임시 서브텍스트를 추가하지 않는다.
- 메뉴는 화면 정중앙에 고정하고 HUD는 Safe Area 모서리를 따른다.
- HUD는 탐험 입력을 가로막지 않도록 `raycastTarget=false`다. 메뉴가 열린 경우에만 오버레이가 레이캐스트를 차단한다.

## 8. 데이터 및 책임 경계

권장 구성:

```text
InGameUiCoordinator
├─ PauseMenuView
├─ InGameHudView
├─ LocationTracker / LocationVolume
├─ SettingsService
├─ SaveGameProgressUseCase
├─ IPlayerSession
└─ ISceneFlowService
```

- `InGameUiCoordinator`: 메뉴 상태, Escape 우선순위, 입력 잠금, 시간 배율 복원 담당
- `PauseMenuView`: 렌더링과 버튼 이벤트 전달만 담당
- `InGameHudView`: 제공받은 읽기 전용 상태를 렌더링
- `LocationTracker`: 방 Trigger를 안정 ID와 표시 이름으로 변환
- 저장, 설정 파일, 씬 전환을 View에서 직접 수행하지 않는다.
- `FindObjectOfType`, `GameObject.Find`, Service Locator, 새로운 전역 mutable static을 사용하지 않는다.
- `GameShellRoot`와 `AppRoot`의 명시적 주입 경로를 확장한다.

## 9. 완료 기준

- 탐험 중 Escape 한 번에 중앙 메뉴가 열린다.
- 다시 Escape를 누르면 닫히고 캐릭터가 정상적으로 움직인다.
- 대화 중 Escape는 대화만 닫고 메뉴를 열지 않는다.
- 메뉴 중 플레이어, 문, 카메라가 움직이지 않는다.
- 모든 닫기/실패/씬 전환 경로에서 `timeScale`과 입력 잠금이 복구된다.
- 좌측 상단 위치가 방 진입에 따라 갱신된다.
- 우측 상단 재화·시간 영역이 가짜 데이터를 만들지 않고 확장 가능한 계약을 가진다.
- 키보드·마우스·게임패드 포커스가 정상 동작한다.
- 컴파일 오류, 콘솔 예외, Missing Script/Reference가 0개다.
