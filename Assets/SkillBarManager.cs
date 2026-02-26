using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 스킬 버튼 UI + 피버 스킬 로직을 관리하는 매니저
// 스킬 버튼은 화면 우측, 하단 바(1/6) 바로 위에 작게 배치
public class SkillBarManager : MonoBehaviour
{
    [Header("피버 스킬 설정")]
    [Tooltip("피버 지속 시간 (초)")]
    public float feverDuration = 5f;

    [Tooltip("피버 쿨다운 (초) — 0이면 즉시 재사용 가능")]
    public float feverCooldown = 0f;

    [Tooltip("피버 중 동전 확대 배율")]
    public float feverZoomScale = 1.2f;

    [Tooltip("피버 종료 시 기본 점프 속도 (제곱 보너스에 곱해짐)")]
    public float feverBaseJump = 10f;

    [Tooltip("피버 줌인 시 카메라 orthographicSize 목표값 (클수록 덜 줌인)")]
    public float feverCameraZoom = 6f;

    // UI 요소들
    private Canvas skillCanvas;
    private Button feverButton;
    private Image cooldownOverlay;
    private TextMeshProUGUI hitCountText;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI buttonLabel;

    // 상태
    private bool isCooldown = false;
    private float cooldownTimer = 0f;
    private float feverTimer = 0f;

    // 참조
    private Transform coinTransform;
    private Rigidbody2D coinRb;
    private Vector3 originalCoinScale;
    private Vector2 originalCoinPosition;
    private float originalCameraSize;
    private float originalGravityScale;
    private CameraController cameraController;

    // 폰트
    private TMPro.TMP_FontAsset customFont;

    void Start()
    {
        customFont = Resources.Load<TMPro.TMP_FontAsset>("2002 SDF");

        GameObject coin = GameObject.Find("Coin");
        if (coin != null)
        {
            coinTransform = coin.transform;
            coinRb = coin.GetComponent<Rigidbody2D>();
        }

        if (Camera.main != null)
            cameraController = Camera.main.GetComponent<CameraController>();

        CreateUI();
    }

    void Update()
    {
        // 쿨다운 처리
        if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownOverlay != null)
                cooldownOverlay.fillAmount = cooldownTimer / feverCooldown;

            if (cooldownTimer <= 0f)
            {
                isCooldown = false;
                cooldownTimer = 0f;
                if (cooldownOverlay != null)
                    cooldownOverlay.fillAmount = 0f;
                if (feverButton != null)
                    feverButton.interactable = true;
            }
        }

        // 피버 진행 중 타이머 표시
        if (CoinPhysics.isFeverActive)
        {
            feverTimer -= Time.deltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(Mathf.Max(0f, feverTimer)).ToString() + "s";
            if (hitCountText != null)
                hitCountText.text = CoinPhysics.feverHitCount + " hits!";
        }
    }

    private void CreateUI()
    {
        // === 캔버스 ===
        GameObject canvasObj = new GameObject("SkillBarCanvas");
        skillCanvas = canvasObj.AddComponent<Canvas>();
        skillCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        skillCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // === 피버 버튼 (우측, 하단 바 바로 위에 작게) ===
        float barTopY = 1f / 8f + 1f / 6f; // 빈 영역(1/8) + 조이스틱 바(1/6) = 약 0.292

        GameObject btnObj = new GameObject("FeverButton");
        btnObj.transform.SetParent(canvasObj.transform, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, barTopY);
        btnRect.anchorMax = new Vector2(1f, barTopY);
        btnRect.pivot = new Vector2(1f, 0f); // 우하단 피벗
        btnRect.anchoredPosition = new Vector2(-20f, 15f); // 우측 가장자리에서 20px, 바 위 15px
        btnRect.sizeDelta = new Vector2(100f, 100f);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(1f, 0.3f, 0.1f, 1f); // 불투명 붉은 오렌지

        feverButton = btnObj.AddComponent<Button>();
        feverButton.targetGraphic = btnImage;

        ColorBlock colors = feverButton.colors;
        colors.normalColor = new Color(1f, 0.3f, 0.1f, 1f);
        colors.highlightedColor = new Color(1f, 0.5f, 0.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.2f, 0.05f, 1f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        feverButton.colors = colors;

        feverButton.onClick.AddListener(ActivateFever);

        // 버튼 텍스트
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        buttonLabel = labelObj.AddComponent<TextMeshProUGUI>();
        buttonLabel.text = "F";
        buttonLabel.fontSize = 36;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.color = Color.white;
        if (customFont != null) buttonLabel.font = customFont;

        // 쿨다운 오버레이
        GameObject overlayObj = new GameObject("CooldownOverlay");
        overlayObj.transform.SetParent(btnObj.transform, false);
        RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        cooldownOverlay = overlayObj.AddComponent<Image>();
        cooldownOverlay.color = new Color(0f, 0f, 0f, 0.6f);
        cooldownOverlay.type = Image.Type.Filled;
        cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
        cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
        cooldownOverlay.fillClockwise = true;
        cooldownOverlay.fillAmount = 0f;
        cooldownOverlay.raycastTarget = false;

        // === 피버 히트 카운트 텍스트 (화면 중앙 상단) ===
        GameObject hitObj = new GameObject("FeverHitCount");
        hitObj.transform.SetParent(canvasObj.transform, false);
        RectTransform hitRect = hitObj.AddComponent<RectTransform>();
        hitRect.anchorMin = new Vector2(0.5f, 0.6f);
        hitRect.anchorMax = new Vector2(0.5f, 0.6f);
        hitRect.pivot = new Vector2(0.5f, 0.5f);
        hitRect.anchoredPosition = Vector2.zero;
        hitRect.sizeDelta = new Vector2(600f, 150f);

        hitCountText = hitObj.AddComponent<TextMeshProUGUI>();
        hitCountText.text = "";
        hitCountText.fontSize = 96;
        hitCountText.alignment = TextAlignmentOptions.Center;
        hitCountText.color = new Color(1f, 1f, 0f, 1f);
        hitCountText.enableWordWrapping = false;
        if (customFont != null) hitCountText.font = customFont;
        hitObj.SetActive(false);

        // === 피버 타이머 텍스트 ===
        GameObject timerObj = new GameObject("FeverTimer");
        timerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform timerRect = timerObj.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.55f);
        timerRect.anchorMax = new Vector2(0.5f, 0.55f);
        timerRect.pivot = new Vector2(0.5f, 0.5f);
        timerRect.anchoredPosition = Vector2.zero;
        timerRect.sizeDelta = new Vector2(400f, 80f);

        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "";
        timerText.fontSize = 48;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        if (customFont != null) timerText.font = customFont;
        timerObj.SetActive(false);
    }

    public void ActivateFever()
    {
        if (CoinPhysics.isFeverActive || isCooldown) return;
        if (coinRb == null || coinTransform == null) return;

        StartCoroutine(FeverRoutine());
    }

    private IEnumerator FeverRoutine()
    {
        // --- 피버 시작 ---
        CoinPhysics.isFeverActive = true;
        CoinPhysics.feverHitCount = 0;
        CoinPhysics.feverHitPower = 0f;
        feverTimer = feverDuration;

        if (feverButton != null)
            feverButton.interactable = false;

        // 동전 물리 정지
        originalGravityScale = coinRb.gravityScale;
        coinRb.linearVelocity = Vector2.zero;
        coinRb.angularVelocity = 0f;
        coinRb.gravityScale = 0f;
        coinRb.constraints = RigidbodyConstraints2D.FreezeAll;

        // 동전을 화면 정중앙으로 이동
        originalCoinPosition = coinRb.position;
        Vector2 screenCenter = Camera.main.transform.position;
        coinRb.constraints = RigidbodyConstraints2D.None;
        coinRb.position = screenCenter;
        coinRb.constraints = RigidbodyConstraints2D.FreezeAll;

        // 동전 확대 (Lerp)
        originalCoinScale = coinTransform.localScale;
        Vector3 targetScale = originalCoinScale * feverZoomScale;
        float scaleElapsed = 0f;
        float scaleDuration = 0.3f;
        while (scaleElapsed < scaleDuration)
        {
            scaleElapsed += Time.deltaTime;
            coinTransform.localScale = Vector3.Lerp(originalCoinScale, targetScale, scaleElapsed / scaleDuration);
            yield return null;
        }
        coinTransform.localScale = targetScale;

        // 카메라 줌인
        if (Camera.main != null)
        {
            originalCameraSize = Camera.main.orthographicSize;
            if (cameraController != null)
                cameraController.ZoomTo(feverCameraZoom, 0.3f);
        }

        // UI 표시
        if (hitCountText != null)
        {
            hitCountText.gameObject.SetActive(true);
            hitCountText.text = "0 hits!";
        }
        if (timerText != null)
            timerText.gameObject.SetActive(true);

        // --- 5초 대기 ---
        yield return new WaitForSeconds(feverDuration);

        // --- 피버 종료 ---
        CoinPhysics.isFeverActive = false;
        int finalHits = CoinPhysics.feverHitCount;

        if (hitCountText != null)
            hitCountText.gameObject.SetActive(false);
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        // 동전 원복 (스케일)
        scaleElapsed = 0f;
        Vector3 currentScale = coinTransform.localScale;
        while (scaleElapsed < scaleDuration)
        {
            scaleElapsed += Time.deltaTime;
            coinTransform.localScale = Vector3.Lerp(currentScale, originalCoinScale, scaleElapsed / scaleDuration);
            yield return null;
        }
        coinTransform.localScale = originalCoinScale;

        if (cameraController != null)
            cameraController.ZoomTo(originalCameraSize, 0.3f);

        // 거리 가중치 기반 √×ln 보너스 높이 계산
        float totalPower = CoinPhysics.feverHitPower;
        float bonusVelocity = feverBaseJump * Mathf.Sqrt(totalPower) * Mathf.Log(1f + totalPower);
        float gravity = 9.81f * originalGravityScale;
        float peakHeight = (bonusVelocity * bonusVelocity) / (2f * gravity);

        coinRb.constraints = RigidbodyConstraints2D.None;
        coinRb.gravityScale = originalGravityScale;

        Vector2 peakPos = new Vector2(originalCoinPosition.x, originalCoinPosition.y + peakHeight);
        coinRb.position = peakPos;
        coinRb.linearVelocity = Vector2.zero;

        float fallTime = Mathf.Sqrt(2f * peakHeight / gravity) + 1f;
        if (cameraController != null)
        {
            cameraController.StartInstantTrack(fallTime);
            cameraController.TriggerShake(1.2f, 0.3f);
        }

        Debug.Log($"[Fever] 종료! {finalHits}회 터치, 파워={totalPower:F1} → 높이: {peakHeight:F0}m, 낙하시간: {fallTime:F1}초");

        // 쿨다운 시작
        isCooldown = true;
        cooldownTimer = feverCooldown;
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 1f;
    }
}
