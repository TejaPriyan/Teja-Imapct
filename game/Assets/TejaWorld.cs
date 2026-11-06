using UnityEngine;
using System.Collections.Generic;
using System.Text;

// The outer island is built from shared low-poly meshes. Static scenery is then
// combined per material; only collectables, chest lids and actors stay separate.
public partial class TejaAdventure {
    class WorldCoin { public Transform root; public Vector3 spawn; public bool taken; }
    class WorldChest { public Transform root, lid; public bool opened; public int id; }
    readonly List<WorldCoin> worldCoins = new List<WorldCoin>();
    readonly List<WorldChest> worldChests = new List<WorldChest>();
    Vector3 merchantPosition = new Vector3(7,0,33);
    string regionName = "Aetherfall Meadow";
    Mesh worldRockMesh, worldCoinMesh;
    float regionMessageCooldown;

    void BuildExpandedWorld() {
        RenderSettings.fogDensity = .0048f;
        var island = G("The expanded island of Aetherfall");
        var scenery = G("Batched regional scenery", island);
        // The first island reaches y=0. The outer ground sits just below it.
        Cylinder("#91ac76",scenery,V(0,-1,0),V(194,1,194),true);
        Cylinder("#647d76",scenery,V(0,-5.6f,0),V(190,3.5f,190));
        Cylinder("#6fa6ad",scenery,V(0,-10,0),V(680,.05f,680));
        worldRockMesh = CreateWorldRockMesh();
        worldCoinMesh = CreateWorldCoinMesh();
        // v0.3: the old decorative rim at radius 95 is open to the frontier.
        // Long, winding paths join every objective and the trading village.
        Vector3[] mainRoad={V(0,0,25),V(0,0,15),V(-2,0,0),V(2,0,-20),V(0,0,-39),V(-3,0,-54),V(0,0,-70)};
        Vector3[] westRoad={V(-1,0,4),V(-17,0,3),V(-34,0,-3),V(-49,0,-14),V(-62,0,-22)};
        Vector3[] eastRoad={V(1,0,5),V(18,0,11),V(36,0,14),V(49,0,11),V(58,0,5)};
        Vector3[] ruinRoad={V(-2,0,-38),V(-13,0,-46),V(-28,0,-53),V(-22,0,-63)};
        WorldRoad(scenery,mainRoad,4.4f); WorldRoad(scenery,westRoad,3.5f);
        WorldRoad(scenery,eastRoad,3.6f); WorldRoad(scenery,ruinRoad,3.2f);
        WorldRoad(scenery,new[]{V(0,0,25),V(5,0,32),V(7,0,39),V(-2,0,45)},4.8f);
        BuildLanternVillage(scenery);
        BuildBlossomGrove(scenery);
        BuildCrystalMarsh(scenery);
        BuildMoonstoneRuins(scenery);
        BuildWindsweptCoast(scenery);
        // Move quest landmarks from the original garden into the new regions.
        Vector3[] sigilSpots={V(-62,0,-22),V(58,0,5),V(-22,0,-63)};
        for(int i=0;i<sigils.Count&&i<sigilSpots.Length;i++)sigils[i].root.position=sigilSpots[i];
        var oldArena=GameObject.Find("The Warden's arena");
        if(oldArena)oldArena.transform.position=V(0,0,-70);
        var oldRift=GameObject.Find("The ancient rift");
        if(oldRift)oldRift.transform.position=V(0,0,-75);
        boss.spawn=V(0,0,-71); boss.root.position=boss.spawn;
        // Six encounter camps. Foes remain dynamic; camp meshes are batched.
        Vector3[] camps={V(-41,0,-11),V(-66,0,-33),V(41,0,20),V(65,0,-8),V(-36,0,-53),V(18,0,-55)};
        for(int i=0;i<camps.Length;i++)BuildRiftCamp(scenery,camps[i],i);
        BuildWorldGrass(scenery);
        AddWorldChest(V(-68,0,-7),0,30);
        AddWorldChest(V(64,0,23),1,-80);
        AddWorldChest(V(-38,0,-65),2,125);
        AddCoinTrail(mainRoad,26,1.3f);
        AddCoinTrail(westRoad,23,1.1f);
        AddCoinTrail(eastRoad,23,1.2f);
        AddCoinTrail(ruinRoad,14,.9f);
        AddCoinTrail(new[]{V(-61,0,-23),V(-72,0,-17),V(-68,0,-7)},8,.5f);
        AddCoinTrail(new[]{V(57,0,7),V(65,0,14),V(64,0,23)},8,.6f);
        CombineWorldScenery(scenery);
    }

    void BuildLanternVillage(Transform p) {
        var village=G("Lantern Village",p);
        Cylinder("#cfceb2",village,V(5,.025f,36),V(28,.025f,23));
        WorldHouse(village,V(-9,0,35),V(7,4.8f,6),18,"#c3c9aa","#517b75");
        WorldHouse(village,V(20,0,40),V(8,5.3f,7),-15,"#dfc8a0","#b65d54");
        WorldHouse(village,V(-5,0,48),V(9,4.5f,6),0,"#dfd1b5","#627f83");
        WorldHouse(village,V(15,0,51),V(6,3.8f,5),180,"#d7b990","#966e65");
        // Vendor faces the southern approach; the map and shop use this spot.
        var stall=G("Mira's arms and remedies",village,merchantPosition);
        Box("#6e5543",stall,V(0,.65f,0),V(4.6f,1.3f,1.7f),true);
        Box("#dab181",stall,V(0,1.35f,0),V(5,.14f,2));
        foreach(int s in new[]{-1,1}) {
            Box("#715948",stall,V(s*2.2f,1.65f,.5f),V(.13f,3.3f,.13f),true);
            Box("#e5c47e",stall,V(s*2.4f,2.15f,-.55f),V(.35f,.54f,.35f));
        }
        for(int i=0;i<8;i++) {
            var canopy=Box(i%2==0?"#3b7b78":"#f0dcab",stall,V(-2.15f+i*.62f,3.12f,-.2f),V(.62f,.12f,3.1f));
            canopy.localRotation=Quaternion.Euler(-8,0,0);
            Box(i%2==0?"#3b7b78":"#f0dcab",stall,V(-2.15f+i*.62f,2.69f,-1.72f),V(.61f,.43f,.07f));
        }
        var vendor=G("Mira - village merchant",stall,V(0,0,.95f));
        Cylinder("#385c61",vendor,V(0,.91f,0),V(.53f,.65f,.53f));
        Ball("#e8c2a0",vendor,V(0,1.83f,-.02f),V(.48f,.51f,.47f));
        Ball("#574355",vendor,V(0,2.04f,.06f),V(.54f,.25f,.54f));
        Box("#e5c47e",vendor,V(0,1.44f,-.27f),V(.2f,.35f,.08f));
        foreach(int s in new[]{-1,1})Box("#e8c2a0",vendor,V(s*.36f,1.29f,-.12f),V(.17f,.5f,.18f));
        for(int i=0;i<3;i++) {
            Cylinder(i==0?"#81c4b2":"#aa8cc7",stall,V(-1.5f+i*.43f,1.62f,-.1f),V(.26f,.23f,.26f));
            Cylinder("#d4bc89",stall,V(-1.5f+i*.43f,1.88f,-.1f),V(.13f,.08f,.13f));
        }
        Box("#b9d7d9",stall,V(.75f,1.46f,-.16f),V(.14f,.08f,1.25f));
        Box("#d1ae6e",stall,V(.75f,1.48f,.37f),V(.46f,.1f,.12f));
        Box("#394d59",stall,V(1.5f,1.47f,.08f),V(.3f,.18f,.65f));
        // Central well and benches make the settlement readable at a distance.
        var well=G("Village wishing well",village,V(-1,0,39));
        Cylinder("#a8ac93",well,V(0,.5f,0),V(3,.5f,3),true);
        Cylinder("#59989c",well,V(0,1.015f,0),V(2.35f,.018f,2.35f));
        foreach(int s in new[]{-1,1})Box("#846e51",well,V(s*1.6f,1.8f,0),V(.2f,3.6f,.2f),true);
        WorldGable(well,V(0,3.3f,0),4.2f,1.4f,3.5f,"#537a77");
        for(int i=0;i<6;i++)WorldLamp(village,V(-7+i*4.8f,0,27.2f));
        for(int i=0;i<4;i++) {
            Vector3 at=V(-12+i*10,0,54);
            WorldTree(village,at,1.05f,i%2==0?"#dfafbb":"#e8c9b0");
        }
        WorldSign(village,V(4,0,27),"#d2b77b");
        for(int i=0;i<4;i++) {
            var crate=Box("#a7875b",village,V(12+i%2, .5f+(i/2),34+i%2),V(.95f,.95f,.95f),true);
            Box("#644e3d",crate,V(0,0,-.51f),V(.8f,.12f,.08f));
        }
        for(int i=0;i<2;i++) {
            Box("#896b50",village,V(-5+i*10,.65f,42),V(3,.2f,.85f),true);
            foreach(int s in new[]{-1,1})Box("#645a45",village,V(-5+i*10+s, .3f,42),V(.18f,.6f,.65f));
        }
    }

    void BuildBlossomGrove(Transform p) {
        var grove=G("Blossom Grove",p);
        Cylinder("#9fad82",grove,V(-58,-.075f,-22),V(43,.035f,45));
        for(int i=0;i<24;i++) {
            float a=i*2.39996f, r=8+Mathf.Sqrt(i/24f)*17;
            var at=V(-58+Mathf.Cos(a)*r,0,-20+Mathf.Sin(a)*r);
            if(Vector3.Distance(at,V(-62,0,-22))<5)continue;
            WorldTree(grove,at,1.1f+(i%4)*.15f,i%3==0?"#e9bbcd":"#dba3b7");
        }
        Cylinder("#7daeb0",grove,V(-56,-.055f,-38),V(13,.015f,7));
        for(int i=0;i<10;i++) {
            float a=i*Mathf.PI*.2f;
            WorldRock(grove,V(-56+Mathf.Cos(a)*6.2f,.1f,-38+Mathf.Sin(a)*3.4f),V(1.5f,.8f,1.2f),"#9baa97",i*29);
        }
        var shrine=G("Grove shrine",grove,V(-62,0,-25));
        foreach(int s in new[]{-1,1}) {
            Box("#9b696d",shrine,V(s*2.4f,2.3f,0),V(.45f,4.6f,.55f),true);
            Box("#d8bd99",shrine,V(s*2.4f,.2f,0),V(.9f,.4f,.85f));
        }
        Box("#a16c70",shrine,V(0,4.5f,0),V(6.2f,.43f,.63f));
        var cap=Box("#566f72",shrine,V(0,4.93f,0),V(7.1f,.32f,.9f));
        WorldLamp(grove,V(-66,0,-24)); WorldLamp(grove,V(-58,0,-24));
        WorldSign(grove,V(-34,0,-5),"#cc9ca8");
    }

    void BuildCrystalMarsh(Transform p) {
        var marsh=G("Crystal Marsh",p);
        Cylinder("#86a69f",marsh,V(59,-.073f,8),V(45,.036f,40));
        foreach(var q in new[]{V(54,-.027f,-5),V(69,-.027f,15),V(44,-.027f,24)}) {
            Cylinder("#77acb4",marsh,q,V(11,.016f,7));
            Cylinder("#8abfc1",marsh,q+V(0,.009f,0),V(7,.016f,4.5f));
        }
        for(int i=0;i<23;i++) {
            float a=i*2.39996f,r=7+(i%7)*2.5f;
            Vector3 pos=V(58+Mathf.Cos(a)*r,0,7+Mathf.Sin(a)*r);
            if(FlatDistance(pos,V(58,0,5))<4)continue;
            WorldCrystal(marsh,pos,1.1f+(i%5)*.46f,i%3==0?"#96bce0":"#8ed3cc",i*57);
            if(i%3==0)WorldRock(marsh,pos+V(-1,.25f,0),V(3,1.3f,2.4f),"#718f99",i*33);
        }
        // Short decorative boardwalk across a shallow, walkable pool.
        for(int i=0;i<16;i++)Box("#a79b7d",marsh,V(43+i*.67f,.1f,14),V(.59f,.16f,3.2f));
        foreach(int s in new[]{-1,1}) {
            for(int i=0;i<4;i++)Box("#6d7462",marsh,V(43+i*3.3f,.6f,14+s*1.7f),V(.16f,1.2f,.16f));
            Box("#8a9274",marsh,V(48,1.15f,14+s*1.7f),V(10.3f,.12f,.14f));
        }
        for(int i=0;i<10;i++)WorldTree(marsh,V(44+(i%4)*10,0,-12+(i/4)*17),.8f+(i%3)*.13f,"#8eb9ac");
        WorldSign(marsh,V(34,0,17),"#89c9ce");
    }

    void BuildMoonstoneRuins(Transform p) {
        var ruins=G("Moonstone Ruins",p);
        Cylinder("#a6b19b",ruins,V(-23,-.06f,-62),V(42,.025f,30));
        for(int i=0;i<5;i++) {
            float z=-50-i*6;
            foreach(int s in new[]{-1,1}) {
                float h=3.4f+((i+(s+1)/2)%3)*1.8f;
                Cylinder("#bdc4ad",ruins,V(-23+s*9,h/2,z),V(1.5f,h/2,1.5f),true);
                Box("#d9d5ba",ruins,V(-23+s*9,h+.1f,z),V(2.3f,.38f,2.2f));
                Box("#aab39e",ruins,V(-23+s*9,.2f,z),V(2.2f,.4f,2.2f));
            }
        }
        Box("#c2c8b0",ruins,V(-23,5.15f,-70),V(19,.85f,1.8f));
        for(int i=0;i<15;i++) {
            float a=i*2.3f,r=11+(i%4)*2;
            WorldRock(ruins,V(-23+Mathf.Cos(a)*r,.3f,-62+Mathf.Sin(a)*r),V(2.5f,1.2f,2),"#a4b09d",i*17);
        }
        for(int i=0;i<7;i++) {
            var block=Box("#bac2ac",ruins,V(-43+i*6,.5f,-76),V(3.5f,1,1.7f),true);
            block.localRotation=Quaternion.Euler(0,i*17,0);
        }
        for(int i=0;i<5;i++)WorldTree(ruins,V(-48+i*5,0,-43-i*3),1.0f,"#8ea893");
        WorldSign(ruins,V(-11,0,-46),"#c3d4ce");
        // The northern gate reads as a distinct destination against the horizon.
        foreach(int s in new[]{-1,1}) {
            WorldRock(ruins,V(s*13,1.5f,-79),V(9,6,8),"#738b89",s*21);
            WorldCrystal(ruins,V(s*11,0,-73),2.4f,"#91ced5",s*33);
            Cylinder("#aab9ad",ruins,V(s*9,2.3f,-66),V(1.3f,2.3f,1.3f),true);
            Ball("#c5dba9",ruins,V(s*9,4.85f,-66),V(.85f,.85f,.85f));
        }
    }

    void BuildWindsweptCoast(Transform p) {
        var coast=G("Windswept Coast",p);
        for(int i=0;i<20;i++) {
            float a=.13f+i*Mathf.PI*.91f/20, r=65+(i%3)*7;
            Vector3 q=V(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
            WorldTree(coast,q,1.0f+(i%3)*.18f,i%4==0?"#e7c5b0":"#9bb383");
            if(i%2==0)WorldRock(coast,q+V(3,.45f,-1),V(4,1.8f,3.3f),"#92a68b",i*47);
        }
        for(int i=0;i<12;i++) {
            float a=3.4f+i*.19f,r=70+(i%4)*4;
            WorldRock(coast,V(Mathf.Cos(a)*r,.5f,Mathf.Sin(a)*r),V(5,2.8f,4),"#8ca28c",i*37);
        }
        // South lookout, visible beyond the village but accessible on foot.
        Cylinder("#b7bba1",coast,V(3,.05f,73),V(14,.05f,14));
        for(int i=0;i<7;i++) {
            float a=i*Mathf.PI/6;
            Box("#b1b59e",coast,V(3+Mathf.Cos(a)*6,.65f,73+Mathf.Sin(a)*6),V(.65f,1.3f,.65f),true);
        }
        WorldRoad(coast,new[]{V(3,0,45),V(2,0,58),V(3,0,73)},3.2f);
        WorldLamp(coast,V(0,0,65)); WorldLamp(coast,V(6,0,65));
    }

    void BuildRiftCamp(Transform p,Vector3 at,int id) {
        var camp=G("Riftling camp "+(id+1),p,at);
        Cylinder("#938e77",camp,V(0,-.04f,0),V(11,.04f,10));
        for(int i=0;i<7;i++) {
            float a=i*Mathf.PI*2/7;
            WorldRock(camp,V(Mathf.Cos(a)*1.1f,.15f,Mathf.Sin(a)*1.1f),V(.6f,.45f,.55f),"#727e77",i*18);
        }
        WorldCrystal(camp,V(0,.15f,0),.65f,"#d98f88",id*45);
        foreach(int s in new[]{-1,1}) {
            var rack=Box("#665b51",camp,V(s*3.6f,1.15f,2.8f),V(.18f,2.3f,.18f));
            rack.localRotation=Quaternion.Euler(0,0,-s*10);
            var flag=Box("#755467",camp,V(s*3.6f+ .47f,1.7f,2.8f),V(.9f,1.1f,.08f));
        }
        for(int i=0;i<3;i++) {
            float a=i*Mathf.PI*2/3+id*.6f;
            var e=MakeFoe(at+V(Mathf.Cos(a)*3.2f,0,Mathf.Sin(a)*3.2f),false);
            e.root.name="Camp "+(id+1)+" riftling "+(i+1);
        }
    }

    void WorldTree(Transform p,Vector3 at,float scale,string leaves) {
        var tree=G("Branching canopy tree",p,at);tree.localScale=Vector3.one*scale;
        Cylinder("#72654e",tree,V(0,1.65f,0),V(.25f,1.65f,.25f),true);
        foreach(int side in new[]{-1,1}){var branch=Cylinder("#72654e",tree,V(side*.45f,2.5f,.12f),V(.12f,.85f,.12f));branch.localRotation=Quaternion.Euler(0,side*20,-side*38);}
        Color color;ColorUtility.TryParseHtmlString(leaves,out color);string shade="#"+ColorUtility.ToHtmlStringRGB(Color.Lerp(color,new Color(.27f,.43f,.35f),.18f));
        V6LeafCluster(tree,V(-.9f,3.3f,.15f),V(1.55f,1.0f,1.4f),shade,32);
        V6LeafCluster(tree,V(1.05f,3.6f,-.1f),V(1.65f,1.0f,1.5f),leaves,75);
        V6LeafCluster(tree,V(.1f,4.2f,.15f),V(1.9f,1.2f,1.6f),leaves,12);
        V6LeafCluster(tree,V(.05f,3.4f,1.1f),V(1.4f,.9f,1.25f),shade,140);
    }
    void WorldRock(Transform p,Vector3 at,Vector3 scale,string color,float yawAngle) {
        var r=G("Faceted scenery",p,at);r.localScale=scale;r.localRotation=Quaternion.Euler(0,yawAngle,0);
        r.gameObject.AddComponent<MeshFilter>().sharedMesh=worldRockMesh;
        r.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(color);
    }
    void WorldCrystal(Transform p,Vector3 at,float h,string color,float yawAngle) {
        var c=G("Sky crystal",p,at);c.localRotation=Quaternion.Euler(0,yawAngle,0);
        WorldRock(c,V(0,h,0),V(h*.5f,h,h*.43f),color,18);
        var shard=G("Leaning crystal",c,V(h*.36f,h*.48f,0));shard.localRotation=Quaternion.Euler(0,0,-25);
        WorldRock(shard,Vector3.zero,V(h*.27f,h*.62f,h*.27f),color,37);
    }
    void WorldHouse(Transform p,Vector3 at,Vector3 size,float rot,string wall,string roof) {
        var house=G("Village house",p,at);house.localRotation=Quaternion.Euler(0,rot,0);
        Box(wall,house,V(0,size.y/2,0),size,true);
        Box("#a79d80",house,V(0,.16f,0),V(size.x+.4f,.32f,size.z+.4f));
        WorldGable(house,V(0,size.y,0),size.x+1.2f,size.y*.39f,size.z+1.3f,roof);
        foreach(int s in new[]{-1,1}) {
            Box("#77634e",house,V(s*(size.x/2-.08f),size.y/2,-size.z/2-.035f),V(.2f,size.y,.12f));
            Box("#526c68",house,V(s*size.x*.29f,size.y*.56f,-size.z/2-.07f),V(1.1f,1.3f,.12f));
            Box("#d6c49b",house,V(s*size.x*.29f,size.y*.56f,-size.z/2-.15f),V(.08f,1.3f,.08f));
            Box("#d6c49b",house,V(s*size.x*.29f,size.y*.56f,-size.z/2-.15f),V(1.1f,.08f,.08f));
        }
        Box("#765d45",house,V(0,1.2f,-size.z/2-.045f),V(1.25f,2.4f,.15f));
        Box("#e0c781",house,V(.39f,1.25f,-size.z/2-.15f),V(.08f,.08f,.08f));
        Box("#826c52",house,V(0,size.y*.72f,-size.z/2-.04f),V(size.x,.17f,.1f));
        WorldLamp(house,V(size.x*.39f,0,-size.z/2-1.2f));
    }
    void WorldGable(Transform p,Vector3 at,float width,float h,float depth,string color) {
        var t=G("Tiled gable roof",p,at);float w=width/2,d=depth/2;
        var mesh=new Mesh();mesh.vertices=new[]{V(-w,0,-d),V(w,0,-d),V(0,h,-d),V(-w,0,d),V(w,0,d),V(0,h,d)};
        mesh.triangles=new[]{0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4,0,1,4,0,4,3};mesh.RecalculateNormals();
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(color);
        Box("#d0b78c",t,V(0,h+.06f,0),V(.15f,.13f,depth+.2f));
    }
    void WorldLamp(Transform p,Vector3 at) {
        var l=G("Village lantern",p,at);
        Cylinder("#686655",l,V(0,1.3f,0),V(.12f,1.3f,.12f));
        Box("#e4c489",l,V(0,2.45f,0),V(.45f,.65f,.45f));
        Box("#576b67",l,V(0,2.81f,0),V(.64f,.12f,.64f));
        Box("#576b67",l,V(0,2.08f,0),V(.55f,.12f,.55f));
    }
    void WorldSign(Transform p,Vector3 at,string color) {
        var s=G("Wayfinding marker",p,at);
        Box("#756952",s,V(0,.9f,0),V(.15f,1.8f,.15f));
        Box(color,s,V(.25f,1.7f,0),V(1.7f,.5f,.15f));
        Box("#e3dac0",s,V(.85f,1.7f,-.09f),V(.16f,.2f,.03f));
    }
    void WorldRoad(Transform p,Vector3[] points,float width) {
        FrontierPath(p,points,width);
    }

    void BuildWorldGrass(Transform p) {
        var blades=new List<Vector3>();var indices=new List<int>();
        var flowers=new List<Vector3>();var flowerIndices=new List<int>();
        for(int i=0;i<6400;i++) {
            float a=UnityEngine.Random.value*Mathf.PI*2;
            float r=Mathf.Sqrt(UnityEngine.Random.Range(36*36,93*93));
            float x=Mathf.Cos(a)*r,z=Mathf.Sin(a)*r;
            if((Mathf.Abs(x)<7&&z<30)||(x>-17&&x<26&&z>25&&z<55))continue;
            float y=-.1f,h=UnityEngine.Random.Range(.18f,.43f);int n=blades.Count;
            blades.Add(V(x-.07f,y,z));blades.Add(V(x+.07f,y,z));blades.Add(V(x+.035f,y+h,z));
            indices.AddRange(new[]{n,n+2,n+1,n,n+1,n+2});
            if(i%16==0) {
                int f=flowers.Count;float sz=.13f;
                flowers.Add(V(x-sz,y+h,z));flowers.Add(V(x,y+h+sz,z));flowers.Add(V(x+sz,y+h,z));flowers.Add(V(x,y+h-sz,z));
                flowerIndices.AddRange(new[]{f,f+1,f+2,f,f+2,f+3,f+2,f+1,f,f+3,f+2,f});
            }
        }
        WorldMesh(p,"Combined outer meadow grass",blades,indices,"#749264");
        WorldMesh(p,"Combined wildflowers",flowers,flowerIndices,"#e3d6b0");
    }
    void WorldMesh(Transform p,string name,List<Vector3> vertices,List<int> triangles,string color) {
        if((name.Contains("grass")||name.Contains("meadow")||name.Contains("wildflower"))&&vertices.Count>600){WorldV6MeshChunks(p,name,vertices,triangles,color);return;}
        var go=G(name,p).gameObject;var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        var normals=new Vector3[vertices.Count];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.up;mesh.normals=normals;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=name=="Frontier trail"?WorldV6TrailMaterial():M(color);
    }
    Mesh CreateWorldRockMesh() {
        // Flat shaded truncated bipyramid: 20 triangles instead of sphere meshes.
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<5;i++) {
            float a=i*Mathf.PI*2/5,b=(i+1)*Mathf.PI*2/5;
            Vector3 lo=V(Mathf.Cos(a)*.76f,-.33f,Mathf.Sin(a)*.76f),nextLo=V(Mathf.Cos(b)*.76f,-.33f,Mathf.Sin(b)*.76f);
            Vector3 hi=V(Mathf.Cos(a)*.8f,.38f,Mathf.Sin(a)*.8f),nextHi=V(Mathf.Cos(b)*.8f,.38f,Mathf.Sin(b)*.8f);
            Vector3[] face={V(0,1,0),nextHi,hi,hi,nextHi,nextLo,hi,nextLo,lo,lo,nextLo,V(0,-.8f,0)};
            int n=vertices.Count;vertices.AddRange(face);for(int k=0;k<12;k++)triangles.Add(n+k);
        }
        var m=new Mesh();m.SetVertices(vertices);m.SetTriangles(triangles,0);m.RecalculateNormals();return m;
    }
    Mesh CreateWorldCoinMesh() {
        var verts=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<10;i++) {
            float a=i*Mathf.PI*.2f,b=(i+1)*Mathf.PI*.2f;int n=verts.Count;
            verts.Add(V(0,0,0));verts.Add(V(Mathf.Cos(a)*.21f,Mathf.Sin(a)*.21f,0));verts.Add(V(Mathf.Cos(b)*.21f,Mathf.Sin(b)*.21f,0));
            tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+1});
        }
        var m=new Mesh();m.SetVertices(verts);m.SetTriangles(tris,0);var normals=new Vector3[verts.Count];for(int n=0;n<normals.Length;n++)normals[n]=Vector3.up;m.normals=normals;return m;
    }
    void CombineWorldScenery(Transform root) {
        // Spatial material batches keep renderer bounds local enough for meaningful culling.
        var groups=new Dictionary<string,List<CombineInstance>>();var materials=new Dictionary<string,Material>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
            var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!filter.sharedMesh)continue;
            var material=renderer.sharedMaterial;if(!material)continue;
            Vector3 center=renderer.bounds.center;string key=material.GetInstanceID()+"/"+Mathf.FloorToInt(center.x/32)+"/"+Mathf.FloorToInt(center.z/32);
            if(!groups.ContainsKey(key)){groups.Add(key,new List<CombineInstance>());materials.Add(key,material);}
            groups[key].Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});
            renderer.enabled=false;Destroy(renderer);Destroy(filter);
        }
        foreach(var pair in groups) {
            var go=G("Local scenery batch "+pair.Key,root).gameObject;var mesh=new Mesh();
            mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(pair.Value.ToArray(),true,true);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=materials[pair.Key];
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;RegisterWorldV6Detail(r);
        }
    }

    void AddCoinTrail(Vector3[] points,int amount,float offset) {
        float total=0;var distances=new float[points.Length];
        for(int i=1;i<points.Length;i++){total+=Vector3.Distance(points[i-1],points[i]);distances[i]=total;}
        for(int i=0;i<amount;i++) {
            float distance=(i+.5f)/amount*total;int segment=1;
            while(segment<points.Length-1&&distances[segment]<distance)segment++;
            float t=(distance-distances[segment-1])/(distances[segment]-distances[segment-1]);
            Vector3 dir=(points[segment]-points[segment-1]).normalized;
            Vector3 at=Vector3.Lerp(points[segment-1],points[segment],t)+V(-dir.z,0,dir.x)*offset*(i%2==0?1:-1);
            at.y=GroundHeight(at)+.9f;var root=G("Aether coin "+worldCoins.Count,null,at);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=worldCoinMesh;
            var rend=root.gameObject.AddComponent<MeshRenderer>();rend.sharedMaterial=M("#f3cd6d");rend.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            worldCoins.Add(new WorldCoin{root=root,spawn=at});
        }
    }
    void AddWorldChest(Vector3 at,int id,float rotation) {
        var r=G("Sealed exploration chest "+(id+1),null,at);r.localRotation=Quaternion.Euler(0,rotation,0);
        Box("#76553f",r,V(0,.4f,0),V(1.65f,.8f,1.05f),true);
        foreach(int s in new[]{-1,1})Box("#d9bd7d",r,V(s*.61f,.4f,0),V(.13f,.87f,1.13f));
        var lid=G("Hinged treasure lid",r,V(0,.81f,.52f));
        Box("#9e7850",lid,V(0,.14f,-.52f),V(1.69f,.28f,1.08f));
        foreach(int s in new[]{-1,1})Box("#e0c482",lid,V(s*.61f,.15f,-.52f),V(.14f,.32f,1.12f));
        Box("#e4ce8d",r,V(0,.61f,-.57f),V(.23f,.25f,.09f));
        worldChests.Add(new WorldChest{root=r,lid=lid,id=id});
    }

    void ResetExploration() {
        foreach(var c in worldCoins){c.taken=false;c.root.gameObject.SetActive(true);c.root.position=c.spawn;}
        foreach(var c in worldChests){c.opened=(chestMask&(1<<c.id))!=0;c.lid.localRotation=Quaternion.Euler(c.opened?108:0,0,0);}
        regionName="Aetherfall Meadow";regionMessageCooldown=3;
    }
    void UpdateExploration(float dt) {
        for(int i=0;i<worldCoins.Count;i++) {
            var c=worldCoins[i];if(c.taken)continue;bool visible=FlatDistance(hero.position,c.spawn)<75;if(c.root.gameObject.activeSelf!=visible)c.root.gameObject.SetActive(visible);
            c.root.Rotate(0,100*dt,0,Space.World);c.root.position=c.spawn+V(0,Mathf.Sin(elapsed*2.4f+i)*.12f,0);
            if(!CoopIsGuest&&((FlatDistance(hero.position,c.spawn)<1.25f&&Mathf.Abs(hero.position.y-(c.spawn.y-.9f))<2.2f)||(CompanionNear(c.spawn,1.25f)&&Mathf.Abs(coopRemote.position.y-(c.spawn.y-.9f))<2.2f))) {
                c.taken=true;c.root.gameObject.SetActive(false);AddCoins(3);Tone(1050,.065f);
            }
        }
        foreach(var c in worldChests) {
            // Save restoration can set chestMask after ResetExploration.
            c.opened=(chestMask&(1<<c.id))!=0;
            c.lid.localRotation=Quaternion.Slerp(c.lid.localRotation,Quaternion.Euler(c.opened?108:0,0,0),1-Mathf.Exp(-dt*7));
        }
        regionMessageCooldown=Mathf.Max(0,regionMessageCooldown-dt);
        Vector3 p=hero.position;
        string next=FrontierRegion(p);
        if(string.IsNullOrEmpty(next))next=p.z>26&&p.z<58&&p.x>-20&&p.x<30?"Lantern Village":
            p.x< -32&&p.z<12&&p.z> -45?"Blossom Grove":
            p.x>32&&p.z> -28&&p.z<40?"Crystal Marsh":
            p.z< -60&&p.x> -12&&p.x<23?"Warden's Gate":
            p.z< -43&&p.x< -9?"Moonstone Ruins":
            p.z>55||new Vector2(p.x,p.z).magnitude>82?"Windswept Coast":"Aetherfall Meadow";
        if(next!=regionName){regionName=next;if(regionMessageCooldown<=0){Notify("Discovered  ·  "+regionName);regionMessageCooldown=5;}}
    }
    bool NearChest() {
        foreach(var c in worldChests)if((chestMask&(1<<c.id))==0&&FlatDistance(hero.position,c.root.position)<3)return true;
        return false;
    }
    bool InteractWithWorld() {
        if(phase!=Phase.Play)return false;
        WorldChest nearest=null;float best=3;
        foreach(var c in worldChests)if((chestMask&(1<<c.id))==0){float distance=FlatDistance(hero.position,c.root.position);if(distance<best){best=distance;nearest=c;}}
        if(nearest==null)return false;
        chestMask|=1<<nearest.id;nearest.opened=true;AddCoins(50);potions=Mathf.Min(9,potions+1);
        PulseAt(nearest.root.position,"#f0d79e",3,.6f);Tone(780,.25f);
        Notify("Treasure found  ·  +50 coins  ·  +1 remedy");SaveJourney(false);return true;
    }
    string CoinState() {
        var state=new StringBuilder(worldCoins.Count);foreach(var c in worldCoins)state.Append(c.taken?'1':'0');return state.ToString();
    }
    void ApplyCoinState(string state) {
        for(int i=0;i<worldCoins.Count;i++){var c=worldCoins[i];c.taken=!string.IsNullOrEmpty(state)&&i<state.Length&&state[i]=='1';c.root.gameObject.SetActive(!c.taken);}
        foreach(var c in worldChests){c.opened=(chestMask&(1<<c.id))!=0;c.lid.localRotation=Quaternion.Euler(c.opened?108:0,0,0);}
    }
}
