Shader "Custom/URPWorldFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.05, 0.05, 0.1, 0.95) // Dark blueish-black
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
            CBUFFER_END

            // We cap the GPU array at 100 points for mobile performance
            float4 _RevealedPoints[100]; 
            int _PointCount;
            float _RevealRadius;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float alpha = _FogColor.a;
                float softness = 5.0; // Creates a 5-meter soft fade edge

                // Loop through our list of points
                for(int i = 0; i < _PointCount; i++)
                {
                    // Calculate distance on the flat ground (ignoring Y height)
                    float dist = distance(input.positionWS.xz, _RevealedPoints[i].xz);
                    
                    if (dist < _RevealRadius)
                    {
                        alpha = 0.0; // Fully transparent hole
                    }
                    else if (dist < _RevealRadius + softness)
                    {
                        // Smoothly fade the edge of the circle
                        float fade = (dist - _RevealRadius) / softness;
                        alpha = min(alpha, fade);
                    }
                }

                return half4(_FogColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}