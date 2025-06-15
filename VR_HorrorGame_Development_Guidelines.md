# VR Horror Game - Development Guidelines

## 프로젝트 개요
- **플랫폼**: Meta Quest 3S
- **엔진**: Unity 2022.3 LTS
- **VR SDK**: Oculus Integration
- **장르**: VR Horror Adventure

## 완료된 시스템

### 1. CinematicManager (영상 재생 시스템)
**위치**: `Assets/Scripts/Core/CinematicManager.cs`

**주요 기능**:
- VR 환경에서 영상 재생
- OVRCameraRig.centerEyeAnchor 기준 동적 위치 조정
- VideoPlayer + Canvas + RawImage 파이프라인
- 페이드 인/아웃 효과
- VR 트리거 스킵 기능
- 게임 일시정지/재개 시스템

**검증된 설정값**:
```csharp
// Canvas 크기 (16:9 비율)
canvasRect.sizeDelta = new Vector2(8f, 4.5f);

// 거리 설정
Vector3 targetPosition = vrCamera.position + vrCamera.forward * 4f;
targetPosition += Vector3.up * 0.1f;

// RenderTexture 해상도
RenderTexture renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
```

**VR 입력**:
```csharp
// 스킵 기능
OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)
OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)
```

### 2. Necromancer 캐릭터 시스템
**위치**: `Assets/Scripts/Characters/Necromancer.cs`

**구현된 기능**:
- 기본 이동 및 애니메이션
- 체력 시스템 (100 HP)
- 피격 효과 (빨간색 플래시)
- 사망 처리 로직
- VR 플레이어 추적 AI
- 공격 패턴 기본 구조

### 3. 게임 진행 플로우
```
게임 시작 → 인트로 영상 자동 재생 → 메인 탐험 → 보스룸 입장 → 보스 인트로 영상 → 보스전 → 엔딩 영상
```

## 절대 금지사항

### 1. BossAI 관련
- **BossAI 스크립트는 더 이상 사용하지 않음**
- BossAI 관련 코드 연결 시도 금지
- `FindFirstObjectByType<BossAI>()` 사용 금지
- BossAI.enabled 설정 시도 금지

### 2. 코드 수정 시 주의점
- 기존 작동하는 코드 함부로 변경 금지
- 복잡한 while 루프나 조건문 과도하게 추가 금지
- 불필요한 try-catch 블록 남발 금지
- 과도한 강제 종료 로직 추가 금지
- 절대 경로 하드코딩 금지

### 3. VR 환경 관련
- 키보드 입력 기반 기능 구현 금지
- Desktop 전용 기능 VR에 적용 시도 금지
- Mouse 입력 사용 금지
- UI 요소를 Screen Space로 설정 금지

### 4. 영상 재생 시스템
- VideoPlayer 렌더링 모드 함부로 변경 금지
- RenderTexture 과도한 해제 로직 추가 금지
- Canvas 크기를 극단적으로 변경 금지
- 복잡한 영상 완료 감지 로직 구현 금지

## 핵심 파일 구조

```
Assets/Scripts/
├── Core/
│   ├── CinematicManager.cs      // 영상 재생 메인 시스템
│   ├── GameProgressManager.cs   // 게임 진행 상태 관리
│   └── VolumeManager.cs        // 오디오 관리
├── Characters/
│   ├── Necromancer.cs          // 네크로맨서 캐릭터
│   └── CultistAI.cs           // 컬티스트 AI
└── Managers/
    └── (기타 매니저 스크립트들)

Assets/Videos/
└── VR_Sinema1.mp4             // 인트로 영상 (8초)
```

## Inspector 설정 가이드

### CinematicManager 설정
1. **Cinematic Videos**:
   - Intro Video: VR_Sinema1 할당
   - Boss Intro Video: (필요시 할당)
   - Ending Video: (필요시 할당)

2. **Video Player Setup**:
   - Video Canvas: Canvas 오브젝트 드래그
   - 나머지는 자동 생성됨

3. **Settings**:
   - Allow Skip: 체크
   - Skip Key: Space
   - Fade Time: 1

4. **Door Control**:
   - Auto Find Door: 체크
   - Boss Room Door: 비워두기 (자동 감지)

## 검증된 작동 방식

### 영상 재생 시퀀스
1. 초기화: VideoPlayer + Canvas + RenderTexture 생성
2. 위치 조정: VR 카메라 기준 동적 배치
3. 영상 준비: Prepare() → isPrepared 대기
4. 재생 시작: Play() → 페이드 인
5. 완료 대기: 영상 길이만큼 대기
6. 정리: 페이드 아웃 → Canvas 비활성화 → 상태 리셋

### 게임 상태 관리
- **일시정지**: CultistAI.enabled = false, BGM 볼륨 감소
- **재개**: CultistAI.enabled = true, BGM 볼륨 복구
- **상태 전환**: IntroVideo → MainExploration → BossBattle → GameComplete

## 문제 해결 가이드

### 영상이 재생되지 않을 때
1. Console에서 RenderTexture 연결 로그 확인
2. VideoCanvas가 Inspector에서 할당되었는지 확인
3. 영상 파일이 올바른 경로에 있는지 확인

### 영상이 끝나지 않을 때
1. VR 트리거로 스킵 시도
2. Console에서 영상 길이 로그 확인
3. isPlayingVideo 상태 확인

### VR에서 영상이 보이지 않을 때
1. Canvas RenderMode가 WorldSpace인지 확인
2. OVRCameraRig가 씬에 있는지 확인
3. Canvas 크기와 거리 설정 확인

## 성공 요인

1. **단순한 구조**: 복잡한 로직 대신 직관적인 플로우
2. **VR 최적화**: OVR 전용 입력 및 카메라 시스템 활용
3. **안정적인 상태 관리**: 명확한 boolean 플래그 사용
4. **검증된 설정값**: 테스트를 통해 확인된 크기/거리 값 사용

## 실패했던 시도들 (참고용)

- Camera 렌더링 모드 (CameraFarPlane) - VR에서 불안정
- 과도한 RenderTexture 해제 로직 - 메모리 충돌
- 복잡한 영상 완료 감지 로직 - 타이밍 이슈
- 키보드 기반 강제 종료 - VR 환경 부적합
- BossAI 연동 시도 - 더 이상 사용하지 않는 시스템

## 개발 원칙

1. **작동하는 코드는 건드리지 않기**
2. **VR 환경에 맞는 입력 시스템만 사용**
3. **단순하고 직관적인 로직 선호**
4. **검증된 설정값 유지**
5. **과도한 최적화나 복잡한 로직 지양**

---

**마지막 업데이트**: 2024년 12월
**작성자**: VR Horror Game Development Team 