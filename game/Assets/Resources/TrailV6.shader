Shader "Teja/TrailV6" {
 SubShader { Tags {"RenderType"="Opaque"} LOD 150
 CGPROGRAM
 #pragma surface surf Lambert
 #include "UnityCG.cginc"
 struct Input {float3 worldPos;};
 float hashV6(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 void surf(Input i,inout SurfaceOutput o){
  float2 p=i.worldPos.xz*1.45;p.x+=floor(p.y)*.46;
  float2 cell=floor(p),f=frac(p);float grain=hashV6(cell);
  float border=min(min(f.x,1-f.x),min(f.y,1-f.y));
  float joint=1-smoothstep(.015,.075,border);
  float3 stone=lerp(float3(.61,.62,.48),float3(.76,.74,.59),grain);
  stone=lerp(stone,float3(.44,.50,.39),joint*.55);
  #ifndef UNITY_COLORSPACE_GAMMA
  stone=GammaToLinearSpace(stone);
  #endif
  o.Albedo=stone;o.Alpha=1;
 }
 ENDCG
 } Fallback "Diffuse"
}
