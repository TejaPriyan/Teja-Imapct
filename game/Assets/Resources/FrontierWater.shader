Shader "Teja/FrontierWater" {
 SubShader {Tags {"RenderType"="Opaque"} Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 struct a{float4 vertex:POSITION;};struct v{float4 pos:SV_POSITION;float3 world:TEXCOORD0;UNITY_FOG_COORDS(1)};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.world=mul(unity_ObjectToWorld,i.vertex).xyz;UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(v i):SV_Target{
     float ripple=sin(i.world.z*2.3+_Time.y*.9+sin(i.world.x*2.1))*sin(i.world.x*1.7-_Time.y*.7);
     float flow=.5+.5*sin(i.world.z*.24-_Time.y*.38+i.world.x*.48);
     float3 color=lerp(float3(.09,.33,.39),float3(.17,.51,.55),flow);
     float fresnel=pow(1-abs(normalize(_WorldSpaceCameraPos-i.world).y),3);
     color=lerp(color,float3(.55,.74,.79),fresnel*.4);
     float edge=abs(i.world.x-(105+sin(i.world.z*.025)*5));
     float foam=smoothstep(2.55,3,edge)*(.32+.22*sin(i.world.z*2-_Time.y));
     float shine=smoothstep(.88,1,ripple)*.2;
     color=lerp(color,float3(.67,.87,.84),saturate(foam+shine));
     #ifndef UNITY_COLORSPACE_GAMMA
     color=GammaToLinearSpace(color);
     #endif
     fixed4 c=fixed4(color,1);UNITY_APPLY_FOG(i.fogCoord,c);return c;
 }
 ENDCG }
 }
}
