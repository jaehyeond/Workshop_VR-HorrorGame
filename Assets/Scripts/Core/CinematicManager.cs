using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// VR Horror Game - 영상 시스템 관리자
/// 첫번째 영상 -> Main Scene -> 두번째 영상 -> Boss Scene -> 세번째 영상
/// </summary>
public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance { get; private set; }

    [Header("=== Cinematic Videos ===")]
    [SerializeField] private VideoClip introVideo;      // 딸 납치 영상
    [SerializeField] private VideoClip bossIntroVideo;  // 보스룸 입장 영상
    [SerializeField] private VideoClip endingVideo;     // 구출 성공 영상

    [Header("=== Video Player Setup ===")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Canvas videoCanvas;
    [SerializeField] private GameObject videoScreen;
    [SerializeField] private AudioSource videoAudioSource;

    [Header("=== Scene Names ===")]
    [SerializeField] private string mainSceneName = "Revert";

    [Header("=== Settings ===")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    [SerializeField] private float fadeTime = 1f;

    // 현재 상태
    private bool isPlayingVideo = false;
    private CinematicType currentCinematic = CinematicType.None;
    
    public enum CinematicType
    {
        None,
        Intro,          // 첫번째 - 딸 납치
        BossIntro,      // 두번째 - 보스룸 입장
        Ending          // 세번째 - 구출 성공
    }

    // Events
    public System.Action<CinematicType> OnCinematicStarted;
    public System.Action<CinematicType> OnCinematicEnded;

    #region Unity Lifecycle

    void Awake()
    {
        // Singleton 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCinematicManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 게임 시작 시 자동으로 인트로 영상 재생
        if (SceneManager.GetActiveScene().name == "Revert" || 
            SceneManager.GetActiveScene().name == mainSceneName)
        {
            StartCoroutine(PlayIntroVideoOnStart());
        }
    }

    void Update()
    {
        // 스킵 기능
        if (isPlayingVideo && allowSkip)
        {
            if (Input.GetKeyDown(skipKey) || 
                OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) ||
                OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            {
                SkipCurrentVideo();
            }
        }
    }

    #endregion

    #region Initialization

    void InitializeCinematicManager()
    {
        // VideoPlayer 설정
        if (videoPlayer == null)
        {
            GameObject videoObj = new GameObject("VideoPlayer");
            videoObj.transform.SetParent(transform);
            videoPlayer = videoObj.AddComponent<VideoPlayer>();
        }

        SetupVideoPlayer();
        SetupVideoCanvas();

        Debug.Log("[CinematicManager] 초기화 완료");
    }

    void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // RenderTexture 생성
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 16);
        videoPlayer.targetTexture = renderTexture;

        // 오디오 설정
        if (videoAudioSource == null)
        {
            videoAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        // 이벤트 연결
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void SetupVideoCanvas()
    {
        if (videoCanvas == null)
        {
            // VR용 World Space Canvas 생성
            GameObject canvasObj = new GameObject("VideoCanvas");
            canvasObj.transform.SetParent(transform);
            
            videoCanvas = canvasObj.AddComponent<Canvas>();
            videoCanvas.renderMode = RenderMode.WorldSpace;
            
            // Canvas 크기 및 위치 설정 (VR 최적화)
            RectTransform canvasRect = videoCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(6f, 3.375f); // 16:9 비율
            
            // VR 카메라 앞 4미터에 배치
            Transform vrCamera = FindVRCamera();
            if (vrCamera != null)
            {
                canvasObj.transform.position = vrCamera.position + vrCamera.forward * 4f;
                canvasObj.transform.LookAt(vrCamera.position);
                canvasObj.transform.rotation *= Quaternion.Euler(0, 180, 0);
            }
        }

        // 초기에는 비활성화
        videoCanvas.gameObject.SetActive(false);
    }

    Transform FindVRCamera()
    {
        // OVRCameraRig의 CenterEyeAnchor 찾기
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null && cameraRig.centerEyeAnchor != null)
        {
            return cameraRig.centerEyeAnchor;
        }

        // 일반 Camera 찾기
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        return null;
    }

    #endregion

    #region Video Playback

    public void PlayCinematic(CinematicType cinematicType)
    {
        if (isPlayingVideo)
        {
            Debug.LogWarning("[CinematicManager] 이미 영상이 재생 중입니다!");
            return;
        }

        VideoClip clipToPlay = GetVideoClip(cinematicType);
        if (clipToPlay == null)
        {
            Debug.LogError($"[CinematicManager] {cinematicType} 영상 클립이 없습니다!");
            return;
        }

        StartCoroutine(PlayVideoCoroutine(cinematicType, clipToPlay));
    }

    IEnumerator PlayVideoCoroutine(CinematicType cinematicType, VideoClip clip)
    {
        Debug.Log($"[CinematicManager] {cinematicType} 영상 재생 시작");
        
        isPlayingVideo = true;
        currentCinematic = cinematicType;

        // VR 편의성을 위해 게임 일시정지
        PauseGameplay();

        // 영상 설정
        videoPlayer.clip = clip;
        videoCanvas.gameObject.SetActive(true);

        // 페이드 인
        yield return StartCoroutine(FadeVideo(0f, 1f));

        // 영상 재생
        videoPlayer.Play();
        
        // 이벤트 호출
        OnCinematicStarted?.Invoke(cinematicType);

        // 영상 완료 대기 (또는 스킵)
        while (videoPlayer.isPlaying && isPlayingVideo)
        {
            yield return null;
        }

        // 영상 종료 처리
        yield return StartCoroutine(EndVideoPlayback());
    }

    IEnumerator EndVideoPlayback()
    {
        Debug.Log($"[CinematicManager] {currentCinematic} 영상 재생 완료");

        // 페이드 아웃
        yield return StartCoroutine(FadeVideo(1f, 0f));

        // 정리
        videoPlayer.Stop();
        videoCanvas.gameObject.SetActive(false);
        
        // 이벤트 호출
        OnCinematicEnded?.Invoke(currentCinematic);

        // 게임 재개
        ResumeGameplay();

        // 다음 씬으로 전환
        HandleSceneTransition();

        // 상태 리셋
        CinematicType completedCinematic = currentCinematic;
        currentCinematic = CinematicType.None;
        isPlayingVideo = false;

        // 게임 진행 상태 업데이트
        GameProgressManager.Instance?.OnCinematicCompleted(completedCinematic);
    }

    void HandleSceneTransition()
    {
        switch (currentCinematic)
        {
            case CinematicType.Intro:
                // 인트로 후 메인 씬으로 (이미 메인 씬이므로 상태만 변경)
                GameProgressManager.Instance?.SetGameState(GameProgressManager.GameState.MainExploration);
                break;
                
            case CinematicType.BossIntro:
                // 보스 인트로 후 보스전 시작
                GameProgressManager.Instance?.SetGameState(GameProgressManager.GameState.BossBattle);
                break;
                
            case CinematicType.Ending:
                // 엔딩 후 게임 완료
                GameProgressManager.Instance?.SetGameState(GameProgressManager.GameState.GameComplete);
                break;
        }
    }

    public void SkipCurrentVideo()
    {
        if (!isPlayingVideo || !allowSkip) return;

        Debug.Log($"[CinematicManager] {currentCinematic} 영상 스킵됨");
        
        videoPlayer.Stop();
        // EndVideoPlayback은 Update의 while 루프에서 자동 호출됨
    }

    IEnumerator FadeVideo(float startAlpha, float endAlpha)
    {
        CanvasGroup canvasGroup = videoCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = videoCanvas.gameObject.AddComponent<CanvasGroup>();
        }

        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeTime);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    #endregion

    #region Game State Management

    void PauseGameplay()
    {
        // VolumeManager BGM 페이드
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.SetBGMVolume(0.1f);
        }

        // AI 일시정지 (안전하게)
        var cultistAIs = FindObjectsByType<CultistAI>(FindObjectsSortMode.None);
        foreach (var ai in cultistAIs)
        {
            if (ai != null && ai.gameObject.activeInHierarchy)
            {
                ai.enabled = false;
            }
        }

        var bossAI = FindFirstObjectByType<BossAI>();
        if (bossAI != null)
        {
            bossAI.enabled = false;
        }

        Debug.Log("[CinematicManager] 게임플레이 일시정지");
    }

    void ResumeGameplay()
    {
        // VolumeManager BGM 복구
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.SetBGMVolume(0.7f); // 기본 볼륨으로 복구
        }

        // AI 재개
        var cultistAIs = FindObjectsByType<CultistAI>(FindObjectsSortMode.None);
        foreach (var ai in cultistAIs)
        {
            if (ai != null && ai.gameObject.activeInHierarchy)
            {
                ai.enabled = true;
            }
        }

        var bossAI = FindFirstObjectByType<BossAI>();
        if (bossAI != null)
        {
            bossAI.enabled = true;
        }

        Debug.Log("[CinematicManager] 게임플레이 재개");
    }

    #endregion

    #region Utility Methods

    VideoClip GetVideoClip(CinematicType cinematicType)
    {
        switch (cinematicType)
        {
            case CinematicType.Intro:
                return introVideo;
            case CinematicType.BossIntro:
                return bossIntroVideo;
            case CinematicType.Ending:
                return endingVideo;
            default:
                return null;
        }
    }

    IEnumerator PlayIntroVideoOnStart()
    {
        // 씬 로드 완료 후 약간의 지연
        yield return new WaitForSeconds(0.5f);
        
        // 게임 진행 상태 확인
        if (GameProgressManager.Instance == null || 
            GameProgressManager.Instance.CurrentState == GameProgressManager.GameState.IntroVideo)
        {
            PlayCinematic(CinematicType.Intro);
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // 영상 재생 완료 시 자동 호출
        Debug.Log("[CinematicManager] 영상 재생 완료됨");
    }

    #endregion

    #region Public Properties

    public bool IsPlayingVideo => isPlayingVideo;
    public CinematicType CurrentCinematic => currentCinematic;

    #endregion
} 