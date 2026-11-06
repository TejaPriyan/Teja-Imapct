Shader "Teja/AetherFX" {
 Properties { _Color ("Color", Color) = (0.5,0.95,1,0.8) }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha One ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;
 struct appdata { float4 vertex:POSITION; }; struct v2f {float4 vertex:SV_POSITION;};
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);return o;}
 fixed4 frag(v2f i):SV_Target{return _Color;}
 ENDCG }
 } }
