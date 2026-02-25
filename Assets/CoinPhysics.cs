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

    [Header("산도알 스타일 물리 파라미터")]
    [Tooltip("기초 중력 조절 (기본 2.0 - 숫자가 클수록 무겁고 빨리 떨어짐)")]
    public float gravityScale = 2.0f; 
    
    [Tooltip("첫 터치 시 위로 솟구치는 기본 속도 (기본 10)")]
    public float jumpVelocity = 10f; 
    
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
    
    [Tooltip("최대 하강 속도 제한 (너무 빨리 떨어져서 누르기 힘든 현상 방지)")]
    public float maxFallSpeed = -10f;

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

        // 기본 Y스케일 저장 (두께감 계산용)
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

        // 에디터/PC 마우스
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            TryHitAt(pos);
        }

        // iOS 실기기 터치
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(ts.primaryTouch.position.ReadValue());
            TryHitAt(pos);
        }
    }

    private void TryHitAt(Vector2 worldPos)
    {
        // 정면 기준 원형 터치 영역 (Y스케일 변해도 항상 동일한 크기)
        float dist = Vector2.Distance(worldPos, (Vector2)transform.position);
        if (dist <= touchRadius)
        {
            Debug.Log($"동전 맞춤! 좌표: {worldPos}");
            HitCoin(worldPos);
        }
        else
        {
            Debug.Log($"빗나감! 좌표: {worldPos}, 충돌체: 없음");
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

        // 콤보가 쌓일수록 점프력이 '곱빼기'로 강해집니다
        float finalJumpVelocity = jumpVelocity;
        float soundPitch = Random.Range(0.9f, 1.1f); // 기본 피치
        float soundVolume = 1.0f; // 기본 볼륨

        if (currentCombo >= 2)
        {
            // 두 번 이상 맞추면 배율(기본 1.5배) 적용
            finalJumpVelocity = jumpVelocity * comboMultiplier;
            
            // 🔊 타격감이 쎄졌으므로 사운드 피치와 볼륨도 다이내믹하게 증폭
            //soundPitch *= comboMultiplier; // 더 높고 경쾌한 톤
            //soundVolume *= comboMultiplier; // 더 큰 소리
            
            // 콤보 성공 사운드 재생 (총소리)
            if (comboSound != null && audioSource != null)
            {
                audioSource.pitch = soundPitch;
                audioSource.PlayOneShot(comboSound, soundVolume);
            }

            // 🎥 [속도감 연출] 카메라 흔들림(Shake) 작동
            if (Camera.main != null)
            {
                CameraController camCtrl = Camera.main.gameObject.GetComponent<CameraController>();
                if (camCtrl != null)
                {
                    // 총소리와 함께 화면을 짧고 강하게 흔듭니다. (크기, 지속시간)
                    camCtrl.TriggerShake(0.6f, 0.15f);
                }
            }
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

        // 이전 중력을 무시하고 위로, 그리고 좌우로 즉각적으로(Instant Burst) 타겟 속도를 덮어씌움
        rb.linearVelocity = new Vector2(horizontalSpeed, finalJumpVelocity);

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
}
