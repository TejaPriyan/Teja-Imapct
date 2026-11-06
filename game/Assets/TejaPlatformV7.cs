using System;
using System.Runtime.InteropServices;
using UnityEngine;

// Keep browser and phone capability differences explicit at both UI and entry points.
public static class TejaPlatformV7 {
    public static string[] LaunchArguments {
        get {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new string[0];
#else
            return Environment.GetCommandLineArgs();
#endif
        }
    }
    public static bool InternetRoomsAvailable {
        get {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }
    public static string Edition {
        get {
#if UNITY_WEBGL && !UNITY_EDITOR
            return "Browser";
#elif UNITY_ANDROID && !UNITY_EDITOR
            return "Android";
#elif UNITY_IOS && !UNITY_EDITOR
            return "iPhone";
#else
            return "Windows";
#endif
        }
    }
}

public partial class TejaAdventure {
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void TejaRegisterBrowser(string objectName);
    [DllImport("__Internal")] static extern void TejaSyncStorage(string objectName);
#endif
    bool browserStorageWarned;

    void InitializePlatformV7(){
#if UNITY_WEBGL && !UNITY_EDITOR
        TejaRegisterBrowser(gameObject.name);
        Application.targetFrameRate=30;
#endif
        Debug.Log("TEJA_PLATFORM_READY v0.7 "+TejaPlatformV7.Edition+"; onlineRooms="+TejaPlatformV7.InternetRoomsAvailable);
    }
    void RequestPlatformSaveSync(){
#if UNITY_WEBGL && !UNITY_EDITOR
        if(!smoke&&!inputCheck)TejaSyncStorage(gameObject.name);
#endif
    }
    // Called from the browser bridge on visibility loss. Save while the page still runs.
    public void OnTejaBrowserHidden(){
        if(smoke||inputCheck)return;
        if(phase!=Phase.Title&&phase!=Phase.Defeat){SaveJourney(false);SaveAudioSettings();SaveControlSettings();}
        if(phase==Phase.Play)Pause();
    }
    public void OnTejaBrowserStorageError(string ignored){
        if(browserStorageWarned)return;browserStorageWarned=true;
        Debug.LogWarning("Browser persistent storage is unavailable; progress may only last for this tab.");
        Notify("Browser storage is unavailable. This journey may not survive closing the tab.");
    }
}
