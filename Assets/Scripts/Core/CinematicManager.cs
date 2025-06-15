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
    
    [Header("=== Door Control ===")]
    [SerializeField] private GameObject bossRoomDoor; // 보스룸 문 (DoorD_V2)
    [SerializeField] private bool autoFindDoor = true; // 자동으로 문 찾기

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
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log("[CinematicManager] 현재 씬: " + currentSceneName);
        
        if (currentSceneName == "Revert" || 
            currentSceneName == mainSceneName ||
            currentSceneName == "Beta(Map LIght)" ||
            currentSceneName.Contains("Beta"))
        {
            StartCoroutine(PlayIntroVideoOnStart());
        }
        else
        {
            Debug.LogWarning("[CinematicManager] 인트로 영상을 재생하지 않는 씬: " + currentSceneName);
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
        SetupDoorControl();

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

        // VideoScreen의 RawImage에 RenderTexture 연결
        if (videoScreen != null)
        {
            UnityEngine.UI.RawImage rawImage = videoScreen.GetComponent<UnityEngine.UI.RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = renderTexture;
                Debug.Log("[CinematicManager] VideoScreen에 RenderTexture 연결 완료");
            }
            else
            {
                Debug.LogWarning("[CinematicManager] VideoScreen에 RawImage 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning("[CinematicManager] VideoScreen이 설정되지 않았습니다!");
        }

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
                Debug.Log("[CinematicManager] VideoCanvas VR 위치 설정 완료: " + canvasObj.transform.position);
            }
            else
            {
                // VR 카메라를 찾지 못한 경우 기본 위치
                canvasObj.transform.position = new Vector3(0, 2, 4);
                canvasObj.transform.rotation = Quaternion.identity;
                Debug.LogWarning("[CinematicManager] VR 카메라를 찾지 못해 기본 위치로 설정");
            }
        }
        else
        {
            // 기존 Canvas가 있는 경우 VR 위치 재조정
            Transform vrCamera = FindVRCamera();
            if (vrCamera != null)
            {
                videoCanvas.transform.position = vrCamera.position + vrCamera.forward * 4f;
                videoCanvas.transform.LookAt(vrCamera.position);
                videoCanvas.transform.rotation *= Quaternion.Euler(0, 180, 0);
                Debug.Log("[CinematicManager] 기존 VideoCanvas VR 위치 재조정 완료");
            }
        }

        // VideoScreen (RawImage) 생성
        if (videoScreen == null)
        {
            GameObject screenObj = new GameObject("VideoScreen");
            screenObj.transform.SetParent(videoCanvas.transform);
            
            UnityEngine.UI.RawImage rawImage = screenObj.AddComponent<UnityEngine.UI.RawImage>();
            
            // RectTransform 설정 (전체 화면)
            RectTransform rectTransform = rawImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            
            // 검은색 배경
            rawImage.color = Color.black;
            
            videoScreen = screenObj;
            Debug.Log("[CinematicManager] VideoScreen 생성 완료");
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
    
    void SetupDoorControl()
    {
        // 자동으로 보스룸 문 찾기
        if (autoFindDoor && bossRoomDoor == null)
        {
            // DoorD_V2 오브젝트 찾기 (보스 위치 근처)
            GameObject[] doors = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (GameObject door in doors)
            {
                if (door.name.Contains("DoorD_V2") && !door.name.Contains("Frame") && 
                    !door.name.Contains("Left") && !door.name.Contains("Right") && 
                    !door.name.Contains("Window"))
                {
                    // 보스 위치 근처의 문인지 확인
                    Vector3 bossPosition = new Vector3(-22.881f, 3.67f, -28.125f);
                    float distance = Vector3.Distance(door.transform.position, bossPosition);
                    
                    if (distance < 15f) // 보스 위치에서 15미터 이내
                    {
                        bossRoomDoor = door;
                        Debug.Log("[CinematicManager] 보스룸 문 자동 감지: " + door.name + " (거리: " + distance.ToString("F1") + "m)");
                        break;
                    }
                }
            }
            
            // 찾지 못했으면 이름으로 직접 찾기
            if (bossRoomDoor == null)
            {
                bossRoomDoor = GameObject.Find("DoorD_V2");
                if (bossRoomDoor != null)
                {
                    Debug.Log("[CinematicManager] 보스룸 문 이름으로 찾음: " + bossRoomDoor.name);
                }
            }
        }
        
        // 문 상태 확인
        if (bossRoomDoor != null)
        {
            Debug.Log("[CinematicManager] 보스룸 문 설정 완료: " + bossRoomDoor.name + " (활성화: " + bossRoomDoor.activeInHierarchy + ")");
        }
        else
        {
            Debug.LogWarning("[CinematicManager] 보스룸 문을 찾을 수 없습니다!");
        }
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
            Debug.LogError("[CinematicManager] " + cinematicType + " 영상 클립이 없습니다!");
            return;
        }

        StartCoroutine(PlayVideoCoroutine(cinematicType, clipToPlay));
    }

    IEnumerator PlayVideoCoroutine(CinematicType cinematicType, VideoClip clip)
    {
        Debug.Log("[CinematicManager] " + cinematicType + " 영상 재생 시작");
        
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
        
        // VideoPlayer 준비 대기
        yield return new WaitForSeconds(0.1f);
        
        // 영상 재생 상태 확인
        Debug.Log("[CinematicManager] VideoPlayer 상태 - isPlaying: " + videoPlayer.isPlaying + ", isPrepared: " + videoPlayer.isPrepared);
        
        // 이벤트 호출
        OnCinematicStarted?.Invoke(cinematicType);

        // 영상 완료 대기 (또는 스킵) - 더 안전한 조건
        float maxWaitTime = (float)clip.length + 2f; // 영상 길이 + 여유시간
        float elapsedTime = 0f;
        
        while (isPlayingVideo && elapsedTime < maxWaitTime)
        {
            if (videoPlayer.isPlaying)
            {
                elapsedTime += Time.deltaTime;
            }
            else if (elapsedTime > 0.5f) // 0.5초 이상 재생된 후 멈추면 종료
            {
                break;
            }
            yield return null;
        }
        
        Debug.Log("[CinematicManager] 영상 재생 종료 - 경과시간: " + elapsedTime.ToString("F1") + "초");

        // 영상 종료 처리
        yield return StartCoroutine(EndVideoPlayback());
    }

    IEnumerator EndVideoPlayback()
    {
        Debug.Log("[CinematicManager] " + currentCinematic + " 영상 재생 완료");

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

        // 특별 처리: BossIntro 영상 종료 시 문 열기
        if (completedCinematic == CinematicType.BossIntro)
        {
            OpenBossRoomDoor();
        }

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

        Debug.Log("[CinematicManager] " + currentCinematic + " 영상 스킵됨");
        
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

    public VideoClip GetVideoClip(CinematicType cinematicType)
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
        
        Debug.Log("[CinematicManager] PlayIntroVideoOnStart 시작");
        
        // GameProgressManager 상태 확인
        if (GameProgressManager.Instance != null)
        {
            Debug.Log("[CinematicManager] GameProgressManager 상태: " + GameProgressManager.Instance.CurrentState);
            Debug.Log("[CinematicManager] hasSeenIntro: " + GameProgressManager.Instance.HasSeenIntro);
        }
        
        // 게임 진행 상태 확인 - 더 관대한 조건
        if (GameProgressManager.Instance == null || 
            GameProgressManager.Instance.CurrentState == GameProgressManager.GameState.IntroVideo ||
            !GameProgressManager.Instance.HasSeenIntro)
        {
            Debug.Log("[CinematicManager] 인트로 영상 재생 조건 만족 - 영상 시작");
            PlayCinematic(CinematicType.Intro);
        }
        else
        {
            Debug.LogWarning("[CinematicManager] 인트로 영상 재생 조건 불만족 - 상태: " + GameProgressManager.Instance?.CurrentState + ", hasSeenIntro: " + GameProgressManager.Instance?.HasSeenIntro);
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // 영상 재생 완료 시 자동 호출
        Debug.Log("[CinematicManager] 영상 재생 완료됨");
    }

    #endregion

    #region Door Control

    /// <summary>
    /// 보스룸 문 열기 (BossIntro 영상 종료 후 호출)
    /// </summary>
    void OpenBossRoomDoor()
    {
        if (bossRoomDoor == null)
        {
            Debug.LogWarning("[CinematicManager] 보스룸 문이 설정되지 않았습니다!");
            return;
        }

        if (bossRoomDoor.activeInHierarchy)
        {
            bossRoomDoor.SetActive(false);
            Debug.Log("[CinematicManager] 보스룸 문 열림: " + bossRoomDoor.name + " 비활성화");
            
            // VolumeManager로 문 열림 사운드 재생
            if (VolumeManager.Instance != null)
            {
                VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.DoorOpen, bossRoomDoor.transform.position);
            }
        }
        else
        {
            Debug.Log("[CinematicManager] 보스룸 문이 이미 열려있습니다.");
        }
    }

    /// <summary>
    /// 보스룸 문 닫기 (필요시 사용)
    /// </summary>
    public void CloseBossRoomDoor()
    {
        if (bossRoomDoor == null)
        {
            Debug.LogWarning("[CinematicManager] 보스룸 문이 설정되지 않았습니다!");
            return;
        }

        if (!bossRoomDoor.activeInHierarchy)
        {
            bossRoomDoor.SetActive(true);
            Debug.Log("[CinematicManager] 보스룸 문 닫힘: " + bossRoomDoor.name + " 활성화");
            
            // VolumeManager로 문 닫힘 사운드 재생
            if (VolumeManager.Instance != null)
            {
                VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.DoorClose, bossRoomDoor.transform.position);
            }
        }
        else
        {
            Debug.Log("[CinematicManager] 보스룸 문이 이미 닫혀있습니다.");
        }
    }

    /// <summary>
    /// 수동으로 보스룸 문 설정
    /// </summary>
    public void SetBossRoomDoor(GameObject door)
    {
        bossRoomDoor = door;
        Debug.Log("[CinematicManager] 보스룸 문 수동 설정: " + (door?.name ?? "null"));
    }

    #endregion

    #region Public Properties

    public bool IsPlayingVideo => isPlayingVideo;
    public CinematicType CurrentCinematic => currentCinematic;
    public GameObject BossRoomDoor => bossRoomDoor;

    #endregion
} 