using UnityEngine;

// 엄청난 속도로 상승/하강할 때 세로 집중선(Speed Lines)을 생성하여 아케이드 속도감을 극대화하는 스크립트
public class SpeedLineManager : MonoBehaviour
{
    [Header("추적할 대상 (동전)")]
    public Transform target;
    private Rigidbody2D targetRb;

    private Sprite squareSprite;
    private float nextSpawnTime;

    void Start()
    {
        // 씬 자동 셋팅시 사용하는 기본 사각형 스프라이트 로링
#if UNITY_EDITOR
        squareSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
#endif
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        if (targetRb == null || squareSprite == null) return;

        // 속도(절댓값)가 15 이상일 때만 화면에 집중선 이펙트가 무수히 쏟아지도록 함
        float speed = Mathf.Abs(targetRb.linearVelocity.y);
        
        if (speed > 15f)
        {
            if (Time.time >= nextSpawnTime)
            {
                // 속도의 방향에 따라 선이 이동할 방향 결정 (올라갈 땐 선이 아래로, 떨어질 땐 선이 위로)
                float direction = -Mathf.Sign(targetRb.linearVelocity.y);
                SpawnSpeedLine(speed, direction);
                
                // 속도가 빠를수록 더 짧은 간격으로 폭풍처럼 생성
                float delay = Mathf.Lerp(0.05f, 0.01f, (speed - 15f) / 30f);
                nextSpawnTime = Time.time + delay;
            }
        }
    }

    private void SpawnSpeedLine(float currentSpeed, float directionY)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 스폰 위치: 카메라의 위(올라갈때) 또는 아래(떨어질때) 화면 밖
        float spawnX = Random.Range(-8f, 8f);
        float spawnYOffset = Random.Range(10f, 15f) * -directionY; // 선이 날아올 시작점 역계산
        float spawnY = cam.transform.position.y + spawnYOffset;
        
        GameObject lineGo = new GameObject("SpeedLine_Effect");
        lineGo.transform.position = new Vector3(spawnX, spawnY, 8f); // 배경 근처 심도 깊게
        
        // 집중선 모양: 매우 얇고 세로로 아주 긴 직사각형 구조
        lineGo.transform.localScale = new Vector3(Random.Range(0.02f, 0.08f), Random.Range(3f, 12f), 1f);

        SpriteRenderer sr = lineGo.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        // 반투명한 하얀색 집중선
        sr.color = new Color(1f, 1f, 1f, Random.Range(0.2f, 0.6f));
        sr.sortingOrder = -5; // 동전보다 뒤쪽

        // 기작성된 배경 움직임 스크립트 재활용
        AtmosphereProp prop = lineGo.AddComponent<AtmosphereProp>();
        // 동전의 현재 속도보다 약 2배 빠른 체감 속도로 수직 스쳐지나감
        prop.velocity = new Vector2(0, currentSpeed * 2.5f * directionY); 
    }
}
