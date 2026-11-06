Shader "Teja/FrontierSky" {
 SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct a{float4 vertex:POSITION;};struct v{float4 pos:SV_POSITION;float3 direction:TEXCOORD0;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.direction=i.vertex.xyz;return o;}
 fixed4 frag(v i):SV_Target{
     float3 d=normalize(i.direction);float h=saturate(d.y);
     float3 color=lerp(float3(.70,.83,.85),float3(.19,.43,.65),pow(h,.58));
     float lightDot=saturate(dot(d,normalize(float3(.45,.62,-.65))));
     float halo=pow(lightDot,14)*.13,disk=smoothstep(.9986,.9995,lightDot);
     color=lerp(color,float3(1,.91,.68),halo);color=lerp(color,float3(1,.96,.81),disk);
     // The authored palette is sRGB; explicitly convert in Linear projects.
     #ifndef UNITY_COLORSPACE_GAMMA
     color=GammaToLinearSpace(color);
     #endif
     return float4(color,1);
 }
 ENDCG }
 }
}
