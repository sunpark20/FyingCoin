using UnityEngine;

// 동전의 고도(Altitude)에 따라 카메라의 배경색을 대기권 층별로 자연스럽게 바꾸는 스크립트
public class BackgroundManager : MonoBehaviour
{
    [Header("추적할 대상 (동전)")]
    public Transform target;

    [System.Serializable]
    public struct AltitudeColor
    {
        public float altitude;
        public Color color;
    }

    [Header("고도별 배경색 설정")]
    public AltitudeColor[] altitudeColors;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // 기본 셋팅이 없을 경우 지구 대기권 데이터를 바탕으로 자동 설정
        if (altitudeColors == null || altitudeColors.Length == 0)
        {
            altitudeColors = new AltitudeColor[5];
            
            ColorUtility.TryParseHtmlString("#87CEEB", out altitudeColors[0].color); // 대류권 0~1200m (밝은 맑은 하늘색)
            altitudeColors[0].altitude = 0f;

            ColorUtility.TryParseHtmlString("#104E8B", out altitudeColors[1].color); // 성층권 1200~5000m (점차 짙어지는 파란색)
            altitudeColors[1].altitude = 1200f;

            ColorUtility.TryParseHtmlString("#000080", out altitudeColors[2].color); // 중간권 5000~8500m (어두운 남색)
            altitudeColors[2].altitude = 5000f;

            ColorUtility.TryParseHtmlString("#1A0033", out altitudeColors[3].color); // 열권 8500~20000m (밤하늘, 짙은 보라색)
            altitudeColors[3].altitude = 8500f;

            ColorUtility.TryParseHtmlString("#000000", out altitudeColors[4].color); // 외기권 및 심우주 20000m~ (칠흑/우주)
            altitudeColors[4].altitude = 20000f;
        }
    }

    void Update()
    {
        if (target == null || cam == null || altitudeColors.Length == 0) return;

        float currentAltitude = target.position.y;
        
        // 현재 고도에 맞춰 보간된 색상을 카메라 배경색으로 적용
        cam.backgroundColor = GetColorAtAltitude(currentAltitude);
    }

    private Color GetColorAtAltitude(float altitude)
    {
        // 최저/최고 고도 예외 처리
        if (altitude <= altitudeColors[0].altitude) return altitudeColors[0].color;
        if (altitude >= altitudeColors[altitudeColors.Length - 1].altitude) return altitudeColors[altitudeColors.Length - 1].color;

        // 현재 고도가 속한 구간을 찾아서 두 색상 사이를 부드럽게 Lerp 연산
        for (int i = 0; i < altitudeColors.Length - 1; i++)
        {
            if (altitude >= altitudeColors[i].altitude && altitude <= altitudeColors[i + 1].altitude)
            {
                float t = (altitude - altitudeColors[i].altitude) / (altitudeColors[i + 1].altitude - altitudeColors[i].altitude);
                return Color.Lerp(altitudeColors[i].color, altitudeColors[i + 1].color, t);
            }
        }

        return Color.black;
    }
}
