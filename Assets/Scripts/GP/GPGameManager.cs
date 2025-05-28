using UnityEngine;
using System.Collections;

/// <summary>
/// GP 게임의 핵심 게임 매니저
/// </summary>
public class GPGameManager : MonoBehaviour
{
    public static GPGameManager Instance { get; private set; }
    
    [Header("게임 설정")]
    [SerializeField] private float gameTimeLimitSeconds = 600f; // 10분
    [SerializeField] private int maxBatteriesNeeded = 3;
    
    [Header("컴포넌트 참조")]
    [SerializeField] private GPUIManager uiManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform escapeDoor;
    
    [Header("오디오")]
    [SerializeField] private AudioSource ambienceSoundSource;
    [SerializeField] private AudioSource musicSoundSource;
    [SerializeField] private AudioClip normalAmbience;
    [SerializeField] private AudioClip chaseMusic;
    [SerializeField] private AudioClip gameOverMusic;
    [SerializeField] private AudioClip victoryMusic;
    
    // 게임 상태
    public enum GameState { Introduction, Playing, GameOver, Victory }
    private GameState currentGameState = GameState.Introduction;
    private float gameTimer = 0f;
    private int batteriesCollected = 0;
    private bool isPlayerDetected = false;
    
    // 내부 참조
    private VRLocomotion playerLocomotion;
    
    // 컴포넌트 공개 액세스
    public GPUIManager UIManager => uiManager;
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 기본 게임 설정 초기화
        InitializeGame();
    }
    
    private void Update()
    {
        // 게임 중에만 타이머 업데이트
        if (currentGameState == GameState.Playing)
        {
            UpdateGameTimer();
        }
    }
    
    /// <summary>
    /// 게임 초기화
    /// </summary>
    private void InitializeGame()
    {
        // 플레이어 로코모션 컴포넌트 참조 획득
        if (playerTransform != null)
        {
            playerLocomotion = playerTransform.GetComponent<VRLocomotion>();
        }
        
        // UI 초기화
        if (uiManager != null)
        {
            uiManager.UpdateBatteryCount(batteriesCollected, maxBatteriesNeeded);
            uiManager.UpdateTimer(gameTimeLimitSeconds);
        }
        
        // 초기 사운드 설정
        if (ambienceSoundSource != null && normalAmbience != null)
        {
            ambienceSoundSource.clip = normalAmbience;
            ambienceSoundSource.loop = true;
            ambienceSoundSource.Play();
        }
        
        // 게임 상태 설정
        SetGameState(GameState.Introduction);
    }
    
    /// <summary>
    /// 게임 타이머 업데이트
    /// </summary>
    private void UpdateGameTimer()
    {
        if (gameTimer < gameTimeLimitSeconds)
        {
            gameTimer += Time.deltaTime;
            
            // UI 타이머 업데이트
            if (uiManager != null)
            {
                uiManager.UpdateTimer(gameTimeLimitSeconds - gameTimer);
            }
        }
        else
        {
            // 시간 초과 게임 오버
            GameOver("시간이 초과되었습니다!");
        }
    }
    
    /// <summary>
    /// 배터리 수집 처리
    /// </summary>
    public void CollectBattery()
    {
        batteriesCollected++;
        
        // UI 업데이트
        if (uiManager != null)
        {
            uiManager.UpdateBatteryCount(batteriesCollected, maxBatteriesNeeded);
            uiManager.ShowNotification($"배터리 {batteriesCollected}/{maxBatteriesNeeded} 수집!");
        }
        
        // 모든 배터리를 모았는지 확인
        if (batteriesCollected >= maxBatteriesNeeded)
        {
            ActivateEscape();
        }
    }
    
    /// <summary>
    /// 탈출 활성화
    /// </summary>
    private void ActivateEscape()
    {
        if (uiManager != null)
        {
            uiManager.ShowNotification("모든 배터리가 수집되었습니다! 탈출구를 찾으세요.");
            uiManager.ShowEscapeIndicator(true);
        }
        
        // 탈출구 활성화 (필요시 추가 로직)
        if (escapeDoor != null)
        {
            // 탈출구 활성화 로직
        }
    }
    
    /// <summary>
    /// 게임 상태 설정
    /// </summary>
    public void SetGameState(GameState newState)
    {
        currentGameState = newState;
        
        switch (newState)
        {
            case GameState.Introduction:
                // 인트로 화면 표시
                if (uiManager != null)
                {
                    uiManager.ShowIntroduction(true);
                }
                break;
                
            case GameState.Playing:
                // 게임 시작
                gameTimer = 0f;
                batteriesCollected = 0;
                isPlayerDetected = false;
                
                // UI 업데이트
                if (uiManager != null)
                {
                    uiManager.ShowIntroduction(false);
                    uiManager.UpdateBatteryCount(batteriesCollected, maxBatteriesNeeded);
                    uiManager.UpdateTimer(gameTimeLimitSeconds);
                }
                
                // 배경음 설정
                if (ambienceSoundSource != null && normalAmbience != null)
                {
                    ambienceSoundSource.clip = normalAmbience;
                    ambienceSoundSource.Play();
                }
                break;
                
            case GameState.GameOver:
                // 게임 오버 처리
                if (uiManager != null)
                {
                    uiManager.ShowGameOver(true);
                }
                
                // 게임 오버 음악 재생
                if (musicSoundSource != null && gameOverMusic != null)
                {
                    musicSoundSource.clip = gameOverMusic;
                    musicSoundSource.Play();
                }
                break;
                
            case GameState.Victory:
                // 승리 처리
                if (uiManager != null)
                {
                    uiManager.ShowVictory(true);
                }
                
                // 승리 음악 재생
                if (musicSoundSource != null && victoryMusic != null)
                {
                    musicSoundSource.clip = victoryMusic;
                    musicSoundSource.Play();
                }
                break;
        }
    }
    
    /// <summary>
    /// 플레이어 탐지 처리
    /// </summary>
    public void PlayerDetected()
    {
        isPlayerDetected = true;
        
        // 플레이어가 은신 중이 아닌 경우만 추격 음악 재생
        if (playerLocomotion == null || !playerLocomotion.IsHiding())
        {
            // 추격 음악 재생
            if (musicSoundSource != null && chaseMusic != null && musicSoundSource.clip != chaseMusic)
            {
                musicSoundSource.clip = chaseMusic;
                musicSoundSource.Play();
            }
        }
    }
    
    /// <summary>
    /// 플레이어 탐지 해제 처리
    /// </summary>
    public void PlayerLost()
    {
        isPlayerDetected = false;
        
        // 일반 배경음으로 복귀 (페이드 효과로 전환)
        StartCoroutine(FadeMusicToAmbience());
    }
    
    /// <summary>
    /// 음악에서 일반 배경음으로 페이드 전환
    /// </summary>
    private IEnumerator FadeMusicToAmbience()
    {
        if (musicSoundSource != null && ambienceSoundSource != null)
        {
            float fadeDuration = 3.0f;
            float startVolume = musicSoundSource.volume;
            
            // 음악 페이드 아웃
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                musicSoundSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
                yield return null;
            }
            
            musicSoundSource.Stop();
            musicSoundSource.volume = startVolume;
            
            // 배경음 재생 확인
            if (!ambienceSoundSource.isPlaying)
            {
                ambienceSoundSource.Play();
            }
        }
    }
    
    /// <summary>
    /// 게임 오버 처리
    /// </summary>
    public void GameOver(string message = "게임 오버!")
    {
        if (currentGameState != GameState.GameOver)
        {
            // 게임 오버 메시지 설정
            if (uiManager != null)
            {
                uiManager.SetGameOverMessage(message);
            }
            
            // 게임 상태 변경
            SetGameState(GameState.GameOver);
        }
    }
    
    /// <summary>
    /// 게임 승리 처리
    /// </summary>
    public void Victory()
    {
        if (currentGameState != GameState.Victory)
        {
            SetGameState(GameState.Victory);
        }
    }
    
    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        // 씬 재로드 또는 게임 상태 리셋
        SetGameState(GameState.Playing);
    }
    
    /// <summary>
    /// 현재 게임 상태 반환
    /// </summary>
    public GameState GetCurrentGameState()
    {
        return currentGameState;
    }
    
    /// <summary>
    /// 플레이어 탐지 상태 반환
    /// </summary>
    public bool IsPlayerDetected()
    {
        return isPlayerDetected;
    }
    
    /// <summary>
    /// 플레이어가 승리 조건을 달성했는지 확인
    /// </summary>
    public bool HasCollectedAllBatteries()
    {
        return batteriesCollected >= maxBatteriesNeeded;
    }
} 