# 게임 진입 전 프런트엔드 후속 개발 프롬프트

아래 코드 블록을 새 개발 에이전트에게 그대로 전달한다. 이 프롬프트는 기존 부팅·세이브 수직 슬라이스를 다시 만드는 작업이 아니라, 현재 구현을 유지하면서 게임 진입 전 품질을 올리는 후속 작업이다.

```text
너는 Unity 6000.4.10f1 기반 PC 3D 게임 `DemonLord_v2`의 시니어 Unity 개발자다.

작업 저장소:
`C:\workspace\unity\DemonLord_v2`

이번 목표:
이미 구현된 아래 흐름을 깨뜨리지 않고, 실제 출시 전 프런트엔드에 가까운 품질로 개선한다.

게임 실행
→ 개발사/세계조정국 로고와 주의 문구
→ 타이틀 인트로
→ 메인 메뉴
→ Continue/New Game/Load Game
→ 세이브 슬롯
→ 새 게임 설정
→ 세이브 기반 GameShell 진입

게임플레이, 전투, 던전, 캐릭터 조작은 이번 범위가 아니다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
0. 작업 시작 전에 반드시 할 일
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1) 다음 파일을 UTF-8로 끝까지 읽어라.
- `AGENTS.md`
- `docs/agent/IMPLEMENTATION_PROMPT.md`
- `docs/agent/WORK_ORDERS.md`
- `docs/architecture/BOOT_SAVE_FLOW.md`
- `docs/architecture/SAVE_DATA.md`
- `Assets/_Project/Scripts/Presentation/FrontendView.cs`
- 관련 Domain/Application/Infrastructure/Bootstrap 코드와 기존 테스트

2) 다음을 확인하고 시작 상태를 보고하라.
- `git status --short`
- `git log --oneline -10`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- Build Settings의 씬 순서
- 현재 Unity 컴파일 결과와 기존 EditMode/PlayMode 테스트 결과

3) Git 안전 규칙:
- 사용자 변경과 관계없는 dirty 파일은 되돌리거나 수정하거나 스테이징하지 마라.
- 현재 로컬 히스토리를 reset/rebase/amend하지 마라.
- 단계별 관련 파일과 `.meta`만 명시적으로 스테이징하라.
- 각 단계의 수용 조건과 테스트가 통과한 뒤 별도 커밋하라.
- 사용자가 명시적으로 요청하기 전에는 push하지 마라.
- 프롬프트 작성 시점의 최신 로컬 커밋은 `495735b fix(frontend): improve menu panel and text contrast`다. 실제 상태는 Git으로 다시 확인하라.

4) 기술 제한:
- Unity 버전은 `6000.4.10f1`로 유지한다.
- 새 패키지를 추가하지 않는다.
- UGUI와 Input System을 유지한다.
- 텍스트를 문장별 이미지로 만들지 않는다. 메뉴 문구는 실제 폰트로 렌더링한다.
- `PlayerPrefs`, mutable global static, Service Locator, `FindObjectOfType`, `GameObject.Find`를 쓰지 않는다.
- View에서 파일 저장·검증·씬 목적지 결정을 하지 않는다.
- 씬 전환은 `ISceneFlowService`, 진입 해석은 `IEntryPointResolver`를 통한다.
- Domain/Application은 가능한 한 UnityEngine에 의존하지 않는다.
- 현재 `FrontendView` 전체를 한 번에 갈아엎지 마라. 동작을 보존하면서 공통 테마, 화면 View, 입력 처리 책임을 작은 단위로 추출한다.

5) 작업 진행 방식:
- 아래 1~6단계를 순서대로 수행한다.
- 한 단계가 컴파일되고 해당 테스트와 수동 확인을 통과하기 전에는 다음 단계로 가지 않는다.
- 막히지 않은 작업은 계속 진행하되, 에셋 하나가 없다는 이유로 전체 작업을 중단하지 마라.
- 임시 대응을 넣었다면 코드와 완료 보고에 명확히 표시한다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. 한글 폰트와 UI 테마 체계
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

목표:
현재 `LegacyRuntime.ttf` 중심의 임시 타이포그래피를 교체하고, 고딕 판타지 패널·아이콘과 읽기 쉬운 한글 문구가 조화를 이루게 한다.

구현 요구사항:
- 프로젝트에 사용 가능한 정식 라이선스의 한글 TTF/OTF가 있는지 먼저 찾는다.
- 폰트가 제공되어 있으면 `Assets/_Project/Resources/UI/Fonts` 또는 프로젝트 규칙에 맞는 위치로 가져오고 `.meta`를 함께 관리한다.
- 폰트가 아직 없다면 중앙 FontProvider/Theme 구조와 fallback만 구현한 뒤, 필요한 폰트 파일 1개를 정확한 경로와 함께 완료 보고에 요청한다. 임의 사이트에서 라이선스 불명 폰트를 내려받지 않는다.
- UGUI Dynamic Font를 사용해도 된다. TextMeshPro는 이미 사용 가능한 구성과 한글 글리프/SDF 검증이 가능할 때만 사용하며, 이를 위해 새 패키지를 추가하지 않는다.
- `FrontendUiTheme` 또는 동등한 중앙 테마를 만들고 다음 토큰을 한곳에서 관리한다.
  - 제목, 부제, 메뉴, 본문, 캡션의 폰트/크기/색상
  - 기본/포커스/비활성/경고/오류 색상
  - 그림자/외곽선
  - 공통 여백과 버튼 높이
- 메인 메뉴의 한글과 영문 보조 문구는 서로 겹치지 않게 별도 Text 요소로 배치한다.
- 모든 텍스트는 1280×720에서도 읽혀야 하며 배경과 충분한 명암 대비를 가져야 한다.
- 패널 중앙 장식이 늘어나거나 세로선이 생기지 않도록 현재 `Simple + preserveAspect` 동작을 유지하거나 더 안전한 레이아웃으로 교체한다.
- 화면마다 복제된 텍스트 생성 코드를 공통 팩토리/헬퍼로 모으되, 기능을 깨뜨리는 대규모 재작성은 하지 않는다.

수용 조건:
- 한글이 깨지거나 잘리지 않는다.
- 메인 메뉴의 번호/한글/영문이 아이콘과 겹치지 않는다.
- 비활성 항목도 비활성임을 알 수 있으면서 문구 자체는 읽힌다.
- 문구별 PNG를 새로 만들지 않는다.
- 기존 로고, 타이틀, 배경, 패널, 아이콘의 투명도가 유지된다.

권장 커밋:
`feat(frontend): add Korean typography and UI theme`

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
2. 환경 설정 화면과 별도 설정 저장
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

목표:
메인 메뉴의 `SETTINGS`를 실제 동작하게 만들고, 게임 세이브 슬롯과 분리된 사용자 환경 설정을 재실행 후에도 유지한다.

구현 요구사항:
- `FrontendScreen`에 Settings 화면/필요한 확인 상태를 추가하고 Back 규칙을 테스트한다.
- Settings 진입을 더 이상 disabled 처리하지 않는다.
- 설정 데이터와 저장 경계를 추가한다.
  - `GameSettings` 또는 동등한 순수 도메인 모델
  - `ISettingsRepository`
  - Load/Apply/Save/Reset 유스케이스
  - Unity 런타임 적용을 담당하는 Infrastructure 서비스
- 저장 위치는 `Application.persistentDataPath/Settings/settings.json`으로 하고 게임 세이브와 분리한다.
- `settings.tmp` 완전 작성·재검증 후 최종 파일 교체 방식으로 저장한다. 손상 시 안전한 기본값을 사용하고 사용자에게 복구 사실을 표시한다.
- MVP 설정 항목:
  - 오디오: Master, BGM, SFX 0~100
  - 화면: 전체 화면 모드, 해상도, VSync, 품질 프리셋
  - 접근성: UI 글자 크기 배율, 화면 흔들림 감소, 섬광 감소, 전환 애니메이션 감소
- `적용`, `취소`, `기본값`을 제공한다.
- 취소는 화면 진입 전 값으로 되돌린다.
- 오디오 설정은 현재 프런트엔드 BGM과 UI 효과음에 즉시 반영한다.
- 지원하지 않는 해상도/품질 값은 검증하고 안전한 값으로 정규화한다.
- 메인 메뉴 `EXIT`에는 확인 대화상자를 추가한다. Editor에서는 Play Mode를 강제 종료하지 말고, 빌드에서만 Application.Quit을 수행한다.
- `ARCHIVE`는 이번 범위에서 잠금 상태를 유지하되 `미구현` 또는 `추후 개방` 상태를 명확히 표시한다.

필수 테스트:
- 설정 값 범위 검증과 정규화
- JSON round-trip
- 손상된 settings.json의 기본값 복구
- Apply/Cancel/Reset 동작
- Settings 화면의 Back
- 게임 세이브 파일과 설정 파일이 서로 영향을 주지 않음

수용 조건:
- 앱 재실행 후 설정이 유지된다.
- BGM/SFX 볼륨이 즉시 변한다.
- 취소 시 원래 값이 복원된다.
- Settings와 Exit Confirm에서 키보드/마우스/게임패드 Back이 결정적으로 동작한다.

권장 커밋:
`feat(settings): add persistent frontend settings`

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
3. 세이브 슬롯 UI와 Continue 정책 완성
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

목표:
세 슬롯의 상태와 정보를 한눈에 구분하고, Continue/Load/New Game의 의미를 명확히 한다.

구현 요구사항:
- `SaveSlotSummary`에 UI가 실제로 필요한 요약 정보가 빠져 있다면 Domain/Mapper/Repository 경계를 통해 추가한다.
- 유효 슬롯 표시 항목:
  - 슬롯 번호
  - 프로필명
  - 난이도 표시명
  - 마지막 저장 시각(로컬 표시, 저장값은 UTC 유지)
  - 플레이 시간
  - 현재 Entry/Checkpoint의 사용자용 표시명
  - 백업 복구 여부 배지
- Empty/Valid/Corrupt/Incompatible를 색상과 문구로 명확히 구분한다.
- Corrupt/Incompatible 슬롯은 Load할 수 없지만 상태와 원인은 읽을 수 있어야 한다.
- New Game 모드에서 Valid 슬롯을 고르면 기존 덮어쓰기 확인을 유지한다.
- Continue는 슬롯 배열의 첫 항목이 아니라 `UpdatedAtUtc`가 가장 최신인 Valid/호환 슬롯을 결정적으로 선택한다. 시각이 같으면 슬롯 ID 오름차순으로 tie-break 한다.
- 유효 슬롯이 없으면 메인 메뉴 Continue는 비활성화하고 선택 시도도 막는다.
- Load Game은 슬롯 목록을 열고, Continue는 최신 유효 슬롯으로 바로 진입한다.
- 썸네일 데이터/이미지가 아직 없으면 고정 placeholder 영역만 만들고 세이브에 가짜 경로나 이미지 데이터를 저장하지 않는다.
- 슬롯 목록을 다시 열 때 최신 디스크 상태를 새로 읽는다.

필수 테스트:
- 최신 Valid 슬롯 선택과 tie-break
- Valid가 없을 때 Continue 비활성
- 플레이 시간/시각 포맷에 필요한 원본 데이터 전달
- Empty/Valid/Corrupt/Incompatible 표시 모델
- 백업 복구 슬롯 표시
- 덮어쓰기 취소 시 원본 유지

수용 조건:
- 세 슬롯의 상태가 시각적으로 즉시 구분된다.
- Continue와 Load Game의 동작 차이가 분명하다.
- 손상/미래 버전 슬롯을 빈 슬롯처럼 취급하지 않는다.

권장 커밋:
`feat(save-ui): complete slot summaries and continue policy`

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
4. 튜토리얼 3단계 데이터 계약과 마이그레이션
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

목표:
현재 UI의 `상세 안내 / 핵심 안내 / 사용 안 함` 3개 선택을 bool 하나로 저장해 정보가 사라지는 문제를 해결한다.

고정 결정:
- 안정 ID는 `detail`, `core`, `off`다.
- Domain에는 문자열 남발 대신 `TutorialMode` 값 객체 또는 동등한 검증 타입을 둔다.
- 신규 기본값은 `detail`이다.
- 기존 v1 `tutorialEnabled=true`는 정보가 이미 손실되어 있으므로 `detail`로 마이그레이션한다.
- 기존 v1 `tutorialEnabled=false`는 `off`로 마이그레이션한다.

구현 요구사항:
- `NewGameSettings.TutorialEnabled` bool을 `TutorialMode` 계약으로 교체한다.
- 세이브 payload v2에는 `tutorialMode` 안정 ID를 저장한다.
- `SaveSchema.CurrentVersion`을 올리고 명시적 `v1 → v2` 순차 마이그레이션을 구현한다.
- v1 원본은 마이그레이션 성공 전 덮어쓰지 않는다.
- checksum/schema/slot 검증 순서를 기존 저장 안전성 규칙에 맞게 유지한다.
- 미래 버전은 계속 Incompatible로 처리하고 절대 덮어쓰지 않는다.
- GameShell 검증 화면에도 tutorialMode를 표시해 실제 전달을 확인할 수 있게 한다.
- `docs/architecture/SAVE_DATA.md`와 관련 프롬프트/설계 문서의 bool 계약을 v2 계약으로 갱신한다.

필수 테스트:
- detail/core/off 생성과 잘못된 ID 거부
- v2 저장/로드 round-trip
- v1 true → detail
- v1 false → off
- v1 마이그레이션 중 실패 시 원본 보존
- 미래 버전 거부
- 신규 게임 세이브에 선택한 3단계 값이 그대로 유지됨

수용 조건:
- `핵심 안내`를 선택해 저장·재로드해도 `core`가 유지된다.
- 기존 v1 세이브를 계속 읽을 수 있다.
- bool 기반 임시 변환 `tutorialSelection != 2`가 제거된다.

권장 커밋:
`feat(save): migrate tutorial preference to three modes`

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
5. 화면 전환·로딩·입력 잠금·오디오 페이드
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

목표:
화면이 즉시 튀거나 연속 입력으로 중복 실행되는 느낌을 없애고, 실패 시 안전하게 복구한다.

구현 요구사항:
- 공통 전체 화면 Fade Overlay를 만들고 화면 진입/이탈을 한 경로로 처리한다.
- 페이드는 `Time.unscaledDeltaTime`을 사용한다.
- Settings의 `전환 애니메이션 감소`가 켜지면 시간을 0 또는 매우 짧게 만든다.
- 전환, 저장, 로드, 씬 로드 중에는 버튼·Back·Submit의 중복 입력을 잠근다.
- 로고/인트로는 이벤트가 누락돼도 기존 최대 시간 후 계속 진행한다.
- 로딩 화면은 spinner/상태 문구를 사용한다. 실제 진행률을 얻지 못하면 가짜 0~100% 수치를 표시하지 않는다.
- Frontend BGM은 GameShell 진입 시 자연스럽게 fade-out한다.
- 씬 로드 실패를 Console 로그로만 끝내지 말고, `IPlayerSession`을 안전하게 정리하고 ErrorDialog 또는 안전한 이전 화면으로 돌아간다.
- 비동기 완료 후 파괴된 View를 접근하지 않도록 수명주기를 방어한다.
- 빠른 연속 클릭으로 저장/로드/씬 전환이 두 번 시작되지 않게 한다.

필수 테스트/검증:
- 중복 Submit 시 한 번만 실행
- Busy 중 Back 정책
- 씬 로드 실패 시 세션 정리와 UI 복귀
- Reduced Motion 적용
- 로고/타이틀 timeout fallback
- BGM/UI SFX가 전환 중 비정상 중첩되지 않음

수용 조건:
- 모든 화면 전환에서 입력이 결정적이다.
- 로딩 실패 후 검은 화면이나 영구 Busy에 갇히지 않는다.
- `Display 1 No cameras rendering`, 중복 AudioListener, 중복 EventSystem 경고가 없다.

권장 커밋:
`feat(frontend): add safe transitions and loading feedback`

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
6. 입력·접근성·해상도 QA와 최종 정리
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

목표:
마우스 없이도 전체 프런트엔드를 사용할 수 있고, 일반 PC 해상도에서 레이아웃이 깨지지 않게 한다.

입력 요구사항:
- 키보드: 방향키/WASD 이동, Enter/Space 확정, Escape 취소
- 게임패드: D-pad/Stick 이동, South 확정, East 취소
- 마우스: hover, click
- 타이틀의 `아무 버튼`은 키보드/마우스/게임패드 입력을 모두 인식한다.
- 화면을 열 때 항상 적절한 첫 interactable을 `EventSystem`에 선택한다.
- 포커스 이동 후 선택 표시가 명확하고 화면 재렌더 뒤 포커스가 사라지지 않는다.
- hover/select SFX가 같은 이동에서 중복 재생되지 않는다.
- 비활성 버튼은 포커스 탐색에서 건너뛰되 비활성 이유는 화면에서 읽힌다.

접근성/레이아웃 요구사항:
- 1920×1080, 2560×1440, 3440×1440, 창 모드 1280×720에서 확인한다.
- 기준 UI는 16:9 safe content 영역 안에 유지하고 울트라와이드 배경만 확장한다.
- 텍스트 크기 배율을 올려도 버튼 문구가 잘리거나 아이콘과 겹치지 않는다.
- 섬광 감소/전환 감소 설정이 로고·타이틀·메뉴 전환에 실제 적용된다.
- 색상만으로 선택/오류/비활성 상태를 전달하지 않는다. 문구나 아이콘/외곽선을 함께 쓴다.

최종 회귀 시나리오:
A. 세이브 없는 첫 실행
- 로고/주의 → 타이틀 → Continue 비활성 → New Game → 빈 슬롯 → 설정 → 저장 → GameShell

B. 재실행 Continue
- 설정과 세이브 유지 → 최신 Valid 슬롯 자동 선택 → 동일 정보로 GameShell

C. Load Game
- 슬롯 3개 상태 표시 → Valid만 진입 가능 → Back 정상

D. 덮어쓰기
- 취소 시 원본 유지 → 확인 시 새 데이터 저장 → 완료 SFX → 한 번만 씬 전환

E. 손상/호환 불가
- Corrupt/Incompatible를 Empty와 구분 → 진입 차단 → 오류/복구 안내

F. 입력 장치
- 위 전체 흐름을 키보드 전용, 마우스 전용, 게임패드 전용으로 각각 확인

G. 실패 복구
- 설정 파일 손상, 세이브 본 파일 손상과 bak 복구, 씬 로드 실패에서 영구 Busy가 없음

최종 품질 기준:
- Unity 재컴파일 오류 0
- Console 예외/오류 0
- Missing Script/Missing Reference 0
- 중복 Camera/AudioListener/EventSystem 0
- 기존 EditMode 테스트 통과
- 새 EditMode/PlayMode 테스트 통과
- 관계없는 dirty 파일을 커밋하지 않음

권장 커밋:
`test(frontend): verify pre-game flow and accessibility`

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
완료 보고 형식
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

각 단계가 끝날 때 다음 형식으로 짧게 보고하고, 테스트가 통과하면 다음 단계로 계속 진행하라.

1. 완료한 단계와 사용자 흐름 변화
2. 변경 파일
3. 실행한 자동 테스트와 결과
4. 수동 확인 해상도/입력 장치와 결과
5. 생성한 커밋 해시
6. 남은 위험 또는 필요한 외부 에셋
7. 다음 단계

모든 단계가 끝난 최종 보고에는 다음도 포함한다.
- 최종 커밋 목록
- push하지 않았는지 여부
- Unity Editor에서 사용자가 직접 확인할 정확한 재생 순서
- 아직 범위 밖인 항목(Archive 실제 내용, 실제 게임플레이, 클라우드 세이브 등)

중요:
- 컴파일이나 테스트를 실제로 실행하지 않았다면 실행했다고 쓰지 마라.
- Unity Editor 자동화가 불가능하면 가능한 컴파일/정적 검증을 수행하고, 수동 확인이 필요한 항목을 정확히 분리해 보고하라.
- 한 단계에서 문제가 발견되면 원인을 해결하고 회귀 테스트한 뒤 다음 단계로 넘어가라.
```

## 권장 사용법

한 에이전트에게 전체 작업을 맡길 때는 위 코드 블록 전체를 전달한다. 단계별로 나눠 맡길 때는 전체 프롬프트와 함께 다음 한 줄을 덧붙인다.

```text
이번 작업에서는 N단계만 수행하고 커밋한 뒤 멈춰라. 이후 단계는 선행 구현하지 마라.
```
