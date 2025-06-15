using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VR 호러 게임의 보스 AI 시스템
/// CultistAI를 기반으로 확장된 보스 전용 AI
/// 단계별 패턴 변화, 특수 공격, VR 최적화 포함
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("보스 기본 설정")]
    public string bossName = "Cult Leader";
    public float maxHealth = 500f;
    public float currentHealth;
    
    [Header("단계별 설정")]
    [Tooltip("1단계: 70% 이상, 2단계: 30-70%, 3단계: 30% 이하")]
    public float phase2HealthThreshold = 0.7f;  // 70%
    public float phase3HealthThreshold = 0.3f;  // 30%
    private int currentPhase = 1;
    
    [Header("이동 설정")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float chargeSpeed = 8f;  // 돌진 공격용
    
    [Header("감지 설정")]
    public float detectionRange = 15f;  // 일반 적보다 넓은 감지범위
    public float attackRange = 2.5f;
    public float specialAttackRange = 8f;  // 특수 공격 범위
    
    [Header("공격 설정")]
    public float basicAttackDamage = 40f;
    public float heavyAttackDamage = 60f;
    public float specialAttackDamage = 80f;
    public float attackCooldown = 2f;
    public float specialAttackCooldown = 8f;
    
    [Header("애니메이션 설정")]
    [Tooltip("보스 등장 애니메이션 시간")]
    public float introAnimationDuration = 3f;
    [Tooltip("단계 변화 애니메이션 시간")]
    public float phaseTransitionDuration = 2f;
    
    [Header("VR 효과")]
    public float screenShakeIntensity = 0.5f;
    public float hapticFeedbackIntensity = 0.8f;
    
    [Header("오디오 (VolumeManager 사용)")]
    [Tooltip("VolumeManager를 통해 사운드가 재생됩니다")]
    public bool useVolumeManager = true;
    
    // 컴포넌트 참조
    private NavMeshAgent agent;
    private Animator animator;
    private BossStateMachine stateMachine;
    
    // 플레이어 참조
    private Transform player;
    private VRPlayerHealth playerHealth;
    
    // 상태 관리
    private bool isDead = false;
    private bool isIntroPlaying = false;
    private bool isPhaseTransitioning = false;
    private float lastAttackTime = 0f;
    private float lastSpecialAttackTime = 0f;
    
    // 공격 패턴
    private List<BossAttackPattern> currentAttackPatterns = new List<BossAttackPattern>();
    private int lastUsedPatternIndex = -1;
    
    public enum BossAttackPattern
    {
        BasicAttack,    // 기본 공격
        HeavyAttack,    // 강공격 (긴 모션)
        ChargeAttack,   // 돌진 공격
        AreaAttack,     // 범위 공격
        Rage            // 분노 상태 (3단계)
    }
    
    // 프로퍼티
    public bool IsDead => isDead;
    public int CurrentPhase => currentPhase;
    public float HealthPercentage => currentHealth / maxHealth;
    
    void Start()
    {
        InitializeBoss();
        StartBossIntro();
    }
    
    void Update()
    {
        if (isDead || isIntroPlaying || isPhaseTransitioning) return;
        
        UpdatePhase();
        HandleBossAI();
    }
    
    void InitializeBoss()
    {
        // 컴포넌트 초기화
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // StateMachine 추가
        stateMachine = GetComponent<BossStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<BossStateMachine>();
        }
        
        // 체력 초기화
        currentHealth = maxHealth;
        
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = player.GetComponent<VRPlayerHealth>();
        }
        
        // NavMeshAgent 설정
        agent.speed = walkSpeed;
        agent.stoppingDistance = attackRange;
        
        // 1단계 공격 패턴 설정
        SetPhaseAttackPatterns(1);
        
        Debug.Log($"[BossAI] {bossName} 초기화 완료!");
    }
    
    void StartBossIntro()
    {
        isIntroPlaying = true;
        
        // 등장 사운드 재생 (VolumeManager 사용)
        if (useVolumeManager && VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossIntro);
            VolumeManager.Instance.PlayBossBattleBGM(); // 보스전 BGM 시작
        }
        
        // VR 화면 효과 (보스 등장)
        TriggerBossIntroEffects();
        
        // 인트로 완료 처리
        StartCoroutine(EndIntroAfterDelay());
    }
    
    IEnumerator EndIntroAfterDelay()
    {
        yield return new WaitForSeconds(introAnimationDuration);
        isIntroPlaying = false;
        
        // 전투 시작
        if (stateMachine != null)
        {
            stateMachine.SetState(BossStateMachine.BossState.Patrol);
        }
        Debug.Log($"[BossAI] {bossName} 전투 시작!");
    }
    
    void UpdatePhase()
    {
        float healthPercent = HealthPercentage;
        int newPhase = currentPhase;
        
        if (healthPercent <= phase3HealthThreshold && currentPhase < 3)
        {
            newPhase = 3;
        }
        else if (healthPercent <= phase2HealthThreshold && currentPhase < 2)
        {
            newPhase = 2;
        }
        
        if (newPhase != currentPhase)
        {
            StartPhaseTransition(newPhase);
        }
    }
    
    void StartPhaseTransition(int newPhase)
    {
        if (isPhaseTransitioning) return;
        
        StartCoroutine(HandlePhaseTransition(newPhase));
    }
    
    IEnumerator HandlePhaseTransition(int newPhase)
    {
        isPhaseTransitioning = true;
        int previousPhase = currentPhase;
        currentPhase = newPhase;
        
        Debug.Log($"[BossAI] {bossName} 단계 변화: {previousPhase} → {newPhase}");
        
        // 단계 변화 애니메이션
        if (animator != null)
        {
            animator.SetTrigger($"PhaseTransition{newPhase}");
            animator.SetInteger("BossPhase", newPhase);
        }
        
        // 단계별 사운드 (VolumeManager 사용)
        if (useVolumeManager && VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossPhaseTransition, transform.position, transform);
        }
        
        // VR 효과
        TriggerPhaseTransitionEffects(newPhase);
        
        // 공격 패턴 변경
        SetPhaseAttackPatterns(newPhase);
        
        // 이동 속도 조정
        AdjustSpeedForPhase(newPhase);
        
        yield return new WaitForSeconds(phaseTransitionDuration);
        
        isPhaseTransitioning = false;
        Debug.Log($"[BossAI] {bossName} {newPhase}단계 전환 완료!");
    }
    
    void SetPhaseAttackPatterns(int phase)
    {
        currentAttackPatterns.Clear();
        
        switch (phase)
        {
            case 1: // 1단계: 기본적인 패턴
                currentAttackPatterns.Add(BossAttackPattern.BasicAttack);
                currentAttackPatterns.Add(BossAttackPattern.HeavyAttack);
                break;
                
            case 2: // 2단계: 돌진 추가
                currentAttackPatterns.Add(BossAttackPattern.BasicAttack);
                currentAttackPatterns.Add(BossAttackPattern.HeavyAttack);
                currentAttackPatterns.Add(BossAttackPattern.ChargeAttack);
                currentAttackPatterns.Add(BossAttackPattern.AreaAttack);
                break;
                
            case 3: // 3단계: 모든 패턴
                currentAttackPatterns.Add(BossAttackPattern.BasicAttack);
                currentAttackPatterns.Add(BossAttackPattern.HeavyAttack);
                currentAttackPatterns.Add(BossAttackPattern.ChargeAttack);
                currentAttackPatterns.Add(BossAttackPattern.AreaAttack);
                currentAttackPatterns.Add(BossAttackPattern.Rage);
                break;
        }
        
        Debug.Log($"[BossAI] {phase}단계 공격 패턴 설정 완료 ({currentAttackPatterns.Count}개)");
    }
    
    void AdjustSpeedForPhase(int phase)
    {
        switch (phase)
        {
            case 1:
                agent.speed = walkSpeed;
                break;
            case 2:
                agent.speed = runSpeed;
                break;
            case 3:
                agent.speed = runSpeed * 1.2f; // 3단계에서 더 빨라짐
                break;
        }
    }
    
    void HandleBossAI()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // 공격 범위 체크
        if (distanceToPlayer <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
        }
        // 특수 공격 범위 체크 (더 먼 거리에서)
        else if (distanceToPlayer <= specialAttackRange && Time.time - lastSpecialAttackTime >= specialAttackCooldown)
        {
            PerformSpecialAttack();
        }
        
        // 플레이어 바라보기
        RotateTowardsPlayer();
    }
    
    void PerformAttack()
    {
        if (currentAttackPatterns.Count == 0) return;
        
        // 다음 공격 패턴 선택 (이전과 다른 패턴)
        int patternIndex;
        do
        {
            patternIndex = Random.Range(0, currentAttackPatterns.Count);
        }
        while (patternIndex == lastUsedPatternIndex && currentAttackPatterns.Count > 1);
        
        lastUsedPatternIndex = patternIndex;
        BossAttackPattern selectedPattern = currentAttackPatterns[patternIndex];
        
        ExecuteAttackPattern(selectedPattern);
        lastAttackTime = Time.time;
    }
    
    void PerformSpecialAttack()
    {
        // 3단계에서만 특수 공격 사용
        if (currentPhase < 3) return;
        
        List<BossAttackPattern> specialPatterns = new List<BossAttackPattern>
        {
            BossAttackPattern.Rage
        };
        
        BossAttackPattern specialPattern = specialPatterns[Random.Range(0, specialPatterns.Count)];
        ExecuteAttackPattern(specialPattern);
        
        lastSpecialAttackTime = Time.time;
    }
    
    void ExecuteAttackPattern(BossAttackPattern pattern)
    {
        Debug.Log($"[BossAI] {bossName} 공격 패턴 실행: {pattern}");
        
        switch (pattern)
        {
            case BossAttackPattern.BasicAttack:
                StartCoroutine(BasicAttackSequence());
                break;
            case BossAttackPattern.HeavyAttack:
                StartCoroutine(HeavyAttackSequence());
                break;
            case BossAttackPattern.ChargeAttack:
                StartCoroutine(ChargeAttackSequence());
                break;
            case BossAttackPattern.AreaAttack:
                StartCoroutine(AreaAttackSequence());
                break;
            case BossAttackPattern.Rage:
                StartCoroutine(RageSequence());
                break;
        }
    }
    
    IEnumerator BasicAttackSequence()
    {
        // 애니메이션 재생
        animator?.SetTrigger("BasicAttack");
        
        // 공격 사운드
        PlayAttackSound();
        
        // 공격 타이밍 (애니메이션에 맞춰 조정)
        yield return new WaitForSeconds(0.8f);
        
        // 플레이어가 범위 내에 있으면 데미지
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            DealDamageToPlayer(basicAttackDamage);
        }
    }
    
    IEnumerator HeavyAttackSequence()
    {
        // 예비 동작 (플레이어에게 경고)
        animator?.SetTrigger("HeavyAttackCharge");
        yield return new WaitForSeconds(1.2f);
        
        // 실제 공격
        animator?.SetTrigger("HeavyAttack");
        PlayAttackSound();
        
        yield return new WaitForSeconds(0.6f);
        
        if (Vector3.Distance(transform.position, player.position) <= attackRange * 1.5f)
        {
            DealDamageToPlayer(heavyAttackDamage);
            TriggerScreenShake();
        }
    }
    
    IEnumerator ChargeAttackSequence()
    {
        Vector3 playerPosition = player.position;
        Vector3 chargeDirection = (playerPosition - transform.position).normalized;
        
        // 돌진 준비
        animator?.SetTrigger("ChargeStart");
        yield return new WaitForSeconds(0.8f);
        
        // 돌진 실행
        animator?.SetTrigger("ChargeAttack");
        agent.speed = chargeSpeed;
        
        float chargeTime = 0f;
        float maxChargeTime = 2f;
        
        while (chargeTime < maxChargeTime)
        {
            // 플레이어와 충돌 체크
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                DealDamageToPlayer(heavyAttackDamage);
                TriggerScreenShake();
                break;
            }
            
            chargeTime += Time.deltaTime;
            yield return null;
        }
        
        // 속도 복구
        AdjustSpeedForPhase(currentPhase);
    }
    
    IEnumerator AreaAttackSequence()
    {
        // 범위 공격 준비
        animator?.SetTrigger("AreaAttackCharge");
        
        // VR 경고 효과 (빨간 원 등)
        ShowAreaAttackWarning();
        
        yield return new WaitForSeconds(2f);
        
        // 범위 공격 실행
        animator?.SetTrigger("AreaAttack");
        PlayAttackSound();
        
        yield return new WaitForSeconds(0.5f);
        
        // 넓은 범위 데미지
        if (Vector3.Distance(transform.position, player.position) <= specialAttackRange)
        {
            DealDamageToPlayer(specialAttackDamage);
            TriggerScreenShake();
        }
        
        HideAreaAttackWarning();
    }
    

    
    IEnumerator RageSequence()
    {
        animator?.SetTrigger("Rage");
        animator?.SetBool("IsRaging", true);
        
        // 분노 상태: 공격속도 증가, 이동속도 증가
        float originalCooldown = attackCooldown;
        attackCooldown *= 0.5f; // 공격속도 2배
        agent.speed *= 1.5f;    // 이동속도 1.5배
        
        // VR 효과
        TriggerRageEffects();
        
        yield return new WaitForSeconds(8f); // 8초간 분노 상태
        
        // 원래 상태로 복구
        attackCooldown = originalCooldown;
        AdjustSpeedForPhase(currentPhase);
        animator?.SetBool("IsRaging", false);
        
        Debug.Log($"[BossAI] {bossName} 분노 상태 종료");
    }
    
    public void TakeDamage(float damage, Vector3 attackPosition)
    {
        if (isDead || isIntroPlaying) return;
        
        currentHealth -= damage;
        
        Debug.Log($"[BossAI] {bossName} 데미지 -{damage}! 체력: {currentHealth}/{maxHealth}");
        
        // 피격 애니메이션
        animator?.SetTrigger("Hit");
        
        // 체력이 0 이하면 사망
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        isDead = true;
        currentHealth = 0;
        
        Debug.Log($"[BossAI] {bossName} 사망!");
        
        // 사망 애니메이션
        animator?.SetBool("IsDead", true);
        animator?.SetTrigger("Die");
        
        // 사망 사운드 (VolumeManager 사용)
        if (useVolumeManager && VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossDeath, transform.position);
            VolumeManager.Instance.PlayVictoryBGM(); // 승리 BGM 재생
        }
        
        // NavMeshAgent 비활성화
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        // VR 보스 사망 효과
        TriggerBossDeathEffects();
        
        // 게임 승리 처리 등
        OnBossDefeated();
    }
    
    void DealDamageToPlayer(float damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Debug.Log($"[BossAI] 플레이어에게 {damage} 데미지!");
        }
    }
    
    void RotateTowardsPlayer()
    {
        if (player == null) return;
        
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 3f * Time.deltaTime);
        }
    }
    
    void PlayAttackSound()
    {
        if (useVolumeManager && VolumeManager.Instance != null)
        {
            // 현재 공격 패턴에 따라 다른 사운드 재생
            VolumeManager.SFXType attackSFX = VolumeManager.SFXType.BossAttack;
            
            // 3D 공간 사운드로 재생 (보스 위치에서)
            VolumeManager.Instance.PlaySFX(attackSFX, transform.position, transform);
        }
    }
    
    void TriggerBossIntroEffects()
    {
        // VR 보스 등장 효과 (화면 어둡게, 햅틱 등)
        TriggerHapticFeedback();
    }
    
    void TriggerPhaseTransitionEffects(int newPhase)
    {
        // 단계 변화 시 VR 효과
        TriggerScreenShake();
        TriggerHapticFeedback();
    }
    
    void TriggerScreenShake()
    {
        // VR 화면 흔들림 효과 구현
        // VRPostProcessingManager 활용 가능
    }
    
    void TriggerHapticFeedback()
    {
        // VR 컨트롤러 진동
        OVRInput.SetControllerVibration(hapticFeedbackIntensity, hapticFeedbackIntensity, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(hapticFeedbackIntensity, hapticFeedbackIntensity, OVRInput.Controller.RTouch);
        
        StartCoroutine(StopHapticAfterDelay(0.3f));
    }
    
    IEnumerator StopHapticAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
    
    void ShowAreaAttackWarning()
    {
        // VR 범위 공격 경고 표시 (빨간 원 등)
    }
    
    void HideAreaAttackWarning()
    {
        // 경고 표시 숨기기
    }
    

    
    void TriggerRageEffects()
    {
        // 분노 상태 VR 효과 (화면 빨갛게, 강한 햅틱 등)
        TriggerHapticFeedback();
    }
    
    void TriggerBossDeathEffects()
    {
        // 보스 사망 시 VR 효과
        TriggerScreenShake();
        TriggerHapticFeedback();
    }
    
    void OnBossDefeated()
    {
        // 게임 승리 처리
        Debug.Log("보스 처치! 게임 승리!");
        // GameOverManager나 게임 승리 매니저 호출
    }
    
    // 에디터용 기즈모
    void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 특수 공격 범위
        Gizmos.color = Color.magenta; // Color.purple은 Unity에서 지원하지 않으므로 magenta로 변경
        Gizmos.DrawWireSphere(transform.position, specialAttackRange);
        
        // 체력 바
        if (Application.isPlaying && !isDead)
        {
            Vector3 healthBarPos = transform.position + Vector3.up * 3f;
            float healthPercentage = currentHealth / maxHealth;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, healthBarPos + Vector3.right * 1f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(healthBarPos - Vector3.right * 1f, 
                           healthBarPos + Vector3.right * (healthPercentage * 2f - 1f));
        }
    }
} 