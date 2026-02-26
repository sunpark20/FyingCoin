using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// [유니티 완벽 물리 프로토타입] 
// 동전에 이 스크립트를 넣고 Rigidbody2D와 CircleCollider2D를 추가합니다.
public class CoinPhysics : MonoBehaviour
{
    private Rigidbody2D rb;

    // 피버 스킬 연동 (SkillBarManager가 제어)
    public static bool isFeverActive = false;
    public static int feverHitCount = 0;        // 터치 횟수 (UI 표시용)
    public static float feverHitPower = 0f;      // 거리 가중치 누적 (보너스 계산용)

    // 콤보 보너스 높이 (GameManager에서 참조하여 높이 표시에 합산)
    public static float comboBonusHeight = 0f;

    [Header("산도알 스타일 물리 파라미터")]
    [Tooltip("동전 크기 배율 (1.0 = 기본, 0.8 = 20% 작게). 크기와 판정 범위가 동시에 조절됨")]
    public float coinScale = 0.7f;

    [Tooltip("기초 중력 조절 (기본 0.5 - 테스트용 느린 속도)")]
    public float gravityScale = 0.5f; 
    
    [Tooltip("첫 터치 시 위로 솟구치는 기본 속도 (테스트용 느린 값)")]
    public float jumpVelocity = 5f; 
    
    [Header("사운드 이펙트")]
    [Tooltip("동전을 맞출 때마다 랜덤으로 재생될 소리 목록")]
    public AudioClip[] hitSounds;
    
    [Tooltip("연속 터치(콤보) 성공 시 재생될 특별한 소리")]
    public AudioClip comboSound;
    private AudioSource audioSource;
    
    [Header("더블 콤보 액션 세팅")]
    [Tooltip("연속 터치로 인정받기 위한 제한 시간 (초 단위, ex: 0.5초 안에 또 때려야 함)")]
    public float comboTimeLimit = 0.5f;
    
    [Tooltip("더블 클릭 시 원래 점프력에 곱해지는 파워업 배율 (1.5 = 1.5배 더 세짐)")]
    public float comboMultiplier = 1.5f;

    private float lastHitTime = -100f; // 마지막으로 맞은 시간 기록 변수
    private int currentCombo = 0; // 현재 연속으로 맞춘 횟수

    // [디버그] 더블 탭 간격 측정 (1→2, 3→4, 5→6... 쌍별 측정)
    private int hitCount = 0;
    private float pairStartTime = 0f;
    public static float lastPairDeltaMs = 0f;
    
    [Space(10)]
    [Tooltip("맞은 위치에 비례하여 생기는 빙글빙글 스핀 속도 배율 (Z축 2D 회전)")]
    public float spinMultiplier = 500f;
    
    [Header("시각적 3D 효과 (가짜 3D 회전)")]
    [Tooltip("동전이 위아래로 튀어 오를 때 앞으로 덤블링하는 3D 시각 효과 속도")]
    public float visual3DSpinSpeed = 1500f;

    [Tooltip("가장자리를 맞췄을 때 좌우로 튕겨 나가는 힘의 강도 (기본 40)")]
    public float horizontalBounceForce = 40f;
    
    [Tooltip("최대 하강 속도 제한 (테스트용 느릴 값)")]
    public float maxFallSpeed = -3f;

    [Header("동전 앞뒷면 스프라이트")]
    [Tooltip("동전 옆면 최소 Y스케일 (0에 가까울수록 얇음, 0.05 = 5%)")]
    [SerializeField] private float edgeMinScaleY = 0.08f;

    private Sprite frontSprite;
    private Sprite backSprite;
    private SpriteRenderer sr;
    private float baseScaleY;
    private float touchRadius; // 정면 기준 터치 반경 (고정)
    private float virtualFlipAngle = 0f; // X축 회전을 가상으로 추적 (실제 transform.Rotate 대체)

    [Header("구멍 이펙트")]
    [Tooltip("터치 타격 지점에 표시되는 구멍 이미지 크기 (동전 크기 대비)")]
    [SerializeField] private float holeEffectScale = 0.3f;
    [Tooltip("구멍 이미지의 렌더링 레이어 (동전보다 높아야 앞에 보임)")]
    [SerializeField] private int holeSortingOrder = 10;
    private Sprite holeSprite;

    // [추가] 터치 가능 영역을 표시할 정면 가이드 오브젝트
    private GameObject touchVisualizer;

    // 디버그용 텍스트 변수
    private string debugTouchMsg = "";
    private float debugTouchTimer = 0f;

    // 괤적 보간 판정용 (이전 프레임 위치 저장)
    private Vector2 previousPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 오디오 소스 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // 앞뒷면 스프라이트 로드
        sr = GetComponent<SpriteRenderer>();
        frontSprite = Resources.Load<Sprite>("dena-front");
        backSprite = Resources.Load<Sprite>("dena-back");
        if (frontSprite == null || backSprite == null)
            Debug.LogWarning("[CoinPhysics] dena-front.png / dena-back.png를 Assets/Resources/ 폴더에 넣어주세요!");

        // 앞면 스프라이트 초기 적용
        if (sr != null && frontSprite != null)
            sr.sprite = frontSprite;

        // coinScale 적용 (크기 + 판정 범위 동시 조절)
        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(
            originalScale.x * coinScale, 
            originalScale.y * coinScale, 
            originalScale.z
        );

        // 기본 Y스케일 저장 (두께감 계산용) — coinScale 적용 후 저장
        baseScaleY = transform.localScale.y;

        // 정면 상태의 터치 반경 계산 (콜라이더 기준)
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
            touchRadius = col.radius * transform.localScale.x * 2.1f;
        else
            touchRadius = transform.localScale.x * 0.5f;

        Debug.Log($"[CoinPhysics] touchRadius={touchRadius}, col.radius={col?.radius}, scale.x={transform.localScale.x}");

        holeSprite = Resources.Load<Sprite>("circle");
        if (holeSprite == null)
            Debug.LogWarning("[CoinPhysics] circle.png를 Assets/Resources/ 폴더에 넣어주세요!");

        if (rb != null)
        {
            // 영상 분석 1: 묵직하고 기민한 아크(Arc)를 위한 고중력 세팅
            rb.gravityScale = gravityScale;
            
            // 영상 분석 2: 진공 상태처럼 마찰력 0으로 세팅 (좌우로 영원히 튕기게)
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            
            // 통통 튀게 하려면 PhysicsMaterial2D(Bounciness=1, Friction=0)를 Collider에 넣어야 합니다.
        }

        // 터치 영역 가이드 이미지 생성
        CreateTouchVisualizer();
    }

    private void CreateTouchVisualizer()
    {
        touchVisualizer = new GameObject("TouchAreaIndicator");
        SpriteRenderer vsr = touchVisualizer.AddComponent<SpriteRenderer>();
        
        if (holeSprite != null)
        {
            vsr.sprite = holeSprite; // circle.png를 holeSprite에 로드해둠
            vsr.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 회색 50% 투명
            vsr.sortingOrder = sr != null ? sr.sortingOrder - 2 : -2; // 동전 뒤
            
            // 터치 반경(touchRadius)에 딱 맞게 스케일 조정
            float circleSize = vsr.sprite.bounds.size.x;
            if (circleSize > 0)
            {
                float targetScale = (touchRadius * 2f) / circleSize;
                touchVisualizer.transform.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }
        else if (frontSprite != null)
        {
            // circle.png가 없으면 그냥 동전 정면을 사용
            vsr.sprite = frontSprite;
            vsr.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            vsr.sortingOrder = sr != null ? sr.sortingOrder - 2 : -2;
            touchVisualizer.transform.localScale = transform.localScale;
        }
    }

    void Update()
    {
        // 최저 낙하 속도 제한 (너무 빨라지면 프레임 뚫고 나가는 버그 방지)
        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }

        // --- 2.5D 시각적 3D 회전 효과 (가상 각도 + Y스케일) ---
        // 기존 X축 transform.Rotate 대신, 가상 각도로 추적하고 Y스케일로 시각화
        if (Mathf.Abs(rb.linearVelocity.y) > 0.1f)
        {
            float flipAmount = visual3DSpinSpeed * Time.deltaTime;
            float direction = rb.linearVelocity.y > 0 ? -1f : 1f;
            virtualFlipAngle += direction * flipAmount;
        }

        // X축 실제 회전 제거 (가상 각도로 대체했으므로)
        Vector3 euler = transform.eulerAngles;
        euler.x = 0f;
        transform.eulerAngles = euler;

        // --- 앞뒷면 스프라이트 전환 + Y스케일로 두께감 ---
        if (sr != null && frontSprite != null && backSprite != null)
        {
            float angle = ((virtualFlipAngle % 360f) + 360f) % 360f;

            // 앞면/뒷면 판정
            bool showFront = (angle <= 90f || angle >= 270f);
            sr.sprite = showFront ? frontSprite : backSprite;

            // cos으로 자연스러운 두께감: 정면(0°) = 1.0, 옆면(90°) = edgeMinScaleY
            float cosVal = Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad));
            float scaleY = Mathf.Lerp(edgeMinScaleY, 1f, cosVal) * baseScaleY;

            Vector3 s = transform.localScale;
            s.y = scaleY;
            transform.localScale = s;
        }

        if (touchVisualizer != null)
        {
            touchVisualizer.transform.position = transform.position;
        }

        if (debugTouchTimer > 0) debugTouchTimer -= Time.deltaTime;

        // 괤적 보간 판정용: 매 프레임 이전 위치 저장
        previousPosition = (Vector2)transform.position;

        // "터치가 아닌 총을 쏘는 과녁형식"으로 변경되었으므로 직접 터치 처리 삭제
    }

    // CrosshairManager (과녁 시스템)에서 버튼을 누를 때 호출되는 새로운 발사 함수
    public void TryHitFromCrosshair(Vector2 worldPos)
    {
        SetDebugText("과녁 발사! (총)");
        
        // 1차: 현재 위치 판정 (정면 기준 원형 터치 영역)
        Vector2 currentPos = (Vector2)transform.position;
        float dist = Vector2.Distance(worldPos, currentPos);
        if (dist <= touchRadius)
        {
            Debug.Log($"동전 맞춤! 좌표: {worldPos}");
            SetDebugText(debugTouchMsg + "\n-> 동전 명중!");
            HitCoin(worldPos);
            return;
        }

        // 2차: 괤적 보간 판정 (이전 프레임 ~ 현재 프레임 사이 경로 체크)
        // 동전이 고속 이동 중일 때, 에임이 괤적 위에 있으면 명중
        float segLen = Vector2.Distance(previousPosition, currentPos);
        if (segLen > 0.01f) // 이동이 있었을 때만
        {
            // 점과 선분 사이의 최단 거리 계산
            Vector2 segDir = currentPos - previousPosition;
            float t = Mathf.Clamp01(Vector2.Dot(worldPos - previousPosition, segDir) / (segLen * segLen));
            Vector2 closestPoint = previousPosition + t * segDir;
            float closestDist = Vector2.Distance(worldPos, closestPoint);

            if (closestDist <= touchRadius)
            {
                Debug.Log($"동전 맞춤! (괤적 보간) 좌표: {worldPos}, 최근접점: {closestPoint}");
                SetDebugText(debugTouchMsg + "\n-> 괤적 보간 명중!");
                HitCoin(closestPoint); // 괤적 위의 최근접점을 타격점으로 사용
                return;
            }
        }

        Debug.Log($"빗나감! 좌표: {worldPos}, 충돌체: 없음");
        SetDebugText(debugTouchMsg + "\n-> 빗나감(허공)");
    }

    private void SetDebugText(string msg)
    {
        debugTouchMsg = msg;
        debugTouchTimer = 1.0f;
    }

    void OnGUI()
    {
        if (debugTouchTimer > 0)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = Screen.width / 15;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;
            
            // 글자 테두리(그림자) 효과를 위해 여러 번 그림
            Rect rect = new Rect(50, 100, Screen.width, Screen.height / 10);
            GUIStyle shadow = new GUIStyle(style);
            shadow.normal.textColor = Color.black;
            
            GUI.Label(new Rect(rect.x+3, rect.y+3, rect.width, rect.height), debugTouchMsg, shadow);
            GUI.Label(rect, debugTouchMsg, style);
        }
    }

    private void HitCoin(Vector2 hitPoint)
    {
        // 피버 중에는 카운트만 올리고 물리 적용 스킵
        if (isFeverActive)
        {
            feverHitCount++;

            // 중심에서 떨어진 거리 비율 (0=정중앙, 1=가장자리)로 가중치 계산
            // 가장자리 맞출수록 세게 (0.5 ~ 2.0 범위)
            float dist = Vector2.Distance(hitPoint, (Vector2)transform.position);
            float ratio = Mathf.Clamp01(dist / touchRadius);
            float hitWeight = 0.5f + ratio * 1.5f; // 중앙=0.5, 가장자리=2.0
            feverHitPower += hitWeight;

            SpawnHoleEffect(hitPoint);

            // 피버 터치 사운드 (일반 효과음)
            if (hitSounds != null && hitSounds.Length > 0 && audioSource != null)
            {
                AudioClip randomClip = hitSounds[Random.Range(0, hitSounds.Length)];
                if (randomClip != null)
                {
                    // 가장자리 맞출수록 피치도 높게
                    audioSource.pitch = 1.0f + feverHitCount * 0.05f + ratio * 0.3f;
                    audioSource.PlayOneShot(randomClip, 0.7f + ratio * 0.3f);
                }
            }
            return;
        }

        Vector2 center = transform.position;

        // 영상 분석 3: 이전 속도를 무시하고 '즉각적으로(Instant Burst)' 타겟 속도로 변경
        float offsetX = hitPoint.x - center.x;

        // --- 더블 터치 (콤보) 시스템 로직 추가 ---
        float timeSinceLastHit = Time.time - lastHitTime;
        if (timeSinceLastHit <= comboTimeLimit)
        {
            currentCombo++; // 제한 시간 안에 치면 콤보 증가 (2번 타격)
            Debug.Log($"🔥 콤보 달성! 연속 타격 횟수: {currentCombo} / 쾅!");
        }
        else
        {
            currentCombo = 1; // 늦게 치면 콤보 초기화 (다시 첫 타격)
        }
        
        lastHitTime = Time.time; // 방금 맞은 시간 저장

        // [디버그] 쌍별 더블 탭 간격 측정
        hitCount++;
        if (hitCount % 2 == 1)
            pairStartTime = Time.time; // 홀수 터치: 타이머 시작
        else
            lastPairDeltaMs = (Time.time - pairStartTime) * 1000f; // 짝수 터치: 간격 기록

        // 콤보가 쌓일수록 보너스 높이가 수치로 추가됩니다 (물리 속도는 동일)
        float soundPitch = Random.Range(0.9f, 1.1f); // 기본 피치
        float soundVolume = 1.0f; // 기본 볼륨

        if (currentCombo >= 2)
        {
            // 물리 속도는 그대로, 보너스 높이만 수치로 계산
            float comboVelocity = jumpVelocity * comboMultiplier;
            float gravity = 9.81f * gravityScale;
            if (gravity > 0f)
            {
                float normalPeak = (jumpVelocity * jumpVelocity) / (2f * gravity);
                float comboPeak = (comboVelocity * comboVelocity) / (2f * gravity);
                comboBonusHeight += (comboPeak - normalPeak);
                Debug.Log($"🚀 콤보 보너스! +{comboPeak - normalPeak:F1}m (누적: {comboBonusHeight:F1}m)");
            }
            
            // 콤보 성공 사운드 재생 (총소리)
            if (comboSound != null && audioSource != null)
            {
                audioSource.pitch = soundPitch;
                audioSource.PlayOneShot(comboSound, soundVolume);
            }

            // 🎥 [속도감 연출] 카메라 흔들림 + 스피드라인 트리거
            if (Camera.main != null)
            {
                CameraController camCtrl = Camera.main.gameObject.GetComponent<CameraController>();
                if (camCtrl != null)
                    camCtrl.TriggerShake(0.6f, 0.15f);
            }

            // 초음속 스피드라인 연출
            SpeedLineManager speedLines = FindAnyObjectByType<SpeedLineManager>();
            if (speedLines != null)
                speedLines.TriggerComboLines(0.8f);
        }
        else
        {
            // 🔊 일반 효과음 랜덤 재생 (콤보가 아닐 때만)
            if (hitSounds != null && hitSounds.Length > 0 && audioSource != null)
            {
                AudioClip randomClip = hitSounds[Random.Range(0, hitSounds.Length)];
                if (randomClip != null)
                {
                    audioSource.pitch = soundPitch;
                    audioSource.PlayOneShot(randomClip, soundVolume); 
                }
            }
        }

        // 가장자리를 때렸을 때 맞은 반대 방향으로 튕겨 나가는 좌우 속도 계산
        float horizontalSpeed = -offsetX * horizontalBounceForce;

        // 콤보 시 약간 더 튀기 (1.0 = 동일, 1.2 = 20% 더 튀김 — 이 값을 조절하세요!)
        float comboPhysicsBoost = (currentCombo >= 2) ? 3.0f : 0.8f;
        rb.linearVelocity = new Vector2(horizontalSpeed, jumpVelocity * comboPhysicsBoost);

        // 영상 분석 4: 맞은 타점(중심점 대비 좌/우)에 따라 아케이드스러운 엄청난 회전(스핀) 부여
        rb.AddTorque(-offsetX * spinMultiplier, ForceMode2D.Impulse);

        SpawnHoleEffect(hitPoint);
    }

    private void SpawnHoleEffect(Vector2 worldPos)
    {
        if (holeSprite == null) return;

        GameObject hole = new GameObject("HoleEffect");
        hole.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        hole.transform.localScale = Vector3.one * holeEffectScale;

        SpriteRenderer sr = hole.AddComponent<SpriteRenderer>();
        sr.sprite = holeSprite;
        sr.sortingOrder = holeSortingOrder;

        // 타격 순간의 동전 중심 대비 오프셋을 월드 공간으로 고정 기록
        Vector3 offset = new Vector3(worldPos.x, worldPos.y, 0f) - transform.position;

        StartCoroutine(FadeAndDestroy(hole, sr, offset));
    }

    private IEnumerator FadeAndDestroy(GameObject obj, SpriteRenderer sr, Vector3 offset)
    {
        // 1초 동안 동전 위치를 따라가며 표시
        float waitElapsed = 0f;
        while (waitElapsed < 1.0f)
        {
            if (obj == null) yield break;
            obj.transform.position = transform.position + offset;
            waitElapsed += Time.deltaTime;
            yield return null;
        }

        // 0.5초에 걸쳐 따라가면서 서서히 사라짐
        float elapsed = 0f;
        const float fadeDuration = 0.5f;
        Color c = sr.color;

        while (elapsed < fadeDuration)
        {
            if (obj == null) yield break;
            obj.transform.position = transform.position + offset;
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            sr.color = c;
            yield return null;
        }

        Destroy(obj);
    }

    // 피버 종료 시 SkillBarManager가 호출하는 보너스 점프
    public static void ApplyFeverBonus(Rigidbody2D rb, float bonusVelocity)
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bonusVelocity);
    }

    private void OnDestroy()
    {
        if (touchVisualizer != null)
        {
            Destroy(touchVisualizer);
        }
    }
}
