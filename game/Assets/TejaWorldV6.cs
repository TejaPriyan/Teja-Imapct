using UnityEngine;
using System;
using System.Collections.Generic;

// Small exploration quest and visual detail. Reward mutations only happen in InteractWorldV6.
public partial class TejaAdventure {
    [Serializable] class WorldV6Save { public int quest,step,waystones=1,checkpoint; }
    int moonwellQuest,moonwellStep,waystoneMask=1,activeWaystone;
    readonly Vector3[] waystoneLocations={new Vector3(-4,0,27),new Vector3(83,0,-2),new Vector3(21,0,101)};
    readonly string[] waystoneNames={"Lantern Village","Silverrun Approach","Whisperpine Trail"};
    readonly Vector3 moonwellCenter=new Vector3(-94,0,-26);
    readonly string[] runeNames={"Sun","Leaf","Moon"};
    readonly int[] runeOrder={1,2,0};
    readonly Transform[] moonwellRunes=new Transform[3],waystoneRoots=new Transform[3];
    readonly Renderer[] runeLights=new Renderer[3],waystoneLights=new Renderer[3];
    Transform trailNotice,grottoPlaque,moonwellTreasure,moonwellLid;
    readonly List<Renderer> v6DetailRenderers=new List<Renderer>();
    readonly List<Transform> moonwellFireflies=new List<Transform>();
    Mesh v6LeafMesh,v6PineMesh;
    Material v6PathMaterial;
    float detailCullClock;

    void BuildWorldV6(){
        var world=G("Moonwell trail and living scenery");
        var staticScenery=G("Moonwell static rocks",world);
        BuildMoonwellGrotto(staticScenery,world);
        trailNotice=BuildMoonwellNotice(world,OnLand(3,25),"Moonwell trail notice");
        grottoPlaque=BuildMoonwellNotice(world,OnLand(-84,-29),"Moonwell stone inscription");
        for(int i=0;i<3;i++)BuildWaystone(world,i);
        var trail=new[]{V(-80,0,-30),V(-84,0,-30),V(-87,0,-27),V(-91,0,-26)};
        FrontierPath(staticScenery,trail,2.2f);
        for(int i=0;i<14;i++){
            float a=i*2.39996f;var p=OnLand(-94+Mathf.Cos(a)*(9+i%3),-26+Mathf.Sin(a)*(8+i%3));
            if(p.x> -86||p.z< -31)continue;
            WorldTree(staticScenery,p,.65f+(i%4)*.12f,i%2==0?"#7ca68d":"#a0b99b");
        }
        CombineWorldScenery(staticScenery);
        // Replace only the original high-poly sphere crowns; trunks/colliders stay intact.
        var original=GameObject.Find("Aetherfall — floating garden");
        if(original)foreach(Transform tree in original.transform)if(tree.name=="Blossom tree"){
            foreach(Transform crown in tree){var filter=crown.GetComponent<MeshFilter>();if(filter&&crown.name.Contains("Sphere")){filter.sharedMesh=V6LeafMesh();crown.localScale*=.58f;}}
        }
        ResetWorldV6();
    }
    Transform BuildMoonwellNotice(Transform parent,Vector3 at,string title){
        var root=G(title,parent,at);Cylinder("#6d624d",root,V(0,.9f,0),V(.11f,.9f,.11f));
        Box("#344f54",root,V(0,1.55f,0),V(1.75f,.96f,.14f));Box("#d7c19a",root,V(0,1.55f,-.08f),V(1.55f,.76f,.025f));
        for(int i=0;i<3;i++){var glyph=Box(i==0?"#709f82":i==1?"#72a6be":"#d9aa5b",root,V(-.47f+i*.47f,1.58f,-.11f),V(.19f,.25f,.03f));glyph.localRotation=Quaternion.Euler(0,0,i==0?35:i==1?0:45);}
        return root;
    }
    void BuildMoonwellGrotto(Transform scenery,Transform dynamicRoot){
        // East-facing entrance. The southern edge stays north of the Ilyra trail.
        for(int i=0;i<9;i++){
            float a=(58+i*30)*Mathf.Deg2Rad;var p=OnLand(moonwellCenter.x+Mathf.Cos(a)*6.7f,moonwellCenter.z+Mathf.Sin(a)*5.4f);
            WorldRock(scenery,p+V(0,2.2f,0),V(3.8f,3.2f,2.6f),i%2==0?"#657e81":"#7b9291",i*31);
            var collision=Box("#657e81",scenery,p+V(0,1.8f,0),V(2.3f,3.6f,1.4f),true);collision.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);collision.GetComponent<Renderer>().enabled=false;
        }
        // A shallow crown leaves headroom and a skylight over the rune chamber.
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3;WorldRock(scenery,OnLand(moonwellCenter.x+Mathf.Cos(a)*3.8f,moonwellCenter.z+Mathf.Sin(a)*3.3f)+V(0,5.1f,0),V(3.3f,1.15f,2.8f),"#738b8d",i*51);}
        foreach(int side in new[]{-1,1}){
            var at=OnLand(-87.8f,-26+side*3.1f);WorldRock(scenery,at+V(0,2.1f,0),V(2.5f,2.9f,2.6f),"#859a95",side*18);
            WorldCrystal(scenery,at+V(0,.2f,-side*.9f),1.1f,"#8ecbd3",side*25);
        }
        WorldRock(scenery,OnLand(-87.8f,-26)+V(0,5.2f,0),V(3.2f,1.2f,4.2f),"#8d9f9b",80);
        Vector3[] runePositions={V(-95,0,-22.7f),V(-92,0,-29),V(-97,0,-28.7f)};
        for(int i=0;i<3;i++){
            var root=G(runeNames[i]+" rune",dynamicRoot,OnLand(runePositions[i].x,runePositions[i].z));moonwellRunes[i]=root;
            Cylinder("#6d8588",root,V(0,.25f,0),V(1.25f,.25f,1.25f),true);Cylinder("#a9bcb5",root,V(0,.61f,0),V(.68f,.36f,.68f));
            var glyph=Box("#739aa5",root,V(0,1.24f,0),V(.38f,.55f,.20f));glyph.localRotation=Quaternion.Euler(0,25,45);runeLights[i]=glyph.GetComponent<Renderer>();
            // Number of small facets makes each glyph readable without text floating in-world.
            for(int j=0;j<=i;j++)Box("#e4d4a7",root,V(-.18f+j*.18f,.77f,-.37f),V(.06f,.12f,.04f));
        }
        moonwellTreasure=G("Moonwell star cache",dynamicRoot,OnLand(-97.4f,-24));
        Box("#45616b",moonwellTreasure,V(0,.38f,0),V(1.6f,.76f,1.05f),true);
        moonwellLid=G("Moonwell treasure lid",moonwellTreasure,V(0,.77f,.51f));
        Box("#a8c4ba",moonwellLid,V(0,.08f,-.51f),V(1.65f,.22f,1.1f));Box("#e8c987",moonwellTreasure,V(0,.49f,-.55f),V(.2f,.32f,.05f));
        for(int i=0;i<12;i++){
            float a=i*2.39996f;var p=OnLand(moonwellCenter.x+Mathf.Cos(a)*4,moonwellCenter.z+Mathf.Sin(a)*3)+V(0,1.3f+(i%4)*.5f,0);
            var mote=Box("#b5e4c9",dynamicRoot,p,Vector3.one*.045f);moonwellFireflies.Add(mote);
        }
    }
    void BuildWaystone(Transform parent,int id){
        var at=OnLand(waystoneLocations[id].x,waystoneLocations[id].z);var root=G(waystoneNames[id]+" waystone",parent,at);waystoneRoots[id]=root;
        Cylinder("#91a6a0",root,V(0,.12f,0),V(2.7f,.12f,2.7f));Cylinder("#526e75",root,V(0,1,0),V(.6f,1,.6f),true);
        Box("#d5c59b",root,V(0,1.15f,-.33f),V(.18f,1.15f,.06f));var light=Box("#7496a2",root,V(0,2.25f,0),V(.43f,.65f,.43f));light.localRotation=Quaternion.Euler(0,45,30);waystoneLights[id]=light.GetComponent<Renderer>();
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var stone=Box("#c2c8aa",root,V(Mathf.Cos(a)*1.1f,.2f,Mathf.Sin(a)*1.1f),V(.3f,.16f,.3f));stone.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
    }
    void TickWorldV6(float dt){
        if(moonwellLid)moonwellLid.localRotation=Quaternion.Slerp(moonwellLid.localRotation,Quaternion.Euler(moonwellQuest==3?108:0,0,0),1-Mathf.Exp(-dt*7));
        for(int i=0;i<moonwellFireflies.Count;i++){var t=moonwellFireflies[i];var p=t.position;p.y=GroundHeight(p)+1.4f+(i%4)*.5f+Mathf.Sin(elapsed*.9f+i)*.18f;t.position=p;t.Rotate(0,dt*45,dt*25);}
        detailCullClock-=dt;if(detailCullClock<=0&&hero){detailCullClock=.4f;float distance=graphicsPreset==0?72:graphicsPreset==1?108:150;
            foreach(var r in v6DetailRenderers)if(r)r.enabled=r.bounds.SqrDistance(hero.position)<distance*distance;
        }
    }
    string WorldV6Prompt(){
        if(!hero)return "";
        int target=NearestWorldV6Target();
        if(target>=0&&target<3)return ((waystoneMask&(1<<target))==0?"Awaken ":"Rest at ")+waystoneNames[target]+" waystone";
        if(target==3||target==4)return "Read the Moonwell clue";
        if(target>=5&&target<=7)return moonwellQuest>=2?"Inspect the awakened "+runeNames[target-5]+" rune":"Touch the "+runeNames[target-5]+" rune";
        if(target==8)return moonwellQuest==3?"Moonwell cache already claimed":moonwellQuest==2?"Open the Moonwell cache":"Inspect the sealed Moonwell cache";
        return "";
    }
    int NearestWorldV6Target(){
        int result=-1;float nearest=2.7f;
        for(int i=0;i<3;i++)FindWorldV6Target(waystoneRoots[i],i,ref result,ref nearest);
        FindWorldV6Target(trailNotice,3,ref result,ref nearest);FindWorldV6Target(grottoPlaque,4,ref result,ref nearest);
        for(int i=0;i<3;i++)FindWorldV6Target(moonwellRunes[i],i+5,ref result,ref nearest);
        FindWorldV6Target(moonwellTreasure,8,ref result,ref nearest);return result;
    }
    void FindWorldV6Target(Transform target,int id,ref int result,ref float nearest){
        if(!target||Mathf.Abs(hero.position.y-target.position.y)>2.8f)return;
        float d=FlatDistance(hero.position,target.position);if(d<nearest){nearest=d;result=id;}
    }
    bool InteractWorldV6(){
        if(phase!=Phase.Play||!hero)return false;int target=NearestWorldV6Target();if(target<0)return false;
        if(target<3){
            // Resting is allowed only outside nearby fights.
            foreach(var e in foes)if(e.active&&!e.dead&&FlatDistance(e.root.position,hero.position)<9){Notify("Clear nearby foes before resting at the waystone.");return true;}
            waystoneMask|=1<<target;activeWaystone=target;hp=MaxHealth;invulnerable=Mathf.Max(invulnerable,1.5f);
            RefreshWorldV6();PulseAt(waystoneRoots[target].position,"#9bd9db",3,.65f);Tone(720,.3f);Notify(waystoneNames[target]+" checkpoint set. Health restored.");SaveJourney(false);return true;
        }
        if(target==3||target==4){if(moonwellQuest==0)moonwellQuest=1;Notify("Moonwell Grotto / west of Blossom Grove. Leaf wakes Moon; Moon welcomes Sun.");SaveJourney(false);return true;}
        if(target>=5&&target<=7){TouchMoonwellRune(target-5);return true;}
        if(moonwellQuest<2){Notify("The cache sleeps. Touch the runes in order: Leaf, Moon, Sun.");return true;}
        if(moonwellQuest==3){Notify("The Moonwell treasure is already yours.");return true;}
        moonwellQuest=3; // Terminal state is committed before the one-time reward.
        AddCoins(120);AwardExperience(180);potions=Mathf.Min(9,potions+1);RefreshWorldV6();
        PulseAt(moonwellTreasure.position,"#e6d4a2",4,.7f);Tone(880,.35f);Notify("Moonwell complete / +120 coins / +180 XP / +1 tonic");SaveJourney(false);return true;
    }
    void TouchMoonwellRune(int rune){
        if(rune<0||rune>=3)return;
        if(moonwellQuest>=2){Notify("The three runes are awake. Claim the Moonwell cache.");return;}
        moonwellQuest=1;
        if(rune==runeOrder[moonwellStep]){moonwellStep++;Tone(650+moonwellStep*110,.18f);PulseAt(moonwellRunes[rune].position,"#b8e5de",1.6f,.4f);}
        else{moonwellStep=0;Tone(190,.15f);Notify("The light fades. Remember: Leaf, Moon, Sun.");}
        if(moonwellStep==3){moonwellQuest=2;Notify("The Moonwell seal is open. Claim its hidden treasure.");}
        else if(moonwellStep>0)Notify(runeNames[rune]+" rune awakened / "+moonwellStep+" of 3");
        RefreshWorldV6();SaveJourney(false);
    }
    string WorldV6Objective(){
        if(moonwellQuest==0)return "Moonwell trail / Read the notice at Lantern Village's southern entrance.";
        if(moonwellQuest==1)return "Moonwell Grotto / West of Blossom Grove / Leaf → Moon → Sun / "+moonwellStep+" of 3";
        if(moonwellQuest==2)return "Moonwell Grotto / Seal opened. Claim the hidden cache.";
        return "Moonwell Grotto / Completed / 120 coins, 180 XP and a tonic earned.";
    }
    void ResetWorldV6(){moonwellQuest=moonwellStep=activeWaystone=0;waystoneMask=1;RefreshWorldV6();}
    WorldV6Save GetWorldV6Save(){return new WorldV6Save{quest=moonwellQuest,step=moonwellStep,waystones=waystoneMask,checkpoint=activeWaystone};}
    void ApplyWorldV6Save(WorldV6Save data){
        if(data==null){ResetWorldV6();return;}moonwellQuest=Mathf.Clamp(data.quest,0,3);moonwellStep=moonwellQuest>=2?3:Mathf.Clamp(data.step,0,2);
        if(moonwellQuest==0)moonwellStep=0;waystoneMask=(data.waystones&7)|1;activeWaystone=Mathf.Clamp(data.checkpoint,0,2);
        if((waystoneMask&(1<<activeWaystone))==0)activeWaystone=0;RefreshWorldV6();
    }
    Vector3 WorldV6RespawnPoint(){var p=waystoneLocations[Mathf.Clamp(activeWaystone,0,2)]+V(0,0,-2.1f);return OnLand(p.x,p.z,.18f);}
    void RefreshWorldV6(){
        for(int i=0;i<3;i++){
            bool lit=false;for(int j=0;j<moonwellStep;j++)if(runeOrder[j]==i)lit=true;
            if(runeLights[i])runeLights[i].sharedMaterial=M(lit?"#c7f0de":"#739aa5");
            if(waystoneLights[i])waystoneLights[i].sharedMaterial=M((waystoneMask&(1<<i))!=0?"#b7ebe2":"#7496a2");
        }
    }
    void RegisterWorldV6Detail(Renderer renderer){if(renderer&&renderer.bounds.size.sqrMagnitude<3600)v6DetailRenderers.Add(renderer);}
    class V6MeshChunk { public List<Vector3> vertices=new List<Vector3>();public List<int> triangles=new List<int>(); }
    void WorldV6MeshChunks(Transform parent,string name,List<Vector3> vertices,List<int> triangles,string color){
        var chunks=new Dictionary<Vector2Int,V6MeshChunk>();
        for(int i=0;i<triangles.Count;i+=3){Vector3 center=(vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3;
            var key=new Vector2Int(Mathf.FloorToInt(center.x/24),Mathf.FloorToInt(center.z/24));
            if(!chunks.TryGetValue(key,out var chunk)){chunk=new V6MeshChunk();chunks.Add(key,chunk);}int n=chunk.vertices.Count;
            chunk.vertices.Add(vertices[triangles[i]]);chunk.vertices.Add(vertices[triangles[i+1]]);chunk.vertices.Add(vertices[triangles[i+2]]);chunk.triangles.AddRange(new[]{n,n+1,n+2});
        }
        foreach(var pair in chunks){var go=G(name+" tile "+pair.Key,parent).gameObject;var mesh=new Mesh();mesh.SetVertices(pair.Value.vertices);mesh.SetTriangles(pair.Value.triangles,0);
            var normals=new Vector3[pair.Value.vertices.Count];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.up;mesh.normals=normals;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=M(color);}
    }
    Material WorldV6TrailMaterial(){if(!v6PathMaterial)v6PathMaterial=new Material(Shader.Find("Teja/TrailV6"));return v6PathMaterial;}
    Mesh V6LeafMesh(){
        if(v6LeafMesh)return v6LeafMesh;
        var v=new List<Vector3>();var t=new List<int>();const int sides=10;
        float[] heights={-.65f,-.15f,.5f,1};float[] radii={.5f,1,.76f,.05f};
        for(int ring=0;ring<3;ring++)for(int i=0;i<sides;i++){
            float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
            Vector3 a0=V(Mathf.Cos(a)*radii[ring],heights[ring],Mathf.Sin(a)*radii[ring]);Vector3 b0=V(Mathf.Cos(b)*radii[ring],heights[ring],Mathf.Sin(b)*radii[ring]);
            Vector3 a1=V(Mathf.Cos(a+.12f)*radii[ring+1],heights[ring+1],Mathf.Sin(a+.12f)*radii[ring+1]);Vector3 b1=V(Mathf.Cos(b+.12f)*radii[ring+1],heights[ring+1],Mathf.Sin(b+.12f)*radii[ring+1]);
            int n=v.Count;v.AddRange(new[]{a0,b0,b1,a0,b1,a1});t.AddRange(new[]{n,n+2,n+1,n+3,n+5,n+4});
        }
        v6LeafMesh=new Mesh();v6LeafMesh.SetVertices(v);v6LeafMesh.SetTriangles(t,0);v6LeafMesh.RecalculateNormals();return v6LeafMesh;
    }
    Mesh V6PineMesh(){
        if(v6PineMesh)return v6PineMesh;var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6,r=1+(i%3)*.065f,r2=1+((i+1)%3)*.065f;int n=v.Count;
            v.AddRange(new[]{V(.09f,2.4f,-.06f),V(Mathf.Cos(a)*r,Mathf.Sin(i*2.4f)*.12f,Mathf.Sin(a)*r),V(Mathf.Cos(b)*r2,Mathf.Sin((i+1)*2.4f)*.12f,Mathf.Sin(b)*r2)});t.AddRange(new[]{n,n+2,n+1});}
        v6PineMesh=new Mesh();v6PineMesh.SetVertices(v);v6PineMesh.SetTriangles(t,0);v6PineMesh.RecalculateNormals();return v6PineMesh;
    }
    void V6LeafCluster(Transform parent,Vector3 at,Vector3 size,string color,float angle){
        var root=G("Faceted leaf crown",parent,at);root.localScale=size;root.localRotation=Quaternion.Euler(0,angle,-7+angle%15);
        root.gameObject.AddComponent<MeshFilter>().sharedMesh=V6LeafMesh();root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(color);
    }
    void ValidateWorldV6(){
        var previous=GetWorldV6Save();var position=hero.position;float previousHp=hp;int money=coins,flasks=potions;var progression=GetProgressionSave();Phase oldPhase=phase;
        try{
            Require(moonwellRunes[0]&&moonwellRunes[1]&&moonwellRunes[2]&&moonwellTreasure,"Moonwell puzzle actors built");
            ResetWorldV6();TouchMoonwellRune(0);Require(moonwellStep==0,"Incorrect first rune cannot open the seal");
            TouchMoonwellRune(1);TouchMoonwellRune(0);Require(moonwellStep==0,"Incorrect second rune resets progress");
            TouchMoonwellRune(1);TouchMoonwellRune(2);TouchMoonwellRune(0);Require(moonwellQuest==2&&moonwellStep==3,"Ordered runes open the Moonwell cache");
            phase=Phase.Play;motor.enabled=false;hero.position=moonwellTreasure.position+V(.7f,.18f,-.8f);motor.enabled=true;
            int before=coins;Require(InteractWorldV6()&&coins==before+120&&moonwellQuest==3,"Moonwell treasure pays once");InteractWorldV6();Require(coins==before+120,"Moonwell duplicate reward rejected");
            var saved=JsonUtility.FromJson<WorldV6Save>(JsonUtility.ToJson(GetWorldV6Save()));ResetWorldV6();ApplyWorldV6Save(saved);Require(moonwellQuest==3,"Completed Moonwell quest survives save roundtrip");
            ApplyWorldV6Save(new WorldV6Save{quest=99,step=99,waystones=0,checkpoint=99});Require(moonwellQuest==3&&moonwellStep==3&&waystoneMask==1&&activeWaystone==0,"Malformed world state bounded");
            ApplyWorldV6Save(new WorldV6Save{waystones=7,checkpoint=2});Vector3 respawn=WorldV6RespawnPoint();Require(FlatDistance(respawn,waystoneLocations[2])<3&&respawn.y>GroundHeight(respawn),"Checkpoint returns a safe ground position");
            foreach(var at in waystoneLocations)Require(Physics.Raycast(at+V(0,20,-2.1f),Vector3.down,40),"Waystone respawn has collidable ground");
            motor.enabled=false;hero.position=OnLand(-84,-26,.18f);motor.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<100;i++)motor.Move(V(-.1f,-.045f,0));
            Require(hero.position.x< -92,"Moonwell entrance is traversable by the character controller");
        }finally{ApplyWorldV6Save(previous);coins=money;potions=flasks;ApplyProgressionSave(progression);hp=previousHp;phase=oldPhase;motor.enabled=false;hero.position=position;motor.enabled=true;Physics.SyncTransforms();}
    }
}
