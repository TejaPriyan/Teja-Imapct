using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// StreamingAssets are copied verbatim by Unity. Never ship the Windows relay
// executable to browsers or phones. Keep a recoverable copy across failed builds.
[InitializeOnLoad]
public sealed class TejaBuildArtifacts : IPreprocessBuildWithReport, IPostprocessBuildWithReport {
    public int callbackOrder {get{return -1000;}}
    static string Source {get{return Path.Combine(Application.dataPath,"StreamingAssets/Networking/cloudflared.exe");}}
    static string Stash {get{return Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/TejaNativeBuildStash/cloudflared.exe"));}}
    static TejaBuildArtifacts(){EditorApplication.delayCall+=()=>{if(!BuildPipeline.isBuildingPlayer)Restore();};}
    public void OnPreprocessBuild(BuildReport report){
        if(report.summary.platform==BuildTarget.StandaloneWindows||report.summary.platform==BuildTarget.StandaloneWindows64){Restore();return;}
        if(File.Exists(Stash)&&File.Exists(Source))throw new BuildFailedException("Both copies of the relay helper exist. Resolve Library/TejaNativeBuildStash before building.");
        if(!File.Exists(Source))return;
        Directory.CreateDirectory(Path.GetDirectoryName(Stash));File.Move(Source,Stash);
        if(File.Exists(Source+".meta"))File.Move(Source+".meta",Stash+".meta");
        AssetDatabase.Refresh();
    }
    public void OnPostprocessBuild(BuildReport report){Restore();}
    [MenuItem("Teja's Impact/Restore native helper after interrupted build")]
    public static void Restore(){
        if(!File.Exists(Stash))return;
        if(File.Exists(Source)){Debug.LogError("Relay helper restore skipped because both source and stash exist.");return;}
        Directory.CreateDirectory(Path.GetDirectoryName(Source));File.Move(Stash,Source);
        if(File.Exists(Stash+".meta")){if(File.Exists(Source+".meta"))File.Delete(Source+".meta");File.Move(Stash+".meta",Source+".meta");}
        AssetDatabase.Refresh();
    }
}
