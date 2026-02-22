# FyingCoin (가칭: 플라잉 코인)

**FyingCoin**은 2D 아케이드 감성의 유니티(Unity) 물리 기반 게임 프로토타입입니다. 
플레이어가 동전을 튕겨 허공으로 날려 보내고, 떨어지는 동전과 상호작용하며 속도감과 타격감을 느끼는 하이퍼 캐주얼 스타일에 중점을 두고 있습니다.

## 🚀 기획 및 세계관
목표: **지구를 벗어나 우주로 진출하여 화성(Mars)에 도달하는 여정**
- 초기: 지구 대기권 내에서 구름과 새를 지나칩니다.
- 중반: 성층권을 지나 외기권, 심우주(은하수)로 진입합니다.
- 후반: 태양을 지나 화성에 도달하여 미션을 클리어합니다.

*(현재 버전은 지구 대기권을 뚫고 심우주로 진입하는 과정을 구현했습니다.)*

## ✨ 주요 기능 및 구현 시스템

> **Note:** 웹/기타 엔진(Godot 등) 프로토타입 파일들은 최상단 폴더에서 정리되어, 현재는 순수 Unity 2D 프로젝트 스크립트만 남아있습니다.

### 1. 1초 자동 씬 셋업 (`AutoSetupCoinGame.cs`)
- 유니티 상단 메뉴의 `CoinGame -> 👉 1초 만에 씬 자동 세팅하기!` 버튼 하나로 씬 구성 완료.
- 동전, 무한 탄성 벽, 카메라 추적, 배경 매니저 설정들을 한 번에 자동으로 구성합니다.

### 2. 게임 오프닝 시퀀스 (`OpeningManager.cs`)
- 게임 시작 전, 2222년 화성 이주 프로젝트에 대한 스토리를 타이핑(Typewriter) 효과로 보여줍니다.
- 시퀀스 동안은 물리 엔진 등이 정지되며, 화면을 터치하면 오프닝이 종료되고 우주 비행이 시작됩니다.

### 3. 아케이드 물리 엔진 (`CoinPhysics.cs`)
- '산도알(Sando-R)' 같은 오락실 게임의 묵직한 조작감과 타격감.
- 동전의 중심을 벗어나 클릭하면 크게 스핀(Spin)하며 튕깁니다.
- **더블 탭(Double Tap)**: 더 강한 힘으로 동전을 하늘 높이 쳐올립니다.
- 빠른 낙하 속도와 즉각적인 상승 반응으로 폭발적인 조작감을 제공합니다.
- **👉 [물리 엔진 튜닝 조절 가이드 보기 (Physics_Tuning_Guide.md)](Physics_Tuning_Guide.md)**: 동전 낙하 속도, 튕기는 반발력 등을 입맛대로 조작하는 상세 가이드입니다.

### 4. 카메라 무한 추적 (`CameraController.cs`)
- 동전의 고도(Altitude)를 부드럽게(Lerp) 따라가는 추적 카메라입니다.

### 5. 고도별 다이나믹 대기권 배경 (`BackgroundManager.cs`)
- 카메라의 배경색이 코인의 높이에 따라 실시간으로 변합니다 (Lerp).
  - **0m:** 밝고 맑은 하늘색 (대류권)
  - **1,200m:** 짙은 파란색 (성층권)
  - **5,000m:** 어두운 남색 (중간권)
  - **8,500m:** 짙은 보라색/밤하늘 (열권)
  - **20,000m 이상:** 칠흑 같은 우주 배경 (외기권/심우주)

### 6. 고도별 환경 파티클/오브젝트 무한 스포너 (`AtmosphereObjectSpawner.cs`)
- 고도별로 구름, 새, 기상 풍선, 제트기, 별똥별, 위성 등 다양한 환경 오브젝트를 화면에 지속적으로 생성합니다.
- **최상층(우주):** 배경에 고정된 작은 "별"들을 스크롤하여 무한한 우주의 층이 이동하는 듯한 패럴랙스(Parallax) 깊이감을 형성합니다.

### 7. 다이나믹 스피드 라인 (`SpeedLineManager.cs`)
- 동전이 하늘로 치솟거나 땅으로 무섭게 떨어질 때(속도 15 이상), 화면 양옆에 만화적인 **세로 속도 집중선** 이펙트를 무더기로 쏟아내어 역동성을 더합니다.

### 8. 모바일 화면 비율 자동 보정 (`MobileCameraAspect.cs`)
- 기준 해상도(1080x1920, 16:9)를 바탕으로 세로로 긴 스마트폰(19.5:9 등)에서도 가로폭(Width)이 잘리지 않도록 카메라 사이즈(`orthographicSize`)를 동적으로 키워줍니다.
- `OpeningManager.cs`의 UI Canvas 역시 가로 기준 넓이에 맞춰 늘어나도록 자동 최적화 되었습니다.

### 9. GitQuickMenu (커스텀 메뉴바 앱)
- 터미널이나 VS Code를 열지 않고도 Mac 상단 메뉴바(Menu Bar)에서 원클릭으로 GitHub에 코드를 Push 할 수 있도록 만들어진 Swift App이 내장되어 있습니다. (`/GitQuickMenu` 폴더 참고)

---

## 🔧 리팩토링 이력

### 2026-02-22 — 코드 품질 개선 (3건)

#### 1. 리플렉션 제거 (`AutoSetupCoinGame.cs`)
- **이전:** `System.Type.GetType("CoinPhysics, Assembly-CSharp")` + `GetField/SetValue`로 런타임 리플렉션 사용
- **이후:** `coin.AddComponent<CoinPhysics>()` 및 `coinPhysics.hitSounds = ...` 직접 타입 참조로 교체
- **이유:** 필드명 변경 시 런타임에서야 에러 발생하는 취약성 제거. 컴파일 타임에 타입 안전성 확보.

#### 2. UI 텍스트 레거시 API 교체 (`OpeningManager.cs`)
- **이전:** `UnityEngine.UI.Text`, `Font customFont`, `TextAnchor`, `HorizontalWrapMode/VerticalWrapMode`
- **이후:** `TextMeshProUGUI`, `TMP_FontAsset customFont`, `TextAlignmentOptions.Center`, `enableWordWrapping/overflowMode`
- **이후 (AutoSetupCoinGame.cs):** 폰트 검색 `t:Font` → `t:TMP_FontAsset`으로 변경
- **주의:** `2002.ttf`를 Unity에서 `Window > TextMeshPro > Font Asset Creator`로 TMP_FontAsset으로 변환 필요. 없으면 기본 TMP 폰트 사용.

#### 3. 에셋 로딩 실패 경고 추가 (`AutoSetupCoinGame.cs`)
- **이전:** GUID 발견 후 `LoadAssetAtPath`가 null 반환 시 조용히 무시
- **이후:** 각 로딩 실패 케이스에 `Debug.LogWarning` 추가 (AudioClip × 2, TMP_FontAsset × 1)

#### 4. 모바일 UI 클릭 먹통(블랙 스크린) 현상 해결 (`OpeningManager.cs`)
- **이전:** 오프닝 UI(Canvas)만 동적으로 생성하고, 터치 이벤트를 처리할 `EventSystem`을 생성하지 않음.
- **이후:** `CreateUI()` 실행 시 씬에 `EventSystem`이 없으면 동적으로 생성 및 `InputSystemUIInputModule` 추가.
- **이유:** 스마트폰(iPhone 등) 환경에서 `EventSystem`이 없으면 화면 터치가 완전히 무시되어, 첫 오프닝 검은 화면에서 게임이 영원히 멈추는(Hang) 치명적인 버그 해결.
- **주의:** 이 프로젝트는 New Input System (`activeInputHandler: 1`, `com.unity.inputsystem: 1.18.0`) 을 사용하므로 `StandaloneInputModule`이 아닌 `InputSystemUIInputModule`을 써야 iOS 터치가 정상 동작함.

### 2026-02-23 — iOS 실기기 블랙스크린 완전 해결 (3건)

#### 1. 오프닝 터치 무반응 근본 수정 (`OpeningManager.cs`)
- **진짜 원인:** `InputSystemUIInputModule`의 `m_PointerBehavior: 0`(SingleUnifiedPointer) 모드가 iOS 실기기 터치를 `GraphicRaycaster → Button.onClick` 체인까지 전달하지 못함 (Unity 6 + New Input System + iOS 알려진 호환성 문제)
- **이전:** `Button.AddComponent` + `onClick.AddListener(OnScreenClicked)` + `GraphicRaycaster` 의존
- **이후:** `Button`, `GraphicRaycaster` 완전 제거. `Update()`에서 `Touchscreen.current.primaryTouch.press.wasPressedThisFrame` 직접 폴링 (에디터는 `Mouse.current`). `CoinPhysics.cs`와 동일 패턴 통일.

#### 2. 폰트 크래시 → 한글 텍스트 정상 표시 (`OpeningManager.cs`)
- **진짜 원인:** `public TMP_FontAsset customFont` Inspector 참조 → iOS IL2CPP 빌드에서 `atlasTexture` getter가 `NullReferenceException` 발생 → `Start()` 크래시 → `TypeText()` 코루틴 미실행 → `isTyping/isFinished` 둘 다 false → `Update()` 가드에서 터치 영원히 무반응 (블랙스크린의 실제 원인)
- **이전:** `public TMP_FontAsset customFont` Inspector 할당 → `introText.font = customFont`
- **이후:** `2002 SDF.asset`을 `Assets/Resources/`로 이동, `Resources.Load<TMP_FontAsset>("2002 SDF")`로 런타임 로드. Inspector 직렬화 참조 완전 제거. 로드 실패 시 `LogWarning` 후 TMP 기본 폰트로 fallback.
- **연계 수정 (`AutoSetupCoinGame.cs`):** `openingMgr.customFont = loadedFont` 참조 제거 (CS1061 컴파일 에러 수정)

#### 3. 동전 터치 무반응 수정 (`CoinPhysics.cs`)
- **원인:** `Update()`가 `Mouse.current`만 체크 → iOS 실기기에서 터치 완전 미감지
- **이후:** `Touchscreen.current.primaryTouch.press.wasPressedThisFrame` 추가. 공통 로직을 `TryHitAt(Vector2 worldPos)` 헬퍼로 분리.

#### 4. Git 초기 설정
- Unity 전용 `.gitignore` 생성 (`Library/`, `Temp/`, `build_ios/`, `UserSettings/`, `.DS_Store` 등 제외)
- remote origin 설정: `git@github.com:sunpark20/FyingCoin.git`
- `git push --force`로 Unity 프로젝트 전면 교체 완료

---

## 🛠️ 향후 계획 (To Do)
- [ ] 화성 도달 연출 및 미션 클리어 판정 추가
- [ ] UI 고도계 디자인 개선
- [ ] 파티클 터치 이펙트 (폭발, 별가루 등) 적용
- [ ] 게임 오버 (Coin Drop) 조건 및 재시작 시스템

---
## 🤖 AI Assistant 작업 지침 (매우 중요)
> **모든 AI Assistant는 이 프로젝트의 새로운 코드를 작성하거나 기존 기능을 수정할 때마다 반드시 아래 두 문서를 최신 상태로 업데이트해야 합니다.**
> 1. 현재 문서 (`README.md`): 새로운 스크립트나 시스템이 추가되면 [주요 기능 및 구현 시스템] 파트에 기능 요약을 추가할 것.
> 2. `Physics_Tuning_Guide.md`: 물리 파라미터나 게임 밸런스, 조작감과 관련된 변수가 추가되거나 변경되면 해당 문서에 조절 방법과 영향도를 기록할 것.
> 3. **컨텍스트 복구 지침**: 새로운 채팅 세션에서 사용자가 *"유니티 동전 게임(`My project`, `FyingCoin`) 폴더 로드해줘!"* 라고 명령하면, AI는 즉시 해당 두 폴더의 코드 구조를 파악하고 현재 `README.md`를 읽어 프로젝트의 전반적인 상태와 세계관, 구현 시스템을 100% 숙지한 채 업무를 이어나가야 합니다.
