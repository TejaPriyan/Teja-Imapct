using UnityEngine;
using System.Collections.Generic;

public partial class TejaAdventure {
    Vector2 smoothMovement;
    float coyoteTime,jumpBuffer,hitConfirm;
    class EnemyMotion {
        public Transform[] legs,arms;
        public Quaternion[] legRest,armRest;
        public Renderer[] renderers;
        public Vector3 previous;
        public float stride,flash;
        public bool flashed;
    }
    readonly Dictionary<Foe,EnemyMotion> enemyMotion=new Dictionary<Foe,EnemyMotion>();
    MaterialPropertyBlock enemyFlash;
    Material aetherMaterial;
    void PrepareEnemyMotion(Foe e){
        if(enemyFlash==null)enemyFlash=new MaterialPropertyBlock();
        var legs=new List<Transform>();var arms=new List<Transform>();
        foreach(Transform t in e.root)if(t!=e.body&&Mathf.Abs(t.localPosition.x)>.1f&&t.localPosition.y<1)legs.Add(t);
        foreach(Transform t in e.body)if(Mathf.Abs(t.localPosition.x)>(e.boss?1.1f:.65f)&&t.localPosition.y<2)arms.Add(t);
        var m=new EnemyMotion{legs=legs.ToArray(),arms=arms.ToArray(),legRest=new Quaternion[legs.Count],armRest=new Quaternion[arms.Count],renderers=e.root.GetComponentsInChildren<Renderer>(),previous=e.root.position};
        for(int i=0;i<legs.Count;i++)m.legRest[i]=legs[i].localRotation;
        for(int i=0;i<arms.Count;i++)m.armRest[i]=arms[i].localRotation;
        enemyMotion[e]=m;
        if(e.kind==FoeKind.Sentinel||e.boss){
            var crest=Box(e.boss?"#a7edff":"#f7d389",e.body,V(0,e.boss?3.4f:1.5f,0),V(.22f,.55f,.22f));crest.localRotation=Quaternion.Euler(0,45,30);
        }
    }
    void TickEnemyPresentation(float dt){
        foreach(var e in foes){
            if(!enemyMotion.TryGetValue(e,out var m))continue;
            m.flash=Mathf.Max(0,m.flash-dt);
            if(e.dead||!e.root.gameObject.activeInHierarchy)continue;
            float speed=FlatDistance(e.root.position,m.previous)/Mathf.Max(dt,.001f);m.previous=e.root.position;
            m.stride+=Mathf.Min(speed,12)*dt*2.7f;
            float walkAmount=Mathf.Clamp01(speed*.4f),wind=e.wind>0?1-Mathf.Clamp01(e.wind/Mathf.Max(.01f,e.windStart)):0;
            for(int i=0;i<m.legs.Length;i++)m.legs[i].localRotation=m.legRest[i]*Quaternion.Euler(Mathf.Sin(m.stride+i*Mathf.PI)*18*walkAmount,0,0);
            for(int i=0;i<m.arms.Length;i++)m.arms[i].localRotation=m.armRest[i]*Quaternion.Euler(-Mathf.Sin(m.stride+i*Mathf.PI)*12*walkAmount-wind*(e.boss?64:35),0,(i%2==0?-1:1)*wind*8);
            e.body.localRotation=Quaternion.Euler(e.stagger>0?-9:e.wind>0?-wind*12:Mathf.Sin(m.stride)*2*walkAmount,0,e.stagger>0?6:0);
            bool flash=m.flash>0;
            if(flash!=m.flashed){m.flashed=flash;enemyFlash.Clear();if(flash)enemyFlash.SetColor("_Color",new Color(1,.88f,.62f));foreach(var r in m.renderers)if(r)r.SetPropertyBlock(flash?enemyFlash:null);}
        }
    }
    void PatrolEnemy(Foe e,float dt){
        float seed=e.spawn.x*.17f+e.spawn.z*.11f;
        Vector3 destination=e.spawn+V(Mathf.Sin(elapsed*.22f+seed)*2.6f,0,Mathf.Cos(elapsed*.22f+seed)*2.1f);
        var direction=destination-e.root.position;direction.y=0;
        if(direction.magnitude>.45f){e.root.rotation=Quaternion.Slerp(e.root.rotation,Quaternion.LookRotation(direction),dt*2);StepEnemy(e,direction,e.speed*.32f*dt);}
    }
    void HitPresentation(Foe e){
        hitConfirm=.22f;if(enemyMotion.TryGetValue(e,out var m))m.flash=.11f;
        PlaySfx(e.boss?"boss_hit":"hit");
    }
    Material AetherFx(){
        if(!aetherMaterial){aetherMaterial=new Material(Shader.Find("Teja/AetherFX"));aetherMaterial.color=new Color(.5f,.95f,1,.8f);}
        return aetherMaterial;
    }
    void DrawShot(Vector3 start,Vector3 end){
        var line=G("Aether tracer").gameObject.AddComponent<LineRenderer>();line.positionCount=2;line.SetPosition(0,start);line.SetPosition(1,end);line.startWidth=.055f;line.endWidth=.012f;line.sharedMaterial=AetherFx();Destroy(line.gameObject,.11f);
        var flash=Ball("#c9ffff",null,start,Vector3.one*.15f);flash.GetComponent<Renderer>().sharedMaterial=AetherFx();Destroy(flash.gameObject,.065f);
    }
    Ray CombatAimRay(){return coopExecuting?coopActionRay:cam.ViewportPointToRay(new Vector3(.5f,.5f,0));}
    void DrawHitConfirm(){
        if(hitConfirm<=0)return;float x=uiWidth*.5f,y=360;
        for(int i=0;i<4;i++){float a=(45+i*90)*Mathf.Deg2Rad;UiLine(new Vector2(x+Mathf.Cos(a)*9,y+Mathf.Sin(a)*9),new Vector2(x+Mathf.Cos(a)*15,y+Mathf.Sin(a)*15),2,uiGold);}
    }
    void ResetMovementPolish(){smoothMovement=Vector2.zero;coyoteTime=jumpBuffer=hitConfirm=0;}
    void TickJumpBuffer(float dt){
        coyoteTime=motor.isGrounded?.12f:Mathf.Max(0,coyoteTime-dt);jumpBuffer=Mathf.Max(0,jumpBuffer-dt);
        if(jumpBuffer>0&&coyoteTime>0){jumpVelocity=6.5f;jumpBuffer=coyoteTime=0;PlaySfx("jump");}
    }
    void RespawnAtCheckpoint(){
        if(CoopIsGuest){QueueCoopCommand("respawn");return;}
        hp=MaxHealth;invulnerable=2;phase=Phase.Play;ResetMovementPolish();ResetControlInput();
        motor.enabled=false;hero.position=WorldV6RespawnPoint();motor.enabled=true;gravityVelocity=jumpVelocity=0;rig.Play("Idle");Notify("Restored at your waystone. Your journey continues.");SaveJourney(false);
    }
}
