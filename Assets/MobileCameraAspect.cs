using UnityEngine;

// 모바일 기기(iPhone/Android)의 화면 비율에 맞춰 카메라의 가로 시야를 고정해주는 스크립트입니다.
// 16:9(1080x1920) 비율을 기준으로 가로가 잘리지 않게 동적으로 Orthographic Size를 조절합니다.
[RequireComponent(typeof(Camera))]
public class MobileCameraAspect : MonoBehaviour
{
    [Tooltip("기준 해상도 가로")]
    public float targetWidth = 1080f;
    [Tooltip("기준 해상도 세로")]
    public float targetHeight = 1920f;
    
    [Tooltip("기본 Orthographic Size (16:9 일 때의 카메라 사이즈)")]
    public float defaultOrthoSize = 10f; // 필요시 조정

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        
        // 기준 비율 (예: 1080 / 1920 = 0.5625)
        float targetAspect = targetWidth / targetHeight;
        
        // 현재 기기 비율
        float currentAspect = (float)Screen.width / (float)Screen.height;

        // 현재 기기의 가로 비율이 기준보다 좁다면 (예: 좀 더 길쭉한 최신 스마트폰 19.5:9 등)
        if (currentAspect < targetAspect)
        {
            // 가로 시야가 잘리지 않도록 카메라 사이즈를 키워줌
            cam.orthographicSize = defaultOrthoSize * (targetAspect / currentAspect);
        }
        else
        {
            // 태블릿처럼 가로 리율이 더 넓다면, 기본 사이즈 유지 (세로 기준으로 맞춰짐)
            // 혹은 레터박스를 넣을 수 있지만, 현재는 무한 상승 게임이므로 기본값(가로가 더 넓게 보임) 유지
            cam.orthographicSize = defaultOrthoSize;
        }
        
        Debug.Log($"📱 모바일 해상도 대응 완료: 기기 비율({currentAspect:F2}), 오르토그래픽 사이즈 조정({cam.orthographicSize:F2})");
    }
}
