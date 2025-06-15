# VR Horror Game - VolumeManager 시스템 가이드

## 📋 개요

VolumeManager는 VR Horror Game의 **통합 사운드 관리 시스템**입니다. BGM, SFX, 3D 공간 사운드, 상황별 음악 전환을 모두 하나의 시스템에서 관리합니다.

## 🎯 주요 기능

### 🎵 BGM 시스템
- **자동 상황별 전환**: 플레이어 체력, 전투 상황에 따른 BGM 자동 변경
- **부드러운 페이드**: 2초 페이드 인/아웃으로 자연스러운 전환
- **환경음 지원**: BGM과 별도의 환경음 채널

### 🔊 SFX 시스템
- **풀링 시스템**: 8개 AudioSource 풀링으로 성능 최적화
- **3D 공간 사운드**: 위치 기반 사운드와 오브젝트 추적
- **랜덤 재생**: 같은 효과음의 여러 버전 랜덤 재생
- **피치 변조**: 자동 피치 변화로 사운드 다양성 증가

### 🎚️ 볼륨 제어
- **4단계 볼륨**: Master, BGM, SFX, Spatial SFX 개별 조절
- **자동 저장**: PlayerPrefs를 통한 설정 자동 저장/로드
- **데시벨 변환**: 선형 볼륨을 데시벨로 자동 변환

## 🛠️ 설정 방법

### 1. 자동 설정 (권장)

```
Unity 메뉴 → VR Horror Game → Audio → Volume Manager Setup
```

1. **"🎵 Complete Volume Manager Setup"** 버튼 클릭
2. AudioMixer, 프리팹, 씬 배치가 자동으로 완료됩니다
3. Inspector에서 사운드 클립들을 할당하세요

### 2. 수동 설정

#### AudioMixer 생성
```
Assets/Audio/VR_Horror_AudioMixer.mixer
```
- Master, BGM, SFX, Spatial SFX 그룹 생성

#### VolumeManager 프리팹 생성
```
Assets/Prefabs/VolumeManager.prefab
```

#### 씬에 배치
- VolumeManager 프리팹을 씬 루트에 배치
- DontDestroyOnLoad로 씬 전환 시에도 유지

## 🎼 BGM 타입별 설명

| BGM 타입 | 설명 | 자동 전환 조건 |
|---------|------|---------------|
| `Exploration` | 평상시 탐험 BGM | 기본 상태 |
| `Tension` | 긴장감 BGM | Enemy 15m 이내 접근 |
| `Combat` | 일반 전투 BGM | Enemy 5m 이내 또는 Boss 10m 이내 |
| `BossBattle` | 보스전 BGM | Boss 등장 시 |
| `Horror` | 공포 상황 BGM | 플레이어 체력 25% 이하 |
| `Victory` | 승리 BGM | Boss 사망 시 |
| `GameOver` | 게임 오버 BGM | 플레이어 사망 시 |

## 🔊 SFX 타입별 설명

### Player SFX
- `PlayerDamage`: 피격 사운드
- `PlayerHeal`: 회복 사운드  
- `PlayerDeath`: 사망 사운드
- `PlayerHeartbeat`: 심장박동 (체력 위험 시)
- `PlayerBreathing`: 숨소리

### Boss SFX
- `BossIntro`: 등장 사운드
- `BossAttack`: 일반 공격
- `BossHeavyAttack`: 강공격
- `BossChargeAttack`: 돌진 공격
- `BossAreaAttack`: 범위 공격
- `BossPhaseTransition`: 단계 전환
- `BossDeath`: 사망
- `BossRage`: 분노 상태

### Enemy SFX
- `EnemyAttack`: 공격 사운드
- `EnemyDeath`: 사망 사운드
- `EnemySpotPlayer`: 플레이어 발견
- `EnemyFootsteps`: 발소리

### Weapon SFX
- `AxeSwing`: 도끼 휘두르기
- `AxeHit`: 도끼 타격
- `AxeEquip`: 도끼 장착
- `AxeUnequip`: 도끼 해제

## 💻 코드 사용법

### BGM 제어
```csharp
// 특정 BGM 재생
VolumeManager.Instance.PlayBGM(VolumeManager.BGMType.BossBattle);

// BGM 정지
VolumeManager.Instance.StopBGM();

// 보스전 BGM (단축 메서드)
VolumeManager.Instance.PlayBossBattleBGM();
```

### SFX 재생
```csharp
// 기본 SFX 재생
VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.PlayerDamage);

// 3D 위치 기반 SFX
VolumeManager.Instance.PlaySFX(
    VolumeManager.SFXType.BossAttack, 
    transform.position
);

// 오브젝트 추적 SFX
VolumeManager.Instance.PlaySFX(
    VolumeManager.SFXType.EnemyAttack, 
    transform.position, 
    transform  // 이 Transform을 따라다님
);
```

### 볼륨 제어
```csharp
// 마스터 볼륨 설정 (0.0 ~ 1.0)
VolumeManager.Instance.SetMasterVolume(0.8f);

// BGM 볼륨 설정
VolumeManager.Instance.SetBGMVolume(0.7f);

// SFX 볼륨 설정
VolumeManager.Instance.SetSFXVolume(0.9f);

// 현재 볼륨 확인
float currentBGMVolume = VolumeManager.Instance.BGMVolume;
```

## 🔧 기존 스크립트 전환

### 자동 전환
```
Volume Manager Setup → "모든 스크립트를 VolumeManager 사용으로 전환"
```

### 수동 전환
각 스크립트의 Inspector에서 **"Use Volume Manager"** 체크박스를 활성화하세요.

#### 전환된 스크립트들:
- ✅ **BossAI.cs**: 모든 Boss 사운드
- ✅ **EnemyAttackSystem.cs**: Enemy 공격 사운드  
- ✅ **AxeWeapon.cs**: 무기 사운드
- ✅ **VRPlayerHealth.cs**: Player 피격/회복/사망 사운드

## 🎨 Inspector 설정

### BGM Clips 설정
1. **BGM Clips** 배열 크기를 BGM 타입 개수만큼 설정
2. 각 요소에 BGM 타입과 AudioClip 할당
3. 볼륨, 루프, 환경음 여부 설정

### SFX Clips 설정  
1. **SFX Clips** 배열 크기를 SFX 타입 개수만큼 설정
2. 각 요소에 SFX 타입과 AudioClip 배열 할당
3. 볼륨, 피치 범위, 3D 사운드 여부, 최대 거리 설정

### AudioMixer 할당
- Master Mixer Group
- BGM Mixer Group  
- SFX Mixer Group
- Spatial SFX Mixer Group

## 🚀 성능 최적화

### AudioSource 풀링
- **SFX**: 8개 AudioSource 재사용
- **3D Spatial**: 최대 16개, 자동 정리
- **메모리 효율**: 불필요한 AudioSource 자동 제거

### 자동 BGM 전환
- **거리 기반**: Enemy/Boss와의 거리로 전투 상태 판단
- **체력 기반**: 플레이어 체력에 따른 공포 BGM 전환
- **상태 캐싱**: 불필요한 전환 방지

## 🐛 문제 해결

### VolumeManager.Instance가 null인 경우
```csharp
if (VolumeManager.Instance != null)
{
    VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.PlayerDamage);
}
```

### 사운드가 재생되지 않는 경우
1. **AudioClip 할당 확인**: Inspector에서 클립이 할당되었는지 확인
2. **AudioMixer 설정**: 각 그룹이 올바르게 할당되었는지 확인  
3. **볼륨 설정**: 마스터 볼륨이 0이 아닌지 확인
4. **3D 사운드**: 거리가 maxDistance 이내인지 확인

### BGM이 전환되지 않는 경우
1. **VRPlayerHealth 확인**: 플레이어 체력 시스템이 있는지 확인
2. **Enemy/Boss 확인**: 해당 컴포넌트들이 씬에 있는지 확인
3. **거리 확인**: VR 카메라와의 거리 계산이 정확한지 확인

## 📝 추가 기능

### 커스텀 BGM 타입 추가
```csharp
// VolumeManager.cs의 BGMType enum에 추가
public enum BGMType
{
    // ... 기존 타입들
    CustomBGM  // 새로운 타입 추가
}
```

### 커스텀 SFX 타입 추가
```csharp
// VolumeManager.cs의 SFXType enum에 추가  
public enum SFXType
{
    // ... 기존 타입들
    CustomSFX  // 새로운 타입 추가
}
```

## 🎯 베스트 프랙티스

1. **사운드 클립 최적화**: 압축 설정으로 메모리 사용량 최소화
2. **3D 사운드 거리**: maxDistance를 적절히 설정하여 성능 최적화
3. **BGM 길이**: 루프 가능한 BGM으로 자연스러운 반복
4. **SFX 다양성**: 같은 효과음의 여러 버전으로 단조로움 방지
5. **볼륨 밸런싱**: BGM과 SFX의 적절한 볼륨 비율 유지

---

## 📞 지원

문제가 발생하거나 추가 기능이 필요한 경우, VolumeManager 시스템은 확장 가능하도록 설계되었습니다. 새로운 사운드 타입이나 기능을 쉽게 추가할 수 있습니다. 