using UnityEngine;
using Mkey;

/// <summary>
/// BackgroundManager + AtmosphereObjectSpawner 를 대체.
/// 고도별 5개 Zone의 패럴랙스 배경을 런타임에 생성하고,
/// coin 고도에 따라 Zone 간 크로스페이드 + 카메라 배경색 전환.
/// </summary>
public class SpaceParallaxController : MonoBehaviour
{
    public Transform target; // coin

    // ── Zone 정의 ──
    private static readonly float[] zoneMinAlt  = { 0f, 1200f, 5000f, 8500f, 20000f };
    private static readonly float[] zoneMaxAlt  = { 1200f, 5000f, 8500f, 20000f, 999999f };
    private static readonly int[]   zoneBkgId   = { 1, 3, 5, 7, 8 };
    private static readonly Color[] zoneBgColor =
    {
        new Color(0.529f, 0.808f, 0.922f, 1f), // #87CEEB
        new Color(0.063f, 0.306f, 0.545f, 1f), // #104E8B
        new Color(0f,     0f,     0.502f, 1f),  // #000080
        new Color(0.102f, 0f,     0.200f, 1f),  // #1A0033
        Color.black                              // #000000
    };
    private const float FADE_RANGE = 200f;
    private const float MAP_SIZE   = 20.48f;
    private const int   ZONE_COUNT = 5;

    // 각 Zone 의 레이어 이름 (BKG별로 파일명이 약간 다르므로 매핑)
    // 순서: SmallStars, StarMiddle, BigStars, Nebula, Planet/Sun, Meteors/Asteroid
    private static readonly string[][] layerNames =
    {
        // BKG 1
        new[] { "Bkg 1 Star Small",  "Bkg 1 Star Middle", "Bkg 1 Big Stars", "Bkg 1 Nebula", "Bkg 1 Planet",  "Bkg 1 Meteors" },
        // BKG 3
        new[] { "Bkg 3 Small Stars", "Bkg 3 Star Middle", "Bkg 3 Big Stars", "Bkg 3 Nebula", "Bkg 3 Planet",  "Bkg 3 Meteors" },
        // BKG 5
        new[] { "Bkg 5 Small Stars", "Bkg 5 Star Middle", "Bkg 5 Big Stars", "Bkg 5 Nebula", null,            "Bkg 5 Meteors" },
        // BKG 7
        new[] { "Bkg 7 Small Stars", "Bkg 7 Middle Stars","Bkg 7 Big Stars", "Bkg 7 Nebula", "Bkg 7 Planet",  "Bkg 7 Meteor"  },
        // BKG 8
        new[] { "Bkg 8 Small Stars", "Bkg 8 Star Middle", "Bkg 8 Big Stars", "Bkg 8 Nebula", "Bkg 8 Planet",  "Bkg 8 Meteor"  },
    };

    // 추가 보너스 레이어 (Sun, Asteroid, SuperNova 등 — Planet 슬롯에 이미 넣거나 별도 로드)
    // BKG 3: Sun, BKG 7: Sun+Asteroid, BKG 8: Super Nova
    // 이들은 Planet 슬롯 대신 추가 레이어로 취급
    private static readonly string[][] bonusLayerNames =
    {
        null,
        new[] { "Bkg 3 Sun" },
        null,
        new[] { "Bkg 7 Sun", "Bkg 7 Asteriod" },
        new[] { "Bkg 8 Super Nova" },
    };

    private static readonly int[]   layerSortOrder    = { -100, -90, -80, -70, -60, -50 };
    private static readonly float[] layerParallaxRate  = { 0.9f, 0.7f, 0.5f, 0.3f, 0.15f, 0.05f };

    // ── 런타임 ──
    private GameObject[]      zoneRoots;
    private SpriteRenderer[][] zoneRenderers; // 모든 SpriteRenderer (alpha 제어용)
    private int currentZone = 0;
    private int prevZone    = -1;

    void Awake()
    {
        zoneRoots     = new GameObject[ZONE_COUNT];
        zoneRenderers = new SpriteRenderer[ZONE_COUNT][];

        for (int z = 0; z < ZONE_COUNT; z++)
        {
            BuildZone(z);
            SetZoneActive(z, z == 0);
            SetZoneAlpha(z, z == 0 ? 1f : 0f);
        }
    }

    void BuildZone(int zoneIdx)
    {
        int bkgId = zoneBkgId[zoneIdx];
        string bkgFolder = "Parallax/BKG " + bkgId + "/";
        string[] names = layerNames[zoneIdx];

        // Zone root
        string[] zoneLabels = { "Troposphere", "Stratosphere", "Mesosphere", "Thermosphere", "Space" };
        GameObject zoneRoot = new GameObject("Zone_" + zoneIdx + "_" + zoneLabels[zoneIdx]);
        zoneRoot.transform.SetParent(transform);

        // 기본 6레이어 중 null 아닌 것 + 보너스 레이어 수 계산
        int baseCount = 0;
        for (int i = 0; i < names.Length; i++)
            if (names[i] != null) baseCount++;

        string[] bonus = bonusLayerNames[zoneIdx];
        int bonusCount = bonus != null ? bonus.Length : 0;
        int totalLayers = baseCount + bonusCount;

        ParallaxPlane[] planeComponents = new ParallaxPlane[totalLayers];
        var rendererList = new System.Collections.Generic.List<SpriteRenderer>();

        int planeIdx = 0;

        // 기본 레이어
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == null) continue;

            Sprite sprite = Resources.Load<Sprite>(bkgFolder + names[i]);
            if (sprite == null)
            {
                Debug.LogWarning($"[SpaceParallax] Sprite not found: {bkgFolder + names[i]}");
                // 빈 플레인이라도 만들어야 배열 인덱스가 맞음
            }

            int sortOrder = i < layerSortOrder.Length ? layerSortOrder[i] : -50 + planeIdx;
            float parallax = i < layerParallaxRate.Length ? layerParallaxRate[i] : 0.05f;

            ParallaxPlane pp = CreatePlane(zoneRoot.transform, names[i], sprite, sortOrder, rendererList);
            planeComponents[planeIdx] = pp;
            planeIdx++;
        }

        // 보너스 레이어 (Planet/Sun 슬롯 근처 sortOrder)
        if (bonus != null)
        {
            for (int b = 0; b < bonus.Length; b++)
            {
                Sprite sprite = Resources.Load<Sprite>(bkgFolder + bonus[b]);
                int sortOrder = -55 - b;
                ParallaxPlane pp = CreatePlane(zoneRoot.transform, bonus[b], sprite, sortOrder, rendererList);
                planeComponents[planeIdx] = pp;
                planeIdx++;
            }
        }

        // SpriteParallax 컴포넌트 추가 및 초기화
        SpriteParallax sp = zoneRoot.AddComponent<SpriteParallax>();
        sp.Initialize(planeComponents, MAP_SIZE, MAP_SIZE);

        zoneRoots[zoneIdx] = zoneRoot;
        zoneRenderers[zoneIdx] = rendererList.ToArray();
    }

    ParallaxPlane CreatePlane(Transform parent, string name, Sprite sprite, int sortOrder,
                              System.Collections.Generic.List<SpriteRenderer> rendererList)
    {
        GameObject planeObj = new GameObject("Plane_" + name);
        planeObj.transform.SetParent(parent);
        ParallaxPlane pp = planeObj.AddComponent<ParallaxPlane>();

        // Container (SpriteRenderer 보유)
        GameObject container = new GameObject("Container");
        container.transform.SetParent(planeObj.transform);
        SpriteRenderer sr = container.AddComponent<SpriteRenderer>();
        if (sprite != null)
            sr.sprite = sprite;
        sr.sortingOrder = sortOrder;
        sr.drawMode = SpriteDrawMode.Simple;

        rendererList.Add(sr);
        pp.SetMainContainer(container.transform);

        return pp;
    }

    void Update()
    {
        if (target == null) return;

        float altitude = target.position.y;

        // 현재 Zone 결정
        int newZone = 0;
        for (int z = ZONE_COUNT - 1; z >= 0; z--)
        {
            if (altitude >= zoneMinAlt[z])
            {
                newZone = z;
                break;
            }
        }

        // Zone 변경 시
        if (newZone != currentZone)
        {
            // 이전 전환 중이던 Zone 비활성화
            if (prevZone >= 0 && prevZone != newZone && prevZone != currentZone)
            {
                SetZoneActive(prevZone, false);
                SetZoneAlpha(prevZone, 0f);
            }
            prevZone = currentZone;
            currentZone = newZone;
            SetZoneActive(currentZone, true);
        }

        // 크로스페이드 계산
        float boundary = zoneMinAlt[currentZone];
        float distFromBoundary = altitude - boundary;

        if (prevZone >= 0 && prevZone != currentZone && distFromBoundary < FADE_RANGE && distFromBoundary >= 0)
        {
            // 전환 구간 내
            float t = distFromBoundary / FADE_RANGE;
            SetZoneAlpha(currentZone, t);
            SetZoneAlpha(prevZone, 1f - t);
            SetZoneActive(prevZone, true);
        }
        else if (prevZone >= 0 && prevZone != currentZone && currentZone > prevZone && distFromBoundary < 0)
        {
            // 경계 아래로 내려감 (Zone 복귀)
            SetZoneAlpha(currentZone, 0f);
            SetZoneAlpha(prevZone, 1f);
        }
        else
        {
            // 전환 완료
            SetZoneAlpha(currentZone, 1f);
            if (prevZone >= 0 && prevZone != currentZone)
            {
                SetZoneActive(prevZone, false);
                SetZoneAlpha(prevZone, 0f);
                prevZone = -1;
            }
        }

        // 카메라 배경색 보간
        UpdateCameraBackgroundColor(altitude);
    }

    void UpdateCameraBackgroundColor(float altitude)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 현재 Zone 과 다음 Zone 사이 보간
        int z = currentZone;
        if (z >= ZONE_COUNT - 1)
        {
            cam.backgroundColor = zoneBgColor[ZONE_COUNT - 1];
            return;
        }

        float boundary = zoneMaxAlt[z];
        float distToBoundary = boundary - altitude;

        if (distToBoundary < FADE_RANGE && distToBoundary >= 0)
        {
            float t = 1f - (distToBoundary / FADE_RANGE);
            cam.backgroundColor = Color.Lerp(zoneBgColor[z], zoneBgColor[z + 1], t);
        }
        else if (distToBoundary < 0)
        {
            cam.backgroundColor = zoneBgColor[Mathf.Min(z + 1, ZONE_COUNT - 1)];
        }
        else
        {
            cam.backgroundColor = zoneBgColor[z];
        }
    }

    void SetZoneActive(int zoneIdx, bool active)
    {
        if (zoneRoots[zoneIdx] != null)
            zoneRoots[zoneIdx].SetActive(active);
    }

    void SetZoneAlpha(int zoneIdx, float alpha)
    {
        if (zoneRenderers[zoneIdx] == null) return;
        foreach (var sr in zoneRenderers[zoneIdx])
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
