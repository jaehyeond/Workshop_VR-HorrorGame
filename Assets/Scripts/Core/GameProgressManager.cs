using UnityEngine;
using System.Collections;

/// <summary>
/// VR Horror Game - 게임 진행 상태 관리자
/// 첫번째 영상 -> Main Scene -> 두번째 영상 -> Boss Battle -> 세번째 영상 순서 관리
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("=== Game Progress ===")]
    [SerializeField] private GameState currentState = GameState.IntroVideo;
    [SerializeField] private bool hasSeenIntro = false;
    [SerializeField] private bool hasSeenBossIntro = false;
    [SerializeField] private bool hasSeenEnding = false;

    [Header("=== Boss Battle Settings ===")]
    [SerializeField] private bool isBossDefeated = false;
    [SerializeField] private bool isDaughterRescued = false;

    [Header("=== Debug ===")]
    [SerializeField] private bool enableDebugLogs = true;

    public enum GameState
    {
        IntroVideo,         // 첫번째 영상 (딸 납치)
        MainExploration,    // 메인 게임 탐험
        BossIntroVideo,     // 두번째 영상 (보스룸 입장)
        BossBattle,         // 보스전
        EndingVideo,        // 세번째 영상 (구출 성공)
        GameComplete        // 게임 완료
    }

    // Events
    public System.Action<GameState, GameState> OnGameStateChanged;
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
            InitializeProgressManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 게임 시작 시 초기 상태 설정
        LoadGameProgress();
        
        // CinematicManager와 연결
        if (CinematicManager.Instance != null)
        {
            CinematicManager.Instance.OnCinematicStarted += OnCinematicStarted;
            CinematicManager.Instance.OnCinematicEnded += OnCinematicEnded;
        }
    }

    void OnDestroy()
    {
        // 이벤트 해제
        if (CinematicManager.Instance != null)
        {
            CinematicManager.Instance.OnCinematicStarted -= OnCinematicStarted;
            CinematicManager.Instance.OnCinematicEnded -= OnCinematicEnded;
        }
    }

    #endregion

    #region Initialization

    void InitializeProgressManager()
    {
        DebugLog("[GameProgressManager] 초기화 완료");
    }

    void LoadGameProgress()
    {
        // PlayerPrefs에서 진행 상황 로드 (선택사항)
        hasSeenIntro = PlayerPrefs.GetInt("HasSeenIntro", 0) == 1;
        hasSeenBossIntro = PlayerPrefs.GetInt("HasSeenBossIntro", 0) == 1;
        hasSeenEnding = PlayerPrefs.GetInt("HasSeenEnding", 0) == 1;
        isBossDefeated = PlayerPrefs.GetInt("IsBossDefeated", 0) == 1;

        // 진행 상황에 따른 초기 상태 설정
        if (!hasSeenIntro)
        {
            SetGameState(GameState.IntroVideo);
        }
        else if (!hasSeenBossIntro)
        {
            SetGameState(GameState.MainExploration);
        }
        else if (!isBossDefeated)
        {
            SetGameState(GameState.BossBattle);
        }
        else if (!hasSeenEnding)
        {
            SetGameState(GameState.EndingVideo);
        }
        else
        {
            SetGameState(GameState.GameComplete);
        }

        DebugLog($"[GameProgressManager] 진행 상황 로드 완료 - 현재 상태: {currentState}");
    }

    void SaveGameProgress()
    {
        PlayerPrefs.SetInt("HasSeenIntro", hasSeenIntro ? 1 : 0);
        PlayerPrefs.SetInt("HasSeenBossIntro", hasSeenBossIntro ? 1 : 0);
        PlayerPrefs.SetInt("HasSeenEnding", hasSeenEnding ? 1 : 0);
        PlayerPrefs.SetInt("IsBossDefeated", isBossDefeated ? 1 : 0);
        PlayerPrefs.Save();

        DebugLog("[GameProgressManager] 진행 상황 저장 완료");
    }

    #endregion

    #region State Management

    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;

        DebugLog($"[GameProgressManager] 상태 변경: {previousState} -> {newState}");

        // 상태 변경 이벤트
        OnGameStateChanged?.Invoke(previousState, newState);

        // 상태별 처리
        HandleStateChange(newState);

        // 진행 상황 저장
        SaveGameProgress();
    }

    void HandleStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.IntroVideo:
                HandleIntroVideoState();
                break;
                
            case GameState.MainExploration:
                HandleMainExplorationState();
                break;
                
            case GameState.BossIntroVideo:
                HandleBossIntroVideoState();
                break;
                
            case GameState.BossBattle:
                HandleBossBattleState();
                break;
                
            case GameState.EndingVideo:
                HandleEndingVideoState();
                break;
                
            case GameState.GameComplete:
                HandleGameCompleteState();
                break;
        }
    }

    void HandleIntroVideoState()
    {
        DebugLog("[GameProgressManager] 인트로 영상 상태");
        
        // 필요한 경우 인트로 영상 자동 재생
        if (CinematicManager.Instance != null && !CinematicManager.Instance.IsPlayingVideo)
        {
            StartCoroutine(PlayIntroVideoDelayed());
        }
    }

    void HandleMainExplorationState()
    {
        DebugLog("[GameProgressManager] 메인 탐험 상태");
        
        // BGM을 탐험 모드로 변경
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlayBGM(VolumeManager.BGMType.Exploration);
        }

        // 보스룸 트리거 활성화
        EnableBossRoomTrigger();
    }

    void HandleBossIntroVideoState()
    {
        DebugLog("[GameProgressManager] 보스 인트로 영상 상태");
        
        // 보스 인트로 영상 재생
        if (CinematicManager.Instance != null)
        {
            CinematicManager.Instance.PlayCinematic(CinematicManager.CinematicType.BossIntro);
        }
    }

    void HandleBossBattleState()
    {
        DebugLog("[GameProgressManager] 보스전 상태");
        
        // 보스전 BGM 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlayBossBattleBGM();
        }

        // 보스 AI 활성화
        EnableBossAI();
        
        // 딸 구출 트리거 활성화
        EnableDaughterRescueTrigger();
    }

    void HandleEndingVideoState()
    {
        DebugLog("[GameProgressManager] 엔딩 영상 상태");
        
        // 엔딩 영상 재생
        if (CinematicManager.Instance != null)
        {
            CinematicManager.Instance.PlayCinematic(CinematicManager.CinematicType.Ending);
        }
    }

    void HandleGameCompleteState()
    {
        DebugLog("[GameProgressManager] 게임 완료 상태");
        
        // 승리 BGM 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlayVictoryBGM();
        }

        // 게임 완료 처리
        StartCoroutine(HandleGameCompletion());
    }

    #endregion

    #region Cinematic Events

    void OnCinematicStarted(CinematicManager.CinematicType cinematicType)
    {
        DebugLog($"[GameProgressManager] 영상 시작: {cinematicType}");
    }

    void OnCinematicEnded(CinematicManager.CinematicType cinematicType)
    {
        DebugLog($"[GameProgressManager] 영상 종료: {cinematicType}");
    }

    public void OnCinematicCompleted(CinematicManager.CinematicType cinematicType)
    {
        switch (cinematicType)
        {
            case CinematicManager.CinematicType.Intro:
                hasSeenIntro = true;
                SetGameState(GameState.MainExploration);
                break;
                
            case CinematicManager.CinematicType.BossIntro:
                hasSeenBossIntro = true;
                SetGameState(GameState.BossBattle);
                break;
                
            case CinematicManager.CinematicType.Ending:
                hasSeenEnding = true;
                SetGameState(GameState.GameComplete);
                break;
        }

        SaveGameProgress();
    }

    #endregion

    #region Boss Battle Management

    public void NotifyBossDefeated()
    {
        if (isBossDefeated) return;

        DebugLog("[GameProgressManager] 보스 처치됨!");
        
        isBossDefeated = true;
        OnBossDefeated?.Invoke();

        // 승리 BGM 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlayVictoryBGM();
        }

        SaveGameProgress();
    }

    public void NotifyDaughterRescued()
    {
        if (isDaughterRescued) return;

        DebugLog("[GameProgressManager] 딸 구출됨!");
        
        isDaughterRescued = true;
        OnDaughterRescued?.Invoke();

        // 엔딩 영상으로 전환
        SetGameState(GameState.EndingVideo);
    }

    #endregion

    #region Helper Methods

    IEnumerator PlayIntroVideoDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        if (CinematicManager.Instance != null)
        {
            CinematicManager.Instance.PlayCinematic(CinematicManager.CinematicType.Intro);
        }
    }

    void EnableBossRoomTrigger()
    {
        // 보스룸 트리거 찾기 및 활성화
        CinematicTrigger bossRoomTrigger = FindBossRoomTrigger();
        if (bossRoomTrigger != null)
        {
            bossRoomTrigger.gameObject.SetActive(true);
            DebugLog("[GameProgressManager] 보스룸 트리거 활성화");
        }
    }

    void EnableBossAI()
    {
        // 네크로맨서 보스 찾기 및 활성화
        NecromancerBoss necromancerBoss = FindFirstObjectByType<NecromancerBoss>();
        if (necromancerBoss != null)
        {
            necromancerBoss.gameObject.SetActive(true);
            DebugLog("[GameProgressManager] 네크로맨서 보스 활성화");
        }
        
        // 기존 BossAI도 호환성을 위해 체크
        BossAI bossAI = FindFirstObjectByType<BossAI>();
        if (bossAI != null)
        {
            bossAI.gameObject.SetActive(true);
            DebugLog("[GameProgressManager] 보스 AI 활성화");
        }
    }

    void EnableDaughterRescueTrigger()
    {
        // 딸 구출 트리거 찾기 및 활성화
        CinematicTrigger daughterTrigger = FindDaughterRescueTrigger();
        if (daughterTrigger != null)
        {
            daughterTrigger.gameObject.SetActive(true);
            DebugLog("[GameProgressManager] 딸 구출 트리거 활성화");
        }
    }

    CinematicTrigger FindBossRoomTrigger()
    {
        CinematicTrigger[] triggers = FindObjectsByType<CinematicTrigger>(FindObjectsSortMode.None);
        foreach (var trigger in triggers)
        {
            if (trigger.cinematicType == CinematicManager.CinematicType.BossIntro)
            {
                return trigger;
            }
        }
        return null;
    }

    CinematicTrigger FindDaughterRescueTrigger()
    {
        CinematicTrigger[] triggers = FindObjectsByType<CinematicTrigger>(FindObjectsSortMode.None);
        foreach (var trigger in triggers)
        {
            if (trigger.cinematicType == CinematicManager.CinematicType.Ending)
            {
                return trigger;
            }
        }
        return null;
    }

    IEnumerator HandleGameCompletion()
    {
        yield return new WaitForSeconds(3f);
        
        DebugLog("[GameProgressManager] 게임 완료!");
        
        // 게임 완료 UI 표시하거나 메인 메뉴로 돌아가기
        // 여기에 게임 완료 로직 추가
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
    public bool HasSeenIntro => hasSeenIntro;
    public bool HasSeenBossIntro => hasSeenBossIntro;
    public bool HasSeenEnding => hasSeenEnding;
    public bool IsBossDefeated => isBossDefeated;
    public bool IsDaughterRescued => isDaughterRescued;

    #endregion

    #region Debug Methods

    [System.Serializable]
    public class DebugCommands
    {
        [Header("=== Debug Controls ===")]
        public KeyCode resetProgressKey = KeyCode.R;
        public KeyCode skipToMainKey = KeyCode.Alpha1;
        public KeyCode skipToBossIntroKey = KeyCode.Alpha2;
        public KeyCode skipToBossBattleKey = KeyCode.Alpha3;
        public KeyCode skipToEndingKey = KeyCode.Alpha4;
    }

    [SerializeField] private DebugCommands debugCommands = new DebugCommands();

    void Update()
    {
        if (!enableDebugLogs) return;

        // 디버그 키 입력 처리
        if (Input.GetKeyDown(debugCommands.resetProgressKey))
        {
            ResetGameProgress();
        }
        else if (Input.GetKeyDown(debugCommands.skipToMainKey))
        {
            SetGameState(GameState.MainExploration);
        }
        else if (Input.GetKeyDown(debugCommands.skipToBossIntroKey))
        {
            SetGameState(GameState.BossIntroVideo);
        }
        else if (Input.GetKeyDown(debugCommands.skipToBossBattleKey))
        {
            SetGameState(GameState.BossBattle);
        }
        else if (Input.GetKeyDown(debugCommands.skipToEndingKey))
        {
            SetGameState(GameState.EndingVideo);
        }
    }

    public void ResetGameProgress()
    {
        hasSeenIntro = false;
        hasSeenBossIntro = false;
        hasSeenEnding = false;
        isBossDefeated = false;
        isDaughterRescued = false;
        
        PlayerPrefs.DeleteAll();
        
        SetGameState(GameState.IntroVideo);
        
        DebugLog("[GameProgressManager] 게임 진행 상황 리셋!");
    }

    #endregion
} 