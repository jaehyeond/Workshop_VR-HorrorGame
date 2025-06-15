using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// VR Horror Game - 통합 게임 플로우 관리자
/// 모든 Scene 전환과 게임 상태를 단일 지점에서 관리
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("=== Game Flow Settings ===")]
    [SerializeField] private GameState currentState = GameState.IntroVideo;
    [SerializeField] private bool enableDebugLogs = true;

    [Header("=== Scene Names ===")]
    [SerializeField] private string introVideoScene = "IntroVideo";
    [SerializeField] private string bossIntroVideoScene = "BossIntroVideo";
    [SerializeField] private string endingVideoScene = "EndingVideo";
    [SerializeField] private string mainGameScene = "Beta(Map Light)";

    public enum GameState
    {
        IntroVideo,         // 인트로 영상
        MainExploration,    // 메인 게임 탐험
        BossIntroVideo,     // 보스 인트로 영상
        BossBattle,         // 보스전
        EndingVideo,        // 엔딩 영상
        GameComplete        // 게임 완료
    }

    // Events
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnBossDefeated;
    public System.Action OnDaughterRescued;

    #region Unity Lifecycle

    void Awake()
    {
        // Singleton 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameFlow();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadGameProgress();
        HandleCurrentScene();
    }

    #endregion

    #region Initialization

    void InitializeGameFlow()
    {
        DebugLog("[GameFlowManager] 게임 플로우 매니저 초기화 완료");
    }

    void LoadGameProgress()
    {
        // PlayerPrefs에서 진행 상황 로드
        bool hasSeenIntro = PlayerPrefs.GetInt("HasSeenIntro", 0) == 1;
        bool hasSeenBossIntro = PlayerPrefs.GetInt("HasSeenBossIntro", 0) == 1;
        bool hasSeenEnding = PlayerPrefs.GetInt("HasSeenEnding", 0) == 1;
        bool isBossDefeated = PlayerPrefs.GetInt("IsBossDefeated", 0) == 1;

        // 진행 상황에 따른 상태 설정
        if (!hasSeenIntro)
        {
            currentState = GameState.IntroVideo;
        }
        else if (!hasSeenBossIntro)
        {
            currentState = GameState.MainExploration;
        }
        else if (!isBossDefeated)
        {
            // BossIntroVideo를 봤지만 Boss를 아직 처치하지 않음
            currentState = GameState.BossBattle;
        }
        else if (!hasSeenEnding)
        {
            // Boss를 처치했지만 엔딩을 보지 않음
            currentState = GameState.BossBattle; // 딸 구출 가능 상태
        }
        else
        {
            currentState = GameState.GameComplete;
        }

        DebugLog($"[GameFlowManager] 게임 진행 상황 로드 완료 - 현재 상태: {currentState}");
        DebugLog($"[GameFlowManager] 진행 상황: Intro({hasSeenIntro}), BossIntro({hasSeenBossIntro}), BossDefeated({isBossDefeated}), Ending({hasSeenEnding})");
    }

    void HandleCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        DebugLog($"[GameFlowManager] 현재 Scene: {sceneName}");

        // Scene별 처리
        if (IsVideoScene(sceneName))
        {
            HandleVideoScene(sceneName);
        }
        else if (IsMainGameScene(sceneName))
        {
            HandleMainGameScene();
        }
    }

    #endregion

    #region Scene Management

    bool IsVideoScene(string sceneName)
    {
        return sceneName == introVideoScene || 
               sceneName == bossIntroVideoScene || 
               sceneName == endingVideoScene;
    }

    bool IsMainGameScene(string sceneName)
    {
        return sceneName.Contains("Beta") || 
               sceneName.Contains("Map") ||
               sceneName.Contains("Main") ||
               sceneName == mainGameScene;
    }

    void HandleVideoScene(string sceneName)
    {
        DebugLog($"[GameFlowManager] Video Scene 처리: {sceneName}");
        
        // Video Scene에서는 VideoSceneManager가 영상 재생 후 자동 전환
        // 여기서는 상태만 업데이트
        UpdateStateForVideoScene(sceneName);
    }

    void HandleMainGameScene()
    {
        DebugLog("[GameFlowManager] MainGame Scene 처리");
        
        // AI 활성화 및 BGM 설정
        StartCoroutine(SetupMainGameScene());
        
        // 플레이어 위치 복원
        RestorePlayerPosition();
        
        // Boss문 상태 설정
        SetupBossDoorState();
        
        // SceneTransitionTrigger 상태 설정
        SetupSceneTransitionTriggers();
    }

    IEnumerator SetupMainGameScene()
    {
        yield return new WaitForSeconds(0.5f);
        
        // AI 활성화
        EnableAllAI();
        
        // BGM 설정
        SetupBGM();
        
        DebugLog("[GameFlowManager] MainGame Scene 설정 완료");
    }

    #endregion

    #region Game State Management

    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;

        DebugLog($"[GameFlowManager] 상태 변경: {previousState} → {newState}");

        OnGameStateChanged?.Invoke(newState);
        SaveGameProgress();
    }

    void UpdateStateForVideoScene(string sceneName)
    {
        switch (sceneName)
        {
            case "IntroVideo":
                SetGameState(GameState.IntroVideo);
                break;
            case "BossIntroVideo":
                SetGameState(GameState.BossIntroVideo);
                break;
            case "EndingVideo":
                SetGameState(GameState.EndingVideo);
                break;
        }
    }

    void SaveGameProgress()
    {
        // 상태에 따른 PlayerPrefs 저장
        switch (currentState)
        {
            case GameState.MainExploration:
                PlayerPrefs.SetInt("HasSeenIntro", 1);
                break;
            case GameState.BossIntroVideo:
                PlayerPrefs.SetInt("HasSeenIntro", 1);
                PlayerPrefs.SetInt("HasSeenBossIntro", 1);
                break;
            case GameState.BossBattle:
                PlayerPrefs.SetInt("HasSeenIntro", 1);
                PlayerPrefs.SetInt("HasSeenBossIntro", 1);
                break;
            case GameState.EndingVideo:
                PlayerPrefs.SetInt("HasSeenEnding", 1);
                break;
            case GameState.GameComplete:
                PlayerPrefs.SetInt("HasSeenIntro", 1);
                PlayerPrefs.SetInt("HasSeenBossIntro", 1);
                PlayerPrefs.SetInt("HasSeenEnding", 1);
                break;
        }
        
        PlayerPrefs.Save();
    }

    #endregion

    #region Game Events

    public void NotifyBossDefeated()
    {
        DebugLog("[GameFlowManager] 보스 처치됨!");
        
        PlayerPrefs.SetInt("IsBossDefeated", 1);
        PlayerPrefs.Save();
        
        OnBossDefeated?.Invoke();
        
        // 승리 BGM
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlayVictoryBGM();
        }
    }

    public void NotifyDaughterRescued()
    {
        DebugLog("[GameFlowManager] 딸 구출됨!");
        OnDaughterRescued?.Invoke();
    }

    public void TriggerBossIntroVideo()
    {
        DebugLog("[GameFlowManager] 보스 인트로 영상 트리거");
        
        // 플레이어 위치 저장 (Boss문 앞)
        SavePlayerPosition();
        
        // BossIntroVideo 상태로 변경
        SetGameState(GameState.BossIntroVideo);
        
        SceneManager.LoadScene(bossIntroVideoScene);
    }

    public void TriggerEndingVideo()
    {
        DebugLog("[GameFlowManager] 엔딩 영상 트리거");
        SceneManager.LoadScene(endingVideoScene);
    }

    #endregion

    #region Helper Methods

    void EnableAllAI()
    {
        // CultistAI 활성화
        var cultistAIs = FindObjectsByType<CultistAI>(FindObjectsSortMode.None);
        foreach (var ai in cultistAIs)
        {
            if (ai != null && ai.gameObject.activeInHierarchy)
            {
                ai.enabled = true;
            }
        }

        // Necromancer Boss 활성화
        var necromancerBoss = FindFirstObjectByType<NecromancerBoss>();
        if (necromancerBoss != null)
        {
            necromancerBoss.gameObject.SetActive(true);
            necromancerBoss.enabled = true;
        }

        DebugLog("[GameFlowManager] 모든 AI 활성화 완료");
    }

    void SetupBGM()
    {
        if (VolumeManager.Instance == null) return;

        switch (currentState)
        {
            case GameState.MainExploration:
                VolumeManager.Instance.PlayBGM(VolumeManager.BGMType.Exploration);
                break;
            case GameState.BossBattle:
                VolumeManager.Instance.PlayBossBattleBGM();
                break;
        }

        VolumeManager.Instance.SetBGMVolume(0.7f);
    }

    void SavePlayerPosition()
    {
        // VR 플레이어 위치 찾기
        GameObject player = FindVRPlayer();
        if (player != null)
        {
            Vector3 position = player.transform.position;
            Vector3 rotation = player.transform.eulerAngles;
            
            PlayerPrefs.SetFloat("PlayerPosX", position.x);
            PlayerPrefs.SetFloat("PlayerPosY", position.y);
            PlayerPrefs.SetFloat("PlayerPosZ", position.z);
            PlayerPrefs.SetFloat("PlayerRotY", rotation.y);
            PlayerPrefs.Save();
            
            DebugLog($"[GameFlowManager] 플레이어 위치 저장: {position}");
        }
    }

    void RestorePlayerPosition()
    {
        // BossIntroVideo를 본 후에만 위치 복원
        if (!HasSeenBossIntro) return;
        
        if (PlayerPrefs.HasKey("PlayerPosX"))
        {
            Vector3 savedPosition = new Vector3(
                PlayerPrefs.GetFloat("PlayerPosX"),
                PlayerPrefs.GetFloat("PlayerPosY"),
                PlayerPrefs.GetFloat("PlayerPosZ")
            );
            
            float savedRotationY = PlayerPrefs.GetFloat("PlayerRotY");
            
            StartCoroutine(RestorePlayerPositionCoroutine(savedPosition, savedRotationY));
        }
    }

    IEnumerator RestorePlayerPositionCoroutine(Vector3 position, float rotationY)
    {
        yield return new WaitForSeconds(0.1f);
        
        GameObject player = FindVRPlayer();
        if (player != null)
        {
            player.transform.position = position;
            player.transform.rotation = Quaternion.Euler(0, rotationY, 0);
            
            DebugLog($"[GameFlowManager] 플레이어 위치 복원: {position}");
            
            // 저장된 위치 정보 삭제
            PlayerPrefs.DeleteKey("PlayerPosX");
            PlayerPrefs.DeleteKey("PlayerPosY");
            PlayerPrefs.DeleteKey("PlayerPosZ");
            PlayerPrefs.DeleteKey("PlayerRotY");
            PlayerPrefs.Save();
        }
    }

    GameObject FindVRPlayer()
    {
        // OVRCameraRig 찾기
        GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
        if (ovrCameraRig != null) return ovrCameraRig;
        
        // Player 태그로 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player;
        
        // VRPlayer 태그로 찾기
        GameObject vrPlayer = GameObject.FindGameObjectWithTag("VRPlayer");
        if (vrPlayer != null) return vrPlayer;
        
        return null;
    }

    void SetupBossDoorState()
    {
        // BossIntroVideo를 본 후에는 Boss문 열기
        if (HasSeenBossIntro)
        {
            StartCoroutine(OpenBossDoor());
        }
    }

    IEnumerator OpenBossDoor()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Boss문 찾기 및 열기
        GameObject bossDoor = GameObject.Find("BossRoomDoor");
        if (bossDoor == null)
        {
            // 다른 이름으로 찾기
            bossDoor = GameObject.Find("Door");
        }
        
        if (bossDoor != null)
        {
            // 문 열기 (Animator가 있는 경우)
            Animator doorAnimator = bossDoor.GetComponent<Animator>();
            if (doorAnimator != null)
            {
                doorAnimator.SetBool("IsOpen", true);
                doorAnimator.SetTrigger("Open");
            }
            
            // 문 비활성화 (Collider가 있는 경우)
            Collider doorCollider = bossDoor.GetComponent<Collider>();
            if (doorCollider != null)
            {
                doorCollider.enabled = false;
            }
            
            DebugLog("[GameFlowManager] Boss문 열림");
        }
        else
        {
            DebugLog("[GameFlowManager] Boss문을 찾을 수 없음");
        }
    }

    void SetupSceneTransitionTriggers()
    {
        // BossIntroVideo 트리거 비활성화
        if (HasSeenBossIntro)
        {
            GameObject bossRoomTrigger = GameObject.Find("BossRoomTransition");
            if (bossRoomTrigger == null)
            {
                bossRoomTrigger = GameObject.Find("BossRoom_SceneTransitionTrigger");
            }
            
            if (bossRoomTrigger != null)
            {
                bossRoomTrigger.SetActive(false);
                DebugLog("[GameFlowManager] BossRoom 트리거 비활성화");
            }
        }
        
        // EndingVideo 트리거는 Boss가 처치된 후에만 활성화
        GameObject daughterRescueTrigger = GameObject.Find("DaughterRescueTransition");
        if (daughterRescueTrigger == null)
        {
            daughterRescueTrigger = GameObject.Find("DaughterRescue_SceneTransitionTrigger");
        }
        
        if (daughterRescueTrigger != null)
        {
            daughterRescueTrigger.SetActive(IsBossDefeated);
            DebugLog($"[GameFlowManager] DaughterRescue 트리거 상태: {IsBossDefeated}");
        }
    }

    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }

    #endregion

    #region Public Properties

    public GameState CurrentState => currentState;
    public bool HasSeenIntro => PlayerPrefs.GetInt("HasSeenIntro", 0) == 1;
    public bool HasSeenBossIntro => PlayerPrefs.GetInt("HasSeenBossIntro", 0) == 1;
    public bool HasSeenEnding => PlayerPrefs.GetInt("HasSeenEnding", 0) == 1;
    public bool IsBossDefeated => PlayerPrefs.GetInt("IsBossDefeated", 0) == 1;

    #endregion

    #region Debug Methods

    [System.Serializable]
    public class DebugCommands
    {
        public KeyCode resetProgressKey = KeyCode.R;
        public KeyCode skipToMainKey = KeyCode.Alpha1;
        public KeyCode skipToBossIntroKey = KeyCode.Alpha2;
        public KeyCode skipToEndingKey = KeyCode.Alpha3;
    }

    [SerializeField] private DebugCommands debugCommands = new DebugCommands();

    void Update()
    {
        if (!enableDebugLogs) return;

        if (Input.GetKeyDown(debugCommands.resetProgressKey))
        {
            ResetGameProgress();
        }
        else if (Input.GetKeyDown(debugCommands.skipToMainKey))
        {
            SceneManager.LoadScene(mainGameScene);
        }
        else if (Input.GetKeyDown(debugCommands.skipToBossIntroKey))
        {
            SceneManager.LoadScene(bossIntroVideoScene);
        }
        else if (Input.GetKeyDown(debugCommands.skipToEndingKey))
        {
            SceneManager.LoadScene(endingVideoScene);
        }
    }

    public void ResetGameProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        currentState = GameState.IntroVideo;
        DebugLog("[GameFlowManager] 게임 진행 상황 리셋!");
    }

    #endregion
} 