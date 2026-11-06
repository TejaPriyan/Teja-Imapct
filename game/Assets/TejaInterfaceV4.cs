using UnityEngine;
using System.Collections.Generic;

public partial class TejaAdventure {
    enum PausePage { Journey, Settings, Controls }
    PausePage pausePage;
    bool uiQuestCollapsed;
    readonly List<Rect> uiHitRects=new List<Rect>(24);
    RenderTexture uiPortrait;
    bool uiPortraitAttempted;
    float uiUpgradeLockUntil;
    float UiScale {get{Rect view=UiViewport;return Mathf.Max(.1f,Mathf.Min(view.height/720f,view.width/960f));}}
    Vector2 UiScreenPoint(Vector2 screenPoint){return MobileUiPoint(screenPoint,UiViewport,UiScale);}
    bool IsPointerOverGameUi(Vector2 screenPoint){
        if(phase!=Phase.Play||uiCredits||uiNewConfirm||uiCoopOpen)return true;
        Vector2 p=UiScreenPoint(screenPoint);foreach(Rect r in uiHitRects)if(r.Contains(p))return true;return false;
    }
    void ResetUiNavigation(){pausePage=PausePage.Journey;uiUpgradeLockUntil=0;uiQuestCollapsed=false;}
    bool HandleUiEscape(){if(uiCoopOpen){uiCoopOpen=false;return true;}if(phase==Phase.Pause&&pausePage!=PausePage.Journey){pausePage=PausePage.Journey;return true;}return false;}
    bool UiInvisible(Rect r,bool enabled=true){
        if(Event.current.type==EventType.Repaint)uiHitRects.Add(r);
        bool before=GUI.enabled;GUI.enabled=before&&enabled;bool result=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=before;if(result)PlaySfx("ui",.65f);return result;
    }
    void UiBorder(Rect r,Color color){UiFill(new Rect(r.x,r.y,r.width,1),color);UiFill(new Rect(r.x,r.yMax-1,r.width,1),color);UiFill(new Rect(r.x,r.y,1,r.height),color);UiFill(new Rect(r.xMax-1,r.y,1,r.height),color);}
    void UiCard(Rect r,Color? accent=null){UiBox(r,uiSlate);if(accent.HasValue)UiFill(new Rect(r.x+16,r.y+1,r.width-32,1),accent.Value);}

    // One small portrait of the actual supplied character, rendered once. No duplicate rig or per-frame camera.
    void UiEnsurePortrait(){
        if(uiPortraitAttempted||!hero||!rig||!cam||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        uiPortraitAttempted=true;var renderers=hero.GetComponentsInChildren<Renderer>(true);var layers=new int[renderers.Length];
        var prior=RenderTexture.active;var portraitObject=new GameObject("Character portrait capture");var portraitCamera=portraitObject.AddComponent<Camera>();portraitCamera.enabled=false;
        try{
            for(int i=0;i<renderers.Length;i++){layers[i]=renderers[i].gameObject.layer;renderers[i].gameObject.layer=31;}
            uiPortrait=new RenderTexture(128,128,16,RenderTextureFormat.ARGB32);uiPortrait.name="Teja portrait";
            portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=uiSlate;portraitCamera.cullingMask=1<<31;portraitCamera.orthographic=true;portraitCamera.orthographicSize=.46f;portraitCamera.nearClipPlane=.05f;portraitCamera.farClipPlane=6;
            Vector3 focus=hero.position+Vector3.up*1.56f;portraitCamera.transform.position=focus+hero.forward*3;portraitCamera.transform.LookAt(focus);portraitCamera.targetTexture=uiPortrait;portraitCamera.Render();portraitCamera.targetTexture=null;
        }finally{for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.layer=layers[i];RenderTexture.active=prior;Destroy(portraitObject);}
    }

    void UiHud(){
        if(MobileControlsActive){UiMobileHud();return;}
        bool enabled=phase==Phase.Play&&!uiCredits&&!uiNewConfirm&&!uiCoopOpen&&!CoopWorldPaused,mobile=Application.isMobilePlatform;
        UiBox(new Rect(24,22,264,62),new Color(16/255f,24/255f,32/255f,.86f));
        UiCircle(47,49,12,uiGold,true);UiText(40,39,17,24,"T",uiSmall,uiGold);
        UiText(70,29,204,24,"TEJA'S IMPACT",uiSmall);UiText(70,55,204,20,regionName??"Aetherfall Meadow",uiTiny,uiCyan);
        UiFill(new Rect(70,80,44,1),uiGold);
        UiBox(new Rect(uiWidth-268,24,158,38),uiNavy);UiCircle(uiWidth-248,43,8,uiGold,true);UiText(uiWidth-232,32,114,24,coins.ToString("N0"),uiSmall,uiGold);
        if(UiButton(new Rect(uiWidth-100,24,76,38),"Menu",false,phase==Phase.Play&&!uiCredits&&!uiNewConfirm&&!uiCoopOpen)){pausePage=PausePage.Journey;Pause();}
        UiQuestTracker();
        Rect minimap=new Rect(uiWidth-182,92,158,158);UiMapArt(minimap,false);
        if(UiInvisible(minimap,enabled))TogglePanel(Phase.Map);
        if(UiButton(new Rect(uiWidth-174,258,142,32),"World map  M",false,enabled))TogglePanel(Phase.Map);
        if(boss!=null&&boss.active&&!boss.dead){
            float bw=Mathf.Min(380,uiWidth-600),bx=(uiWidth-bw)*.5f;UiBox(new Rect(bx-12,30,bw+24,64),uiNavy);
            UiText(bx,37,bw,25,"THE RIFT WARDEN",uiCenter);UiBar(new Rect(bx,72,bw,5),boss.hp/boss.max,uiRed);
        }
        if(phase==Phase.Play){UiWorldIndicators();UiCrosshair();}

        float hy=mobile?627:614;
        Rect health=new Rect(24,hy,304,82);UiCard(health,hp<MaxHealth*.25f?uiRed:uiCyan);
        if(uiPortrait)GUI.DrawTexture(new Rect(34,hy+14,54,54),uiPortrait,ScaleMode.ScaleToFit);
        else{UiCircle(61,hy+41,23,uiCyan,true);UiText(52,hy+28,20,30,"T",uiBody,uiCyan);}
        UiText(100,hy+11,115,23,"Teja  /  Lv. "+playerLevel,uiSmall);
        UiText(210,hy+11,98,22,Mathf.CeilToInt(hp)+" / "+Mathf.RoundToInt(MaxHealth),uiRight);
        UiBar(new Rect(100,hy+39,208,6),uiHealth/MaxHealth,hp<MaxHealth*.25f?uiRed:uiCyan);
        UiText(100,hy+49,208,17,playerLevel>=LevelCap?"MAX LEVEL":experience+" / "+ExperienceToNextLevel+" XP"+(skillPoints>0?"  ·  "+skillPoints+" points":""),uiTiny,uiMuted);
        UiBar(new Rect(100,hy+70,208,3),playerLevel>=LevelCap?1:experience/(float)ExperienceToNextLevel,uiGold);
        if(UiInvisible(health,enabled))TogglePanel(Phase.Character);

        if(!mobile){
            float wx=346,ww=Mathf.Min(220,uiWidth-674);Rect slot=new Rect(wx,636,ww,60);UiCard(slot);
            UiWeaponIcon(new Rect(wx+10,650,38,32),equippedWeapon,uiCyan);
            UiText(wx+56,644,ww-64,24,weapons[equippedWeapon].name,uiSmall);
            UiText(wx+56,671,ww-64,19,equippedWeapon==0?"Loadout  I":reloadTime>0?"Recharging":ammo[equippedWeapon]+" / "+weapons[equippedWeapon].magazine+"  ·  R reload",uiTiny,uiMuted);
            if(UiInvisible(slot,enabled))TogglePanel(Phase.Inventory);
        }else{
            UiCircle(140,551,65,new Color(.06f,.1f,.15f,.48f));UiCircle(140,551,65,new Color(.51f,.86f,.90f,.55f),true);UiCircle(140+stick.x*42,551+stick.y*42,24,uiCyan);
            if(UiButton(new Rect(28,447,117,39),mobileSprint?"Sprint on":"Sprint off",false,enabled))mobileSprint=!mobileSprint;
            if(UiButton(new Rect(uiWidth-304,492,130,39),"Switch weapon",false,enabled)){for(int j=1;j<=3;j++){int next=(equippedWeapon+j)%3;if((ownedWeapons&(1<<next))!=0){EquipWeapon(next);break;}}}
            if(equippedWeapon>0){if(UiButton(new Rect(uiWidth-164,492,140,39),aiming?"Aim on":"Aim off",false,enabled)){aimToggle=!aimToggle;aiming=aimToggle;}
                if(UiButton(new Rect(uiWidth-164,539,140,39),reloadTime>0?"Recharging":"Reload",false,enabled&&reloadTime<=0))Reload();}
        }
        float ax=uiWidth-304;
        if(UiAbility(new Rect(ax,618,64,78),"DODGE",mobile?"Dash":"SHIFT",dodgeCD,1.6f,uiCyan,enabled))Dodge();
        if(UiAbility(new Rect(ax+72,618,64,78),"FROST",mobile?"Nova":"E",burstCD,8,uiCyan,enabled))Burst();
        if(UiAbility(new Rect(ax+144,618,64,78),"JUMP",mobile?"Leap":"SPACE",0,1,uiIvory,enabled))Jump();
        if(UiAbility(new Rect(ax+216,606,64,90),equippedWeapon==0?"STRIKE":"FIRE",mobile?"Attack":"LMB",strikeCD,Mathf.Max(.1f,weapons[equippedWeapon].cooldown),uiGold,enabled))Strike();
        if(UiButton(new Rect(uiWidth-126,565,102,32),"Tonic  "+potions,false,enabled&&potions>0&&hp<MaxHealth))UsePotion();

        string prompt=UiInteractionPrompt();
        if(enabled&&prompt.Length>0&&UiButton(new Rect(uiWidth*.5f-166,504,332,40),(mobile?"Tap":"F")+"  /  "+prompt,true))Interact();
        if(noticeTime>0&&phase==Phase.Play){
            float nw=Mathf.Min(450,uiWidth-430);Color old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(noticeTime*2));UiBox(new Rect((uiWidth-nw)*.5f,546,nw,52),uiNavy);UiText((uiWidth-nw)*.5f+14,553,nw-28,40,notice,uiSmall,uiGold);GUI.color=old;
        }
        if(!mobile&&phase==Phase.Play){UiFill(new Rect(0,700,uiWidth,20),new Color(.025f,.06f,.09f,.88f));UiText(28,701,460,18,"Move mouse to look  ·  Hold Alt for cursor  ·  Esc menu",uiTiny,uiIvory);}
        if(hp<MaxHealth*.25f&&phase==Phase.Play)UiFill(new Rect(0,0,uiWidth,3),uiRed);
    }
    string UiInteractionPrompt(){
        string prompt=WorldV6Prompt();if(prompt.Length>0)return prompt;
        prompt=NearbyProgressionPrompt();if(prompt.Length>0)return prompt;
        if(hero&&FlatDistance(hero.position,merchantPosition)<4)return "Lantern Armory";
        if(NearChest())return "Open treasure chest";
        foreach(var sigil in sigils)if(!sigil.taken&&FlatDistance(hero.position,sigil.root.position)<2.3f)return "Recover lost sigil";
        return "";
    }
    void UiQuestTracker(){
        bool enabled=phase==Phase.Play&&!uiCredits;float h=uiQuestCollapsed?56:133;
        UiBox(new Rect(24,102,264,h),new Color(16/255f,24/255f,32/255f,.86f));UiFill(new Rect(24,115,2,h-26),uiGold);
        UiText(39,112,202,19,"THE SLEEPING RIFT",uiSmall,uiGold);
        if(UiButton(new Rect(248,110,28,28),uiQuestCollapsed?"+":"−",false,enabled))uiQuestCollapsed=!uiQuestCollapsed;
        if(uiQuestCollapsed){UiText(39,137,218,18,count+" / 3 sigils  ·  Q journal",uiTiny);return;}
        string objective=count<3?"Recover the scattered sigils.":boss!=null&&boss.dead?"Aetherfall is safe again.":boss!=null&&boss.active?"Defeat the Rift Warden.":"Reach the northern Warden's Gate.";
        UiText(39,144,229,41,objective,uiSmall,uiIvory);
        for(int i=0;i<3;i++)UiCircle(44+i*20,207,4,i<count?uiGold:uiMuted,i>=count);
        UiText(105,198,166,24,count+" / 3 sigils",uiTiny,uiGold);
        int id=trackedMission;if(id<0||id>2||missionStates[id]==3){id=-1;for(int i=0;i<3;i++)if(missionStates[i]==1||missionStates[i]==2){id=i;break;}}
        if(id>=0){UiBox(new Rect(24,245,264,100),new Color(16/255f,24/255f,32/255f,.80f));UiFill(new Rect(24,258,2,70),uiCyan);UiText(39,257,229,22,missionNames[id],uiSmall,uiCyan);UiText(39,287,229,45,MissionObjective(id),uiSmall,uiMuted);}
        if(UiButton(new Rect(24,id>=0?355:245,142,30),"Quest journal  Q",false,enabled))TogglePanel(Phase.Journal);
    }
    bool UiAbility(Rect r,string name,string hint,float cooldown,float maxCooldown,Color accent,bool enabled){
        bool ready=cooldown<=0,click=UiButton(r,"",false,enabled&&ready);UiFill(new Rect(r.x+12,r.y+1,r.width-24,1),accent);
        float cx=r.center.x,cy=r.y+23;
        if(name=="STRIKE"||name=="FIRE")UiWeaponIcon(new Rect(cx-15,cy-12,30,26),equippedWeapon,accent);
        else if(name=="DODGE"){UiLine(new Vector2(cx-13,cy+6),new Vector2(cx-2,cy-6),2,accent);UiLine(new Vector2(cx-2,cy+6),new Vector2(cx+9,cy-6),2,accent);}
        else if(name=="FROST"){for(int i=0;i<3;i++){float a=i*Mathf.PI/3;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*12;UiLine(new Vector2(cx,cy)-d,new Vector2(cx,cy)+d,1.5f,accent);}}
        else{UiLine(new Vector2(cx-9,cy+3),new Vector2(cx,cy-7),2,accent);UiLine(new Vector2(cx,cy-7),new Vector2(cx+9,cy+3),2,accent);}
        UiText(r.x+2,r.y+42,r.width-4,19,ready?name:cooldown.ToString("0.0")+"s",uiTinyCenter,accent);
        UiText(r.x+2,r.yMax-21,r.width-4,18,hint,uiTinyCenter,uiMuted);
        if(!ready)UiFill(new Rect(r.x+10,r.yMax-3,(r.width-20)*(1-Mathf.Clamp01(cooldown/maxCooldown)),2),accent);return click;
    }

    void UiCharacterPanel(){
        Rect f=UiFrame(1040,648);float x=f.x+28,y=f.y+23;UiClose(f,Phase.Character);
        UiText(x,y,760,20,"CHARACTER PROGRESSION",uiTiny,uiGold);UiText(x,y+30,f.width-120,44,"Shape your journey.",uiBig);
        UiText(x,y+79,f.width-80,22,"Strengthen your resolve.",uiSmall,uiMuted);
        UiText(x,y+114,f.width-56,24,"TEJA  /  LEVEL "+playerLevel+"      "+skillPoints+" SKILL POINTS      "+coins.ToString("N0")+" COINS",uiSmall,uiCyan);
        float lw=284,rx=x+304,rw=f.width-360,top=y+155;UiCard(new Rect(x,top,lw,377));
        UiText(x+18,top+15,lw-36,25,"Attributes",uiHeading);UiText(x+18,top+51,lw-36,22,Mathf.RoundToInt(MaxHealth)+" max HP  ·  "+DamageMultiplier.ToString("0.00")+"× damage",uiSmall,uiGold);
        bool lockout=Time.realtimeSinceStartup<uiUpgradeLockUntil;
        for(int a=0;a<2;a++){
            int rank=a==0?vitalityRank:powerRank;float ay=top+94+a*136;bool can=rank<5&&skillPoints>0&&!lockout;
            UiText(x+18,ay,lw-36,23,(a==0?"VITALITY":"FOCUS")+"  /  "+rank+" of 5",uiSmall);
            string effect=a==0?"+"+(rank*5)+" HP from vitality":"+"+(rank*5)+"% base damage from focus";
            string next=rank>=5?"Maximum attribute rank":a==0?"Next: "+Mathf.RoundToInt(MaxHealth)+" → "+Mathf.RoundToInt(MaxHealth+5)+" max HP":"Next: "+DamageMultiplier.ToString("0.00")+"× → "+(DamageMultiplier+.05f).ToString("0.00")+"× damage";
            UiText(x+18,ay+28,lw-36,20,effect,uiTiny,uiMuted);UiText(x+18,ay+53,lw-36,22,next,uiTiny,rank<5?uiCyan:uiMuted);
            string action=rank>=5?"Maximum rank":skillPoints==0?"Earn a level for skill points":"Improve  /  1 skill point";
            if(UiButton(new Rect(x+18,ay+82,lw-36,34),action,false,can)){if(SpendSkillPoint(a==0))uiUpgradeLockUntil=Time.realtimeSinceStartup+.28f;}
        }
        for(int i=0;i<3;i++){
            float cy=top+i*129;bool owned=(ownedWeapons&(1<<i))!=0,equipped=equippedWeapon==i;int rank=weaponRanks[i],cost=WeaponUpgradeCost(i);Rect card=new Rect(rx,cy,rw,119);UiCard(card);
            if(equipped)UiBorder(card,new Color(uiCyan.r,uiCyan.g,uiCyan.b,.65f));UiWeaponIcon(new Rect(rx+13,cy+24,53,45),i,owned?uiCyan:uiMuted);
            float dx=rx+80,dw=rw-96;UiText(dx,cy+11,dw,23,weapons[i].name,uiBody);
            UiText(dx,cy+38,dw,19,(owned?(equipped?"EQUIPPED":"OWNED"):"LOCKED")+"  ·  RANK "+rank+" / 3  ·  +"+(rank*15)+"%",uiTiny,owned?uiCyan:uiMuted);
            float mul=1+.02f*(playerLevel-1)+.05f*powerRank+.15f*rank;
            string result=owned?(rank<3?"Damage "+(weapons[i].damage*mul).ToString("0.#")+" → "+(weapons[i].damage*(mul+.15f)).ToString("0.#")+" next rank":"Damage "+(weapons[i].damage*mul).ToString("0.#")+"  ·  Fully upgraded"):"Unlock at Lantern Armory for "+weapons[i].price+" coins";
            UiText(dx,cy+61,dw,20,result,uiTiny,uiMuted);
            string action=!owned?"Visit Lantern Armory":rank>=3?"Maximum rank":coins<cost?"Need "+(cost-coins)+" more coins":"Upgrade  /  "+cost+" coins";
            if(UiButton(new Rect(dx,cy+87,dw,27),action,owned&&rank<3&&coins>=cost,owned&&rank<3&&coins>=cost&&!lockout)){if(UpgradeWeapon(i))uiUpgradeLockUntil=Time.realtimeSinceStartup+.28f;}
        }
        UiText(x,f.yMax-77,f.width-56,34,noticeTime>0?notice:"Gain experience from foes and quests. Every level grants one skill point and +4 maximum HP.",uiSmall,noticeTime>0?uiGold:uiMuted);
        UiText(x,f.yMax-32,f.width-56,20,"C / Esc  Return    ·    Improvements save with your journey.",uiTiny);
    }

    void UiPausePanel(){
        if(phase!=Phase.Pause){V03UiPausePanel();return;}
        if(MobileControlsActive){UiMobilePausePanel();return;}
        Rect f=UiFrame(850,596);float x=f.x+30,y=f.y+27;UiText(x,y,640,20,"JOURNEY PAUSED",uiTiny,uiGold);
        UiText(x,y+32,680,48,pausePage==PausePage.Journey?"A moment of stillness.":pausePage==PausePage.Settings?"Settings":"Controls",uiBig);
        if(UiButton(new Rect(f.xMax-68,f.y+20,40,36),"X")){phase=Phase.Play;pausePage=PausePage.Journey;}
        if(pausePage==PausePage.Journey){
            UiText(x,y+91,f.width-60,27,regionName+"  ·  Level "+playerLevel+"  ·  "+coins+" coins",uiSmall,uiMuted);
            if(UiButton(new Rect(x,y+139,f.width-60,44),"Resume journey",true))phase=Phase.Play;
            float bw=(f.width-72)*.5f;
            string[] labels={"Quest journal  /  Q","Character  /  C","Weapons & inventory  /  I","World map  /  M","Settings","Controls"};
            for(int i=0;i<6;i++)if(UiButton(new Rect(x+(i%2)*(bw+12),y+198+(i/2)*53,bw,41),labels[i])){
                if(i<4)phase=i==0?Phase.Journal:i==1?Phase.Character:i==2?Phase.Inventory:Phase.Map;
                else pausePage=i==4?PausePage.Settings:PausePage.Controls;
            }
            UiLine(new Vector2(x,y+376),new Vector2(f.xMax-30,y+376),1,new Color(.21f,.30f,.34f));
            if(UiButton(new Rect(x,y+397,bw,41),CoopIsGuest?"Host saves this journey":"Save journey",false,!CoopIsGuest))SaveJourney(true);
            if(UiButton(new Rect(x+bw+12,y+397,bw,41),CoopIsGuest?"Leave & return to title":"Save & return to title")){SaveJourney(false);EndCoop(true);}
            if(UiButton(new Rect(x,y+451,f.width-60,38),!TejaPlatformV7.InternetRoomsAvailable?"Play options  /  Platform info":CoopActive?"Online co-op  /  Session details":"Online co-op  /  Host or join"))uiCoopOpen=true;
            UiText(x,y+505,f.width-60,35,noticeTime>0?notice:CoopIsGuest?"Shared progress is saved by your host. Your solo journey stays on this device.":"Your progress stays on this device.",uiSmall,noticeTime>0?uiGold:uiMuted);
        }else if(pausePage==PausePage.Settings)UiSettingsPage(f,x,y);
        else UiControlsPage(f,x,y);
    }
    void UiSettingsPage(Rect f,float x,float y){
        float w=f.width-60;UiText(x,y+91,w,22,"Tune the view and feel of your journey.",uiSmall,uiMuted);
        UiText(x,y+136,280,24,"Graphics quality",uiBody);if(UiButton(new Rect(x+w-224,y+129,224,37),new[]{"Low","Balanced","High"}[Mathf.Clamp(graphicsPreset,0,2)]))ApplyGraphicsPreset((graphicsPreset+1)%3);
        UiText(x,y+172,w,30,graphicsPreset==0?"30 FPS cap · shadows off · lowest GPU load":graphicsPreset==1?"60 FPS cap · hard shadows · 2× antialiasing":"60 FPS cap · soft shadows · 4× antialiasing",uiTiny,uiMuted);
        UiText(x,y+207,280,24,"Game audio",uiBody);if(UiButton(new Rect(x+w-224,y+200,224,35),soundOn?"On":"Off"))SetAudioEnabled(!soundOn);
        UiV6AudioSettings(x,y+250,w);
        UiText(x,y+337,280,24,"Look sensitivity",uiBody);float next=GUI.HorizontalSlider(new Rect(x+300,y+343,w-370,18),lookSensitivity,.35f,2.5f);if(Mathf.Abs(next-lookSensitivity)>.001f){lookSensitivity=next;SaveControlSettings();}UiText(x+w-62,y+335,62,24,lookSensitivity.ToString("0.00")+"×",uiSmall,uiCyan);
        UiText(x,y+383,280,24,"Invert vertical look",uiBody);if(UiButton(new Rect(x+w-224,y+376,224,35),invertLook?"On":"Off")){invertLook=!invertLook;SaveControlSettings();}
        UiText(x,y+429,280,24,"Camera distance",uiBody);float distance=GUI.HorizontalSlider(new Rect(x+300,y+436,w-370,18),cameraDistanceOffset,-2,3);if(Mathf.Abs(distance-cameraDistanceOffset)>.001f){cameraDistanceOffset=distance;SaveControlSettings();}UiText(x+w-62,y+427,62,24,cameraDistanceOffset.ToString("+0.0;-0.0;0.0"),uiSmall,uiCyan);
        if(UiButton(new Rect(x,f.yMax-67,210,39),"About & credits"))uiCredits=true;
        if(UiButton(new Rect(f.xMax-240,f.yMax-67,210,39),"Back to pause"))pausePage=PausePage.Journey;
    }
    void UiControlsPage(Rect f,float x,float y){
        if(MobileControlsActive){UiMobileControlsPage(f,x,y);return;}
        UiText(x,y+91,f.width-60,40,"Left-click to attack or shoot. Move the mouse to look around.",uiSmall,uiCyan);
        string[] names={"Move / sprint","Attack / fire","Camera orbit","Precision aim","Recenter / zoom","Jump / dodge","Frost nova / interact","Reload / tonic","Weapons / lock on","Journal / character / loadout / map"};
        string[] keys={"WASD or arrows / Left Ctrl","Left mouse or J (hold to repeat)","Move mouse / hold Alt for cursor","Hold right mouse or toggle V","T / mouse wheel","Space / Left Shift","E / F","R / H","1, 2, 3 / Tab","Q / C / I / M"};
        for(int i=0;i<names.Length;i++){float ry=y+146+i*29;if(i%2==0)UiFill(new Rect(x-5,ry-3,f.width-50,28),new Color(.08f,.15f,.20f,.45f));UiText(x+9,ry,330,25,names[i],uiSmall,uiMuted);UiText(x+350,ry,f.width-410,25,keys[i],uiSmall);}
        if(UiButton(new Rect(f.xMax-240,f.yMax-67,210,39),"Back to pause"))pausePage=PausePage.Journey;
    }
}
