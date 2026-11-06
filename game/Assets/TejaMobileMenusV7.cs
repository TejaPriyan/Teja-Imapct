using UnityEngine;

public partial class TejaAdventure {
    GUIStyle mobileMenuText,mobileMenuHeading;
    void InitMobileMenuStyles(){
        if(mobileMenuText!=null)return;
        mobileMenuText=new GUIStyle(uiBody){fontSize=26,wordWrap=true};
        mobileMenuHeading=new GUIStyle(uiHeading){fontSize=38};
    }
    bool MobileMenuButton(Rect r,string text,bool primary=false,bool enabled=true){
        int old=uiButton.fontSize,oldPrimary=uiPrimary.fontSize;uiButton.fontSize=26;uiPrimary.fontSize=26;
        bool clicked=UiButton(r,text,primary,enabled);uiButton.fontSize=old;uiPrimary.fontSize=oldPrimary;return clicked;
    }
    void UiMobilePausePanel(){
        InitMobileMenuStyles();Rect f=UiFrame(Mathf.Min(uiWidth-48,1300),672);float x=f.x+28,y=f.y+20,w=f.width-56;
        UiText(x,y,w-210,58,pausePage==PausePage.Journey?"Journey paused":pausePage==PausePage.Settings?"Settings":"Touch controls",mobileMenuHeading);
        if(MobileMenuButton(new Rect(f.xMax-206,y,178,78),pausePage==PausePage.Journey?"Resume":"Back",true)){if(pausePage==PausePage.Journey)phase=Phase.Play;else pausePage=PausePage.Journey;}
        if(pausePage==PausePage.Journey){
            UiText(x,y+67,w-215,44,regionName+"  /  Level "+playerLevel+"  /  "+coins+" coins",mobileMenuText,uiMuted);
            float bw=(w-18)*.5f;
            string[] names={"Quest journal","Character & upgrades","Weapons & loadout","World map","Settings","Touch controls","Save journey","Save & return to title"};
            for(int i=0;i<names.Length;i++)if(MobileMenuButton(new Rect(x+(i%2)*(bw+18),y+135+(i/2)*102,bw,88),names[i])){
                if(i<4)phase=i==0?Phase.Journal:i==1?Phase.Character:i==2?Phase.Inventory:Phase.Map;
                else if(i==4||i==5)pausePage=i==4?PausePage.Settings:PausePage.Controls;
                else if(i==6)SaveJourney(true);else{SaveJourney(false);EndCoop(true);}
            }
            UiText(x,y+555,w,65,noticeTime>0?notice:"Your journey is saved on this device. Browser and Android play are solo.",mobileMenuText,noticeTime>0?uiGold:uiMuted);
        }else if(pausePage==PausePage.Settings){
            float controlX=x+w*.48f,controlW=w*.52f;
            string[] labels={"Graphics quality","Game audio","Music volume","Effects volume","Look sensitivity","Invert vertical look","Camera distance"};
            for(int i=0;i<labels.Length;i++){
                float row=y+105+i*74;UiText(x,row+10,w*.46f,54,labels[i],mobileMenuText);
                if(i==0){if(MobileMenuButton(new Rect(controlX,row,controlW,66),new[]{"Low","Balanced","High"}[Mathf.Clamp(graphicsPreset,0,2)]))ApplyGraphicsPreset((graphicsPreset+1)%3);}
                else if(i==1){if(MobileMenuButton(new Rect(controlX,row,controlW,66),soundOn?"On":"Off"))SetAudioEnabled(!soundOn);}
                else if(i==5){if(MobileMenuButton(new Rect(controlX,row,controlW,66),invertLook?"On":"Off")){invertLook=!invertLook;SaveControlSettings();}}
                else{
                    float value=i==2?MusicVolume:i==3?SfxVolume:i==4?lookSensitivity:cameraDistanceOffset;
                    float step=i==2||i==3?.1f:i==4?.15f:.5f;
                    string display=i==2||i==3?Mathf.RoundToInt(value*100)+"%":value.ToString("0.0")+(i==4?"×":"");
                    bool minus=MobileMenuButton(new Rect(controlX,row,100,66),"−"),plus=MobileMenuButton(new Rect(controlX+controlW-100,row,100,66),"+");
                    UiText(controlX+110,row+14,controlW-220,48,display,mobileMenuText,uiCyan);
                    if(minus||plus){float next=value+(plus?step:-step);if(i==2)SetMusicVolume(next);else if(i==3)SetSfxVolume(next);else if(i==4){lookSensitivity=Mathf.Clamp(next,.35f,2.5f);SaveControlSettings();}else{cameraDistanceOffset=Mathf.Clamp(next,-2,3);SaveControlSettings();}}
                }
            }
        }else{
            string[] names={"Move / sprint","Look / recenter","Attack / shoot","Jump / dodge / frost","Aim / reload / lock","Switch weapons / interact","Heal / menus"};
            string[] values={"Left stick / Sprint toggle","Drag empty right side / Recenter","Hold Strike or Fire","Jump, Dodge and Frost buttons","Aim, Reload and Lock buttons","Weapon / nearby Interact prompt","Tonic / Menu, cards and Map"};
            for(int i=0;i<names.Length;i++){float row=y+110+i*72;if(i%2==0)UiFill(new Rect(x,row,w,68),uiSlate);UiText(x+12,row+9,w*.4f-24,54,names[i],mobileMenuText,uiMuted);UiText(x+w*.4f,row+9,w*.6f-12,54,values[i],mobileMenuText);}
        }
    }
}
