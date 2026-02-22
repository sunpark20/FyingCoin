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
    }

    void Update()
    {
        if (coinTransform != null && heightText != null)
        {
            // 현재 높이 계산 (시작점 대비 얼마나 올라왔는지)
            float currentHeight = (coinTransform.position.y - initialY) * heightMultiplier;
            
            // 공중에 있을 때(0m 이상일 때) 혹은 최고 기록 갱신 시 업데이트
            if (currentHeight > highestAltitude)
            {
                highestAltitude = currentHeight;
            }

            // 바닥 밑(-y)으로 떨어지면 그냥 0m로 표시
            if (currentHeight < 0) currentHeight = 0;

            // 소수점 1자리까지 표시 (예: 15.2m)
            heightText.text = currentHeight.ToString("F1") + "m";
            
            // 만약 최고 기록만 찍고 싶다면 아래 코드를 사용하세요.
            // heightText.text = highestAltitude.ToString("F1") + "m";
        }
    }
}
