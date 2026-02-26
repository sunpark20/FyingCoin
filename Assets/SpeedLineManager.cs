using UnityEngine;

// 엄청난 속도로 상승/하강할 때 세로 집중선(Speed Lines)을 생성하여 아케이드 속도감을 극대화하는 스크립트
public class SpeedLineManager : MonoBehaviour
{
    [Header("추적할 대상 (동전)")]
    public Transform target;
    private Rigidbody2D targetRb;

    private Sprite squareSprite;
    private float nextSpawnTime;

    // 콤보 스피드라인 강제 트리거
    private float comboLineTimer = 0f;

    void Start()
    {
        // 씬 자동 셋팅시 사용하는 기본 사각형 스프라이트 로드
#if UNITY_EDITOR
        squareSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
#endif
        // iOS 빌드에서도 동작하도록 Resources 폴백
        if (squareSprite == null)
            squareSprite = Resources.Load<Sprite>("square");
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        if (targetRb == null || squareSprite == null) return;

        // --- 콤보 스피드라인 (강제 트리거) ---
        if (comboLineTimer > 0f)
        {
            comboLineTimer -= Time.deltaTime;
            if (Time.time >= nextSpawnTime)
            {
                SpawnComboSpeedLine();
                nextSpawnTime = Time.time + Random.Range(0.015f, 0.04f); // 매우 빠른 간격
            }
            return; // 콤보 중에는 일반 스피드라인 스킵
        }

        // --- 일반 스피드라인 (속도 기반) ---
        float speed = Mathf.Abs(targetRb.linearVelocity.y);
        
        if (speed > 15f)
        {
            if (Time.time >= nextSpawnTime)
            {
                float direction = -Mathf.Sign(targetRb.linearVelocity.y);
                SpawnSpeedLine(speed, direction);
                
                float delay = Mathf.Lerp(0.05f, 0.01f, (speed - 15f) / 30f);
                nextSpawnTime = Time.time + delay;
            }
        }
    }

    /// <summary>콤보 발동 시 일정 시간 동안 초음속 스피드라인 폭발 연출</summary>
    public void TriggerComboLines(float duration)
    {
        comboLineTimer = duration;
        Debug.Log($"💨 콤보 스피드라인 발동! {duration}초");
    }

    private void SpawnComboSpeedLine()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float spawnX = Random.Range(-8f, 8f);
        // 위에서 아래로 쏟아지는 라인 (위로 올라가는 느낌)
        float spawnY = cam.transform.position.y + Random.Range(10f, 18f);

        GameObject lineGo = new GameObject("ComboSpeedLine");
        lineGo.transform.position = new Vector3(spawnX, spawnY, 8f);
        
        // 일반보다 더 굵고 긴 라인
        lineGo.transform.localScale = new Vector3(
            Random.Range(0.03f, 0.12f), 
            Random.Range(5f, 18f), 
            1f
        );

        SpriteRenderer sr = lineGo.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        // 노란/주황색 계열로 초음속 돌파 느낌
        float r = Random.Range(0.9f, 1f);
        float g = Random.Range(0.5f, 0.9f);
        float b = Random.Range(0.0f, 0.2f);
        sr.color = new Color(r, g, b, Random.Range(0.3f, 0.8f));
        sr.sortingOrder = -4; // 동전보다 뒤, 일반 라인보다 앞

        AtmosphereProp prop = lineGo.AddComponent<AtmosphereProp>();
        // 아래로 빠르게 쏟아짐 (초음속 체감)
        prop.velocity = new Vector2(0, -Random.Range(30f, 60f));
    }

    private void SpawnSpeedLine(float currentSpeed, float directionY)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float spawnX = Random.Range(-8f, 8f);
        float spawnYOffset = Random.Range(10f, 15f) * -directionY;
        float spawnY = cam.transform.position.y + spawnYOffset;
        
        GameObject lineGo = new GameObject("SpeedLine_Effect");
        lineGo.transform.position = new Vector3(spawnX, spawnY, 8f);
        
        lineGo.transform.localScale = new Vector3(Random.Range(0.02f, 0.08f), Random.Range(3f, 12f), 1f);

        SpriteRenderer sr = lineGo.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(1f, 1f, 1f, Random.Range(0.2f, 0.6f));
        sr.sortingOrder = -5;

        AtmosphereProp prop = lineGo.AddComponent<AtmosphereProp>();
        prop.velocity = new Vector2(0, currentSpeed * 2.5f * directionY); 
    }
}
