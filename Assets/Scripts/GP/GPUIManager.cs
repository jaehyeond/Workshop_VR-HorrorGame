using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// GP 게임의 UI 요소를 관리하는 컴포넌트
/// </summary>
public class GPUIManager : MonoBehaviour
{
    [Header("타이머 UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image timerBackground;
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = Color.red;
    [SerializeField] private float warningBlinkRate = 1f; // 경고 시 깜빡임 속도
    
    [Header("게임 상태 UI")]
    [SerializeField] private GameObject gameStartPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameSuccessPanel;
    [SerializeField] private GameObject introductionPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button restartButton;
    
    [Header("아이템 상태 UI")]
    [SerializeField] private GameObject keyIcon;
    [SerializeField] private GameObject noKeyIcon;
    [SerializeField] private TextMeshProUGUI batteryCountText;
    
    [Header("알림 UI")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 3f;
    
    [Header("상호작용 UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI interactionText;
    
    [Header("은신 상태 UI")]
    [SerializeField] private GameObject hidingIndicator;
    
    [Header("탈출 UI")]
    [SerializeField] private GameObject escapeIndicator;
    
    // 내부 상태
    private bool isWarningActive = false;
    private float blinkTimer = 0f;
    private bool blinkState = false;
    private float notificationTimer = 0f;
    
    private void Start()
    {
        // 기본 UI 상태 설정
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
            
        if (hidingIndicator != null)
            hidingIndicator.SetActive(false);
            
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        if (gameSuccessPanel != null)
            gameSuccessPanel.SetActive(false);
            
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
            
        if (escapeIndicator != null)
            escapeIndicator.SetActive(false);
            
        // 재시작 버튼 이벤트 설정
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => {
                // 게임 재시작 로직
                if (GPGameManager.Instance != null)
                {
                    GPGameManager.Instance.RestartGame();
                }
            });
        }
    }
    
    private void Update()
    {
        // 경고 상태일 때 타이머 깜빡임 처리
        if (isWarningActive && timerText != null)
        {
            blinkTimer += Time.deltaTime;
            
            if (blinkTimer >= warningBlinkRate)
            {
                blinkTimer = 0f;
                blinkState = !blinkState;
                
                if (timerBackground != null)
                {
                    timerBackground.color = blinkState ? warningTimerColor : normalTimerColor;
                }
                else
                {
                    timerText.color = blinkState ? warningTimerColor : normalTimerColor;
                }
            }
        }
        
        // 알림 타이머 업데이트
        if (notificationPanel != null && notificationPanel.activeSelf)
        {
            notificationTimer -= Time.deltaTime;
            
            if (notificationTimer <= 0)
            {
                notificationPanel.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 타이머 UI 업데이트
    /// </summary>
    public void UpdateTimer(float remainingSeconds)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingSeconds / 60);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60);
            
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    /// <summary>
    /// 시간 경고 표시 활성화
    /// </summary>
    public void ShowTimeWarning(bool show)
    {
        isWarningActive = show;
        
        if (!show)
        {
            // 경고 상태 해제 시 색상 원복
            if (timerBackground != null)
            {
                timerBackground.color = normalTimerColor;
            }
            
            if (timerText != null)
            {
                timerText.color = normalTimerColor;
            }
        }
    }
    
    /// <summary>
    /// 게임 시작 UI 표시
    /// </summary>
    public void ShowGameStartUI(bool show)
    {
        if (gameStartPanel != null)
        {
            gameStartPanel.SetActive(show);
        }
    }
    
    /// <summary>
    /// 인트로 화면 표시
    /// </summary>
    public void ShowIntroduction(bool show)
    {
        if (introductionPanel != null)
        {
            introductionPanel.SetActive(show);
        }
    }
    
    /// <summary>
    /// 게임 오버 UI 표시
    /// </summary>
    public void ShowGameOver(bool show)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(show);
        }
    }
    
    /// <summary>
    /// 게임 오버 메시지 설정
    /// </summary>
    public void SetGameOverMessage(string message)
    {
        if (gameOverText != null)
        {
            gameOverText.text = message;
        }
    }
    
    /// <summary>
    /// 승리 UI 표시
    /// </summary>
    public void ShowVictory(bool show)
    {
        if (gameSuccessPanel != null)
        {
            gameSuccessPanel.SetActive(show);
        }
    }
    
    /// <summary>
    /// 배터리 수집 개수 업데이트
    /// </summary>
    public void UpdateBatteryCount(int collected, int total)
    {
        if (batteryCountText != null)
        {
            batteryCountText.text = $"{collected}/{total}";
        }
    }
    
    /// <summary>
    /// 탈출 지시 표시 설정
    /// </summary>
    public void ShowEscapeIndicator(bool show)
    {
        if (escapeIndicator != null)
        {
            escapeIndicator.SetActive(show);
        }
    }
    
    /// <summary>
    /// 알림 메시지 표시
    /// </summary>
    public void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            notificationTimer = notificationDuration;
        }
    }
    
    /// <summary>
    /// 열쇠 소지 상태 UI 업데이트
    /// </summary>
    public void SetKeyStatus(bool hasKey)
    {
        if (keyIcon != null)
        {
            keyIcon.SetActive(hasKey);
        }
        
        if (noKeyIcon != null)
        {
            noKeyIcon.SetActive(!hasKey);
        }
    }
    
    /// <summary>
    /// 상호작용 프롬프트 표시
    /// </summary>
    public void ShowInteractionPrompt(string message, bool show)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show);
            
            if (show && interactionText != null)
            {
                interactionText.text = message;
            }
        }
    }
    
    /// <summary>
    /// 은신 상태 표시기 업데이트
    /// </summary>
    public void UpdateHidingIndicator(bool isHiding)
    {
        if (hidingIndicator != null)
        {
            hidingIndicator.SetActive(isHiding);
        }
    }
} 