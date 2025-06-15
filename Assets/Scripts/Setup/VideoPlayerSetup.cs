#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;

/// <summary>
/// VideoPlayer UI 자동 생성 도구
/// Canvas + RawImage + VideoPlayer + AudioSource를 자동으로 설정
/// </summary>
public class VideoPlayerSetup : MonoBehaviour
{
    [MenuItem("VR Horror Game/Setup/Create VideoPlayer UI")]
    public static void CreateVideoPlayerUI()
    {
        Debug.Log("[VideoPlayerSetup] VideoPlayer UI 생성 시작...");
        
        if (CreateVideoPlayerSystem())
        {
            Debug.Log("[VideoPlayerSetup] VideoPlayer UI 생성 완료!");
            EditorUtility.DisplayDialog("VideoPlayer Setup", "VideoPlayer UI가 성공적으로 생성되었습니다!", "확인");
        }
        else
        {
            Debug.LogError("[VideoPlayerSetup] VideoPlayer UI 생성 실패!");
            EditorUtility.DisplayDialog("VideoPlayer Setup", "VideoPlayer UI 생성에 실패했습니다.", "확인");
        }
    }
    
    [MenuItem("VR Horror Game/Setup/Setup CinematicManager VideoPlayer")]
    public static void SetupCinematicManagerVideoPlayer()
    {
        Debug.Log("[VideoPlayerSetup] CinematicManager VideoPlayer 연결 시작...");
        
        CinematicManager cinematicManager = FindFirstObjectByType<CinematicManager>();
        if (cinematicManager == null)
        {
            Debug.LogError("[VideoPlayerSetup] CinematicManager를 찾을 수 없습니다!");
            EditorUtility.DisplayDialog("VideoPlayer Setup", "CinematicManager를 먼저 생성해주세요.", "확인");
            return;
        }
        
        if (SetupCinematicManagerComponents(cinematicManager))
        {
            Debug.Log("[VideoPlayerSetup] CinematicManager VideoPlayer 연결 완료!");
            EditorUtility.DisplayDialog("VideoPlayer Setup", "CinematicManager에 VideoPlayer가 성공적으로 연결되었습니다!", "확인");
        }
        else
        {
            Debug.LogError("[VideoPlayerSetup] CinematicManager VideoPlayer 연결 실패!");
        }
    }
    
    static bool CreateVideoPlayerSystem()
    {
        try
        {
            // 1. Canvas 생성
            GameObject canvasObj = CreateVideoCanvas();
            if (canvasObj == null) return false;
            
            // 2. RawImage (Video Screen) 생성
            GameObject rawImageObj = CreateVideoScreen(canvasObj.transform);
            if (rawImageObj == null) return false;
            
            // 3. VideoPlayer 오브젝트 생성
            GameObject videoPlayerObj = CreateVideoPlayerObject(canvasObj.transform);
            if (videoPlayerObj == null) return false;
            
            // 4. AudioSource 추가
            AudioSource audioSource = videoPlayerObj.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = videoPlayerObj.AddComponent<AudioSource>();
            }
            
            // 5. VideoPlayer 설정
            VideoPlayer videoPlayer = videoPlayerObj.GetComponent<VideoPlayer>();
            RawImage rawImage = rawImageObj.GetComponent<RawImage>();
            
            SetupVideoPlayerComponents(videoPlayer, rawImage, audioSource);
            
            // 6. 변경사항 저장
            EditorUtility.SetDirty(canvasObj);
            EditorUtility.SetDirty(rawImageObj);
            EditorUtility.SetDirty(videoPlayerObj);
            
            Debug.Log("[VideoPlayerSetup] VideoPlayer 시스템 생성 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VideoPlayerSetup] VideoPlayer 시스템 생성 실패: {e.Message}");
            return false;
        }
    }
    
    static GameObject CreateVideoCanvas()
    {
        // 기존 VideoCanvas 확인
        Canvas existingCanvas = FindCanvasByName("VideoCanvas");
        if (existingCanvas != null)
        {
            Debug.Log("[VideoPlayerSetup] VideoCanvas가 이미 존재합니다.");
            return existingCanvas.gameObject;
        }
        
        // Canvas 생성
        GameObject canvasObj = new GameObject("VideoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        GraphicRaycaster graphicRaycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        // Canvas 설정
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 다른 UI보다 위에 표시
        
        // CanvasScaler 설정
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        
        // 초기에는 비활성화
        canvasObj.SetActive(false);
        
        Debug.Log("[VideoPlayerSetup] VideoCanvas 생성 완료");
        return canvasObj;
    }
    
    static GameObject CreateVideoScreen(Transform parent)
    {
        // 기존 VideoScreen 확인
        Transform existingScreen = parent.Find("VideoScreen");
        if (existingScreen != null)
        {
            Debug.Log("[VideoPlayerSetup] VideoScreen이 이미 존재합니다.");
            return existingScreen.gameObject;
        }
        
        // RawImage 생성
        GameObject rawImageObj = new GameObject("VideoScreen");
        rawImageObj.transform.SetParent(parent);
        
        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
        
        // RectTransform 설정 (전체 화면)
        RectTransform rectTransform = rawImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        
        // 검은색 배경
        rawImage.color = Color.black;
        
        Debug.Log("[VideoPlayerSetup] VideoScreen 생성 완료");
        return rawImageObj;
    }
    
    static GameObject CreateVideoPlayerObject(Transform parent)
    {
        // 기존 VideoPlayer 확인
        Transform existingPlayer = parent.Find("VideoPlayer");
        if (existingPlayer != null)
        {
            Debug.Log("[VideoPlayerSetup] VideoPlayer가 이미 존재합니다.");
            return existingPlayer.gameObject;
        }
        
        // VideoPlayer 오브젝트 생성
        GameObject videoPlayerObj = new GameObject("VideoPlayer");
        videoPlayerObj.transform.SetParent(parent);
        
        // VideoPlayer 컴포넌트 추가
        VideoPlayer videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();
        
        Debug.Log("[VideoPlayerSetup] VideoPlayer 오브젝트 생성 완료");
        return videoPlayerObj;
    }
    
    static void SetupVideoPlayerComponents(VideoPlayer videoPlayer, RawImage rawImage, AudioSource audioSource)
    {
        // VideoPlayer 기본 설정
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // RenderTexture 생성
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
        renderTexture.name = "VideoRenderTexture";
        
        // VideoPlayer에 RenderTexture 할당
        videoPlayer.targetTexture = renderTexture;
        
        // RawImage에 RenderTexture 할당
        rawImage.texture = renderTexture;
        
        // AudioSource 설정
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);
        
        // AudioSource 기본 설정
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
        audioSource.spatialBlend = 0f; // 2D 사운드
        
        Debug.Log("[VideoPlayerSetup] VideoPlayer 컴포넌트 설정 완료");
    }
    
    static bool SetupCinematicManagerComponents(CinematicManager cinematicManager)
    {
        try
        {
            // VideoCanvas 찾기
            Canvas videoCanvas = FindCanvasByName("VideoCanvas");
            if (videoCanvas == null)
            {
                Debug.LogError("[VideoPlayerSetup] VideoCanvas를 찾을 수 없습니다. 먼저 VideoPlayer UI를 생성해주세요.");
                return false;
            }
            
            // VideoPlayer 찾기
            VideoPlayer videoPlayer = videoCanvas.GetComponentInChildren<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError("[VideoPlayerSetup] VideoPlayer를 찾을 수 없습니다.");
                return false;
            }
            
            // RawImage 찾기
            RawImage rawImage = videoCanvas.GetComponentInChildren<RawImage>();
            if (rawImage == null)
            {
                Debug.LogError("[VideoPlayerSetup] RawImage를 찾을 수 없습니다.");
                return false;
            }
            
            // AudioSource 찾기
            AudioSource audioSource = videoPlayer.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("[VideoPlayerSetup] AudioSource를 찾을 수 없습니다.");
                return false;
            }
            
            // CinematicManager에 컴포넌트 할당 (리플렉션 사용)
            var cinematicManagerType = typeof(CinematicManager);
            
            var videoPlayerField = cinematicManagerType.GetField("videoPlayer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var videoCanvasField = cinematicManagerType.GetField("videoCanvas", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var videoScreenField = cinematicManagerType.GetField("videoScreen", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var videoAudioSourceField = cinematicManagerType.GetField("videoAudioSource", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (videoPlayerField != null) videoPlayerField.SetValue(cinematicManager, videoPlayer);
            if (videoCanvasField != null) videoCanvasField.SetValue(cinematicManager, videoCanvas);
            if (videoScreenField != null) videoScreenField.SetValue(cinematicManager, rawImage.gameObject); // GameObject로 변환
            if (videoAudioSourceField != null) videoAudioSourceField.SetValue(cinematicManager, audioSource);
            
            // 변경사항 저장
            EditorUtility.SetDirty(cinematicManager);
            
            Debug.Log("[VideoPlayerSetup] CinematicManager 컴포넌트 연결 완료!");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VideoPlayerSetup] CinematicManager 설정 실패: {e.Message}");
            return false;
        }
    }
    
    static Canvas FindCanvasByName(string name)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == name)
            {
                return canvas;
            }
        }
        return null;
    }
    
    [MenuItem("VR Horror Game/Setup/Remove VideoPlayer UI")]
    public static void RemoveVideoPlayerUI()
    {
        Canvas videoCanvas = FindCanvasByName("VideoCanvas");
        if (videoCanvas != null)
        {
            if (EditorUtility.DisplayDialog("VideoPlayer Setup", 
                "VideoPlayer UI를 삭제하시겠습니까?", "삭제", "취소"))
            {
                DestroyImmediate(videoCanvas.gameObject);
                Debug.Log("[VideoPlayerSetup] VideoPlayer UI 삭제 완료");
            }
        }
        else
        {
            Debug.Log("[VideoPlayerSetup] 삭제할 VideoPlayer UI가 없습니다.");
        }
    }
}
#endif 