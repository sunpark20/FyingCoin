using System.Collections;
using UnityEngine;

// 메인 카메라에 부착되어 동전의 높이를 부드럽게 추적하는 스크립트입니다.
public class CameraController : MonoBehaviour
{
    [Header("추적할 대상 (동전)")]
    public Transform target;

    [Header("카메라 세팅")]
    [Tooltip("카메라가 동전을 따라가는 속도 (클수록 빠름)")]
    public float smoothSpeed = 10f;
    
    [Tooltip("화면 중앙에서 동전이 얼마나 아래/위에 위치하게 할지 오프셋")]
    public float yOffset = 1.0f;

    private float baseSmoothSpeed;

    [Header("카메라 화면 흔들림(Shake) 및 지연(Lag) 효과")]
    private float shakeTimer = 0f;
    private float currentShakeMagnitude = 0f;

    // 시작 높이 (이 아래로는 카메라가 내려가지 않음)
    private float initialY;
    
    // 흔들림 등으로 인해 카메라 진짜 위치가 오염되지 않게 논리적 위치를 추적합니다.
    private Vector3 logicalPosition;

    void Start()
    {
        initialY = transform.position.y;
        logicalPosition = transform.position;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // 동전의 현재 Y위치에 오프셋을 더한 목표 지점
            float targetY = target.position.y + yOffset;

            // X와 Z는 흔들림 이전의 논리적 위치(logicalPosition)를 유지합니다.
            Vector3 desiredPosition = new Vector3(logicalPosition.x, targetY, logicalPosition.z);
            
            // 논리적 위치를 Lerp를 사용하여 부드럽게 이동 (산도알 특유의 카메라 워킹 + 지연)
            logicalPosition = Vector3.Lerp(logicalPosition, desiredPosition, smoothSpeed * Time.deltaTime);

            Vector3 finalRenderPosition = logicalPosition;

            // 2. 화면 흔들림(Camera Shake) 로직 적용
            if (shakeTimer > 0)
            {
                // 랜덤한 원 안의 좌표를 구해 카메라 위치 주변으로 일시적인 오프셋을 더합니다.
                // 논리적 위치(logicalPosition)는 변하지 않으므로, 화면이 영구적으로 엇나가지 않습니다.
                Vector2 shakeOffset = Random.insideUnitCircle * currentShakeMagnitude;
                finalRenderPosition.x += shakeOffset.x;
                finalRenderPosition.y += shakeOffset.y;
                
                shakeTimer -= Time.deltaTime;
            }

            // 실제 카메라 오브젝트의 위치 업데이트
            transform.position = finalRenderPosition;
        }
    }

    // 카메라를 즉시 타겟 위치로 스냅 (피버 발사 등 급격한 이동 시)
    public void SnapToTarget()
    {
        if (target == null) return;
        float targetY = target.position.y + yOffset;
        logicalPosition = new Vector3(logicalPosition.x, targetY, logicalPosition.z);
        transform.position = logicalPosition;
    }

    // 외부(CoinPhysics 등)에서 콤보 터치 시 호출하는 흔들림 함수
    public void TriggerShake(float magnitude, float duration)
    {
        currentShakeMagnitude = magnitude;
        shakeTimer = duration;
    }

    // 카메라 orthographicSize를 부드럽게 변경 (피버 줌인/아웃용)
    private Coroutine zoomCoroutine;

    public void ZoomTo(float targetSize, float duration)
    {
        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(ZoomRoutine(targetSize, duration));
    }

    private IEnumerator ZoomRoutine(float targetSize, float duration)
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) yield break;

        float startSize = cam.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
            yield return null;
        }
        cam.orthographicSize = targetSize;
        zoomCoroutine = null;
    }
}
