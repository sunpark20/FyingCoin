using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 유니티 상단 메뉴에 버튼을 만들어, 한 번 클릭으로 씬 전체를 자동 생성해주는 매니저입니다.
public class AutoSetupCoinGame : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("CoinGame/👉 1초 만에 씬 자동 세팅하기!")]
    public static void SetupScene()
    {
        // 1. 완벽 탄성 / 마찰 0 인 물리 메테리얼 생성
        PhysicsMaterial2D mat = new PhysicsMaterial2D("AutoBouncy");
        mat.friction = 0f;
        mat.bounciness = 0.8f;

        // 존재하지 않을 때만 파일로 저장
        if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/AutoBouncy.physicsMaterial2D") == null)
        {
            AssetDatabase.CreateAsset(mat, "Assets/AutoBouncy.physicsMaterial2D");
        }
        else
        {
            mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/AutoBouncy.physicsMaterial2D");
        }

        // 2. 화면 이탈 방지 벽 3면 생성 (좌, 우, 하단)
        // 카메라 자식으로 넣으면 물리엔진 떨림(Jitter)이 발생하므로, 스케일을 아주 거대하게(길이 1000) 만듭니다.
        GameObject leftWall = CreateWall("LeftWall_Auto", new Vector2(-5f, 500f), new Vector2(1, 1000), mat);
        GameObject rightWall = CreateWall("RightWall_Auto", new Vector2(5f, 500f), new Vector2(1, 1000), mat);
        GameObject bottomWall = CreateWall("BottomWall_Auto", new Vector2(0, -6f), new Vector2(20, 1), mat);

        // 3. 동전 생성 (기존에 있으면 삭제하지 않고 유지하여 유저의 '물리 튜닝값'을 보호)
        GameObject coin = GameObject.Find("Coin");

        if (coin == null)
        {
            // 아예 없으면 새로 생성
            coin = new GameObject("Coin");
            coin.transform.position = new Vector3(0, 3f, 0); // 공중에서 시작
            
            // 동전 모양 시각화 (dena-front 스프라이트 사용)
            SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
            Sprite denaFront = Resources.Load<Sprite>("dena-front");
            if (denaFront != null)
            {
                sr.sprite = denaFront;
                // 512x512 스프라이트에 맞게 스케일 조정 (100 PPU 기준 → 5.12 유닛)
                // 기존 Knob(4x4) 대비 비슷한 화면 크기를 유지하도록 약 0.8 스케일
                coin.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            }
            else
            {
                // 폴백: dena-front가 없으면 기존 Knob 사용
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                coin.transform.localScale = new Vector3(4f, 4f, 1f);
                Debug.LogWarning("⚠️ dena-front.png를 Assets/Resources/ 폴더에 넣어주세요! 기본 Knob 사용 중.");
            }

            // 물리 컴포넌트 추가 및 덜덜거림(Jitter) 방지
            Rigidbody2D rb = coin.AddComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 고속 이동 시 카메라와 겹쳐서 떨리는 현상 방지

            CircleCollider2D col = coin.AddComponent<CircleCollider2D>();
            col.sharedMaterial = mat;

            // 유저님이 아까 유니티에 넣으신 CoinPhysics.cs 연동
            coin.AddComponent<CoinPhysics>();
            Debug.Log("✅ CoinPhysics 스크립트 연결 완료!");
        }
        else
        {
            Debug.Log("🪙 기존 동전이 발견되어 유저가 조절한 '물리 엔진 튜닝 값'을 안심하고 그대로 유지합니다!");
        }

        // [사운드 세팅 추가] CoinPhysics.cs의 hitSounds 배열을 자동 매핑
        CoinPhysics coinPhysics = coin.GetComponent<CoinPhysics>();
        if (coinPhysics != null)
        {
            // 두 가지 사운드 에셋 검색
            string[] soundNames = new string[] { "coin-flip_B_major", "Coin Flip  Free Sound Effect" };
            System.Collections.Generic.List<AudioClip> clipList = new System.Collections.Generic.List<AudioClip>();

            foreach (string sName in soundNames)
            {
                string[] audioGuids = AssetDatabase.FindAssets(sName + " t:AudioClip");
                if (audioGuids.Length > 0)
                {
                    string audioPath = AssetDatabase.GUIDToAssetPath(audioGuids[0]);
                    AudioClip loadedAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
                    if (loadedAudio != null)
                        clipList.Add(loadedAudio);
                    else
                        Debug.LogWarning($"⚠️ '{sName}' 파일을 찾았지만 AudioClip으로 로드 실패: {audioPath}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ '{sName}' 사운드 파일을 찾을 수 없어 제외했습니다.");
                }
            }

            if (clipList.Count > 0)
            {
                coinPhysics.hitSounds = clipList.ToArray();
                Debug.Log($"🎵 동전 튕기기 랜덤 사운드 {clipList.Count}개 매핑 완료!");
            }

            // [콤보 사운드 세팅 추가] gunshot 사운드 매핑
            string[] comboGuids = AssetDatabase.FindAssets("gshot t:AudioClip");
            if (comboGuids.Length > 0)
            {
                string comboPath = AssetDatabase.GUIDToAssetPath(comboGuids[0]);
                AudioClip comboAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(comboPath);
                if (comboAudio != null)
                {
                    coinPhysics.comboSound = comboAudio;
                    Debug.Log("🔫 더블탭(콤보) 전용 총소리 매핑 완료!");
                }
                else
                {
                    Debug.LogWarning($"⚠️ 'gunshot' 파일을 찾았지만 AudioClip으로 로드 실패: {comboPath}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 'gunshot' 사운드 파일을 찾을 수 없어 콤보 사운드를 연결하지 못했습니다.");
            }
        }

        // 4. 메인 카메라 배경색 수정, 추적 스크립트 및 배경 매니저 연결
        if (Camera.main != null)
        {
            // 배경색은 BackgroundManager에서 고도에 따라 자동 설정되므로 고정색 세팅은 제거
            // 4-1. 카메라 추적 스크립트 부착 (기존 컴포넌트 유지하여 Inspector 값 보존)
            CameraController camCtrl = Camera.main.GetComponent<CameraController>();
            if (camCtrl == null)
                camCtrl = Camera.main.gameObject.AddComponent<CameraController>();
            camCtrl.target = coin.transform;
            Debug.Log("🎥 카메라 무한 추적 세팅 완료!");

            // 4-2. 배경색 변화 매니저 부착 (기존 컴포넌트 유지하여 Inspector 값 보존)
            BackgroundManager bgManager = Camera.main.GetComponent<BackgroundManager>();
            if (bgManager == null)
                bgManager = Camera.main.gameObject.AddComponent<BackgroundManager>();
            bgManager.target = coin.transform;
            Debug.Log("🌌 고도별 대기권 배경색 변환 매니저 세팅 완료!");

            // 4-3. 고도별 오브젝트 스포너 부착 (기존 컴포넌트 유지하여 Inspector 값 보존)
            AtmosphereObjectSpawner spawner = Camera.main.gameObject.GetComponent<AtmosphereObjectSpawner>();
            if (spawner == null)
                spawner = Camera.main.gameObject.AddComponent<AtmosphereObjectSpawner>();
            spawner.target = coin.transform;
            Debug.Log("🛸 고도별 대기권 배경 오브젝트 스포너 세팅 완료!");

            // 4-4. 가속 시 화면 집중선 이펙트 매니저 부착 (기존 컴포넌트 유지하여 Inspector 값 보존)
            SpeedLineManager speedLineMgr = Camera.main.gameObject.GetComponent<SpeedLineManager>();
            if (speedLineMgr == null)
                speedLineMgr = Camera.main.gameObject.AddComponent<SpeedLineManager>();
            speedLineMgr.target = coin.transform;
            Debug.Log("⚡ 최고 속도 시 발생하는 스피드 집중선 효과 세팅 완료!");

            // 4-5. 게임 시작 오프닝 매니저 및 폰트 부착 (기존 컴포넌트 유지하여 Inspector 값 보존)
            OpeningManager openingMgr = Camera.main.gameObject.GetComponent<OpeningManager>();
            if (openingMgr == null)
                openingMgr = Camera.main.gameObject.AddComponent<OpeningManager>();
            
            // 4-6. 모바일 해상도(가로폭 고정) 카메라 세팅 부착 (기존 컴포넌트 유지하여 Inspector 값 보존)
            MobileCameraAspect aspectMgr = Camera.main.gameObject.GetComponent<MobileCameraAspect>();
            if (aspectMgr == null)
            {
                aspectMgr = Camera.main.gameObject.AddComponent<MobileCameraAspect>();
                // 벽 외측 엣지(5.5) = 벽 중심(5) + 반두께(0.5) → 화면 가장자리에 벽이 딱 맞게 배치됨
                aspectMgr.targetHalfWidth = 5.5f;
            }
            Debug.Log("📱 모바일 화면 비율 유지(MobileCameraAspect) 매니저 세팅 완료!");
            
            // 폰트 찾기 (TMP_FontAsset으로 검색 - Unity에서 2002.ttf를 TMP 폰트 에셋으로 변환 필요)
            string[] fontGuids = AssetDatabase.FindAssets("2002 t:TMP_FontAsset");
            if (fontGuids.Length > 0)
            {
                string fontPath = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
                TMPro.TMP_FontAsset loadedFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);
                if (loadedFont != null)
                {
                    // 폰트는 Resources.Load로 런타임 로드되므로 Inspector 할당 불필요
                    Debug.Log("폰트 확인 완료 (런타임 Resources.Load 사용): " + fontPath);
                }
                else
                {
                    Debug.LogWarning($"⚠️ '2002' 폰트 에셋을 찾았지만 TMP_FontAsset으로 로드 실패: {fontPath}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ '2002' TMP 폰트 에셋을 찾을 수 없습니다. Window > TextMeshPro > Font Asset Creator로 변환 후 재시도하세요.");
            }
            Debug.Log("🎬 오프닝 타이핑 연출 세팅 완료!");
        }

        // 5~6. SkillBarManager / CrosshairManager는 런타임에 GameManager.BootstrapManagers()가 자동 생성
        // (에디터에서 미리 만들면 Start()에서 또 만들어져서 중복됨)
        Debug.Log("ℹ️ SkillBarManager / CrosshairManager는 Play 시 GameManager가 자동 생성합니다.");

        Debug.Log("🎉 [자동 세팅 완료] 이제 화면 상단의 Play(▶️) 버튼을 눌러보세요!");
    }

    private static GameObject CreateWall(string name, Vector2 pos, Vector2 size, PhysicsMaterial2D mat)
    {
        // 혹시 기존에 같은 이름이 있다면 삭제 후 재생성 (중복 생성 방지)
        GameObject existing = GameObject.Find(name);
        if (existing != null) DestroyImmediate(existing);

        GameObject wall = new GameObject(name);
        wall.transform.position = pos;
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = mat;

        // 벽이 눈에 보이도록 추가 (흰색 사각형)
        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
#endif
        sr.color = new Color(0.2f, 0.8f, 1f, 0.9f); // 선명한 네온 블루 (거의 불투명)
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);
        
        return wall;
    }
#endif
}
