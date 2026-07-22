# 구현 작업 순서

각 항목은 별도 커밋 단위로 끝낸다. 앞 단계의 수용 조건이 확인되기 전 다음 단계로 넘어가지 않는다.

## 0. 기반 구조

- `Assets/_Project` 표준 폴더, Runtime/Editor/EditMode/PlayMode asmdef 구성
- `00_Boot`, `10_Frontend`, `90_GameShell` 생성 및 Build Settings 순서 등록
- Unity 컴파일 성공과 테스트 어셈블리 실행 확인

## 1. 도메인과 세이브 계약

- SlotId/DifficultyId/EntryId, NewGameSettings, GameSave, SaveSlotSummary
- DTO/매퍼, 입력 검증, checksum, SaveReadResult
- 순수 EditMode 테스트

## 2. 파일 저장소

- 슬롯 경로 정책, tmp→검증→교체→bak 저장
- 손상 본 파일의 백업 복구, schema/migration 골격
- 실제 임시 폴더 기반 round-trip 테스트

## 3. 상태 머신과 유스케이스

- FrontendCoordinator, Back/Busy/Error 정책
- ListSaveSlots/CreateNewGame/LoadGame
- PlayerSession, EntryPointResolver
- View 없이 전체 분기 테스트

## 4. Boot·씬 전환·GameShell

- 명시적 AppRoot 조립과 중복 방어
- ISceneFlowService 비동기 구현
- GameShell이 세션 정보만 표시

## 5. UGUI 연결

- 로고, 인트로, 메인, 시작 방식, 슬롯, 설정, 확인/오류/Busy View
- 입력 포커스, Back, 중복 클릭 방어

## 6. 통합 검증

- 첫 실행·이어하기·덮어쓰기·손상 파일·연속 클릭 시나리오
- Windows Development Build와 PlayMode/Editor 테스트
- 콘솔 오류/Missing Reference 0개 확인

에이전트에게 한 단계를 맡길 때는 다음만 추가로 전달한다.

```text
AGENTS.md와 architecture 문서를 읽어라.
이번에는 WORK_ORDERS의 작업 N만 수행하고 다음 작업을 선행하지 마라.
사용자 변경을 건드리지 말고, 테스트 증거와 함께 보고하라.
```
