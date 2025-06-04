using UnityEngine;

/// <summary>
/// 광신도 애니메이션 설정 가이드
/// 
/// Animator Controller에 다음 파라미터들을 추가해야 합니다:
/// 
/// 기존 파라미터들:
/// - PlayerDetected (Bool): 플레이어 감지 여부
/// - StartChase (Bool): 추격 시작
/// - InAttackRange (Bool): 공격 범위 내 여부
/// - LostPlayer (Bool): 플레이어를 놓침
/// - ReturnToPraying (Bool): 기도 위치로 복귀
/// - IsStunned (Bool): 스턴 상태
/// 
/// 새로 추가할 파라미터들:
/// - Hit (Trigger): 피격 애니메이션 트리거
/// - Die (Trigger): 사망 애니메이션 트리거
/// - HitCount (Int): 현재 타격 횟수 (0~최대값)
/// - IsDead (Bool): 사망 상태
/// 
/// 권장 애니메이션 상태들:
/// 1. Idle_Praying: 기도 상태 (루프)
/// 2. Walk: 걷기 애니메이션
/// 3. Run: 달리기 애니메이션
/// 4. Hit_1: 첫 번째 피격 애니메이션
/// 5. Hit_2: 두 번째 피격 애니메이션
/// 6. Hit_3: 세 번째 피격 애니메이션 (일반 광신도 사망)
/// 7. Hit_4: 네 번째 피격 애니메이션 (간부용)
/// 8. Hit_5: 다섯 번째 피격 애니메이션 (간부 사망)
/// 9. Death: 사망 애니메이션
/// 10. Attack: 공격 애니메이션
/// 
/// 애니메이션 트랜지션 조건 예시:
/// - Any State -> Hit_X: Hit 트리거 + HitCount == X
/// - Hit_X -> Death: HitCount >= MaxHitCount
/// - Hit_X -> Idle_Praying: 애니메이션 완료 후
/// 
/// 필요한 애니메이션 에셋:
/// 1. 기본 휴머노이드 애니메이션 팩
/// 2. 피격 반응 애니메이션 (뒤로 밀려나는 동작)
/// 3. 사망 애니메이션 (쓰러지는 동작)
/// 4. 기도 애니메이션 (무릎 꿇고 기도하는 동작)
/// 
/// Unity Asset Store 추천 에셋:
/// - "Basic Motions FREE" by Kevin Iglesias
/// - "Sword And Shield Animset Free" by Explosive
/// - "RPG Character Mecanim Animation Pack FREE" by Explosive
/// </summary>
public class AnimationGuide : MonoBehaviour
{
    [Header("애니메이션 테스트용")]
    [SerializeField] private Animator testAnimator;
    [SerializeField] private bool testMode = false;
    
    [Header("테스트 버튼들")]
    [SerializeField] private bool triggerHit = false;
    [SerializeField] private bool triggerDeath = false;
    [SerializeField] private int setHitCount = 0;
    
    private void Update()
    {
        if (!testMode || testAnimator == null) return;
        
        // 테스트 버튼 처리
        if (triggerHit)
        {
            triggerHit = false;
            testAnimator.SetTrigger("Hit");
            testAnimator.SetInteger("HitCount", setHitCount);
            Debug.Log($"Hit 트리거 실행, HitCount: {setHitCount}");
        }
        
        if (triggerDeath)
        {
            triggerDeath = false;
            testAnimator.SetTrigger("Die");
            testAnimator.SetBool("IsDead", true);
            Debug.Log("Death 트리거 실행");
        }
    }
    
    /// <summary>
    /// 애니메이션 파라미터 설정 도우미 메서드
    /// </summary>
    public static void SetupAnimatorParameters(Animator animator)
    {
        if (animator == null) return;
        
        // 기존 파라미터들
        animator.SetBool("PlayerDetected", false);
        animator.SetBool("StartChase", false);
        animator.SetBool("InAttackRange", false);
        animator.SetBool("LostPlayer", false);
        animator.SetBool("ReturnToPraying", false);
        animator.SetBool("IsStunned", false);
        
        // 새로운 피격/사망 관련 파라미터들
        animator.SetInteger("HitCount", 0);
        animator.SetBool("IsDead", false);
        
        Debug.Log("애니메이터 파라미터 설정 완료");
    }
} 