# GP 공포 게임 이동 시스템 가이드

이 문서는 Meta XR Movement SDK의 이동 시스템을 GP 공포 게임에 맞게 커스텀하는 방법을 설명합니다.

## 설정 방법

### 1. TogglerActiveState 오류 해결하기

기존 `TogglerActiveState` 컴포넌트가 UI Toggle에 의존하여 발생하는 NullReferenceException을 해결하기 위해 다음 단계를 따르세요:

1. 먼저 Unity 에디터에서 빈 게임 오브젝트를 생성하고 `GPSetup`이라고 이름 짓습니다.
2. `GPSetup` 오브젝트에 `LocomotionSettingsUpdater` 컴포넌트를 추가합니다.
3. `LocomotionSettingsWiring` 오브젝트를 Inspector 창의 `locomotionSettingsWiring` 필드에 할당합니다.
4. `activeMovementTypes` 목록에 MoveDirection 이동 방식을 추가합니다 (계층 구조에서 `LocomotionSettingsWiring` > `ControllerMovementStyle` > `MoveDirection` 오브젝트).
5. "이동 설정 업데이트" 버튼을 클릭하여 TogglerActiveState를 DirectActiveState로 교체합니다.

### 2. GP 이동 관리자 설정하기

1. `GPSetup` 오브젝트에 `GPLocomotionManager` 컴포넌트를 추가합니다.
2. 변환된 DirectActiveState 컴포넌트들을 해당 필드에 할당합니다:
   - `moveDirectionState`: MoveDirection 오브젝트의 DirectActiveState
   - `slideState`: Slide 오브젝트의 DirectActiveState
   - 필요시 `gestureState` 설정
3. GP 게임에 맞게 설정을 조정합니다:
   - `enableMoveDirection`을 true로 설정 (권장)
   - `enableSlide`를 필요에 따라 설정
   - 이동 속도와 앉기/은신 시 속도 감소 비율 조정

### 3. 은신 시스템 설정하기

1. `GPSetup` 오브젝트에 `GPHidingSystem` 컴포넌트를 추가합니다.
2. `headTransform` 필드에 OVRCameraRig의 CenterEyeAnchor를 할당합니다.
3. `locomotionManager` 필드에 이전에 추가한 GPLocomotionManager를 할당합니다.
4. 필요에 따라 다음 설정을 조정합니다:
   - `hidingHeightThreshold`: 이 높이 이하로 내려가면 은신 상태로 인식
   - 진동 피드백 강도와 지속 시간
   - 은신 상태 UI 요소

### 4. 은신 위치 설정하기

GP 게임의 시나리오에 따라 은신할 수 있는 위치를 설정할 수 있습니다:

1. 은신 지점으로 사용할 게임 오브젝트에 Box Collider를 추가하고 "Is Trigger"를 체크합니다.
2. `HidingSpot` 스크립트를 추가합니다.

### 5. 테스트 및 미세 조정

1. 플레이 모드에서 이동 기능이 정상적으로 작동하는지 확인합니다.
2. 앉기/숙이기 동작으로 은신 상태가 제대로 감지되는지 테스트합니다.
3. 필요에 따라 이동 속도, 은신 감지 높이 등의 값을 조정합니다.

## 참고 사항

- 이 시스템은 `ISDKExampleMenu`와 같은 불필요한 UI 요소를 제거하고 기본 이동 시스템만 활용합니다.
- 텔레포트 이동 방식은 제거되었으며, 자연스러운 이동 방식(MoveDirection)만 사용합니다.
- Meta XR Movement SDK의 손 추적 기능은 그대로 유지됩니다.
- `LocomotionEnvironment`의 시각적 요소들은 GP 게임의 환경으로 대체해야 합니다.

## 문제 해결

- NullReferenceException이 계속 발생하면 `IActiveState` 인터페이스를 사용하는 다른 컴포넌트들을 확인하고 필요시 DirectActiveState로 교체합니다.
- 이동 시스템이 작동하지 않으면 콘솔 로그를 확인하고 필요한 참조가 모두 설정되었는지 확인합니다. 