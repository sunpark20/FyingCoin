using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public class GameBuilder
{
    // 커맨드 라인에서 호출할 정적 메서드
    public static void BuildIOS()
    {
        Debug.Log("🚀 [iOS Build 1/3] 자동 씬 세팅 시작...");
        
        // 1. 필요한 오브젝트 (배경, UI, 이벤트 시스템 등) 씬에 1초 자동 세팅
        AutoSetupCoinGame.SetupScene();

        // 2. 현재 열려 있는 씬 강제 저장 (수정된 오브젝트들을 씬 파일에 굽기)
        // 빌드 시 방금 생성한 객체들이 누락되지 않게 하려는 핵심 조치
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("✅ [iOS Build 2/3] 씬 저장 완료. 오프닝 UI 누락 방지 조치 적용됨.");

        // 3. Build 설정 준비
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        
        // 빌드에 포함될 씬 목록 (Build Settings 창의 Scenes In Build)
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" }; 
        buildPlayerOptions.locationPathName = "Builds/iOS";     // xcode 프로젝트가 떨어질 위치
        buildPlayerOptions.target = BuildTarget.iOS;            // iOS 타겟
        buildPlayerOptions.options = BuildOptions.None;         // 기본 빌드

        Debug.Log("⚙️ [iOS Build 3/3] Unity-iPhone Xcode 프로젝트 추출(BuildPlayer) 시작...");

        // 4. 빌드 파이프라인 가동
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"🎉 [Build 성공] Xcode 프로젝트가 성공적으로 추출되었습니다. (크기: {summary.totalSize} bytes, 위치: {buildPlayerOptions.locationPathName})");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"❌ [Build 실패] Xcode 프로젝트 생성 중 오류 발생 ({summary.totalErrors} errors)");
        }
    }
}
