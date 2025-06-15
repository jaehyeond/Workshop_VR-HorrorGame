using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 네크로맨서 보스 메인 컨트롤러
/// 각 모듈들을 조율하는 역할만 담당 (단일 책임 원칙)
/// </summary>
public class NecromancerBoss : MonoBehaviour
{
    [Header("=== 보스 기본 정보 ===")]
    public string bossName = "Necromancer";
    public float maxHealth = 400f;
    
    [Header("=== 감지 및 공격 범위 ===")]
    public float detectionRange = 12f;
    public float attackRange = 3f;
    public float specialAttackRange = 6f;
    
    [Header("=== 디버그 ===")]
    public bool enableDebugLogs = true;
    
    // 컴포넌트 참조
    private NecromancerAnimationController animController;
    private NecromancerCombatSystem combatSystem;
    private NecromancerMovement movement;
    private NecromancerHealth healthSystem;
    private NecromancerVFXController vfxController;
    
    // 플레이어 참조
    private Transform player;
    private VRPlayerHealth playerHealth;
    
    // 상태 관리
    private BossState currentState = BossState.Idle;
    private bool isDead = false;
    private bool isInCombat = false;
    
    public enum BossState
    {
        Idle,           // 대기
        Patrol,         // 순찰 (필요시)
        Chasing,        // 추격
        Attacking,      // 공격
        Casting,        // 마법 시전
        Hit,            // 피격
        Dead            // 사망
    }
    
    // 프로퍼티
    public bool IsDead => isDead;
    public bool IsInCombat => isInCombat;
    public BossState CurrentState => currentState;
    public Transform Player => player;
    
    // 이벤트
    public System.Action OnBossDefeated;
    
    void Start()
    {
        InitializeComponents();
        FindPlayer();
        SetState(BossState.Idle);
        
        // 보스 등장 사운드
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossIntro, transform.position);
        }
        
        DebugLog($"{bossName} 초기화 완료!");
    }
    
    void Update()
    {
        if (isDead) return;
        
        UpdateBossLogic();
    }
    
    void InitializeComponents()
    {
        // 각 모듈 컴포넌트 가져오기
        animController = GetComponent<NecromancerAnimationController>();
        combatSystem = GetComponent<NecromancerCombatSystem>();
        movement = GetComponent<NecromancerMovement>();
        healthSystem = GetComponent<NecromancerHealth>();
        vfxController = GetComponent<NecromancerVFXController>();
        
        // 컴포넌트 초기화
        if (animController != null) animController.Initialize(this);
        if (combatSystem != null) combatSystem.Initialize(this);
        if (movement != null) movement.Initialize(this);
        if (healthSystem != null) healthSystem.Initialize(this, maxHealth);
        if (vfxController != null) vfxController.Initialize(this);
        
        // 이벤트 연결
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthChanged;
            healthSystem.OnDeath += OnDeath;
            healthSystem.OnHit += OnHit;
        }
    }
    
    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = player.GetComponent<VRPlayerHealth>();
            DebugLog("플레이어 발견!");
        }
        else
        {
            Debug.LogError($"[{bossName}] Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }
    
    void UpdateBossLogic()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        switch (currentState)
        {
            case BossState.Idle:
                HandleIdleState(distanceToPlayer);
                break;
                
            case BossState.Chasing:
                HandleChasingState(distanceToPlayer);
                break;
                
            case BossState.Attacking:
                HandleAttackingState(distanceToPlayer);
                break;
                
            case BossState.Casting:
                HandleCastingState(distanceToPlayer);
                break;
                
            case BossState.Hit:
                HandleHitState();
                break;
        }
    }
    
    void HandleIdleState(float distanceToPlayer)
    {
        if (distanceToPlayer <= detectionRange)
        {
            SetState(BossState.Chasing);
            isInCombat = true;
            
            // 전투 시작 포효
            if (animController != null)
                animController.PlayRoar();
                
            // 전투 BGM 시작
            if (VolumeManager.Instance != null)
                VolumeManager.Instance.PlayBossBattleBGM();
        }
    }
    
    void HandleChasingState(float distanceToPlayer)
    {
        // 이동 처리는 NecromancerMovement에서 담당
        if (movement != null)
            movement.ChasePlayer(player.position);
        
        // 공격 범위 체크
        if (distanceToPlayer <= attackRange)
        {
            SetState(BossState.Attacking);
        }
        else if (distanceToPlayer <= specialAttackRange && Random.Range(0f, 1f) < 0.3f)
        {
            SetState(BossState.Casting);
        }
    }
    
    void HandleAttackingState(float distanceToPlayer)
    {
        // 전투 처리는 NecromancerCombatSystem에서 담당
        if (combatSystem != null && combatSystem.CanAttack())
        {
            combatSystem.PerformAttack();
        }
        
        // 공격 범위를 벗어나면 추격 재개
        if (distanceToPlayer > attackRange * 1.5f)
        {
            SetState(BossState.Chasing);
        }
    }
    
    void HandleCastingState(float distanceToPlayer)
    {
        // 마법 시전 처리
        if (combatSystem != null && combatSystem.CanCast())
        {
            combatSystem.PerformSpellCast();
        }
        
        // 시전 완료 후 상태 전환은 애니메이션 이벤트에서 처리
    }
    
    void HandleHitState()
    {
        // 피격 상태는 애니메이션 완료 후 자동으로 이전 상태로 복귀
        // NecromancerAnimationController에서 처리
    }
    
    public void SetState(BossState newState)
    {
        if (currentState == newState) return;
        
        BossState previousState = currentState;
        currentState = newState;
        
        // 애니메이션 컨트롤러에 상태 변경 알림
        if (animController != null)
            animController.OnStateChanged(newState, previousState);
            
        DebugLog($"상태 변경: {previousState} → {newState}");
    }
    
    // 이벤트 핸들러들
    void OnHealthChanged(float currentHealth, float maxHealth)
    {
        float healthPercent = currentHealth / maxHealth;
        
        // 체력에 따른 행동 변화 (필요시)
        if (healthPercent < 0.3f && !isInCombat)
        {
            // 저체력 시 더 공격적으로
            detectionRange *= 1.5f;
        }
    }
    
    void OnHit(float damage, Vector3 hitPosition)
    {
        if (currentState != BossState.Hit && currentState != BossState.Dead)
        {
            SetState(BossState.Hit);
            
            // VFX 효과
            if (vfxController != null)
                vfxController.PlayHitEffect(hitPosition);
        }
    }
    
    void OnDeath()
    {
        isDead = true;
        isInCombat = false;
        SetState(BossState.Dead);
        
        // 사망 처리
        if (animController != null)
            animController.PlayDeath();
            
        // 승리 BGM
        if (VolumeManager.Instance != null)
            VolumeManager.Instance.PlayVictoryBGM();
            
        // 게임 진행 알림
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.NotifyBossDefeated();
            
        OnBossDefeated?.Invoke();
        
        DebugLog($"{bossName} 사망!");
    }
    
    // 외부에서 호출 가능한 메서드들
    public void TakeDamage(float damage, Vector3 attackPosition)
    {
        if (healthSystem != null)
            healthSystem.TakeDamage(damage, attackPosition);
    }
    
    public void OnAttackComplete()
    {
        // 공격 완료 후 추격 상태로 복귀
        if (currentState == BossState.Attacking)
            SetState(BossState.Chasing);
    }
    
    public void OnCastComplete()
    {
        // 시전 완료 후 추격 상태로 복귀
        if (currentState == BossState.Casting)
            SetState(BossState.Chasing);
    }
    
    public void OnHitComplete()
    {
        // 피격 완료 후 이전 상태로 복귀
        if (currentState == BossState.Hit)
            SetState(isInCombat ? BossState.Chasing : BossState.Idle);
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[{bossName}] {message}");
    }
    
    void OnDestroy()
    {
        // 이벤트 해제
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthChanged;
            healthSystem.OnDeath -= OnDeath;
            healthSystem.OnHit -= OnHit;
        }
    }
} 