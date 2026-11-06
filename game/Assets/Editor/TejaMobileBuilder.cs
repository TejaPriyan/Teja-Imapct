using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;

public static class TejaMobileBuilder {
    [MenuItem("Teja's Impact/Check mobile build support")]
    public static void CheckSupport(){
        Debug.Log("TEJA_PLATFORM_SUPPORT Android="+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android)+" iOS="+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS)+" WebGL="+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL));
    }
    [MenuItem("Teja's Impact/Build Android ARM64 APK")]
    public static void BuildAndroid(){
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new System.InvalidOperationException("Install Android Build Support, Android SDK & NDK Tools, and OpenJDK for Unity 2022.3.20f1 in Unity Hub first.");
        CharacterBuilder.Build();TejaBuilder.CreateScene();
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;PlayerSettings.Android.useCustomKeystore=false;
        PlayerSettings.Android.bundleVersionCode=7;
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android,ManagedStrippingLevel.Low);
        // The procedural world requires engine types absent from the bootstrap scene.
        PlayerSettings.stripEngineCode=false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.Android.androidIsGame=true;
        EditorUserBuildSettings.buildAppBundle=false;
        AssetDatabase.SaveAssets();
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Tejas-Impact-Android-v0.7.apk"));
        try {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Aetherfall.unity"},locationPathName=output,target=BuildTarget.Android,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Android build failed: "+report.summary.result);
            Debug.Log("TEJA_ANDROID_BUILD_OK "+output);
        }finally{TejaBuildArtifacts.Restore();}
    }
    [MenuItem("Teja's Impact/Export iOS Xcode project")]
    public static void ExportIOS(){
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,BuildTarget.iOS))throw new System.InvalidOperationException("Install iOS Build Support for this editor first. The exported project still requires macOS, Xcode, signing and device validation.");
        CharacterBuilder.Build();TejaBuilder.CreateScene();PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS,ScriptingImplementation.IL2CPP);
        try {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Aetherfall.unity"},locationPathName=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Tejas-Impact-iOS-v0.7")),target=BuildTarget.iOS,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("iOS export failed: "+report.summary.result);
            Debug.Log("TEJA_IOS_EXPORT_OK "+report.summary.outputPath);
        }finally{TejaBuildArtifacts.Restore();}
    }
}
