using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CrosshairManager : MonoBehaviour
{
    [Header("사격 시스템 설정")]
    [Tooltip("총알 최대 충전 개수")]
    public int maxAmmo = 2;
    [Tooltip("총알 1개 충전되는 시간 (초)")]
    public float ammoRechargeTime = 1f;
    [Tooltip("과녁 이동 속도")]
    public float crosshairSpeed = 10f;
    
    // 외부에 공개할 상태
    public Vector2 CrosshairWorldPos { get; private set; }
    
    private int currentAmmo;
    private float rechargeTimer;

    // UI 요소들
    private RectTransform crosshairUI;
    private RectTransform joystickHandle;
    private RectTransform joystickBg;
    private TextMeshProUGUI ammoText;
    
    // 조이스틱 상태
    private Vector2 joystickInput = Vector2.zero;

    // CoinPhysics 캐시
    private CoinPhysics cachedCoin;

    void Start()
    {
        // 중복 인스턴스 방지 (씬에 이미 있으면 자신을 파괴)
        CrosshairManager[] all = FindObjectsByType<CrosshairManager>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Debug.Log("🗑️ CrosshairManager 중복 감지 → 파괴");
            Destroy(gameObject);
            return;
        }
        Initialize();
    }

    public void Initialize()
    {
        currentAmmo = maxAmmo;
        rechargeTimer = ammoRechargeTime;
        CreateUI();
    }

    void Update()
    {
        // 총알 무제한 (테스트 모드) — 항상 최대 유지
        currentAmmo = maxAmmo;

        // 과녁 이동 로직 (조이스틱 입력에 따라)
        if (joystickInput.sqrMagnitude > 0)
        {
            if (crosshairUI != null)
            {
                crosshairUI.anchoredPosition += joystickInput * crosshairSpeed * 100f * Time.deltaTime;
                
                // 게임 영역 안에서만 움직이도록 클램프
                float clampedX = Mathf.Clamp(crosshairUI.anchoredPosition.x, -500f, 500f);
                float clampedY = Mathf.Clamp(crosshairUI.anchoredPosition.y, -750f, 750f);
                crosshairUI.anchoredPosition = new Vector2(clampedX, clampedY);
            }
        }

        // 실제 월드 좌표 계산 (매 프레임 — 카메라 이동에도 정확하게 추적)
        if (crosshairUI != null && Camera.main != null)
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, crosshairUI.position);
            screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
            CrosshairWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
        }
    }

    private void CreateUI()
    {
        // === 캔버스 ===
        GameObject canvasObj = new GameObject("CrosshairCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // === 하단 빈 영역 (화면 하단 1/8) — 아이폰 스와이프 방지 ===
        float deadZoneTop = 1f / 8f; // 0.125
        GameObject deadZone = new GameObject("BottomDeadZone");
        deadZone.transform.SetParent(canvasObj.transform, false);
        RectTransform dzRect = deadZone.AddComponent<RectTransform>();
        dzRect.anchorMin = new Vector2(0f, 0f);
        dzRect.anchorMax = new Vector2(1f, deadZoneTop);
        dzRect.offsetMin = Vector2.zero;
        dzRect.offsetMax = Vector2.zero;
        // 빈 영역이지만 터치를 먹어서 게임에 영향 안 주도록
        Image dzImage = deadZone.AddComponent<Image>();
        dzImage.color = new Color(0f, 0f, 0f, 0.01f); // 거의 투명
        dzImage.raycastTarget = true; // 터치 차단

        // === 조이스틱+발사 바 (빈 영역 바로 위, 화면의 약 1/6 높이) ===
        float barBottom = deadZoneTop; // 0.125
        float barTop = deadZoneTop + 1f / 6f; // 약 0.292
        GameObject barPanel = new GameObject("BottomBar");
        barPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform barRect = barPanel.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, barBottom);
        barRect.anchorMax = new Vector2(1f, barTop);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;

        Image barImage = barPanel.AddComponent<Image>();
        barImage.color = new Color(0.05f, 0.05f, 0.1f, 0.7f); // 어두운 반투명 배경
        barImage.raycastTarget = false;

        // 1. 좌측 조이스틱
        CreateJoystick(barPanel.transform);

        // 2. 우측 전체 발사 영역
        CreateShootArea(barPanel.transform);

        // === 에임/십자선 (게임 영역 중앙) ===
        GameObject chObj = new GameObject("Crosshair");
        chObj.transform.SetParent(canvasObj.transform, false);
        crosshairUI = chObj.AddComponent<RectTransform>();
        // 게임 영역 = barTop(0.292) ~ 1.0, 중앙 = barTop + (1-barTop)/2
        float gameAreaCenter = barTop + (1f - barTop) / 2f;
        crosshairUI.anchorMin = new Vector2(0.5f, gameAreaCenter);
        crosshairUI.anchorMax = new Vector2(0.5f, gameAreaCenter);
        crosshairUI.pivot = new Vector2(0.5f, 0.5f);
        crosshairUI.anchoredPosition = Vector2.zero;
        crosshairUI.sizeDelta = new Vector2(150f, 150f);

        Image chImage = chObj.AddComponent<Image>();
        Sprite targetSprite = Resources.Load<Sprite>("aim");
        if (targetSprite != null)
            chImage.sprite = targetSprite;
        else
            chImage.color = new Color(1f, 0f, 0f, 0.5f);
        chImage.raycastTarget = false;
    }

    private void CreateJoystick(Transform parent)
    {
        // 조이스틱 배경 (좌측에 배치)
        GameObject bgObj = new GameObject("JoystickBg");
        bgObj.transform.SetParent(parent, false);
        joystickBg = bgObj.AddComponent<RectTransform>();
        joystickBg.anchorMin = new Vector2(0f, 0.5f);
        joystickBg.anchorMax = new Vector2(0f, 0.5f);
        joystickBg.pivot = new Vector2(0.5f, 0.5f);
        joystickBg.anchoredPosition = new Vector2(200f, 0f);
        joystickBg.sizeDelta = new Vector2(250f, 250f);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.25f);
        bgImage.sprite = Resources.Load<Sprite>("circle");

        // 핸들
        GameObject hdObj = new GameObject("JoystickHandle");
        hdObj.transform.SetParent(bgObj.transform, false);
        joystickHandle = hdObj.AddComponent<RectTransform>();
        joystickHandle.anchorMin = new Vector2(0.5f, 0.5f);
        joystickHandle.anchorMax = new Vector2(0.5f, 0.5f);
        joystickHandle.pivot = new Vector2(0.5f, 0.5f);
        joystickHandle.anchoredPosition = Vector2.zero;
        joystickHandle.sizeDelta = new Vector2(100f, 100f);

        Image hdImage = hdObj.AddComponent<Image>();
        hdImage.color = new Color(1f, 1f, 1f, 0.8f);
        hdImage.sprite = Resources.Load<Sprite>("circle");

        // 조이스틱 드래그 컴포넌트
        SimpleJoystick uiJoy = bgObj.AddComponent<SimpleJoystick>();
        uiJoy.handle = joystickHandle;
        uiJoy.onMove = (Vector2 dir) => { joystickInput = dir; };
    }

    private void CreateShootArea(Transform parent)
    {
        // 우측 전체를 덮는 발사 터치 영역 (바의 35%~100% 가로)
        GameObject shObj = new GameObject("ShootArea");
        shObj.transform.SetParent(parent, false);
        RectTransform shRect = shObj.AddComponent<RectTransform>();
        shRect.anchorMin = new Vector2(0.35f, 0f);
        shRect.anchorMax = new Vector2(1f, 1f);
        shRect.offsetMin = Vector2.zero;
        shRect.offsetMax = Vector2.zero;

        Image shImage = shObj.AddComponent<Image>();
        shImage.color = new Color(0.8f, 0.15f, 0.1f, 0.35f);

        // Button 대신 IPointerDownHandler로 즉시 발사 (리듬게임 방식)
        ShootTrigger trigger = shObj.AddComponent<ShootTrigger>();
        trigger.owner = this;

        // "SHOOT" + 총알 갯수 텍스트
        GameObject txtObj = new GameObject("AmmoText");
        txtObj.transform.SetParent(shObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0.5f, 0.5f);
        txtRect.anchorMax = new Vector2(0.5f, 0.5f);
        txtRect.pivot = new Vector2(0.5f, 0.5f);
        txtRect.anchoredPosition = Vector2.zero;
        txtRect.sizeDelta = new Vector2(400f, 80f);

        ammoText = txtObj.AddComponent<TextMeshProUGUI>();
        ammoText.fontSize = 48;
        ammoText.alignment = TextAlignmentOptions.Center;
        ammoText.color = Color.white;
        ammoText.raycastTarget = false;
        TMP_FontAsset customFont = Resources.Load<TMP_FontAsset>("2002 SDF");
        if (customFont != null) ammoText.font = customFont;

        UpdateAmmoUI();
    }

    public void OnShootPressed()
    {
        // 총알 무제한 (테스트 모드) — 항상 발사 가능
        if (cachedCoin == null)
            cachedCoin = FindAnyObjectByType<CoinPhysics>();

        if (cachedCoin != null)
            cachedCoin.TryHitFromCrosshair(CrosshairWorldPos);

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = currentAmmo + " / " + maxAmmo;
    }
}

// 조이스틱 UI 움직임을 iOS에서도 완벽 지원하기 위한 인터페이스 상속 클래스
public class SimpleJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform handle;
    public System.Action<Vector2> onMove;
    private Vector2 startPos;
    private float radius;

    void Start()
    {
        startPos = handle.anchoredPosition;
        RectTransform bg = GetComponent<RectTransform>();
        radius = bg.sizeDelta.x / 2f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera, out localPoint);

        if (localPoint.magnitude > radius)
            localPoint = localPoint.normalized * radius;

        handle.anchoredPosition = localPoint;
        onMove?.Invoke(localPoint / radius);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = startPos;
        onMove?.Invoke(Vector2.zero);
    }
}

// 발사 버튼: 누르는 즉시(PointerDown) 발사 (리듬게임 방식)
// Button.onClick은 손가락 뗄 때(PointerUp) 발동 → 50~100ms 지연 발생
public class ShootTrigger : MonoBehaviour, IPointerDownHandler
{
    public CrosshairManager owner;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnShootPressed();
    }
}
