using UnityEngine;
using System;
using System.Collections.Generic;

public partial class TejaAdventure {
    // Gameplay gestures use independent finger IDs. IMGUI remains responsible for
    // ordinary menu taps, but never for attack/move/look multi-touch gestures.
    enum MobileAction { None, Attack, Jump, Dodge, Nova, Heal, Aim, Reload, Switch, Lock, Sprint, Recenter, Interact, Menu, Map, Journal, Character, Inventory }
    readonly Dictionary<int,MobileAction> mobileFingers=new Dictionary<int,MobileAction>();
    readonly List<int> mobileStaleFingers=new List<int>();
    readonly HashSet<int> mobileSeenFingers=new HashSet<int>();
    bool mobilePreviewEnabled,mobileMenuTapBlocked;
    Rect mobileLastViewport;
    bool mobileLastPortrait;
    GUIStyle mobileLabel,mobileHint;
    bool MobileControlsActive {get{return Application.isMobilePlatform||mobilePreviewEnabled;}}
    bool MobilePortraitBlocked {get{return MobileControlsActive&&Screen.height>Screen.width;}}
    bool MobileGameplayInputAllowed {get{return MobileControlsActive&&!MobilePortraitBlocked&&phase==Phase.Play&&!uiCredits&&!uiNewConfirm&&!uiCoopOpen&&!CoopWorldPaused;}}
    Rect UiViewport {get{return MobileControlsActive?ClampMobileViewport(Screen.width,Screen.height,Screen.safeArea):new Rect(0,0,Screen.width,Screen.height);}}
    static Rect ClampMobileViewport(float width,float height,Rect area){
        if(area.width<=0||area.height<=0)return new Rect(0,0,width,height);
        float x=Mathf.Clamp(area.x,0,width),y=Mathf.Clamp(area.y,0,height);
        return new Rect(x,y,Mathf.Max(1,Mathf.Min(area.xMax,width)-x),Mathf.Max(1,Mathf.Min(area.yMax,height)-y));
    }
    static Vector2 MobileUiPoint(Vector2 screenPoint,Rect viewport,float scale){return new Vector2((screenPoint.x-viewport.x)/scale,(viewport.yMax-screenPoint.y)/scale);}
    Vector3 UiOrigin {get{Rect view=UiViewport;return V(view.x,Screen.height-view.yMax,0);}}
    float MobileUiWidth {get{return UiViewport.width/UiScale;}}
    void InitializeMobileControls(){
        mobilePreviewEnabled=Array.IndexOf(TejaPlatformV7.LaunchArguments,"-teja-mobile-preview")>=0;
        if(Application.isMobilePlatform){
            Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
            Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;
            Screen.orientation=ScreenOrientation.AutoRotation;
        }
        mobileLastViewport=UiViewport;mobileLastPortrait=MobilePortraitBlocked;
    }
    void ResetMobileControls(){
        mobileFingers.Clear();mobileSeenFingers.Clear();mobileStaleFingers.Clear();
        stick=Vector2.zero;stickFinger=lookFinger=-1;mobileSprint=sprinting=false;attackBuffer=0;
    }
    void TickMobileControls(){
        if(!MobileControlsActive)return;
        if(Input.touchCount==0)mobileMenuTapBlocked=false;
        Rect viewport=UiViewport;bool portrait=MobilePortraitBlocked;
        if(viewport!=mobileLastViewport||portrait!=mobileLastPortrait){ResetMobileControls();mobileLastViewport=viewport;mobileLastPortrait=portrait;}
        if(portrait&&phase==Phase.Play)Pause();
        if(!MobileGameplayInputAllowed)ResetMobileControls();
    }
    void BlockMobileMenuCarryover(){if(MobileControlsActive&&Input.touchCount>0)mobileMenuTapBlocked=true;}
    static Rect MobileRect(MobileAction action,float width){
        switch(action){
            case MobileAction.Attack:return new Rect(width-118,587,98,110);
            case MobileAction.Jump:return new Rect(width-228,603,98,94);
            case MobileAction.Dodge:return new Rect(width-338,603,98,94);
            case MobileAction.Nova:return new Rect(width-118,477,98,94);
            case MobileAction.Heal:return new Rect(width-228,477,98,94);
            case MobileAction.Aim:return new Rect(width-338,477,98,94);
            case MobileAction.Reload:return new Rect(width-448,603,98,94);
            case MobileAction.Switch:return new Rect(width-448,477,98,94);
            case MobileAction.Lock:return new Rect(width-448,367,98,94);
            case MobileAction.Sprint:return new Rect(28,335,116,88);
            case MobileAction.Recenter:return new Rect(156,335,116,88);
            case MobileAction.Interact:return new Rect(width*.5f-190,271,380,88);
            case MobileAction.Menu:return new Rect(width-112,24,88,88);
            case MobileAction.Map:return new Rect(width-212,24,88,88);
            case MobileAction.Inventory:return new Rect(width-328,24,104,88);
            case MobileAction.Journal:return new Rect(24,146,300,76);
            case MobileAction.Character:return new Rect(24,24,300,110);
            default:return new Rect();
        }
    }
    bool MobileActionVisible(MobileAction action){
        if(action==MobileAction.None)return false;
        if(action==MobileAction.Interact)return UiInteractionPrompt().Length>0;
        if(action==MobileAction.Aim||action==MobileAction.Reload)return equippedWeapon>0;
        return true;
    }
    MobileAction MobileHitAction(Vector2 point,float width){
        for(int n=1;n<=(int)MobileAction.Inventory;n++){var action=(MobileAction)n;if(MobileActionVisible(action)&&MobileRect(action,width).Contains(point))return action;}
        return MobileAction.None;
    }
    void BeginMobileFinger(int finger,Vector2 point,float width){
        if(!MobileGameplayInputAllowed)return;
        MobileAction action=MobileHitAction(point,width);
        if(action!=MobileAction.None){
            mobileFingers[finger]=action;
            if(action!=MobileAction.Attack)RunMobileAction(action);
            return;
        }
        if(new Rect(20,425,280,278).Contains(point)&&stickFinger<0){stickFinger=finger;stickCenter=point;stick=Vector2.zero;return;}
        if(point.x>width*.34f&&point.y>104&&lookFinger<0){lookFinger=finger;return;}
    }
    void EndMobileFinger(int finger){
        mobileFingers.Remove(finger);
        if(stickFinger==finger){stickFinger=-1;stick=Vector2.zero;}
        if(lookFinger==finger)lookFinger=-1;
    }
    static Vector2 MobileStickValue(Vector2 origin,Vector2 point){
        Vector2 delta=(point-origin)/82f;float length=delta.magnitude;
        if(length<.08f)return Vector2.zero;
        return delta.normalized*Mathf.Clamp01((length-.08f)/.92f);
    }
    void UpdateMobileFinger(int finger,Vector2 point,Vector2 delta,TouchPhase touchPhase,float width){
        bool ended=touchPhase==TouchPhase.Ended||touchPhase==TouchPhase.Canceled;
        if(ended){EndMobileFinger(finger);return;}
        if(touchPhase==TouchPhase.Began)BeginMobileFinger(finger,point,width);
        if(!MobileGameplayInputAllowed)return;
        if(finger==stickFinger)stick=MobileStickValue(stickCenter,point);
        if(finger==lookFinger&&touchPhase==TouchPhase.Moved){
            lockTarget=null;yaw+=delta.x*.16f*lookSensitivity;
            pitch=Mathf.Clamp(pitch+delta.y*(invertLook?.13f:-.13f)*lookSensitivity,8,55);
        }
        MobileAction action;
        if(mobileFingers.TryGetValue(finger,out action)&&action==MobileAction.Attack&&MobileRect(action,width).Contains(point))Strike();
    }
    void ReadMobileTouches(){
        if(!MobileGameplayInputAllowed)return;
        float width=MobileUiWidth;mobileSeenFingers.Clear();
        for(int i=0;i<Input.touchCount;i++){
            Touch touch=Input.GetTouch(i);mobileSeenFingers.Add(touch.fingerId);
            UpdateMobileFinger(touch.fingerId,UiScreenPoint(touch.position),touch.deltaPosition/UiScale,touch.phase,width);
        }
        // Some devices lose cancellation events while opening system panels.
        // Reconcile against the live finger IDs so a stick or attack cannot stick.
        mobileStaleFingers.Clear();foreach(var entry in mobileFingers)if(!mobileSeenFingers.Contains(entry.Key))mobileStaleFingers.Add(entry.Key);
        foreach(int finger in mobileStaleFingers)EndMobileFinger(finger);
        if(stickFinger>=0&&!mobileSeenFingers.Contains(stickFinger))EndMobileFinger(stickFinger);
        if(lookFinger>=0&&!mobileSeenFingers.Contains(lookFinger))EndMobileFinger(lookFinger);
        sprinting=mobileSprint&&stick.sqrMagnitude>.01f;aiming=equippedWeapon>0&&aimToggle;
    }
    void RunMobileAction(MobileAction action){
        if(!MobileGameplayInputAllowed)return;
        switch(action){
            case MobileAction.Jump:Jump();break;
            case MobileAction.Dodge:Dodge();break;
            case MobileAction.Nova:Burst();break;
            case MobileAction.Heal:UsePotion();break;
            case MobileAction.Aim:aimToggle=!aimToggle;aiming=equippedWeapon>0&&aimToggle;break;
            case MobileAction.Reload:Reload();break;
            case MobileAction.Switch:for(int j=1;j<=3;j++){int next=(equippedWeapon+j)%3;if((ownedWeapons&(1<<next))!=0){EquipWeapon(next);break;}}break;
            case MobileAction.Lock:ToggleLockOn();break;
            case MobileAction.Sprint:mobileSprint=!mobileSprint;break;
            case MobileAction.Recenter:lockTarget=null;yaw=hero.eulerAngles.y-180;pitch=20;break;
            case MobileAction.Interact:Interact();break;
            case MobileAction.Menu:pausePage=PausePage.Journey;Pause();break;
            case MobileAction.Map:TogglePanel(Phase.Map);break;
            case MobileAction.Journal:TogglePanel(Phase.Journal);break;
            case MobileAction.Character:TogglePanel(Phase.Character);break;
            case MobileAction.Inventory:TogglePanel(Phase.Inventory);break;
        }
        if(phase!=Phase.Play){BlockMobileMenuCarryover();ResetMobileControls();}
    }
    void UiMobileAction(MobileAction action,string label,string hint,bool enabled=true,float cooldown=0){
        if(!MobileActionVisible(action))return;
        Rect rect=MobileRect(action,uiWidth);Color accent=action==MobileAction.Attack?uiGold:uiCyan;
        if(Event.current.type==EventType.Repaint)uiHitRects.Add(rect);
        UiBox(rect,enabled?uiNavy:new Color(.04f,.08f,.1f,.65f));UiFill(new Rect(rect.x+12,rect.y+1,rect.width-24,2),enabled?accent:uiMuted);
        if(rect.height>=90){
            if(action==MobileAction.Attack)UiWeaponIcon(new Rect(rect.center.x-18,rect.y+13,36,31),equippedWeapon,accent);
            else UiCircle(rect.center.x,rect.y+23,7,enabled?accent:uiMuted,true);
            UiText(rect.x+2,rect.y+43,rect.width-4,24,cooldown>0?cooldown.ToString("0.0")+"s":label,mobileLabel,enabled?accent:uiMuted);
            UiText(rect.x+2,rect.yMax-25,rect.width-4,22,hint,mobileHint,uiMuted);
        }else UiText(rect.x+4,rect.y+6,rect.width-8,rect.height-12,label,mobileLabel,enabled?accent:uiMuted);
    }
    void UiMobileHud(){
        if(mobileLabel==null){mobileLabel=new GUIStyle(uiCenter){fontSize=20};mobileHint=new GUIStyle(uiTinyCenter){fontSize=16};}
        bool enabled=MobileGameplayInputAllowed;
        Rect health=MobileRect(MobileAction.Character,uiWidth);UiCard(health,hp<MaxHealth*.25f?uiRed:uiCyan);
        if(Event.current.type==EventType.Repaint)uiHitRects.Add(health);
        UiText(40,34,200,26,"TEJA  /  LEVEL "+playerLevel,uiBody,uiIvory);UiText(40,62,260,22,Mathf.CeilToInt(hp)+" / "+Mathf.RoundToInt(MaxHealth)+" HP   ·   "+coins+" coins",uiSmall,uiCyan);
        UiBar(new Rect(40,91,268,7),uiHealth/MaxHealth,hp<MaxHealth*.25f?uiRed:uiCyan);UiText(40,104,268,24,"Character & upgrades"+(skillPoints>0?"  +"+skillPoints:""),uiTiny,uiGold);
        Rect quest=MobileRect(MobileAction.Journal,uiWidth);UiBox(quest,uiNavy);if(Event.current.type==EventType.Repaint)uiHitRects.Add(quest);
        UiText(40,155,268,25,"QUEST JOURNAL",uiSmall,uiGold);UiText(40,183,268,26,count+" / 3 sigils  ·  "+(regionName??"Aetherfall"),uiTiny);
        UiMobileAction(MobileAction.Menu,"Menu","",enabled);UiMobileAction(MobileAction.Map,"Map","",enabled);UiMobileAction(MobileAction.Inventory,"Loadout","",enabled);
        if(boss!=null&&boss.active&&!boss.dead){float w=Mathf.Min(380,uiWidth-720);float x=(uiWidth-w)*.5f;UiBox(new Rect(x-12,24,w+24,66),uiNavy);UiText(x,30,w,26,"RIFT WARDEN",mobileLabel,uiRed);UiBar(new Rect(x,67,w,7),boss.hp/boss.max,uiRed);}
        if(phase==Phase.Play){UiWorldIndicators();UiCrosshair();}
        UiCircle(150,565,87,new Color(.04f,.09f,.13f,.7f));UiCircle(150,565,87,new Color(.5f,.8f,.84f,.6f),true);UiCircle(150+stick.x*57,565+stick.y*57,31,uiCyan);UiText(32,665,236,30,"Drag to move",mobileLabel,uiMuted);
        UiMobileAction(MobileAction.Sprint,mobileSprint?"Sprint ON":"Sprint","",enabled);UiMobileAction(MobileAction.Recenter,"Recenter","",enabled);
        UiMobileAction(MobileAction.Attack,equippedWeapon==0?"Strike":"Fire","Hold",enabled&&reloadTime<=0,strikeCD);
        UiMobileAction(MobileAction.Jump,"Jump","Leap",enabled);UiMobileAction(MobileAction.Dodge,"Dodge","Dash",enabled&&dodgeCD<=0,dodgeCD);
        UiMobileAction(MobileAction.Nova,"Frost","Nova",enabled&&burstCD<=0,burstCD);UiMobileAction(MobileAction.Heal,"Tonic",potions+" left",enabled&&potions>0&&hp<MaxHealth);
        UiMobileAction(MobileAction.Aim,aimToggle?"Aim ON":"Aim","Toggle",enabled);UiMobileAction(MobileAction.Reload,"Reload",equippedWeapon>0?ammo[equippedWeapon]+" / "+weapons[equippedWeapon].magazine:"",enabled&&reloadTime<=0,reloadTime);
        UiMobileAction(MobileAction.Switch,"Weapon","Switch",enabled);UiMobileAction(MobileAction.Lock,lockTarget!=null?"Unlock":"Lock","Target",enabled);
        if(UiInteractionPrompt().Length>0)UiMobileAction(MobileAction.Interact,"Interact  /  "+UiInteractionPrompt(),"",enabled);
        if(noticeTime>0&&phase==Phase.Play){float w=Mathf.Min(500,uiWidth-680);UiBox(new Rect((uiWidth-w)*.5f,140,w,68),uiNavy);UiText((uiWidth-w)*.5f+12,149,w-24,50,notice,uiSmall,uiGold);}
    }
    void UiMobilePortraitPrompt(){
        Matrix4x4 previous=GUI.matrix;GUI.matrix=Matrix4x4.identity;UiFill(new Rect(0,0,Screen.width,Screen.height),uiNavy);
        float scale=Mathf.Min(Screen.width/390f,Screen.height/700f);GUI.matrix=Matrix4x4.TRS(V((Screen.width-390*scale)*.5f,(Screen.height-700*scale)*.5f,0),Quaternion.identity,V(scale,scale,1));
        UiBorder(new Rect(111,195,168,86),uiGold);UiBorder(new Rect(119,203,152,70),uiCyan);
        var heading=new GUIStyle(uiCenter){fontSize=28};var text=new GUIStyle(uiCenter){fontSize=18};
        UiText(30,325,330,70,"Turn your phone sideways",heading,uiIvory);UiText(40,410,310,90,"Teja's Impact plays in landscape.\nYour journey is paused while you rotate.",text,uiMuted);
        GUI.matrix=previous;
    }
    void UiMobileControlsPage(Rect frame,float x,float y){
        UiText(x,y+91,frame.width-60,40,"Landscape play  /  Move, look and attack with separate fingers.",uiBody,uiCyan);
        string[] names={"Move / sprint","Look / recenter","Attack / shoot","Jump / dodge / frost","Aim / reload / lock","Switch / equip weapons","Interact / heal","Quests / upgrades / map","Pause / sound / graphics"};
        string[] actions={"Left stick / Sprint toggle","Drag open right side / Recenter","Hold Strike or Fire","Jump / Dodge / Frost buttons","Aim toggle / Reload / Lock","Weapon button / Loadout menu","Nearby Interact prompt / Tonic","Quest card / Health card / Map","Menu; return with Resume"};
        for(int i=0;i<names.Length;i++){float row=y+151+i*36;if(i%2==0)UiFill(new Rect(x-5,row-3,frame.width-50,34),new Color(.08f,.15f,.20f,.45f));UiText(x+7,row,300,32,names[i],uiBody,uiMuted);UiText(x+320,row,frame.width-385,32,actions[i],uiBody);}
        if(UiButton(new Rect(frame.xMax-240,frame.yMax-67,210,44),"Back to pause"))pausePage=PausePage.Journey;
    }
    void ValidateMobileControlsV7(){
        bool priorPreview=mobilePreviewEnabled;Phase priorPhase=phase;bool priorCredits=uiCredits,priorConfirm=uiNewConfirm,priorCoop=uiCoopOpen;int priorWeapon=equippedWeapon;float priorYaw=yaw,priorPitch=pitch;bool priorAim=aimToggle,priorSprint=mobileSprint;
        try{
            mobilePreviewEnabled=true;phase=Phase.Play;uiCredits=uiNewConfirm=uiCoopOpen=false;
            Rect viewport=ClampMobileViewport(844,390,new Rect(44,21,756,369));float scale=viewport.height/720f;
            Vector2 point=MobileUiPoint(new Vector2(viewport.x+100*scale,viewport.yMax-200*scale),viewport,scale);
            Require(Vector2.Distance(point,new Vector2(100,200))<.01f,"Mobile safe-area coordinates preserve touch alignment");
            float width=viewport.width/scale;
            for(int n=1;n<=(int)MobileAction.Inventory;n++){Rect rect=MobileRect((MobileAction)n,width);Require(rect.x>=0&&rect.y>=0&&rect.xMax<=width&&rect.yMax<=720,"Mobile controls remain inside landscape safe area");Require(rect.width*scale>=44&&rect.height*scale>=38,"Mobile touch targets retain usable physical size");}
            Require(MobileStickValue(Vector2.zero,new Vector2(82,0)).x>.99f,"Mobile right movement is positive X input");Require(MobileStickValue(Vector2.zero,new Vector2(0,-82)).y<-.99f,"Mobile forward movement is negative Y input");Require(MobileStickValue(Vector2.zero,new Vector2(2,2))==Vector2.zero,"Mobile joystick dead zone stops drift");
            BeginMobileFinger(41,new Vector2(140,560),width);BeginMobileFinger(87,new Vector2(width*.6f,240),width);
            Require(stickFinger==41&&lookFinger==87,"Mobile move and look own separate stable finger IDs");
            BeginMobileFinger(105,MobileRect(MobileAction.Attack,width).center,width);Require(mobileFingers.ContainsKey(105)&&stickFinger==41&&lookFinger==87,"Mobile attack finger coexists with movement and camera");
            EndMobileFinger(105);Require(!mobileFingers.ContainsKey(105)&&stickFinger==41&&lookFinger==87,"Canceled attack preserves independent movement and camera gestures");
            UpdateMobileFinger(41,new Vector2(222,560),Vector2.zero,TouchPhase.Moved,width);Require(stick.x>.99f,"Mobile drag updates movement");
            UpdateMobileFinger(87,new Vector2(width*.6f+10,240),new Vector2(10,0),TouchPhase.Moved,width);Require(yaw>priorYaw,"Mobile camera responds independently while moving");
            EndMobileFinger(87);Require(stickFinger==41&&lookFinger<0,"Ending camera gesture preserves movement gesture");EndMobileFinger(41);Require(stick==Vector2.zero,"Ending movement clears joystick");
            mobileFingers[12]=MobileAction.Attack;Pause();TickMobileControls();Require(mobileFingers.Count==0&&stick==Vector2.zero,"Opening a menu releases all mobile gameplay gestures");
            phase=Phase.Play;uiCoopOpen=true;BeginMobileFinger(62,new Vector2(140,560),width);Require(stickFinger<0,"Mobile modal panels block new movement gestures");
        }finally{ResetMobileControls();mobilePreviewEnabled=priorPreview;phase=priorPhase;uiCredits=priorCredits;uiNewConfirm=priorConfirm;uiCoopOpen=priorCoop;equippedWeapon=priorWeapon;yaw=priorYaw;pitch=priorPitch;aimToggle=priorAim;mobileSprint=priorSprint;}
    }
}
