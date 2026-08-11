# 레이어드 이미지 맵 전환 구현 프롬프트

## 0. 실행 지시

`C:\workspace\unity\DemonLord_v2`에서 아래 단계를 번호 순서대로 구현한다.

- 모든 문서는 UTF-8로 끝까지 읽는다.
- `AGENTS.md`, `docs/agent/IMPLEMENTATION_PROMPT.md`, `docs/architecture/EXPLORATION_PROTOTYPE.md`, `docs/architecture/AREA_TRANSITION_AND_MAP_SYSTEM.md`를 우선 적용한다.
- 기존 사용자 변경과 현재 dirty worktree를 보존한다. 무관한 파일을 되돌리거나 정리하지 않는다.
- 커밋과 푸시는 하지 않는다.
- 전체 아트 QA는 별도 단계로 남기되, 컴파일·참조·씬 구조·입력 회귀는 이번 작업에서 검증한다.

## 1. 목표

현재의 가시적인 3D 블록아웃 맵을 다음과 같은 고정 쿼터뷰 이미지 맵으로 전환한다.

```text
고해상도 프리렌더/일러스트 배경
├─ 보이지 않는 기존 3D 충돌 지오메트리
├─ 동적으로 열리고 닫히는 문
├─ 플레이어 3D 모델
├─ NPC 3D 모델과 F 상호작용
├─ 포털·위치·잠금·지도 트리거
└─ 선택적 전경/광원 오버레이
```

시각 목표는 다음 특성을 가진 고품질 한국형 다크 판타지 2.5D RPG 탐험 화면이다.

- 고정된 직교 투영 쿼터뷰/아이소메트릭 시점
- 손으로 다듬은 프리렌더 환경 원화처럼 보이는 배경
- 캐릭터가 이동할 바닥과 출입구가 명확하게 읽히는 구성
- 환경은 이미지가 담당하고, 게임 규칙은 기존 Collider·Trigger·컴포넌트가 담당
- 현재 구현된 플레이어 이동, 대시, NPC 대화, 잠긴 방, 문, 지역 전환, 저장, 미니맵, 전체 지도를 유지

## 2. 확정 카메라 규칙

- 탐험 카메라는 고정 각도다.
- Q/E 탐험 카메라 회전을 제거한다. 입력 액션이 남더라도 탐험 중 회전에 영향을 주면 안 된다.
- 마우스 휠/패드 줌은 유지하며 이미지 맵의 안전 범위 안에서만 작동한다.
- 방이나 `CameraZone` 진입으로 yaw/pitch가 바뀌면 안 된다. 이미지 원근과 캐릭터 원근이 어긋나기 때문이다.
- 대화 시작/종료 시 카메라 위치·회전·줌을 변경하지 않는다.
- 대화는 기존 대화 UI와 인물 프로필만 사용한다.
- 지역 전환 후 `SnapImmediate`는 유지하되 같은 고정 프로필로 즉시 정렬한다.

## 3. 이미지 맵 데이터 계약

지역별 이미지 표현을 명시적으로 담는 ScriptableObject를 추가한다.

```text
LayeredImageMapDefinition
  stableId
  baseSprite                 # 필수, 불투명 배경 원화
  foregroundSprite           # 선택, 캐릭터 앞을 가릴 투명 전경
  lightingSprite             # 선택, 가산/알파 광원·안개
  referenceWorldCenter       # 원화를 캘리브레이션한 월드 중심
  referenceOrthographicSize  # 원화 전체 높이에 대응하는 직교 크기
  referenceAspect            # 원화의 가로/세로 비율
  referenceYaw
  referencePitch
  baseTint
  foregroundTint
  lightingTint
```

규칙:

- 런타임에 `Resources.LoadAll`, 파일명 추론, `GameObject.Find`를 사용하지 않는다.
- `AreaDefinition` 또는 `AreaRoot`에서 해당 이미지 맵 정의를 명시적으로 참조한다.
- base는 필수이고 foreground/lighting은 null을 허용한다.
- 이미지와 게임 카메라의 yaw/pitch/aspect가 다르면 검증 실패 또는 명확한 경고를 낸다.
- 지역별 캘리브레이션 값은 stable ID 데이터로 관리하고 임의 런타임 추론을 하지 않는다.

## 4. 런타임 표현 계층

`LayeredImageMapRenderer` 또는 동등한 단일 책임 컴포넌트를 구현한다.

- 지역 씬이 활성화될 때 base/foreground/lighting 레이어를 생성·표시한다.
- 배경은 카메라 투영면과 평행한 월드 공간 평면 또는 SpriteRenderer로 표시한다.
- 배경은 캐릭터·문·NPC보다 뒤에 렌더링한다.
- foreground는 캐릭터보다 앞에 렌더링하되 투명 픽셀은 가리지 않는다.
- lighting은 입력과 충돌을 소유하지 않으며 `raycastTarget` 같은 UI 입력을 막지 않는다.
- 지역 언로드 시 생성한 Material/오브젝트를 누수 없이 정리한다.
- 매 프레임 LINQ, 문자열, 배열, Material 인스턴스 할당을 만들지 않는다.
- 카메라가 플레이어를 따라가고 확대/축소해도 배경과 캐릭터의 투영이 같은 축을 유지해야 한다.

## 5. 아트 제작 규칙

다음 두 지역의 base 원화를 새로 제작한다.

1. `world_adjustment_lab_interior`
2. `bureau_courtyard`

공통 팔레트:

- Primary `#101720`
- PrimaryLight `#26384B`
- Secondary `#5CAECC`
- SecondaryDark `#234B63`
- AccentGold `#B99A59`
- AccentRed `#7D1827`
- Surface `#171C22`

공통 제작 조건:

- 플레이어, NPC, 대화창, HUD, 글자, 로고, 워터마크를 넣지 않는다.
- 실제 플레이 경로와 문 개구부를 장식으로 막지 않는다.
- 이미지 가장자리에는 카메라 줌아웃 시 보일 수 있는 충분한 여유 배경을 둔다.
- 캐릭터보다 앞에 와야 할 높은 기둥·문틀·아치는 추후 foreground 레이어로 분리할 수 있도록 실루엣을 명확하게 만든다.
- 첫 수직 슬라이스에서는 base 레이어를 필수 적용하고, 투명 foreground/lighting은 데이터 슬롯과 렌더 경계까지만 준비할 수 있다.

연구실 원화:

- 세계조정국의 심야 행정 마법 연구소
- 암청색 슬레이트/현무암 바닥, 황동 행정 장식, 청록 마력 회로
- 접수실, 분석실, 기록보관 구역, 잠긴 격리실로 이어지는 통로가 읽혀야 한다.
- 기존 `91_LabInterior`의 충돌·문·NPC·스폰 좌표와 대략 맞도록 현재 레이아웃을 구도 참고로 사용한다.

청사 외곽 원화:

- 같은 기관의 야간 안뜰과 연구실 입구
- 젖은 석재, 황동 인장, 청록 유도등, 어두운 버건디 배너
- 연구실 출입구와 플레이 가능한 안뜰 경계가 명확해야 한다.

## 6. Unity 임포트 규칙

- 원본 PNG는 `Assets/_Project/Art/Maps/Layered/<AreaName>/` 아래에 둔다.
- Texture Type은 Sprite (2D and UI), Sprite Mode는 Single로 고정한다.
- Alpha Source는 입력 알파를 사용하고 `Alpha Is Transparency`를 켠다.
- Wrap Mode는 Clamp, Mip Map은 비활성화한다.
- sRGB는 base/foreground에 사용한다. 별도 마스크가 생길 때만 Linear를 검토한다.
- Max Size는 원본을 손상시키지 않는 범위에서 4096 이상을 우선한다.
- 플랫폼 압축 때문에 가장자리나 글로우가 번지지 않도록 검증한다.
- 파일명과 `.meta` GUID가 재생성 때 바뀌지 않게 한다.

## 7. 기존 3D 환경의 역할 변경

- 기존 바닥·벽·가구·계단 MeshRenderer는 이미지 맵 적용 지역에서 숨긴다.
- Collider는 유지한다.
- `CharacterController`, `AreaPortal`, `LocationVolume`, `MapFloorVolume`, 잠긴 문 메시지, NPC 상호작용은 유지한다.
- 동적 문은 최소한 상호작용 상태를 읽을 수 있게 남긴다. 원화와 충돌하는 임시 MeshRenderer는 전용 얇은 오버레이나 비표시 상태로 전환할 수 있다.
- 플레이어와 NPC는 이미지 배경 위에 정상적으로 렌더링되어야 한다.
- 배경 전환을 위해 Collider를 이미지 알파나 픽셀 색으로 자동 생성하지 않는다. 이동 불가 영역은 명시적인 Collider/Volume 데이터가 진실의 원천이다.

## 8. 지역별 적용

### 8.1 연구실 내부

- `91_LabInterior`의 현재 월드 중심과 카메라 프로필에 base 이미지를 캘리브레이션한다.
- 접수 시작점, 외부 복귀 스폰, NPC, 전투 연결용 NPC, 일반문, 기록실 양문, 격리실 잠금문을 보존한다.
- 이미지 장식과 Collider가 크게 어긋나는 구간은 개발용 토글로 Collider gizmo를 표시해 조정 가능하게 한다.

### 8.2 청사 외곽

- `92_BureauCourtyard`에도 동일한 렌더러/데이터 구조를 사용한다.
- 연구실로 돌아오는 포털과 `lab_exit` 스폰을 보존한다.
- 실내/외 전환 때 이전 지역 이미지가 남거나 두 장이 겹치면 안 된다.

## 9. 씬 빌더와 재생성

- 기존 Editor SceneBuilder가 진실의 원천이면 이미지 맵 생성과 참조도 같은 빌더에 넣는다.
- 수동 씬 수정만으로 끝내지 않는다.
- 빌더 재실행은 멱등적이어야 하며 이미지 맵 오브젝트가 중복 생성되면 안 된다.
- 이미 dirty인 씬을 덮어쓰기 전 변경 내용을 확인하고, 기존 사용자 수정과 겹치는 필드만 최소 변경한다.
- 씬 재생성 후 `90_GameShell`, `91_LabInterior`, `92_BureauCourtyard`의 Missing Script/Reference가 없어야 한다.

## 10. 지도 UI와 구분

- 배경 원화와 `M` 전체 지도/미니맵 이미지는 서로 다른 책임과 에셋이다.
- 기존 `MapFloorDefinition.backgroundSprite`와 월드 좌표 투영은 유지한다.
- 이미지 맵 원화를 미니맵으로 직접 재사용하지 않는다.
- Q/E는 탐험 카메라 회전에 쓰지 않는다. 전체 지도에서 다층 전환이 구현되어 있다면 그 컨텍스트에서만 사용할 수 있다.

## 11. 문서 갱신

- `docs/architecture/EXPLORATION_PROTOTYPE.md`의 Q/E 카메라 회전과 대화 카메라 오버라이드 설명을 현재 확정안으로 수정한다.
- `docs/architecture/AREA_TRANSITION_AND_MAP_SYSTEM.md`에서 탐험 카메라 회전에 의존하는 문구를 제거하고, 이미지 배경과 Collider 분리 원칙을 추가한다.
- 생성한 이미지의 역할, 프롬프트 요약, 파일 경로, 캘리브레이션 값을 아트 매니페스트 또는 구현 문서에 기록한다.

## 12. 검증 기준

- 플레이 모드에서 WASD 8방향 이동, 걷기/달리기/대시가 유지된다.
- Q/E를 눌러도 탐험 카메라가 회전하지 않는다.
- 마우스 휠 줌은 설정된 범위에서 작동한다.
- F 대화 중 카메라가 움직이거나 확대되지 않는다.
- 문, 잠긴 문 메시지, NPC 대화, 지역 전환이 기존과 동일하게 동작한다.
- 실내 ↔ 외부 왕복 후 현재 지역 이미지 하나만 표시된다.
- 캐릭터가 이미지에서 이동 가능해 보이는 바닥과 대체로 일치하고 벽을 통과하지 않는다.
- M 전체 지도와 미니맵 투영이 기존과 동일하게 동작한다.
- 1280×720, 1920×1080, 2560×1440, 3440×1440에서 이미지가 빈 화면을 노출하지 않는다.
- 컴파일 오류, 콘솔 예외, Missing Script/Reference가 0개다.
- 기존 dirty 파일과 사용자 에셋을 삭제·되돌림·불필요한 재직렬화하지 않는다.
