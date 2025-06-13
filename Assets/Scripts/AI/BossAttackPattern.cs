using UnityEngine;

/// <summary>
/// 보스 공격 패턴 관리 스크립트
/// Attack1과 Attack2를 번갈아 사용하도록 제어
/// </summary>
public class BossAttackPattern : MonoBehaviour
{
    [Header("공격 패턴 설정")]
    [Tooltip("체크하면 Boss 모드 (Attack1↔Attack2 순환), 체크 안하면 Enemy 모드 (Attack1만)")]
    public bool isBoss = false;
    
    [Header("디버그")]
    public bool enableDebug = true;
    
    // 내부 변수
    private int attackIndex = 0;  // 0: Attack1, 1: Attack2
    private Animator animator;
    
    void Start()
    {
        // 컴포넌트 초기화
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError($"[BossAttackPattern] {name}에 Animator 컴포넌트가 없습니다!");
            enabled = false;
            return;
        }
        
        // 초기 AttackIndex 설정
        animator.SetInteger("AttackIndex", 0);
        
        DebugLog($"초기화 완료 - 모드: {(isBoss ? "Boss" : "Enemy")}");
    }
    
    /// <summary>
    /// 공격 완료 시 호출되는 함수
    /// Animation Event에서 각 공격 애니메이션 끝에 호출
    /// </summary>
    public void OnAttackComplete()
    {
        if (isBoss)
        {
            // Boss 모드: Attack1 ↔ Attack2 번갈아가며
            attackIndex = attackIndex == 0 ? 1 : 0;
            animator.SetInteger("AttackIndex", attackIndex);
            
            string nextAttack = attackIndex == 0 ? "Attack1" : "Attack2";
            DebugLog($"다음 공격으로 전환: {nextAttack} (AttackIndex: {attackIndex})");
        }
        else
        {
            // Enemy 모드: 항상 Attack1 (AttackIndex = 0 유지)
            animator.SetInteger("AttackIndex", 0);
            DebugLog("Enemy 모드 - Attack1 유지");
        }
    }
    
    /// <summary>
    /// 현재 공격 인덱스 반환 (디버그용)
    /// </summary>
    public int GetCurrentAttackIndex()
    {
        return attackIndex;
    }
    
    /// <summary>
    /// 공격 패턴 강제 리셋 (필요시 사용)
    /// </summary>
    public void ResetAttackPattern()
    {
        attackIndex = 0;
        if (animator != null)
        {
            animator.SetInteger("AttackIndex", 0);
        }
        DebugLog("공격 패턴 리셋 - Attack1로 초기화");
    }
    
    /// <summary>
    /// 디버그 로그 출력
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebug)
        {
            Debug.Log($"[BossAttackPattern - {name}] {message}");
        }
    }
    
    /// <summary>
    /// Inspector에서 실시간 확인용
    /// </summary>
    void OnValidate()
    {
        // Inspector에서 isBoss 값이 변경될 때 즉시 반영
        if (Application.isPlaying && animator != null)
        {
            if (!isBoss)
            {
                // Enemy 모드로 변경 시 Attack1로 리셋
                ResetAttackPattern();
            }
        }
    }
} 