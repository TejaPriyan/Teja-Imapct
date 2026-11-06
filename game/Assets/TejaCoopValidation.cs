using UnityEngine;
using System;
using System.IO;
using System.Collections;

public partial class TejaAdventure {
    bool coopTest; string coopTestDir; bool coopTestPublic;
    void ValidateCoopRules(){
        int serial=rig.TriggerSerial;rig.Trigger("Shoot");Require(rig.TriggerSerial==serial+1,"Attack animation serial increments");
        Require(rig.ApplyNetworkState("Shoot",500)&&!rig.ApplyNetworkState("Shoot",500)&&rig.ApplyNetworkState("Shoot",501),"Remote repeated shots restart exactly once per action");
        Require(!rig.ApplyNetworkState("Idle",502)&&TejaRig.NetworkAnimation("invalid")=="Idle","Remote animation state is bounded");
        string originalPath=savePath;bool wasSmoke=smoke,wasCheck=inputCheck;string testPath=Path.Combine(Application.temporaryCachePath,"teja-guest-save-isolation-test.json");
        try{
            File.WriteAllText(testPath,"solo-save-sentinel");savePath=testPath;smoke=inputCheck=false;coopGuestSession=true;SaveJourney(false);
            Require(File.ReadAllText(testPath)=="solo-save-sentinel","Guest never overwrites the solo save, including after transport failure");
            coopGuestSession=false;coopExecuting=true;SaveJourney(false);Require(File.ReadAllText(testPath)=="solo-save-sentinel","Remote actor context never writes a host save with guest position");
            Require(!ValidPosition(new Vector3(float.NaN,0,0))&&!ValidPosition(new Vector3(10000,0,0)),"Invalid network positions rejected");
        }finally{savePath=originalPath;smoke=wasSmoke;inputCheck=wasCheck;coopGuestSession=coopExecuting=false;if(File.Exists(testPath))File.Delete(testPath);}
    }
    void CoopTestPlace(Transform actor,CharacterController controller,Vector3 position){controller.enabled=false;actor.position=position;controller.enabled=true;Physics.SyncTransforms();}
    void CoopTestStage(string stage){File.WriteAllText(Path.Combine(coopTestDir,"stage.txt"),stage);}
    string CoopTestReadStage(){try{return File.ReadAllText(Path.Combine(coopTestDir,"stage.txt"));}catch{return "";}}
    IEnumerator CoopTestWait(Func<bool> ready,string description,float seconds=35){
        float deadline=Time.realtimeSinceStartup+seconds;float logAfter=0;while(!ready()){if(Time.realtimeSinceStartup>logAfter){logAfter=Time.realtimeSinceStartup+5;File.WriteAllText(Path.Combine(coopTestDir,CoopIsGuest?"guest-progress.txt":"host-progress.txt"),description+"; "+(coop==null?"no transport":coop.Status+"; "+coop.LastDiagnostic));}if(Time.realtimeSinceStartup>deadline)throw new Exception("Timed out: "+description+"; "+(coop==null?"no transport":coop.Status+"; "+coop.LastDiagnostic)+"; "+coopMessage);yield return null;}
    }
    // Tests run in two actual players and never read or write a normal journey.
    IEnumerator RunCoopTest(bool host){
        var enumerator=host?CoopTestHost():CoopTestGuest();Exception failure=null;
        var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(enumerator);
        while(stack.Count>0){object current=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception ex){failure=ex;break;}
            if(!next){stack.Pop();continue;}if(current is IEnumerator){stack.Push((IEnumerator)current);continue;}yield return current;
        }
        string result=failure==null?"PASS v0.7 "+(host?"HOST":"GUEST")+": real transport; room join; shared purchase and duplicate protection; guest movement; guest ranged kill; shared quest reward once; guest boss kill and shared victory.":"FAIL "+failure;
        File.WriteAllText(Path.Combine(coopTestDir,host?"host-result.txt":"guest-result.txt"),result);Debug.Log("TEJA_COOP_TEST "+result);
        EndCoop(false);Application.Quit(failure==null?0:1);
    }
    IEnumerator CoopTestHost(){
        yield return null;HostCoop(!coopTestPublic);uiCoopOpen=false;previewCameraOverride=true;
        foreach(var e in foes){e.active=false;e.root.gameObject.SetActive(false);}coins=240;ownedWeapons=1;invulnerable=9999;
        CoopTestPlace(hero,motor,merchantPosition+V(0,.2f,-3));
        File.WriteAllText(Path.Combine(coopTestDir,"snapshot-size.txt"),System.Text.Encoding.UTF8.GetByteCount(JsonUtility.ToJson(CreateCoopSnapshot())).ToString());
        yield return CoopTestWait(()=>!string.IsNullOrEmpty(coop.InviteCode),"room invite",75);
        File.WriteAllText(Path.Combine(coopTestDir,"invite.private.txt"),coop.InviteCode);
        yield return CoopTestWait(()=>CoopFriendReady&&coopGuestInitialized,"guest connected",75);
        yield return CoopTestWait(()=>ownedWeapons==3&&coins==180,"guest purchases sidearm");
        CoopTestStage("purchased");yield return CoopTestWait(()=>coopAck>=2,"duplicate purchase acknowledged");Require(coins==180,"Duplicate guest purchase is free");
        Vector3 before=coopRemote.position;CoopTestStage("move");yield return CoopTestWait(()=>FlatDistance(before,coopRemote.position)>.8f,"guest movement reaches host");
        AcceptMission(0);commissionDefeats=5;var enemy=foes[0];enemy.dead=false;enemy.active=true;enemy.hp=20;enemy.cooldown=999;enemy.speed=0;enemy.root.gameObject.SetActive(true);
        Vector3 arena=OnLand(45,60,.18f);CoopTestPlace(coopRemote,coopRemoteMotor,arena);coopRemoteState.invuln=9999;enemy.root.position=arena+V(0,0,-5);enemy.root.position=OnLand(enemy.root.position.x,enemy.root.position.z,.1f);enemy.spawn=enemy.root.position;
        CoopTestStage("shoot");yield return CoopTestWait(()=>enemy.dead,"guest shot kills authoritative foe");Require(missionStates[0]==2&&commissionDefeats==6,"Guest defeat advances shared mission: state="+missionStates[0]+" defeats="+commissionDefeats);
        int beforeQuest=coins;CoopTestPlace(coopRemote,coopRemoteMotor,npcLocations[0]+V(0,.2f,-2));CoopTestStage("quest");
        yield return CoopTestWait(()=>missionStates[0]==3,"guest claims quest");yield return new WaitForSecondsRealtime(1);Require(coins==beforeQuest+missionCoins[0],"Shared mission pays once");
        CoopTestPlace(coopRemote,coopRemoteMotor,arena);boss.active=true;boss.dead=false;boss.hp=1;boss.cooldown=999;boss.speed=0;boss.spawn=arena+V(0,0,-6);boss.root.position=OnLand(boss.spawn.x,boss.spawn.z,.1f);boss.root.gameObject.SetActive(true);CoopTestStage("boss");
        yield return CoopTestWait(()=>boss.dead&&phase==Phase.Win,"guest defeats boss");
        yield return CoopTestWait(()=>File.Exists(Path.Combine(coopTestDir,"guest-victory.txt")),"guest receives victory");
        Require(coopAck>=6,"Guest commands acknowledged");yield return new WaitForSecondsRealtime(.5f);
    }
    IEnumerator CoopTestGuest(){
        yield return CoopTestWait(()=>File.Exists(Path.Combine(coopTestDir,"invite.private.txt")),"host invite",90);
        JoinCoop(File.ReadAllText(Path.Combine(coopTestDir,"invite.private.txt")),!coopTestPublic);previewCameraOverride=true;
        yield return CoopTestWait(()=>coopGuestInitialized&&coop.Connected&&coins==240,"initial shared snapshot",75);
        QueueCoopCommand("buy",1);yield return CoopTestWait(()=>ownedWeapons==3&&coins==180,"shared weapon purchase");QueueCoopCommand("buy",1);
        yield return CoopTestWait(()=>CoopTestReadStage()=="move","movement stage");float until=Time.realtimeSinceStartup+1;
        while(Time.realtimeSinceStartup<until){motor.Move(Vector3.right*2*Time.deltaTime);yield return null;}
        yield return CoopTestWait(()=>CoopTestReadStage()=="shoot"&&FlatDistance(hero.position,OnLand(45,60))<1.5f,"combat position");
        yield return new WaitForSecondsRealtime(.3f);EquipWeapon(1);strikeCD=0;cam.transform.position=hero.position+Vector3.up*1.35f;cam.transform.LookAt(foes[0].root.position+Vector3.up*EnemyTargetHeight(foes[0]));Strike();
        yield return CoopTestWait(()=>foes[0].dead&&missionStates[0]==2,"shared guest kill and mission progress");
        yield return CoopTestWait(()=>CoopTestReadStage()=="quest"&&NearbyNpc()==0,"quest return position");QueueCoopCommand("complete",0);QueueCoopCommand("complete",0);
        yield return CoopTestWait(()=>missionStates[0]==3,"shared mission completed");
        yield return CoopTestWait(()=>CoopTestReadStage()=="boss"&&FlatDistance(hero.position,OnLand(45,60))<1.5f&&boss.active,"boss position");
        yield return new WaitForSecondsRealtime(.3f);strikeCD=0;cam.transform.position=hero.position+Vector3.up*1.35f;cam.transform.LookAt(boss.root.position+Vector3.up*EnemyTargetHeight(boss));Strike();
        yield return CoopTestWait(()=>phase==Phase.Win&&boss.dead,"shared victory");File.WriteAllText(Path.Combine(coopTestDir,"guest-victory.txt"),"received");yield return new WaitForSecondsRealtime(1);
    }
}
