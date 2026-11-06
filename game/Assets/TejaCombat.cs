using UnityEngine;
using System;
using System.Collections.Generic;

public partial class TejaAdventure {
    bool aiming,sprinting,mobileSprint;
    Foe lockTarget;
    int graphicsPreset;
    float comboWindow,impactShake;
    class FloatingHit {public Vector3 world;public string text;public float life;public bool critical;}
    readonly List<FloatingHit> floatingHits=new List<FloatingHit>();
    class EnemyBolt {public Transform root;public Vector3 velocity;public float life,damage;}
    readonly List<EnemyBolt> enemyBolts=new List<EnemyBolt>();
    class Shockwave {public Transform root;public Vector3 origin;public float radius,life,damage;public bool hit,hitRemote;}
    readonly List<Shockwave> shockwaves=new List<Shockwave>();
    readonly RaycastHit[] combatSceneHits=new RaycastHit[64];

    void ApplyGraphicsPreset(int preset){
        graphicsPreset=Mathf.Clamp(preset,0,2);highQuality=graphicsPreset>0;
        Application.targetFrameRate=graphicsPreset==0?30:60;QualitySettings.vSyncCount=0;
        QualitySettings.antiAliasing=graphicsPreset==2?4:graphicsPreset==1?2:0;
        QualitySettings.shadows=graphicsPreset==0?ShadowQuality.Disable:graphicsPreset==1?ShadowQuality.HardOnly:ShadowQuality.All;
        QualitySettings.shadowResolution=graphicsPreset==2?ShadowResolution.Medium:ShadowResolution.Low;
        QualitySettings.shadowDistance=graphicsPreset==2?42:24;QualitySettings.pixelLightCount=1;
        if(sun){sun.shadows=graphicsPreset==2?LightShadows.Soft:LightShadows.Hard;sun.shadowStrength=.35f;}
        if(!smoke&&!inputCheck)PlayerPrefs.SetInt("TejaGraphics",graphicsPreset);
    }
    void ResetCombat(){
        aiming=sprinting=mobileSprint=false;lockTarget=null;combo=0;comboWindow=0;floatingHits.Clear();
        foreach(var b in enemyBolts)if(b.root)Destroy(b.root.gameObject);enemyBolts.Clear();
        foreach(var w in shockwaves)if(w.root)Destroy(w.root.gameObject);shockwaves.Clear();
    }
    void ToggleLockOn(){
        if(phase!=Phase.Play)return;if(lockTarget!=null){lockTarget=null;return;}
        float best=32;foreach(var e in foes)if(e.active&&!e.dead){float d=FlatDistance(hero.position,e.root.position);if(d<best){best=d;lockTarget=e;}}
        Notify(lockTarget!=null?"Locked on: "+lockTarget.title:"No enemies nearby.");
    }
    Foe MakeFoe(Vector3 position,bool isBoss){return MakeEnemy(position,isBoss?FoeKind.Warden:(FoeKind)(foes.Count%4));}
    Foe MakeEnemy(Vector3 position,FoeKind kind){
        bool isBoss=kind==FoeKind.Warden;var root=G("Enemy / "+kind,null,position);var bodyRoot=G("Animated enemy",root,V(0,1,0));
        var e=new Foe{root=root,body=bodyRoot,spawn=position,kind=kind,boss=isBoss,active=!isBoss,cooldown=1};
        switch(kind){
            case FoeKind.Riftling:
                e.title="Rift Prowler";e.max=75;e.speed=2.5f;e.damage=12;e.reward=15;
                Ball("#3d727c",bodyRoot,V(0,-.2f,0),V(1.15f,1.3f,1));Ball("#629b9b",bodyRoot,V(0,.28f,.12f),V(.82f,.65f,.75f));
                foreach(int s in new[]{-1,1}){var horn=Box("#193847",bodyRoot,V(s*.36f,.58f,.05f),V(.16f,.6f,.21f));horn.localRotation=Quaternion.Euler(0,0,-s*23);Ball("#254c58",root,V(s*.36f,.2f,.1f),V(.4f,.4f,.65f));Box("#f8d68c",bodyRoot,V(s*.19f,.17f,.48f),V(.16f,.075f,.035f));}break;
            case FoeKind.Sentinel:
                e.title="Ruin Sentinel";e.max=110;e.speed=1.45f;e.damage=15;e.reward=22;
                Box("#b1bda8",bodyRoot,V(0,.25f,0),V(.9f,1.35f,.68f));Box("#526f77",bodyRoot,V(0,1.05f,0),V(.68f,.55f,.65f));Box("#6be5e5",bodyRoot,V(0,.35f,.37f),V(.18f,.6f,.05f));
                foreach(int s in new[]{-1,1}){Box("#64848a",root,V(s*.25f,.45f,0),V(.34f,.9f,.4f));var arm=Box("#a5b6a7",bodyRoot,V(s*.7f,.4f,.13f),V(.4f,.65f,.55f));arm.localRotation=Quaternion.Euler(0,0,s*17);Ball("#79d3d1",bodyRoot,V(s*.7f,.2f,.48f),V(.22f,.22f,.16f));}break;
            case FoeKind.Wisp:
                e.title="Storm Wisp";e.max=58;e.speed=2.9f;e.damage=10;e.reward=18;
                Ball("#8c77be",bodyRoot,V(0,.8f,0),V(.78f,.78f,.78f));var core=Box("#c9d8fa",bodyRoot,V(0,.8f,.04f),V(.58f,.58f,.58f));core.localRotation=Quaternion.Euler(25,45,25);
                for(int i=0;i<4;i++){var wing=Box("#6c91c4",bodyRoot,V(Mathf.Cos(i*Mathf.PI/2)*.65f,.8f,Mathf.Sin(i*Mathf.PI/2)*.65f),V(.13f,.7f,.26f));wing.localRotation=Quaternion.Euler(0,-i*90,45);}break;
            case FoeKind.Brute:
                e.title="Thornback Brute";e.max=185;e.speed=1.9f;e.damage=23;e.reward=30;
                Ball("#586858",bodyRoot,V(0,.3f,0),V(1.8f,1.9f,1.4f));Ball("#abb18c",bodyRoot,V(0,1.13f,.4f),V(.95f,.8f,.85f));
                foreach(int s in new[]{-1,1}){Ball("#414f4d",root,V(s*.5f,.35f,0),V(.65f,.7f,.8f));Ball("#738568",bodyRoot,V(s*1.05f,.1f,0),V(.6f,1.3f,.7f));for(int k=0;k<3;k++){var spike=Box("#d2c49b",bodyRoot,V(s*(.55f+k*.17f),.9f-k*.2f,-.5f),V(.21f,.7f,.21f));spike.localRotation=Quaternion.Euler(-35,0,-s*25);}}break;
            default:
                e.title="The Rift Warden";e.max=920;e.speed=2.3f;e.damage=26;e.reward=200;
                Ball("#273d57",bodyRoot,V(0,1,0),V(2.35f,2.8f,1.5f));Box("#a1f2ee",bodyRoot,V(0,1.1f,.8f),V(.52f,1.0f,.1f));Box("#577d91",bodyRoot,V(0,2.3f,0),V(1.05f,.9f,.9f));Box("#c2f2ff",bodyRoot,V(0,2.4f,.48f),V(.7f,.07f,.05f));
                foreach(int s in new[]{-1,1}){Box("#435f78",root,V(s*.6f,.65f,0),V(.65f,1.3f,.75f));Ball("#648c9c",bodyRoot,V(s*1.35f,1.55f,0),V(1.05f,1.05f,1.05f));Box("#354e6c",bodyRoot,V(s*1.55f,.45f,0),V(.6f,1.8f,.65f));for(int j=0;j<3;j++){var crystal=Box("#9ee1dd",bodyRoot,V(s*(.36f+j*.17f),2.95f-j*.1f,-.1f),V(.16f,.65f,.18f));crystal.localRotation=Quaternion.Euler(-12,0,-s*(20+j*13));}}break;
        }
        e.hp=e.max;foes.Add(e);PrepareEnemyMotion(e);root.gameObject.SetActive(!isBoss);return e;
    }
    int BossStage(Foe e){return e.hp/e.max>.66f?1:e.hp/e.max>.33f?2:3;}
    float EnemyTargetHeight(Foe e){return e.boss?2.2f:e.kind==FoeKind.Wisp?1.8f:1.1f;}
    void UpdateFoes(float dt){
        foreach(var e in foes){
            if(phase!=Phase.Play)break;if(!e.active||e.dead)continue;
            Vector3 delta=EnemyFocus(e)-e.root.position;delta.y=0;float distance=delta.magnitude;
            if(distance>70){if(e.root.gameObject.activeSelf)e.root.gameObject.SetActive(false);continue;}if(!e.root.gameObject.activeSelf)e.root.gameObject.SetActive(true);e.stagger=Mathf.Max(0,e.stagger-dt);
            if(delta.sqrMagnitude>.01f)e.root.rotation=Quaternion.Slerp(e.root.rotation,Quaternion.LookRotation(delta),dt*5);
            float bob=e.kind==FoeKind.Wisp?.22f:.055f;e.body.localPosition=V(0,1+Mathf.Sin(Time.time*(e.kind==FoeKind.Wisp?2:4)+e.spawn.x)*bob,0);
            if(e.stagger>0)continue;
            if(e.charge>0){e.charge-=dt;StepEnemy(e,e.target-e.root.position,18*dt);if(FlatDistance(hero.position,e.root.position)<1.8f)Hurt(e.damage);if(CompanionNear(e.root.position,1.8f))HurtCompanion(e.damage);if(e.charge<=0){PulseAt(e.root.position,"#f1a38c",2.6f,.4f);Sparks(e.root.position+Vector3.up*.25f,"#ffcda0",8);}continue;}
            if(e.wind>0){
                e.wind-=dt;if(e.warning){float progress=1-Mathf.Clamp01(e.wind/e.windStart);var renderer=e.warning.GetComponent<Renderer>();if(renderer)renderer.sharedMaterial=M(progress>.7f?"#ffad84":"#d8757c");}
                if(e.wind<=0){ResolveEnemyAttack(e);if(e.warning)Destroy(e.warning);e.cooldown=e.boss?Mathf.Lerp(2.5f,1.4f,(BossStage(e)-1)/2f):e.kind==FoeKind.Wisp?2.1f:2.6f;}continue;
            }
            e.cooldown-=dt;float aggro=e.boss?50:e.kind==FoeKind.Wisp||e.kind==FoeKind.Sentinel?20:14;
            if(distance>aggro){if(!e.boss)PatrolEnemy(e,dt);continue;}
            bool ranged=e.kind==FoeKind.Wisp||e.kind==FoeKind.Sentinel;
            if(ranged){if(distance>12)StepEnemy(e,delta.normalized,e.speed*dt);else if(distance<6)StepEnemy(e,-delta.normalized,e.speed*.6f*dt);else if(e.kind==FoeKind.Wisp)StepEnemy(e,Vector3.Cross(delta.normalized,Vector3.up)*Mathf.Sin(elapsed*.5f+e.spawn.x),e.speed*.45f*dt);}
            else if(distance>(e.boss?4:2))StepEnemy(e,delta.normalized,e.speed*(e.boss?1+.18f*(BossStage(e)-1):1)*dt);
            if(e.cooldown<=0&&distance<(e.boss?26:ranged?22:e.kind==FoeKind.Brute?8:2.8f))BeginEnemyAttack(e);
        }
    }
    void StepEnemy(Foe e,Vector3 direction,float distance){
        direction.y=0;if(direction.sqrMagnitude<.001f)return;direction.Normalize();
        if(Physics.Raycast(e.root.position+Vector3.up*.8f,direction,distance+.55f)){direction=Quaternion.Euler(0,70,0)*direction;if(Physics.Raycast(e.root.position+Vector3.up*.8f,direction,distance+.55f))return;}
        Vector3 p=e.root.position+direction*distance;if(new Vector2(p.x,p.z).magnitude>WorldRadius-1)return;p.y=GroundHeight(p);e.root.position=p;
    }
    void BeginEnemyAttack(Foe e){
        int stage=e.boss?BossStage(e):1;e.attackStyle=e.boss?(e.attackCycle++%(stage==1?2:3)):e.kind==FoeKind.Sentinel||e.kind==FoeKind.Wisp?1:e.kind==FoeKind.Brute?2:0;
        e.target=EnemyFocus(e);e.target.y=GroundHeight(e.target)+.07f;e.wind=e.boss?1.25f:e.attackStyle==2?1.05f:.8f;e.windStart=e.wind;
        float radius=e.boss?3.7f:e.kind==FoeKind.Brute?2.6f:1.6f;
        if(e.attackStyle!=1){e.warning=Cylinder("#d8757c",null,e.target,V(radius*2,.015f,radius*2)).gameObject;}
        else {PulseAt(e.root.position,"#d7a0e4",1,.4f);}
        PlaySfxAt("warning",e.root.position,e.boss?1:.45f);
        if(e.boss)Notify("Warden / "+(e.attackStyle==0?"Rift slam — leave the circle":e.attackStyle==1?"Aether volley — dodge the bolts":"Shockwave — jump over the ring"));
    }
    void ResolveEnemyAttack(Foe e){
        if(e.attackStyle==1){int amount=e.boss?3+2*(BossStage(e)-1):e.kind==FoeKind.Wisp?3:1;for(int i=0;i<amount;i++){var start=e.root.position+Vector3.up*EnemyTargetHeight(e);var direction=(EnemyFocus(e)+Vector3.up*.9f-start).normalized;direction=Quaternion.Euler(0,(i-(amount-1)*.5f)*9,0)*direction;var t=Ball(e.boss?"#93dce9":"#d1a0e0",null,start,Vector3.one*.23f);enemyBolts.Add(new EnemyBolt{root=t,velocity=direction*(e.boss?9:7),life=4,damage=e.damage});}return;}
        if(e.attackStyle==2&&e.boss){var ring=G("Warden shockwave",null,e.root.position+Vector3.up*.1f);PulseAt(e.root.position,"#eeb090",.1f,.01f);ring.gameObject.AddComponent<MeshFilter>().sharedMesh=ringMesh;ring.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M("#ffc39e");shockwaves.Add(new Shockwave{root=ring,origin=e.root.position,radius=.5f,life=3,damage=e.damage});return;}
        if(e.attackStyle==2&&!e.boss){e.charge=.34f;return;}
        float radius=e.boss?3.7f:e.kind==FoeKind.Brute?2.6f:1.6f;
        if(FlatDistance(hero.position,e.target)<radius&&hero.position.y-GroundHeight(hero.position)<.9f)Hurt(e.damage);
        if(CompanionNear(e.target,radius)&&coopRemote.position.y-GroundHeight(coopRemote.position)<.9f)HurtCompanion(e.damage);
        PulseAt(e.target,"#f1a38c",radius,.4f);Sparks(e.target+Vector3.up*.25f,"#ffcda0",e.boss?14:6);
    }
    void TickCombatPresentation(float dt){
        comboWindow=Mathf.Max(0,comboWindow-dt);impactShake=Mathf.Max(0,impactShake-dt);
        for(int i=floatingHits.Count-1;i>=0;i--){var f=floatingHits[i];f.life-=dt;f.world+=Vector3.up*.65f*dt;if(f.life<=0)floatingHits.RemoveAt(i);}
    }
    void TickCombat(float dt){
        TickCombatPresentation(dt);
        if(CoopIsGuest)return;
        for(int i=enemyBolts.Count-1;i>=0;i--){
            var b=enemyBolts[i];var previous=b.root.position;var next=previous+b.velocity*dt;b.life-=dt;
            Vector3 travel=next-previous;bool blocked=ShotSceneHit(new Ray(previous,travel.normalized),travel.magnitude,out var scene);
            if(blocked)next=scene.point;
            float localFraction,remoteFraction=2;
            float localDistance=CombatSegmentDistance(previous,next,hero.position+Vector3.up*.9f,out localFraction);
            bool touching=hp>0&&localDistance<.65f;
            // Evaluate each actor before deciding which one intercepted the projectile.
            bool touchingRemote=CoopFriendReady&&coopRemoteState.hp>0&&CombatSegmentDistance(previous,next,coopRemote.position+Vector3.up*.9f,out remoteFraction)<.65f;
            if(touchingRemote&&(!touching||remoteFraction<localFraction))HurtCompanion(b.damage);else if(touching)Hurt(b.damage);
            if(blocked||touching||touchingRemote||b.life<=0){Sparks(next,"#e5b4e7",3);Destroy(b.root.gameObject);enemyBolts.RemoveAt(i);}else b.root.position=next;
        }
        for(int i=shockwaves.Count-1;i>=0;i--){
            var w=shockwaves[i];w.life-=dt;float oldRadius=w.radius;w.radius+=7*dt;w.root.localScale=V(w.radius,.025f,w.radius);
            float d=FlatDistance(hero.position,w.origin);
            if(!w.hit&&d>=oldRadius-.7f&&d<=w.radius+.7f&&hero.position.y-GroundHeight(hero.position)<.85f){w.hit=true;Hurt(w.damage);}
            if(CoopFriendReady){float remoteDistance=FlatDistance(coopRemote.position,w.origin);if(!w.hitRemote&&remoteDistance>=oldRadius-.7f&&remoteDistance<=w.radius+.7f&&coopRemote.position.y-GroundHeight(coopRemote.position)<.85f){w.hitRemote=true;HurtCompanion(w.damage);}}
            if(w.life<=0){Destroy(w.root.gameObject);shockwaves.RemoveAt(i);}
        }
    }
    float CombatSegmentDistance(Vector3 start,Vector3 end,Vector3 point,out float fraction){
        Vector3 segment=end-start;fraction=segment.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(point-start,segment)/segment.sqrMagnitude):0;
        return Vector3.Distance(start+segment*fraction,point);
    }
    bool ShotSceneHit(Ray ray,float range,out RaycastHit nearest){
        // Ignore both player controllers: co-op has no friendly fire and a shoulder camera
        // starts behind its own actor. Reuse the query buffer for shots and enemy bolts.
        nearest=default(RaycastHit);float best=range;bool found=false;
        int hits=Physics.RaycastNonAlloc(ray,combatSceneHits,range,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<hits;i++){var hit=combatSceneHits[i];if(hit.collider is CharacterController||hit.distance>best)continue;nearest=hit;best=hit.distance;found=true;}
        return found;
    }
    Foe FindShotTarget(Ray ray,float range,out float distance){
        Foe result=null;distance=range;foreach(var e in foes)if(e.active&&!e.dead){var center=e.root.position+Vector3.up*EnemyTargetHeight(e);float radius=e.boss?1.4f:e.kind==FoeKind.Brute?.95f:.7f;Vector3 offset=ray.origin-center;float b=Vector3.Dot(offset,ray.direction),c=offset.sqrMagnitude-radius*radius,disc=b*b-c;if(disc<0)continue;float t=c<=0?0:-b-Mathf.Sqrt(disc);if(t>=0&&t<distance){distance=t;result=e;}}return result;
    }
    void Strike(){
        if(GuestAttack())return;
        if(phase!=Phase.Play||uiCredits||uiNewConfirm||uiCoopOpen||CoopWorldPaused||strikeCD>0||reloadTime>0)return;var w=weapons[equippedWeapon];if(equippedWeapon>0&&ammo[equippedWeapon]<=0){Reload();return;}
        strikeCD=w.cooldown;swing=equippedWeapon==0?.43f:.29f;
        if(equippedWeapon==0){combo=comboWindow>0?(combo+1)%3:0;comboWindow=1.1f;var target=lockTarget;float best=3.5f;foreach(var e in foes)if(e.active&&!e.dead&&FlatDistance(hero.position,e.root.position)<best){best=FlatDistance(hero.position,e.root.position);target=e;}if(target!=null){var toward=target.root.position-hero.position;toward.y=0;if(toward.sqrMagnitude>.01f)hero.rotation=Quaternion.LookRotation(toward);}
            string clip=combo==0?"Sword":combo==1?"Sword2":"Sword3";swing=TejaRig.Duration(clip);strikeCD=swing*.85f;rig.Trigger(clip);foreach(var e in foes)if(e.active&&!e.dead){var toward=e.root.position-hero.position;toward.y=0;if(toward.magnitude<(e.boss?4:w.range)&&Vector3.Dot(hero.forward,toward.normalized)>-.15f)Hit(e,(w.damage+(combo==2?12:0))*DamageMultiplier);}
            SlashEffect();PlaySfx("sword"+(combo+1));return;
        }
        ammo[equippedWeapon]--;rig.Trigger("Shoot");shotFlash=.1f;Vector3 start=hero.position+Vector3.up*1.4f+hero.forward*.4f;Vector3 end;Foe hitTarget=null;
        Ray ray=CombatAimRay();
        bool locked=!aiming&&lockTarget!=null&&lockTarget.active&&!lockTarget.dead&&FlatDistance(hero.position,lockTarget.root.position)<=w.range;
        float distance;
        if(locked){hitTarget=lockTarget;end=lockTarget.root.position+Vector3.up*EnemyTargetHeight(lockTarget);distance=Vector3.Distance(start,end);ray=new Ray(start,(end-start).normalized);}
        else{hitTarget=FindShotTarget(ray,w.range,out distance);end=ray.GetPoint(distance);}
        if(ShotSceneHit(ray,distance,out var scene)){hitTarget=null;end=scene.point;}
        var facing=end-hero.position;facing.y=0;if(facing.sqrMagnitude>.01f)hero.rotation=Quaternion.LookRotation(facing);start=hero.position+Vector3.up*1.4f+hero.forward*.4f;
        Vector3 shotPath=end-start;
        if(ShotSceneHit(new Ray(start,shotPath.normalized),shotPath.magnitude,out var obstacle)){hitTarget=null;end=obstacle.point;}
        if(hitTarget!=null)Hit(hitTarget,w.damage*DamageMultiplier);DrawShot(start,end);Sparks(end,"#b3eae9",4);PlaySfx(equippedWeapon==1?"sidearm":"rifle");
    }
    void SlashEffect(){
        var line=G("Frost blade arc",hero,V(0,1,0)).gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=15;for(int i=0;i<15;i++){float a=(-65+i*130f/14)*Mathf.Deg2Rad;line.SetPosition(i,V(Mathf.Sin(a)*2.2f,Mathf.Sin(i/14f*Mathf.PI)*.4f,Mathf.Cos(a)*2.2f));}line.startWidth=.08f;line.endWidth=.015f;line.sharedMaterial=AetherFx();Destroy(line.gameObject,.16f);
    }
    void Hit(Foe e,float damage){
        if(e.dead||!e.active||CoopIsGuest)return;e.hp=Mathf.Max(0,e.hp-damage);e.stagger=e.boss?.04f:.16f;HitPresentation(e);impactShake=.08f;floatingHits.Add(new FloatingHit{world=e.root.position+Vector3.up*(EnemyTargetHeight(e)+.5f),text=Mathf.RoundToInt(damage).ToString(),life=.8f,critical=damage>50});Sparks(e.root.position+Vector3.up,e.boss?"#b3eeed":"#efca99",6);
        if(e.hp>0)return;e.dead=true;e.root.gameObject.SetActive(false);if(e.warning)Destroy(e.warning);kills++;RecordDefeat(e);hp=Mathf.Min(MaxHealth,hp+10);AddCoins(e.reward);
        if(e.boss){phase=Phase.Win;rig.Play("Idle");Notify("The Rift Warden has fallen / +"+e.reward+" coins");}else Notify(e.title+" defeated / +"+e.reward+" coins");SaveJourney(false);
    }
}
