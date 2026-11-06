using UnityEngine;
using System.IO;

// Shared IMGUI drawing tools. Journey layouts live in TejaInterfaceV4.cs.
public partial class TejaAdventure {
    readonly Color uiNavy = new Color(16/255f,24/255f,32/255f,.97f);
    readonly Color uiSlate = new Color(21/255f,38/255f,51/255f,.98f);
    readonly Color uiIvory = new Color(241/255f,239/255f,230/255f);
    readonly Color uiMuted = new Color(156/255f,174/255f,178/255f);
    readonly Color uiCyan = new Color(131/255f,221/255f,229/255f);
    readonly Color uiGold = new Color(229/255f,182/255f,92/255f);
    readonly Color uiRed = new Color(.91f,.42f,.46f);
    Texture2D uiRound,uiHover,uiPressed,uiDisk,uiRing,uiArrow;
    GUIStyle uiPanel,uiButton,uiPrimary,uiSmall,uiBody,uiHeading,uiBig,uiTitle,uiCenter,uiTiny,uiTinyCenter,uiRight;
    bool uiReady,uiCredits,uiNewConfirm;
    Phase uiLastPhase;
    float uiEntered,uiWidth,uiHealth=100;

    void UiInitialize(){
        if(uiReady)return;
        uiReady=true;uiLastPhase=phase;uiEntered=Time.realtimeSinceStartup;
        uiRound=UiTexture(48,0);uiHover=UiTexture(48,1);uiPressed=UiTexture(48,2);
        uiDisk=UiTexture(96,3);uiRing=UiTexture(96,4);uiArrow=UiTexture(32,5);
        var font=Resources.Load<Font>("Fonts/Rubik-Regular");if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial","Roboto","DejaVu Sans"},20);
        var display=Resources.Load<Font>("Fonts/Rajdhani-SemiBold");if(!display)display=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial","DejaVu Sans"},88);
        uiBody=new GUIStyle(GUI.skin.label){font=font,fontSize=17,wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0),normal={textColor=uiIvory}};
        uiSmall=new GUIStyle(uiBody){fontSize=14};
        uiTiny=new GUIStyle(uiBody){fontSize=12,normal={textColor=uiMuted}};
        uiTinyCenter=new GUIStyle(uiTiny){alignment=TextAnchor.MiddleCenter};
        uiHeading=new GUIStyle(uiBody){fontSize=24,font=display};
        uiBig=new GUIStyle(uiBody){fontSize=34,font=display};
        uiTitle=new GUIStyle(uiBody){fontSize=90,font=display,wordWrap=false};
        uiCenter=new GUIStyle(uiBody){alignment=TextAnchor.MiddleCenter};
        uiRight=new GUIStyle(uiSmall){alignment=TextAnchor.MiddleRight};
        uiPanel=new GUIStyle(){border=new RectOffset(12,12,12,12),normal={background=uiRound}};
        uiButton=new GUIStyle(GUI.skin.button){font=font,fontSize=15,wordWrap=false,alignment=TextAnchor.MiddleCenter,border=new RectOffset(12,12,12,12),padding=new RectOffset(8,8,4,4),
            normal={background=uiRound,textColor=uiIvory},hover={background=uiHover,textColor=Color.white},active={background=uiPressed,textColor=Color.white},focused={background=uiHover,textColor=Color.white}};
        uiPrimary=new GUIStyle(uiButton){normal={textColor=uiNavy},hover={textColor=uiNavy},active={textColor=uiNavy},focused={textColor=uiNavy}};
    }

    Texture2D UiTexture(int size,int kind){
        var t=new Texture2D(size,size,TextureFormat.RGBA32,false);t.name="Teja UI / "+kind;t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float alpha=1,value=1;
            float dx=x-(size-1)*.5f,dy=y-(size-1)*.5f,d=Mathf.Sqrt(dx*dx+dy*dy);
            if(kind==3)alpha=Mathf.Clamp01(size*.5f-1-d);
            else if(kind==4)alpha=Mathf.Clamp01(size*.5f-1-d)*Mathf.Clamp01(d-(size*.5f-4));
            else if(kind==5){float u=x/(float)(size-1),v=y/(float)(size-1);alpha=(Mathf.Abs(u-.5f)<(.85f-v)*.43f&&v>.15f&&v<.87f&&(v>.37f||Mathf.Abs(u-.5f)>.085f))?1:0;}
            else{
                float qx=Mathf.Max(Mathf.Abs(dx)-(size*.5f-10),0),qy=Mathf.Max(Mathf.Abs(dy)-(size*.5f-10),0);
                alpha=Mathf.Clamp01(9-Mathf.Sqrt(qx*qx+qy*qy));
                value=kind==1?1:(kind==2?.76f:.94f);
            }
            pixels[y*size+x]=new Color(value,value,value,alpha);
        }
        t.SetPixels(pixels);t.Apply(false,true);return t;
    }

    void UiBox(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.Box(r,GUIContent.none,uiPanel);GUI.color=old;}
    void UiFill(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    void UiLabel(Rect r,string text,GUIStyle style=null,Color? color=null){var old=GUI.color;if(color.HasValue)GUI.color=color.Value;GUI.Label(r,text,style??uiBody);GUI.color=old;}
    void UiText(float x,float y,float w,float h,string text,GUIStyle style=null,Color? color=null){UiLabel(new Rect(x,y,w,h),text,style,color);}
    bool UiButton(Rect r,string text,bool primary=false,bool enabled=true){
        if(Event.current.type==EventType.Repaint)uiHitRects.Add(r);
        bool before=GUI.enabled;GUI.enabled=before&&enabled;var old=GUI.backgroundColor;GUI.backgroundColor=primary?uiGold:uiSlate;
        bool clicked=GUI.Button(r,text,primary?uiPrimary:uiButton);GUI.backgroundColor=old;GUI.enabled=before;if(clicked)PlaySfx("ui",.65f);return clicked;
    }
    void UiLine(Vector2 a,Vector2 b,float thickness,Color color){
        var previous=GUI.matrix;float angle=Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;
        GUI.matrix=previous*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
        UiFill(new Rect(0,-thickness*.5f,Vector2.Distance(a,b),thickness),color);GUI.matrix=previous;
    }
    void UiCircle(float x,float y,float radius,Color color,bool ring=false){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(x-radius,y-radius,radius*2,radius*2),ring?uiRing:uiDisk);GUI.color=old;}
    void UiBar(Rect r,float value,Color color){UiFill(r,new Color(.18f,.25f,.29f,.98f));if(value>.001f)UiFill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value),r.height),color);}
    string UiWeaponName(int index){return weapons[index].name;}
    string UiTime(float value){return Mathf.FloorToInt(value/60).ToString("00")+":"+Mathf.FloorToInt(value%60).ToString("00");}

    void OnGUI(){
        var priorTarget=RenderTexture.active;bool capture=uiPreviewTarget&&Event.current.type==EventType.Repaint;if(capture){RenderTexture.active=uiPreviewTarget;GL.PushMatrix();GL.LoadPixelMatrix(0,Screen.width,Screen.height,0);}
        UiInitialize();InitializeV6Ui();uiWidth=UiViewport.width/UiScale;
        if(Event.current.type==EventType.Repaint){uiHitRects.Clear();UiEnsurePortrait();}
        var priorMatrix=GUI.matrix;var priorColor=GUI.color;
        GUI.matrix=Matrix4x4.TRS(UiOrigin,Quaternion.identity,new Vector3(UiScale,UiScale,1));
        if(phase!=uiLastPhase){uiLastPhase=phase;uiEntered=Time.realtimeSinceStartup;uiNewConfirm=false;}
        if(Event.current.type==EventType.Repaint)uiHealth=Mathf.Lerp(uiHealth,hp,1-Mathf.Exp(-Time.unscaledDeltaTime*9));
        bool originalEnabled=GUI.enabled;bool baseEnabled=originalEnabled&&!mobileMenuTapBlocked;GUI.enabled=baseEnabled&&!uiCredits&&!uiCoopOpen;
        if(MobilePortraitBlocked)UiMobilePortraitPrompt();else if(phase==Phase.Title)UiTitleScreen();else{
            UiHud();CoopHud();if(phase==Phase.Play)DrawHitConfirm();
            if(phase==Phase.Shop||phase==Phase.Inventory)UiEquipmentPanel(phase==Phase.Shop);
            else if(phase==Phase.Map)UiWorldMap();
            else if(phase==Phase.Dialogue)UiDialoguePanel();
            else if(phase==Phase.Journal)UiJournalPanel();
            else if(phase==Phase.Character)UiCharacterPanel();
            else if(phase==Phase.Win&&CoopActive)UiCoopVictory();
            else if(phase==Phase.Pause||phase==Phase.Win||phase==Phase.Defeat)UiPausePanel();
        }
        GUI.enabled=baseEnabled&&!uiCoopOpen;if(uiCredits&&!MobilePortraitBlocked)UiCreditsPanel();GUI.enabled=baseEnabled;if(uiCoopOpen&&!MobilePortraitBlocked)UiCoopPanel();
        GUI.color=priorColor;GUI.matrix=priorMatrix;GUI.enabled=originalEnabled;
        if(capture){FinishUiPreview();GL.PopMatrix();RenderTexture.active=priorTarget;}
    }

    void UiTitleScreen(){
        bool titleEnabled=GUI.enabled;GUI.enabled=titleEnabled&&!uiNewConfirm;
        float width=Mathf.Min(560,uiWidth*.56f),left=50;
        UiFill(new Rect(0,0,width+35,720),new Color(.025f,.065f,.10f,.86f));
        UiFill(new Rect(0,0,6,720),uiGold);
        UiText(left,44,width-70,22,"T E J A   /   A N   A E T H E R F A L L   S T O R Y",uiTiny,uiGold);
        UiText(left-3,106,width,116,"Teja's",uiTitle);
        UiText(left+36,202,width,116,"Impact",uiTitle);
        UiLine(new Vector2(left+4,338),new Vector2(left+258,338),1,uiGold);
        UiCircle(left+269,338,3,uiGold);
        UiText(left+4,360,width-50,30,"CHAPTER 01  /  THE SLEEPING RIFT",uiSmall,uiCyan);
        UiText(left+4,405,width-58,72,"A world above the clouds.\nA wanderer with a spark to spare.",uiBody);
        bool saved=HasJourneySave();
        if(UiButton(new Rect(left,510,Mathf.Min(335,width-62),54),saved?"Continue journey   >":"Begin journey   >",true)&&!uiCredits)StartGame(saved);
        if(saved&&UiButton(new Rect(left,576,160,40),"New journey")&&!uiCredits)uiNewConfirm=true;
        if(UiButton(new Rect(left+(saved?173:0),576,160,40),"About & credits"))uiCredits=true;
        if(UiButton(new Rect(left,626,Mathf.Min(335,width-62),38),TejaPlatformV7.InternetRoomsAvailable?"Online co-op  /  Host or join":"Play options  /  Platform info"))uiCoopOpen=true;
        UiText(left,677,width-60,18,"v0.7  /  "+TejaPlatformV7.Edition.ToUpperInvariant()+" EDITION",uiTiny,uiGold);
        UiText(left,698,width-60,18,TejaPlatformV7.InternetRoomsAvailable?"Solo or two travellers  /  Host saves the journey":"Solo adventure  /  Progress stays on this device",uiTiny);
        float right=uiWidth-318;
        UiText(right,60,270,30,"AETHERFALL",uiHeading);
        UiText(right,101,280,48,"New lands and people to discover.\nExplore. Grow. Protect Aetherfall.",uiSmall);
        UiBox(new Rect(right,noticeTime>0?480:540,270,noticeTime>0?172:112),new Color(.035f,.08f,.115f,.94f));
        UiText(right+18,noticeTime>0?495:555,234,23,noticeTime>0?"JOURNEY STATUS":"YOUR NEXT ADVENTURE",uiTiny,uiGold);
        UiText(right+18,noticeTime>0?526:582,234,noticeTime>0?113:53,noticeTime>0?notice:TejaPlatformV7.InternetRoomsAvailable?"Hidden places. New stories.\nA friend beside you.":"Hidden places. New stories.\nAn island to explore.",noticeTime>0?uiSmall:uiBody);
        GUI.enabled=titleEnabled;
        if(uiNewConfirm){
            UiFill(new Rect(0,0,uiWidth,720),new Color(.02f,.05f,.08f,.8f));float x=uiWidth*.5f-230;
            UiBox(new Rect(x,240,460,240),uiNavy);UiText(x+28,264,400,40,"Begin a new journey?",uiHeading);
            UiText(x+28,325,404,60,"Your current local progress will be replaced when the new journey is saved.");
            if(UiButton(new Rect(x+28,414,192,42),"Begin again",true))StartGame(false);
            if(UiButton(new Rect(x+232,414,200,42),"Keep my journey"))uiNewConfirm=false;
        }
    }

    void V03UiHud(){
        bool interactable=phase==Phase.Play&&!uiCredits;
        UiBox(new Rect(24,22,299,64),new Color(.025f,.08f,.115f,.86f));
        UiCircle(48,52,13,uiGold,true);UiText(40,42,17,25,"T",uiSmall,uiGold);
        UiText(73,30,226,24,"TEJA'S IMPACT",uiSmall,uiIvory);
        UiText(73,56,226,22,regionName??"Aetherfall Meadow",uiTiny,uiCyan);
        float tx=uiWidth-684;
        if(UiButton(new Rect(tx,24,86,42),"Quests Q",false,interactable))TogglePanel(Phase.Journal);
        if(UiButton(new Rect(tx+94,24,106,42),"Character C",false,interactable))TogglePanel(Phase.Character);
        if(UiButton(new Rect(tx+208,24,96,42),"Loadout I",false,interactable))TogglePanel(Phase.Inventory);
        if(UiButton(new Rect(tx+312,24,78,42),"Map M",false,interactable))TogglePanel(Phase.Map);
        if(UiButton(new Rect(tx+398,24,68,42),"Pause",false,interactable))Pause();
        UiBox(new Rect(uiWidth-203,24,178,42),uiNavy);UiCircle(uiWidth-178,45,10,uiGold,true);
        UiText(uiWidth-160,31,120,27,coins.ToString("N0")+"  coins",uiSmall,uiGold);

        UiBox(new Rect(24,105,299,149),new Color(.025f,.08f,.115f,.84f));
        UiFill(new Rect(24,120,3,111),uiGold);UiText(43,118,255,22,"WORLD QUEST  /  CHAPTER 01",uiTiny,uiGold);
        UiText(43,147,265,37,"The sleeping rift",uiHeading);
        string quest=count<3?"Recover the scattered sigils":boss!=null&&boss.dead?"Aetherfall is safe again":boss!=null&&boss.active?"Defeat the Rift Warden":"Reach the northern Warden's Gate";
        UiText(43,190,258,35,quest,uiSmall);
        for(int i=0;i<3;i++){UiCircle(51+i*27,237,5,i<count?uiGold:uiMuted,i>=count);}
        UiText(134,226,163,23,count+" / 3  sigils",uiTiny,uiGold);
        UiQuestTracker();
        if(boss!=null&&boss.active&&!boss.dead){
            float x=uiWidth*.5f-195;UiBox(new Rect(x-12,83,414,73),new Color(.03f,.06f,.11f,.9f));
            UiText(x,88,390,28,"THE RIFT WARDEN",uiCenter);UiBar(new Rect(x,124,390,7),boss.hp/boss.max,uiRed);
            UiText(x,139,390,18,"Leave the red circles before they erupt",uiTiny);
        }
        UiMiniMap(new Rect(uiWidth-194,89,170,170));
        if(phase==Phase.Play){UiWorldIndicators();UiCrosshair();}

        bool mobile=Application.isMobilePlatform;
        float healthY=mobile?630:599;
        UiBox(new Rect(24,healthY,298,mobile?76:94),new Color(.025f,.075f,.12f,.91f));
        UiCircle(53,healthY+25,17,uiCyan,true);UiText(47,healthY+15,15,25,"T",uiBody,uiCyan);
        UiText(81,healthY+11,105,25,"Teja / Lv. "+playerLevel,uiSmall);UiText(186,healthY+11,113,25,Mathf.CeilToInt(hp)+" / "+MaxHealth+" HP",uiRight);
        UiBar(new Rect(81,healthY+38,217,7),uiHealth/MaxHealth,hp<MaxHealth*.25f?uiRed:uiCyan);
        UiText(42,healthY+50,255,20,playerLevel>=LevelCap?"LEVEL 20  /  MAXIMUM LEVEL":experience+" / "+ExperienceToNextLevel+" XP"+(skillPoints>0?"   ·   "+skillPoints+" skill points":""),uiTiny,uiGold);
        UiBar(new Rect(42,healthY+70,256,3),playerLevel>=LevelCap?1:experience/(float)ExperienceToNextLevel,uiGold);
        if(!mobile)UiText(42,healthY+78,255,16,"C  /  CHARACTER & WEAPON UPGRADES",uiTiny);

        float slotX=Mathf.Max(342,uiWidth*.5f-174);
        if(!mobile){
            UiBox(new Rect(slotX,609,255,84),new Color(.025f,.075f,.12f,.91f));UiWeaponIcon(new Rect(slotX+14,619,49,41),equippedWeapon,uiGold);
            UiText(slotX+77,620,168,24,UiWeaponName(equippedWeapon),uiSmall);
            string ammoText=equippedWeapon==0?"1 / 2 / 3  switch weapons":reloadTime>0?"Reloading...":ammo[equippedWeapon]+" / "+weapons[equippedWeapon].magazine+"   R  reload";
            UiText(slotX+77,650,168,24,ammoText,uiTiny,uiCyan);
            UiFill(new Rect(0,700,uiWidth,20),new Color(.025f,.06f,.09f,.88f));UiText(28,703,uiWidth-56,17,"Ctrl Sprint   WASD Move   Mouse Look / RMB Aim   Tab Lock-on   Space Jump   F Interact   H Potion   Q Quests   C Character   Esc Pause",uiTiny);
        }else{
            UiCircle(140,551,67,new Color(.04f,.11f,.17f,.46f));UiCircle(140,551,67,new Color(.77f,.86f,.85f,.47f),true);
            UiCircle(140+stick.x*42,551+stick.y*42,25,new Color(.52f,.82f,.81f,.68f));
            UiText(89,625,105,19,"MOVE",uiTiny);
            if(UiButton(new Rect(uiWidth-286,538,116,52),equippedWeapon==0?"Sword / swap":ammo[equippedWeapon]+" / "+weapons[equippedWeapon].magazine+"  / swap",false,interactable)){
                for(int j=1;j<=3;j++){int next=(equippedWeapon+j)%3;if((ownedWeapons&(1<<next))!=0){EquipWeapon(next);break;}}
            }
            if(equippedWeapon>0&&UiButton(new Rect(uiWidth-161,478,137,49),reloadTime>0?"Reloading":"Reload",false,interactable&&reloadTime<=0))Reload();
            if(UiButton(new Rect(28,447,117,39),mobileSprint?"Sprint: on":"Sprint: off",false,interactable))mobileSprint=!mobileSprint;
            if(equippedWeapon>0&&UiButton(new Rect(uiWidth-286,478,116,49),aiming?"Aim: on":"Aim: off",false,interactable))aiming=!aiming;
        }
        float ax=uiWidth-291;
        if(UiAbility(new Rect(ax,615,79,78),"DODGE",mobile?"dash":"SHIFT",dodgeCD,1.6f,uiCyan,interactable))Dodge();
        if(UiAbility(new Rect(ax+89,615,79,78),"FROST",mobile?"nova":"E",burstCD,8,uiGold,interactable))Burst();
        if(UiAbility(new Rect(ax+178,605,88,88),equippedWeapon==0?"STRIKE":"FIRE",mobile?"attack":"J",0,1,uiIvory,interactable))Strike();
        if(UiButton(new Rect(uiWidth-157,545,61,48),"Jump",false,interactable))Jump();
        if(UiButton(new Rect(uiWidth-88,545,64,48),"H  "+potions,false,interactable&&potions>0&&hp<MaxHealth))UsePotion();

        if(interactable){
            string prompt=NearbyProgressionPrompt();
            if(prompt.Length>0){}
            else if(hero&&FlatDistance(hero.position,merchantPosition)<4)prompt="Visit the Lantern Armory";
            else if(NearChest())prompt="Open the treasure chest";
            else foreach(var sigil in sigils)if(!sigil.taken&&FlatDistance(hero.position,sigil.root.position)<2.3f){prompt="Recover the lost sigil";break;}
            if(prompt.Length>0){float pw=330;if(UiButton(new Rect(uiWidth*.5f-pw*.5f,495,pw,47),(mobile?"Tap":"F")+"  /  "+prompt,true))Interact();}
        }
        if(noticeTime>0&&phase==Phase.Play){
            float nw=Mathf.Min(560,uiWidth-680);UiBox(new Rect(uiWidth*.5f-nw*.5f,550,nw,48),new Color(.035f,.085f,.12f,.92f));
            UiText(uiWidth*.5f-nw*.5f+14,557,nw-28,38,notice,uiSmall,uiGold);
        }
        if(hp<MaxHealth*.25f&&phase==Phase.Play){UiFill(new Rect(0,0,uiWidth,4),new Color(.87f,.2f,.25f,.75f));UiText(30,416,280,42,"Low health  /  H to use a tonic",uiSmall,uiRed);}
    }

    bool V03UiAbility(Rect r,string name,string hint,float cooldown,float maxCooldown,Color accent,bool enabled){
        bool ready=cooldown<=0;bool click=UiButton(r,"",false,enabled&&ready);UiFill(new Rect(r.x+14,r.y+9,r.width-28,2),accent);
        UiText(r.x+3,r.y+19,r.width-6,23,ready?name:cooldown.ToString("0.0"),uiCenter,accent);
        UiText(r.x+3,r.y+48,r.width-6,23,ready?hint:"cooldown",uiCenter,uiMuted);
        if(!ready)UiFill(new Rect(r.x+9,r.yMax-7,(r.width-18)*(1-Mathf.Clamp01(cooldown/maxCooldown)),2),accent);
        return click;
    }

    void V03UiQuestTracker(){
        int id=trackedMission;
        if(id<0||id>2||missionStates[id]==3){id=-1;for(int i=0;i<3;i++)if(missionStates[i]==1||missionStates[i]==2){id=i;break;}}
        UiBox(new Rect(24,265,299,135),new Color(.025f,.08f,.115f,.86f));
        UiFill(new Rect(24,281,3,96),uiCyan);
        UiText(43,278,258,20,"PEOPLE OF AETHERFALL  /  "+CompletedMissions+" OF 3",uiTiny,uiCyan);
        UiText(43,307,258,29,id<0?"A world full of stories":missionNames[id],uiBody);
        UiText(43,340,258,44,id<0?(CompletedMissions==3?"All three stories complete. The frontier is yours to explore.":"Speak to Mira in Lantern Village. Q opens your quest journal."):MissionObjective(id),uiSmall,uiMuted);
    }

    bool UiProject(Vector3 world,out Vector2 point){
        point=Vector2.zero;if(!cam)return false;Vector3 p=cam.WorldToScreenPoint(world);if(p.z<=0)return false;
        point=UiScreenPoint(p);
        return point.x>15&&point.x<uiWidth-15&&point.y>75&&point.y<540;
    }
    void UiWorldIndicators(){
        foreach(var foe in foes){
            if(foe.dead||!foe.active||foe.boss||!foe.root)continue;
            float distance=FlatDistance(hero.position,foe.root.position);
            if(distance>32||(distance>18&&foe.hp>=foe.max))continue;
            Vector2 screen;if(!UiProject(foe.root.position+V(0,2.35f,0),out screen))continue;
            if(screen.x<335&&screen.y<415||screen.x>uiWidth-208&&screen.y<290)continue;
            string name=string.IsNullOrEmpty(foe.title)?"Rift foe":foe.title;
            UiText(screen.x-80,screen.y-23,160,21,name,uiCenter,uiIvory);
            UiBox(new Rect(screen.x-49,screen.y+1,98,9),uiNavy);UiBar(new Rect(screen.x-46,screen.y+3,92,5),foe.hp/Mathf.Max(1,foe.max),foe.wind>0?uiGold:uiRed);
        }
        for(int i=0;i<3;i++)if(questNpcs[i]&&FlatDistance(hero.position,questNpcs[i].position)<22){
            Vector2 screen;if(!UiProject(questNpcs[i].position+V(0,3.15f,0),out screen))continue;
            if(screen.x<335&&screen.y<415||screen.x>uiWidth-208&&screen.y<290)continue;
            UiBox(new Rect(screen.x-92,screen.y-19,184,48),new Color(.03f,.08f,.12f,.83f));
            UiText(screen.x-85,screen.y-16,170,23,npcNames[i]+(missionStates[i]==2?"  /  REWARD":""),uiCenter,missionStates[i]==2?uiCyan:uiGold);
            UiText(screen.x-85,screen.y+9,170,19,npcRoles[i],uiTiny,uiIvory);
        }
        foreach(var hit in floatingHits){
            Vector2 screen;if(!UiProject(hit.world,out screen))continue;
            Color color=hit.critical?uiGold:uiIvory;color.a=Mathf.Clamp01(hit.life*2);
            UiText(screen.x-55,screen.y-16,110,35,hit.text,hit.critical?uiHeading:uiCenter,color);
        }
        if(hitFlash>0){float a=Mathf.Clamp01(hitFlash*2);UiFill(new Rect(0,0,5,720),new Color(.9f,.25f,.3f,a));UiFill(new Rect(uiWidth-5,0,5,720),new Color(.9f,.25f,.3f,a));}
    }
    void UiCrosshair(){
        if(equippedWeapon==0&&!aiming)return;
        float x=uiWidth*.5f,y=360,gap=aiming?5:10;gap+=shotFlash*45;
        Color tint=reloadTime>0?uiMuted:aiming?uiCyan:uiIvory;
        UiLine(new Vector2(x-gap-6,y),new Vector2(x-gap,y),1.5f,tint);
        UiLine(new Vector2(x+gap,y),new Vector2(x+gap+6,y),1.5f,tint);
        UiLine(new Vector2(x,y-gap-6),new Vector2(x,y-gap),1.5f,tint);
        UiLine(new Vector2(x,y+gap),new Vector2(x,y+gap+6),1.5f,tint);
        UiCircle(x,y,1.5f,tint);
        if(reloadTime>0)UiText(x-70,y+29,140,23,"RECHARGING",uiCenter,uiGold);
    }

    void UiDialoguePanel(){
        int id=Mathf.Clamp(dialogueNpc,0,2);Rect f=UiFrame(880,398);float x=f.x+30,y=f.y+27;
        UiText(x,y,640,20,"AETHERFALL  /  "+npcRoles[id].ToUpper(),uiTiny,uiGold);UiClose(f,Phase.Dialogue);
        UiText(x,y+34,680,49,npcNames[id]+"  /  "+missionNames[id],uiHeading);
        UiLine(new Vector2(x,y+92),new Vector2(f.xMax-30,y+92),1,new Color(.23f,.34f,.38f));
        UiText(x,y+116,f.width-60,108,DialogueText(id),uiBody);
        UiText(x,y+235,f.width-60,31,"REWARD  /  "+missionCoins[id]+" earned coins   ·   "+missionXp[id]+" XP"+(id==1?"   ·   1 tonic":""),uiSmall,missionStates[id]==3?uiMuted:uiGold);
        int state=missionStates[id];string action=state==0?"I'll help. Accept quest":state==2?"Complete quest / receive reward":"Return to the realm";
        if(UiButton(new Rect(x,f.yMax-78,f.width-235,47),action,true)){
            if(state==0){AcceptMission(id);phase=Phase.Play;}
            else if(state==2)CompleteMission(id);
            else phase=Phase.Play;
        }
        if(UiButton(new Rect(f.xMax-193,f.yMax-78,163,47),state==0?"Maybe later":"Close"))phase=Phase.Play;
    }

    void UiJournalPanel(){
        Rect f=UiFrame(1020,648);float x=f.x+28,y=f.y+23;
        UiText(x,y,780,20,"STORIES OF THE SKYBOUND FRONTIER",uiTiny,uiGold);UiText(x,y+32,730,48,"Quest journal",uiBig);UiClose(f,Phase.Journal);
        UiText(x,y+92,f.width-56,25,CompletedMissions+" of 3 local stories complete. Follow a quest or discover the Moonwell trail.",uiSmall,uiMuted);
        string[] states={"AVAILABLE","IN PROGRESS","READY TO TURN IN","COMPLETED"};
        for(int i=0;i<3;i++){
            float cy=y+132+i*114;Color accent=missionStates[i]==2?uiCyan:missionStates[i]==3?uiMuted:uiGold;
            UiBox(new Rect(x,cy,f.width-56,104),uiSlate);UiFill(new Rect(x+1,cy+12,3,80),accent);
            UiText(x+18,cy+10,f.width-350,19,"0"+(i+1)+" / "+npcNames[i].ToUpper()+"   ·   "+states[missionStates[i]],uiTiny,accent);
            UiText(x+18,cy+33,f.width-350,30,missionNames[i],uiHeading);
            UiText(x+18,cy+67,f.width-360,32,MissionObjective(i),uiSmall,uiMuted);
            float right=f.xMax-277;
            UiText(right,cy+15,225,24,missionCoins[i]+" coins  /  "+missionXp[i]+" XP",uiSmall,accent);
            if(UiButton(new Rect(right,cy+51,225,38),missionStates[i]==3?"Reward received":trackedMission==i?"Tracking this quest":"Track quest",trackedMission==i,missionStates[i]!=3&&trackedMission!=i)){trackedMission=i;SaveJourney(false);}
        }
        UiCard(new Rect(x,y+485,f.width-56,91),moonwellQuest==3?uiMuted:uiCyan);
        UiText(x+18,y+497,f.width-92,22,"EXPLORATION  /  THE MOONWELL TRAIL",uiSmall,moonwellQuest==3?uiMuted:uiCyan);
        UiText(x+18,y+529,f.width-92,39,WorldV6Objective(),uiSmall,uiMuted);
        UiText(x,f.yMax-31,f.width-56,20,"Q / Esc  Return     M  Map markers show quest contacts and your current destination.",uiTiny);
    }

    void V03UiCharacterPanel(){
        Rect f=UiFrame(1020,630);float x=f.x+28,y=f.y+25;
        UiText(x,y,760,20,"TEJA  /  CHARACTER & WEAPON GROWTH",uiTiny,uiGold);UiText(x,y+32,700,47,"Make every journey count.",uiBig);UiClose(f,Phase.Character);
        UiText(x,y+91,850,27,"Level "+playerLevel+" / 20    ·    "+skillPoints+" skill points    ·    "+coins+" earned coins    ·    "+CompletedMissions+" quests complete",uiSmall,uiCyan);
        float left=330,right=x+354,rightWidth=f.width-410;
        UiBox(new Rect(x,y+137,left,368),uiSlate);
        UiText(x+20,y+153,left-40,29,"Wanderer's training",uiHeading);
        UiText(x+20,y+197,left-40,26,Mathf.RoundToInt(MaxHealth)+" max HP   /   "+DamageMultiplier.ToString("0.00")+"x equipped damage",uiSmall,uiGold);
        UiText(x+20,y+238,left-40,23,"VITALITY  /  "+vitalityRank+" OF 5",uiSmall);
        UiText(x+20,y+268,left-40,38,"Each point adds 5 maximum health.",uiSmall,uiMuted);
        if(UiButton(new Rect(x+20,y+309,left-40,39),vitalityRank>=5?"Vitality at maximum":"Improve vitality / 1 point",false,skillPoints>0&&vitalityRank<5))SpendSkillPoint(true);
        UiText(x+20,y+369,left-40,23,"FOCUS  /  "+powerRank+" OF 5",uiSmall);
        UiText(x+20,y+399,left-40,36,"Each point adds 5% base damage.",uiSmall,uiMuted);
        if(UiButton(new Rect(x+20,y+447,left-40,39),powerRank>=5?"Focus at maximum":"Improve focus / 1 point",false,skillPoints>0&&powerRank<5))SpendSkillPoint(false);
        for(int i=0;i<3;i++){
            float cy=y+137+i*126;bool owned=(ownedWeapons&(1<<i))!=0;int rank=weaponRanks[i],cost=WeaponUpgradeCost(i);
            UiBox(new Rect(right,cy,rightWidth,116),uiSlate);UiWeaponIcon(new Rect(right+16,cy+22,72,51),i,owned?uiCyan:uiMuted);
            UiText(right+105,cy+13,rightWidth-120,25,weapons[i].name+"  /  RANK "+rank+" OF 3",uiBody);
            UiText(right+105,cy+43,rightWidth-120,27,"+"+(rank*15)+"% base damage from ranks",uiSmall,uiMuted);
            string action=!owned?"Purchase at Lantern Armory":rank>=3?"Maximum rank reached":"Upgrade / "+cost+" coins";
            if(UiButton(new Rect(right+105,cy+76,rightWidth-121,31),action,owned&&rank<3,owned&&rank<3&&coins>=cost))UpgradeWeapon(i);
        }
        UiText(x,y+524,f.width-56,36,noticeTime>0?notice:"Earn XP from combat and quests. Each level grants one skill point and +4 maximum health.",uiSmall,noticeTime>0?uiGold:uiMuted);
        UiText(x,f.yMax-34,f.width-56,20,"C / Esc  Return     Weapon ranks use earned coins. All improvements save on this device.",uiTiny);
    }

    void UiDim(){float a=Mathf.SmoothStep(0,1,(Time.realtimeSinceStartup-uiEntered)/.18f);UiFill(new Rect(0,0,uiWidth,720),new Color(.015f,.035f,.065f,.82f*a));}
    Rect UiFrame(float width,float height){UiDim();width=Mathf.Min(width,uiWidth-48);float y=(720-height)*.5f+10*(1-Mathf.Clamp01((Time.realtimeSinceStartup-uiEntered)/.18f));var r=new Rect((uiWidth-width)*.5f,y,width,height);UiBox(r,uiNavy);UiFill(new Rect(r.x+25,r.y+1,r.width-50,2),uiGold);return r;}
    void UiClose(Rect frame,Phase panel){if(UiButton(new Rect(frame.xMax-70,frame.y+20,46,38),"X"))TogglePanel(panel);}

    void UiEquipmentPanel(bool shop){
        Rect frame=UiFrame(1010,586);float x=frame.x+28,y=frame.y+24;
        UiText(x,y,600,21,shop?"LANTERN VILLAGE  /  THE TRAVELLER'S ARMORY":"TEJA  /  JOURNEY EQUIPMENT",uiTiny,uiGold);
        UiText(x,y+32,660,44,shop?"A spark for every adventure.":"Ready for the road.",uiBig);
        UiText(x,y+88,frame.width-300,28,shop?"Spend the coins earned from foes, sigils and treasure.":"Choose a weapon, stock your potions and return to the realm.",uiSmall,uiMuted);
        UiClose(frame,shop?Phase.Shop:Phase.Inventory);
        UiBox(new Rect(frame.xMax-220,y+77,166,36),uiSlate);UiText(frame.xMax-204,y+84,138,23,coins+"  COINS",uiSmall,uiGold);
        float cardY=y+136,cardWidth=(frame.width-80)/3;
        for(int i=0;i<3;i++){
            float cx=x+i*(cardWidth+12);bool owned=(ownedWeapons&(1<<i))!=0,equipped=equippedWeapon==i;
            UiBox(new Rect(cx,cardY,cardWidth,298),equipped?new Color(.09f,.21f,.255f):uiSlate);
            UiFill(new Rect(cx+16,cardY+1,cardWidth-32,2),equipped?uiCyan:uiGold);
            UiText(cx+17,cardY+17,cardWidth-34,23,"0"+(i+1)+"  /  "+(i==0?"BLADE":i==1?"SIDEARM":"RIFLE"),uiTiny,equipped?uiCyan:uiGold);
            UiWeaponIcon(new Rect(cx+cardWidth*.5f-65,cardY+47,130,68),i,equipped?uiCyan:uiIvory);
            UiText(cx+17,cardY+131,cardWidth-34,28,UiWeaponName(i),uiBody);
            UiText(cx+17,cardY+170,cardWidth-34,45,weapons[i].description,uiSmall,uiMuted);
            float multiplier=1+.02f*(playerLevel-1)+.05f*powerRank+.15f*weaponRanks[i];
            string stat=Mathf.RoundToInt(weapons[i].damage*multiplier)+" damage  /  rank "+weaponRanks[i]+"  /  "+(i==0?"melee":weapons[i].magazine+" rounds");
            UiText(cx+17,cardY+220,cardWidth-34,24,stat,uiTiny,uiGold);
            string action=equipped?"Equipped":owned?"Equip weapon":shop?"Buy  /  "+weapons[i].price+" coins":"Buy at Lantern Village";
            bool can=!equipped&&(owned||(shop&&coins>=weapons[i].price));
            if(UiButton(new Rect(cx+15,cardY+252,cardWidth-30,33),action,!owned&&shop,can)){if(owned)EquipWeapon(i);else BuyWeapon(i);}
        }
        float foot=frame.yMax-115;
        UiLine(new Vector2(x,foot-14),new Vector2(frame.xMax-28,foot-14),1,new Color(.24f,.33f,.37f));
        UiCircle(x+19,foot+21,18,uiCyan,true);UiText(x+11,foot+9,20,30,"+",uiBody,uiCyan);
        UiText(x+52,foot+1,242,24,"Wayfarer's tonic  /  "+potions+" owned",uiBody);
        UiText(x+52,foot+32,280,26,"Restore health  /  H to use",uiSmall,uiMuted);
        if(shop){if(UiButton(new Rect(frame.xMax-244,foot,216,48),potions>=9?"Tonic pouch full":"Buy tonic  /  25 coins",true,coins>=25&&potions<9))BuyPotion();}
        else{if(UiButton(new Rect(frame.xMax-244,foot,216,48),"Drink a tonic",true,potions>0&&hp<MaxHealth))UsePotion();}
        UiText(x,frame.yMax-35,frame.width-56,24,shop?"Earned in the realm. No real-money purchases.":"1 / 2 / 3  Switch weapons     R  Reload     H  Drink tonic     I or Esc  Return",uiTiny);
        if(noticeTime>0)UiText(x+350,foot+10,frame.width-660,46,notice,uiSmall,uiGold);
    }

    void UiWeaponIcon(Rect r,int index,Color color){
        float x=r.x,y=r.y,w=r.width,h=r.height;
        if(index==0){
            UiLine(new Vector2(x+w*.24f,y+h*.87f),new Vector2(x+w*.83f,y+h*.13f),h*.095f,color);
            UiLine(new Vector2(x+w*.15f,y+h*.67f),new Vector2(x+w*.44f,y+h*.98f),h*.055f,uiGold);
            UiLine(new Vector2(x+w*.13f,y+h),new Vector2(x+w*.29f,y+h*.78f),h*.075f,uiGold);
            UiCircle(x+w*.85f,y+h*.10f,h*.038f,uiGold);
        }else if(index==1){
            UiFill(new Rect(x+w*.13f,y+h*.32f,w*.65f,h*.22f),color);UiFill(new Rect(x+w*.28f,y+h*.53f,w*.17f,h*.40f),color);
            UiFill(new Rect(x+w*.44f,y+h*.55f,w*.12f,h*.12f),uiGold);UiFill(new Rect(x+w*.70f,y+h*.38f,w*.19f,h*.095f),uiGold);
            UiFill(new Rect(x+w*.19f,y+h*.26f,w*.07f,h*.06f),uiGold);
        }else{
            UiFill(new Rect(x+w*.20f,y+h*.40f,w*.56f,h*.19f),color);UiFill(new Rect(x+w*.02f,y+h*.47f,w*.21f,h*.22f),color);
            UiFill(new Rect(x+w*.74f,y+h*.445f,w*.24f,h*.07f),uiGold);UiFill(new Rect(x+w*.36f,y+h*.57f,w*.075f,h*.30f),color);
            UiFill(new Rect(x+w*.47f,y+h*.60f,w*.13f,h*.23f),uiGold);UiFill(new Rect(x+w*.43f,y+h*.28f,w*.17f,h*.09f),uiGold);
            UiFill(new Rect(x+w*.49f,y+h*.35f,w*.04f,h*.06f),color);
        }
    }

    Vector2 UiMapPoint(Rect r,Vector3 pos){return new Vector2(r.center.x+pos.x/WorldRadius*r.width*.465f,r.center.y+pos.z/WorldRadius*r.height*.465f);}
    void UiMapDot(Rect r,Vector3 p,Color c,float radius,bool ring=false){var pt=UiMapPoint(r,p);UiCircle(pt.x,pt.y,radius,c,ring);}
    void UiMapArt(Rect r,bool large){
        UiCircle(r.center.x,r.center.y,r.width*.495f,new Color(.095f,.21f,.25f,.98f));
        UiCircle(r.center.x,r.center.y,r.width*.47f,new Color(.19f,.34f,.35f,.95f));
        UiCircle(r.center.x,r.center.y,r.width*.47f,new Color(.48f,.65f,.62f,.35f),true);
        UiCircle(r.center.x,r.center.y,r.width*.34f,new Color(.26f,.42f,.37f,.7f),true);
        Vector3 village=new Vector3(7,0,33),grove=new Vector3(-57,0,-18),marsh=new Vector3(57,0,8),ruins=new Vector3(-22,0,-63),gate=new Vector3(0,0,-71);
        Vector2 center=UiMapPoint(r,Vector3.zero);
        foreach(var destination in new[]{village,grove,marsh,ruins,gate,new Vector3(-113,0,-30),new Vector3(112,0,-12),new Vector3(0,0,111),new Vector3(0,0,-120)})UiLine(center,UiMapPoint(r,destination),large?3:1,new Color(.69f,.70f,.48f,.45f));
        foreach(var s in sigils)if(!s.taken)UiMapDot(r,s.root.position,uiGold,large?7:4,true);
        foreach(var e in foes)if(e.active&&!e.dead)UiMapDot(r,e.root.position,e.boss?uiRed:new Color(.9f,.55f,.48f),large?4:2);
        UiMapDot(r,merchantPosition,uiCyan,large?6:4);
        for(int i=0;i<waystoneLocations.Length;i++)UiMapDot(r,waystoneLocations[i],i==activeWaystone?uiGold:uiCyan,large?5:3,true);
        if(moonwellQuest>0&&moonwellQuest<3)UiMapDot(r,moonwellCenter,uiCyan,large?8:4,true);
        if(CoopActive&&coopRemote&&coop.Connected)UiMapDot(r,coopRemote.position,uiCyan,large?6:4);
        for(int i=0;i<3;i++)if(missionStates[i]!=3)UiMapDot(r,npcLocations[i],missionStates[i]==2?uiCyan:uiGold,large?7:4,true);
        if(trackedMission>=0&&trackedMission<3&&missionStates[trackedMission]!=3){
            Vector3 destination=MissionDestination(trackedMission);UiMapDot(r,destination,uiIvory,large?11:6,true);
            if(large){Vector2 q=UiMapPoint(r,destination);UiText(q.x-65,q.y+13,130,20,"QUEST DESTINATION",uiTiny,uiGold);}
        }
        if(boss!=null&&!boss.dead)UiMapDot(r,boss.spawn,uiRed,large?7:4,true);
        Vector2 player=UiMapPoint(r,hero.position);var old=GUI.matrix;float angle=Mathf.Atan2(hero.forward.x,-hero.forward.z)*Mathf.Rad2Deg;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(player.x,player.y,0),Quaternion.Euler(0,0,angle),Vector3.one);var previous=GUI.color;GUI.color=uiIvory;float arrow=large?22:14;GUI.DrawTexture(new Rect(-arrow*.5f,-arrow*.5f,arrow,arrow),uiArrow);GUI.color=previous;GUI.matrix=old;
        UiText(r.center.x-5,r.y+5,17,20,"N",uiTiny,uiGold);
        if(large){
            UiMapName(r,grove,"BLOSSOM GROVE",-67,19);UiMapName(r,marsh,"CRYSTAL MARSH",-56,22);
            UiMapName(r,village,"LANTERN VILLAGE",-64,19);UiMapName(r,ruins,"MOONSTONE RUINS",-101,-24);
            UiMapName(r,gate,"WARDEN'S GATE",9,-2);UiMapName(r,new Vector3(0,0,4),"AETHERFALL",-53,15);
            UiMapName(r,new Vector3(-113,0,27),"SUNBREAK",-43,-15);UiMapName(r,new Vector3(112,0,24),"SILVERRUN",-41,0);
            UiMapName(r,new Vector3(0,0,112),"WHISPERPINE FOREST",-69,0);UiMapName(r,new Vector3(0,0,-123),"STARFALL SANCTUM",-65,-12);
        }
    }
    void UiMapName(Rect r,Vector3 p,string label,float dx,float dy){Vector2 point=UiMapPoint(r,p);UiText(point.x+dx,point.y+dy,170,20,label,uiTiny,uiIvory);}
    void UiMiniMap(Rect r){UiBox(r,new Color(.025f,.065f,.10f,.9f));UiMapArt(new Rect(r.x+7,r.y+7,r.width-14,r.height-14),false);UiText(r.x+17,r.yMax+8,r.width-34,23,"M  /  WORLD MAP",uiTiny);}

    void UiWorldMap(){
        Rect f=UiFrame(980,630);UiText(f.x+28,f.y+23,690,44,"The skybound realm",uiBig);UiText(f.x+30,f.y+79,540,23,"AETHERFALL  /  EXPLORATION MAP",uiTiny,uiGold);UiClose(f,Phase.Map);
        float size=465;Rect map=new Rect(f.x+33,f.y+125,size,size);UiMapArt(map,true);
        float x=f.x+535,w=f.width-567;
        UiText(x,f.y+138,w,32,"Follow your curiosity.",uiHeading);
        UiText(x,f.y+189,w,67,"Meet the people of Aetherfall. Awaken waystones to set your return point, and follow the Moonwell trail west.",uiBody,uiMuted);
        UiLine(new Vector2(x,f.y+271),new Vector2(x+w,f.y+271),1,new Color(.23f,.34f,.38f));
        string[] labels={"You / Teja","Lost sigil","Armory / co-op companion","Rift foe / Warden","Quest contact / destination","Waystone / Moonwell trail"};Color[] colors={uiIvory,uiGold,uiCyan,uiRed,uiGold,uiCyan};
        for(int i=0;i<labels.Length;i++){UiCircle(x+7,f.y+299+i*31,6,colors[i],i==1||i>=4);UiText(x+28,f.y+286+i*31,w-28,29,labels[i],uiSmall);}
        UiText(x,f.y+496,w,23,"CURRENT REGION",uiTiny,uiGold);UiText(x,f.y+523,w,31,regionName??"Aetherfall Meadow",uiBody);
        UiText(x,f.y+568,w,38,"Explore on foot. M or Esc to return.",uiSmall,uiMuted);
    }

    void V03UiPausePanel(){
        bool paused=phase==Phase.Pause,won=phase==Phase.Win;
        Rect f=UiFrame(720,paused?578:474);float x=f.x+35,y=f.y+30;
        UiText(x,y,650,22,paused?"JOURNEY PAUSED":won?"CHAPTER COMPLETE":"A MOMENT TO BEGIN AGAIN",uiTiny,won?uiCyan:uiGold);
        UiText(x,y+42,650,65,paused?"The world can wait.":won?"A light worth protecting.":"Rise again, wanderer.",uiBig);
        string desc=paused?"Rest a moment. Your next adventure will be here.":won?"The Rift Warden has fallen. Aetherfall breathes again.":"Leave enemy warning circles before they erupt.\nVisit Lantern Village to prepare and keep a tonic ready.";
        UiText(x,y+119,640,65,desc,uiBody,uiMuted);
        if(paused){
            UiText(x,y+187,640,72,"WASD Move   Ctrl Sprint   Mouse Look / RMB Aim   Tab Lock-on   Space Jump\nLMB / J Attack   E Frost nova   Shift Dodge   F Interact   H Tonic\n1 / 2 / 3 Weapons   R Reload   I Loadout   M Map   Q Quests   C Character",uiSmall);
            if(UiButton(new Rect(x,y+270,650,48),"Return to the realm",true))phase=Phase.Play;
            if(UiButton(new Rect(x,y+329,318,43),"Save journey"))SaveJourney(true);
            if(UiButton(new Rect(x+332,y+329,318,43),"Return to title")){SaveJourney(false);phase=Phase.Title;}
            if(UiButton(new Rect(x,y+387,208,42),soundOn?"Audio  /  On":"Audio  /  Off"))soundOn=!soundOn;
            if(UiButton(new Rect(x+222,y+387,208,42),"Graphics / "+new[]{"Low","Balanced","High"}[Mathf.Clamp(graphicsPreset,0,2)]))ApplyGraphicsPreset((graphicsPreset+1)%3);
            if(UiButton(new Rect(x+444,y+387,206,42),"About & credits"))uiCredits=true;
            UiText(x,y+451,650,45,noticeTime>0?notice:"Progress is saved locally. Lower effects are recommended for integrated graphics.",uiSmall,noticeTime>0?uiGold:uiMuted);
        }else{
            UiBox(new Rect(x,y+189,650,57),uiSlate);UiText(x+18,y+205,610,27,kills+" foes defeated      /      "+coins+" coins      /      "+UiTime(elapsed)+" journey",uiBody,uiGold);
            if(UiButton(new Rect(x,y+275,650,48),won?"Begin another adventure":"Rise at your waystone",true)){if(won)StartGame(false);else RespawnAtCheckpoint();}
            if(UiButton(new Rect(x,y+337,650,42),"Return to title"))EndCoop(true);
        }
    }

    void UiCreditsPanel(){
        UiFill(new Rect(0,0,uiWidth,720),new Color(.015f,.03f,.06f,.9f));float width=Mathf.Min(760,uiWidth-60),x=(uiWidth-width)*.5f;
        UiBox(new Rect(x,84,width,552),uiNavy);UiFill(new Rect(x+30,85,width-60,2),uiGold);
        UiText(x+32,111,width-64,23,"TEJA'S IMPACT  /  v0.7",uiTiny,uiGold);UiText(x+32,151,width-64,53,"An adventure taking shape.",uiBig);
        UiText(x+32,224,width-64,59,"An adventure prototype for solo or two travellers: an original realm, quests, combat, an earned-coin shop and a journey saved on the host's device.",uiBody);
        UiText(x+32,306,width-64,77,"Character skin: the user-supplied Eula model from Genshin Impact. Eula and Genshin Impact belong to HoYoverse. This prototype is not affiliated with or endorsed by HoYoverse.",uiSmall,uiMuted);
        UiText(x+32,397,width-64,64,"Weapons: Kenney, CC0. Original procedural environment, animation, music and sound effects. Online rooms use Cloudflare Tunnel (Apache 2.0). Full audio and network credits are included.",uiSmall,uiMuted);
        UiText(x+32,475,width-64,47,"Eula model by Meta Headz. Embedded license: CC BY-NC 4.0. Noncommercial prototype. Full credits and modifications are in ATTRIBUTION.md.",uiSmall,uiGold);
        if(UiButton(new Rect(x+32,550,width-64,48),"Back to the adventure",true))uiCredits=false;
    }
}
