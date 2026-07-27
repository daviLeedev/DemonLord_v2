# 인게임 일시정지 메뉴 핵심 기능 설계

## 1. 목적

`90_GameShell`의 일시정지 메뉴에서 다음 세 명령을 실제 서비스와 연결해 완성한다.

- 저장하기
- 환경 설정
- 타이틀로 돌아가기

`PauseMenuView`는 버튼 이벤트와 표시만 담당한다. 저장 파일, 설정 파일, 세션 정리, 씬 전환은 각각 기존 Application/Infrastructure 서비스가 담당한다.

## 2. 현재 구현 상태와 보완점

현재 코드에는 다음 골격이 이미 존재한다.

- `InGameUiCoordinator`: 메뉴 상태, `Time.timeScale`, 입력 잠금, 세 명령의 기본 분기
- `SaveGameProgressUseCase`: 현재 슬롯에 체크포인트를 원자적으로 저장
- `SettingsService`: `Persisted`/`Working` 사본과 적용·저장·취소
- `ISceneFlowService`: Frontend와 GameShell 씬 전환
- `LabProgressController`: 연구실 진행 단계 완료 시 안정적인 체크포인트 저장

다음 공백을 보완해야 한다.

1. 세 명령의 성공·실패·연타·뒤로 가기 경로에 대한 통합 테스트가 부족하다.
2. 수동 저장의 의미가 명시되어 있지 않다.
3. 인게임 환경 설정의 취소/실패 후 런타임 복구를 검증해야 한다.
4. GameShell 진입 전 `FrontendCoordinator`는 `Busy`일 수 있다. 이 상태로 Frontend를 다시 로드하면 타이틀 입력 또는 로딩 화면이 고착될 수 있다.
5. 타이틀 전환 실패 시 세션, 메뉴, `timeScale`, 입력 잠금을 모두 복구해야 한다.

## 3. 공통 명령 규칙

- 명령은 `InGameUiCoordinator`만 실행한다.
- `PauseMenuView`는 `SaveRequested`, `SettingsRequested`, `ReturnToTitleRequested` 이벤트만 전달한다.
- 실행 전 현재 `InGameMenuState`가 허용 상태인지 확인한다.
- 저장 또는 씬 전환이 시작되면 `Busy`로 전환하고 중복 입력을 거부한다.
- 메뉴가 열려 있는 동안 탐험 입력 잠금과 기존 `timeScale` 보존 정책을 유지한다.
- 복구 가능한 실패는 메뉴 내부 상태 문구로 표시하고 게임을 종료하거나 세션을 조용히 버리지 않는다.
- 성공·실패 여부를 색상에만 의존하지 않고 문구와 버튼 활성 상태로 함께 전달한다.
- View에서 `Resources`, 파일 경로, `SceneManager`, 저장소를 직접 호출하지 않는다.

## 4. 저장하기

### 4.1 저장 의미

이번 단계의 수동 저장은 현재 세션의 다음 값만 유지·갱신한다.

- 현재 `slotId`
- 현재 `entryId`
- `LabProgressController`가 이미 성공적으로 기록한 현재 `checkpointId`
- 저장 수정 시각과 체크섬

플레이어의 임의 좌표, 카메라 각도, 열려 있는 문, 대화 중간 상태를 새 필드로 추가하지 않는다. 로드 위치는 안정적인 체크포인트 SpawnPoint다.

연구실 진행 완료 시 `LabProgressController`가 `SaveGameProgressUseCase`를 통해 `IPlayerSession.CurrentSave`를 갱신하므로, 메뉴의 수동 저장은 `CurrentSave.Progress.CheckpointId`를 다시 저장하는 것이 정식 동작이다.

### 4.2 흐름

```text
Root
→ 현재 세이브 확인
→ Busy
→ SaveGameProgressUseCase.Execute(session, currentCheckpointId)
├─ 성공: 세션의 CurrentSave 갱신 + 저장 완료 효과음 + Root 상태 문구
└─ 실패: 기존 세션 유지 + Root 오류 문구
```

- 활성 세이브가 없으면 저장 버튼을 비활성화한다.
- 성공 후 메뉴는 닫지 않는다. 사용자가 결과를 확인한 뒤 계속하기를 선택한다.
- 실패 후에도 메뉴와 일시정지는 유지한다.
- 저장 중 모든 루트 버튼을 비활성화한다.
- 성공 문구: `기록을 저장했습니다.`
- 실패 문구: `기록을 저장하지 못했습니다. 다시 시도해 주세요.`
- 저장 완료 효과음은 `ui_save_complete_01.wav`를 사용한다.
- Repository의 tmp → 검증 → 최종 파일 교체 → bak 정책을 우회하지 않는다.

## 5. 환경 설정

### 5.1 단일 설정 원본

프런트엔드와 인게임은 동일한 `SettingsService`를 사용한다. 인게임 전용 설정 파일이나 별도 `PlayerPrefs`를 만들지 않는다.

```text
Root → Settings
  BeginEdit()
  ├─ 값 변경: SetWorking() → 런타임 즉시 미리보기
  ├─ 기본값: ResetWorking() → 아직 영구 저장하지 않음
  ├─ 적용: SaveWorking()
  │   ├─ 성공: Persisted 갱신 → Root
  │   └─ 실패: Settings 유지 + 오류 문구
  └─ 취소/Escape: CancelEdit() → Persisted 재적용 → Root
```

### 5.2 실제 적용 범위

- 화면 모드, 해상도, VSync, 품질은 기존 `UnityGameSettingsRuntimeApplier`로 즉시 미리보기한다.
- 주 음량과 효과음은 최소한 인게임 UI 효과음의 실제 볼륨에 반영한다.
- BGM AudioSource가 GameShell 조립 경로에 명시적으로 존재할 때만 주 음량 × BGM 음량을 적용한다. 존재하지 않는 BGM 시스템을 가짜로 만들지 않는다.
- UI 크기는 인게임 메뉴의 기준 루트에 적용하되 Safe Area와 중앙 정렬을 깨지 않는다.
- 화면 흔들림·섬광·전환 감소는 해당 효과가 실제로 있는 컴포넌트만 소비한다. 존재하지 않는 효과를 구현한 것처럼 표시하지 않는다.
- 적용 실패 시 `Working`은 화면에 남아 있어도 되지만 `Persisted`는 바뀌면 안 된다. 이후 취소하면 반드시 기존 `Persisted`가 다시 적용되어야 한다.

## 6. 타이틀로 돌아가기

### 6.1 사용자 흐름

```text
Root
→ ConfirmReturnToTitle
├─ 취소/Escape: Root
└─ 확인: Busy
    → Frontend를 MainMenu 진입 모드로 준비
    → 현재 PlayerSession 정리
    → 10_Frontend 로드
    ├─ 성공: 로고/주의 문구를 반복하지 않고 MainMenu 표시
    └─ 실패: 기존 세이브 세션 복구 + Root 복구 + 오류 문구
```

- 확인창에는 `저장하지 않은 진행은 사라질 수 있습니다.`를 표시한다.
- 타이틀 복귀가 암묵적으로 수동 저장을 실행하지 않는다.
- 세션을 비우기 전 원본 `GameSave` 참조를 보관하고 전환 실패 시 복원한다.
- 씬 전환 전 `timeScale`과 입력 잠금을 해제한다.
- 전환 실패 시 다시 일시정지 상태를 적용한다.

### 6.2 Frontend 진입 모드

초기 부팅과 게임에서의 복귀를 구분하는 명시적 계약을 둔다.

```csharp
public enum FrontendEntryMode
{
    Opening,
    MainMenu,
}
```

권장 계약은 `ISceneFlowService.LoadFrontendAsync(FrontendEntryMode mode)`다.

- `AppRoot.Start`: `Opening`
- `InGameUiCoordinator`의 타이틀 복귀: `MainMenu`
- `FrontendCoordinator`는 진입 모드에 맞춰 화면 상태, 오류, 선택 슬롯을 초기화한다.
- `FrontendView.Initialize`는 `Opening`일 때만 로고 시퀀스를 재생한다.
- `MainMenu` 진입은 슬롯 목록을 갱신하고 첫 활성 버튼에 포커스를 둔다.
- 기존 `Busy` 상태를 그대로 Frontend에 전달하지 않는다.

## 7. 실패 복구 불변식

모든 예외와 실패 반환에서 다음 조건을 만족해야 한다.

| 상황 | 메뉴 상태 | 세션 | timeScale | 입력 잠금 |
|---|---|---|---|---|
| 저장 실패 | Root | 유지 | 일시정지 유지 | 메뉴 외 탐험 잠금 유지 |
| 설정 저장 실패 | Settings | 유지 | 일시정지 유지 | 메뉴 외 탐험 잠금 유지 |
| 설정 취소 | Root | 유지 | 일시정지 유지 | 메뉴 외 탐험 잠금 유지 |
| 타이틀 전환 실패 | Root | 원본 복원 | 일시정지 재적용 | 메뉴 외 탐험 잠금 재적용 |
| 타이틀 전환 성공 | Frontend MainMenu | 비움 | 정상값 복원 | GameShell 잠금 폐기 |

`OnDisable`, `OnDestroy`, 예외 처리에서 잠금 토큰을 두 번 Dispose하지 않는다.

## 8. 테스트 기준

### EditMode

- 수동 저장 성공 시 현재 체크포인트와 슬롯을 유지하고 수정 시각/세션 스냅샷을 갱신한다.
- 저장 실패 시 세션 스냅샷이 바뀌지 않는다.
- 설정 취소 시 Persisted 값이 런타임에 다시 적용된다.
- 설정 저장 실패 시 Persisted가 바뀌지 않는다.
- Frontend `Opening`과 `MainMenu` 진입 모드가 결정적으로 상태를 초기화한다.
- `Busy` 중 같은 메뉴 명령을 거부한다.

### PlayMode

- 저장 버튼 성공/실패 문구와 저장 효과음을 확인한다.
- 환경 설정 변경 후 취소하면 실제 미리보기 값이 원래 값으로 돌아간다.
- 환경 설정 적용 후 Frontend 설정 화면에서도 같은 값이 보인다.
- 타이틀 복귀 확인 취소는 Root로 돌아간다.
- 타이틀 복귀 성공은 `10_Frontend`의 MainMenu로 직접 이동하고 무한 스피너가 없다.
- 타이틀 전환 실패는 세션, 메뉴, `timeScale`, 입력 잠금을 복구한다.
- 빠른 연속 입력으로 저장 또는 씬 로드가 중복 실행되지 않는다.

## 9. 완료 기준

- 세 버튼이 실제 Application 서비스와 연결되어 동작한다.
- 성공·실패·취소·연타 경로가 모두 결정적이다.
- 타이틀 복귀 후 로고를 다시 재생하지 않고 MainMenu가 열린다.
- 저장 포맷에 임시 좌표나 가짜 게임 상태를 추가하지 않는다.
- 설정은 프런트엔드와 같은 파일 및 서비스로 유지된다.
- 컴파일 오류, 콘솔 예외, Missing Script/Reference가 0개다.
