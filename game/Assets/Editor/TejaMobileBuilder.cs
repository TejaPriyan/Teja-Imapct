using UnityEditor;
using UnityEditor.Android;
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
        // Prefer the project-local, writable SDK when it has been provisioned
        // for a current Android compatibility build; otherwise use Unity's SDK.
        string sdkRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../tools/android-sdk"));
        if(File.Exists(Path.Combine(sdkRoot,"platforms/android-36/android.jar"))){
            AndroidExternalToolsSettings.sdkRootPath=sdkRoot;
            Debug.Log("TEJA_ANDROID_CUSTOM_SDK "+sdkRoot);
        }else Debug.LogWarning("TEJA_ANDROID_CUSTOM_SDK_NOT_FOUND; using Unity's configured Android SDK.");
        CharacterBuilder.Build();TejaBuilder.CreateScene();
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        // API 36 keeps the sideloaded build current on modern Android versions.
        // Cast keeps this 2022 LTS project compatible with the newer installed SDK.
        PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;PlayerSettings.Android.useCustomKeystore=false;
        PlayerSettings.Android.bundleVersionCode=8;
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android,ManagedStrippingLevel.Low);
        // The procedural world requires engine types absent from the bootstrap scene.
        PlayerSettings.stripEngineCode=false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.Android.androidIsGame=true;
        EditorUserBuildSettings.buildAppBundle=false;
        AssetDatabase.SaveAssets();
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Tejas-Impact-Android-v0.7.1.apk"));
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

// Unity 2022 ships an older Android Gradle Plugin which otherwise downloads an
// AAPT2 binary that cannot read the Android 16 resource table. Point Gradle at
// the API 36-compatible AAPT2 when the project-local SDK provides it.
public sealed class TejaAndroidGradlePatch : IPostGenerateGradleAndroidProject {
    public int callbackOrder { get { return 999; } }
    public void OnPostGenerateGradleAndroidProject(string path){
        string sdkRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../tools/android-sdk"));
        string aapt2=Path.Combine(sdkRoot,"build-tools/36.1.0/aapt2.exe").Replace('\\','/');
        if(!File.Exists(aapt2)){
            Debug.LogWarning("TEJA_AAPT2_OVERRIDE_NOT_FOUND; using Unity/Gradle's configured AAPT2.");
            return;
        }
        string gradleProperties=Path.Combine(Directory.GetParent(path).FullName,"gradle.properties");
        File.AppendAllText(gradleProperties,"\n# Teja's Impact Android 16 compatibility override\nandroid.aapt2FromMavenOverride="+aapt2+"\n");
        Debug.Log("TEJA_AAPT2_OVERRIDE "+aapt2);
    }
}
