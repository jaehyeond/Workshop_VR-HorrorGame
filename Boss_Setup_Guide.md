# 🏆 Unity Scene 보스 시스템 설정 가이드

## Step 4: 필수 컴포넌트 추가

Boss_CultLeader 게임오브젝트 선택 후 Inspector에서:

### A. NavMesh Agent 추가
```
Component > Navigation > Nav Mesh Agent
설정:
- Agent Type: Humanoid
- Base Offset: 0
- Speed: 2 (BossAI에서 제어됨)
- Angular Speed: 120
- Acceleration: 8
- Stopping Distance: 2.5
- Auto Braking: ✓
- Radius: 0.5
- Height: 2
```

### B. Animator 추가
```
Component > Miscellaneous > Animator
설정:
- Controller: (Step 5에서 생성)
- Avatar: (모델의 Avatar 사용)
- Apply Root Motion: ✓
- Update Mode: Normal
- Culling Mode: Cull Update Transforms
```

### C. Audio Source 추가
```
Component > Audio > Audio Source
설정:
- AudioClip: (비워둠 - 스크립트에서 제어)
- Play On Awake: ✗
- Loop: ✗
- Volume: 0.8
- Spatial Blend: 1 (3D)
- Doppler Level: 1
- Rolloff Mode: Logarithmic
- Min Distance: 1
- Max Distance: 50
```

### D. Rigidbody 추가 (선택사항)
```
Component > Physics > Rigidbody
설정:
- Mass: 1
- Drag: 0
- Angular Drag: 0.05
- Use Gravity: ✓
- Is Kinematic: ✗
- Freeze Rotation: Y축만 체크 (보스가 넘어지지 않도록)
```

### E. Capsule Collider 추가
```
Component > Physics > Capsule Collider
설정:
- Is Trigger: ✗
- Material: (없음)
- Center: (0, 1, 0)
- Radius: 0.5
- Height: 2
- Direction: Y-Axis
```

## Step 5: 애니메이터 컨트롤러 생성

### A. 자동 생성 방법 (권장)
```
1. Window > VR Horror Game > Boss Animation Setup
2. "🎭 보스 애니메이터 컨트롤러 생성" 버튼 클릭
3. Assets/Animations/BossAnimatorController.controller 생성됨
4. Boss GameObject의 Animator Component에 할당
```

### B. 수동 생성 방법
```
1. Project 창에서 우클릭 > Create > Animator Controller
2. 이름: "BossAnimatorController"
3. 더블클릭하여 Animator 윈도우 열기
4. Parameters 탭에서 필요한 파라미터들 수동 추가:
   - Speed (Float)
   - BossPhase (Int)
   - IsPatrolling (Bool)
   - IsInCombat (Bool)
   - IsAttacking (Bool)
   - IsRaging (Bool)
   - IsDead (Bool)
   - BasicAttack (Trigger)
   - HeavyAttack (Trigger)
   - ChargeAttack (Trigger)
   - AreaAttack (Trigger)
   - Rage (Trigger)
   - Hit (Trigger)
   - Die (Trigger)
```

## Step 6: Mixamo 애니메이션 다운로드 및 설정

### A. Mixamo에서 애니메이션 다운로드
```
필수 애니메이션:
1. Idle - 기본 대기
2. Walking - 걷기
3. Running - 달리기
4. Punching - 기본 공격
5. Heavy Attack - 강공격
6. Charge - 돌진
7. Roaring - 분노
8. Hit Reaction - 피격
9. Death - 사망

다운로드 설정:
- Format: FBX for Unity
- Skin: With Skin
- Keyframe Reduction: None
```

### B. Unity에 임포트
```
1. 다운로드한 FBX 파일들을 Assets/Animations/Boss/ 폴더에 복사
   (총 9개 애니메이션 파일)
2. 각 애니메이션 파일 선택 후 Inspector에서:
   - Rig > Animation Type: Humanoid
   - Animation > Import Settings:
     - Animation Name: 적절한 이름으로 변경
     - Loop Time: 필요에 따라 체크
   - Apply 클릭
```

### C. 애니메이터에 애니메이션 할당
```
1. BossAnimatorController 더블클릭
2. 각 State를 선택하고 Motion 필드에 해당 애니메이션 할당:
   - Idle State → Idle 애니메이션
   - Walk State → Walking 애니메이션
   - Run State → Running 애니메이션
   - Basic Attack State → Punching 애니메이션
   - Heavy Attack State → Heavy Attack 애니메이션
   - Charge Attack State → Charge 애니메이션
   - Area Attack State → Heavy Attack 애니메이션 (재사용)
   - Rage State → Roaring 애니메이션
   - Hit State → Hit Reaction 애니메이션
   - Death State → Death 애니메이션
```

## Step 7: 보스 AI 스크립트 추가

```
1. Boss_CultLeader GameObject 선택
2. Inspector 하단의 "Add Component" 클릭
3. Scripts에서 "BossAI" 검색하여 추가
4. Scripts에서 "BossStateMachine" 검색하여 추가
```

## Step 8: BossAI 컴포넌트 설정

Inspector에서 BossAI 컴포넌트 설정:

### 보스 기본 설정
```
- Boss Name: "Cult Leader"
- Max Health: 500
- Current Health: 500 (자동 설정됨)
```

### 단계별 설정
```
- Phase2 Health Threshold: 0.7
- Phase3 Health Threshold: 0.3
```

### 이동 설정
```
- Walk Speed: 2
- Run Speed: 5
- Charge Speed: 8
```

### 감지 설정
```
- Detection Range: 15
- Attack Range: 2.5
- Special Attack Range: 8
```

### 공격 설정
```
- Basic Attack Damage: 40
- Heavy Attack Damage: 60
- Special Attack Damage: 80
- Attack Cooldown: 2
- Special Attack Cooldown: 8
```

### 애니메이션 설정
```
- Intro Animation Duration: 3
- Phase Transition Duration: 2
```

### VR 효과
```
- Screen Shake Intensity: 0.5
- Haptic Feedback Intensity: 0.8
```

### 오디오
```
- Phase Sounds: (배열) 단계별 사운드 클립
- Attack Sounds: (배열) 공격 사운드 클립
- Intro Sound: 등장 사운드
- Death Sound: 사망 사운드
```

## Step 9: NavMesh 설정

### A. 바닥 오브젝트 NavMesh 설정
```
1. 바닥 GameObject 선택
2. Inspector 상단의 Static 체크박스 클릭
3. Navigation Static 체크
```

### B. NavMesh Bake
```
1. Window > AI > Navigation
2. Navigation 윈도우에서 Bake 탭 선택
3. 설정:
   - Agent Radius: 0.5
   - Agent Height: 2
   - Max Slope: 45
   - Step Height: 0.4
4. "Bake" 버튼 클릭
5. 파란색 NavMesh가 바닥에 표시되는지 확인
```

## Step 10: 태그 및 레이어 설정

### A. Boss 태그 생성
```
1. Boss GameObject 선택
2. Inspector 상단의 Tag 드롭다운 클릭
3. "Add Tag..." 선택
4. Tags 리스트에서 "+" 클릭
5. "Boss" 입력 후 Save
6. Boss GameObject에 "Boss" 태그 할당
```

### B. Player 태그 확인
```
1. 플레이어 GameObject에 "Player" 태그가 있는지 확인
2. 없다면 위와 같은 방법으로 생성하여 할당
```

## Step 11: 플레이어 참조 설정

VRPlayerHealth 스크립트가 플레이어에 있는지 확인:
```
1. 플레이어 GameObject 선택
2. VRPlayerHealth 컴포넌트가 있는지 확인
3. 없다면 Component > Scripts > VRPlayerHealth 추가
```

## Step 12: 테스트 환경 설정

### A. 간단한 테스트 씬 구성
```
1. 바닥 (Plane) 생성: 10x10 크기
2. 벽 몇 개 배치 (Cube로 생성)
3. 플레이어 스폰 위치 설정
4. 보스 스폰 위치 설정 (플레이어에서 10m 정도 떨어진 곳)
```

### B. 라이팅 설정
```
1. Window > Rendering > Lighting
2. Lighting 윈도우에서:
   - Auto Generate 체크 해제
   - Generate Lighting 클릭
```

## Step 13: 테스트 실행

### A. 플레이 모드 테스트
```
1. Play 버튼 클릭
2. 확인 사항:
   - 보스가 Idle 애니메이션 재생
   - 플레이어 접근 시 감지 여부
   - NavMesh 위에서 정상 이동
   - 애니메이션 전환 정상 여부
```

### B. 디버그 확인
```
1. Scene 뷰에서 보스 선택
2. Gizmos가 표시되는지 확인:
   - 노란색 원: 감지 범위
   - 빨간색 원: 공격 범위
   - 분홍색 원: 특수 공격 범위
```

## 🎯 완성 체크리스트

- ✅ 보스 GameObject 생성
- ✅ 필수 컴포넌트 추가 (NavMeshAgent, Animator, AudioSource, Collider)
- ✅ 애니메이터 컨트롤러 생성 및 할당
- ✅ Mixamo 애니메이션 다운로드 및 설정
- ✅ BossAI, BossStateMachine 스크립트 추가
- ✅ BossAI 컴포넌트 설정 완료
- ✅ NavMesh Bake 완료
- ✅ 태그 설정 ("Boss", "Player")
- ✅ 플레이어 VRPlayerHealth 확인
- ✅ 테스트 환경 구성
- ✅ 플레이 테스트 정상 동작 확인

이 가이드를 따라하면 완전한 보스 시스템이 구성됩니다! 🏆 