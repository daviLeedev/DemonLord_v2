# Navigation-Aligned Image Map & SD Tax Officer Implementation Prompt

## Mission

현재 `91_LabInterior`는 실제 충돌/이동 구조와 배경 일러스트가 일치하지 않아 길과 벽을 판별하기 어렵고, 숨겨지지 않은 3D 문·상호작용 블록이 2D 배경 위에 노출된다. 또한 플레이어 세무관은 3D 모델 대신 게임 테마에 맞는 SD 캐릭터로 교체한다.

이번 작업의 목표는 다음 두 가지다.

1. **내비게이션 정합 이미지 맵**: 이동·충돌·문·스폰 데이터를 단일 원본으로 삼고, 배경 이미지·길 안내·미니맵을 같은 원본에서 파생한다.
2. **4방향 SD 세무관**: 논리 이동은 기존 8방향 자유 이동을 유지하고, 화면 표현은 카메라 기준 상/하/좌/우 SD 스프라이트로 바꾼다.

## Non-negotiable rules

- 기존 사용자 변경을 보존한다. 관계없는 파일을 되돌리거나 재생성하지 않는다.
- 커밋·푸시는 하지 않는다.
- 기존 세이브 슬롯, `AreaRoot`, 포털/문/NPC/상호작용 stable ID를 바꾸지 않는다.
- 충돌 판정을 AI 이미지의 픽셀에서 추론하지 않는다. **Unity 월드의 명시적 Collider/Navigation 데이터가 유일한 게임플레이 원본**이다.
- 배경·가이드·미니맵은 동일한 월드 범위와 투영 상수를 사용한다.
- 탐색 카메라는 직교 투영, yaw 45°, pitch 35° 고정이다. Q/E 탐색 회전과 대화 카메라 전환은 다시 도입하지 않는다.
- 씬 빌더는 여러 번 실행해도 중복 오브젝트가 생기지 않아야 한다.
- 전체 아트 QA는 별도 단계로 남길 수 있지만, 컴파일·정적 검증·핵심 동작 검증은 이번 작업에서 수행한다.

## 0. Audit and preservation

- `git status --short`와 관련 씬/프리팹/빌더의 현재 상태를 기록한다.
- 기존 `LayeredImageMap*`, `AreaMapDefinition`, `MiniMapView`, `PlayerMotor`, `PlayerFacing`, `DirectionalSpritePresenter`를 우선 재사용한다.
- 현재 씬의 충돌체와 위치를 임의로 배경에 맞춰 옮기지 않는다. 배경을 실제 구조에 맞춘다.

## 1. Establish one projection contract

- 연구실 기준값을 한 곳에 정의한다.
  - world center: `(0, 0, 2.5)`
  - orthographic size: `18`
  - aspect: `16:9`
  - yaw: `45°`
  - pitch: `35°`
  - output: `1672 × 941`
- 런타임 이미지 레이어, 에디터 가이드 내보내기, 검증기가 같은 값을 사용하게 한다.

## 2. Export an exact authoring guide

- `91_LabInterior`의 실제 BoxCollider/문/스폰을 기준으로 직교 카메라 투영 가이드를 PNG로 내보내는 Editor 도구를 만든다.
- 색상 규칙:
  - 청록: 걸을 수 있는 방 바닥
  - 적색: 벽·가구·고정 장애물
  - 금색: 문/통로
  - 청색: 시작 스폰
- 가이드에는 방의 연결 방향과 출입구가 한눈에 보여야 하며, 아트 생성 레퍼런스로만 사용한다.
- 출력 예: `Assets/_Project/Art/Maps/Authoring/WorldAdjustmentLab/lab_projection_guide_v1.png`

## 3. Produce aligned map art

- 기존 연구실 미술 분위기(남청색 석재, 황동 장식, 청록 마력)를 유지하되, 가이드의 외곽선·통로·문 위치를 바꾸지 않는 새 버전 이미지를 만든다.
- 플레이 가능한 바닥은 주변보다 15~25% 밝게, 통로에는 황동 인레이/카펫/청록 유도등을 넣어 길이 읽히게 한다.
- 막힌 곳은 벽·난간·가구로 명확히 보이게 한다.
- 이미지에 글자, UI, 플레이어, NPC, 문짝 애니메이션 상태를 포함하지 않는다.
- 기존 파일을 덮어쓰지 않고 `world_adjustment_lab_base_v2.png`로 추가한다.

## 4. Walkability and obstruction truth

- 바닥 Collider와 벽/가구 Collider를 유지한다.
- 모든 플레이 가능 바닥은 실제 Floor collider 내부여야 하며 배경의 밝은 보행 영역과 대응해야 한다.
- 문 통과 지점은 벽 사이의 실제 개구부와 정확히 일치해야 한다.
- 개발용 토글 가능한 디버그 오버레이를 제공해 보행 가능 영역, 장애물, 문, 스폰을 런타임 화면 위에서 비교할 수 있게 한다.

## 5. Image-map presentation cleanup

- `LayeredImageMapRenderer`가 배경 표시 중 기존 Environment MeshRenderer를 숨긴다.
- `LabDoorController`의 3D 문짝/선택 마커와 비-NPC 조사 오브젝트의 임시 큐브 렌더러도 숨긴다. Collider와 상호작용 컴포넌트는 유지한다.
- NPC 표현은 별도 Presenter가 담당하므로 무조건 숨기지 않는다.
- 숨김 대상 목록은 중복 제거하고 null-safe하게 직렬화한다.

## 6. Door presentation

- 문 상호작용/잠금/충돌은 기존 `LabDoorController`를 유지한다.
- 배경에 닫힌 문을 영구로 그려 넣지 않는다. 필요한 경우 독립 2D door presenter를 사용해 열림/닫힘/잠김 상태를 표시한다.
- 이번 단계에서 독립 문 아트가 준비되지 않으면, 최소한 기존 3D 문짝이 배경 위로 튀어나오지 않게 하고 문 위치에 금색 바닥 문턱/표식을 둔다.

## 7. Rebuild the minimap from the same layout

- 기존 임의 직사각형 미니맵을 실제 방/통로 배치가 보이는 지도 이미지로 교체한다.
- `MapFloorDefinition`의 world origin/axes/size는 연구실 월드 범위를 정확히 포함해야 한다.
- 플레이어 마커와 포털 마커가 실제 지도 위의 위치와 일치해야 한다.
- 미니맵에서도 현재 이동 가능한 방과 연결 통로를 즉시 판별할 수 있어야 한다.

## 8. SD tax officer art specification

- 기존 세무관 초상화의 정체성을 유지한다: 젊은 남성, 검은 머리, 남청색 세계조정국 제복, 은색 파이핑과 흉장, 검은 서류가방.
- 2.5~3등신 SD 비율, 전신, 선명한 실루엣, 등각/쿼터뷰 게임에 어울리는 반실사 판타지 일러스트로 제작한다.
- 4방향: down/front, up/back, left, right. 각 방향의 체형·의상·가방 크기와 기준 발 위치가 일관되어야 한다.
- 배경은 완전 투명이어야 하며 바닥 그림자는 별도 스프라이트로 분리한다.
- 원본 시트는 버전 파일로 보관하고 Unity용 투명 결과를 별도 파일로 둔다.

## 9. Keep 8-direction movement, collapse visual direction to 4

- `PlayerMotor`와 `PlayerFacing`의 8방향 논리 이동은 변경하지 않는다.
- `DirectionalSpritePresenter`에서 카메라 상대 방향을 구한 후 4방향으로 축약한다.
- 대각선 입력은 화면 공간에서 절댓값이 큰 축을 우선한다. 정확히 같은 경우 마지막 표시 방향을 유지해 떨림을 방지한다.
- 정지하면 마지막 표시 방향을 유지한다.
- 카메라 각도는 고정이지만 변환 함수는 테스트 가능한 순수 함수로 둔다.

## 10. Import and animation wiring

- SD 시트를 Sprite Multiple로 import하고 발 위치가 같은 pivot을 사용한다.
- `DirectionalAnimationSet`의 8방향 슬롯에는 4방향 결과를 매핑한다.
- Idle/Walk/Run/Dash 상태 연결은 유지한다. 1차 에셋이 단일 프레임이면 같은 프레임을 재사용하되 상태 전환과 방향 전환은 즉시 동작해야 한다.
- 향후 다중 프레임 교체가 가능하도록 파일명/설정 구조를 고정한다.

## 11. Replace the 3D player visual

- `WorldAdjustmentLabPlayer.prefab`에서 `TaxOfficer3DModel`, Animator, `TaxOfficerModelAnimator` 의존성을 제거한다.
- 루트의 `CharacterController`, `PlayerMotor`, `ExplorationInputReader`, `InteractionSensor`, 체크포인트/세이브 연결은 유지한다.
- `ModelVisual` 아래에 SpriteRenderer + `DirectionalSpritePresenter`를 만들고 카메라 빌보드, 적절한 PPU/스케일/정렬을 설정한다.
- 발 아래 반투명 타원 그림자를 별도 SpriteRenderer로 둔다.
- Sprite는 콜라이더 바닥면에 발이 닿도록 pivot/offset을 맞춘다.

## 12. Builder and validation changes

- `WorldAdjustmentLabSceneBuilder`가 3D 모델 패키지를 요구하지 않고 SD animation set을 생성/연결하도록 바꾼다.
- 검증은 `TaxOfficer3DModel` 존재가 아니라 정확히 하나의 `DirectionalSpritePresenter`, 유효한 animation set, SpriteRenderer, CharacterController를 확인한다.
- 구형 3D 플레이어 오브젝트가 남아 있으면 검증 실패로 처리한다.

## 13. Automated tests

- EditMode:
  - 8방향→카메라 상대 4방향 매핑
  - 대각선 우세축 및 동률 시 마지막 방향 유지
  - 이미지 맵 definition 투영 상수 검증
  - 미니맵 world-to-normalized 경계 검증
- PlayMode 또는 정적 씬 검증:
  - 연구실 진입 후 PlayerMotor 입력 가능
  - 걷기/달리기/대시 상태가 Presenter에 반영
  - 방향별 sprite가 null이 아님
  - 숨긴 시각 오브젝트의 Collider와 door interaction은 활성

## 14. Acceptance criteria

- 스크린샷만 보아도 중앙 접수실에서 각 방으로 가는 길을 설명할 수 있다.
- 플레이어가 보이는 벽/가구 위를 걷거나, 보이는 바닥에서 보이지 않는 벽에 막히는 주요 불일치가 없다.
- 기존 3D 문/검은 큐브/보라색 임시 블록이 배경 위로 노출되지 않는다.
- 미니맵의 방·통로와 실제 이동 구조가 일치한다.
- 세무관은 3D 모델이 아니라 SD 전신 스프라이트이며, 이동 중 화면 기준 상/하/좌/우가 올바르게 바뀐다.
- WASD, 걷기/달리기/대시, F 상호작용, ESC 메뉴, M 지도, 세이브 진입 흐름이 회귀하지 않는다.
- Unity 컴파일 오류가 없고 관련 EditMode 검증과 씬 빌더 검증이 통과한다.

