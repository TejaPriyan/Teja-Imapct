using UnityEngine;
using System;
using System.IO;
using System.Collections;

public partial class TejaAdventure {
    void ValidateGameplayFixesV7(){
        string originalPath=savePath;bool wasSmoke=smoke,wasCheck=inputCheck;
        string testDirectory=Path.Combine(Application.temporaryCachePath,"teja-save-recovery-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        try{
            StartGame();coins=149;var saved=CurrentJourney();string good=JsonUtility.ToJson(saved);
            savePath=Path.Combine(testDirectory,"journey-v3.json");File.WriteAllText(savePath,"{broken save");File.WriteAllText(savePath+".bak",good);
            smoke=inputCheck=false;StartGame(true);
            Require(phase==Phase.Play&&coins==149,"Continue recovers progress from a valid backup");
            Require(File.ReadAllText(savePath+".bak")==good,"Recovery does not replace the good backup with the corrupt primary");
            Require(JsonUtility.FromJson<JourneyV2>(File.ReadAllText(savePath)).coins==149,"Recovered primary is saved successfully");
            File.WriteAllText(savePath,"{broken primary");File.WriteAllText(savePath+".bak","{broken backup");StartGame(true);
            Require(phase==Phase.Title,"Failed Continue returns to title instead of starting a fresh saved journey");
            Require(File.ReadAllText(savePath)=="{broken primary"&&File.ReadAllText(savePath+".bak")=="{broken backup","Failed Continue preserves the original files for recovery");
            smoke=true;StartGame();activeWaystone=2;waystoneMask=7;hp=60;gravityVelocity=-80;dashTime=.2f;
            motor.enabled=false;hero.position=V(45,-7,60);motor.enabled=true;Move(Vector2.zero,.01f);
            Require(FlatDistance(hero.position,WorldV6RespawnPoint())<.1f&&gravityVelocity==0&&dashTime==0&&hp==50,"Fall recovery uses the active checkpoint and clears downward velocity and dash");
            var foe=foes[0];var center=foe.root.position+Vector3.up*EnemyTargetHeight(foe);float distance;
            Require(FindShotTarget(new Ray(center,Vector3.forward),10,out distance)==foe&&distance==0,"Point-blank aim detects an enemy containing the ray origin");
        }finally{
            smoke=true;savePath=originalPath;StartGame();smoke=wasSmoke;inputCheck=wasCheck;
            foreach(string path in Directory.GetFiles(testDirectory))File.Delete(path);Directory.Delete(testDirectory);
        }
    }
    IEnumerator SmokeTest(){
        yield return null;int code=0;string result;
        try{
            StartGame();Require(foes.Count==47&&worldChests.Count==6&&worldCoins.Count==188,"Expanded world counts");Require(rig.animationPlayer.GetClipCount()==15,"Fifteen authored animation clips");
            foreach(var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())foreach(var w in renderer.sharedMesh.boneWeights)Require(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.001f,"Normalized skinning");
            foreach(var p in new[]{V(112,0,-12),V(-108,0,-54),V(-123,0,-70),V(0,0,-119),V(29,0,108)}){Require(Physics.Raycast(p+Vector3.up*30,Vector3.down,out var ground,60),"Frontier has collidable ground");Require(ground.point.y>=LandscapeHeight(p)-.6f,"Frontier terrain height");}
            ValidateAudio();ValidateWorldV6();ValidateCoopRules();ValidateProgression();ValidateControls();ValidateMobileControlsV7();ValidateGameplayFixesV7();StartGame();motor.enabled=false;hero.position=OnLand(91,-12,.15f);motor.enabled=true;Physics.SyncTransforms();for(int step=0;step<115;step++)Move(V2(-1,0),.05f);Require(hero.position.x>123,"Bridge is traversable by the character controller");StartGame();var before=hero.position;Move(V2(0,-1),.1f);Require(hero.position.z<before.z,"Movement");
            BuyWeapon(1);Require(coins==0&&ownedWeapons==1,"Purchase affordability");AddCoins(240);BuyWeapon(1);BuyWeapon(1);Require(coins==180&&ownedWeapons==3,"No duplicate purchase charge");BuyWeapon(2);Require(coins==0&&equippedWeapon==2,"Both guns unlock");
            var foe=foes[0];foe.root.position=hero.position+hero.forward*5;foe.hp=75;cam.transform.position=hero.position+Vector3.up*1.35f;cam.transform.LookAt(foe.root.position+Vector3.up*EnemyTargetHeight(foe));var blocker=Box("#aabbcc",null,hero.position+hero.forward*2.5f+Vector3.up*1.2f,V(2,3,1),true);Physics.SyncTransforms();strikeCD=0;Strike();Require(foe.hp==75,"Scenery blocks ranged fire");blocker.GetComponent<Collider>().enabled=false;Destroy(blocker.gameObject);ammo[2]=18;strikeCD=0;Strike();Require(foe.hp<75&&ammo[2]==17,"Ranged hit and magazine");ammo[2]=0;strikeCD=0;Strike();Require(reloadTime>0,"Empty magazine reload");UpdateWeaponTimers(2);Require(ammo[2]==18,"Reload completion");
            float rayDistance;var center=foe.root.position+Vector3.up*EnemyTargetHeight(foe);Require(FindShotTarget(new Ray(center-Vector3.forward*4,Vector3.forward),10,out rayDistance)==foe,"Aim ray hits enemy volume");Require(FindShotTarget(new Ray(V(0,100,0),Vector3.up),10,out rayDistance)==null,"Aim miss");
            hp=60;potions=1;UsePotion();Require(hp==MaxHealth&&potions==0,"Healing");UsePotion();Require(potions==0,"Flask underflow");AddCoins(25);BuyPotion();Require(potions==1&&coins==0,"Flask economy");
            foreach(var s in sigils){motor.enabled=false;hero.position=s.root.position+Vector3.up*.2f;motor.enabled=true;Interact();}Require(count==3&&coins==90,"Sigil rewards");int funds=coins;Interact();Require(coins==funds,"No repeated sigil reward");
            motor.enabled=false;hero.position=worldCoins[0].spawn;motor.enabled=true;UpdateExploration(0);Require(coins==93,"Trail coin");UpdateExploration(0);Require(coins==93,"No repeated trail reward");
            motor.enabled=false;hero.position=worldChests[5].root.position+V(0,.2f,-2);motor.enabled=true;Interact();Require((chestMask&32)!=0&&coins==143,"Sixth chest reward and mask");Interact();Require(coins==143,"No repeated chest reward");
            foes[33].dead=true;foes[1].dead=false;var saved=JsonUtility.FromJson<JourneyV2>(JsonUtility.ToJson(CurrentJourney()));StartGame();Require(ApplyJourney(saved)&&foes[33].dead&&!foes[1].dead&&chestMask==32&&coins==143,"Save supports more than 32 foes and 6 chests");saved.x=float.NaN;Require(!ApplyJourney(saved),"Corrupt save rejected");
            StartGame();var legacy=new JourneyV2{version=2,x=0,z=25,hp=75,coins=55,owned=3,equipped=1,deadMask=2,mask=1,ammo=new[]{0,4,18}};Require(ApplyJourney(legacy)&&coins==55&&foes[1].dead&&!foes[33].dead&&playerLevel==1,"v0.2 progress migration");
            StartGame();OpenShop();Require(phase==Phase.Play,"Shop proximity");motor.enabled=false;hero.position=merchantPosition+V(0,0,-3);motor.enabled=true;OpenShop();Require(phase==Phase.Shop,"Shop panel");HandleEscape();TogglePanel(Phase.Journal);Require(phase==Phase.Journal,"Journal panel");HandleEscape();TogglePanel(Phase.Character);Require(phase==Phase.Character,"Character panel");HandleEscape();phase=Phase.Pause;uiCredits=true;HandleEscape();Require(!uiCredits&&phase==Phase.Pause,"Modal close does not resume");HandleEscape();
            boss.active=true;boss.root.gameObject.SetActive(true);boss.hp=boss.max;Require(BossStage(boss)==1,"Boss phase one");boss.hp=boss.max*.5f;Require(BossStage(boss)==2,"Boss phase two");boss.hp=boss.max*.2f;Require(BossStage(boss)==3,"Boss phase three");boss.attackStyle=1;ResolveEnemyAttack(boss);Require(enemyBolts.Count==7,"Final phase seven-bolt volley");boss.attackStyle=2;ResolveEnemyAttack(boss);Require(shockwaves.Count==1,"Jumpable boss shockwave");
            hp=MaxHealth;invulnerable=0;Hurt(12);Require(hp==MaxHealth-12,"Enemy damage");float hurtHp=hp;Hurt(12);Require(hp==hurtHp,"Damage immunity window");funds=coins;Hit(boss,9999);Require(phase==Phase.Win&&coins==funds+200,"Boss victory and reward");StartGame();Require(enemyBolts.Count==0&&shockwaves.Count==0&&playerLevel==1&&coins==0,"Full new-game reset");invulnerable=0;Hurt(9999);Require(phase==Phase.Defeat,"Defeat");
            result="PASS v0.7: landscape mobile control rules; backup save recovery and failed-load preservation; checkpoint fall recovery; point-blank aim; original audio assets, grotto puzzle and checkpoints, guest save isolation; 47 enemies / 4 normal types / phased boss; 188 coins; 6 chests; A/D camera-relative movement at four headings; LMB sword/gun/hold/reload and UI blocking; automatic mouse look, cursor release and aim controls; lock target priority; terrain collision and full bridge traversal; scenery blocks shots; 15 clips and skin weights; all 3 NPC quest flows and duplicate rewards; XP/skill/weapon progression caps and restoration; movement; shop; ranged aim hits/misses; ammo/reload; healing; quest/treasure economy; saves beyond 32 enemies; v0.2 migration; modal controls; boss volley/shockwave; victory/reset/defeat.";
        }catch(Exception ex){code=1;result="FAIL v0.7: "+ex;}
        Debug.Log("TEJA_SMOKE "+result);var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-tejaTestResult");if(i>=0&&i+1<args.Length)File.WriteAllText(args[i+1],result);Application.Quit(code);
    }
    Vector2 V2(float x,float y){return new Vector2(x,y);}
    IEnumerator CaptureV5(){
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-tejaPreviewDir");if(index<0||index+1>=args.Length){Application.Quit(1);yield break;}string dir=args[index+1];Directory.CreateDirectory(dir);
        yield return new WaitForSecondsRealtime(1);RequestUiPreview(Path.Combine(dir,"title.png"));yield return new WaitForSecondsRealtime(.4f);
        StartGame();yield return new WaitForSecondsRealtime(.8f);RequestUiPreview(Path.Combine(dir,"gameplay.png"));yield return new WaitForSecondsRealtime(.4f);
        phase=Phase.Dialogue;dialogueNpc=0;yield return new WaitForSecondsRealtime(.3f);RequestUiPreview(Path.Combine(dir,"npc-mission.png"));yield return new WaitForSecondsRealtime(.4f);
        AcceptMission(0);phase=Phase.Journal;yield return new WaitForSecondsRealtime(.3f);RequestUiPreview(Path.Combine(dir,"quest-journal.png"));yield return new WaitForSecondsRealtime(.4f);
        AwardExperience(550);coins=320;phase=Phase.Character;yield return new WaitForSecondsRealtime(.3f);RequestUiPreview(Path.Combine(dir,"character-upgrades.png"));yield return new WaitForSecondsRealtime(.4f);
        phase=Phase.Pause;previewCameraOverride=true;cam.transform.position=V(129,19,11);cam.transform.LookAt(V(104,1,-15));RenderPreview(Path.Combine(dir,"silverrun-bridge.png"));
        cam.transform.position=V(48,20,134);cam.transform.LookAt(OnLand(23,109));RenderPreview(Path.Combine(dir,"whisperpine-forest.png"));cam.transform.position=V(27,19,-96);cam.transform.LookAt(OnLand(0,-119));RenderPreview(Path.Combine(dir,"starfall-sanctum.png"));
        motor.enabled=false;hero.position=OnLand(0,25,.15f);motor.enabled=true;hero.rotation=Quaternion.Euler(0,180,0);cam.transform.position=hero.position+V(-2.8f,1.5f,-3.2f);cam.transform.LookAt(hero.position+Vector3.up*1.1f);EquipWeapon(0);
        foreach(string clip in new[]{"Idle","Run","Sword2","Sword3","Dodge"}){rig.animationPlayer.Stop();rig.animationPlayer.GetClip(clip).SampleAnimation(rig.gameObject,TejaRig.Duration(clip)*.45f);yield return null;RenderPreview(Path.Combine(dir,"character-"+clip.ToLower()+".png"));}
        ownedWeapons=7;EquipWeapon(2);rig.animationPlayer.Stop();rig.animationPlayer.GetClip("GunIdle").SampleAnimation(rig.gameObject,.2f);yield return null;RenderPreview(Path.Combine(dir,"rifle-stance.png"));
        phase=Phase.Map;yield return new WaitForSecondsRealtime(.3f);RequestUiPreview(Path.Combine(dir,"world-map.png"));yield return new WaitForSecondsRealtime(.4f);
        phase=Phase.Pause;pausePage=PausePage.Journey;yield return new WaitForSecondsRealtime(.25f);RequestUiPreview(Path.Combine(dir,"pause.png"));yield return new WaitForSecondsRealtime(.35f);
        pausePage=PausePage.Controls;yield return new WaitForSecondsRealtime(.25f);RequestUiPreview(Path.Combine(dir,"controls.png"));yield return new WaitForSecondsRealtime(.35f);
        pausePage=PausePage.Settings;yield return new WaitForSecondsRealtime(.25f);RequestUiPreview(Path.Combine(dir,"settings.png"));yield return new WaitForSecondsRealtime(.35f);
        previewCameraOverride=false;StartGame();uiQuestCollapsed=true;yield return new WaitForSecondsRealtime(.6f);RequestUiPreview(Path.Combine(dir,"hud-collapsed.png"));yield return new WaitForSecondsRealtime(.35f);
        Screen.SetResolution(1024,768,false);yield return new WaitForSecondsRealtime(.8f);phase=Phase.Character;coins=320;AwardExperience(550);yield return new WaitForSecondsRealtime(.25f);RequestUiPreview(Path.Combine(dir,"character-4x3.png"));yield return new WaitForSecondsRealtime(.35f);
        phase=Phase.Pause;pausePage=PausePage.Controls;yield return new WaitForSecondsRealtime(.25f);RequestUiPreview(Path.Combine(dir,"controls-4x3.png"));yield return new WaitForSecondsRealtime(.35f);
        phase=Phase.Play;yield return new WaitForSecondsRealtime(.3f);RequestUiPreview(Path.Combine(dir,"hud-4x3.png"));yield return new WaitForSecondsRealtime(.35f);
        Screen.SetResolution(1280,800,false);yield return new WaitForSecondsRealtime(.8f);phase=Phase.Character;yield return new WaitForSecondsRealtime(.25f);RequestUiPreview(Path.Combine(dir,"character-16x10.png"));yield return new WaitForSecondsRealtime(.35f);
        Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.6f);phase=Phase.Pause;uiCoopOpen=true;yield return new WaitForSecondsRealtime(.25f);RequestUiPreview(Path.Combine(dir,"online-coop.png"));yield return new WaitForSecondsRealtime(.4f);uiCoopOpen=false;previewCameraOverride=true;cam.transform.position=OnLand(-85.7f,-26)+Vector3.up*2.7f;cam.transform.LookAt(OnLand(-94,-26)+Vector3.up*1.7f);RenderPreview(Path.Combine(dir,"moonwell-grotto.png"));
        Debug.Log("TEJA_PREVIEW_OK v0.7");Application.Quit(0);
    }
}
