# 행성수호자

행성을 향해 전진해오는 적을 저지하는 2D 슈팅 게임입니다.  
플레이어는 드래그 이동으로 전투를 진행하고, 적 처치 시 보상 선택을 통해 전투 성능을 강화합니다.

## 프로젝트 개요
- 프로젝트명: 행성수호자
- 개발기간: 2026.04.01 ~ 2026.04.05
- 개발인원: 1명
- 기술스택: Unity, C#
- 플랫폼: PC, Mobile

## 핵심 구현 포인트
- Spline 없이 베지에 곡선 기반 적 이동 동선 구현
  - `PathCurveEvaluator`에서 선형/베지에 샘플링을 지원하고, `SnakeController`가 경로 길이 기반으로 적 이동을 처리합니다.
  - 외부 Spline 패키지 의존 없이 코드 레벨에서 경로 품질을 제어할 수 있도록 구성했습니다.

- ScriptableObject 기반 스테이지/동선 데이터 관리
  - `StageConfigSO`에서 이동 경로 포인트, 속도, 체력, 세그먼트 수 등 스테이지 파라미터를 데이터로 분리했습니다.
  - 코드 수정 없이 에디터에서 스테이지 밸런싱이 가능하도록 설계했습니다.

- ScriptableObject로 광폭화(Enrage) 구간 설정
  - 스테이지 데이터에 광폭화 배율, 지속시간, 트리거 진행도 구간(Progress)과 전용 헤드 스프라이트를 분리 관리합니다.
  - 런타임에서 구간 도달 시 자동으로 광폭화 상태/비주얼을 전환합니다.

## 주요 시스템
- 플레이어 전투 시스템
  - 무기 모듈 구조(`WeaponModuleBase` 파생)로 라이플/레이저/미사일 동작을 분리했습니다.
  - `ProjectilePool`을 통해 투사체를 풀링 처리해 런타임 할당을 줄였습니다.

- 보상(Reward) 시스템
  - CSV 기반 보상 정의 파싱 후 런타임 데이터로 변환합니다.
  - 희귀도 가중치 기반 랜덤 선택, 획득 횟수 제한, 무기 타입 스코프(Common/Weapon) 조건을 지원합니다.

- 피드백 시스템
  - 데미지 텍스트 및 무기 VFX를 `FeedbackSystem`에서 통합 관리합니다.
  - 파티클 VFX는 프리팹 ID 기반으로 풀링해 재사용합니다.

## 데이터 중심 설계 포인트
- SO/CSV 기반으로 게임 밸런스 수치를 코드에서 분리했습니다.
- 런타임 컨트롤러(`StageRuntimeController`, `RewardSystem`)가 데이터 로딩과 상태 전환을 담당하도록 역할을 분리했습니다.

## 다운로드
[Windosw 실행 파일 다운로드](https://github.com/homebd/GameCamp/releases/tag/v1.0.0)

## 디렉터리 개요
- `Assets/Scripts/Game/Path`: 경로 평가(선형/베지에) 로직
- `Assets/Scripts/Game/Snake`: 적 이동/세그먼트/광폭화 처리
- `Assets/Scripts/Game/Player`: 플레이어 이동/스탯/무기 연동
- `Assets/Scripts/Game/Rewards`: 보상 데이터 파싱/선택/적용
- `Assets/Scripts/Game/Combat/Projectiles`: 투사체 및 풀링

## 회고
- 짧은 기간(4일) 내에도 데이터 주도 설계(SO/CSV)를 적용해, 기능 추가와 밸런싱 변경에 유연하게 대응할 수 있는 구조를 구축했습니다.
