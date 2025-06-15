using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Video Scene 전용 매니저 - 영상 재생 후 자동 Scene 전환
/// VR 환경 최적화 포함
/// </summary>
public class VideoSceneManager : MonoBehaviour
{
    [Header("=== Video Settings ===")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "Beta(Map Light)";
    
    [Header("=== Skip Settings ===")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    
    [Header("=== VR Settings ===")]
    [SerializeField] private bool forceVRSetup = true;
    [SerializeField] private float videoDistance = 12f;
    [SerializeField] private Vector2 videoSize = new Vector2(20f, 11.25f);
    
    [Header("=== VR FOV Settings ===")]
    [SerializeField] private bool useCalculatedSize = true;
    [SerializeField] [Range(30f, 90f)] private float horizontalFOV = 65f;
    [SerializeField] [Range(20f, 60f)] private float verticalFOV = 36.5f;
    
    private bool isTransitioning = false;
    private Canvas videoCanvas;
    private RawImage videoScreen;

    #region Unity Lifecycle

    void Start()
    {
        StartCoroutine(SetupAndPlayVideo());
    }

    void Update()
    {
        // 스킵 기능
        if (allowSkip && !isTransitioning)
        {
            if (Input.GetKeyDown(skipKey) || 
                OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) ||
                OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            {
                SkipVideo();
            }
        }
    }

    #endregion

    #region Video Setup and Playback

    IEnumerator SetupAndPlayVideo()
    {
        Debug.Log("[VideoSceneManager] VR 영상 시스템 설정 시작...");
        
        // VideoPlayer 찾기
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[VideoSceneManager] VideoPlayer를 찾을 수 없습니다!");
            yield return new WaitForSeconds(1f);
            TransitionToNextScene();
            yield break;
        }

        // Canvas와 RawImage 찾기
        SetupVRVideoComponents();

        // 영상이 할당되지 않은 경우
        if (videoPlayer.clip == null)
        {
            Debug.LogWarning("[VideoSceneManager] 영상이 할당되지 않음 - 3초 후 Scene 전환");
            yield return new WaitForSeconds(3f);
            TransitionToNextScene();
            yield break;
        }

        // VR 환경에서 VideoPlayer 설정 최적화
        OptimizeVideoPlayerForVR();

        Debug.Log($"[VideoSceneManager] 영상 재생 시작: {videoPlayer.clip.name} (길이: {videoPlayer.clip.length:F1}초)");

        // VideoPlayer 준비 대기
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        Debug.Log("[VideoSceneManager] VideoPlayer 준비 완료");

        // 영상 재생 시작
        videoPlayer.Play();
        
        // 재생 시작 확인
        float waitTime = 0f;
        while (!videoPlayer.isPlaying && waitTime < 3f)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        if (!videoPlayer.isPlaying)
        {
            Debug.LogError("[VideoSceneManager] 영상 재생 시작 실패!");
            TransitionToNextScene();
            yield break;
        }

        Debug.Log("[VideoSceneManager] 영상 재생 중...");

        // 영상 완료 대기
        yield return StartCoroutine(WaitForVideoCompletion());

        // Scene 전환
        TransitionToNextScene();
    }

    void SetupVRVideoComponents()
    {
        // Canvas 찾기
        videoCanvas = FindFirstObjectByType<Canvas>();
        if (videoCanvas != null)
        {
            // VR용 WorldSpace Canvas 설정
            if (forceVRSetup)
            {
                videoCanvas.renderMode = RenderMode.WorldSpace;
                
                // VR 카메라 찾기 (OVRCameraRig 또는 MainCamera)
                Transform cameraTransform = FindVRCamera();
                
                if (cameraTransform != null)
                {
                    Vector3 canvasPosition = cameraTransform.position + cameraTransform.forward * videoDistance;
                    canvasPosition.y += 2f; // 높이 조정
                    videoCanvas.transform.position = canvasPosition;
                    videoCanvas.transform.LookAt(cameraTransform);
                    videoCanvas.transform.Rotate(0, 180, 0); // 뒤집기
                    
                    Debug.Log($"[VideoSceneManager] VR 카메라 발견: {cameraTransform.name}");
                }
                else
                {
                    videoCanvas.transform.position = new Vector3(0, 2f, videoDistance);
                    videoCanvas.transform.rotation = Quaternion.identity;
                    Debug.LogWarning("[VideoSceneManager] VR 카메라를 찾을 수 없음 - 기본 위치 사용");
                }

                // Canvas 크기 설정
                Vector2 finalSize = useCalculatedSize ? CalculateOptimalVRSize(videoDistance) : videoSize;
                
                RectTransform canvasRect = videoCanvas.GetComponent<RectTransform>();
                canvasRect.sizeDelta = finalSize;
                
                Debug.Log($"[VideoSceneManager] VR Canvas 설정 완료: 위치={videoCanvas.transform.position}, 거리={videoDistance}m, 크기={finalSize}, 계산모드={useCalculatedSize}");
            }
        }

        // RawImage 찾기
        videoScreen = FindFirstObjectByType<RawImage>();
        if (videoScreen != null)
        {
            Debug.Log("[VideoSceneManager] VideoScreen 발견");
        }
    }

    Transform FindVRCamera()
    {
        // OVRCameraRig의 CenterEyeAnchor 찾기 (우선순위 1)
        GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
        if (ovrCameraRig != null)
        {
            Transform centerEye = ovrCameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
            if (centerEye != null)
            {
                Debug.Log("[VideoSceneManager] OVRCameraRig CenterEyeAnchor 발견");
                return centerEye;
            }
            
            // TrackingSpace가 없는 경우 직접 찾기
            Transform[] children = ovrCameraRig.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name == "CenterEyeAnchor")
                {
                    Debug.Log("[VideoSceneManager] OVRCameraRig CenterEyeAnchor 직접 발견");
                    return child;
                }
            }
        }

        // VRCameraRig 찾기 (우선순위 2)
        GameObject vrCameraRig = GameObject.Find("VRCameraRig");
        if (vrCameraRig != null)
        {
            Transform mainCamera = vrCameraRig.transform.Find("MainCamera");
            if (mainCamera != null)
            {
                Debug.Log("[VideoSceneManager] VRCameraRig MainCamera 발견");
                return mainCamera;
            }
        }

        // 일반 MainCamera 찾기 (우선순위 3)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log("[VideoSceneManager] 일반 MainCamera 발견");
            return mainCam.transform;
        }

        // 태그로 MainCamera 찾기 (우선순위 4)
        GameObject mainCameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCameraObj != null)
        {
            Debug.Log("[VideoSceneManager] 태그로 MainCamera 발견");
            return mainCameraObj.transform;
        }

        Debug.LogWarning("[VideoSceneManager] 어떤 카메라도 찾을 수 없음");
        return null;
    }

    Vector2 CalculateOptimalVRSize(float distance)
    {
        // VR에서 편안한 시청을 위한 시야각 계산
        // 인간의 수평 시야각: 약 110도, 수직 시야각: 약 70도
        // 편안한 영상 시청: 수평 60-70도, 수직 35-40도 권장
        
        // 각도를 라디안으로 변환
        float horizontalRadians = horizontalFOV * Mathf.Deg2Rad;
        float verticalRadians = verticalFOV * Mathf.Deg2Rad;
        
        // 거리에 따른 실제 크기 계산
        float width = 2f * distance * Mathf.Tan(horizontalRadians / 2f);
        float height = 2f * distance * Mathf.Tan(verticalRadians / 2f);
        
        Debug.Log($"[VideoSceneManager] VR 크기 계산: 거리={distance}m, FOV=({horizontalFOV}°, {verticalFOV}°), 크기=({width:F1}, {height:F1})");
        
        return new Vector2(width, height);
    }

    void OptimizeVideoPlayerForVR()
    {
        // VR 최적화 설정
        videoPlayer.playOnAwake = false; // 수동 제어
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = true;
        
        // RenderTexture 모드 확인
        if (videoPlayer.renderMode == VideoRenderMode.RenderTexture)
        {
            if (videoPlayer.targetTexture == null)
            {
                // RenderTexture 생성
                RenderTexture renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                renderTexture.Create();
                videoPlayer.targetTexture = renderTexture;
                
                // RawImage에 할당
                if (videoScreen != null)
                {
                    videoScreen.texture = renderTexture;
                }
                
                Debug.Log("[VideoSceneManager] RenderTexture 생성 및 할당 완료");
            }
        }

        // AudioSource 설정
        AudioSource audioSource = videoPlayer.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f; // 2D 사운드
            audioSource.volume = 1f;
            Debug.Log("[VideoSceneManager] AudioSource 설정 완료");
        }
    }

    IEnumerator WaitForVideoCompletion()
    {
        float videoLength = (float)videoPlayer.clip.length;
        float elapsedTime = 0f;
        float lastLogTime = 0f;

        while (elapsedTime < videoLength + 1f && !isTransitioning)
        {
            if (videoPlayer.isPlaying)
            {
                elapsedTime += Time.deltaTime;
                
                // 5초마다 진행상황 로그
                if (elapsedTime - lastLogTime >= 5f)
                {
                    Debug.Log($"[VideoSceneManager] 영상 재생 중... {elapsedTime:F1}/{videoLength:F1}초");
                    lastLogTime = elapsedTime;
                }
            }
            else
            {
                // 영상이 멈췄는지 확인
                if (elapsedTime >= videoLength - 0.5f)
                {
                    Debug.Log("[VideoSceneManager] 영상 재생 완료");
                    break;
                }
                else if (elapsedTime > 1f) // 1초 이후에 멈췄다면 문제
                {
                    Debug.LogWarning($"[VideoSceneManager] 영상이 예상치 못하게 멈춤 (진행시간: {elapsedTime:F1}초)");
                    yield return new WaitForSeconds(1f);
                    
                    if (!videoPlayer.isPlaying)
                    {
                        Debug.LogError("[VideoSceneManager] 영상 재생 중단됨 - Scene 전환");
                        break;
                    }
                }
            }

            yield return null;
        }
    }

    void SkipVideo()
    {
        Debug.Log("[VideoSceneManager] 영상 스킵됨");
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        TransitionToNextScene();
    }

    void TransitionToNextScene()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        
        Debug.Log($"[VideoSceneManager] Scene 전환: {SceneManager.GetActiveScene().name} → {nextSceneName}");
        
        // VideoPlayer 정리
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // GameFlowManager에 영상 완료 알림
        NotifyVideoCompleted();
        
        // Scene 전환
        SceneManager.LoadScene(nextSceneName);
    }

    #endregion

    #region GameFlowManager Integration

    void NotifyVideoCompleted()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // GameFlowManager에 영상 완료 알림
        if (GameFlowManager.Instance != null)
        {
            switch (currentSceneName)
            {
                case "IntroVideo":
                    GameFlowManager.Instance.SetGameState(GameFlowManager.GameState.MainExploration);
                    break;
                    
                case "BossIntroVideo":
                    GameFlowManager.Instance.SetGameState(GameFlowManager.GameState.BossBattle);
                    break;
                    
                case "EndingVideo":
                    GameFlowManager.Instance.SetGameState(GameFlowManager.GameState.GameComplete);
                    break;
            }
        }
        
        // PlayerPrefs에도 저장 (호환성)
        UpdatePlayerPrefs(currentSceneName);
    }

    void UpdatePlayerPrefs(string sceneName)
    {
        switch (sceneName)
        {
            case "IntroVideo":
                PlayerPrefs.SetInt("HasSeenIntro", 1);
                break;
                
            case "BossIntroVideo":
                PlayerPrefs.SetInt("HasSeenBossIntro", 1);
                break;
                
            case "EndingVideo":
                PlayerPrefs.SetInt("HasSeenEnding", 1);
                break;
        }
        
        PlayerPrefs.Save();
        Debug.Log($"[VideoSceneManager] {sceneName} 완료 상태 저장");
    }

    #endregion
} 