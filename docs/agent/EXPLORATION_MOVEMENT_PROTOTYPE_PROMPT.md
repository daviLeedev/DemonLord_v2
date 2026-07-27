# 쿼터뷰 탐색 이동 프로토타입 개발 프롬프트

아래 코드 블록을 개발 에이전트에게 그대로 전달한다. 이 작업은 최종 아트나 전투를 만드는 작업이 아니라, 기존 세이브 기반 `90_GameShell` 진입 뒤 실제로 이동하고 NPC와 상호작용할 수 있는 첫 게임플레이 프로토타입을 만드는 작업이다.

```text
너는 Unity 6000.4.10f1 기반 PC 게임 `DemonLord_v2`의 시니어 Unity 게임플레이 개발자다.

작업 저장소:
`C:\workspace\unity\DemonLord_v2`

이번 목표:
기존 부팅·프런트엔드·세이브 흐름을 보존하면서, 세이브의 EntryId/CheckpointId로 `90_GameShell`에 진입한 직후 아래 기능을 실제로 조작 가능한 수준까지 구현한다.

- 정사영 쿼터뷰 3D 공간
- 카메라 기준 WASD 8방향 자유 이동
- 걷기, Shift 달리기, Space 대시
- 캐릭터 추적 카메라
- Q/E 90도 카메라 회전
- 마우스 휠 확대·축소
- 전방의 상호작용 대상 자동 선택
- 선택 표시와 `F 대화`/`F 조사` 프롬프트
- NPC 임시 대화
- 대화 중 이동 잠금, 서로 마주보기, 대화 카메라 전환과 복귀
- 특정 방 진입 시 카메라 기본 각도 변경
- 평지, 벽, 완만한 경사, 짧은 계단에서 이동 검증

이 작업에 대한 사용자의 확정 결정:

- 그래픽 최종 방식은 아직 결정하지 않았다.
- 기반 좌표계·충돌·카메라는 3D로 만든다.
- 캐릭터 외형은 나중에 3D 모델 또는 8방향 2D 스프라이트로 교체할 수 있어야 한다.
- 화면은 이미지처럼 원근 왜곡이 적은 정사영 쿼터뷰다.
- 이동은 타일 단위가 아니라 자유 이동이며, 키보드 조합 기준 8방향이다.
- 이동 방향은 월드 북쪽이 아니라 현재 카메라 화면 기준이다.
- 캐릭터는 마지막으로 이동한 8방향을 바라본다.
- 카메라는 캐릭터를 부드럽게 따라가며 기본 각도는 고정이다.
- Q/E로 좌우 90도씩 부드럽게 회전한다.
- 마우스 휠로 확대·축소한다.
- 특정 방에서는 지정된 기본 카메라 각도로 바뀌지만 Q/E 회전은 계속 허용한다.
- NPC 대화 중에는 플레이어 이동과 자유 카메라 입력을 잠근다.
- 상호작용 범위 안에서 전방에 있고 가장 적합한 대상이 자동 선택된다.
- 선택된 대상에는 명확한 표시와 F 프롬프트가 나타난다.
- F로 대화/조사를 실행한다.
- 걷기, 달리기, 짧은 쿨다운형 대시가 필요하다.
- 점프는 이번 범위가 아니다.
- 전투는 추후 별도 화면/씬에서 구현하며 이번 범위가 아니다.
- 첫 프로토타입의 게임플레이 입력은 키보드·마우스만 지원한다.
- 커밋과 푸시는 하지 않는다.

`AGENTS.md`의 기존 문구가 게임플레이를 현재 범위 밖이라고 설명하더라도, 이번 사용자 지시는 그 범위를 오직 이 탐색 프로토타입까지 확장한다. 전투·인벤토리·퀘스트 등으로 더 넓히지 마라.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
0. 작업 시작 전 필수 확인
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1) 다음 파일을 UTF-8로 끝까지 읽어라.

- `AGENTS.md`
- `docs/agent/IMPLEMENTATION_PROMPT.md`
- `docs/architecture/BOOT_SAVE_FLOW.md`
- `docs/architecture/SAVE_DATA.md`
- `Assets/_Project/Scripts/Bootstrap/AppRoot.cs`
- `Assets/_Project/Scripts/Bootstrap/UnitySceneFlowService.cs`
- `Assets/_Project/Scripts/Presentation/GameShellSessionView.cs`
- `Assets/_Project/Scenes/90_GameShell.unity`
- `Assets/InputSystem_Actions.inputactions`
- 관련 asmdef와 기존 EditMode/PlayMode 테스트

2) 다음 상태를 확인하고 시작 보고를 남겨라.

- `git status --short`
- `git log --oneline -10`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- Build Settings의 씬 순서
- 현재 Unity 컴파일 결과
- 기존 EditMode/PlayMode 테스트 결과

3) 저장소 안전 규칙:

- 현재 작업 트리는 이미 사용자 변경과 프런트엔드 미커밋 작업을 포함할 수 있다.
- 사용자 변경을 reset, checkout, restore, clean, stash하지 마라.
- 관계없는 파일을 수정하거나 포맷하지 마라.
- `Library`, `Temp`, `Logs`, `UserSettings`와 IDE 산출물을 작업 결과에 포함하지 마라.
- 새 패키지를 추가하지 마라.
- Unity 버전 `6000.4.10f1`을 변경하지 마라.
- 작업 완료 후에도 `git add`, `git commit`, `git push`를 실행하지 마라.
- 최종 보고에서 이번 작업으로 변경한 파일만 별도로 열거하라.

4) 금지 사항:

- `FindObjectOfType`, `GameObject.Find`, 범용 Service Locator를 사용하지 마라.
- mutable global static 상태를 만들지 마라.
- 플레이어 위치나 설정을 `PlayerPrefs`에 저장하지 마라.
- 입력을 여러 MonoBehaviour에서 직접 폴링해 흩뿌리지 마라.
- 최종 캐릭터 아트가 없는 것을 이유로 외부 에셋을 임의 다운로드하지 마라.
- Cinemachine 등 새 패키지를 설치하지 마라. 현재 패키지로 카메라 리그를 구현하라.
- 하나의 거대한 `GameShellController`에 입력·이동·카메라·상호작용·대화를 모두 넣지 마라.
- 런타임 코드에서 매번 전체 테스트 맵을 절차적으로 생성하는 방식으로 끝내지 마라. 테스트 공간은 씬/프리팹으로 직렬화한다.
- 기존 프런트엔드와 세이브 구조를 다시 작성하지 마라.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. 고정 기술 방향과 구조
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

기반 방식은 `정사영 쿼터뷰 3D`로 확정한다.

- 월드 이동 평면은 XZ, 높이는 Y다.
- 플레이어 충돌은 3D `CharacterController`를 우선 사용한다.
- 카메라는 `Camera.orthographic=true`인 자체 쿼터뷰 카메라 리그를 구현한다.
- Input System을 사용한다.
- Unity 물리, 입력, 카메라와 결합된 컴포넌트는 Presentation 또는 명확한 Gameplay/Exploration 영역에 둔다.
- Domain/Application에 UnityEngine 의존성을 새로 퍼뜨리지 마라.
- 플레이어 논리 루트와 외형 루트를 분리한다. 이동기는 캡슐/콜라이더를 제어하고, 마지막 방향 표현은 별도 visual root/adapter가 담당한다.
- 최종 외형이 3D이면 visual adapter가 3D Animator를 구동하고, 2D이면 같은 8방향 상태를 스프라이트 선택에 사용할 수 있어야 한다.

권장 책임 분리. 이름은 프로젝트 관례에 맞게 조정할 수 있지만 책임을 합치지 마라.

- `ExplorationInputReader`: Move/Sprint/Dash/Interact/CameraRotate/Zoom 입력의 단일 진입점
- `PlayerMotor`: CharacterController 기반 걷기·달리기·중력·대시
- `PlayerFacing`: 마지막 유효 이동 입력을 8방향으로 양자화
- `PlayerVisualPresenter` 또는 동등 구조: 외형 루트 회전/향후 2D·3D 어댑터 경계
- `QuarterViewCameraRig`: 추적·정사영 줌·90도 회전·프로필 전환
- `CameraZone`: 방 진입/퇴장 시 카메라 기본 프로필 적용
- `InteractionSensor`: 후보 수집·점수 계산·가시선 검사·현재 선택 결정
- `IInteractable` 또는 동등 계약: 표시명, 동작명, 포커스 위치, 상호작용 실행
- `InteractionPromptView`: 선택 표시와 F 프롬프트
- `DialogueFocusController`: 입력 잠금·서로 마주보기·대화 카메라·상태 복원
- `GameShellRoot`: 씬의 composition root. 직렬화된 참조로 위 컴포넌트를 연결하고 세이브 진입 컨텍스트를 받는다.

입력 잠금은 단일 bool을 여러 곳에서 덮어쓰는 방식보다 소유자가 명확한 gate/token 또는 동등한 구조를 사용한다. 대화 종료나 오브젝트 비활성화 시 잠금이 남지 않도록 반드시 복구한다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
2. 기존 세이브 진입과 GameShell 연결
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

현재 흐름을 보존한다.

```text
Boot
→ Frontend
→ 새 게임 생성 또는 기존 세이브 로드
→ IEntryPointResolver
→ EntryDestination(sceneKey="90_GameShell", spawnKey="start")
→ 90_GameShell
```

구현 요구사항:

- `UnitySceneFlowService.LoadEntryAsync(EntryDestination)`가 씬 로드 후 GameShell root에 `IPlayerSession`과 `EntryDestination`을 명시적으로 전달하게 한다.
- 현재 `GameShellSessionView`의 세션 표시 기능은 필요한 경우 개발용 진단 UI로 축소하거나 `development build/editor only`로 제한할 수 있다.
- `GameShellRoot` 또는 동등한 루트가 씬에 직렬화된 플레이어, 카메라, UI, 스폰 지점 참조를 가진다.
- `spawnKey="start"`에 대응하는 명시적 SpawnPoint를 만든다.
- 플레이어를 스폰 지점에 배치한 뒤 카메라가 첫 프레임부터 올바른 위치를 보도록 snap 초기화한다.
- 알 수 없는 spawnKey를 조용히 `(0,0,0)`으로 대체하지 마라. 명확한 오류를 내고 플레이어 조작을 시작하지 마라.
- 씬 루트 검색은 현재 서비스처럼 로드한 씬의 root objects 범위에서 명시적으로 수행할 수 있다. 전역 씬 검색은 금지한다.

수용 조건:

- Boot에서 새 게임 또는 Load/Continue를 통해 GameShell에 들어가도 동일한 플레이어 프로토타입이 초기화된다.
- `start` 위치에서 플레이가 시작된다.
- 활성 세이브 세션이 유지된다.
- Missing Reference, Missing Script, 초기화 순서 예외가 없다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
3. 입력과 플레이어 이동
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

전용 Exploration action map을 사용하거나 기존 Input Actions를 안전하게 확장한다. 기존 UI 입력을 깨뜨리지 마라.

필수 액션과 키:

| Action | 입력 | 동작 |
|---|---|---|
| Move | WASD | 카메라 기준 Vector2 이동 |
| Sprint | Left Shift hold | 달리기 |
| Dash | Space press | 현재 이동/마지막 방향으로 대시 |
| Interact | F press | 선택된 대상 상호작용 |
| CameraRotateLeft | Q press | 카메라 90도 좌회전 |
| CameraRotateRight | E press | 카메라 90도 우회전 |
| CameraZoom | Mouse Scroll Y | 정사영 확대·축소 |

- Interact는 Hold가 아니라 Press 동작이어야 한다.
- 첫 프로토타입에서는 게임패드 바인딩을 요구하지 않는다.
- 향후 리바인딩이 가능하도록 하드코딩된 `Keyboard.current` 검사를 여러 시스템에 넣지 마라.
- 입력 활성화/비활성화와 이벤트 구독 해제 수명주기를 명확히 처리한다.

이동 규칙:

- 카메라 forward/right를 XZ 평면으로 투영해 화면 기준 이동 벡터를 만든다.
- 카메라 pitch가 있어도 수직 성분이 이동 속도에 섞이지 않아야 한다.
- 대각선 입력을 정규화해 직선보다 빠르지 않게 한다.
- 입력이 있는 동안 visual root는 마지막 이동 방향 8방향 중 하나를 바라본다.
- 입력이 0이 되면 마지막 방향을 유지한다.
- CharacterController의 중력을 안정적으로 적용해 경사/계단에서 뜨거나 흔들리지 않게 한다.
- 점프, 낙하 대미지, 공중 제어는 구현하지 않는다.
- 벽에 대시할 때 관통하지 않아야 한다.
- 대화 또는 외부 입력 잠금 상태에서는 걷기·달리기·대시·상호작용이 실행되지 않아야 한다.

초기 기본값. 반드시 Inspector 또는 설정 데이터에서 조정 가능하게 한다.

- walkSpeed: `3.0 m/s`
- sprintSpeed: `5.5 m/s`
- rotation/visual facing: 8방향, 45도 단위
- dashDistance: 약 `3.0 m`
- dashDuration: `0.18 s`
- dashCooldown: `0.65 s`
- slopeLimit: 약 `45°`
- stepOffset: 약 `0.3 m`, 실제 캡슐 크기에 맞게 조정

대시 규칙:

- 이동 입력이 있으면 현재 이동 방향을 사용한다.
- 입력이 없으면 마지막으로 바라본 방향을 사용한다.
- 대시 도중 방향을 급격히 바꾸지 않는다.
- 대시는 짧은 시간 기반 이동이며 스태미나와 무적 시간은 없다.
- 대시 종료 후 정상 이동으로 확실히 복귀한다.
- 쿨다운 중 입력은 무시하며 대시 코루틴/상태가 중첩되지 않는다.

수용 조건:

- 카메라가 어떤 90도 회전 상태여도 W는 화면 위쪽으로 이동한다.
- W+D가 W보다 빠르지 않다.
- 걷기/달리기 속도 차이가 명확하다.
- 대시 거리와 쿨다운이 프레임레이트 변화에 과도하게 달라지지 않는다.
- 벽, 경사, 계단에서 떨림·관통·비정상 부유가 없다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
4. 쿼터뷰 카메라
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

초기 권장값:

- Projection: Orthographic
- 기본 yaw: `45°`
- 기본 pitch: `35°` 전후
- 회전 단위: `90°`
- 회전 시간: `0.25 s` 전후
- follow damping: `0.12 s` 전후
- orthographic size 기본값: `8`
- zoom 범위: `6 ~ 12`
- zoom smoothing: `0.1 ~ 0.2 s`

구현 요구사항:

- 추적 target과 pivot을 분리한다.
- 플레이어를 부드럽게 따라가되 씬 진입 직후에는 보간 전 위치가 노출되지 않도록 snap한다.
- Q/E를 누르면 가장 가까운 90도 기준 방향으로 정확히 수렴한다.
- 회전 입력을 연속으로 눌러도 각도가 누적 오차로 틀어지지 않는다.
- 줌은 정사영 size를 제한 범위 안에서 부드럽게 변경한다.
- 화면 해상도와 무관하게 플레이어가 기본적으로 안정된 구도에 위치한다.
- 이번 단계에서는 카메라 벽 가림 해결을 필수로 구현하지 않는다. 다만 이후 반투명/충돌 처리를 추가할 경계를 막지 마라.

Camera Zone 요구사항:

- BoxCollider trigger 기반의 명시적인 zone을 만든다.
- zone profile은 기본 yaw/pitch/orthographic size/follow offset/transition duration을 가질 수 있다.
- 진입 시 zone 기본 각도로 부드럽게 이동한다.
- zone 안에서도 Q/E 90도 추가 회전과 휠 줌을 허용한다.
- 퇴장 시 이전 기본 profile로 복귀한다.
- zone이 겹치면 priority 또는 가장 최근 진입 등 결정적 정책을 코드와 문서에 명시한다.
- 플레이어 외 Collider가 zone을 발동하지 않게 한다.

수용 조건:

- 추적, Q/E 회전, 휠 줌이 동시에 안정적으로 작동한다.
- 카메라 회전 직후에도 이동 방향이 화면 기준으로 즉시 맞는다.
- zone 진입·퇴장을 반복해도 카메라 상태가 누적되거나 잘못 복원되지 않는다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
5. 상호작용 대상 선택과 프롬프트
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

상호작용 후보는 매 프레임 할당을 과도하게 만드는 방식 대신 Trigger 후보 등록 또는 `Physics.OverlapSphereNonAlloc` 등 명확하고 제한된 방식으로 관리한다.

초기 권장값:

- interactionRadius: `2.2 m`
- forwardCone: 전체 `100°` 전후
- lineOfSightMask: 벽/고정 지형 포함
- refresh: 매 프레임이어도 후보 수가 제한되도록 설계

선택 규칙:

1. 활성 상태이며 상호작용 가능한 후보만 남긴다.
2. 반경 밖 후보를 제거한다.
3. 플레이어의 마지막 facing 기준 전방 cone 밖 후보를 제거한다.
4. 플레이어와 대상 사이 가시선이 벽에 막히면 제거한다.
5. 남은 후보를 각도 우선 + 거리 보조 점수로 정렬해 하나를 결정한다.
6. 동점일 때 인스턴스 탐색 순서에 의존하지 않는 결정적 tie-break를 사용한다.

표시 요구사항:

- 현재 선택 대상에만 간단한 링, 마커 또는 발광 표시가 보인다.
- 외부 최종 아트 없이 Unity 기본 도형/간단한 프로젝트 내부 머티리얼로 구현한다.
- 화면 또는 대상 근처에 `F 대화`, `F 조사` 같은 프롬프트를 표시한다.
- 선택이 바뀌거나 대상이 비활성화되면 이전 표시가 즉시 해제된다.
- 대화 중에는 프롬프트를 숨긴다.

상호작용 계약에는 최소한 다음 의미가 있어야 한다.

- 사용자에게 보일 대상명
- 사용자에게 보일 동작명: 대화/조사 등
- 선택 표시와 카메라가 사용할 focus point
- 현재 상호작용 가능 여부
- 상호작용 실행 결과 또는 실행 콜백

수용 조건:

- 가까워도 뒤쪽 대상은 선택되지 않는다.
- 벽 너머 대상은 선택되지 않는다.
- 후보가 둘 이상이어도 하나만 선택된다.
- F를 한 번 눌렀을 때 상호작용이 한 번만 발생한다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
6. NPC 임시 대화와 대화 카메라
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

이번 단계는 최종 대화 시스템이 아니다. 데이터베이스, 분기, 로컬라이제이션 툴을 만들지 말고, 이동과 카메라 잠금 계약을 검증할 수 있는 작은 placeholder dialogue를 만든다.

필수 동작:

1. NPC가 현재 선택된 상태에서 F를 누른다.
2. 플레이어 이동·달리기·대시·일반 상호작용을 잠근다.
3. 플레이어와 NPC가 수평면에서 서로 마주본다.
4. NPC의 dialogue camera anchor 또는 계산된 투샷 위치로 부드럽게 전환한다.
5. NPC 이름과 1~2줄의 임시 대사를 UGUI로 표시한다.
6. F 또는 Enter로 다음 줄을 진행한다.
7. 마지막 줄 또는 Escape에서 대화를 닫는다.
8. 대화 전 카메라 profile/yaw/zoom 상태를 정확히 복원한다.
9. 입력 잠금을 해제하고 탐색 프롬프트를 다시 평가한다.

대화 중에는 Q/E 회전과 휠 줌을 잠근다. NPC나 대화 UI가 도중에 비활성화되더라도 카메라와 입력 잠금이 남지 않도록 종료 경로를 방어한다.

수용 조건:

- 대화 중 플레이어가 움직이거나 대시하지 않는다.
- 플레이어와 NPC가 수평면에서 서로 마주본다.
- 대화를 여러 번 반복해도 입력 이벤트가 중복되지 않는다.
- 대화 종료 후 이전 카메라 각도와 줌이 복구된다.
- 대화 종료 후 바로 이동과 상호작용이 가능하다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
7. 90_GameShell 그레이박스 테스트 공간
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

`Assets/_Project/Scenes/90_GameShell.unity`를 실제 이동 검증 씬으로 확장한다.

필수 구성:

- GameShell composition root
- start SpawnPoint
- 플레이어 placeholder와 CharacterController
- 정사영 QuarterView Camera
- Directional Light와 필요한 URP 기본 조명
- 충분한 크기의 평지
- 충돌 가능한 벽과 모서리
- 완만한 경사로
- CharacterController가 오를 수 있는 짧은 계단
- 상호작용 가능한 NPC 2명
- 조사 가능한 오브젝트 1개
- 카메라 profile이 바뀌는 방/영역 1개
- Interaction Prompt와 placeholder Dialogue UI Canvas

placeholder 외형:

- 플레이어와 NPC는 Capsule/Cube 등 기본 primitive와 프로젝트 내부 단색 머티리얼을 사용한다.
- 플레이어의 앞 방향을 알 수 있도록 작은 화살표·코·색상 면 등 명확한 표시를 둔다.
- 최종 아트처럼 꾸미는 데 시간을 사용하지 마라.
- 최종 모델/스프라이트가 들어갈 visual root와 교체 지점을 명확히 둔다.

씬/프리팹 작성 원칙:

- 가능한 요소는 재사용 가능한 prefab으로 만든다.
- 씬 YAML을 대규모로 손으로 추측해 작성하지 마라.
- Unity Editor에서 직접 구성하거나, 필요하면 idempotent Editor authoring 도구로 생성한 뒤 씬/프리팹을 저장하고 실제 결과를 다시 열어 검증한다.
- 런타임에 매번 테스트 월드 전체를 코드로 생성하는 구현을 최종 결과로 남기지 마라.
- 생성한 모든 Asset의 `.meta`를 보존한다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
8. 테스트
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

테스트하기 쉬운 계산은 MonoBehaviour에서 분리해 순수 함수/작은 클래스로 검증한다.

필수 EditMode 테스트:

- 카메라 yaw별 화면 기준 이동 벡터 계산
- 대각선 이동 정규화
- 마지막 방향의 8방향 양자화
- 입력이 없을 때 마지막 facing 유지
- 대시 방향 선택: 현재 입력 우선, 없으면 마지막 방향
- 대시 cooldown/state 중복 방지
- 상호작용 후보의 각도·거리 점수
- 전방 cone 밖 후보 제외
- 결정적 tie-break
- 알 수 없는 spawnKey 거부

필수 PlayMode 테스트 또는 자동화 가능한 스모크 테스트:

- GameShell root 초기화와 start 스폰
- 평지에서 걷기/달리기
- 대각선 속도 보정
- 벽 충돌과 대시 관통 방지
- 경사/계단 이동
- 카메라 follow, 90도 회전, zoom clamp
- 카메라 회전 후 화면 기준 이동
- CameraZone 진입/퇴장과 복원
- NPC 자동 선택과 F 상호작용 1회
- 대화 중 이동 잠금
- 대화 종료 후 카메라·입력 복원

Unity Test Framework 특성상 외부 `dotnet test`가 테스트를 실제 실행하지 못할 수 있다. Unity Editor 또는 batchmode Test Runner로 실제 테스트 실행 여부와 결과 XML을 확인하라. `dotnet build`만 성공한 것을 Unity 테스트 통과라고 보고하지 마라.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
9. 단계별 구현 순서
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

아래 순서를 지키고 각 단계에서 컴파일과 최소 검증을 수행한다.

1) 기존 GameShell 진입 분석, GameShellRoot/EntryDestination/spawn 연결
2) 전용 입력 경계와 CharacterController 기반 이동·facing·대시
3) QuarterViewCameraRig의 follow/회전/zoom
4) 그레이박스 공간, 경사, 계단, 벽 구성
5) Interaction 계약, 후보 선택, 선택 표시, F 프롬프트
6) placeholder NPC 대화, 입력 잠금, 대화 카메라와 복원
7) CameraZone
8) EditMode/PlayMode 테스트와 해상도별 수동 QA
9) 관련 설계 문서 갱신과 완료 보고

한 단계가 실패한 상태에서 뒤 단계의 기능을 얹지 마라. 다만 최종 아트가 없다는 이유로 작업을 중단하지 말고 placeholder로 계속 진행한다.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
10. 전체 수용 조건
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

다음 시나리오가 실제 Play Mode에서 동작해야 한다.

```text
00_Boot 실행
→ Frontend 진입
→ New Game 또는 Continue/Load
→ 90_GameShell의 start SpawnPoint 진입
→ WASD 이동
→ Shift 달리기
→ Space 대시
→ Q/E 카메라 회전
→ 휠 확대·축소
→ NPC 접근
→ NPC 자동 선택 및 F 프롬프트
→ F 대화
→ 이동 잠금·대화 카메라
→ 대화 종료
→ 이전 카메라 복원 및 이동 재개
→ CameraZone 진입·퇴장
```

품질 기준:

- Console 컴파일 오류와 런타임 예외 0개
- Missing Script/Reference 0개
- 1280×720, 1920×1080에서 카메라와 프롬프트가 정상
- 30/60/120fps에서 이동·대시 체감이 과도하게 달라지지 않음
- 카메라 회전 후 WASD 방향 오류 없음
- 대화 반복 후 입력 중복/영구 잠금 없음
- 기존 Frontend, Settings, Save/Load, EntryResolver 테스트를 깨뜨리지 않음
- 캐릭터 최종 외형을 넣지 않아도 테스트 가능
- 외형 교체가 PlayerMotor 수정 없이 가능

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
11. 이번 범위가 아닌 것
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- 전투
- 적 AI
- NPC 이동/NavMesh 행동
- 인벤토리와 아이템 획득
- 퀘스트 시스템
- 최종 대화 데이터/분기 시스템
- 최종 캐릭터 모델·스프라이트·애니메이션
- 점프, 낙하, 수영
- 게임플레이 중 체크포인트 저장
- 카메라 가림 오브젝트 반투명 처리
- 패드 입력
- 최종 사운드와 VFX

이 항목들은 TODO 경계만 남길 수 있지만 구현하지 마라.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
12. 완료 보고 형식
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

완료 시 다음을 구체적으로 보고하라.

1. 실제 구현된 플레이 흐름
2. 추가·수정한 파일 목록
3. 주요 클래스별 책임
4. 사용한 입력 키와 조정 가능한 초기 수치
5. 실행한 Unity 컴파일/EditMode/PlayMode 테스트와 정확한 결과
6. 직접 확인한 해상도와 수동 QA 시나리오
7. placeholder와 미완료 위험
8. 최종 2D/2.5D/3D 아트 결정 시 교체해야 할 지점
9. 다음에 할 한 가지 권장 작업
10. `git status --short`

커밋과 푸시는 하지 마라.
```
