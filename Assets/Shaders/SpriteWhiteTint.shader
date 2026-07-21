Shader "Custom/SpriteWhiteTint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _WhiteThreshold ("White Threshold", Range(0.0, 1.0)) = 0.55
        _WhiteTolerance ("White Tolerance", Range(0.01, 0.5)) = 0.15
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaCutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _RendererColor;
            sampler2D _MainTex;
            half _WhiteThreshold;
            half _WhiteTolerance;

            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                fixed4 origColor = tex2D(_MainTex, IN.texcoord);

                // Calculate brightness & saturation of original sprite pixel
                float minRGB = min(origColor.r, min(origColor.g, origColor.b));
                float maxRGB = max(origColor.r, max(origColor.g, origColor.b));
                float saturation = (maxRGB - minRGB) / (maxRGB + 0.0001);
                
                // Measure how "white / grayscale bright" the original pixel is
                float whiteFactor = minRGB * (1.0 - saturation);
                
                // Smooth step for clean thresholding between white shirt vs skin/outlines
                float tintMask = smoothstep(_WhiteThreshold - _WhiteTolerance, _WhiteThreshold + _WhiteTolerance, whiteFactor);

                // Apply tint color IN.color only to the white parts of the sprite
                fixed3 tintedRGB = origColor.rgb * IN.color.rgb;
                fixed3 finalRGB = lerp(origColor.rgb, tintedRGB, tintMask);
                
                fixed4 c;
                c.rgb = finalRGB * origColor.a * IN.color.a;
                c.a = origColor.a * IN.color.a;

                return c;
            }
        ENDCG
        }
    }
}
