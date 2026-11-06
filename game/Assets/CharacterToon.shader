Shader "Teja/CharacterToon" {
 Properties { _MainTex("Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
 SubShader { Tags{"RenderType"="TransparentCutout" "Queue"="AlphaTest"} Cull Back
 Pass { Tags{"LightMode"="ForwardBase"}
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 #include "Lighting.cginc"
 sampler2D _MainTex;float4 _MainTex_ST;fixed4 _Color;
 struct a {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
 struct v {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 view:TEXCOORD2;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=TRANSFORM_TEX(i.uv,_MainTex);o.normal=UnityObjectToWorldNormal(i.normal);o.view=WorldSpaceViewDir(i.vertex);return o;}
 fixed4 frag(v i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv)*_Color;clip(c.a-.08);float n=dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz));float shade=lerp(.78,1.06,smoothstep(-.1,.3,n));float rim=pow(1-saturate(dot(normalize(i.normal),normalize(i.view))),3)*.08;return fixed4(c.rgb*shade+rim*float3(.5,.7,.8),1);}
 ENDCG
 }
 }
 Fallback "Unlit/Texture"
}

