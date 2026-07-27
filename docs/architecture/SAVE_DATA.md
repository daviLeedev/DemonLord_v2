# 세이브 데이터 계약

## 파일 구조

```text
Application.persistentDataPath/
  Saves/
    slot-01/save.json
    slot-01/save.bak
    slot-01/save.tmp
    slot-02/...
    slot-03/...
```

파일 경로는 슬롯 ID의 화이트리스트로만 조립한다.

## v3 논리 스키마

```json
{
  "schemaVersion": 3,
  "saveId": "uuid",
  "slotId": "slot-01",
  "createdAtUtc": "2026-07-22T00:00:00.0000000Z",
  "updatedAtUtc": "2026-07-22T00:00:00.0000000Z",
  "buildVersion": "0.1.0",
  "payloadJson": "{...}",
  "payloadSha256": "lowercase hex"
}
```

Payload에는 아래 논리 데이터가 들어간다.

```json
{
  "profile": {
    "profileName": "마왕",
    "difficultyId": "normal",
    "tutorialMode": "detail"
  },
  "progress": {
    "entryId": "prologue_start",
    "checkpointId": "reception-start",
    "areaId": "world-adjustment-lab-interior",
    "spawnId": "reception-start",
    "playTimeSeconds": 0
  }
}
```

직렬화 DTO와 런타임 도메인 객체는 분리한다. 현지화 문구, Unity 오브젝트 참조, 씬 이름은 저장하지 않는다. `areaId`와 `spawnId`는 지역 정의의 안정 ID이며, 현재 씬 경로나 build index가 아니다.

## 저장/로드 알고리즘

저장:

1. 도메인 객체를 DTO로 변환하고 결정적 payload JSON과 SHA-256을 생성한다.
2. `save.tmp`에 envelope를 완전하게 쓴 후 flush한다.
3. tmp를 다시 읽어 자체 검증한다.
4. 기존 `save.json`을 유지한 상태에서 교체하고 직전 정상 파일을 `save.bak`으로 남긴다.
5. 지원하지 않는 플랫폼의 교체 대안도 백업이 남는 순서를 보장한다.

로드:

1. `save.json`의 최소 필드, schema, slotId, SHA-256, DTO 값 범위를 검사한다.
2. 실패하면 `save.bak`에 같은 검사를 수행한다.
3. 백업 성공은 `RecoveredFromBackup=true`로 반환한다.
4. 둘 다 실패하면 원인 코드를 가진 `Corrupt` 결과를 반환한다.

저장소 경계의 결과 모델:

```text
SaveReadResult
  Status: Success | Empty | Corrupt | Incompatible | IoFailure
  Save: GameSave?                 # Success만 존재
  RecoveredFromBackup: bool
  ErrorCode: stable enum/string
  DiagnosticMessage: developer-facing only
```

## 버전 관리

- 미래 버전 세이브는 읽기 전용 `Incompatible`으로 취급하고 절대 덮어쓰지 않는다.
- 과거 버전은 `v1 → v2 → v3`처럼 단계별 마이그레이션을 통과한다.
- 마이그레이션은 새 DTO를 반환하며 원본을 즉시 덮어쓰지 않는다.
- 각 버전에는 고정 fixture 기반 EditMode 테스트를 둔다.

### v1 → v2

- v1의 `tutorialEnabled=true`는 `tutorialMode="detail"`로 변환한다.
- v1의 `tutorialEnabled=false`는 `tutorialMode="off"`로 변환한다.
- v1에는 핵심 안내 여부 정보가 없었으므로 `core`로 추정하지 않는다.
- 마이그레이션 전에 v1 payload checksum을 먼저 검증하며, 원본 `save.json`은 자동으로 덮어쓰지 않는다.

### v2 → v3

- v2에는 지역 정보가 없으므로 `areaId="world-adjustment-lab-interior"`, `spawnId="reception-start"`를 명시적으로 부여한다.
- 기존 `checkpointId`는 보존한다. 구형 entry/checkpoint 해석은 resolver가 담당하며 마이그레이션이 Unity 씬명을 기록하지 않는다.
- v3 이후 저장은 현재 유효한 `ExplorationLocation(areaId, spawnId)`만 기록한다.
