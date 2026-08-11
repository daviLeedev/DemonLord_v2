# 레이어드 이미지 맵 아트 매니페스트

## 목적

고정 쿼터뷰 프리렌더 원화를 정적 환경 표현으로 사용한다. 이동과 상호작용의 진실의 원천은 기존 3D Collider·Trigger·컴포넌트이며 이미지 픽셀에서 충돌을 추론하지 않는다.

## 에셋

| stable ID | 파일 | 역할 | 캘리브레이션 |
|---|---|---|---|
| `world-adjustment-lab-image-map` | `Assets/_Project/Art/Maps/Layered/WorldAdjustmentLab/world_adjustment_lab_base_v1.png` | 세계조정국 연구실 base | center `(0, 0, 2.5)`, ortho `18`, yaw/pitch `45/35`, aspect `16:9` |
| `bureau-courtyard-image-map` | `Assets/_Project/Art/Maps/Layered/BureauCourtyard/bureau_courtyard_base_v1.png` | 세계조정국 청사 외곽 base | center `(0, 0, 0)`, ortho `14`, yaw/pitch `45/35`, aspect `16:9` |

## 생성 프롬프트 요약

### 연구실

현재 Unity 연구실 스크린샷은 카메라 각도·방 윤곽·통로·문 개구부 참고로만 사용했다. 심야 세계조정국 행정 마법 연구소, 암청색 슬레이트와 현무암, 황동 장식, 청록 마력 회로, 접수·분석·기록·격리 구역, 명확한 이동 바닥과 넓은 카메라 여백을 요구했다. 캐릭터, NPC, UI, 글자, 로고, 워터마크는 제외했다.

### 청사 외곽

같은 기관의 야간 안뜰, 젖은 석재, 황동 인장, 청록 유도등, 버건디 배너, 명확한 연구실 입구와 플레이 경계를 요구했다. 캐릭터, NPC, UI, 글자, 로고, 워터마크는 제외했다.

## 레이어 상태

- `base`: v1 원화 적용
- `foreground`: 데이터와 렌더 슬롯 준비, 투명 전경 원화는 추후 제작
- `lighting`: 데이터와 렌더 슬롯 준비, 별도 광원/안개 원화는 추후 제작

전경과 광원 레이어를 추가할 때 base와 픽셀 크기·캔버스·카메라 캘리브레이션을 완전히 동일하게 유지한다.
