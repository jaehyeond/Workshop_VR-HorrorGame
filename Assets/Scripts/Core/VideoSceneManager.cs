using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Video Scene 전용 매니저 - 영상 재생 후 자동 Scene 전환
/// GameFlowManager와 연동하여 작동
/// </summary>
public class VideoSceneManager : MonoBehaviour
{
    [Header("=== Video Settings ===")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "Beta(Map Light)";
    
    [Header("=== Skip Settings ===")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    
    private bool isTransitioning = false;

    #region Unity Lifecycle

    void Start()
    {
        StartCoroutine(PlayVideoAndTransition());
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

    #region Video Playback

    IEnumerator PlayVideoAndTransition()
    {
        // VideoPlayer 찾기 (할당되지 않은 경우)
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[VideoSceneManager] VideoPlayer를 찾을 수 없습니다!");
            TransitionToNextScene();
            yield break;
        }

        // 영상이 할당되지 않은 경우 바로 전환
        if (videoPlayer.clip == null)
        {
            Debug.LogWarning("[VideoSceneManager] 영상이 할당되지 않음 - 바로 Scene 전환");
            TransitionToNextScene();
            yield break;
        }

        Debug.Log($"[VideoSceneManager] 영상 재생 시작: {videoPlayer.clip.name}");

        // VideoPlayer가 이미 Play On Awake로 재생 중일 수 있음
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }

        // 영상 완료 대기
        float videoLength = (float)videoPlayer.clip.length;
        float elapsedTime = 0f;

        while (elapsedTime < videoLength + 1f && !isTransitioning)
        {
            if (videoPlayer.isPlaying)
            {
                elapsedTime += Time.deltaTime;
            }
            else if (elapsedTime >= videoLength - 0.5f)
            {
                // 영상 재생 완료
                break;
            }

            yield return null;
        }

        // Scene 전환
        if (!isTransitioning)
        {
            TransitionToNextScene();
        }
    }

    void SkipVideo()
    {
        Debug.Log("[VideoSceneManager] 영상 스킵됨");
        TransitionToNextScene();
    }

    void TransitionToNextScene()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        
        Debug.Log($"[VideoSceneManager] Scene 전환: {SceneManager.GetActiveScene().name} → {nextSceneName}");
        
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