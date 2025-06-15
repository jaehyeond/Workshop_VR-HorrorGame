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
    [SerializeField] private float videoDistance = 5f;
    [SerializeField] private Vector2 videoSize = new Vector2(16f, 9f);
    
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
                
                // Canvas 위치 설정 (플레이어 앞)
                Transform cameraTransform = Camera.main?.transform;
                if (cameraTransform != null)
                {
                    Vector3 canvasPosition = cameraTransform.position + cameraTransform.forward * videoDistance;
                    videoCanvas.transform.position = canvasPosition;
                    videoCanvas.transform.LookAt(cameraTransform);
                    videoCanvas.transform.Rotate(0, 180, 0); // 뒤집기
                }
                else
                {
                    videoCanvas.transform.position = new Vector3(0, 2f, videoDistance);
                    videoCanvas.transform.rotation = Quaternion.identity;
                }

                // Canvas 크기 설정
                RectTransform canvasRect = videoCanvas.GetComponent<RectTransform>();
                canvasRect.sizeDelta = videoSize;
                
                Debug.Log($"[VideoSceneManager] VR Canvas 설정 완료: 위치={videoCanvas.transform.position}, 크기={videoSize}");
            }
        }

        // RawImage 찾기
        videoScreen = FindFirstObjectByType<RawImage>();
        if (videoScreen != null)
        {
            Debug.Log("[VideoSceneManager] VideoScreen 발견");
        }
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