Shader "Hidden/Pixelation"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{

        Cull Off ZWrite Off ZTest Always
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
            float2 chunkSize;
            float2 chunkCount;            

			fixed4 frag (v2f_img i) : SV_Target
			{
			    float2 chunkPosition = floor(i.uv * chunkCount);
			    float2 samplingCoord = (chunkPosition + 0.5) * chunkSize;
			
				fixed4 col = tex2D(_MainTex, samplingCoord);
				return col;
			}
			ENDCG
		}
	}
}
