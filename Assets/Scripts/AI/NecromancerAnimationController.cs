using UnityEngine;

/// <summary>
/// 네크로맨서 애니메이션 전용 컨트롤러
/// 애니메이션 재생과 상태 관리만 담당
/// </summary>
public class NecromancerAnimationController : MonoBehaviour
{
    [Header("=== 애니메이션 설정 ===")]
    [SerializeField] private bool enableAnimationLogs = true;
    
    // 기존 애니메이션 컨트롤러와 호환되는 파라미터들
    // 파라미터가 없는 기존 컨트롤러에서는 직접 애니메이션 재생 사용
    private const string PARAM_STATE = "State";
    private const string PARAM_ATTACK_TYPE = "AttackType";
    private const string PARAM_IS_MOVING = "IsMoving";
    private const string PARAM_HIT_TRIGGER = "Hit";
    private const string PARAM_DEATH_TRIGGER = "Death";
    private const string PARAM_ROAR_TRIGGER = "Roar";
    private const string PARAM_CAST_TRIGGER = "Cast";
    
    // 기존 컨트롤러 호환 모드
    private bool useDirectAnimationPlay = true;
    
    // 애니메이션 상태 값들
    private const int STATE_IDLE = 0;
    private const int STATE_WALK = 1;
    private const int STATE_ATTACK = 2;
    private const int STATE_CAST = 3;
    private const int STATE_HIT = 4;
    private const int STATE_DEATH = 5;
    
    // 컴포넌트 참조
    private Animator animator;
    private NecromancerBoss bossController;
    
    // 애니메이션 상태 추적
    private int currentAnimationState = STATE_IDLE;
    private bool isPlayingSpecialAnimation = false;
    private float lastAttackTime = 0f;
    private int lastAttackType = 1; // 1 또는 2 (atack1, atack2)
    
    // 애니메이션 이벤트 콜백
    public System.Action OnAttackHit;
    public System.Action OnAttackComplete;
    public System.Action OnCastComplete;
    public System.Action OnHitComplete;
    public System.Action OnDeathComplete;
    
    public void Initialize(NecromancerBoss boss)
    {
        bossController = boss;
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError($"[NecromancerAnimation] Animator 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        // 초기 상태 설정
        SetAnimationState(STATE_IDLE);
        
        AnimLog("애니메이션 컨트롤러 초기화 완료");
    }
    
    public void OnStateChanged(NecromancerBoss.BossState newState, NecromancerBoss.BossState previousState)
    {
        switch (newState)
        {
            case NecromancerBoss.BossState.Idle:
                PlayIdle();
                break;
                
            case NecromancerBoss.BossState.Chasing:
                PlayWalk();
                break;
                
            case NecromancerBoss.BossState.Attacking:
                PlayAttack();
                break;
                
            case NecromancerBoss.BossState.Casting:
                PlaySpellCast();
                break;
                
            case NecromancerBoss.BossState.Hit:
                PlayHit();
                break;
                
            case NecromancerBoss.BossState.Dead:
                PlayDeath();
                break;
        }
    }
    
    #region 애니메이션 재생 메서드들
    
    public void PlayIdle()
    {
        if (isPlayingSpecialAnimation) return;
        
        SetAnimationState(STATE_IDLE);
        SetMoving(false);
        
        // idle1과 idle2 중 랜덤 선택
        if (Random.Range(0f, 1f) < 0.7f)
        {
            PlayAnimationDirect("idle1");
        }
        else
        {
            PlayAnimationDirect("idle2");
        }
        
        AnimLog("Idle 애니메이션 재생");
    }
    
    public void PlayWalk()
    {
        if (isPlayingSpecialAnimation) return;
        
        SetAnimationState(STATE_WALK);
        SetMoving(true);
        PlayAnimationDirect("walk");
        
        AnimLog("Walk 애니메이션 재생");
    }
    
    public void PlayAttack()
    {
        if (Time.time - lastAttackTime < 1f) return; // 공격 쿨다운
        
        isPlayingSpecialAnimation = true;
        lastAttackTime = Time.time;
        
        // 공격 타입 번갈아가며 사용
        lastAttackType = (lastAttackType == 1) ? 2 : 1;
        
        SetAnimationState(STATE_ATTACK);
        SetAttackType(lastAttackType);
        
        string attackAnim = (lastAttackType == 1) ? "atack1" : "atack2";
        PlayAnimationDirect(attackAnim);
        
        AnimLog($"Attack{lastAttackType} 애니메이션 재생");
        
        // 공격 완료 타이머 (애니메이션 길이에 맞춰 조정)
        Invoke(nameof(OnAttackAnimationComplete), 1.5f);
    }
    
    public void PlaySpellCast()
    {
        isPlayingSpecialAnimation = true;
        
        SetAnimationState(STATE_CAST);
        TriggerAnimation(PARAM_CAST_TRIGGER);
        PlayAnimationDirect("spellcast1");
        
        AnimLog("SpellCast 애니메이션 재생");
        
        // 시전 완료 타이머
        Invoke(nameof(OnCastAnimationComplete), 2f);
    }
    
    public void PlayHit()
    {
        isPlayingSpecialAnimation = true;
        
        SetAnimationState(STATE_HIT);
        TriggerAnimation(PARAM_HIT_TRIGGER);
        PlayAnimationDirect("getgit");
        
        AnimLog("Hit 애니메이션 재생");
        
        // 피격 완료 타이머
        Invoke(nameof(OnHitAnimationComplete), 0.8f);
    }
    
    public void PlayDeath()
    {
        isPlayingSpecialAnimation = true;
        
        SetAnimationState(STATE_DEATH);
        TriggerAnimation(PARAM_DEATH_TRIGGER);
        PlayAnimationDirect("death");
        
        AnimLog("Death 애니메이션 재생");
        
        // 사망 애니메이션 완료 후 처리
        Invoke(nameof(OnDeathAnimationComplete), 3f);
    }
    
    public void PlayRoar()
    {
        if (isPlayingSpecialAnimation) return;
        
        TriggerAnimation(PARAM_ROAR_TRIGGER);
        PlayAnimationDirect("roar");
        
        AnimLog("Roar 애니메이션 재생");
        
        // 포효 완료 후 자동으로 이전 상태로 복귀
        Invoke(nameof(OnRoarComplete), 1.5f);
    }
    
    public void PlayWound()
    {
        // 중상 애니메이션 (필요시 사용)
        PlayAnimationDirect("wound");
        AnimLog("Wound 애니메이션 재생");
    }
    
    #endregion
    
    #region 애니메이션 파라미터 설정
    
    private void SetAnimationState(int state)
    {
        if (animator != null && currentAnimationState != state)
        {
            currentAnimationState = state;
            // 기존 컨트롤러에 파라미터가 없을 수 있으므로 try-catch 사용
            if (!useDirectAnimationPlay)
            {
                try { animator.SetInteger(PARAM_STATE, state); }
                catch { /* 파라미터가 없으면 무시 */ }
            }
        }
    }
    
    private void SetAttackType(int attackType)
    {
        if (animator != null && !useDirectAnimationPlay)
        {
            try { animator.SetInteger(PARAM_ATTACK_TYPE, attackType); }
            catch { /* 파라미터가 없으면 무시 */ }
        }
    }
    
    private void SetMoving(bool isMoving)
    {
        if (animator != null && !useDirectAnimationPlay)
        {
            try { animator.SetBool(PARAM_IS_MOVING, isMoving); }
            catch { /* 파라미터가 없으면 무시 */ }
        }
    }
    
    private void TriggerAnimation(string triggerName)
    {
        if (animator != null && !useDirectAnimationPlay)
        {
            try { animator.SetTrigger(triggerName); }
            catch { /* 파라미터가 없으면 무시 */ }
        }
    }
    
    private void PlayAnimationDirect(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName, 0, 0f);
        }
    }
    
    #endregion
    
    #region 애니메이션 완료 콜백들
    
    private void OnAttackAnimationComplete()
    {
        isPlayingSpecialAnimation = false;
        OnAttackComplete?.Invoke();
        
        if (bossController != null)
            bossController.OnAttackComplete();
            
        AnimLog("공격 애니메이션 완료");
    }
    
    private void OnCastAnimationComplete()
    {
        isPlayingSpecialAnimation = false;
        OnCastComplete?.Invoke();
        
        if (bossController != null)
            bossController.OnCastComplete();
            
        AnimLog("시전 애니메이션 완료");
    }
    
    private void OnHitAnimationComplete()
    {
        isPlayingSpecialAnimation = false;
        OnHitComplete?.Invoke();
        
        if (bossController != null)
            bossController.OnHitComplete();
            
        AnimLog("피격 애니메이션 완료");
    }
    
    private void OnDeathAnimationComplete()
    {
        OnDeathComplete?.Invoke();
        AnimLog("사망 애니메이션 완료");
    }
    
    private void OnRoarComplete()
    {
        AnimLog("포효 애니메이션 완료");
        // 포효 후 자동으로 이전 상태로 복귀하지 않음 (보스 로직에서 처리)
    }
    
    #endregion
    
    #region 애니메이션 이벤트 (Animation Event에서 호출)
    
    // 공격 애니메이션의 타격 지점에서 호출
    public void OnAttackHitEvent()
    {
        OnAttackHit?.Invoke();
        AnimLog("공격 타격 이벤트");
    }
    
    // 마법 시전의 발동 지점에서 호출
    public void OnSpellCastEvent()
    {
        AnimLog("마법 시전 이벤트");
    }
    
    #endregion
    
    #region 유틸리티
    
    public bool IsPlayingAnimation(string animationName)
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName);
    }
    
    public float GetCurrentAnimationLength()
    {
        if (animator == null) return 0f;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length;
    }
    
    public float GetCurrentAnimationNormalizedTime()
    {
        if (animator == null) return 0f;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime;
    }
    
    private void AnimLog(string message)
    {
        if (enableAnimationLogs)
            Debug.Log($"[NecromancerAnim] {message}");
    }
    
    #endregion
    
    void OnDestroy()
    {
        // 모든 Invoke 취소
        CancelInvoke();
    }
} 