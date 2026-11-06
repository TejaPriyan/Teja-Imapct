using UnityEngine;
using System.Collections.Generic;

public partial class TejaAdventure {
    float LandscapeHeight(Vector3 p){
        float radius=new Vector2(p.x,p.z).magnitude;if(radius<90)return -.18f;
        float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(90,108,radius));
        float hills=(Mathf.PerlinNoise(p.x*.018f+21,p.z*.018f+11)*8+Mathf.Sin(p.z*.024f)*1.5f)*blend-.18f;
        if(p.x>88&&p.z>-92&&p.z<98){float river=105+Mathf.Sin(p.z*.025f)*5;float channel=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.8f,9,Mathf.Abs(p.x-river)));hills=Mathf.Lerp(hills,-1.3f,channel);}
        return hills;
    }
    float GroundHeight(Vector3 p){
        if(new Vector2(p.x,p.z).magnitude<90)return 0;
        float ground=LandscapeHeight(p);
        if(Mathf.Abs(p.z+12)<3.2f&&p.x>91&&p.x<126){float ramp=Mathf.Min(Mathf.InverseLerp(91,99,p.x),Mathf.InverseLerp(126,118,p.x));ground=Mathf.Max(ground,Mathf.Lerp(.1f,3.4f,ramp));}
        return ground;
    }
    string FrontierRegion(Vector3 p){
        if(p.z< -94)return "Starfall Sanctum";if(p.x>94)return "Silverrun River";
        if(p.z>94)return "Whisperpine Forest";if(p.x< -94)return "Sunbreak Highlands";return "";
    }
    Vector3 OnLand(float x,float z,float offset=0){var p=V(x,0,z);p.y=GroundHeight(p)+offset;return p;}
    void BuildFrontier(){
        // A lower ambient fill keeps the direction of the afternoon light visible
        // even on Low, where real-time shadows are deliberately disabled.
        RenderSettings.fogColor=new Color(.57f,.72f,.78f);RenderSettings.fogDensity=.0027f;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.36f,.47f,.58f);RenderSettings.ambientEquatorColor=new Color(.23f,.31f,.30f);RenderSettings.ambientGroundColor=new Color(.12f,.16f,.19f);
        RenderSettings.reflectionIntensity=.35f;
        sun.color=new Color(1,.91f,.76f);sun.intensity=1.04f;
        sun.transform.rotation=Quaternion.Euler(38,-35,0);
        var sky=new Material(Shader.Find("Teja/FrontierSky"));RenderSettings.skybox=sky;
        var original=GameObject.Find("Aetherfall — floating garden");if(original)foreach(Transform child in original.transform){
            float r=new Vector2(child.position.x,child.position.z).magnitude;
            if(child.name=="Drifting cloud"){child.position=V(child.position.x*1.7f,35+child.position.y*.35f,child.position.z*1.7f);}
            else if(r>60&&(child.name.StartsWith("#85aea5")||child.name.StartsWith("#a0b997")||child.name.StartsWith("#bfd3c1"))){child.position=V(child.position.x*2,child.position.y+18,child.position.z*2);}
        }
        var frontier=G("The Skybound Frontier");CreateLandscape(frontier);
        var scenery=G("Frontier scenery",frontier);
        Vector3[] east={V(58,0,5),V(79,0,-5),V(91,0,-12),V(104,0,-12),V(120,0,-12),V(126,0,8),V(113,0,39)};
        Vector3[] west={V(-62,0,-22),V(-84,0,-33),V(-108,0,-54),V(-123,0,-70)};
        Vector3[] south={V(7,0,39),V(13,0,72),V(12,0,103),V(36,0,123),V(55,0,123)};
        Vector3[] north={V(0,0,-75),V(-8,0,-94),V(0,0,-114),V(18,0,-126)};
        FrontierPath(scenery,east,4);FrontierPath(scenery,west,3.3f);FrontierPath(scenery,south,4);FrontierPath(scenery,north,4.3f);
        CreateRiver(frontier);CreateRiverBridge(scenery);
        for(int i=0;i<105;i++){
            float angle=i*2.39996f,r=104+Mathf.Sqrt((i%31)/31f)*39;Vector3 p=OnLand(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r);
            if(p.x>88&&p.z>-35&&p.z<55||Mathf.Abs(p.x)<8&&p.z< -94||Mathf.Abs(p.x-12)<8&&p.z>90||FlatDistance(p,V(-123,0,-70))<8||FlatDistance(p,V(-108,0,-54))<7)continue;
            FrontierPine(scenery,p,1+(i%4)*.25f);
        }
        for(int i=0;i<48;i++){float a=i*Mathf.PI/24;var p=OnLand(Mathf.Cos(a)*146,Mathf.Sin(a)*146);WorldRock(scenery,p+V(0,1,0),V(3.5f,2+i%3,3),"#77928a",i*37);}
        // A ruined courtyard, reachable on foot from the old gate.
        var sanctum=G("Starfall Sanctum",scenery,OnLand(0,-119));
        Cylinder("#9baea2",sanctum,V(0,.06f,0),V(24,.06f,23));
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var p=V(Mathf.Cos(a)*11,2.6f,Mathf.Sin(a)*10);Cylinder("#9aaea7",sanctum,p,V(.85f,2.6f,.85f),true);Box("#d1c9a4",sanctum,p+V(0,2.7f,0),V(1.2f,.22f,1.2f));}
        var arch=G("Starfall overlook",sanctum,V(0,0,-10));foreach(int side in new[]{-1,1})Box("#718d91",arch,V(side*3.2f,3.5f,0),V(1,7,1.3f),true);Box("#aebdaa",arch,V(0,7.2f,0),V(8,.6f,1.7f));
        var gem=Box("#9bd5dc",arch,V(0,5.9f,.2f),V(.9f,.9f,.9f));gem.localRotation=Quaternion.Euler(35,45,0);
        // An open pavilion serves as a walk-in rest stop.
        var lodge=G("Whisperpine shelter",scenery,OnLand(29,108));Box("#899a84",lodge,V(0,.08f,0),V(9,.16f,8),true);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Cylinder("#6f6551",lodge,V(x*3.8f,2,z*3.2f),V(.25f,2,.25f),true);
        WorldGable(lodge,V(0,4,0),10,2.2f,9,"#476d67");Box("#957f58",lodge,V(0,.6f,2),V(5,.2f,1),true);Box("#746653",lodge,V(0,.25f,2),V(4,.5f,.7f),true);
        Vector3[] camps={V(115,0,-24),V(123,0,4),V(110,0,40),V(-115,0,-50),V(-120,0,10),V(-70,0,105),V(15,0,120),V(10,0,-125)};
        for(int c=0;c<camps.Length;c++){var center=OnLand(camps[c].x,camps[c].z);Cylinder("#a4a98a",scenery,center+V(0,.02f,0),V(8,.02f,8));for(int i=0;i<3;i++){var p=center+V((i-1)*3.4f,0,(i%2)*3);p.y=GroundHeight(p);MakeEnemy(p,(FoeKind)((c+i)%4));}var marker=Box("#907d94",scenery,center+V(0,1.2f,2),V(.4f,2.4f,.4f));marker.localRotation=Quaternion.Euler(0,25,0);}
        AddWorldChest(OnLand(-126,22),3,45);AddWorldChest(OnLand(55,123),4,10);AddWorldChest(OnLand(18,-126),5,180);
        AddCoinTrail(east,24,.8f);AddCoinTrail(west,22,.8f);AddCoinTrail(south,22,1);AddCoinTrail(north,18,.7f);
        BuildFrontierMeadow(scenery,new[]{east,west,south,north});
        CombineWorldScenery(scenery);
        // Reuse the existing material cache and scenery batching. A restrained
        // saturation lift unifies the old island and new frontier without assets
        // or extra render passes, and leaves every collider and quest unchanged.
        foreach(var material in mats.Values){
            Color originalColor=material.color;float hue,saturation,value;
            Color.RGBToHSV(originalColor,out hue,out saturation,out value);
            Color graded=Color.HSVToRGB(hue,Mathf.Clamp01(saturation*1.10f),value*.96f);
            graded.a=originalColor.a;material.color=graded;material.SetFloat("_Glossiness",.06f);
        }
    }
    void CreateLandscape(Transform parent){
        const int n=153;const float span=306;var vertices=new Vector3[n*n];var colors=new Color[n*n];var triangles=new List<int>();
        for(int z=0;z<n;z++)for(int x=0;x<n;x++){int i=z*n+x;var p=V(-span/2+x*span/(n-1),0,-span/2+z*span/(n-1));p.y=LandscapeHeight(p);vertices[i]=p;float t=Mathf.PerlinNoise(p.x*.045f+17,p.z*.045f+31);colors[i]=Color.Lerp(new Color(.29f,.47f,.33f),new Color(.52f,.65f,.39f),t);if(p.x>89)colors[i]=Color.Lerp(colors[i],new Color(.50f,.61f,.53f),.3f);if(p.z< -98)colors[i]=Color.Lerp(colors[i],new Color(.40f,.53f,.57f),.35f);if(x<n-1&&z<n-1&&new Vector2(p.x+1,p.z+1).magnitude<152){triangles.AddRange(new[]{i,i+n,i+1,i+1,i+n,i+n+1});}}
        var mesh=new Mesh();mesh.vertices=vertices;mesh.colors=colors;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
        var land=G("Rolling frontier ground",parent).gameObject;land.AddComponent<MeshFilter>().sharedMesh=mesh;land.AddComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find("Teja/FrontierTerrain"));land.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    void FrontierPath(Transform parent,Vector3[] points,float width){
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int p=1;p<points.Length;p++){var delta=points[p]-points[p-1];int steps=Mathf.CeilToInt(delta.magnitude/2);var across=Vector3.Cross(delta.normalized,Vector3.up)*width*.5f;for(int j=0;j<=steps;j++){var center=Vector3.Lerp(points[p-1],points[p],j/(float)steps);var left=center-across;var right=center+across;left.y=GroundHeight(left)+.035f;right.y=GroundHeight(right)+.035f;int i=vertices.Count;vertices.Add(left);vertices.Add(right);if(j>0)triangles.AddRange(new[]{i-2,i-1,i,i-1,i+1,i});}}
        WorldMesh(parent,"Frontier trail",vertices,triangles,"#b4b897");
    }
    void FrontierPine(Transform parent,Vector3 at,float scale){
        var tree=G("Whisper pine",parent,at);tree.localScale=Vector3.one*scale;Cylinder("#6d6b53",tree,V(0,1.6f,0),V(.23f,1.6f,.23f),true);
        for(int i=0;i<3;i++){var top=G("Shared pine crown",tree,V(Mathf.Sin(at.x+i)*.10f,1.35f+i*1.14f,Mathf.Cos(at.z+i)*.1f));top.localScale=V(1.8f-i*.27f,1,1.8f-i*.27f);top.localRotation=Quaternion.Euler(0,i*37+at.x,0);top.gameObject.AddComponent<MeshFilter>().sharedMesh=V6PineMesh();top.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(i==0?"#37695b":i==1?"#4d8067":"#6b9878");}
    }
    void CreateRiver(Transform parent){
        var vertices=new List<Vector3>();var triangles=new List<int>();for(int i=0;i<=96;i++){float z=-92+i*2;float x=105+Mathf.Sin(z*.025f)*5;vertices.Add(V(x-3,.08f,z));vertices.Add(V(x+3,.08f,z));if(i>0){int k=i*2;triangles.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}}
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();var river=G("Silverrun current",parent).gameObject;river.AddComponent<MeshFilter>().sharedMesh=mesh;river.AddComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find("Teja/FrontierWater"));
    }
    void CreateRiverBridge(Transform parent){
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<=70;i++){float x=91+i*.5f;float y=GroundHeight(V(x,0,-12));vertices.Add(V(x,y,-14.7f));vertices.Add(V(x,y,-9.3f));if(i>0){int k=i*2;triangles.AddRange(new[]{k-2,k-1,k,k-1,k+1,k});}}
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();var bridge=G("Continuous traversable bridge deck",parent).gameObject;bridge.AddComponent<MeshFilter>().sharedMesh=mesh;bridge.AddComponent<MeshRenderer>().sharedMaterial=M("#aa9671");bridge.AddComponent<MeshCollider>().sharedMesh=mesh;
        for(int i=0;i<35;i++){float x=91+i;var p=OnLand(x,-12);if(x>99&&x<118)Box("#b8a580",parent,p+V(0,.008f,0),V(.035f,.012f,5.4f));if(i%3==0)foreach(int side in new[]{-1,1}){Box("#736b51",parent,p+V(0,.65f,side*2.8f),V(.18f,1.3f,.18f),true);Box("#9b8c68",parent,p+V(0,1.15f,side*2.8f),V(3.2f,.15f,.15f));}}
    }
    void BuildFrontierMeadow(Transform parent,Vector3[][] paths){
        var vertices=new List<Vector3>();var tris=new List<int>();var flowers=new List<Vector3>();var flowerTris=new List<int>();
        for(int i=0;i<8500;i++){
            float a=UnityEngine.Random.value*Mathf.PI*2,r=Mathf.Sqrt(UnityEngine.Random.Range(99*99,148*148));Vector3 p=OnLand(Mathf.Cos(a)*r,Mathf.Sin(a)*r);
            if(p.x>90&&p.x<119&&p.z>-92&&p.z<98)continue;
            bool road=false;foreach(var path in paths)for(int k=1;k<path.Length;k++){var delta=path[k]-path[k-1];var point=p;point.y=0;float t=Mathf.Clamp01(Vector3.Dot(point-path[k-1],delta)/delta.sqrMagnitude);if(FlatDistance(point,path[k-1]+delta*t)<3.3f)road=true;}if(road)continue;
            float height=UnityEngine.Random.Range(.2f,.52f);int n=vertices.Count;vertices.Add(p+V(-.08f,0,0));vertices.Add(p+V(.08f,0,0));vertices.Add(p+V(.02f,height,0));tris.AddRange(new[]{n,n+2,n+1,n,n+1,n+2});
            if(i%23==0){int f=flowers.Count;flowers.Add(p+V(-.13f,height,0));flowers.Add(p+V(.13f,height,0));flowers.Add(p+V(0,height+.22f,0));flowerTris.AddRange(new[]{f,f+2,f+1,f,f+1,f+2});}
        }
        WorldMesh(parent,"Frontier meadow blades",vertices,tris,"#86a46e");WorldMesh(parent,"Frontier wildflowers",flowers,flowerTris,"#e0d8ac");
    }
}
