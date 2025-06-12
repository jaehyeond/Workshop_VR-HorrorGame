# VR Horror Game - Cult Escape
> Unity 기반 Meta Quest VR 호러 게임 프로젝트

## 📋 프로젝트 개요

**장르**: VR 호러 서바이벌  
**플랫폼**: Meta Quest (Oculus All-in-One SDK)  
**엔진**: Unity 6 (6000.0.42f1)  
**테마**: 컬트(사이비 종교) 탈출 시뮬레이션  

플레이어는 VR 환경에서 위험한 컬트 시설에서 탈출해야 하며, 도끼를 사용하여 광신도들과 전투를 벌이는 몰입형 호러 게임입니다.

## 🎮 핵심 기능

### 1. VR 이동 시스템 (Locomotion)
- **텔레포트 이동**: 안전한 VR 이동을 위한 포인트 앤 클릭 텔레포트
- **연속 이동**: 부드러운 조이스틱 기반 이동 (멀미 방지 옵션 포함)
- **회전 시스템**: 스냅 턴 및 연속 회전 지원
- **손 추적**: Meta Quest의 핸드 트래킹 기능 활용

### 2. 전투 시스템 (Combat System)

#### 🪓 도끼 무기 시스템
**파일 위치**: `Assets/Scripts/Combat/AxeWeapon.cs`

##### 기본 사양
- **기본 데미지**: 50 HP
- **공격 쿨다운**: 1.0초
- **최소 휘두르기 속도**: 2.0 m/s
- **타격 감지 반경**: 0.8 유닛
- **타겟 레이어**: Enemy 레이어

##### 컨트롤러 입력 시스템
```csharp
// 주요 입력 바인딩
- Grip Button (PrimaryHandTrigger): 도끼 장착/해제
- Index Trigger (PrimaryIndexTrigger): 공격 실행
```

##### 핵심 메커니즘
1. **장착 시스템**
   - Grip 버튼으로 도끼 장착/해제
   - 자동 Hand Anchor 감지 (우선순위: RightHandAnchor → LeftHandAnchor)
   - 장착 시 컨트롤러에 고정, 해제 시 원래 위치로 복귀

2. **공격 시스템**
   - Index Trigger로 공격 트리거
   - 휘두르기 속도 기반 데미지 계산
   - 치명타 시스템 (1.5배 속도 시 1.5배 데미지)
   - Physics.OverlapSphere를 통한 타격 감지

3. **햅틱 피드백**
   - 장착/해제 시: 0.3f 강도 0.2초
   - 일반 타격 시: 0.5f 강도 0.3초
   - 치명타 시: 0.8f 강도 0.5초

##### 공격 로직 상세
```csharp
private void Attack()
{
    // 1. 쿨다운 체크 (1초)
    // 2. 휘두르기 속도 계산
    // 3. 최소 속도 검증 (관대한 기준 적용)
    // 4. Physics.OverlapSphere로 적 감지
    // 5. CultistAI 컴포넌트 확인
    // 6. 데미지 적용 및 이펙트 재생
}
```

### 3. AI 시스템

#### 🧟 광신도 AI (CultistAI)
**파일 위치**: `Assets/Scripts/AI/CultistAI.cs`

##### 기본 스펙
- **체력 시스템**: 100 HP (2타 사망)
- **계급별 체력**: 
  - 일반 광신도: 3타 (150 HP 예정)
  - 간부급: 5타 (250 HP 예정)  
  - 리더급: 7타 (350 HP 예정)

##### 데미지 처리 시스템
```csharp
public void TakeDamage(float damage, Vector3 attackPosition)
{
    currentHealth -= damage;
    
    // 히트 애니메이션 트리거
    if (animator != null)
        animator.SetTrigger("Hit");
        
    // 사망 처리
    if (currentHealth <= 0 && !isDead)
    {
        Die();
    }
}
```

#### 🎯 상태 머신 (CultistStateMachine)
**파일 위치**: `Assets/Scripts/AI/CultistStateMachine.cs`

##### 초기화 시스템
```csharp
private void InitializeAnimatorParameters()
{
    animator.SetBool("IsDead", false);
    animator.SetTrigger("Hit");      // 히트 파라미터 초기화
    animator.SetTrigger("Die");      // 사망 파라미터 초기화
}
```

#### 🎮 AI 매니저 (CultistManager)
- 다중 광신도 관리
- 스폰 시스템
- 전역 AI 상태 제어

### 4. 애니메이션 시스템

#### 애니메이터 컨트롤러 설정
**파라미터**:
- `Hit` (Trigger): 피격 애니메이션 트리거
- `Die` (Trigger): 사망 애니메이션 트리거  
- `IsDead` (Bool): 사망 상태 플래그

#### 상태 전환
```
Any State → Enemy_Hit (조건: Hit 트리거)
Any State → Enemy_Death (조건: Die 트리거)
```

#### 전환 설정
- **Has Exit Time**: false (즉시 전환)
- **Transition Duration**: 0.1초 (부드러운 전환)
- **Interruption Source**: Current State (현재 상태 우선)

## 🔧 기술적 구현 세부사항

### 입력 시스템 전환
**변경 전**: HandGrab Interaction 기반
```csharp
// 문제: API 호환성 이슈
SelectingInteractor // 더 이상 지원되지 않음
```

**변경 후**: OVRInput Controller 기반
```csharp
// 해결: 직접 컨트롤러 입력 처리
bool gripPressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controllerType);
bool attackPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controllerType);
```

### Hand Anchor 자동 감지
```csharp
private Transform FindHandAnchor()
{
    // 우선순위 1: 태그 기반 검색
    GameObject rightHand = GameObject.FindGameObjectWithTag("RightHandAnchor");
    if (rightHand != null) return rightHand.transform;
    
    // 우선순위 2: 이름 기반 검색
    rightHand = GameObject.Find("RightHandAnchor");
    if (rightHand != null) return rightHand.transform;
    
    // 우선순위 3: LeftHandAnchor로 대체
    GameObject leftHand = GameObject.Find("LeftHandAnchor");
    if (leftHand != null) return leftHand.transform;
    
    return null; // 실패 시
}
```

### 디버그 시스템
모든 주요 기능에 광범위한 로깅 시스템 구현:

```csharp
Debug.Log("[AxeWeapon] Attack 메서드 호출됨!");
Debug.Log($"[AxeWeapon] 휘두르기 속도: {swingSpeed:F2}");
Debug.Log($"[AxeWeapon] 타격 감지 시작 - 위치: {axeHead.position}");
Debug.Log($"[AxeWeapon] 감지된 콜라이더 수: {hitColliders.Length}");
```

### 성능 최적화
- **Object Pooling**: 이펙트 시스템용 (구현 예정)
- **LOD 시스템**: 거리별 AI 상세도 조절 (구현 예정)
- **Occlusion Culling**: VR 최적화를 위한 컬링 시스템

## 📁 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Combat/
│   │   └── AxeWeapon.cs          # 도끼 전투 시스템
│   ├── AI/
│   │   ├── CultistAI.cs          # 광신도 AI 로직
│   │   ├── CultistStateMachine.cs # AI 상태 머신
│   │   └── CultistManager.cs     # AI 매니저
│   └── VR/
│       └── [VR 관련 스크립트들]
├── Scenes/
│   └── Revert.unity              # 메인 게임 씬 (727KB)
├── Prefabs/
│   ├── Cultist/                  # 광신도 프리팹들
│   └── Weapons/                  # 무기 프리팹들
└── Animations/
    ├── Cultist/                  # 광신도 애니메이션
    └── Combat/                   # 전투 애니메이션
```

## 🎯 현재 개발 상태

### ✅ 완료된 기능
- [x] VR 기본 이동 시스템
- [x] 도끼 장착/해제 시스템
- [x] 컨트롤러 기반 입력 시스템
- [x] 기본 전투 메커니즘
- [x] AI 체력 시스템
- [x] 애니메이터 파라미터 설정
- [x] 햅틱 피드백 시스템
- [x] 디버그 로깅 시스템

### 🔄 진행 중인 기능
- [ ] 타격 감지 정확도 개선
- [ ] 광신도 히트/데스 애니메이션 트리거
- [ ] 계급별 체력 시스템 구현

### 📋 예정된 기능
- [ ] 사운드 시스템 (타격음, 배경음악, 환경음)
- [ ] 파티클 이펙트 (혈흔, 타격 이펙트)
- [ ] 추가 무기 종류 (칼, 총 등)
- [ ] 레벨 디자인 확장
- [ ] 스토리 시스템
- [ ] 세이브/로드 시스템

## 🛠 개발 도구 및 환경

### Unity 설정
- **버전**: Unity 6 (6000.0.42f1)
- **렌더 파이프라인**: Universal Render Pipeline (URP)
- **XR 플러그인**: Meta XR All-in-One SDK
- **Build Target**: Android (Meta Quest)

### Meta Quest 설정
```json
{
  "SDK": "Meta Quest All-in-One SDK",
  "API Level": "Android API Level 29+",
  "Minimum OS": "Quest OS v28+",
  "Hand Tracking": "Enabled",
  "Guardian System": "Enabled"
}
```

### 필수 패키지
```
com.unity.xr.oculus
com.unity.xr.management
com.unity.xr.interaction.toolkit
```

## 🔍 디버깅 가이드

### 공통 문제 해결

#### 1. 도끼가 장착되지 않는 경우
```csharp
// 체크 포인트:
1. Hand Anchor 오브젝트 존재 확인
2. 컨트롤러 연결 상태 확인
3. OVRInput.Controller.Touch 정상 작동 확인
```

#### 2. 공격이 반응하지 않는 경우
```csharp
// 디버그 단계:
1. Index Trigger 입력 감지 확인
2. Attack() 메서드 호출 로그 확인
3. OverlapSphere 반경 및 레이어 설정 확인
4. CultistAI 컴포넌트 존재 확인
```

#### 3. 애니메이션이 재생되지 않는 경우
```csharp
// 확인 사항:
1. Animator Controller 연결 상태
2. 파라미터 이름 정확성 ("Hit", "Die", "IsDead")
3. 상태 전환 조건 설정
4. 애니메이션 클립 할당 상태
```

## 📊 성능 지표

### VR 최적화 목표
- **프레임레이트**: 72 FPS (Quest 2), 90 FPS (Quest 3)
- **해상도**: 1832x1920 per eye (Quest 2)
- **지연시간**: <20ms (Motion-to-Photon)

### 메모리 사용량
- **총 메모리**: <4GB
- **텍스처 메모리**: <1GB
- **메시 메모리**: <500MB

## 🤝 협업 가이드

### 코드 스타일
```csharp
// 들여쓰기: 스페이스 4칸
// 줄 끝: LF
// 인코딩: UTF-8

public class ExampleClass 
{
    private float exampleFloat = 1.0f;
    
    public void ExampleMethod()
    {
        Debug.Log("[ClassName] 상세한 로그 메시지");
    }
}
```

### 커밋 컨벤션
```
feat: 새로운 기능 추가
fix: 버그 수정
docs: 문서 수정
style: 코드 포맷팅
refactor: 코드 리팩토링
test: 테스트 추가
chore: 빌드 업무 수정
```

## 📚 참고 자료

### Unity VR 개발
- [Unity XR Toolkit Documentation](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest)
- [Meta Quest Developer Center](https://developer.oculus.com/)

### AI 시스템
- [Unity NavMesh Documentation](https://docs.unity3d.com/Manual/nav-NavigationSystem.html)
- [State Machine Pattern](https://gameprogrammingpatterns.com/state.html)

---

**마지막 업데이트**: 2024년 12월  
**개발자**: SOGANG  
**라이선스**: MIT License