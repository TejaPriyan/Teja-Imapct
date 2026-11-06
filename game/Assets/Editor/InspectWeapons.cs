using UnityEditor;
using UnityEngine;
public static class InspectWeapons {
 public static void Inspect(){
  foreach(string n in new[]{"a","k"}){
   string path="Assets/Resources/Weapons/blaster-"+n+".fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.SaveAndReimport();
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
   foreach(var t in root.GetComponentsInChildren<Transform>())Debug.Log("WEAPON_NODE "+n+" "+t.name+" scale="+t.localScale+" rotation="+t.localEulerAngles);
   foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
    var m=f.sharedMesh;var lo=new Vector2(100,100);var hi=new Vector2(-100,-100);foreach(var uv in m.uv){lo=Vector2.Min(lo,uv);hi=Vector2.Max(hi,uv);}
    Debug.Log("WEAPON_MESH "+n+" "+m.name+" bounds="+m.bounds+" world="+f.GetComponent<Renderer>().bounds+" uv="+lo+".."+hi);
   }
   Object.DestroyImmediate(root);
  }
 }
}
