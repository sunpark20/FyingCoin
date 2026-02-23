using UnityEngine;

// 벽(±5 world units, 두께 1)의 외측 엣지(±5.5)가 화면 가장자리에 정확히 맞도록
// 기기 가로/세로 비율에 따라 Orthographic Size를 자동 계산합니다.
// iOS 실기기에서 Start() 시점에 Screen.width/height가 0으로 보고되는 문제를
// Update()에서 유효한 값이 잡힐 때까지 매 프레임 체크하는 방식으로 우회합니다.
[RequireComponent(typeof(Camera))]
public class MobileCameraAspect : MonoBehaviour
{
    [Tooltip("카메라가 보여줄 가로 반너비 (월드 유닛). 기본 5.5 = 벽 중심(5) + 반두께(0.5)")]
    public float targetHalfWidth = 5.5f;

    private Camera cam;
    private bool applied = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (applied) return;

        float w = Screen.width;
        float h = Screen.height;

        if (w > 0 && h > 0)
        {
            float currentAspect = w / h;
            cam.orthographicSize = targetHalfWidth / currentAspect;
            applied = true;
            Debug.Log($"📱 카메라 orthoSize 설정 완료: {cam.orthographicSize:F2} (해상도: {w}x{h}, 비율: {currentAspect:F2})");
        }
    }
}
