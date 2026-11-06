using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using System.IO;

public static class TejaBuilder {
    [MenuItem("Teja's Impact/Create or reset game scene")]
    public static void CreateScene() {
        Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Resources");
        var material = new Material(Shader.Find("Standard"));
        if (!File.Exists("Assets/Resources/World.mat")) AssetDatabase.CreateAsset(material,"Assets/Resources/World.mat");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Teja's Impact — Runtime World").AddComponent<TejaAdventure>();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/Aetherfall.unity");
        EditorBuildSettings.scenes = new[]{ new EditorBuildSettingsScene("Assets/Scenes/Aetherfall.unity",true) };
        PlayerSettings.companyName="Teja Games";PlayerSettings.productName="Teja's Impact";
        PlayerSettings.bundleVersion="0.7.0";PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone,"com.tejagames.tejasimpact");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.tejagames.tejasimpact");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS,"com.tejagames.tejasimpact");
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
        PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.runInBackground=false;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
        QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=2;
        QualitySettings.shadows=ShadowQuality.Disable;QualitySettings.pixelLightCount=1;
        AssetDatabase.SaveAssets(); Debug.Log("TEJA_SCENE_READY");
    }
    [MenuItem("Teja's Impact/Build Windows")]
    public static void BuildWindows() {
        TejaMobileBuilder.CheckSupport();
        CharacterBuilder.Build();
        CreateScene();
        TejaBuildArtifacts.Restore();
        var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Tejas-Impact-Windows-v0.7/TejasImpact.exe"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Aetherfall.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Windows build failed: "+report.summary.result);
        Debug.Log("TEJA_WINDOWS_BUILD_OK "+path);
    }
}
