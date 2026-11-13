Shader "SWG/UI/Panel Surface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _OuterColor ("Outer / bottom", Color) = (0, .098, .118, 1)
        _MiddleColor ("Middle", Color) = (0, .169, .216, 1)
        _InnerColor ("Inner / top", Color) = (0, .188, .243, 1)
        [Toggle] _SquareGradient ("Square radial gradient", Float) = 1
        _EdgeTransition ("Edge transition", Range(.01, 1)) = .1
        _BandColor ("Band color", Color) = (0, .24, .28, 1)
        _Bands ("Outward moving bands", Range(0, 1)) = 0
        _BandCount ("Bands per half", Float) = 18
        _BandSpeed ("Bands per second", Float) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _OuterColor, _MiddleColor, _InnerColor, _BandColor;
            float _SquareGradient, _EdgeTransition, _Bands, _BandCount, _BandSpeed;
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local=v.vertex;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.color=v.color;
                o.uv=v.uv;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 distance=abs(i.uv*2-1);
                float inward=1-max(distance.x,distance.y);
                fixed4 radial=lerp(_OuterColor,_MiddleColor,saturate(inward/_EdgeTransition));
                radial=lerp(radial,_InnerColor,saturate((inward-_EdgeTransition)/(1-_EdgeTransition)));
                fixed4 color=lerp(lerp(_OuterColor,_InnerColor,i.uv.y),radial,_SquareGradient);
                float phase=frac(distance.y*_BandCount-_Time.y*_BandSpeed);
                float aa=max(fwidth(phase),.01);
                float band=smoothstep(.05,.05+aa,phase)*(1-smoothstep(.48,.48+aa,phase));
                color.rgb+=_BandColor.rgb*band*_Bands*smoothstep(0,.2,distance.y)*(1-smoothstep(.85,1,distance.y));
                color*=i.color*tex2D(_MainTex,i.uv);
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
