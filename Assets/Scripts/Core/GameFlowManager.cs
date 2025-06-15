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
            currentState = GameState.BossBattle;
        }
        else if (!hasSeenEnding)
        {
            currentState = GameState.EndingVideo;
        }
        else
        {
            currentState = GameState.GameComplete;
        }

        DebugLog($"[GameFlowManager] 게임 진행 상황 로드 완료 - 현재 상태: {currentState}");
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
            case GameState.BossBattle:
                PlayerPrefs.SetInt("HasSeenBossIntro", 1);
                break;
            case GameState.GameComplete:
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