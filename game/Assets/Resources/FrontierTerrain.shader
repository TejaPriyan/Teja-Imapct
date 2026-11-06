Shader "Teja/FrontierTerrain" {
 Properties { _Color("Tint",Color)=(1,1,1,1) }
 SubShader { Tags { "RenderType"="Opaque" } LOD 150
 CGPROGRAM
 #pragma surface surf Lambert vertex:vert
 #include "UnityCG.cginc"
 fixed4 _Color;
 struct Input {float4 color:COLOR;float3 worldPos;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.color=v.color;}
 float terrainHash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float terrainNoise(float2 p){float2 b=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(terrainHash(b),terrainHash(b+float2(1,0)),f.x),lerp(terrainHash(b+float2(0,1)),terrainHash(b+1),f.x),f.y);}
 void surf(Input i,inout SurfaceOutput o){
     float3 grass=i.color.rgb;
     #ifndef UNITY_COLORSPACE_GAMMA
     grass=GammaToLinearSpace(grass);
     #endif
     float patch=lerp(.90,1.12,terrainNoise(i.worldPos.xz*.18));
     float grain=lerp(.965,1.035,terrainNoise(i.worldPos.xz*2.2));
     o.Albedo=grass*_Color.rgb*patch*grain;o.Alpha=1;
 }
 ENDCG
 } Fallback "Diffuse"
}
