using UnityEngine;
using TMPro; // 최신 유니티 기본 텍스트인 TextMeshPro 사용

public class GameManager : MonoBehaviour
{
    [Header("UI 연결")]
    [Tooltip("화면에 높이를 표시할 텍스트 컴포넌트")]
    public TextMeshProUGUI heightText;
    
    [Header("추적할 동전")]
    public Transform coinTransform;

    [Header("설정")]
    [Tooltip("유니티의 1 유닛을 몇 미터로 환산할 것인가? (기본 1유닛=1m)")]
    public float heightMultiplier = 1.0f;

    // 시작 시점의 바닥 Y 위치 (이곳이 0m가 됩니다)
    private float initialY;
    private float highestAltitude = 0f;

    // [카메라 보정] MobileCameraAspect가 미실행될 경우 대비 fallback
    private bool cameraApplied = false;

    // [디버그] 하단 디버그 패널
    private TextMeshProUGUI debugText;

    // 바닥벽 추적용 (동전이 버튼 영역에 안 내려오게)
    private Transform bottomWallTransform;

    void Start()
    {
        if (coinTransform != null)
        {
            initialY = coinTransform.position.y;
        }
        else
        {
            Debug.LogWarning("동전(CoinTransform)이 연결되지 않았습니다!");
        }

        CreateDebugUI();
        BootstrapManagers();
    }

    private void AdjustBottomWall(Camera cam)
    {
        GameObject bottomWall = GameObject.Find("BottomWall_Auto");
        if (bottomWall == null || cam == null) return;

        float screenBottom = cam.transform.position.y - cam.orthographicSize;
        // 벽 중심을 화면 바닥 아래로 (벽 높이 1의 절반 = 0.5 보정 → 윗면이 화면 바닥과 일치)
        bottomWall.transform.position = new Vector2(0, screenBottom - 0.5f);
        Debug.Log($"🧱 바닥 벽 위치 조정: y={bottomWall.transform.position.y:F1} (화면 바닥: {screenBottom:F1})");
    }

    private void CreateDebugUI()
    {
        Canvas canvas = heightText != null ? heightText.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return;

        GameObject debugObj = new GameObject("DebugText");
        debugObj.transform.SetParent(canvas.transform, false);

        debugText = debugObj.AddComponent<TextMeshProUGUI>();
        debugText.fontSize = 28;
        debugText.color = new Color(1f, 1f, 1f, 0.7f);
        debugText.alignment = TextAlignmentOptions.Center;
        debugText.text = "Tap: -- ms";

        RectTransform rt = debugObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 80f);
        rt.sizeDelta = new Vector2(400, 60);
    }

    void Update()
    {
        // [카메라 보정] 벽 기준 카메라 크기 강제 적용 (MobileCameraAspect 미실행 시 fallback)
        if (!cameraApplied && Camera.main != null)
        {
            float w = Screen.width;
            float h = Screen.height;
            if (w > 0 && h > 0)
            {
                float aspect = w / h;
                Camera.main.orthographicSize = 5.5f / aspect;
                cameraApplied = true;
                Debug.Log($"📱 [GameManager] 카메라 강제 설정: orthoSize={Camera.main.orthographicSize:F2}, 해상도={w}x{h}, 비율={aspect:F4}");
            }
        }

        // 바닥벽을 카메라 하단에서 (1/8 빈 영역 + 1/6 조이스틱 바) 위치로 추적
        UpdateBottomWall();

        if (coinTransform != null && heightText != null)
        {
            float currentHeight = (coinTransform.position.y - initialY) * heightMultiplier;
            currentHeight += CoinPhysics.comboBonusHeight; // 콤보 보너스 높이 합산
            
            if (currentHeight > highestAltitude)
                highestAltitude = currentHeight;

            if (currentHeight < 0) currentHeight = 0;
            heightText.text = currentHeight.ToString("F1") + "m";
        }

        // [디버그] 더블 탭 간격 표시
        if (debugText != null)
        {
            if (CoinPhysics.lastPairDeltaMs > 0f)
                debugText.text = $"Tap: {CoinPhysics.lastPairDeltaMs:F0} ms";
        }
    }

    /// <summary>씬에 CrosshairManager / SkillBarManager가 없으면 자동 생성</summary>
    private void BootstrapManagers()
    {
        if (FindAnyObjectByType<CrosshairManager>() == null)
        {
            GameObject chObj = new GameObject("CrosshairManager");
            chObj.AddComponent<CrosshairManager>();
            Debug.Log("🎯 CrosshairManager 자동 생성");
        }

        if (FindAnyObjectByType<SkillBarManager>() == null)
        {
            GameObject skObj = new GameObject("SkillBarManager");
            skObj.AddComponent<SkillBarManager>();
            Debug.Log("🔥 SkillBarManager 자동 생성");
        }

        // 바닥벽에 Kinematic Rigidbody2D 추가 (매 프레임 이동을 효율적으로 처리)
        GameObject bw = GameObject.Find("BottomWall_Auto");
        if (bw != null)
        {
            bottomWallTransform = bw.transform;
            Rigidbody2D wallRb = bw.GetComponent<Rigidbody2D>();
            if (wallRb == null)
            {
                wallRb = bw.AddComponent<Rigidbody2D>();
                wallRb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
    }

    /// <summary>바닥벽을 화면 하단 (1/8 빈 영역 + 1/6 바) 위치로 실시간 추적</summary>
    private void UpdateBottomWall()
    {
        if (bottomWallTransform == null || Camera.main == null) return;

        Camera cam = Camera.main;
        float screenHeight = cam.orthographicSize * 2f;
        // 하단 빈 영역(1/8) + 조이스틱 바(1/6) = 7/24 ≈ 0.292
        float barWorldHeight = screenHeight * (1f / 8f + 1f / 6f);
        float gameAreaBottom = cam.transform.position.y - cam.orthographicSize + barWorldHeight;

        // 벽 윗면이 게임 영역 하단에 오도록 배치 (벽 높이의 절반만큼 아래로)
        bottomWallTransform.position = new Vector2(0, gameAreaBottom - 0.5f);
    }
}
