/* 
 * Original file ColorCorrectionLut2D produced by Unity Technologies.
 * Modified by Paulius LIEKIS to work with 2D textures instead of 3D. This 
 * allows it to be used on mobiles.
 */
using UnityEngine;
using System.Collections;


namespace UnityStandardAssets.ImageEffects {
	
	[ExecuteInEditMode]
	[AddComponentMenu ("Image Effects/Color Adjustments/Color Correction (2D Lookup Texture)")]

	public class ColorCorrectionLut2D : PostEffectsBase {
		
		public Shader shader;
		private Material material;
		
		public Texture2D sourceLut = null;
		
		// serialize this instead of having another 2d texture ref'ed
		// this texture contains 256 squares of size (sourceLut.height x sourceLut.height), i.e.
		// filtering of RG channels will happen by sampling pixels in one of these squares 
		// and filtering of B channel happens because we sample appropriate square
		public Texture2D converted2DLut = null;
		public string basedOnTempTex = "";
		
		public override bool CheckResources (){		
			CheckSupport (false);
			
			material = CheckShaderAndCreateMaterial (shader, material);
			
			if(!isSupported)
				ReportAutoDisable ();
			return isSupported;			
		}
		
		void  OnDisable (){
			if (material) {
				DestroyImmediate (material);
				material = null;
			}
		}
		
		void  OnDestroy (){
			if (converted2DLut)
				DestroyImmediate (converted2DLut);
			converted2DLut = null;
		}
		
		public void  SetIdentityLut (){
			int dim = 16;
			Color[] newC = new Color[dim*dim*dim*dim];
			float oneOverDim = 1.0f / (1.0f * dim - 1.0f);
			
			for(int i = 0; i < dim; i++) {
				for(int j = 0; j < dim; j++) {
					for(int x = 0; x < dim; x++) {
						for(int y = 0; y < dim; y++) 
						{
							newC[x + i * dim + y * dim * dim + j * dim * dim * dim] = 
								new Color(x * oneOverDim, y * oneOverDim, (j * dim + i) / (dim * dim - 1.0f), 1);
						}
					}
				}
			}
			
			if (converted2DLut)
				DestroyImmediate (converted2DLut);
			converted2DLut = new Texture2D (dim * dim, dim * dim, TextureFormat.ARGB32, false);
			converted2DLut.SetPixels (newC);
			converted2DLut.Apply ();
			basedOnTempTex = "";		
		}
		
		public bool ValidDimensions ( Texture2D tex2d  ){
			if (!tex2d) return false;
			int h = tex2d.height;
			if (h != Mathf.FloorToInt(Mathf.Sqrt(tex2d.width))) {
				return false;				
			}
			// we do not support other sizes than 256x16 
			if (h != 16) {
				return false;				
			}
			return true;
		}
		
		public void  Convert ( Texture2D temp2DTex ,   string path  ){
			
			// conversion fun: the given 2D texture needs to be of the format
			//  w * h, wheras h is the 'depth' (or 3d dimension 'dim') and w = dim * dim
			
			if (temp2DTex) {
				int dim = temp2DTex.width * temp2DTex.height;
				dim = temp2DTex.height;
				
				if (!ValidDimensions(temp2DTex)) {
					Debug.LogWarning ("The given 2D texture " + temp2DTex.name + " cannot be used as a 3D LUT.");				
					basedOnTempTex = "";
					return;				
				}
				
				Color[] c = temp2DTex.GetPixels();
				Color[] newC = new Color[dim * dim * dim * dim];
				
				for(int i = 0; i < dim; i++) {
					for(int j = 0; j < dim; j++) 
					{
						for(int x = 0; x < dim; x++) {
							for(int y = 0; y < dim; y++) 
							{
								float b = (i + j * dim * 1.0f) / dim;
								int bi0 = Mathf.FloorToInt(b);
								int bi1 = Mathf.Min(bi0 + 1, dim - 1);
								float f = b - bi0;
								
								int index = x + (dim - y - 1) * dim * dim;
								// perform filtering of B channel in code
								Color col1 = c[index + bi0 * dim];
								Color col2 = c[index + bi1 * dim];
								
								newC[x + i * dim + y * dim * dim + j * dim * dim * dim] = 
									Color.Lerp(col1, col2, f);
							}
						}
					}
				}
				
				if (converted2DLut)
					DestroyImmediate (converted2DLut);
				converted2DLut = new Texture2D (dim * dim, dim * dim, TextureFormat.ARGB32, false);
				converted2DLut.SetPixels (newC);
				converted2DLut.Apply ();
				basedOnTempTex = path;
			}		
			else {
				// error, something went terribly wrong
				Debug.LogError ("Couldn't color correct with 2D LUT texture. Image Effect will be disabled.");
			}		
		}
		
		void  OnRenderImage ( RenderTexture source ,   RenderTexture destination  ){	
			if(CheckResources () == false) {
				Graphics.Blit (source, destination);
				return;
			}
			
			if (converted2DLut == null) 
			{
				if (sourceLut == null)
					SetIdentityLut ();
				else
					Convert(sourceLut, "");
			}
			
			float lutSize = converted2DLut.width;
			float lutSquare = Mathf.Sqrt(lutSize);
			
			converted2DLut.wrapMode = TextureWrapMode.Clamp;
			material.SetFloat("_ScaleRG", (lutSquare - 1) / lutSize);
			material.SetFloat("_Dim", lutSquare);
			material.SetFloat("_Offset", 1 / (2 * lutSize));		
			material.SetTexture("_LutTex", converted2DLut);
			
			Graphics.Blit (source, destination, material, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);			
		}
	}
}