using UnityEngine;
using System.Collections;

public partial class TejaAdventure {
    void ValidateControls(){
        ValidateMovementDirections();
        StartGame();lookSensitivity=1;invertLook=false;cameraDistanceOffset=0;
        var target=foes[0];target.root.position=hero.position+hero.forward*2;target.hp=500;
        float startYaw=yaw,startPitch=pitch;strikeCD=0;
        var left=new DesktopInput{leftHeld=true,leftPressed=true};
        HandleDesktopInput(left,.016f);Require(target.hp<500&&combo==0,"LMB alone attacks with sword");
        Require(yaw==startYaw&&pitch==startPitch,"LMB does not rotate camera");
        float firstHit=target.hp;left.leftPressed=false;HandleDesktopInput(left,.016f);Require(target.hp==firstHit,"Held attack respects cooldown");
        for(int i=0;i<18;i++){Move(Vector2.zero,.05f);HandleDesktopInput(left,.05f);}
        Require(target.hp<firstHit&&combo==1,"Holding LMB repeats sword combo");
        ownedWeapons=7;EquipWeapon(1);strikeCD=0;target.root.position=hero.position+hero.forward*5;target.hp=500;cam.transform.position=hero.position+Vector3.up*1.35f;cam.transform.LookAt(target.root.position+Vector3.up*EnemyTargetHeight(target));
        HandleDesktopInput(left,.016f);Require(ammo[1]==7&&target.hp<500,"LMB alone fires sidearm");
        for(int i=0;i<10;i++){Move(Vector2.zero,.05f);HandleDesktopInput(left,.05f);}
        Require(ammo[1]==6,"Holding LMB repeats gunfire");
        ammo[1]=0;strikeCD=0;HandleDesktopInput(left,.016f);Require(reloadTime>0,"LMB empty magazine starts reload");
        HandleDesktopInput(left,.016f);Require(ammo[1]==0,"Reload blocks firing");UpdateWeaponTimers(2);strikeCD=0;
        HandleDesktopInput(left,.016f);Require(ammo[1]==7,"Held LMB resumes after reload");
        strikeCD=0;int magazine=ammo[1];float blockedHp=target.hp;
        left.pointerOverUi=true;HandleDesktopInput(left,.016f);Require(ammo[1]==magazine&&target.hp==blockedHp,"LMB over HUD never fires");
        left.pointerOverUi=false;HandleDesktopInput(left,.016f);Require(ammo[1]==magazine,"UI-started gesture stays blocked after drag away");
        HandleDesktopInput(new DesktopInput(),.016f);HandleDesktopInput(left,.016f);Require(ammo[1]==magazine-1,"Fresh world LMB resumes fire");
        foreach(var panel in new[]{Phase.Pause,Phase.Dialogue,Phase.Character,Phase.Map,Phase.Journal,Phase.Shop}){
            phase=panel;strikeCD=0;magazine=ammo[1];startYaw=yaw;
            HandleDesktopInput(new DesktopInput{leftHeld=true,lookActive=true,mouseDelta=Vector2.one,aimPressed=true,keyAttackHeld=true},.016f);
            Require(ammo[1]==magazine&&yaw==startYaw,"Menus suppress attack and look: "+panel);
            Require(!ShouldCaptureCursor(true,false),"Menus release cursor: "+panel);
        }
        phase=Phase.Play;HandleDesktopInput(left,.016f);Require(ammo[1]==magazine,"A click held from a menu cannot fire on resume");
        Require(ShouldCaptureCursor(true,false),"Gameplay captures mouse for automatic look");
        Require(!ShouldCaptureCursor(true,true)&&!ShouldCaptureCursor(false,false),"Alt and focus loss free the cursor");
        phase=Phase.Play;uiCredits=true;HandleDesktopInput(left,.016f);Require(ammo[1]==magazine,"Credits block fire");uiCredits=false;
        uiNewConfirm=true;HandleDesktopInput(left,.016f);Require(ammo[1]==magazine,"Confirmation blocks fire");uiNewConfirm=false;
        HandleDesktopInput(new DesktopInput(),.016f);startYaw=yaw;startPitch=pitch;
        HandleDesktopInput(new DesktopInput{lookActive=true,mouseDelta=Vector2.one},.016f);
        Require(yaw>startYaw&&pitch<startPitch&&ammo[1]==magazine,"Mouse movement without a button rotates camera without firing");
        startYaw=yaw;HandleDesktopInput(new DesktopInput{lookActive=true,mouseDelta=Vector2.one,pointerOverUi=true,scroll=100},.016f);
        Require(yaw==startYaw&&cameraDistanceOffset==0,"HUD blocks camera and scroll");
        HandleDesktopInput(new DesktopInput{scroll=100},.016f);Require(cameraDistanceOffset==-2,"Zoom close clamp");
        HandleDesktopInput(new DesktopInput{scroll=-100},.016f);Require(cameraDistanceOffset==3,"Zoom far clamp");
        HandleDesktopInput(new DesktopInput{aimHeld=true},.016f);Require(aiming&&ammo[1]==magazine,"Right mouse aims without firing");
        HandleDesktopInput(new DesktopInput(),.016f);Require(!aiming,"Releasing right mouse exits held aim");
        HandleDesktopInput(new DesktopInput{aimPressed=true},.016f);Require(aimToggle&&aiming,"V toggles precision aim");
        HandleDesktopInput(new DesktopInput{weaponSlot=1},.016f);Require(!aiming&&equippedWeapon==0,"Sword never uses gun aim camera");
        hero.rotation=Quaternion.Euler(0,180,0);HandleDesktopInput(new DesktopInput{recenterPressed=true},.016f);
        Require(Mathf.Abs(Mathf.DeltaAngle(yaw,0))<.01f&&pitch==20,"Camera recenter");
        EquipWeapon(1);aimToggle=aiming=false;strikeCD=0;magazine=ammo[1];
        HandleDesktopInput(new DesktopInput{keyAttackHeld=true,keyAttackPressed=true},.016f);Require(ammo[1]==magazine-1,"J remains a working attack alternative");
        var nearby=foes[1];nearby.active=true;nearby.dead=false;nearby.hp=500;nearby.root.position=hero.position+hero.forward*3;
        target.hp=500;target.root.position=hero.position+hero.forward*6;lockTarget=target;strikeCD=0;ammo[1]=8;Physics.SyncTransforms();Strike();
        Require(target.hp<500&&nearby.hp==500,"Valid locked target takes priority over nearby enemy");
        StartGame();Require(!aimToggle&&!aiming&&attackBuffer==0,"New journey resets transient input");
        phase=Phase.Pause;pausePage=PausePage.Controls;HandleEscape();Require(phase==Phase.Pause&&pausePage==PausePage.Journey,"Escape closes controls to pause");
        pausePage=PausePage.Settings;HandleEscape();Require(phase==Phase.Pause&&pausePage==PausePage.Journey,"Escape closes settings to pause");
        TogglePanel(Phase.Character);Require(phase==Phase.Character,"Pause can open character");HandleEscape();
        uiHitRects.Clear();uiHitRects.Add(new Rect(24,22,264,62));
        Require(IsPointerOverGameUi(new Vector2(40*UiScale,Screen.height-40*UiScale)),"HUD hit testing follows UI scale");
        Require(!IsPointerOverGameUi(new Vector2(450*UiScale,Screen.height-350*UiScale)),"World pointer remains available");uiHitRects.Clear();
        lookSensitivity=1;cameraDistanceOffset=0;invertLook=false;
    }
    void ValidateMovementDirections(){
        Vector3 cameraPosition=cam.transform.position;Quaternion cameraRotation=cam.transform.rotation;
        foreach(float heading in new[]{0f,90f,180f,270f}){
            foreach(float horizontal in new[]{-1f,1f}){
                StartGame();yaw=heading;
                Vector3 viewForward=Quaternion.AngleAxis(heading,Vector3.up)*Vector3.back;
                cam.transform.position=hero.position-viewForward*6+Vector3.up*3;cam.transform.LookAt(hero.position+Vector3.up);
                Vector3 before=cam.WorldToViewportPoint(hero.position+Vector3.up);
                Move(HandleDesktopInput(new DesktopInput{movement=new Vector2(horizontal,0)},.05f),.05f);
                Vector3 after=cam.WorldToViewportPoint(hero.position+Vector3.up);
                Require((after.x-before.x)*horizontal>.001f,(horizontal>0?"D moves screen right":"A moves screen left")+" at camera heading "+heading);
            }
            StartGame();yaw=heading;Vector3 start=hero.position;
            Move(HandleDesktopInput(new DesktopInput{movement=new Vector2(0,-1)},.05f),.05f);
            Vector3 forward=cam.transform.forward;forward.y=0;
            Require(Vector3.Dot(hero.position-start,forward)>.1f,"W moves toward the view at heading "+heading);
        }
        cam.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
    }
    IEnumerator PrepareInputCheck(){
        yield return null;StartGame();ownedWeapons=7;coins=320;EquipWeapon(1);strikeCD=0;invulnerable=99999;
        foreach(var enemy in foes){enemy.active=false;enemy.root.gameObject.SetActive(false);}
        Notify("Control check: left-click to fire. Move mouse to look. Esc opens the menu.");
        Debug.Log("TEJA_INPUT_CHECK_READY: isolated play; normal saves and settings are not written.");
    }
}
