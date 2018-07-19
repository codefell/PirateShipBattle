
//	VertexDirt plug-in for Unity
//	Copyright 2014-2017, Zoltan Farago, All rights reserved.

Shader "Hidden/VD-AMBIENTOCCLUSION" {
	SubShader { Pass {
		cull off
		fog {mode off}
		CGPROGRAM 
		#pragma vertex vert 
		#pragma fragment frag
		half4 _VDOccluderColor;
		half4 vert(half4 vertexPos : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertexPos); }
		half4 frag(void) : COLOR { return _VDOccluderColor; }
		ENDCG
	} }
}
