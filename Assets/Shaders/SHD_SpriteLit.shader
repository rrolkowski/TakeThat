Shader "Custom/URP/SpriteLit3D_Cutout_LitPlusShadowCaster"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color("Tint", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.25
        _Smoothness("Smoothness", Range(0,1)) = 0.1
        _SpecularStrength("Specular Strength", Range(0,2)) = 0.5
        _NormalSign("Normal Sign", Float) = 1.0

        // Na Z-fighting w stosie (domyślnie 0 = wyłączone)
        _DepthOffset("Depth Offset", Range(-0.01, 0.01)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "DisableBatching"="True"   // <<< KLUCZOWE: rozbija batching dla tego shadera
        }

        Cull Off
        ZWrite On
        ZTest LEqual

        // ========= FORWARD LIT =========
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Cutoff;
                float  _Smoothness;
                float  _SpecularStrength;
                float  _NormalSign;
                float  _DepthOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float  fogCoord   : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;

                // opcjonalny anti z-fight
                OUT.positionCS.z += _DepthOffset * OUT.positionCS.w;

                OUT.positionWS = pos.positionWS;

                float3 normalOS = float3(0, 0, -1) * _NormalSign;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(normalOS));

                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;

                OUT.fogCoord = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            float3 SpecularBlinnPhong(float3 N, float3 V, float3 L, float smoothness, float strength)
            {
                float3 H = normalize(L + V);
                float ndh = saturate(dot(N, H));
                float exp = lerp(8.0, 256.0, smoothness);
                float s = pow(ndh, exp) * strength;
                return s.xxx;
            }

            float3 SpecularTwoSided(float3 N, float3 V, float3 L, float smoothness, float strength)
            {
                float3 s1 = SpecularBlinnPhong( N, V, L, smoothness, strength);
                float3 s2 = SpecularBlinnPhong(-N, V, L, smoothness, strength);
                return max(s1, s2);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 albedo = tex * IN.color;

                // alpha cutout
                clip(albedo.a - _Cutoff);

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // (Nawet w Unlit ten shader nadal jest używany — różnice mogą wynikać z depth/batch, nie z light.)
                float3 ambient = SampleSH(N);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 Lm = normalize(mainLight.direction);

                float ndl = abs(dot(N, Lm));
                float3 diffuse = ndl * mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                float3 spec = SpecularTwoSided(N, V, Lm, _Smoothness, _SpecularStrength) * mainLight.color
                            * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint i = 0; i < count; i++)
                {
                    Light l = GetAdditionalLight(i, IN.positionWS);
                    float3 L = normalize(l.direction);

                    float ndl2 = abs(dot(N, L));
                    diffuse += ndl2 * l.color * l.distanceAttenuation * l.shadowAttenuation;

                    spec += SpecularTwoSided(N, V, L, _Smoothness, _SpecularStrength) * l.color
                          * l.distanceAttenuation * l.shadowAttenuation;
                }
                #endif

                float3 col = albedo.rgb * (ambient + diffuse) + spec;
                col = MixFog(col, IN.fogCoord);

                return half4(col, albedo.a);
            }
            ENDHLSL
        }

        // ========= SHADOW CASTER (prosty, bez tangentOS) =========
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertSC
            #pragma fragment fragSC

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Cutoff;
                float  _Smoothness;
                float  _SpecularStrength;
                float  _NormalSign;
                float  _DepthOffset;
            CBUFFER_END

            struct AttributesSC
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct VaryingsSC
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : TEXCOORD1;
            };

            VaryingsSC vertSC(AttributesSC IN)
            {
                VaryingsSC OUT;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Unity w pass ShadowCaster ustawia macierze pod shadowmapę,
                // więc TransformWorldToHClip działa jako "do shadow clip".
                OUT.positionCS = TransformWorldToHClip(positionWS);

                OUT.positionCS.z += _DepthOffset * OUT.positionCS.w;

                return OUT;
            }

            half4 fragSC(VaryingsSC IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float a = tex.a * IN.color.a;
                clip(a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
