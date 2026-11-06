//Unlit, alpha-blended shader for MGizmoBatch: every vertex carries its own colour, so thousands of
//differently coloured lines and triangles draw as one mesh. _Color tints the whole batch and stays white
//by default. Not instanced: a batch is one mesh drawn once, so the material must not enable instancing.
//A debug picture usually lies exactly on the surfaces it describes, where it would fight the scene's own
//meshes for depth and flicker half out of sight; each vertex is pulled toward the camera by a small share
//of its distance (_DepthPullTowardCamera). Anything thinner than that share of its distance shows its
//hidden edges through it. A perspective camera scales the whole view-space position, which leaves the
//point where it was on screen; an orthographic one moves depth alone.
Shader "ArcaneOnyx/MGizmos/Vertex Color Unlit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _DepthPullTowardCamera ("Share of the distance pulled toward the camera", Range(0, 0.01)) = 0.002
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            float _DepthPullTowardCamera;

            v2f vert (appdata v)
            {
                v2f o;
                float3 viewPosition = UnityObjectToViewPos(v.vertex.xyz);
                float keptShare = 1.0 - _DepthPullTowardCamera;
                bool isOrthographic = unity_OrthoParams.w == 1.0;
                viewPosition = isOrthographic ? float3(viewPosition.xy, viewPosition.z * keptShare) : viewPosition * keptShare;
                o.vertex = mul(UNITY_MATRIX_P, float4(viewPosition, 1.0));
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
