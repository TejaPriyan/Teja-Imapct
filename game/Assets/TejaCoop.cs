using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

// The host owns combat, enemies and shared rewards. Guests predict only their movement.
public partial class TejaAdventure {
    TejaInternetTransport coop;
    bool coopGuestSession; string coopPeer="";
    bool uiCoopOpen,coopExecuting,coopWasConnected,coopRemotePaused,coopGuestInitialized;
    string coopCode="",coopMessage="",coopLastProgress="",coopLastWorld="",coopLastCoins="";
    float coopClock,coopRemoteTimer,coopActionLimit;
    int coopNextCommand,coopAck,coopPanelSerial,coopSeenPanel;
    Ray coopActionRay;
    Transform coopRemote,coopRemoteModel;TejaRig coopRemoteRig;CharacterController coopRemoteMotor;
    ActorState coopRemoteState;
    Vector3 coopRemoteGoal;Quaternion coopRemoteRotation;
    readonly List<CoopCommand> coopPending=new List<CoopCommand>();
    bool CoopIsGuest {get{return coopGuestSession;}}
    bool CoopIsHost {get{return coop!=null&&coop.IsHost;}}
    bool CoopActive {get{return CoopIsGuest||CoopIsHost;}}
    bool CoopFriendReady {get{return CoopIsHost&&coop!=null&&coop.Connected&&coopRemote!=null;}}
    bool CoopWorldPaused {get{return uiCoopOpen||coopGuestSession&&(coop==null||!coop.Connected)||CoopActive&&coopRemotePaused;}}
    [Serializable] class CoopCommand {public int id,arg;public string kind;public Vector3 origin,direction;}
    [Serializable] class CoopPlayer {public Vector3 position;public float angle,hp;public int weapon,animationSerial;public string animation;public bool paused;}
    [Serializable] class CoopInput {public int version=6;public CoopPlayer player;public CoopCommand[] commands;}
    [Serializable] class CoopEnemy {public Vector3 position;public float angle,hp,wind,windStart;public bool dead,active;public int style;public Vector3 target;}
    [Serializable] class CoopSnapshot {
        public int version=6,ack,panelSerial,panel,npc;public bool paused,won;
        public CoopPlayer host,guest;public JourneyV2 journey;public CoopEnemy[] enemies;
        public int[] guestAmmo;public float guestReload;public string message;
        public Vector3[] bolts;public float[] rings;
    }
    class ActorState {
        public Transform hero,model;public TejaRig rig;public CharacterController motor;
        public int weapon,combo;public int[] ammo;public float hp,strike,swing,burst,dodge,invuln,reload,dash,comboWindow,yaw,pitch;
        public bool aiming;public Foe target;
    }
    ActorState CaptureActor(){return new ActorState{hero=hero,model=equippedModel,rig=rig,motor=motor,weapon=equippedWeapon,combo=combo,ammo=ammo,hp=hp,strike=strikeCD,swing=swing,burst=burstCD,dodge=dodgeCD,invuln=invulnerable,reload=reloadTime,dash=dashTime,comboWindow=comboWindow,yaw=yaw,pitch=pitch,aiming=aiming,target=lockTarget};}
    void RestoreActor(ActorState a){hero=a.hero;equippedModel=a.model;rig=a.rig;motor=a.motor;equippedWeapon=a.weapon;combo=a.combo;ammo=a.ammo;hp=a.hp;strikeCD=a.strike;swing=a.swing;burstCD=a.burst;dodgeCD=a.dodge;invulnerable=a.invuln;reloadTime=a.reload;dashTime=a.dash;comboWindow=a.comboWindow;yaw=a.yaw;pitch=a.pitch;aiming=a.aiming;lockTarget=a.target;}
    void CreateCoopAvatar(Vector3 position){
        if(coopRemote)return;
        coopRemote=G("Teja / travelling companion",null,position);
        coopRemoteMotor=coopRemote.gameObject.AddComponent<CharacterController>();coopRemoteMotor.height=1.92f;coopRemoteMotor.center=V(0,.98f,0);coopRemoteMotor.radius=.28f;coopRemoteMotor.stepOffset=.32f;
        var model=Instantiate(Resources.Load<GameObject>("Eula/Eula"),coopRemote,false);model.transform.localScale=Vector3.one*.92f;coopRemoteModel=model.transform;coopRemoteRig=model.GetComponent<TejaRig>();
        coopRemoteState=new ActorState{hero=coopRemote,rig=coopRemoteRig,motor=coopRemoteMotor,hp=MaxHealth,ammo=new[]{0,8,18},weapon=-1,pitch=20};
        var prior=CaptureActor();bool wasExecuting=coopExecuting;coopExecuting=true;RestoreActor(coopRemoteState);EquipWeapon(0);coopRemoteState=CaptureActor();RestoreActor(prior);coopExecuting=wasExecuting;
        coopRemoteGoal=position;coopRemoteRotation=Quaternion.Euler(0,180,0);
        var halo=Cylinder("#8fe7e3",coopRemote,V(0,.05f,0),V(.8f,.015f,.8f));halo.GetComponent<Renderer>().sharedMaterial=AetherFx();
        if(CoopIsGuest)coopRemoteMotor.enabled=false;
    }
    void HostCoop(bool localTest=false){
        if(!TejaPlatformV7.InternetRoomsAvailable){coopMessage="Online rooms are available in the Windows edition. This edition supports solo adventures.";uiCoopOpen=true;return;}
        bool fromTitle=phase==Phase.Title;EndCoop(false);if(fromTitle)StartGame(!smoke&&HasJourneySave());else phase=Phase.Play;
        if(fromTitle&&phase!=Phase.Play){coopMessage=notice;uiCoopOpen=true;return;}
        coop=new TejaInternetTransport();Application.runInBackground=true;
        if(localTest)coop.StartHostLocalForTests();else coop.StartHost(Path.Combine(Application.streamingAssetsPath,"Networking/cloudflared.exe"));
        uiCoopOpen=true;coopMessage="Opening your room…";coopClock=0;coopAck=coopPanelSerial=0;coopGuestInitialized=false;
    }
    void JoinCoop(string code,bool localTest=false){
        if(!TejaPlatformV7.InternetRoomsAvailable){coopMessage="Online rooms are available in the Windows edition. This edition supports solo adventures.";uiCoopOpen=true;return;}
        EndCoop(false);coopGuestSession=true;StartGame();coop=new TejaInternetTransport();Application.runInBackground=true;
        if(localTest)coop.JoinLocalForTests(code);else coop.Join(code);
        uiCoopOpen=true;coopMessage="Connecting to your friend…";coopCode=code;coopNextCommand=coopAck=coopSeenPanel=0;coopPending.Clear();coopGuestInitialized=false;
    }
    void EndCoop(bool toTitle){
        bool guest=coopGuestSession;if(coop!=null){coop.Dispose();coop=null;}
        if(coopRemote)Destroy(coopRemote.gameObject);coopRemote=null;coopRemoteState=null;
        foreach(var v in coopBoltVisuals)if(v)Destroy(v.gameObject);foreach(var v in coopRingVisuals)if(v)Destroy(v.gameObject);coopBoltVisuals.Clear();coopRingVisuals.Clear();coopGuestSession=false;coopPeer="";
        coopPending.Clear();coopWasConnected=coopRemotePaused=coopGuestInitialized=false;coopAck=coopNextCommand=coopSeenPanel=0;coopLastProgress=coopLastWorld=coopLastCoins="";
        uiCoopOpen=false;Application.runInBackground=smoke;
        if(toTitle||guest)phase=Phase.Title;
    }
    void QueueCoopCommand(string kind,int arg=0){
        if(!CoopIsGuest||coopPending.Count>=24)return;
        var ray=cam.ViewportPointToRay(new Vector3(.5f,.5f,0));
        coopPending.Add(new CoopCommand{id=++coopNextCommand,kind=kind,arg=arg,origin=ray.origin,direction=ray.direction});
    }
    bool GuestAttack(){
        if(!CoopIsGuest)return false;
        if(phase!=Phase.Play||CoopWorldPaused||strikeCD>0||reloadTime>0)return true;
        QueueCoopCommand("attack",equippedWeapon);strikeCD=weapons[equippedWeapon].cooldown;
        if(equippedWeapon==0){combo=comboWindow>0?(combo+1)%3:0;comboWindow=1.1f;string clip=combo==0?"Sword":combo==1?"Sword2":"Sword3";swing=TejaRig.Duration(clip);strikeCD=swing*.85f;rig.Trigger(clip);SlashEffect();PlaySfx("sword"+(combo+1));}
        else if(ammo[equippedWeapon]>0){rig.Trigger("Shoot");swing=.28f;shotFlash=.1f;PlaySfx(equippedWeapon==1?"sidearm":"rifle");
            Ray ray=CombatAimRay();float distance;FindShotTarget(ray,weapons[equippedWeapon].range,out distance);Vector3 end=ray.GetPoint(distance);if(ShotSceneHit(ray,distance,out var hit))end=hit.point;
            DrawShot(hero.position+Vector3.up*1.4f+hero.forward*.4f,end);
        }
        return true;
    }
    CoopPlayer LocalCoopPlayer(){return new CoopPlayer{position=hero.position,angle=hero.eulerAngles.y,hp=hp,weapon=equippedWeapon,animation=rig.CurrentState,animationSerial=rig.TriggerSerial,paused=phase!=Phase.Play||uiCoopOpen||uiCredits||uiNewConfirm};}
    bool ValidPosition(Vector3 p){return Finite(p.x)&&Finite(p.y)&&Finite(p.z)&&new Vector2(p.x,p.z).magnitude<=WorldRadius+1&&p.y>=-4&&p.y<40;}
    void TickCoop(float dt){
        if(coop==null)return;
        if(coop.Error.Length>0){coopMessage=coop.Error;uiCoopOpen=true;return;}
        if(CoopIsHost&&coop.Connected&&coop.PeerSessionId!=coopPeer){coopPeer=coop.PeerSessionId;coopAck=coopPanelSerial=0;coopWasConnected=coopGuestInitialized=false;if(coopRemote)Destroy(coopRemote.gameObject);coopRemote=null;coopRemoteState=null;}
        if(coop.Connected&&!coopWasConnected){coopWasConnected=true;coopMessage=CoopIsHost?"Your friend joined. Explore and finish the story together.":"Joined the host's journey.";uiCoopOpen=false;phase=Phase.Play;PlaySfx("ui");}
        if(coopWasConnected&&!coop.Connected){coopRemotePaused=true;coopMessage="Connection interrupted. Waiting for your friend…";if(coop.LastReceiveAge>20){uiCoopOpen=true;coopMessage="Connection lost. Close this session and host or join again.";}}
        if(coopRemote){
            if(CoopIsGuest)coopRemote.position=Vector3.Lerp(coopRemote.position,coopRemoteGoal,1-Mathf.Exp(-dt*14));
            coopRemote.rotation=Quaternion.Slerp(coopRemote.rotation,coopRemoteRotation,1-Mathf.Exp(-dt*14));
            if(coopRemoteModel)coopRemoteModel.localPosition=Vector3.Lerp(coopRemoteModel.localPosition,Vector3.zero,1-Mathf.Exp(-dt*16));
        }
        if(CoopIsHost&&coopRemoteState!=null){
            var a=coopRemoteState;a.strike=Mathf.Max(0,a.strike-dt);a.swing=Mathf.Max(0,a.swing-dt);a.burst=Mathf.Max(0,a.burst-dt);a.dodge=Mathf.Max(0,a.dodge-dt);a.invuln=Mathf.Max(0,a.invuln-dt);a.dash=Mathf.Max(0,a.dash-dt);a.comboWindow=Mathf.Max(0,a.comboWindow-dt);
            if(a.reload>0){a.reload=Mathf.Max(0,a.reload-dt);if(a.reload==0)a.ammo[a.weapon]=weapons[a.weapon].magazine;}
        }
        coopRemoteTimer+=dt;coopClock-=dt;if(coopClock>0)return;coopClock=.10f;
        try{
            string packet;
            if(CoopIsHost){while(coop.TryGetGuestInput(out packet))ApplyCoopInput(JsonUtility.FromJson<CoopInput>(packet));coop.SetHostSnapshot(JsonUtility.ToJson(CreateCoopSnapshot()));}
            else if(CoopIsGuest){while(coop.TryGetHostSnapshot(out packet)){var snapshot=JsonUtility.FromJson<CoopSnapshot>(packet);if(snapshot!=null&&snapshot.host!=null)ApplyCoopSnapshot(snapshot);}coop.SetGuestInput(JsonUtility.ToJson(new CoopInput{player=LocalCoopPlayer(),commands=coopPending.ToArray()}));}
        }catch(Exception ex){Debug.LogWarning("Co-op packet rejected: "+ex.GetType().Name);coopMessage="The session data was invalid. Close this session and try again.";uiCoopOpen=true;}
    }
    void ApplyCoopInput(CoopInput input){
        if(input==null||input.version!=6||input.player==null||!ValidPosition(input.player.position)||!Finite(input.player.angle))return;
        bool first=!coopRemote;CreateCoopAvatar(hero.position+hero.right*1.5f);var p=input.player;
        coopRemotePaused=p.paused;
        if(!first&&coopGuestInitialized){
            bool startingDodge=coopRemoteState.dodge<=0&&input.commands!=null&&Array.Exists(input.commands,c=>c!=null&&c.kind=="dodge"&&c.id>coopAck&&c.id<=coopAck+24);
            Vector3 delta=p.position-coopRemote.position;float limit=(coopRemoteState.dash>0||startingDodge?22:10)*Mathf.Min(coopRemoteTimer,.5f)+.55f;
            if(delta.magnitude<=limit&&p.position.y<=GroundHeight(p.position)+5){Vector3 before=coopRemote.position;coopRemoteMotor.Move(delta);coopRemoteModel.position-=coopRemote.position-before;}
        }
        coopGuestInitialized=true;coopRemoteTimer=0;coopRemoteRotation=Quaternion.Euler(0,p.angle,0);
        if(p.weapon>=0&&p.weapon<3&&(ownedWeapons&(1<<p.weapon))!=0&&p.weapon!=coopRemoteState.weapon)WithRemoteActor(()=>EquipWeapon(p.weapon));
        if(coopRemoteState.swing<=0)coopRemoteRig.Play(SafeAnimation(p.animation));
        if(input.commands!=null){int n=0;foreach(var command in input.commands){if(command==null||command.id<=coopAck)continue;if(++n>24||command.id!=coopAck+1)break;ExecuteCoopCommand(command,p.paused);coopAck=command.id;}}
    }
    string SafeAnimation(string value){switch(value){case "Run":case "GunRun":case "Sprint":case "Jump":case "Dodge":case "GunIdle":return value;default:return "Idle";}}
    void WithRemoteActor(Action action){
        if(coopRemoteState==null)return;var prior=CaptureActor();Phase oldPhase=phase;int oldNpc=dialogueNpc;bool priorExecuting=coopExecuting;
        try{RestoreActor(coopRemoteState);phase=Phase.Play;coopExecuting=true;action();coopRemoteState=CaptureActor();if(phase==Phase.Dialogue||phase==Phase.Shop){coopPanelSerial++;coopPanel=(int)phase;coopNpc=dialogueNpc;}}
        finally{RestoreActor(prior);phase=boss.dead?Phase.Win:oldPhase;dialogueNpc=oldNpc;coopExecuting=priorExecuting;}
    }
    int coopPanel,coopNpc;
    void ExecuteCoopCommand(CoopCommand c,bool guestPaused){
        if(c.kind==null||c.kind.Length>24)return;
        bool fight=c.kind=="attack"||c.kind=="burst"||c.kind=="dodge";
        if(fight&&(guestPaused||phase!=Phase.Play||coopRemoteState.hp<=0))return;
        if(!Finite(c.origin.x)||!Finite(c.origin.y)||!Finite(c.origin.z)||!Finite(c.direction.x)||!Finite(c.direction.y)||!Finite(c.direction.z))return;
        if(c.kind=="attack"&&(Vector3.Distance(c.origin,coopRemote.position)>12||c.direction.sqrMagnitude<.8f||c.direction.sqrMagnitude>1.2f))return;
        coopActionRay=new Ray(c.origin,c.direction.normalized);int beforeKills=kills;
        WithRemoteActor(()=>{
            switch(c.kind){
                case "attack":aiming=true;Strike();break;
                case "burst":Burst();break;
                case "dodge":Dodge();break;
                case "reload":Reload();break;
                case "heal":UsePotion();break;
                case "interact":Interact();break;
                case "buy":if(FlatDistance(hero.position,merchantPosition)<6)BuyWeapon(c.arg);break;
                case "flask":if(FlatDistance(hero.position,merchantPosition)<6)BuyPotion();break;
                case "accept":if(NearbyNpc()==c.arg)AcceptMission(c.arg);break;
                case "complete":if(NearbyNpc()==c.arg)CompleteMission(c.arg);break;
                case "upgrade":UpgradeWeapon(c.arg);break;
                case "skill":SpendSkillPoint(c.arg==1);break;
                case "respawn":if(hp<=0)RespawnAtCheckpoint();break;
            }
        });
        if(!fight||kills!=beforeKills)SaveJourney(false);
    }
    CoopSnapshot CreateCoopSnapshot(){
        var state=new CoopSnapshot{ack=coopAck,panelSerial=coopPanelSerial,panel=coopPanel,npc=coopNpc,paused=phase!=Phase.Play||uiCoopOpen,won=boss.dead,host=LocalCoopPlayer(),journey=CurrentJourney(),message=noticeTime>0?notice:"",enemies=new CoopEnemy[foes.Count]};
        for(int i=0;i<foes.Count;i++){var e=foes[i];state.enemies[i]=new CoopEnemy{position=e.root.position,angle=e.root.eulerAngles.y,hp=e.hp,dead=e.dead,active=e.active,wind=e.wind,windStart=e.windStart,target=e.target,style=e.attackStyle};}
        if(coopRemoteState!=null){var a=coopRemoteState;state.guest=new CoopPlayer{position=coopRemote.position,angle=coopRemote.eulerAngles.y,hp=a.hp,weapon=a.weapon,animation=coopRemoteRig.CurrentState};state.guestAmmo=(int[])a.ammo.Clone();state.guestReload=a.reload;}
        state.bolts=new Vector3[enemyBolts.Count];for(int i=0;i<enemyBolts.Count;i++)state.bolts[i]=enemyBolts[i].root.position;
        state.rings=new float[shockwaves.Count*4];for(int i=0;i<shockwaves.Count;i++){var w=shockwaves[i];state.rings[i*4]=w.origin.x;state.rings[i*4+1]=w.origin.y;state.rings[i*4+2]=w.origin.z;state.rings[i*4+3]=w.radius;}
        return state;
    }
    void ApplyCoopSnapshot(CoopSnapshot s){
        if(s.version!=6||s.journey==null||s.enemies==null||s.enemies.Length!=foes.Count||!ValidPosition(s.host.position))return;
        coopRemotePaused=s.paused;CreateCoopAvatar(s.host.position);coopRemoteGoal=s.host.position;coopRemoteRotation=Quaternion.Euler(0,s.host.angle,0);coopRemoteState.hp=s.host.hp;
        if(coopRemoteRig.ApplyNetworkState(s.host.animation,s.host.animationSerial))PresentRemoteAction(s.host);
        if(s.host.weapon!=coopRemoteState.weapon&&s.host.weapon>=0&&s.host.weapon<3){int own=ownedWeapons;ownedWeapons=7;WithRemoteActor(()=>EquipWeapon(s.host.weapon));ownedWeapons=own;}
        var d=s.journey;coins=Mathf.Clamp(d.coins,0,999999);ownedWeapons=(d.owned&7)|1;potions=Mathf.Clamp(d.potions,0,9);chestMask=d.chests;kills=d.kills;elapsed=d.time;
        string progress=JsonUtility.ToJson(d.progression);if(progress!=coopLastProgress){int npc=dialogueNpc;ApplyProgressionSave(d.progression);dialogueNpc=npc;coopLastProgress=progress;}
        string world=JsonUtility.ToJson(d.worldV6);if(world!=coopLastWorld){ApplyWorldV6Save(d.worldV6);coopLastWorld=world;}
        count=0;for(int i=0;i<sigils.Count;i++){var sigil=sigils[i];sigil.taken=(d.mask&(1<<i))!=0;sigil.root.gameObject.SetActive(!sigil.taken);if(sigil.taken)count++;}
        if(d.coinState!=coopLastCoins){ApplyCoinState(d.coinState);coopLastCoins=d.coinState;}
        for(int i=0;i<foes.Count;i++){
            var e=foes[i];var n=s.enemies[i];if(n==null||!ValidPosition(n.position)||!Finite(n.hp))continue;
            if(n.hp<e.hp&&!e.dead){HitPresentation(e);Sparks(n.position+Vector3.up,"#b3eae9",3);}
            e.hp=Mathf.Clamp(n.hp,0,e.max);e.dead=n.dead;e.active=n.active;e.wind=n.wind;e.windStart=n.windStart;e.target=n.target;e.attackStyle=n.style;
            e.root.position=n.position;e.root.rotation=Quaternion.Euler(0,n.angle,0);e.root.gameObject.SetActive(n.active&&!n.dead&&FlatDistance(hero.position,n.position)<70);
            if(n.wind>0&&n.style!=1){if(!e.warning)e.warning=Cylinder("#d8757c",null,n.target,V(e.boss?7.4f:e.kind==FoeKind.Brute?5.2f:3.2f,.015f,e.boss?7.4f:e.kind==FoeKind.Brute?5.2f:3.2f)).gameObject;e.warning.transform.position=n.target;}else if(e.warning)Destroy(e.warning);
        }
        ApplyCoopHazards(s);
        if(s.guest!=null){
            if(!coopGuestInitialized||Vector3.Distance(hero.position,s.guest.position)>2.5f){motor.enabled=false;hero.position=s.guest.position;motor.enabled=true;coopGuestInitialized=true;}
            float priorHp=hp;hp=Mathf.Clamp(s.guest.hp,0,MaxHealth);if(hp<priorHp){hitFlash=.35f;PlaySfx("hurt");rig.Trigger(hp<=0?"Death":"Hurt");}if(hp<=0)phase=Phase.Defeat;else if(priorHp<=0&&phase==Phase.Defeat){phase=Phase.Play;ResetMovementPolish();gravityVelocity=jumpVelocity=0;rig.Play("Idle");}
            if(s.guestAmmo!=null&&s.guestAmmo.Length==3)ammo=(int[])s.guestAmmo.Clone();if(s.guestReload>reloadTime+.4f){rig.Trigger("Reload");PlaySfx("reload");}reloadTime=s.guestReload;
        }
        coopAck=s.ack;var bought=coopPending.FindLast(c=>c.kind=="buy"&&c.id<=coopAck&&c.arg>=0&&c.arg<3&&(ownedWeapons&(1<<c.arg))!=0);if(bought!=null&&bought.arg!=equippedWeapon)EquipWeapon(bought.arg);coopPending.RemoveAll(c=>c.id<=coopAck);
        if(s.panelSerial>coopSeenPanel){coopSeenPanel=s.panelSerial;if(s.panel==(int)Phase.Dialogue||s.panel==(int)Phase.Shop){phase=(Phase)s.panel;dialogueNpc=Mathf.Clamp(s.npc,0,2);}}
        if(s.won){phase=Phase.Win;boss.dead=true;}
        if(!string.IsNullOrEmpty(s.message)&&s.message!=notice)Notify(s.message);
    }
    readonly List<Transform> coopBoltVisuals=new List<Transform>(),coopRingVisuals=new List<Transform>();
    void PresentRemoteAction(CoopPlayer player){
        if(player.animation=="Shoot"){
            Vector3 start=coopRemote.position+Vector3.up*1.4f+coopRemote.forward*.4f;Vector3 end=start+coopRemote.forward*18;
            var ray=new Ray(start,coopRemote.forward);float distance;var target=FindShotTarget(ray,18,out distance);if(target!=null)end=ray.GetPoint(distance);
            if(ShotSceneHit(ray,Vector3.Distance(start,end),out var hit))end=hit.point;
            DrawShot(start,end);PlaySfxAt(player.weapon==1?"sidearm":"rifle",coopRemote.position);
        }else if(player.animation=="Sword"||player.animation=="Sword2"||player.animation=="Sword3"){
            var original=hero;hero=coopRemote;SlashEffect();hero=original;PlaySfxAt(player.animation=="Sword"?"sword1":player.animation=="Sword2"?"sword2":"sword3",coopRemote.position);
        }else if(player.animation=="Reload")PlaySfxAt("reload",coopRemote.position);
    }
    void ApplyCoopHazards(CoopSnapshot s){
        int bolts=s.bolts==null?0:Mathf.Min(32,s.bolts.Length);
        while(coopBoltVisuals.Count<bolts)coopBoltVisuals.Add(Ball("#b8dcff",null,Vector3.zero,Vector3.one*.23f));
        for(int i=0;i<coopBoltVisuals.Count;i++){coopBoltVisuals[i].gameObject.SetActive(i<bolts);if(i<bolts)coopBoltVisuals[i].position=s.bolts[i];}
        int rings=s.rings==null?0:Mathf.Min(8,s.rings.Length/4);
        while(coopRingVisuals.Count<rings){if(!ringMesh)PulseAt(hero.position,"#ffc39e",.1f,.01f);var r=G("Remote shockwave");r.gameObject.AddComponent<MeshFilter>().sharedMesh=ringMesh;r.gameObject.AddComponent<MeshRenderer>().sharedMaterial=AetherFx();coopRingVisuals.Add(r);}
        for(int i=0;i<coopRingVisuals.Count;i++){var r=coopRingVisuals[i];r.gameObject.SetActive(i<rings);if(i<rings){r.position=V(s.rings[i*4],s.rings[i*4+1],s.rings[i*4+2]);float radius=s.rings[i*4+3];r.localScale=V(radius,.025f,radius);}}
    }
    Vector3 EnemyFocus(Foe e){return CoopFriendReady&&coopRemoteState.hp>0&&(hp<=0||FlatDistance(e.root.position,coopRemote.position)<FlatDistance(e.root.position,hero.position))?coopRemote.position:hero.position;}
    void HurtCompanion(float damage){if(!CoopFriendReady||coopRemoteState.invuln>0||coopRemoteState.hp<=0)return;coopRemoteState.hp=Mathf.Max(0,coopRemoteState.hp-damage);coopRemoteState.invuln=.7f;coopRemoteRig.Trigger(coopRemoteState.hp<=0?"Death":"Hurt");}
    bool CompanionNear(Vector3 p,float radius){return CoopFriendReady&&coopRemoteState.hp>0&&FlatDistance(coopRemote.position,p)<radius;}
    void CoopHud(){
        if(!CoopActive)return;float x=uiWidth*.5f-145,y=boss!=null&&boss.active&&!boss.dead?105:16;
        UiBox(new Rect(x,y,290,42),uiNavy);UiText(x+12,y+7,266,28,coop.Connected?"CO-OP  /  2 travellers":"CO-OP  /  "+coop.Status,uiSmall,uiCyan);
        if(coopRemote&&coop.Connected){Vector2 p;if(UiProject(coopRemote.position+Vector3.up*2.35f,out p))UiText(p.x-85,p.y-20,170,25,CoopIsHost?"Friend":"Host",uiCenter,uiCyan);}
        if(CoopWorldPaused&&!uiCoopOpen&&phase==Phase.Play)UiText(uiWidth*.5f-200,y+55,400,40,"Waiting while your friend is in a menu",uiCenter,uiGold);
    }
}
