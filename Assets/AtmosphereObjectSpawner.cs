using UnityEngine;

// 씬에 생성된 배경 오브젝트(구름, 새, 유성 등)가 동작하고 화면을 벗어나면 삭제되는 스크립트
public class AtmosphereProp : MonoBehaviour
{
    public Vector2 velocity;
    public float rotateSpeed;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // 이동 및 회전
        transform.Translate(velocity * Time.deltaTime, Space.World);
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

        // 카메라(화면) 기준점 아래로 너무 많이 내려가면 삭제 (최적화)
        if (cam != null && transform.position.y < cam.transform.position.y - 15f)
        {
            Destroy(gameObject);
        }
    }
}

// 고도에 따라 알맞은 배경 오브젝트들을 화면 위쪽에 지속적으로 스폰하는 매니저
public class AtmosphereObjectSpawner : MonoBehaviour
{
    [Header("추적할 대상 (동전)")]
    public Transform target;
    
    [Header("스폰 설정")]
    public float minSpawnDelay = 0.5f;
    public float maxSpawnDelay = 2.0f;
    private float nextSpawnTime;

    // 사용할 기본 스프라이트 참조
    private Sprite circleSprite;
    private Sprite squareSprite;

    // bg 에셋 스프라이트
    private Sprite clouds1Sprite;
    private Sprite clouds2Sprite;
    private Sprite planetsSprite;

    void Start()
    {
        // bg 에셋 스프라이트 로드
        clouds1Sprite = Resources.Load<Sprite>("bg/clouds_1");
        clouds2Sprite = Resources.Load<Sprite>("bg/clouds_2");
        planetsSprite = Resources.Load<Sprite>("bg/planets");

        // 빌트인 스프라이트 로드 (새, 유성, 제트기 등 에셋 없는 오브젝트용 폴백)
#if UNITY_EDITOR
        circleSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        squareSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
#endif
        // iOS 빌드에서도 동작하도록 Resources 폴백
        if (circleSprite == null)
            circleSprite = Resources.Load<Sprite>("circle");
        if (squareSprite == null)
            squareSprite = Resources.Load<Sprite>("square");
        // 에셋이 없을 경우 clouds를 circleSprite 폴백으로 사용
        if (clouds1Sprite == null) clouds1Sprite = circleSprite;
        if (clouds2Sprite == null) clouds2Sprite = circleSprite;
        if (planetsSprite == null) planetsSprite = circleSprite;

        nextSpawnTime = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    void Update()
    {
        if (target == null) return;

        // 동전이 매우 빠르게 올라갈수록 스폰 주기를 짧게 해서 빽빽하게 보이게 만듦
        float speedMultiplier = 1f;
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb != null && rb.linearVelocity.y > 10f)
        {
            speedMultiplier = 10f / rb.linearVelocity.y; // 속도가 높을수록 딜레이 감소
        }

        if (Time.time >= nextSpawnTime)
        {
            SpawnObject(target.position.y);
            float delay = Random.Range(minSpawnDelay, maxSpawnDelay) * Mathf.Clamp(speedMultiplier, 0.1f, 1f);
            nextSpawnTime = Time.time + delay;
        }
    }

    private void SpawnObject(float currentAltitude)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 스폰 위치는 카메라보다 좀 더 위쪽 상공
        float spawnY = cam.transform.position.y + 12f;
        float spawnX = Random.Range(-8f, 8f);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 10f); // z=10 으로 동전보다 뒤에 배치

        GameObject propGo = new GameObject("AtmosphereProp");
        propGo.transform.position = spawnPos;

        SpriteRenderer sr = propGo.AddComponent<SpriteRenderer>();
        AtmosphereProp prop = propGo.AddComponent<AtmosphereProp>();

        // Z 인덱스를 낮춰서 배경처럼 보이게 (동전이 가리도록)
        sr.sortingOrder = -10;

        // --- 고도별 오브젝트 모양 및 움직임 결정 ---
        if (currentAltitude < 1200f)
        {
            // 대류권 (구름 또는 새)
            if (Random.value > 0.3f)
            {
                // 구름 (픽셀아트 clouds_1 / clouds_2 랜덤)
                sr.sprite = Random.value > 0.5f ? clouds1Sprite : clouds2Sprite;
                sr.color = new Color(1f, 1f, 1f, Random.Range(0.5f, 0.9f));
                propGo.transform.localScale = Vector3.one * Random.Range(1.5f, 3.5f);
                prop.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f)); // 천천히 표류
            }
            else
            {
                // 실루엣 새 (검은빛 타원) — 에셋 없으므로 기존 유지
                sr.sprite = circleSprite;
                sr.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
                propGo.transform.localScale = new Vector3(1f, 0.2f, 1f); // 얇게
                prop.velocity = new Vector2(Random.Range(2f, 5f) * Mathf.Sign(Random.Range(-1f, 1f)), Random.Range(0.5f, 1.5f)); // 좌우로 날아감
            }
        }
        else if (currentAltitude < 5000f)
        {
            // 성층권 (높은 구름 또는 고고도 제트기)
            if (Random.value > 0.5f)
            {
                // 높은 구름 (clouds_2)
                sr.sprite = clouds2Sprite;
                sr.color = new Color(1f, 1f, 1f, Random.Range(0.4f, 0.8f));
                propGo.transform.localScale = Vector3.one * Random.Range(2f, 4f);
                prop.velocity = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.3f, 0.8f)); // 천천히 표류
            }
            else
            {
                // 제트기 (어두운 색) — 에셋 없으므로 기존 유지
                sr.sprite = squareSprite;
                sr.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                propGo.transform.localScale = new Vector3(2f, 0.5f, 1f);
                float speedX = Random.Range(5f, 10f) * Mathf.Sign(Random.Range(-1f, 1f));
                prop.velocity = new Vector2(speedX, 0); // 빠른 가로 이동
            }
        }
        else if (currentAltitude < 8500f)
        {
            // 중간권 (별똥별/유성)
            sr.sprite = circleSprite;
            sr.color = new Color(1f, 0.8f, 0.2f, 1f); // 노란/주황 빛
            propGo.transform.localScale = new Vector3(0.5f, 2f, 1f); // 길쭉하게
            // 사선으로 엄청 빠르게 떨어짐
            prop.velocity = new Vector2(Random.Range(-5f, 5f), Random.Range(-20f, -10f));
            
            // 이동 방향(속도)에 맞춰 회전 세팅 (진행 방향 응시)
            float angle = Mathf.Atan2(prop.velocity.y, prop.velocity.x) * Mathf.Rad2Deg;
            propGo.transform.rotation = Quaternion.Euler(0, 0, angle + 90f); 
        }
        else
        {
            // 열권, 외기권 이상 (행성, 인공위성, 우주 배경의 별)
            float rand = Random.value;
            if (rand > 0.9f) // 10% 확률로 거대한 행성 등장
            {
                sr.sprite = planetsSprite;
                sr.color = new Color(1f, 1f, 1f, Random.Range(0.6f, 1f));
                propGo.transform.localScale = Vector3.one * Random.Range(3f, 8f); // 행성 크기 크게
                prop.velocity = new Vector2(0f, Random.Range(-0.1f, -0.5f)); // 매우 느리게 이동
                sr.sortingOrder = -15; // 별보다는 앞, 동전보다는 뒤
            }
            else if (rand > 0.7f) // 20% 확률로 인공위성/우주쓰레기
            {
                sr.sprite = squareSprite;
                sr.color = new Color(0.6f, 0.6f, 0.6f, 1f); // 회색
                propGo.transform.localScale = new Vector3(Random.Range(0.5f, 2f), Random.Range(0.5f, 2f), 1f);
                prop.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.5f, -2f)); // 서서히 표류
                prop.rotateSpeed = Random.Range(-45f, 45f); // 뱅글뱅글 돎
            }
            else // 70% 확률로 심우주의 작은 별들 (패럴랙스 용도)
            {
                sr.sprite = circleSprite;
                sr.color = new Color(1f, 1f, 1f, Random.Range(0.4f, 1f)); // 반짝이는 하얀 별
                propGo.transform.localScale = Vector3.one * Random.Range(0.1f, 0.4f); // 매우 작게
                prop.velocity = Vector2.zero; // 완벽히 움직이지 않음 (카메라가 올라가며 패럴랙스로 통과함)
                sr.sortingOrder = -20; // 가장 뒷 배경에 배치
            }
        }
    }
}
