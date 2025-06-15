using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VR Horror Game 전용 통합 사운드 관리 시스템
/// BGM, SFX, 3D 공간 사운드, 상황별 음악 전환을 모두 관리
/// </summary>
public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance { get; private set; }

    [Header("=== Audio Mixer ===")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup spatialSfxMixerGroup; // 3D 공간 사운드용

    [Header("=== BGM System ===")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource ambientAudioSource; // 환경음용
    
    [Header("=== SFX System ===")]
    [SerializeField] private AudioSource[] sfxAudioSources = new AudioSource[8]; // SFX 풀링
    [SerializeField] private int maxSpatialSources = 16; // 3D 사운드 최대 개수
    private Queue<AudioSource> availableSfxSources = new Queue<AudioSource>();
    private List<AudioSource> spatialAudioSources = new List<AudioSource>();

    [Header("=== Volume Settings ===")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Range(0f, 1f)] public float spatialSfxVolume = 0.9f;

    [Header("=== BGM Clips ===")]
    [SerializeField] private BGMClipData[] bgmClips;
    
    [Header("=== SFX Clips ===")]
    [SerializeField] private SFXClipData[] sfxClips;

    [Header("=== Transition Settings ===")]
    [SerializeField] private float bgmFadeTime = 2f;
    [SerializeField] private float ambientFadeTime = 1.5f;

    // 현재 상태
    private BGMType currentBGMType = BGMType.None;
    private BGMType previousBGMType = BGMType.None;
    private bool isBGMTransitioning = false;
    private Dictionary<string, AudioClip> sfxClipDict = new Dictionary<string, AudioClip>();
    private Dictionary<BGMType, AudioClip> bgmClipDict = new Dictionary<BGMType, AudioClip>();

    // VR 관련
    private Transform vrCameraTransform;
    private VRPlayerHealth playerHealth;

    #region Enums and Data Classes

    public enum BGMType
    {
        None,
        MainMenu,
        Exploration,    // 평상시 탐험
        Tension,        // 긴장감 (적 근처)
        Combat,         // 일반 전투
        BossBattle,     // 보스전
        Victory,        // 승리
        GameOver,       // 게임 오버
        Horror          // 공포 상황
    }

    public enum SFXType
    {
        // Player SFX
        PlayerDamage,
        PlayerHeal,
        PlayerDeath,
        PlayerHeartbeat,
        PlayerBreathing,
        
        // Boss SFX
        BossIntro,
        BossAttack,
        BossHeavyAttack,
        BossChargeAttack,
        BossAreaAttack,
        BossPhaseTransition,
        BossDeath,
        BossRage,
        
        // Enemy SFX
        EnemyAttack,
        EnemyDeath,
        EnemySpotPlayer,
        EnemyFootsteps,
        
        // Weapon SFX
        AxeSwing,
        AxeHit,
        AxeEquip,
        AxeUnequip,
        
        // Environment SFX
        DoorOpen,
        DoorClose,
        ItemPickup,
        ButtonClick,
        
        // Horror SFX
        ScreamDistant,
        WhisperClose,
        CreepyAmbient,
        JumpScare
    }

    [System.Serializable]
    public class BGMClipData
    {
        public BGMType bgmType;
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume = 0.7f;
        public bool loop = true;
        public bool isAmbient = false; // 환경음인지 여부
    }

    [System.Serializable]
    public class SFXClipData
    {
        public SFXType sfxType;
        public AudioClip[] audioClips; // 랜덤 재생용 배열
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitchMin = 0.9f;
        [Range(0.5f, 2f)] public float pitchMax = 1.1f;
        public bool is3D = false; // 3D 공간 사운드 여부
        public float maxDistance = 20f; // 3D 사운드 최대 거리
    }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // Singleton 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeVolumeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindVRReferences();
        LoadVolumeSettings();
        
        // 기본 BGM 시작 (탐험 모드)
        PlayBGM(BGMType.Exploration);
    }

    void Update()
    {
        // 플레이어 체력에 따른 자동 BGM 전환
        AutoBGMTransition();
        
        // 사용하지 않는 3D 오디오 소스 정리
        CleanupInactiveSpatialSources();
    }

    #endregion

    #region Initialization

    void InitializeVolumeManager()
    {
        // BGM AudioSource 설정
        if (bgmAudioSource == null)
        {
            GameObject bgmObj = new GameObject("BGM_AudioSource");
            bgmObj.transform.SetParent(transform);
            bgmAudioSource = bgmObj.AddComponent<AudioSource>();
        }
        
        bgmAudioSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;

        // Ambient AudioSource 설정
        if (ambientAudioSource == null)
        {
            GameObject ambientObj = new GameObject("Ambient_AudioSource");
            ambientObj.transform.SetParent(transform);
            ambientAudioSource = ambientObj.AddComponent<AudioSource>();
        }
        
        ambientAudioSource.outputAudioMixerGroup = bgmMixerGroup;
        ambientAudioSource.loop = true;
        ambientAudioSource.playOnAwake = false;

        // SFX AudioSource 풀 생성
        for (int i = 0; i < sfxAudioSources.Length; i++)
        {
            if (sfxAudioSources[i] == null)
            {
                GameObject sfxObj = new GameObject($"SFX_AudioSource_{i}");
                sfxObj.transform.SetParent(transform);
                sfxAudioSources[i] = sfxObj.AddComponent<AudioSource>();
            }
            
            sfxAudioSources[i].outputAudioMixerGroup = sfxMixerGroup;
            sfxAudioSources[i].playOnAwake = false;
            availableSfxSources.Enqueue(sfxAudioSources[i]);
        }

        // 딕셔너리 초기화
        InitializeClipDictionaries();

        Debug.Log("[VolumeManager] 초기화 완료!");
    }

    void InitializeClipDictionaries()
    {
        // BGM 딕셔너리 생성
        bgmClipDict.Clear();
        foreach (var bgmData in bgmClips)
        {
            if (bgmData.audioClip != null)
            {
                bgmClipDict[bgmData.bgmType] = bgmData.audioClip;
            }
        }

        // SFX 딕셔너리 생성
        sfxClipDict.Clear();
        foreach (var sfxData in sfxClips)
        {
            if (sfxData.audioClips != null && sfxData.audioClips.Length > 0)
            {
                // 첫 번째 클립을 기본으로 저장 (랜덤 재생은 별도 처리)
                sfxClipDict[sfxData.sfxType.ToString()] = sfxData.audioClips[0];
            }
        }
    }

    void FindVRReferences()
    {
        // VR 카메라 찾기
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {
            vrCameraTransform = cameraRig.centerEyeAnchor;
        }

        // 플레이어 체력 시스템 찾기
        playerHealth = FindFirstObjectByType<VRPlayerHealth>();
    }

    #endregion

    #region BGM Management

    public void PlayBGM(BGMType bgmType, bool forceRestart = false)
    {
        if (currentBGMType == bgmType && !forceRestart) return;
        if (isBGMTransitioning) return;

        BGMClipData bgmData = GetBGMData(bgmType);
        if (bgmData == null || bgmData.audioClip == null)
        {
            Debug.LogWarning($"[VolumeManager] BGM 클립을 찾을 수 없음: {bgmType}");
            return;
        }

        StartCoroutine(TransitionBGM(bgmData));
    }

    public void StopBGM()
    {
        if (isBGMTransitioning) return;
        StartCoroutine(FadeOutBGM());
    }

    IEnumerator TransitionBGM(BGMClipData newBGMData)
    {
        isBGMTransitioning = true;
        previousBGMType = currentBGMType;
        currentBGMType = newBGMData.bgmType;

        AudioSource targetSource = newBGMData.isAmbient ? ambientAudioSource : bgmAudioSource;
        AudioSource otherSource = newBGMData.isAmbient ? bgmAudioSource : ambientAudioSource;

        // 기존 BGM 페이드 아웃
        if (targetSource.isPlaying)
        {
            yield return StartCoroutine(FadeAudioSource(targetSource, 0f, bgmFadeTime));
        }

        // 새 BGM 설정 및 페이드 인
        targetSource.clip = newBGMData.audioClip;
        targetSource.loop = newBGMData.loop;
        targetSource.volume = 0f;
        targetSource.Play();

        yield return StartCoroutine(FadeAudioSource(targetSource, newBGMData.volume * bgmVolume, bgmFadeTime));

        // 다른 소스가 재생 중이면 정지
        if (otherSource.isPlaying)
        {
            yield return StartCoroutine(FadeAudioSource(otherSource, 0f, bgmFadeTime));
            otherSource.Stop();
        }

        isBGMTransitioning = false;
        Debug.Log($"[VolumeManager] BGM 전환 완료: {currentBGMType}");
    }

    IEnumerator FadeOutBGM()
    {
        isBGMTransitioning = true;

        if (bgmAudioSource.isPlaying)
        {
            yield return StartCoroutine(FadeAudioSource(bgmAudioSource, 0f, bgmFadeTime));
            bgmAudioSource.Stop();
        }

        if (ambientAudioSource.isPlaying)
        {
            yield return StartCoroutine(FadeAudioSource(ambientAudioSource, 0f, ambientFadeTime));
            ambientAudioSource.Stop();
        }

        currentBGMType = BGMType.None;
        isBGMTransitioning = false;
    }

    IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float fadeTime)
    {
        float startVolume = source.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeTime);
            yield return null;
        }

        source.volume = targetVolume;
    }

    void AutoBGMTransition()
    {
        if (playerHealth == null || isBGMTransitioning) return;

        BGMType targetBGM = currentBGMType;

        // 플레이어 체력에 따른 BGM 전환
        if (playerHealth.IsDead)
        {
            targetBGM = BGMType.GameOver;
        }
        else if (playerHealth.HealthPercentage <= 0.25f)
        {
            targetBGM = BGMType.Horror; // 체력 위험 시 공포 BGM
        }
        else if (IsInCombat())
        {
            targetBGM = BGMType.Combat;
        }
        else if (IsNearEnemy())
        {
            targetBGM = BGMType.Tension;
        }
        else
        {
            targetBGM = BGMType.Exploration;
        }

        if (targetBGM != currentBGMType)
        {
            PlayBGM(targetBGM);
        }
    }

    #endregion

    #region SFX Management

    public void PlaySFX(SFXType sfxType, Vector3? position = null, Transform followTarget = null)
    {
        SFXClipData sfxData = GetSFXData(sfxType);
        if (sfxData == null || sfxData.audioClips == null || sfxData.audioClips.Length == 0)
        {
            Debug.LogWarning($"[VolumeManager] SFX 클립을 찾을 수 없음: {sfxType}");
            return;
        }

        // 랜덤 클립 선택
        AudioClip clipToPlay = sfxData.audioClips[Random.Range(0, sfxData.audioClips.Length)];

        if (sfxData.is3D && position.HasValue)
        {
            PlaySpatialSFX(clipToPlay, sfxData, position.Value, followTarget);
        }
        else
        {
            PlayRegularSFX(clipToPlay, sfxData);
        }
    }

    void PlayRegularSFX(AudioClip clip, SFXClipData sfxData)
    {
        AudioSource source = GetAvailableSFXSource();
        if (source == null)
        {
            Debug.LogWarning("[VolumeManager] 사용 가능한 SFX AudioSource가 없습니다!");
            return;
        }

        source.clip = clip;
        source.volume = sfxData.volume * sfxVolume;
        source.pitch = Random.Range(sfxData.pitchMin, sfxData.pitchMax);
        source.spatialBlend = 0f; // 2D 사운드
        source.Play();

        StartCoroutine(ReturnSFXSourceAfterPlay(source));
    }

    void PlaySpatialSFX(AudioClip clip, SFXClipData sfxData, Vector3 position, Transform followTarget = null)
    {
        AudioSource spatialSource = CreateSpatialAudioSource();
        if (spatialSource == null) return;

        spatialSource.transform.position = position;
        spatialSource.clip = clip;
        spatialSource.volume = sfxData.volume * spatialSfxVolume;
        spatialSource.pitch = Random.Range(sfxData.pitchMin, sfxData.pitchMax);
        spatialSource.spatialBlend = 1f; // 3D 사운드
        spatialSource.maxDistance = sfxData.maxDistance;
        spatialSource.rolloffMode = AudioRolloffMode.Logarithmic;
        spatialSource.Play();

        if (followTarget != null)
        {
            StartCoroutine(FollowTargetCoroutine(spatialSource, followTarget));
        }

        StartCoroutine(DestroySpatialSourceAfterPlay(spatialSource));
    }

    AudioSource GetAvailableSFXSource()
    {
        if (availableSfxSources.Count > 0)
        {
            return availableSfxSources.Dequeue();
        }

        // 모든 소스가 사용 중인 경우, 가장 오래된 것을 찾아서 재사용
        foreach (var source in sfxAudioSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        return null;
    }

    AudioSource CreateSpatialAudioSource()
    {
        if (spatialAudioSources.Count >= maxSpatialSources)
        {
            // 가장 오래된 3D 사운드 제거
            AudioSource oldestSource = spatialAudioSources[0];
            spatialAudioSources.RemoveAt(0);
            Destroy(oldestSource.gameObject);
        }

        GameObject spatialObj = new GameObject("Spatial_SFX");
        AudioSource spatialSource = spatialObj.AddComponent<AudioSource>();
        spatialSource.outputAudioMixerGroup = spatialSfxMixerGroup;
        spatialSource.playOnAwake = false;

        spatialAudioSources.Add(spatialSource);
        return spatialSource;
    }

    IEnumerator ReturnSFXSourceAfterPlay(AudioSource source)
    {
        yield return new WaitWhile(() => source.isPlaying);
        availableSfxSources.Enqueue(source);
    }

    IEnumerator DestroySpatialSourceAfterPlay(AudioSource source)
    {
        yield return new WaitWhile(() => source.isPlaying);
        spatialAudioSources.Remove(source);
        if (source != null)
        {
            Destroy(source.gameObject);
        }
    }

    IEnumerator FollowTargetCoroutine(AudioSource source, Transform target)
    {
        while (source != null && target != null && source.isPlaying)
        {
            source.transform.position = target.position;
            yield return null;
        }
    }

    void CleanupInactiveSpatialSources()
    {
        for (int i = spatialAudioSources.Count - 1; i >= 0; i--)
        {
            if (spatialAudioSources[i] == null || !spatialAudioSources[i].isPlaying)
            {
                if (spatialAudioSources[i] != null)
                {
                    Destroy(spatialAudioSources[i].gameObject);
                }
                spatialAudioSources.RemoveAt(i);
            }
        }
    }

    #endregion

    #region Volume Control

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (masterMixerGroup != null)
        {
            masterMixerGroup.audioMixer.SetFloat("MasterVolume", VolumeToDecibel(masterVolume));
        }
        SaveVolumeSettings();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmMixerGroup != null)
        {
            bgmMixerGroup.audioMixer.SetFloat("BGMVolume", VolumeToDecibel(bgmVolume));
        }
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxMixerGroup != null)
        {
            sfxMixerGroup.audioMixer.SetFloat("SFXVolume", VolumeToDecibel(sfxVolume));
        }
        SaveVolumeSettings();
    }

    public void SetSpatialSFXVolume(float volume)
    {
        spatialSfxVolume = Mathf.Clamp01(volume);
        if (spatialSfxMixerGroup != null)
        {
            spatialSfxMixerGroup.audioMixer.SetFloat("SpatialSFXVolume", VolumeToDecibel(spatialSfxVolume));
        }
        SaveVolumeSettings();
    }

    float VolumeToDecibel(float volume)
    {
        return volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
    }

    #endregion

    #region Special BGM Controls

    public void PlayBossBattleBGM()
    {
        PlayBGM(BGMType.BossBattle);
    }

    public void PlayVictoryBGM()
    {
        PlayBGM(BGMType.Victory);
    }

    public void PlayGameOverBGM()
    {
        PlayBGM(BGMType.GameOver);
    }

    public void ReturnToExplorationBGM()
    {
        PlayBGM(BGMType.Exploration);
    }

    #endregion

    #region Helper Methods

    BGMClipData GetBGMData(BGMType bgmType)
    {
        foreach (var bgmData in bgmClips)
        {
            if (bgmData.bgmType == bgmType)
            {
                return bgmData;
            }
        }
        return null;
    }

    SFXClipData GetSFXData(SFXType sfxType)
    {
        foreach (var sfxData in sfxClips)
        {
            if (sfxData.sfxType == sfxType)
            {
                return sfxData;
            }
        }
        return null;
    }

    bool IsInCombat()
    {
        // Boss나 Enemy가 플레이어를 공격 중인지 확인
        BossAI boss = FindFirstObjectByType<BossAI>();
        if (boss != null && !boss.IsDead)
        {
            float distanceToBoss = Vector3.Distance(vrCameraTransform.position, boss.transform.position);
            if (distanceToBoss <= 10f) // 보스와 가까우면 전투 상태
            {
                return true;
            }
        }

        // Enemy 확인 (간단한 거리 체크)
        EnemyAttackSystem[] enemies = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                float distanceToEnemy = Vector3.Distance(vrCameraTransform.position, enemy.transform.position);
                if (distanceToEnemy <= 5f) // Enemy와 가까우면 전투 상태
                {
                    return true;
                }
            }
        }

        return false;
    }

    bool IsNearEnemy()
    {
        if (vrCameraTransform == null) return false;

        // Enemy 근처에 있는지 확인 (긴장감 BGM용)
        EnemyAttackSystem[] enemies = FindObjectsByType<EnemyAttackSystem>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                float distanceToEnemy = Vector3.Distance(vrCameraTransform.position, enemy.transform.position);
                if (distanceToEnemy <= 15f) // 15m 이내면 긴장 상태
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Save/Load Settings

    void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("SpatialSFXVolume", spatialSfxVolume);
        PlayerPrefs.Save();
    }

    void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        spatialSfxVolume = PlayerPrefs.GetFloat("SpatialSFXVolume", 0.9f);

        // 로드된 설정 적용
        SetMasterVolume(masterVolume);
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
        SetSpatialSFXVolume(spatialSfxVolume);
    }

    #endregion

    #region Public Properties

    public BGMType CurrentBGMType => currentBGMType;
    public bool IsBGMTransitioning => isBGMTransitioning;
    public float MasterVolume => masterVolume;
    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;
    public float SpatialSFXVolume => spatialSfxVolume;

    #endregion
}