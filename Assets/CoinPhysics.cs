using UnityEngine;
using UnityEngine.InputSystem;

// [유니티 완벽 물리 프로토타입] 
// 동전에 이 스크립트를 넣고 Rigidbody2D와 CircleCollider2D를 추가합니다.
public class CoinPhysics : MonoBehaviour
{
    private Rigidbody2D rb;

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

        // --- 2.5D 시각적 3D 회전 효과 구현 ---
        // 물리엔진의 속도 비율에 맞춰서, 유니티 2D 공간의 오브젝트를 X축으로 강제 회전시켜 3D처럼 보이게 만듭니다.
        // 현재 Y축 방향 속도가 빠를수록 더 격렬하게 덤블링합니다.
        if (Mathf.Abs(rb.linearVelocity.y) > 0.1f)
        {
            float flipAmount = visual3DSpinSpeed * Time.deltaTime;
            // 위로 올라갈 땐 뒤로 돌고, 내려갈 땐 앞으로 돌게 방향 설정
            float direction = rb.linearVelocity.y > 0 ? -1f : 1f;
            transform.Rotate(direction * flipAmount, 0, 0, Space.Self);
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
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null && hit.collider.gameObject == this.gameObject)
        {
            Debug.Log($"동전 맞춤! 좌표: {worldPos}");
            HitCoin(worldPos);
        }
        else
        {
            Debug.Log($"빗나감! 좌표: {worldPos}, 충돌체: {(hit.collider != null ? hit.collider.name : "없음")}");
        }
    }

    private void HitCoin(Vector2 hitPoint)
    {
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

        // 콤보가 쌓일수록 점프력이 '곱빼기'로 강해집니다
        float finalJumpVelocity = jumpVelocity;
        float soundPitch = Random.Range(0.9f, 1.1f); // 기본 피치
        float soundVolume = 1.0f; // 기본 볼륨

        if (currentCombo >= 2)
        {
            // 두 번 이상 맞추면 배율(기본 1.5배) 적용
            finalJumpVelocity = jumpVelocity * comboMultiplier;
            
            // 🔊 타격감이 쎄졌으므로 사운드 피치와 볼륨도 다이내믹하게 증폭
            soundPitch *= comboMultiplier; // 더 높고 경쾌한 톤
            soundVolume *= comboMultiplier; // 더 큰 소리
            
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
    }
}
