# 개발 에이전트 전달용 구현 프롬프트

아래 내용을 새 개발 에이전트에게 그대로 전달한다.

```text
너는 Unity 6000.4.10f1 기반 PC 게임 DemonLord_v2의 시니어 Unity 개발자다.

가장 먼저 저장소 루트의 AGENTS.md, docs/architecture/BOOT_SAVE_FLOW.md,
docs/architecture/SAVE_DATA.md를 모두 읽고 지켜라. 기존 사용자 변경은 절대 되돌리거나
스테이징하지 마라.

목표는 다음 수직 슬라이스다.
게임 실행 → 로고/주의 문구 → 타이틀 인트로 → 메인 화면 → 게임 시작 방식 선택
→ 세이브 슬롯 선택 → 새 게임 설정 → 세이브 생성 또는 로드
→ 세이브 EntryId/CheckpointId에 따른 GameShell 진입.

고정 결정:
- 첫 씬: Assets/_Project/Scenes/00_Boot.unity
- 프런트엔드 씬: Assets/_Project/Scenes/10_Frontend.unity
- 검증용 목적지: Assets/_Project/Scenes/90_GameShell.unity
- Frontend 상태: LogoNotice, TitleIntro, MainMenu, StartMode, SaveSlots,
  NewGameSetup, Busy, ErrorDialog
- 슬롯: slot-01, slot-02, slot-03
- 시작 방식: Continue, NewGame
- 새 게임 설정: profileName(정리 후 1~16자), difficultyId(story/normal/hard),
  tutorialEnabled
- 초기 진입점: entryId=prologue_start, checkpointId=start
- 세이브에 씬 이름을 저장하지 않는다. IEntryPointResolver가 안정 ID를 목적지로 변환한다.
- Continue는 유효/호환 세이브가 있을 때만 활성화한다.
- 사용 중인 슬롯의 새 게임은 덮어쓰기 확인이 필수다.
- 저장 성공 전에는 씬 전환하지 않는다.
- 알 수 없는 EntryId는 기본값으로 진행하지 말고 오류 표시 후 슬롯 화면으로 돌아간다.
- 실제 세이브는 PlayerPrefs가 아니라 tmp 검증 → 최종 교체 → bak 보존 방식으로 구현한다.

설계 경계:
- Domain/Application은 UnityEngine 의존을 피하는 순수 C#이다.
- Infrastructure가 파일, JSON, checksum, 씬 로딩을 구현한다.
- Presentation은 UGUI View/Presenter만 가진다.
- Bootstrap이 명시적 composition root다.
- mutable static, Service Locator, Find 계열 탐색, 화면에서 직접 SceneManager 호출을 쓰지 않는다.
- manifest에 이미 존재하는 Input System/UGUI/Test Framework 외 패키지를 추가하지 않는다.
- 아트가 없으면 플레이스홀더 UGUI로 기능만 완성한다.

필수 인터페이스/유스케이스:
- ISaveRepository: 슬롯 요약, 전체 로드, 저장, 삭제
- ISaveMigrationPipeline
- IEntryPointResolver
- ISceneFlowService
- IPlayerSession
- IClock, IAppLogger
- FrontendCoordinator
- ListSaveSlotsUseCase, CreateNewGameUseCase, LoadGameUseCase

필수 EditMode 테스트:
- 상태 전이와 Back
- profileName/difficulty 검증
- Empty/Valid/Corrupt/Incompatible 분류
- checksum 오류 거부
- save.json 손상 시 save.bak 복구
- 저장 후 재로드 round-trip
- 알 수 없는 EntryId 거부
- CreateNewGame의 초기 EntryId/CheckpointId
- LoadGame 실패 시 PlayerSession 미오염

필수 수동/PlayMode 스모크 테스트:
A. 세이브 없는 첫 실행: Continue 비활성 → NewGame → 저장 → GameShell
B. 재실행: Continue → 기존 슬롯 → 동일 정보로 GameShell
C. 사용 중 슬롯: 덮어쓰기 취소 시 원본 유지, 확인 시 교체
D. 손상 세이브: Corrupt 표시 및 진입 불가
E. 빠른 연속 클릭: 이중 저장/이중 씬 로드 없음

완료 보고에는 1) 구현 흐름 2) 핵심 파일 3) 실행한 테스트와 결과
4) 수동 확인 5) 위험/미완료 6) 다음 정확한 작업을 적어라.
Unity 재컴파일 및 테스트 확인 전에는 완료라고 보고하지 마라.
```
