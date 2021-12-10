Shader "Unlit/Wireframe"
{
    Properties
    {
        _WireColor ("WireColor", Color) = (1,1,1,1)
        _WireWidth ("WireWidth", Range(0,10)) = 1
        _WireSmoothness ("WireSmoothness", Range(0,10)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Cull Back
            ZWrite on
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                v2g data;
                float3 bary : TEXCOORD9;
            };

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                return o;
            }

            [maxvertexcount(3)]
            void geom (
                triangle v2g i[3],
                inout TriangleStream<g2f> stream
            ) {
                float3 p0 = i[0].vertex.xyz;
                float3 p1 = i[1].vertex.xyz;
                float3 p2 = i[2].vertex.xyz;

                float3 triangleNormal = normalize(cross(p1 - p0, p2 - p0));
                i[0].normal = triangleNormal;
                i[1].normal = triangleNormal;
                i[2].normal = triangleNormal;

                //stream.Append(i[0]);
                //stream.Append(i[1]);
                //stream.Append(i[2]);

                g2f g0, g1, g2;
                g0.data = i[0];
                g1.data = i[1];
                g2.data = i[2];

                g0.bary = float3(1, 0, 0);
                g1.bary = float3(0, 1, 0);
                g2.bary = float3(0, 0, 1);

                stream.Append(g0);
                stream.Append(g1);
                stream.Append(g2);
            }

            float4 _WireColor; 
            float _WireWidth;
            float _WireSmoothness;

            fixed4 frag (g2f i) : SV_Target
            {
                fixed4 col = _WireColor;
                float3 bary = i.bary;
                float3 deltas = fwidth (bary);
                float3 smoothing = deltas * _WireSmoothness;
                float3 thickness = deltas * _WireWidth / max((1 - 0.5) * i.data.vertex.w, 0.5);
                bary = smoothstep(thickness, thickness + smoothing, bary);
                float minBary = min(bary.x, min(bary.y, bary.z));
                return col * (1 - minBary);
            }
            ENDCG
        }
    }
}
