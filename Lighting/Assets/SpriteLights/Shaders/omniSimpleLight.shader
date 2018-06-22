Shader "Lights/OmniSimple"{

    Properties{

        _MainTex ("Light Texture", 2D) = "white" {} 
		[HDR]_FrontColor ("Front Color", Color) = (0.5,0.5,0.5,0.5)
        _MinPixelSize ("Minimum screen size", FLOAT) = 5.0
        _Attenuation ("Attenuation", Range(0.01, 1)) = 0.37
        _BrightnessOffset ("Brightness offset", Range(-1, 1)) = 0
    }	

    SubShader{

        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha One
        AlphaTest Greater .01
        ColorMask RGB
        Lighting Off ZWrite Off   
		
        Pass{

            CGPROGRAM
            
            #include "UnityCG.cginc"
            #include "lightFunctions.cginc"

            #pragma vertex vert  
            #pragma fragment frag          
            #pragma multi_compile_fog //Enable fog
            #pragma glsl_no_auto_normalization
          //  #pragma enable_d3d11_debug_symbols //For debugging.
  
            uniform sampler2D _MainTex;        
            float _MinPixelSize;
            half _BrightnessOffset;
            float _Attenuation;
			half4 _FrontColor;

            //These global variables are set from a Unity script.
            float _ScaleFactor; 

            struct vertexInput {

                float4 center : POSITION; //Mesh center position is stored in the position channel (vertices in Unity).
                float4 corner : TANGENT; //Mesh corner is stored in the tangent channel (tangent in Unity). The scale is stored in the w component.
                float2 uvs : TEXCOORD0; //Texture coordinates (uv in Unity).        
            };

            struct vertexOutput{

                float4 pos : SV_POSITION;
                float2 uvs : TEXCOORD0;
                half4 color : COLOR;

                //This is not a UV coordinate but it is just used to pass some variables
                //from the vertex shader to the fragment shader. x = gain.
                float2 container : TEXCOORD1;

                //Enable fog.
                UNITY_FOG_COORDS(2)
            };			

            vertexOutput vert(vertexInput input){

                vertexOutput output;
                half gain;
                half distanceGain;
                float scale;

                //Get a vector from the vertex to the camera and cache the result.
                float3 objSpaceViewDir = ObjSpaceViewDir(input.center);

                //Get the distance between the camera and the light.
                float distance = length(objSpaceViewDir);    

                output.color = _FrontColor;

                //Calculate the scale. If the light size is smaller than one pixel, scale it up
                //so it remains at least one pixel in size.
                scale = ScaleUp(distance, _ScaleFactor, input.corner.w, 1.0f, _MinPixelSize);

                //Get the vertex offset to shift and scale the light.
                float4 offset = GetOffset(scale, input.corner);

                //Place the vertex by moving it away from the center.
                //Rotate the billboard towards the camera.
				output.pos = mul(UNITY_MATRIX_P, float4(UnityObjectToViewPos(input.center), 1.0f) + offset);

                //Far away lights should be less bright. Attenuate with the inverse square law.
                distanceGain = Attenuate(distance, _Attenuation);

                //Merge the distance gain (attenuation), and light brightness into a single gain value.
				gain = (_BrightnessOffset - (1.0h - distanceGain));

                //Send the gain to the fragment shader.
                output.container = float2(gain, 0.0f);

                //UV mapping.
                output.uvs = input.uvs;

                //Enable fog.
                UNITY_TRANSFER_FOG(output, output.pos);

                return output;
            }

            half4 frag(vertexOutput input) : COLOR{

                //Compute the final color.
                //Note: input.container.x fetches the gain from the vertex shader. No need to calculate this for each fragment.
				half4 col = 2.0h * input.color * tex2D(_MainTex, input.uvs) * (exp(input.container.x * 5.0h));		
                
                //Enable fog. Use black due to the blend mode used.		
				UNITY_APPLY_FOG_COLOR(input.fogCoord, col, half4(0,0,0,0)); 
                	
				return col;
            }

	        ENDCG
        }        
    }
	
    FallBack "Diffuse"
}
