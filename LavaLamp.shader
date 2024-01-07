Shader "Unlit/LavaLamp"
{
    Properties
    {
        _LampSize("Lamp Size", Vector) = (1.0, 1.0, 0.0, 0.0)
        _TopPercent("Top Percent", Float) = 1.0
        _Threshold("Threshold", Float) = 1.0
        _AntiAliasing("Anti-Aliasing", Float) = 0.1

        [Header(Lighting)]
        _FresnelColor("FresnelColor", Color) = (1, 1, 1, 1)
        _FresnelDistance("Fresnel Distance", Float) = 0.1
        _FresnelPower("Fresnel Power", Float) = 2
        _Specular("Specular", Float) = 2
        _SpecularPower("Specular Power", Float) = 2
        _SpecularOpacity("Specular Opacity", Float) = 0.5

        [Header(Colors)]
        _DefaultColor("Default Color", Color) = (1, 1, 1, 1)
        _TopColor("Top Color", Color) = (0, 0, 1, 1)
        _BottomColor("Bottom Color", Color) = (1, 0, 1, 1)
        _GradientStrength("Gradient Strength", Float) = 1

    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float2 _LampSize;

            float _TopPercent;
            float _Threshold;
            float _AntiAliasing;

            // lighting
            float4 _FresnelColor;
            float _FresnelDistance;
            float _FresnelPower;
            float _Specular;
            float _SpecularPower;
            float _SpecularOpacity;

            float _BlobPositions[20];
            float _BlobWeights[10];

            // colors
            float4 _DefaultColor;
            float4 _TopColor;
            float4 _BottomColor;
            float _GradientStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv.x = o.uv.x - 0.5f;

                return o;
            }

            float GetValue(float distance, float radius) {

                float x = distance * 0.707f / radius;
                return x < 0.707f ? 4 * (x * x * x * x - x * x + 0.25f) : 0;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                if (1 - input.uv.y * (1 - _TopPercent) < abs(input.uv.x * 2))
                    discard;

                float2 coords = input.uv * _LampSize;

                float value = 0;
                for (int i = 0; i < 10; i++) {

                    float2 displacement = float2(_BlobPositions[2 * i] - coords.x, _BlobPositions[2*i + 1] - coords.y);
                    value += GetValue(sqrt(dot(displacement, displacement)), _BlobWeights[i]);
                }

                // expand blob near top
                float topValue = saturate((1 - input.uv.y * 3) * _Threshold);
                value += topValue * topValue;

                // expand blob near bottom
                float bottomValue = saturate((input.uv.y * 3 - 2) * _Threshold);
                value += bottomValue * bottomValue;

                float2 gradient = float2(ddx(value), ddy(value));
                float mask = smoothstep(_Threshold - _AntiAliasing, _Threshold + _AntiAliasing, value);

                // fresnel
                float fresnel = clamp(value - _Threshold, 0, _FresnelDistance) / _FresnelDistance;
                fresnel = pow(1.01f - fresnel, _FresnelPower);

                // specular
                float2 specularDirection = float2(0.707, 0.707);
                float specularSpot = 1 - saturate(length(specularDirection + gradient));
                float specular = saturate(pow(specularSpot * _Specular / value, _SpecularPower)) * _SpecularOpacity;
                
                // diffuse
                float colorGradient = (0.5f * gradient.y * _GradientStrength + 0.5f);

                _TopColor *= 0.5f * input.uv.y + 0.5f;
                _BottomColor *= 0.5f * (1 - input.uv.y) + 0.5f;

                float4 diffuse = lerp(_TopColor, _BottomColor, saturate(colorGradient));
                diffuse = _DefaultColor * (1 - diffuse.a) + diffuse * diffuse.a;

                return mask * (diffuse + fresnel * _FresnelColor * _FresnelColor.a + specular);
            }
            ENDCG
        }
    }
}
