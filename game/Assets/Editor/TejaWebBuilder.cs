using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class TejaWebBuilder {
    [MenuItem("Teja's Impact/Build browser edition")]
    public static void BuildWebGL(){
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("Install WebGL Build Support for Unity 2022.3.20f1 first.");
        CharacterBuilder.Build();TejaBuilder.CreateScene();
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL,ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL,ManagedStrippingLevel.Low);
        // The world creates primitive colliders at runtime; no scene references keep them alive.
        PlayerSettings.stripEngineCode=false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.WebGL.nameFilesAsHashes=false;
        PlayerSettings.WebGL.template="PROJECT:Teja";
        PlayerSettings.WebGL.memorySize=256;
        // Newer 2022 LTS patches expose growth settings through additional properties.
        SetOptionalMemory("initialMemorySize",256);SetOptionalMemory("maximumMemorySize",1024);SetOptionalGrowth();
        PlayerSettings.runInBackground=false;
        AssetDatabase.SaveAssets();
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Tejas-Impact-WebGL-v0.7"));
        try {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Aetherfall.unity"},locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Browser build failed: "+report.summary.result);
            Debug.Log("TEJA_WEBGL_BUILD_OK "+output);
        }finally{TejaBuildArtifacts.Restore();}
    }
    static void SetOptionalMemory(string name,int value){
        var property=typeof(PlayerSettings.WebGL).GetProperty(name,BindingFlags.Public|BindingFlags.Static);
        if(property!=null&&property.CanWrite)property.SetValue(null,Convert.ChangeType(value,property.PropertyType),null);
    }
    static void SetOptionalGrowth(){
        var property=typeof(PlayerSettings.WebGL).GetProperty("memoryGrowthMode",BindingFlags.Public|BindingFlags.Static);
        if(property!=null&&property.CanWrite&&property.PropertyType.IsEnum&&Array.IndexOf(Enum.GetNames(property.PropertyType),"Geometric")>=0)property.SetValue(null,Enum.Parse(property.PropertyType,"Geometric"),null);
    }
}
