Shader "TPSBR/UI/CircleMask"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Tint",    Color) = (1,1,1,1)
        _OutlineColor("OutlineColor", Color) = (1,1,1,1)

        _OutlineWidth("OutlineWidth", Float) = 1
        _CircleRadius("CircleRadius", Float) = 1
        _CircleCenter("CircleCenter", Vector) = (0,0,0,0)

        _CutoutRadius("CutoutRadius", Float) = 1
        _CutoutCenter("CutoutCenter", Vector) = (0,0,0,0)

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
    }

        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
                "PreviewType" = "Plane"
                "CanUseSpriteAtlas" = "True"
            }

            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }

            Cull Off
            Lighting Off
            ZWrite Off
            ZTest[unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                Name "Default"
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #include "UnityCG.cginc"
                #include "UnityUI.cginc"

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    float2 texcoord  : TEXCOORD0;
                };

                fixed4 _Color;
                fixed4 _OutlineColor;
                float2 _CircleCenter;
                float  _CircleRadius;
                float  _OutlineWidth;

                float2 _CutoutCenter;
                float  _CutoutRadius;

                v2f vert(appdata_t v)
                {
                    v2f OUT;
                    OUT.vertex = UnityObjectToClipPos(v.vertex);

                    OUT.texcoord = v.texcoord;

                    return OUT;
                }

                fixed4 frag(v2f IN) : SV_Target
                {
                    if (_CutoutRadius < 1)
                    {
                        float cutoutDistance = distance(IN.texcoord, _CutoutCenter);
                        if (cutoutDistance > _CutoutRadius)
                        {
                            return (0, 0, 0, 0);
                        }
                    }

                    float dist = distance(IN.texcoord, _CircleCenter);

                    if (dist > _CircleRadius + _OutlineWidth)
                    {
                        return _Color;
                    }
                    else if (dist > _CircleRadius)
                    {
                        return _OutlineColor;
                    }

                    return (0, 0, 0, 0);
                }
            ENDCG
            }
        }
}