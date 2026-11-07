Shader "Climb/Surface"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) _VertexColors ("Vertex colors", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _VertexColors;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert(appdata v) {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal); o.color = v.color; return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                float light = .45 + .55 * saturate(dot(normalize(i.normal), normalize(float3(-.4,.8,-.3))));
                return fixed4(_Color.rgb * lerp(float3(1,1,1),i.color.rgb,_VertexColors) * light, _Color.a);
            }
            ENDCG
        }
    }
}
