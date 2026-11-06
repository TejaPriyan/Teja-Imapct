using UnityEngine;

public partial class TejaAdventure {
    float lookSensitivity=1, cameraDistanceOffset;
    bool invertLook,aimToggle,leftGestureBlocked,cursorCaptured;
    int lookResumeFrame;
    float attackBuffer;
    float webCaptureRequestedAt;

    // This same input path is exercised by runtime checks and desktop polling.
    struct DesktopInput {
        public Vector2 movement,mouseDelta;
        public float scroll;
        public bool pointerOverUi,leftHeld,leftPressed,keyAttackHeld,keyAttackPressed,lookActive,aimHeld;
        public bool aimPressed,lockPressed,recenterPressed,sprintHeld,interactPressed;
        public bool burstPressed,dodgePressed,jumpPressed,potionPressed,reloadPressed;
        public int weaponSlot; // 0 means unchanged; 1..3 select an owned weapon.
    }
    void LoadControlSettings(){
        lookSensitivity=Mathf.Clamp(PlayerPrefs.GetFloat("TejaLookSensitivity",1),.35f,2.5f);
        invertLook=PlayerPrefs.GetInt("TejaInvertLook",0)==1;
        cameraDistanceOffset=Mathf.Clamp(PlayerPrefs.GetFloat("TejaCameraDistance",0),-2,3);
    }
    void SaveControlSettings(){
        lookSensitivity=Mathf.Clamp(lookSensitivity,.35f,2.5f);cameraDistanceOffset=Mathf.Clamp(cameraDistanceOffset,-2,3);
        if(smoke||inputCheck)return;
        PlayerPrefs.SetFloat("TejaLookSensitivity",lookSensitivity);PlayerPrefs.SetInt("TejaInvertLook",invertLook?1:0);
        PlayerPrefs.SetFloat("TejaCameraDistance",cameraDistanceOffset);PlayerPrefs.Save();
    }
    void ResetControlInput(){aimToggle=aiming=false;leftGestureBlocked=false;attackBuffer=0;ResetMobileControls();ResetUiNavigation();}
    // The camera sits behind +Z and looks toward -Z, so its screen-right is -X.
    Vector3 MovementDirection(Vector2 input){return Quaternion.Euler(0,yaw,0)*V(-input.x,0,input.y);}
    bool ShouldCaptureCursor(bool focused,bool freeCursor){return focused&&!freeCursor&&phase==Phase.Play&&!uiCredits&&!uiNewConfirm&&!uiCoopOpen&&!CoopWorldPaused;}
    void SyncCursorMode(){
        if(smoke||MobileControlsActive)return;
        bool wanted=ShouldCaptureCursor(Application.isFocused,Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt));
#if UNITY_WEBGL && !UNITY_EDITOR
        // Browsers require a new player gesture to acquire pointer lock. Escape
        // releases it outside Unity, so never silently recapture it every frame.
        if(wanted&&cursorCaptured&&Cursor.lockState!=CursorLockMode.Locked&&Time.realtimeSinceStartup-webCaptureRequestedAt>.5f){cursorCaptured=false;Pause();wanted=false;}
        if(wanted&&!cursorCaptured){
            wanted=Input.GetMouseButtonDown(0)&&!IsPointerOverGameUi(Input.mousePosition);
            if(wanted)webCaptureRequestedAt=Time.realtimeSinceStartup;
        }
#endif
        if(wanted!=cursorCaptured||(wanted&&Cursor.lockState!=CursorLockMode.Locked)){
            cursorCaptured=wanted;attackBuffer=0;leftGestureBlocked|=Input.GetMouseButton(0);
            // Ignore the cursor warp when entering or returning from a menu.
            lookResumeFrame=Time.frameCount+1;
        }
        Cursor.lockState=wanted?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!wanted;
    }
    void OnDisable(){ResetMobileControls();if(!smoke&&!MobileControlsActive){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;cursorCaptured=false;}}
    DesktopInput ReadDesktopInput(){
        // Unity mirrors the first touch as a mouse click for IMGUI menus. Never
        // feed that synthetic click into combat or camera input on a phone.
        if(MobileControlsActive)return new DesktopInput{pointerOverUi=true};
        var sample=new DesktopInput{
            movement=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)),
            mouseDelta=Time.frameCount>lookResumeFrame?new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y")):Vector2.zero,scroll=Input.mouseScrollDelta.y,
            pointerOverUi=!cursorCaptured,leftHeld=Input.GetMouseButton(0),leftPressed=Input.GetMouseButtonDown(0),
            keyAttackHeld=Input.GetKey(KeyCode.J),keyAttackPressed=Input.GetKeyDown(KeyCode.J),
            lookActive=cursorCaptured,aimHeld=cursorCaptured&&Input.GetMouseButton(1),
            aimPressed=Input.GetKeyDown(KeyCode.V),lockPressed=Input.GetKeyDown(KeyCode.Tab),recenterPressed=Input.GetKeyDown(KeyCode.T),
            sprintHeld=Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl),interactPressed=Input.GetKeyDown(KeyCode.F),
            burstPressed=Input.GetKeyDown(KeyCode.E),dodgePressed=Input.GetKeyDown(KeyCode.LeftShift),jumpPressed=Input.GetKeyDown(KeyCode.Space),
            potionPressed=Input.GetKeyDown(KeyCode.H),reloadPressed=Input.GetKeyDown(KeyCode.R)
        };
        for(int i=0;i<3;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))sample.weaponSlot=i+1;
        return sample;
    }
    Vector2 HandleDesktopInput(DesktopInput sample,float dt){
        if(!sample.leftHeld)leftGestureBlocked=false;
        if(phase!=Phase.Play||uiCredits||uiNewConfirm||uiCoopOpen||CoopWorldPaused){leftGestureBlocked=sample.leftHeld;attackBuffer=0;return Vector2.zero;}
        if(sample.leftHeld&&sample.pointerOverUi)leftGestureBlocked=true;
        if(sample.interactPressed){Interact();if(phase!=Phase.Play){attackBuffer=0;leftGestureBlocked=sample.leftHeld;return Vector2.zero;}}
        if(sample.weaponSlot>0)EquipWeapon(sample.weaponSlot-1);
        if(sample.aimPressed){aimToggle=!aimToggle;Notify(aimToggle?"Precision aim on":"Precision aim off");}
        if(!MobileControlsActive)aiming=equippedWeapon>0&&(aimToggle||sample.aimHeld);
        if(sample.lockPressed)ToggleLockOn();
        if(sample.recenterPressed){lockTarget=null;yaw=hero.eulerAngles.y-180;pitch=20;}
        if(!sample.pointerOverUi){
            if(sample.lookActive&&sample.mouseDelta.sqrMagnitude>.0001f){lockTarget=null;yaw+=sample.mouseDelta.x*3*lookSensitivity;pitch=Mathf.Clamp(pitch+sample.mouseDelta.y*(invertLook?2:-2)*lookSensitivity,8,55);}
            if(Mathf.Abs(sample.scroll)>.001f){cameraDistanceOffset=Mathf.Clamp(cameraDistanceOffset-sample.scroll*.45f,-2,3);}
        }
        sprinting=sample.sprintHeld||mobileSprint;
        if(sample.burstPressed)Burst();if(sample.dodgePressed)Dodge();if(sample.jumpPressed)Jump();
        if(sample.potionPressed)UsePotion();if(sample.reloadPressed)Reload();
        bool mouseAttack=sample.leftHeld&&!leftGestureBlocked&&!sample.pointerOverUi;
        if(sample.keyAttackPressed||(sample.leftPressed&&mouseAttack))attackBuffer=.16f;
        if(sample.pointerOverUi&&!sample.keyAttackHeld)attackBuffer=0;
        if(sample.keyAttackHeld||mouseAttack||attackBuffer>0){
            bool ready=strikeCD<=0&&reloadTime<=0;Strike();if(ready)attackBuffer=0;
        }
        attackBuffer=Mathf.Max(0,attackBuffer-dt);
        return Vector2.ClampMagnitude(sample.movement,1);
    }
}
