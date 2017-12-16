Shader "Custom/ExplosionWarpShader"
{
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader {
	Cull Off ZWrite Off ZTest Always    
	
		Pass {
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed _DisplacementPower;

            fixed _Waves;
            fixed _Ratio;
            fixed _TimeMultiplier;
 


            int _Work;
            int _NegativeColors;
            
            int _TimeUnscaled;
            

			float4 frag(v2f_img i) : SV_Target {
			    if(_Work == 1) {
			        fixed xDis = _Waves * i.uv.x / _Ratio + _Time * _TimeMultiplier;
			        fixed yDis = _Waves * i.uv.y + _Time * _TimeMultiplier;
                    fixed2 displacementVector = fixed2(sin(xDis), cos(yDis));

                    fixed2 uv_distorted = i.uv + _DisplacementPower * displacementVector.xy;

                    fixed4 color = tex2D(_MainTex, uv_distorted.xy);
                    if(_NegativeColors == 1)
                        color = 1 - color;
                        
                        
                        
                    return color;
				} else {
				    fixed4 color = tex2D(_MainTex, i.uv);
				    if(_NegativeColors == 1)
                        color = 1 - color;

				    return color;
				}
			}
			ENDCG
		}
	}
}