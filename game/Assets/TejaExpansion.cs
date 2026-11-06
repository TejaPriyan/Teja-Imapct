using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public partial class TejaAdventure {
    class WeaponInfo { public string name,description;public int price,damage,magazine;public float cooldown,range; }
    readonly WeaponInfo[] weapons={
        new WeaponInfo{name="Frostglass",description="A swift sword. Three strikes form a combo.",price=0,damage=23,magazine=0,cooldown=.43f,range=3.2f},
        new WeaponInfo{name="Dawn Sidearm",description="A precise eight-shot aether blaster.",price=60,damage=27,magazine=8,cooldown=.38f,range=24},
        new WeaponInfo{name="Aurora Rifle",description="Fast fire, a long reach, and eighteen shots.",price=180,damage=17,magazine=18,cooldown=.15f,range=34}
    };
    int coins,ownedWeapons=1,equippedWeapon,potions=1,chestMask;
    bool journeyPreserveBackup;
    int[] ammo={0,8,18};float reloadTime,hitFlash,shotFlash,landingTime;TejaRig rig;Transform equippedModel;Material blasterMaterial;
    class Spark {public Transform root;public Vector3 velocity;public float life;}
    readonly List<Spark> sparks=new List<Spark>();Mesh ringMesh;bool wasGrounded,previewCameraOverride;
    void BuildHero(){
        hero=G("Teja — Eula prototype skin",null,V(0,.15f,25));motor=hero.gameObject.AddComponent<CharacterController>();motor.height=1.92f;motor.center=V(0,.98f,0);motor.radius=.28f;motor.stepOffset=.32f;motor.skinWidth=.035f;
        var prefab=Resources.Load<GameObject>("Eula/Eula");if(!prefab)throw new InvalidOperationException("Eula prefab missing. Run CharacterBuilder.Build before building the game.");
        var model=Instantiate(prefab,hero,false);model.transform.localScale=Vector3.one*.92f;rig=model.GetComponent<TejaRig>();
        if(!rig||rig.bones.Length<20)throw new InvalidOperationException("Eula generated rig is incomplete.");body=rig.bones[0];head=rig.bones[4];EquipWeapon(0);
    }
    void EquipWeapon(int index){
        if(index<0||index>=weapons.Length)return;if((ownedWeapons&(1<<index))==0){Notify("Find the trader to unlock this weapon.");return;}
        equippedWeapon=index;if(index==0)aiming=false;reloadTime=0;strikeCD=.15f;if(equippedModel)Destroy(equippedModel.gameObject);
        var holder=G("Equipped "+weapons[index].name);equippedModel=holder;
        if(index==0){Box("#e4c58f",holder,V(0,0,.12f),V(.31f,.07f,.1f));Cylinder("#283d58",holder,V(0,0,-.025f),V(.08f,.12f,.08f)).localRotation=Quaternion.Euler(90,0,0);Box("#bcf0ee",holder,V(0,0,.58f),V(.075f,.12f,.86f));Box("#e7e5c4",holder,V(0,0,1.035f),V(.03f,.08f,.09f));holder.SetParent(rig.socket,false);holder.localRotation=Quaternion.Euler(90,0,0);}
        else {var prefab=Resources.Load<GameObject>(index==1?"Weapons/blaster-k":"Weapons/blaster-a");if(!prefab)throw new InvalidOperationException("Kenney blaster model is missing.");var model=Instantiate(prefab,holder,false);var rr=model.GetComponentsInChildren<Renderer>();Bounds bounds=new Bounds();bool first=true;foreach(var r in rr){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));model.transform.position-=bounds.center;holder.localScale=Vector3.one*((index==1?.48f:.82f)/Mathf.Max(.01f,size));if(!blasterMaterial){blasterMaterial=new Material(Shader.Find("Teja/CharacterToon"));blasterMaterial.mainTexture=Resources.Load<Texture2D>("Weapons/Textures/variation-a");}var weaponMaterial=blasterMaterial;foreach(var r in rr){var ms=new Material[r.sharedMaterials.Length];for(int n=0;n<ms.Length;n++)ms[n]=weaponMaterial;r.sharedMaterials=ms;}holder.SetParent(rig.socket,false);holder.localPosition=index==1?V(0,.09f,.04f):V(0,.07f,.19f);holder.localRotation=Quaternion.Euler(0,0,48);}
        if(phase==Phase.Play||phase==Phase.Inventory||phase==Phase.Shop){Notify(weapons[index].name+" equipped");SaveJourney(false);}
    }
    void BuyWeapon(int index){if(CoopIsGuest){QueueCoopCommand("buy",index);return;}if(index<1||index>=weapons.Length)return;if((ownedWeapons&(1<<index))!=0){EquipWeapon(index);return;}if(coins<weapons[index].price){Notify("You need "+(weapons[index].price-coins)+" more coins.");return;}coins-=weapons[index].price;ownedWeapons|=1<<index;ammo[index]=weapons[index].magazine;EquipWeapon(index);Notify(weapons[index].name+" unlocked and equipped.");SaveJourney(false);Tone(780,.2f);}
    void BuyPotion(){if(CoopIsGuest){QueueCoopCommand("flask");return;}if(coins<25){Notify("A healing flask costs 25 coins.");return;}if(potions>=9){Notify("Your flask pouch is full (9).");return;}coins-=25;potions++;Notify("Healing flask added to your inventory.");SaveJourney(false);}
    void UsePotion(){if(CoopIsGuest){QueueCoopCommand("heal");return;}if(phase!=Phase.Play&&phase!=Phase.Inventory)return;if(hp>=MaxHealth){Notify("Your health is already full.");return;}if(potions<=0){Notify("No flasks left. Visit the trader.");return;}potions--;hp=Mathf.Min(MaxHealth,hp+45);PulseAt(hero.position,"#bce4ab",2,.5f);Notify("Healing flask  ·  +45 HP");Tone(720,.3f);SaveJourney(false);}
    void AddCoins(int amount){if(amount<=0)return;coins=Mathf.Min(999999,coins+amount);Tone(970,.08f);}
    void OpenShop(){if(phase==Phase.Shop){phase=Phase.Play;return;}if(phase!=Phase.Play)return;if(FlatDistance(hero.position,merchantPosition)>6){Notify("Visit the trader in Lantern Village. Open M for the map.");return;}phase=Phase.Shop;stick=Vector2.zero;stickFinger=lookFinger=-1;}
    void TogglePanel(Phase panel){if(phase==panel){phase=Phase.Play;return;}if(phase==Phase.Play||phase==Phase.Pause||phase==Phase.Inventory||phase==Phase.Map||phase==Phase.Journal||phase==Phase.Character){ResetUiNavigation();phase=panel;stick=Vector2.zero;stickFinger=lookFinger=-1;}}
    void Reload(){if(CoopIsGuest){if(reloadTime<=0){QueueCoopCommand("reload");reloadTime=.3f;rig.Trigger("Reload");PlaySfx("reload");}return;}if(phase!=Phase.Play||equippedWeapon==0||reloadTime>0||ammo[equippedWeapon]>=weapons[equippedWeapon].magazine)return;reloadTime=equippedWeapon==1?1.05f:1.5f;rig.Trigger("Reload");swing=reloadTime;Notify("Recharging "+weapons[equippedWeapon].name+"…");PlaySfx("reload");}
    void UpdateWeaponTimers(float dt){if(reloadTime>0){reloadTime-=dt;if(reloadTime<=0){reloadTime=0;ammo[equippedWeapon]=weapons[equippedWeapon].magazine;PlaySfx("reload_done");}}}
    void StartGame(bool load=false){
        ResetWorldV6();ResetMovementPolish();ResetProgression();ResetCombat();ResetControlInput();journeyPreserveBackup=false;hp=MaxHealth;count=kills=coins=chestMask=0;ownedWeapons=1;equippedWeapon=0;potions=1;ammo=new[]{0,8,18};elapsed=saveClock=0;landingTime=hitFlash=shotFlash=0;wasGrounded=false;burstCD=dodgeCD=strikeCD=swing=invulnerable=dashTime=jumpVelocity=gravityVelocity=reloadTime=0;yaw=0;pitch=20;stick=Vector2.zero;stickFinger=lookFinger=-1;
        motor.enabled=false;hero.position=V(0,.16f,25);hero.rotation=Quaternion.Euler(0,180,0);motor.enabled=true;Physics.SyncTransforms();
        foreach(var s in sigils){s.taken=false;s.root.gameObject.SetActive(true);}foreach(var e in foes){e.hp=e.max;e.dead=false;e.active=!e.boss;e.root.gameObject.SetActive(e.active);e.root.position=e.spawn;e.wind=e.charge=e.stagger=0;e.attackCycle=0;e.cooldown=1;if(e.warning)Destroy(e.warning);}
        foreach(var p in pulses)if(p.root)Destroy(p.root.gameObject);pulses.Clear();foreach(var s in sparks)if(s.root)Destroy(s.root.gameObject);sparks.Clear();ResetExploration();phase=Phase.Play;
        if(load&&!LoadJourney()){phase=Phase.Title;rig.Play("Idle");Notify("Your save could not be loaded. Your files are unchanged; choose New journey only if you want to start over.");return;}
        bool recovered=journeyPreserveBackup;EquipWeapon(equippedWeapon);rig.Play("Idle");Notify(load?(recovered?"Journey recovered from your backup. Your progress is safe.":"Journey continued. Welcome back, Teja."):"Teja, welcome to the Skybound Isles. Coins are waiting along the trails.");
    }
    void Update(){
        float dt=Mathf.Min(Time.deltaTime,.05f);TickMobileControls();TickCoop(dt);TickAudio(dt);TickWorldV6(dt);TickEnemyPresentation(dt);hitConfirm=Mathf.Max(0,hitConfirm-dt);noticeTime=Mathf.Max(0,noticeTime-dt);hitFlash=Mathf.Max(0,hitFlash-dt);shotFlash=Mathf.Max(0,shotFlash-dt);
        if(!smoke){if(phase!=Phase.Play||uiCredits||uiNewConfirm||uiCoopOpen){attackBuffer=0;leftGestureBlocked=Input.GetMouseButton(0);}if(Input.GetKeyDown(KeyCode.Escape))HandleEscape();if(!uiCredits&&!uiNewConfirm&&!uiCoopOpen){if(Input.GetKeyDown(KeyCode.I))TogglePanel(Phase.Inventory);if(Input.GetKeyDown(KeyCode.M))TogglePanel(Phase.Map);if(Input.GetKeyDown(KeyCode.B))OpenShop();if(Input.GetKeyDown(KeyCode.Q))TogglePanel(Phase.Journal);if(Input.GetKeyDown(KeyCode.C))TogglePanel(Phase.Character);}}
        SyncCursorMode();
        if(phase==Phase.Play&&!uiCredits&&!uiNewConfirm&&!uiCoopOpen&&!MobilePortraitBlocked&&!CoopWorldPaused){elapsed+=dt;ReadTouch();Vector2 movement=stick;if(!smoke)movement+=HandleDesktopInput(ReadDesktopInput(),dt);
            if(phase==Phase.Play){Move(movement,dt);UpdateExploration(dt);if(!CoopIsGuest){UpdateFoes(dt);TickProgression(dt);TickCombat(dt);UpdateWeaponTimers(dt);}else TickCombatPresentation(dt);saveClock+=dt;if(saveClock>12){saveClock=0;SaveJourney(false);}}
        }
        if(phase==Phase.Play||phase==Phase.Title||phase==Phase.Win||phase==Phase.Defeat){for(int i=pulses.Count-1;i>=0;i--){var p=pulses[i];p.life-=dt;if(p.life<=0){Destroy(p.root.gameObject);pulses.RemoveAt(i);}else{float size=Mathf.Lerp(.3f,p.radius,1-p.life/p.max);p.root.localScale=V(size,.02f,size);}}foreach(var s in sigils)if(!s.taken){s.crystal.Rotate(0,45*dt,0,Space.World);s.crystal.localPosition=V(0,1.6f+Mathf.Sin(Time.time*2)*.15f,0);}for(int i=sparks.Count-1;i>=0;i--){var s=sparks[i];s.life-=dt;if(s.life<=0){Destroy(s.root.gameObject);sparks.RemoveAt(i);}else{s.velocity+=Vector3.down*7*dt;s.root.position+=s.velocity*dt;s.root.localScale=Vector3.one*s.life*.09f;}}}
        SyncCursorMode();
        if(previewCameraOverride)return;
        if(phase==Phase.Title){cam.transform.position=hero.position+V(2.4f,1.8f,4.6f);cam.transform.LookAt(hero.position+V(1.4f,1.22f,0));rig.Play("Idle");}
        else{if(lockTarget!=null&&(!lockTarget.active||lockTarget.dead||FlatDistance(hero.position,lockTarget.root.position)>40))lockTarget=null;if(lockTarget!=null&&!aiming){var facing=lockTarget.root.position-hero.position;yaw=Mathf.LerpAngle(yaw,Mathf.Atan2(-facing.x,-facing.z)*Mathf.Rad2Deg,1-Mathf.Exp(-dt*6));}cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,aiming?40:48,dt*8);Vector3 offset=Quaternion.Euler(0,yaw,0)*(aiming?V(.75f,1.5f+Mathf.Sin(pitch*Mathf.Deg2Rad)*2.5f,3.3f+cameraDistanceOffset*.25f):V(.3f,1.7f+Mathf.Sin(pitch*Mathf.Deg2Rad)*6,6.1f+cameraDistanceOffset));Vector3 desired=V(hero.position.x,Mathf.Max(0,hero.position.y),hero.position.z)+offset;var collisionFocus=hero.position+Vector3.up*1.35f;var focus=equippedWeapon>0?hero.position+Vector3.up*1.45f+Quaternion.Euler(0,yaw,0)*V(0,-Mathf.Sin((pitch-20)*Mathf.Deg2Rad)*30,-30):collisionFocus;if(Physics.Linecast(collisionFocus,desired,out var obstacle)&&obstacle.collider!=motor)desired=obstacle.point+(collisionFocus-obstacle.point).normalized*.3f;cam.transform.position=Vector3.Lerp(cam.transform.position,desired,1-Mathf.Exp(-dt*9));cam.transform.LookAt(focus);}
    }
    void HandleEscape(){if(uiCoopOpen){uiCoopOpen=false;return;}if(uiCredits){uiCredits=false;return;}if(uiNewConfirm){uiNewConfirm=false;return;}if(HandleUiEscape())return;if(phase==Phase.Play)Pause();else if(phase==Phase.Pause||phase==Phase.Shop||phase==Phase.Inventory||phase==Phase.Map||phase==Phase.Dialogue||phase==Phase.Journal||phase==Phase.Character)phase=Phase.Play;}
    void Move(Vector2 input,float dt){
        burstCD=Mathf.Max(0,burstCD-dt);dodgeCD=Mathf.Max(0,dodgeCD-dt);strikeCD=Mathf.Max(0,strikeCD-dt);swing=Mathf.Max(0,swing-dt);invulnerable=Mathf.Max(0,invulnerable-dt);dashTime=Mathf.Max(0,dashTime-dt);input=Vector2.ClampMagnitude(input,1);smoothMovement=Vector2.Lerp(smoothMovement,input,1-Mathf.Exp(-dt/(input.sqrMagnitude>.01f?.07f:.10f)));Vector3 direction=MovementDirection(smoothMovement);if(direction.sqrMagnitude>.01f&&swing<=0)hero.rotation=Quaternion.Slerp(hero.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-dt*18));if(dashTime>0)direction=hero.forward;
        TickJumpBuffer(dt);if(motor.isGrounded&&gravityVelocity<0)gravityVelocity=-2;gravityVelocity-=18*dt;if(jumpVelocity>0){gravityVelocity=jumpVelocity;jumpVelocity=0;}motor.Move((direction*(dashTime>0?19:sprinting?8.6f:6.3f)+Vector3.up*gravityVelocity)*dt);var p=hero.position;float radius=new Vector2(p.x,p.z).magnitude;if(radius>WorldRadius){motor.enabled=false;hero.position=V(p.x*WorldRadius/radius,Mathf.Max(p.y,.05f),p.z*WorldRadius/radius);motor.enabled=true;}if(hero.position.y< -5)RecoverFromFall();
        if(motor.isGrounded&&!wasGrounded){landingTime=.24f;if(swing<=0)rig.Trigger("Land");}wasGrounded=motor.isGrounded;landingTime=Mathf.Max(0,landingTime-dt);
        rig.SetLocomotionSpeed(smoothMovement.magnitude*(sprinting?8.6f:6.3f));if(aiming&&swing<=0)hero.rotation=Quaternion.Euler(0,yaw+180,0);
        if(swing<=0){string state=dashTime>0?"Dodge":hitFlash>0?"Hurt":!motor.isGrounded?"Jump":landingTime>0?"Land":smoothMovement.magnitude>.08f?(equippedWeapon>0?"GunRun":sprinting?"Sprint":"Run"):(equippedWeapon>0?"GunIdle":"Idle");rig.Play(state);}
        if(!CoopIsGuest&&count==3&&!boss.active&&!boss.dead&&(FlatDistance(hero.position,boss.spawn)<16||CompanionNear(boss.spawn,16))){boss.active=true;boss.root.gameObject.SetActive(true);Notify("The Rift Warden awakens. Keep moving when a red circle appears.");PulseAt(boss.spawn,"#9ce0de",7,1);}
    }
    void V02Strike(){
        if(phase!=Phase.Play||strikeCD>0||reloadTime>0)return;var w=weapons[equippedWeapon];if(equippedWeapon>0&&ammo[equippedWeapon]<=0){Reload();return;}strikeCD=w.cooldown;swing=equippedWeapon==0?.43f:.29f;combo=(combo+1)%3;
        Foe target=null;float best=w.range;foreach(var e in foes)if(e.active&&!e.dead){Vector3 toward=e.root.position-hero.position;toward.y=0;float d=toward.magnitude;if(d<best&&(equippedWeapon==0||Vector3.Dot(hero.forward,toward.normalized)>.05f)){best=d;target=e;}}
        if(target!=null){var dir=target.root.position-hero.position;dir.y=0;if(dir.sqrMagnitude>.01f)hero.rotation=Quaternion.LookRotation(dir);}
        if(equippedWeapon==0){rig.Trigger("Sword");foreach(var e in foes)if(e.active&&!e.dead&&FlatDistance(hero.position,e.root.position)<(e.boss?3.8f:w.range))Hit(e,w.damage+(combo==2?12:0));PulseAt(hero.position+hero.forward,"#cff8ef",2,.22f);Tone(340+combo*90,.12f);}
        else {ammo[equippedWeapon]--;rig.Trigger("Shoot");shotFlash=.1f;var start=hero.position+Vector3.up*1.35f+hero.forward*.4f;Vector3 end=target!=null?target.root.position+Vector3.up*(target.boss?2:1):start+hero.forward*w.range;bool blocked=Physics.Linecast(start,end,out var hit)&&hit.collider!=motor;if(blocked)end=hit.point;else if(target!=null)Hit(target,w.damage);var line=G("Aether shot").gameObject.AddComponent<LineRenderer>();line.positionCount=2;line.SetPosition(0,start);line.SetPosition(1,end);line.startWidth=.045f;line.endWidth=.016f;line.sharedMaterial=M("#b6f4ed");Destroy(line.gameObject,.12f);Sparks(end,"#b3eae9",5);Tone(equippedWeapon==1?280:210,.1f);}
    }
    void Burst(){if(CoopWorldPaused)return;if(CoopIsGuest){if(phase==Phase.Play&&burstCD<=0){QueueCoopCommand("burst");burstCD=8;swing=.6f;rig.Trigger("Sword");PulseAt(hero.position,"#a9e9f3",7,.7f);PlaySfx("burst");}return;}if(phase!=Phase.Play||burstCD>0||uiCredits)return;burstCD=8;invulnerable=.65f;swing=.6f;rig.Trigger("Sword");PulseAt(hero.position,"#a9e9f3",7,.7f);Sparks(hero.position+Vector3.up,"#c7edfa",25);foreach(var e in foes)if(e.active&&!e.dead&&FlatDistance(e.root.position,hero.position)<7)Hit(e,85*DamageMultiplier);Notify("Frost nova");Tone(145,.4f);}
    void Collect(){Interact();}
    void Interact(){if(phase!=Phase.Play||CoopWorldPaused&&!coopExecuting)return;if(CoopIsGuest){QueueCoopCommand("interact");return;}if(InteractWorldV6())return;foreach(var s in sigils)if(!s.taken&&FlatDistance(hero.position,s.root.position)<2.5f){s.taken=true;s.root.gameObject.SetActive(false);count++;hp=Mathf.Min(MaxHealth,hp+20);AddCoins(30);PulseAt(s.root.position,"#f9dfa7",4,.7f);Sparks(s.root.position+Vector3.up,"#ffe5ab",20);Tone(880,.4f);Notify(count==3?"All sigils restored. The northern rift is open.":"Sigil recovered  ·  "+count+" / 3  ·  +30 coins  ·  +20 HP");SaveJourney(false);return;}if(InteractProgression())return;if(InteractWithWorld()){SaveJourney(false);return;}if(FlatDistance(hero.position,merchantPosition)<6)OpenShop();else Notify("Move closer to a sigil, chest, or the village trader.");}
    void V02Hit(Foe e,float damage){if(e.dead||!e.active)return;e.hp=Mathf.Max(0,e.hp-damage);Sparks(e.root.position+Vector3.up,e.boss?"#b3eeed":"#efca99",8);if(e.hp<=0){e.dead=true;e.root.gameObject.SetActive(false);if(e.warning)Destroy(e.warning);kills++;hp=Mathf.Min(100,hp+10);AddCoins(e.boss?150:15);if(e.boss){phase=Phase.Win;rig.Play("Idle");Notify("Aetherfall is safe  ·  +150 coins");}else Notify("Riftling defeated  ·  +15 coins  ·  +10 HP");SaveJourney(false);}}
    void Hurt(float damage){if(invulnerable>0||phase!=Phase.Play)return;hp=Mathf.Max(0,hp-damage);invulnerable=.7f;hitFlash=.35f;Tone(75,.25f);if(hp<=0){phase=Phase.Defeat;rig.Trigger("Death");}else rig.Trigger("Hurt");}
    void Sparks(Vector3 position,string color,int amount){for(int i=0;i<amount;i++){var t=Box(color,null,position,Vector3.one*.08f);sparks.Add(new Spark{root=t,life=UnityEngine.Random.Range(.35f,.75f),velocity=UnityEngine.Random.insideUnitSphere*3+Vector3.up*2});}}
    void PulseAt(Vector3 at,string color,float radius,float duration){if(!ringMesh){ringMesh=new Mesh();var v=new Vector3[128];var t=new int[384];for(int i=0;i<64;i++){float a=i*Mathf.PI/32;v[i*2]=V(Mathf.Cos(a)*.88f,0,Mathf.Sin(a)*.88f);v[i*2+1]=V(Mathf.Cos(a),0,Mathf.Sin(a));int n=(i+1)%64;int k=i*6;t[k]=i*2;t[k+1]=n*2+1;t[k+2]=i*2+1;t[k+3]=i*2;t[k+4]=n*2;t[k+5]=n*2+1;}ringMesh.vertices=v;ringMesh.triangles=t;ringMesh.RecalculateNormals();}at.y=GroundHeight(at)+.12f;var root=G("Expanding aether ring",null,at);root.gameObject.AddComponent<MeshFilter>().sharedMesh=ringMesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(color);pulses.Add(new Pulse{root=root,life=duration,max=duration,radius=radius});}
    [Serializable]class JourneyV2 {
        public int version=3,mask,deadMask,kills,coins,owned=1,equipped,potions=1,chests;
        public bool bossDefeated;public float x,z,hp,time;public string coinState,enemyState;public int[] ammo;public ProgressionSave progression;public WorldV6Save worldV6;
    }
    string LegacyJourneyPath {get{return Path.Combine(Path.GetDirectoryName(savePath),"journey-v2.json");}}
    bool HasJourneySave(){return File.Exists(savePath)||File.Exists(savePath+".bak")||File.Exists(LegacyJourneyPath);}
    JourneyV2 CurrentJourney(){
        int mask=0;for(int i=0;i<sigils.Count;i++)if(sigils[i].taken)mask|=1<<i;
        var states=new System.Text.StringBuilder(foes.Count);foreach(var e in foes)states.Append(e.dead?'1':'0');
        return new JourneyV2{mask=mask,kills=kills,coins=coins,owned=ownedWeapons,equipped=equippedWeapon,potions=potions,chests=chestMask,bossDefeated=boss.dead,x=hero.position.x,z=hero.position.z,hp=hp,time=elapsed,coinState=CoinState(),enemyState=states.ToString(),ammo=(int[])ammo.Clone(),progression=GetProgressionSave(),worldV6=GetWorldV6Save()};
    }
    bool ApplyJourney(JourneyV2 d){
        if(d==null||(d.version!=2&&d.version!=3)||!Finite(d.x)||!Finite(d.z)||!Finite(d.hp)||!Finite(d.time)||d.hp<=0||new Vector2(d.x,d.z).magnitude>WorldRadius+1)return false;
        ApplyWorldV6Save(d.worldV6);if(d.progression!=null)ApplyProgressionSave(d.progression);else ResetProgression();
        coins=Mathf.Clamp(d.coins,0,999999);ownedWeapons=(d.owned&7)|1;equippedWeapon=Mathf.Clamp(d.equipped,0,2);if((ownedWeapons&(1<<equippedWeapon))==0)equippedWeapon=0;
        potions=Mathf.Clamp(d.potions,0,9);chestMask=d.chests&((1<<worldChests.Count)-1);hp=Mathf.Clamp(d.hp,1,MaxHealth);kills=Mathf.Clamp(d.kills,0,foes.Count);elapsed=Mathf.Max(0,d.time);count=0;
        for(int i=0;i<3;i++){sigils[i].taken=(d.mask&(1<<i))!=0;sigils[i].root.gameObject.SetActive(!sigils[i].taken);if(sigils[i].taken)count++;}
        for(int i=0;i<foes.Count;i++){var e=foes[i];e.dead=d.version==2?(i<31&&(d.deadMask&(1<<i))!=0):(!string.IsNullOrEmpty(d.enemyState)&&i<d.enemyState.Length&&d.enemyState[i]=='1');e.hp=e.max;e.active=!e.boss;e.wind=0;e.root.position=e.spawn;e.root.gameObject.SetActive(!e.dead&&!e.boss);}
        boss.dead=d.bossDefeated;boss.active=false;boss.root.gameObject.SetActive(false);ResetExploration();ApplyCoinState(d.coinState);
        if(d.ammo!=null&&d.ammo.Length==3)for(int i=1;i<3;i++)ammo[i]=Mathf.Clamp(d.ammo[i],0,weapons[i].magazine);
        var position=V(d.x,0,d.z);position.y=GroundHeight(position)+.17f;motor.enabled=false;hero.position=position;motor.enabled=true;Physics.SyncTransforms();return true;
    }
    static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
    void SaveJourney(bool notify){
        if(smoke||inputCheck||coopGuestSession||coopExecuting||hp<=0||phase==Phase.Title)return;
        try{
            Directory.CreateDirectory(Application.persistentDataPath);string temp=savePath+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(CurrentJourney()));
            if(File.Exists(savePath)){
#if UNITY_WEBGL && !UNITY_EDITOR
                // IDBFS does not implement File.Replace. Keep the known-good backup
                // while replacing a corrupt primary that was recovered on Continue.
                if(!journeyPreserveBackup)File.Copy(savePath,savePath+".bak",true);
                File.Delete(savePath);File.Move(temp,savePath);
#else
                File.Replace(temp,savePath,journeyPreserveBackup?null:savePath+".bak");
#endif
            }else File.Move(temp,savePath);
            journeyPreserveBackup=false;RequestPlatformSaveSync();if(notify)Notify("Journey saved on this device.");
        }catch(Exception ex){Debug.LogWarning("Save failed: "+ex.Message);if(notify)Notify("Could not save. Check storage permissions.");}
    }
    bool TryRestoreJourney(string primary,string legacy,out bool recoveredBackup){
        recoveredBackup=false;
        string[] candidates={primary,primary+".bak",legacy};
        for(int i=0;i<candidates.Length;i++){
            string path=candidates[i];if(string.IsNullOrEmpty(path)||!File.Exists(path))continue;
            try{if(ApplyJourney(JsonUtility.FromJson<JourneyV2>(File.ReadAllText(path)))){recoveredBackup=i==1;return true;}}
            catch(Exception ex){Debug.LogWarning("Save candidate could not be loaded: "+ex.GetType().Name);}
        }
        return false;
    }
    bool LoadJourney(){return TryRestoreJourney(savePath,LegacyJourneyPath,out journeyPreserveBackup);}
    void RecoverFromFall(){
        motor.enabled=false;hero.position=WorldV6RespawnPoint();motor.enabled=true;Physics.SyncTransforms();
        gravityVelocity=jumpVelocity=dashTime=swing=0;ResetMovementPolish();ResetControlInput();hp=Mathf.Max(1,hp-10);invulnerable=Mathf.Max(invulnerable,1);
        Notify("Returned to your waystone / -10 HP");
    }
    IEnumerator V02SmokeTest(){yield return null;string result;int code=0;try{
        StartGame();Require(rig.GetComponentsInChildren<SkinnedMeshRenderer>().Length==23,"Imported character meshes");Require(rig.animationPlayer.GetClipCount()==10,"Animation clips");foreach(var r in rig.GetComponentsInChildren<SkinnedMeshRenderer>())foreach(var w in r.sharedMesh.boneWeights)Require(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.001f,"Normalized skin weights");
        Require(foes.Count>=20&&sigils.Count==3,"Expanded world actors");Vector3 before=hero.position;Move(new Vector2(0,-1),.1f);Require(hero.position.z<before.z,"Forward movement");BuyWeapon(1);Require(equippedWeapon==0&&coins==0,"Insufficient coins gate");AddCoins(240);BuyWeapon(1);Require(coins==180&&equippedWeapon==1,"Sidearm purchase");BuyWeapon(1);Require(coins==180,"Owned weapon never double-charged");BuyWeapon(2);Require(coins==0&&equippedWeapon==2,"Rifle purchase");
        var foe=foes[0];foe.root.position=hero.position+hero.forward*5;foe.hp=65;strikeCD=0;Strike();Require(foe.hp<65&&ammo[2]==17,"Ranged hit and ammo");ammo[2]=0;strikeCD=0;Strike();Require(reloadTime>0,"Empty magazine reload");UpdateWeaponTimers(2);Require(ammo[2]==18&&reloadTime==0,"Reload completion");
        hp=60;potions=1;UsePotion();Require(hp==100&&potions==0,"Healing flask");UsePotion();Require(potions==0,"No negative potion count");AddCoins(25);BuyPotion();Require(coins==0&&potions==1,"Potion purchase");
        foreach(var s in sigils){motor.enabled=false;hero.position=s.root.position+Vector3.up*.2f;motor.enabled=true;Interact();}Require(count==3&&coins==90,"Sigil rewards");int oldCoins=coins;Interact();Require(coins==oldCoins,"No repeated sigil reward");
        motor.enabled=false;hero.position=worldCoins[0].spawn;motor.enabled=true;UpdateExploration(0);Require(coins==93&&worldCoins[0].taken,"Trail coin collection");UpdateExploration(0);Require(coins==93,"Coin cannot be collected twice");motor.enabled=false;hero.position=worldChests[0].root.position+V(0,0,-2);motor.enabled=true;Interact();Require(coins==143&&chestMask==1&&potions==2,"Chest rewards");Interact();Require(coins==143,"Chest cannot pay twice");
        var data=JsonUtility.FromJson<JourneyV2>(JsonUtility.ToJson(CurrentJourney()));coins=0;Require(ApplyJourney(data)&&coins==143&&equippedWeapon==2&&chestMask==1&&worldCoins[0].taken,"Save round-trip");data.x=float.NaN;Require(!ApplyJourney(data),"Corrupt save rejected");
        motor.enabled=false;hero.position=boss.spawn+V(0,.2f,3);motor.enabled=true;Move(Vector2.zero,.1f);Require(boss.active,"Boss activation");boss.hp=80;burstCD=0;Burst();Require(boss.dead&&phase==Phase.Win,"Victory");Require(coins==293,"Boss reward");StartGame();Require(count==0&&coins==0&&ownedWeapons==1&&!boss.active&&chestMask==0&&!worldCoins[0].taken,"New-game reset");OpenShop();Require(phase==Phase.Play,"Remote shop blocked");motor.enabled=false;hero.position=merchantPosition+V(0,0,-3);motor.enabled=true;OpenShop();Require(phase==Phase.Shop,"Village shop opens");TogglePanel(Phase.Shop);Require(phase==Phase.Play,"Shop closes");TogglePanel(Phase.Map);Require(phase==Phase.Map,"Map opens");TogglePanel(Phase.Map);Require(phase==Phase.Play,"Map closes");phase=Phase.Pause;uiCredits=true;HandleEscape();Require(!uiCredits&&phase==Phase.Pause,"Credits close without resuming");HandleEscape();Require(phase==Phase.Play,"Escape resumes pause");invulnerable=0;Hurt(100);Require(phase==Phase.Defeat,"Defeat");result="PASS v0.2: imported skinned Eula; normalized weights; animation clips; expanded world; movement; shop affordability and duplicate purchase; weapon equipment; ranged damage/ammo/reload; healing; sigil/coin/chest rewards and duplicate prevention; shop proximity; panel transitions; save round-trip and corruption rejection; boss gating/victory/reward; reset; defeat.";
    }catch(Exception ex){result="FAIL v0.2: "+ex;code=1;}Debug.Log("TEJA_SMOKE "+result);var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-tejaTestResult");if(i>=0&&i+1<args.Length)File.WriteAllText(args[i+1],result);Application.Quit(code);}
    IEnumerator CapturePreview(){
        var args=Environment.GetCommandLineArgs();int ix=Array.IndexOf(args,"-tejaPreviewDir");if(ix<0||ix+1>=args.Length){Application.Quit(1);yield break;}string dir=args[ix+1];Directory.CreateDirectory(dir);
        yield return new WaitForSecondsRealtime(1);RenderPreview(Path.Combine(dir,"eula-title-scene.png"));RequestUiPreview(Path.Combine(dir,"title-interface.png"));yield return new WaitForSecondsRealtime(.4f);
        StartGame();yield return new WaitForSecondsRealtime(1);RenderPreview(Path.Combine(dir,"expanded-gameplay.png"));RequestUiPreview(Path.Combine(dir,"gameplay-interface.png"));yield return new WaitForSecondsRealtime(.4f);
        phase=Phase.Pause;previewCameraOverride=true;cam.transform.position=hero.position+V(0,1.5f,-3.8f);cam.transform.LookAt(hero.position+Vector3.up*1.1f);rig.Play("Idle");yield return new WaitForSecondsRealtime(.3f);RenderPreview(Path.Combine(dir,"eula-front.png"));
        ownedWeapons=7;EquipWeapon(2);rig.Play("GunIdle");yield return new WaitForSecondsRealtime(.3f);cam.transform.position=hero.position+V(-3,1.7f,-2.8f);cam.transform.LookAt(hero.position+Vector3.up*1.15f);RenderPreview(Path.Combine(dir,"eula-rifle.png"));
        EquipWeapon(0);yield return null;foreach(string clip in new[]{"Run","Sword","Jump","Dodge"}){rig.animationPlayer.Stop();rig.animationPlayer.GetClip(clip).SampleAnimation(rig.gameObject,TejaRig.Duration(clip)*.3f);yield return null;RenderPreview(Path.Combine(dir,"pose-"+clip.ToLower()+".png"));}
        cam.transform.position=V(20,15,51);cam.transform.LookAt(V(1,1,35));RenderPreview(Path.Combine(dir,"lantern-village.png"));
        phase=Phase.Shop;ownedWeapons=1;equippedWeapon=0;coins=240;noticeTime=0;yield return new WaitForSecondsRealtime(.3f);RequestUiPreview(Path.Combine(dir,"shop-interface.png"));yield return new WaitForSecondsRealtime(.4f);phase=Phase.Map;yield return new WaitForSecondsRealtime(.3f);RequestUiPreview(Path.Combine(dir,"map-interface.png"));yield return new WaitForSecondsRealtime(.4f);
        Debug.Log("TEJA_PREVIEW_OK v0.2");Application.Quit(0);
    }
    RenderTexture uiPreviewTarget;string uiPreviewPath;
    void RequestUiPreview(string path){if(uiPreviewTarget){uiPreviewTarget.Release();Destroy(uiPreviewTarget);}uiPreviewPath=path;uiPreviewTarget=new RenderTexture(Screen.width,Screen.height,24);cam.targetTexture=uiPreviewTarget;cam.Render();cam.targetTexture=null;}
    void FinishUiPreview(){var tex=new Texture2D(uiPreviewTarget.width,uiPreviewTarget.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0);tex.Apply();File.WriteAllBytes(uiPreviewPath,tex.EncodeToPNG());Destroy(tex);RenderTexture.active=null;uiPreviewTarget.Release();Destroy(uiPreviewTarget);uiPreviewTarget=null;}
}
