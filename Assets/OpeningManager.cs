using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class OpeningManager : MonoBehaviour
{
    private GameObject uiCanvas;
    private TextMeshProUGUI introText;

    // 문단 단위로 한꺼번에 출력하기 위해 배열로 나눔
    private string[] storyLines = {
        "2222년 드디어 하이퍼루프 우주선을 개발했다",
        "더블탭 기술을 연속으로 성공시켜 우주선의 추력을 최대로 올리자",
        "당신의 손가락에 인류의 운명이 달려있다",
        "화성으로 가자",
        "\n\n[화면을 터치하여 시작]"
    };

    private string fullStory = "";

    public float lineDelay = 0.8f; // 한 줄당 0.8초 딜레이

    private bool isTyping = false;
    private bool isFinished = false;

    void Start()
    {
        // 1. 게임 일시 정지 (물리, 배경 애니메이션 등)
        Time.timeScale = 0f;

        // 건너뛰기를 위한 전체 스토리 조합
        foreach (string line in storyLines) {
            fullStory += line + "\n\n";
        }

        // 2. 전체 화면 UI 생성
        CreateUI();

        // 3. 문단별 연출 시작
        StartCoroutine(TypeText());
    }

    private void CreateUI()
    {
        // [필수] EventSystem 없으면 Button.onClick이 iOS에서 동작 안 함
        // New Input System (activeInputHandler: 1) 환경이므로 InputSystemUIInputModule 사용
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Canvas 생성
        uiCanvas = new GameObject("OpeningCanvas");
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // 최상단 노출
        
        CanvasScaler scaler = uiCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f; // 가로(Width) 고정 비율로 모바일에 맞춤
        
        // GraphicRaycaster 제거 — Update()에서 직접 폴링으로 대체

        // 검은색 배경 패널 생성
        GameObject bgPanel = new GameObject("BlackBackground");
        bgPanel.transform.SetParent(uiCanvas.transform, false);
        Image bgImg = bgPanel.AddComponent<Image>();
        bgImg.color = Color.black;
        
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Button/onClick 제거 — Update() 직접 폴링으로 대체

        // 텍스트 생성
        GameObject txtObj = new GameObject("IntroText");
        txtObj.transform.SetParent(uiCanvas.transform, false);
        introText = txtObj.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset loadedFont = Resources.Load<TMP_FontAsset>("2002 SDF");
        if (loadedFont != null)
        {
            try { introText.font = loadedFont; }
            catch (System.Exception e) { Debug.LogWarning("[OpeningManager] 폰트 로드 실패: " + e.Message); }
        }
        else
        {
            Debug.LogWarning("[OpeningManager] Resources/2002 SDF.asset 없음 — 기본 폰트 사용");
        }

        introText.fontSize = 34; // 기존 50에서 읽기 쉬운 크기로 대폭 감소
        introText.color = Color.white;
        introText.alignment = TextAlignmentOptions.Center;
        introText.textWrappingMode = TextWrappingModes.Normal;
        introText.overflowMode = TextOverflowModes.Overflow;

        // 텍스트 자체가 터치를 가리지 않도록 막음
        introText.raycastTarget = false;

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        // 좌우 여백을 넓혀 텍스트가 잘리지 않고 가운데로 모이도록 조정
        txtRect.anchorMin = new Vector2(0.1f, 0.1f);
        txtRect.anchorMax = new Vector2(0.9f, 0.9f);
        txtRect.sizeDelta = Vector2.zero;
        
        introText.text = ""; // 초기화
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        introText.text = "";
        
        foreach (string line in storyLines)
        {
            introText.text += line + "\n\n";
            // Time.timeScale이 0이므로 WaitForSecondsRealtime을 사용해야 함
            yield return new WaitForSecondsRealtime(lineDelay); 
        }

        isTyping = false;
        isFinished = true;
    }

    void Update()
    {
        if (!isTyping && !isFinished) return;

        bool tapped = false;

        // iOS 실기기 터치 (New Input System)
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
            tapped = true;

        // 에디터/PC 마우스 테스트용
        var ms = Mouse.current;
        if (ms != null && ms.leftButton.wasPressedThisFrame)
            tapped = true;

        if (tapped) OnScreenClicked();
    }

    public void OnScreenClicked()
    {
        if (isTyping)
        {
            // 타이핑 중이면 즉시 전체 텍스트 출력 후 완료 처리
            StopAllCoroutines();
            introText.text = fullStory;
            isTyping = false;
            isFinished = true;
        }
        else if (isFinished)
        {
            // 화면 닫고 게임 시작
            StartGame();
        }
    }

    private void StartGame()
    {
        // 게임 속도 정상화
        Time.timeScale = 1f;

        // 오프닝 UI 삭제
        if (uiCanvas != null)
        {
            Destroy(uiCanvas);
        }

        // 자기 자신 (스크립트) 삭제 
        Destroy(this);
    }
}
