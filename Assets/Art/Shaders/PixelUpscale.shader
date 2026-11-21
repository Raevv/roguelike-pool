// Shows the low-res world render at any (non-integer) scale with crisp, even pixels ("sharp bilinear"):
// inside each art pixel the texture is sampled at the texel center, and only the last screen pixel
// next to a texel edge is blended with the neighbour (needs a Bilinear texture). Used by the RawImage
// of PixelCameraRig. With _KeepAlpha (and Blend One OneMinusSrcAlpha) it also composites a transparent,
// premultiplied overlay render (the cue drawn above some UI elements).
Shader "Hidden/RoguelikePool/PixelUpscale"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _KeepAlpha ("Keep Alpha (premultiplied overlay)", Float) = 0
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _KeepAlpha;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 t = i.uv * _MainTex_TexelSize.zw;   // position in texels
                float2 a = max(fwidth(t), 1e-4);           // texels per screen pixel
                float2 f = frac(t);
                // 0.5 (texel center) everywhere except within half a screen pixel of a texel edge:
                // each edge is antialiased over exactly one screen pixel.
                float2 g = clamp(f / a, 0.0, 0.5) + clamp((f - 1.0) / a + 0.5, 0.0, 0.5);
                fixed4 c = tex2D(_MainTex, (floor(t) + g) * _MainTex_TexelSize.xy);
                if (_KeepAlpha < 0.5) c.a = 1.0; // the world render is opaque; an overlay keeps its alpha
                return c;
            }
            ENDCG
        }
    }
}
