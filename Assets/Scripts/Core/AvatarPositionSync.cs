// using UnityEngine;

// /// <summary>
// /// 플레이어 컨트롤러와 아바타 사이의 위치를 동기화하는 컴포넌트
// /// </summary>
// public class AvatarPositionSync : MonoBehaviour
// {
//     [Header("References")]
//     public Transform playerController; // PlayerController Transform
//     public Transform avatarRoot;      // HighFidelityCharacter Transform

//     [Header("Offset Settings")]
//     public Vector3 positionOffset = new Vector3(0, 0, 0);
//     public bool lockRotation = false;

//     private OVRBody ovrBody;

//     void Start()
//     {
//         ovrBody = GetComponent<OVRBody>();

//         // 아바타를 플레이어 위치로 초기화
//         SetupAvatarSync();
//     }

//     void SetupAvatarSync()
//     {
//         // OVR Body의 Root Transform 설정 비활성화
//         if (ovrBody != null)
//         {
//             // Root Motion 비활성화
//             var retargeter = GetComponent<OVRUnityHumanoidSkeletonRetargeter>();
//             if (retargeter != null)
//             {
//                 retargeter.ApplyRootMotion = false;
//             }
//         }
//     }

//     void LateUpdate()
//     {
//         if (playerController == null || avatarRoot == null) return;

//         // 위치 동기화 (Y축은 유지)
//         Vector3 syncPosition = playerController.position + positionOffset;
//         syncPosition.y = avatarRoot.position.y; // Y축은 Body Tracking이 유지
//         avatarRoot.position = syncPosition;

//         // 회전 동기화 (옵션)
//         if (!lockRotation)
//         {
//             avatarRoot.rotation = Quaternion.Euler(0, playerController.eulerAngles.y, 0);
//         }
//     }
// } 