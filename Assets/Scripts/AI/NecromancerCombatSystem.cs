using UnityEngine;

/// <summary>
/// 네크로맨서 전투 시스템
/// 공격, 마법 시전, 데미지 처리만 담당
/// </summary>
public class NecromancerCombatSystem : MonoBehaviour
{
    [Header("=== 공격 설정 ===")]
    public float meleeAttackDamage = 30f;
    public float spellAttackDamage = 50f;
    public float attackCooldown = 2f;
    public float spellCooldown = 4f;
    
    [Header("=== 공격 범위 ===")]
    public float meleeAttackRange = 3f;
    public float spellAttackRange = 8f;
    public float attackAngle = 60f; // 공격 각도
    
    [Header("=== VR 상호작용 ===")]
    public LayerMask playerLayer = 1 << 0; // Player 레이어
    public string axeTag = "Axe"; // VR 도끼 태그
    
    [Header("=== 이펙트 ===")]
    public GameObject meleeHitEffect;
    public GameObject spellCastEffect;
    public GameObject spellHitEffect;
    
    // 사운드는 VolumeManager에서 처리
    
    // 컴포넌트 참조
    private NecromancerBoss bossController;
    private NecromancerAnimationController animController;
    
    // 전투 상태
    private float lastAttackTime = 0f;
    private float lastSpellTime = 0f;
    private bool isAttacking = false;
    private bool isCasting = false;
    
    // VR 플레이어 참조
    private Transform player;
    private VRPlayerHealth playerHealth;
    
    public void Initialize(NecromancerBoss boss)
    {
        bossController = boss;
        animController = GetComponent<NecromancerAnimationController>();
        
        // AudioSource 제거됨 - VolumeManager 사용
        
        // 플레이어 참조 가져오기
        if (bossController != null && bossController.Player != null)
        {
            player = bossController.Player;
            playerHealth = player.GetComponent<VRPlayerHealth>();
        }
        
        // 애니메이션 이벤트 연결
        if (animController != null)
        {
            animController.OnAttackHit += OnMeleeAttackHit;
        }
        
        // 애니메이션 이벤트가 없으면 타이머로 대체
        StartCoroutine(CheckForAnimationEvents());
        
        CombatLog("전투 시스템 초기화 완료");
    }
    
    #region 공격 가능 여부 체크
    
    public bool CanAttack()
    {
        return !isAttacking && 
               !isCasting && 
               Time.time - lastAttackTime >= attackCooldown &&
               IsPlayerInMeleeRange();
    }
    
    public bool CanCast()
    {
        return !isAttacking && 
               !isCasting && 
               Time.time - lastSpellTime >= spellCooldown &&
               IsPlayerInSpellRange();
    }
    
    private bool IsPlayerInMeleeRange()
    {
        if (player == null) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= meleeAttackRange && IsPlayerInAttackAngle();
    }
    
    private bool IsPlayerInSpellRange()
    {
        if (player == null) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= spellAttackRange;
    }
    
    private bool IsPlayerInAttackAngle()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 forward = transform.forward;
        
        float angle = Vector3.Angle(forward, directionToPlayer);
        return angle <= attackAngle * 0.5f;
    }
    
    #endregion
    
    #region 공격 실행
    
    public void PerformAttack()
    {
        if (!CanAttack()) return;
        
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // 플레이어 방향으로 회전
        LookAtPlayer();
        
        // VolumeManager로 공격 사운드 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossAttack, transform.position);
        }
        
        CombatLog("근접 공격 시작");
        
        // 애니메이션은 NecromancerAnimationController에서 처리
        // 실제 데미지는 OnMeleeAttackHit에서 처리
    }
    
    public void PerformSpellCast()
    {
        if (!CanCast()) return;
        
        isCasting = true;
        lastSpellTime = Time.time;
        
        // 플레이어 방향으로 회전
        LookAtPlayer();
        
        // VolumeManager로 시전 사운드 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossChargeAttack, transform.position);
        }
        
        // 시전 이펙트 생성
        if (spellCastEffect != null)
        {
            GameObject effect = Instantiate(spellCastEffect, transform.position + Vector3.up * 2f, transform.rotation);
            Destroy(effect, 3f);
        }
        
        CombatLog("마법 시전 시작");
        
        // 실제 마법 발사는 애니메이션 이벤트에서 처리
        Invoke(nameof(CastSpellProjectile), 1f); // 시전 딜레이
    }
    
    #endregion
    
    #region 공격 히트 처리
    
    // 애니메이션 이벤트에서 호출 (Enemy와 동일한 이름)
    public void OnAttack1Hit()
    {
        OnMeleeAttackHit();
    }
    
    // 실제 공격 처리
    private void OnMeleeAttackHit()
    {
        if (player == null || !IsPlayerInMeleeRange()) return;
        
        // 플레이어에게 데미지
        if (playerHealth != null)
        {
            // VRPlayerHealth는 float 인자만 받음
            playerHealth.TakeDamage(meleeAttackDamage);
        }
        
        // 히트 이펙트
        if (meleeHitEffect != null)
        {
            Vector3 hitPosition = player.position + Vector3.up * 1f;
            GameObject effect = Instantiate(meleeHitEffect, hitPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // VolumeManager로 히트 사운드 재생
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.PlaySFX(VolumeManager.SFXType.BossHeavyAttack, transform.position);
        }
        
        CombatLog($"플레이어에게 {meleeAttackDamage} 데미지!");
    }
    
    private void CastSpellProjectile()
    {
        if (player == null) return;
        
        // 마법 투사체 생성 (간단한 구현)
        Vector3 spellDirection = (player.position - transform.position).normalized;
        Vector3 spellStartPos = transform.position + Vector3.up * 1.5f + transform.forward * 1f;
        
        // 레이캐스트로 즉시 히트 처리 (간단한 구현)
        RaycastHit hit;
        if (Physics.Raycast(spellStartPos, spellDirection, out hit, spellAttackRange, playerLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                // 플레이어에게 마법 데미지
                if (playerHealth != null)
                {
                    // VRPlayerHealth는 float 인자만 받음
                    playerHealth.TakeDamage(spellAttackDamage);
                }
                
                // 마법 히트 이펙트
                if (spellHitEffect != null)
                {
                    GameObject effect = Instantiate(spellHitEffect, hit.point, Quaternion.identity);
                    Destroy(effect, 2f);
                }
                
                CombatLog($"마법으로 플레이어에게 {spellAttackDamage} 데미지!");
            }
        }
        
        isCasting = false;
    }
    
    #endregion
    
    #region VR 도끼 충돌 처리
    
    void OnTriggerEnter(Collider other)
    {
        // VR 도끼와의 충돌 감지 (태그 또는 AxeWeapon 컴포넌트로 확인)
        if (other.CompareTag(axeTag) || other.GetComponent<AxeWeapon>() != null)
        {
            HandleAxeHit(other);
        }
    }
    
    private void HandleAxeHit(Collider axeCollider)
    {
        // 도끼 컴포넌트 가져오기
        AxeWeapon axeWeapon = axeCollider.GetComponent<AxeWeapon>();
        if (axeWeapon == null) return;
        
        // 기존 AxeWeapon의 baseDamage 사용
        float damage = axeWeapon.baseDamage;
        Vector3 hitPosition = axeCollider.ClosestPoint(transform.position);
        
        // 보스에게 데미지 적용
        if (bossController != null)
        {
            bossController.TakeDamage(damage, hitPosition);
        }
        
        // 기존 AxeWeapon 시스템과 호환되는 피드백
        // 햅틱 피드백 직접 처리
        OVRInput.SetControllerVibration(0.8f, 0.8f, axeWeapon.controllerType);
        
        // 0.2초 후 진동 정지
        StartCoroutine(StopVibrationAfterDelay(axeWeapon.controllerType, 0.2f));
        
        CombatLog($"도끼로부터 {damage} 데미지 받음!");
    }
    
    private System.Collections.IEnumerator StopVibrationAfterDelay(OVRInput.Controller controller, float delay)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
    
    #endregion
    
    #region 유틸리티
    
    private void LookAtPlayer()
    {
        if (player == null) return;
        
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Y축 회전만
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }
    
    // 사운드는 VolumeManager에서 처리
    
    public void OnAttackComplete()
    {
        isAttacking = false;
        CombatLog("공격 완료");
    }
    
    public void OnCastComplete()
    {
        isCasting = false;
        CombatLog("시전 완료");
    }
    
    private void CombatLog(string message)
    {
        Debug.Log($"[NecromancerCombat] {message}");
    }
    
    #endregion
    
    #region 디버그 기즈모
    
    void OnDrawGizmosSelected()
    {
        // 근접 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
        
        // 마법 공격 범위
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spellAttackRange);
        
        // 공격 각도
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward * meleeAttackRange;
        Vector3 rightBoundary = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward * meleeAttackRange;
        
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
    
    #endregion
    
    #region 애니메이션 이벤트 대체 시스템
    
    private System.Collections.IEnumerator CheckForAnimationEvents()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            
            // 공격 중이고 애니메이션이 특정 시점에 도달했을 때 히트 처리
            if (isAttacking && animController != null)
            {
                // 공격 애니메이션의 중간 지점에서 히트 처리
                AnimatorStateInfo stateInfo = animController.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
                
                // atack1 또는 atack2 애니메이션이 50% 진행되었을 때
                if ((stateInfo.IsName("atack1") || stateInfo.IsName("atack2")) && 
                    stateInfo.normalizedTime >= 0.5f && stateInfo.normalizedTime <= 0.6f)
                {
                    OnMeleeAttackHit();
                    yield return new WaitForSeconds(1f); // 중복 호출 방지
                }
            }
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        // 이벤트 해제
        if (animController != null)
        {
            animController.OnAttackHit -= OnMeleeAttackHit;
        }
        
        // Invoke 취소
        CancelInvoke();
    }
} 