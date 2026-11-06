using UnityEngine;

public partial class TejaAdventure {
    GUIStyle uiInput;
    float uiInviteCopiedUntil;

    void InitializeV6Ui(){
        if(uiInput!=null)return;
        uiInput=new GUIStyle(GUI.skin.textField){font=uiBody.font,fontSize=14,wordWrap=false,
            padding=new RectOffset(12,12,12,10),border=new RectOffset(12,12,12,12),
            normal={background=uiRound,textColor=uiIvory},hover={background=uiHover,textColor=uiIvory},
            focused={background=uiHover,textColor=Color.white},active={background=uiPressed,textColor=Color.white}};
    }

    void UiCoopPanel(){
        if(!TejaPlatformV7.InternetRoomsAvailable){UiPlatformInfo();return;}
        UiFill(new Rect(0,0,uiWidth,720),new Color(.01f,.025f,.04f,.82f));
        Rect f=UiFrame(840,600);float x=f.x+30,y=f.y+27,w=f.width-60;
        UiText(x,y,w-72,20,"ONLINE CO-OP  /  TWO TRAVELLERS",uiTiny,uiCyan);
        UiText(x,y+32,w-65,47,"Your world. A shared story.",uiBig);
        if(UiButton(new Rect(f.xMax-68,f.y+20,40,36),"X"))uiCoopOpen=false;
        bool hasRoom=coop!=null;
        if(!hasRoom){
            UiText(x,y+97,w,45,"Host a journey and send its invite code to a friend. Both players need the v0.7 Windows game and an internet connection.",uiSmall,uiMuted);
            if(UiButton(new Rect(x,y+160,w,46),"Host an online room",true)){HostCoop();return;}
            UiLine(new Vector2(x,y+229),new Vector2(x+w,y+229),1,new Color(.21f,.30f,.34f));
            UiText(x,y+249,w,22,"JOIN YOUR FRIEND",uiSmall,uiCyan);
            UiText(x,y+278,w,22,"Paste their complete TEJA6 invite code below.",uiSmall,uiMuted);
            GUI.SetNextControlName("TejaInviteCode");
            var inputBackground=GUI.backgroundColor;GUI.backgroundColor=uiSlate;
            coopCode=GUI.TextField(new Rect(x,y+313,w,46),coopCode??"",400,uiInput);GUI.backgroundColor=inputBackground;
            if(UiButton(new Rect(x,y+374,w,45),"Join their journey",false,!string.IsNullOrWhiteSpace(coopCode))){JoinCoop(coopCode.Trim());GUI.FocusControl(null);return;}
            if(!string.IsNullOrEmpty(coopMessage))UiText(x,y+433,w,38,coopMessage,uiSmall,uiGold);
        }else{
            bool host=!coopGuestSession,connected=coop.Connected;
            UiCard(new Rect(x,y+96,w,78),connected?uiCyan:uiGold);
            UiCircle(x+19,y+119,4,connected?uiCyan:uiGold);
            UiText(x+34,y+108,w-52,23,!string.IsNullOrEmpty(coop.Error)?"ROOM NEEDS ATTENTION":connected?(host?"YOUR FRIEND IS HERE":"CONNECTED TO YOUR HOST"):"CONNECTING YOUR JOURNEY",uiSmall,connected?uiCyan:uiGold);
            string status=!string.IsNullOrEmpty(coop.Error)?coop.Error:!string.IsNullOrEmpty(coopMessage)?coopMessage:coop.Status;
            UiText(x+16,y+140,w-32,31,status,uiSmall,uiMuted);
            if(host){
                UiText(x,y+198,w,22,"YOUR INVITE CODE",uiSmall,uiGold);
                string invitation=coop.InviteCode??"";
                // Text remains selectable for manual copying if the clipboard is unavailable.
                var inputBackground=GUI.backgroundColor;GUI.backgroundColor=uiSlate;GUI.TextField(new Rect(x,y+232,w,46),invitation,uiInput);GUI.backgroundColor=inputBackground;
                if(invitation.Length==0)UiText(x+12,y+245,w-24,21,"Preparing your room's secure invite…",uiSmall,uiMuted);
                if(UiButton(new Rect(x,y+292,w,42),Time.realtimeSinceStartup<uiInviteCopiedUntil?"Invite copied  /  Share it with your friend":"Copy invite code",true,invitation.Length>0)){
                    GUIUtility.systemCopyBuffer=invitation;uiInviteCopiedUntil=Time.realtimeSinceStartup+3;PlaySfx("ui");
                }
                UiText(x,y+353,w,49,"Share this code privately. It admits one friend to this room. A new room creates a new invite.",uiSmall,uiMuted);
            }else{
                UiText(x,y+204,w,29,"TOGETHER IN AETHERFALL",uiHeading,uiCyan);
                UiText(x,y+249,w,102,"Explore the same realm, defeat foes and finish the chapter together. Coins, unlocked weapons and quest rewards are shared. Your host saves this journey; your solo save stays on your own device.",uiBody,uiMuted);
            }
            float bw=(w-12)*.5f;
            if(UiButton(new Rect(x,y+416,bw,43),"Return to the realm",true,connected||host))uiCoopOpen=false;
            if(UiButton(new Rect(x+bw+12,y+416,bw,43),host?"Close this room":"Leave this room")){if(host)SaveJourney(false);EndCoop(!host);return;}
        }
        UiLine(new Vector2(x,y+486),new Vector2(x+w,y+486),1,new Color(.21f,.30f,.34f));
        UiText(x,y+502,w,48,"Free prototype rooms use a temporary Cloudflare connection. The host's game and PC must stay open. Opening a menu pauses the shared world.",uiTiny,uiMuted);
    }

    void UiPlatformInfo(){
        UiFill(new Rect(0,0,uiWidth,720),new Color(.01f,.025f,.04f,.82f));
        Rect f=UiFrame(760,500);float x=f.x+30,y=f.y+27,w=f.width-60;
        UiText(x,y,w-60,22,TejaPlatformV7.Edition.ToUpperInvariant()+" EDITION  /  v0.7",uiTiny,uiCyan);
        UiText(x,y+42,w,50,"Your adventure, on this device.",uiBig);
        UiText(x,y+118,w,76,"Explore Aetherfall, complete missions, earn upgrades and defeat the Rift Warden in solo play.",uiBody,uiIvory);
        UiText(x,y+211,w,80,"Two-player online rooms are available in the Windows download. Browser and mobile editions currently support solo play.",uiBody,uiMuted);
        UiText(x,y+314,w,55,TejaPlatformV7.Edition=="Browser"?"Progress stays in this browser. Allow site storage and use the same browser to continue. Clearing site data removes local progress.":"Progress is saved locally on this device. Uninstalling the game can remove your saved journey.",uiSmall,uiMuted);
        if(UiButton(new Rect(x,f.yMax-76,w,44),"Back to the adventure",true))uiCoopOpen=false;
    }

    void UiV6AudioSettings(float x,float y,float w){
        UiText(x,y,280,24,"Music volume",uiBody);
        SetMusicVolume(GUI.HorizontalSlider(new Rect(x+300,y+6,w-370,18),MusicVolume,0,1));
        UiText(x+w-62,y-2,62,24,Mathf.RoundToInt(MusicVolume*100)+"%",uiSmall,uiCyan);
        UiText(x,y+40,280,24,"Sound effects",uiBody);
        SetSfxVolume(GUI.HorizontalSlider(new Rect(x+300,y+46,w-370,18),SfxVolume,0,1));
        UiText(x+w-62,y+38,62,24,Mathf.RoundToInt(SfxVolume*100)+"%",uiSmall,uiCyan);
    }

    void UiCoopVictory(){
        Rect f=UiFrame(760,446);float x=f.x+34,y=f.y+31,w=f.width-68;
        UiText(x,y,w,23,"CHAPTER COMPLETE  /  A SHARED VICTORY",uiTiny,uiCyan);
        UiText(x,y+43,w,58,"Two travellers. One victory.",uiBig);
        UiText(x,y+119,w,64,"The Rift Warden has fallen. You protected Aetherfall together. The host keeps this shared journey's progress.",uiBody,uiMuted);
        UiCard(new Rect(x,y+208,w,62),uiGold);
        UiText(x+18,y+228,w-36,26,kills+" foes defeated    /    "+coins+" shared coins    /    "+CompletedMissions+" stories complete",uiSmall,uiGold);
        if(UiButton(new Rect(x,f.yMax-87,w,49),"Close room & return to title",true)){SaveJourney(false);EndCoop(true);}
    }
}
