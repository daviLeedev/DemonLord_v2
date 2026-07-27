# 인게임 HUD 및 ESC 일시정지 메뉴 구현 프롬프트

## 1. 역할과 최종 목표

당신은 `C:\workspace\unity\DemonLord_v2`를 수정하는 시니어 Unity 클라이언트 개발자다.

현재 `90_GameShell`의 세계조정국 연구실 탐험 슬라이스에 다음 기능을 완성한다.

```text
탐험 화면
├─ 좌측 상단: 현재 위치 HUD
├─ 우측 상단: 재화·게임 시간용 HUD
└─ Escape / Gamepad Start
   └─ 화면 정중앙 일시정지 메뉴
      ├─ 계속하기
      ├─ 저장하기
      ├─ 환경 설정
      ├─ 조작 안내
      ├─ 타이틀로 돌아가기
      └─ 게임 종료
```

재화 경제와 게임 시간 시스템 자체는 이번 범위가 아니다. UI와 읽기 전용 확장 계약만 만들며 가짜 값을 생성하지 않는다.

커밋과 푸시는 하지 않는다.

---

## 2. 반드시 UTF-8로 끝까지 읽을 문서

구현 전에 다음 문서를 UTF-8로 끝까지 읽는다.

1. `AGENTS.md`
2. `MASTER_IMPLEMENTATION_PROMPT.md`
3. `docs/architecture/INGAME_HUD_PAUSE_MENU.md`
4. `docs/architecture/EXPLORATION_PROTOTYPE.md`
5. `docs/architecture/BOOT_SAVE_FLOW.md`
6. `docs/architecture/SAVE_DATA.md`
7. `docs/agent/WORLD_ADJUSTMENT_LAB_VERTICAL_SLICE_PROMPT.md`

충돌 시 구체적이고 최신인 `docs/architecture/INGAME_HUD_PAUSE_MENU.md`를 이 작업 범위에서 우선한다. 기존 사용자 변경은 보존한다.

---

## 3. 작업 시작 점검

다음 항목을 먼저 확인하고 간단히 기록한다.

- `git status --short`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Assets/_Project/Scenes/90_GameShell.unity`
- `WorldAdjustmentLabSceneBuilder`
- `GameShellRoot`, `UnitySceneFlowService`, `AppRoot`
- `ExplorationInputReader`, `ExplorationInputGate`, `DialogueFocusController`
- `SaveGameProgressUseCase`, `SettingsService`, `IPlayerSession`, `ISceneFlowService`
- 기존 EditMode/PlayMode 테스트

새 패키지를 추가하지 않는다. 현재 Input System, UGUI, Test Framework만 사용한다.

---

## 4. 확정 UX 규칙

### 4.1 HUD

- 좌측 상단에는 `세계조정국 연구실`과 현재 방 이름을 2단으로 표시한다.
- 우측 상단에는 재화와 시간을 각각 독립된 행 또는 칩으로 표시한다.
- 데이터가 아직 없으면 재화 `—`, 시간 `--:--`을 표시한다.
- `9999`, 임의 날짜, 임시 AURA 코드, 지역 번호 같은 가짜 텍스트를 넣지 않는다.
- HUD는 Safe Area를 따르고 탐험 레이캐스트를 차단하지 않는다.
- HUD는 메뉴가 열려도 뒤에 남지만 전체 딤 아래에 놓인다.

### 4.2 ESC 메뉴

- 메뉴 패널은 화면 정중앙에 고정한다.
- 항목 순서는 다음과 같다.
  1. 계속하기
  2. 저장하기
  3. 환경 설정
  4. 조작 안내
  5. 타이틀로 돌아가기
  6. 게임 종료
- 최초 선택은 계속하기다.
- 메뉴는 키보드, 마우스, 게임패드로 모두 조작할 수 있어야 한다.
- 타이틀 복귀와 게임 종료는 확인창을 거친다.
- 환경 설정 또는 조작 안내에서 Escape/취소는 루트 메뉴로 돌아간다.

### 4.3 Escape 우선순위

Escape를 여러 Update가 경쟁해서 소비하지 않게 한다.

```text
대화 활성      → 대화만 종료
하위 메뉴 활성 → 루트 메뉴로 복귀
루트 메뉴 활성 → 메뉴 닫기/탐험 재개
탐험 중        → 루트 메뉴 열기
```

- 키보드 Escape는 최상위 `InGameUiCoordinator`가 소유한다.
- 게임패드 Start는 메뉴 토글이다.
- 게임패드 B/East는 대화 또는 하위 메뉴 취소다.
- 대화 종료에 사용한 Escape가 다음 프레임 메뉴를 열면 실패다.

---

## 5. 구현 단계

아래 0~12단계를 순서대로 구현한다. 한 단계의 컴파일 오류를 다음 단계로 넘기지 않는다.

### 0단계: 기준선 확인

- 기존 씬과 테스트를 변경 전에 확인한다.
- 현재 사용자 변경을 되돌리거나 정리하지 않는다.
- 기존 프런트엔드 설정 UI와 저장 흐름을 분석하고 재사용 가능한 계약을 확인한다.

### 1단계: 메뉴 상태 모델

Unity 의존이 필요 없는 상태 모델 또는 코디네이터 상태를 만든다.

```csharp
Closed
Root
Settings
Controls
ConfirmReturnToTitle
ConfirmQuit
Busy
```

- 허용되지 않은 전이는 거부한다.
- Busy 중 중복 명령을 거부한다.
- Back 결과가 결정적이어야 한다.
- 상태 전이 EditMode 테스트를 먼저 작성한다.

### 2단계: 입력 소유권 정리

- `ExplorationInputReader`에 별도 Pause/Menu 액션을 추가한다.
- Keyboard Escape와 Gamepad Start를 연결한다.
- 기존 Dialogue Cancel과 같은 프레임에 두 소비자가 경쟁하지 않게 한다.
- 필요하면 `ExplorationInputChannel.Menu`를 추가한다.
- 메뉴 토글 채널은 탐험 채널 잠금 이후에도 사용할 수 있어야 한다.
- one-shot 입력은 시작/종료 시 명시적으로 비운다.
- 대화 중 Escape는 대화만 종료하도록 PlayMode 테스트를 추가한다.

### 3단계: 인게임 UI 코디네이터

`InGameUiCoordinator` 또는 동등한 단일 소유자를 구현한다.

책임:

- Escape 우선순위
- 메뉴 상태 전이
- 입력 Gate 토큰 보유/해제
- 이전 `Time.timeScale` 보관/복원
- 하위 뷰 열기/닫기
- 저장, 설정, 씬 전환 명령 전달
- 실패 후 안전 복구

금지:

- View가 파일 저장 또는 씬 전환을 직접 수행
- 여러 컴포넌트가 각각 Escape를 polling
- 메뉴 닫을 때 무조건 `Time.timeScale=1`
- 정적 전역 플래그로 메뉴 상태 보관

### 4단계: 일시정지 동작

- 메뉴 열기 직전 `Time.timeScale`을 저장하고 `0`으로 설정한다.
- UI 전환은 `Time.unscaledDeltaTime`을 사용한다.
- 메뉴가 열린 동안 다음을 Gate 토큰으로 잠근다.
  - Movement
  - Dash
  - Interaction
  - Camera
  - Dialogue
- 닫기, 비활성화, 파괴, 타이틀 전환 실패에서 토큰과 timeScale을 한 번만 복구한다.

### 5단계: 중앙 메뉴 View

UGUI로 `PauseMenuView`를 구현한다.

- 전체 화면 딤 패널
- 중앙 메뉴 프레임
- 세로 버튼 6개
- 상태 라벨 또는 저장 결과 표시 영역
- 확인 모달
- 첫 포커스와 마지막 포커스 복구
- 마우스 hover, 키보드/게임패드 Select, Confirm, Cancel

기존 리소스가 적합하면 다음을 재사용한다.

- `UI/Common/modal_frame`
- `UI/Common/button_standard`
- `UI/Common/notice_window_frame`
- 기존 `FrontendUiTheme`의 글꼴과 색상

에셋이 없다는 이유로 기능을 중단하지 않는다. 단색/기존 프레임으로 동작 가능한 UI를 먼저 만든다.

### 6단계: 환경 설정 하위 화면

- 기존 `SettingsService`를 명시적으로 주입한다.
- `BeginEdit`, `SetWorking`, `SaveWorking`, `CancelEdit` 계약을 그대로 사용한다.
- 음향, 화면, 접근성 페이지를 제공하되 기존 프런트엔드와 값 범위가 달라지지 않게 한다.
- 설정 적용/취소가 `Time.timeScale=0`에서도 동작해야 한다.
- 설정 저장 실패 시 Root로 강제 이탈하지 말고 오류를 표시한다.

### 7단계: 조작 안내

실제 현재 바인딩만 표시한다.

- WASD: 이동
- Left Shift: 달리기
- Space: 대시
- F: 상호작용/대화 진행
- Q/E: 카메라 회전
- Mouse Wheel: 확대/축소
- Escape: 메뉴/뒤로
- 대응 게임패드 입력

텍스트가 패널을 벗어나지 않게 하고 1280x720에서도 읽을 수 있어야 한다.

### 8단계: 저장 명령

- 현재 `IPlayerSession.CurrentSave`가 없으면 버튼을 비활성화하거나 명확한 오류를 표시한다.
- 현재 체크포인트 ID를 유지해 `SaveGameProgressUseCase`를 호출한다.
- 성공하면 저장 완료 효과음을 한 번 재생하고 성공 문구를 표시한다.
- 실패하면 오류 코드 전체를 사용자에게 노출하지 말고 로그와 사용자 문구를 분리한다.
- 현재 스키마에 없는 재화/시간/임의 좌표를 저장 데이터에 추가하지 않는다.

### 9단계: 타이틀 복귀와 종료

- `ConfirmReturnToTitle`, `ConfirmQuit` 상태를 사용한다.
- 타이틀 복귀 확인 시 중복 호출을 막고 `ISceneFlowService.LoadFrontendAsync()`를 호출한다.
- 성공 경로에서 현재 세션을 정리한다.
- 실패하면 timeScale과 입력을 안전하게 복구하고 메뉴를 다시 표시한다.
- 게임 종료는 테스트 가능한 `IApplicationQuitter` 같은 경계를 두고 Player에서만 `Application.Quit()`을 수행한다.

### 10단계: 위치 HUD

`LocationVolume`과 `LocationTracker` 또는 동등한 구조를 구현한다.

- `LocationVolume`: 안정 ID, 상위 지역 표시명, 방 표시명, Trigger 범위
- `LocationTracker`: 플레이어 진입/이탈 우선순위와 현재 위치 결정
- `InGameHudView`: 상태 이벤트를 받아 텍스트만 갱신

연구실 방 매핑:

| ID | 표시 이름 |
|---|---|
| `reception` | 중앙 접수실 |
| `tax_office` | 세무 집행실 |
| `analysis_lab` | 세계선 분석실 |
| `archive` | 기록보관실 |
| `archive_annex` | 기록보관실 별관 |
| `restricted` | 격리실 |

- Trigger가 겹치면 명시적 priority로 결정한다.
- 이탈 순간 잠깐 빈 위치가 표시되지 않게 마지막 유효 위치를 유지한다.
- `GameObject.Find`나 이름 문자열 검색으로 방을 판정하지 않는다.

### 11단계: 재화·시간 확장 계약

- 읽기 전용 HUD 상태를 정의한다.
- 실제 공급자가 없을 때는 `HasCurrency=false`, `HasGameTime=false`처럼 빈 상태를 표현한다.
- 뷰는 빈 상태에서 `—`, `--:--`을 표시한다.
- 이벤트 기반 갱신을 우선하고 매 프레임 문자열을 재생성하지 않는다.
- 이후 재화/시간 구현이 들어와도 HUD View를 수정하지 않고 상태 소스만 교체할 수 있어야 한다.

### 12단계: 씬 빌더·검증·테스트

`WorldAdjustmentLabSceneBuilder`를 확장해 반복 실행해도 중복 없는 결과를 만든다.

필수 씬 참조:

- HUD Canvas/SafeArea Root
- 좌측 위치 패널
- 우측 상태 패널
- Pause Overlay/Menu View
- InGameUiCoordinator
- LocationTracker와 방별 LocationVolume
- SettingsService, SaveGameProgressUseCase, IPlayerSession, ISceneFlowService의 명시적 초기화 경로

검증:

- 중복 Canvas, EventSystem, AudioListener 없음
- Missing Script/Reference 없음
- 메뉴와 HUD 필수 참조 누락 시 빌더 검증 실패
- 씬의 LocationVolume ID 중복 금지
- 재실행 시 오브젝트 중복 없음

---

## 6. 시각 디자인 확정값

`AGENTS.md` 색상 토큰을 사용한다.

- 전체 딤: 검정 알파 `0.72`
- 메뉴 패널: `Surface #171C22`, 알파 `0.96`
- 테두리: `AccentGold #B99A59`
- 포커스: `Secondary #5CAECC`
- 기본 글자: `TextMain #EEE6D5`
- 보조 글자: `TextMuted #9AA6AF`
- 위험 확인: `AccentRed #7D1827`

레이아웃 기준:

- 중앙 메뉴 권장 크기: 폭 `520~620`, 높이 `680~780`
- 버튼 높이: `58~68`, 버튼 간격 `12~18`
- 좌측 HUD Safe Area 여백: `32~48`
- 우측 HUD Safe Area 여백: `32~48`
- 1280x720에서도 본문 최소 18px 상당 가독성 확보
- 코드명, AURA ID, 관리구역 번호 같은 임시 부제 금지

---

## 7. 테스트 요구사항

### EditMode

- 메뉴 상태 전이와 Back 우선순위
- Busy 중 중복 명령 거부
- Gate 토큰 중첩 및 정확한 해제
- 위치 ID 중복/priority 판정
- 빈 재화/시간 상태의 표시 모델
- 설정 취소/적용 흐름
- 저장 성공/실패 결과 처리

### PlayMode

- 탐험 중 Escape로 메뉴 열기
- 다시 Escape로 닫기
- 메뉴 중 이동·대시·상호작용·카메라 정지
- 닫은 뒤 모든 입력 정상 복구
- 대화 중 Escape는 대화만 닫음
- 설정/조작/확인창에서 Escape는 Root로 복귀
- 게임패드 Start/B/Confirm과 첫 선택
- 저장 버튼 연타 방지
- 타이틀 전환 실패 복구
- 방 이동 시 좌측 위치 텍스트 갱신
- 1280x720, 1920x1080, 2560x1440, 21:9에서 HUD와 중앙 메뉴가 화면 밖으로 나가지 않음
- HUD가 탐험 레이캐스트를 막지 않음

전체 시각 QA는 별도 최종 QA 단계에서 수행한다. 이번 구현에서는 기능 검증과 명백한 겹침/잘림만 확인한다.

---

## 8. 금지사항

- 새 패키지 설치
- `PlayerPrefs`에 메뉴/재화/시간 저장
- `FindObjectOfType`, `GameObject.Find`, Service Locator
- View에서 파일 IO 또는 씬 로드 직접 실행
- Escape 입력을 여러 컴포넌트가 경쟁해 소비
- 메뉴 닫을 때 무조건 `Time.timeScale=1`
- 재화와 시간을 임의 값으로 꾸며서 표시
- 기존 세이브 스키마를 근거 없이 확장
- 기존 사용자 변경 되돌리기
- 테스트를 통과시키기 위한 기능 삭제
- 커밋 또는 푸시

---

## 9. 완료 보고 형식

완료 시 다음 순서로 보고한다.

1. 구현된 사용자 흐름
2. 변경 파일 목록
3. 입력·timeScale·Gate 복구 방식
4. 위치 HUD와 재화/시간 확장 계약
5. 실행한 EditMode/PlayMode 테스트와 결과
6. Unity 컴파일/콘솔/Missing Reference 결과
7. 수동 확인이 필요한 항목
8. 남은 위험과 다음 한 작업
