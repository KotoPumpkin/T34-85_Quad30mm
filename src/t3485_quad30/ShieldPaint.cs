using System;
using System.Collections.Generic;
using GHPC;
using UnityEngine;

namespace T3485Quad30 {
 public sealed class ShieldPaintState:MonoBehaviour {
  public Material SourceMaterial;
  public string[] Properties;
  public Texture2D[] SourceTextures;
  public Texture2D[] CroppedTextures;
 }
 internal static class ShieldPaint {
  static readonly string[] AtlasProperties={"_Albedo","_Normal","_Occlusion","_Smoothness","_PaintMask"};
  public static int[] CropRegion(int width,int height){
   if(width<=0||height<=0||width%128!=0||height%32!=0)throw new ArgumentException("Unsupported T34 atlas dimensions");
   var region=new[]{width*5/128,height*3/16,width/32,height/32};
   foreach(int value in region)if(value%4!=0)throw new ArgumentException("T34 paint patch must align with compressed 4x4 texture blocks");
   if(region[2]<4||region[3]<4)throw new ArgumentException("T34 paint patch is too small");
   return region;
  }
  static Texture2D Crop(Texture2D source,string property){
   var r=CropRegion(source.width,source.height);
   // Copy native compressed blocks verbatim, retaining sRGB/linear interpretation.
   // Never Apply after this GPU copy: CPU texture data would overwrite the crop.
   var patch=new Texture2D(r[2],r[3],source.format,false,!source.isDataSRGB);
   patch.name="T3485Q30MOD_ShieldPatch"+property;patch.hideFlags=HideFlags.DontUnloadUnusedAsset;
   patch.filterMode=FilterMode.Bilinear;patch.wrapMode=TextureWrapMode.Repeat;patch.anisoLevel=1;
   Graphics.CopyTexture(source,0,0,r[0],r[1],r[2],r[3],patch,0,0,0,0);
   return patch;
  }
  public static void Install(GameObject root,GameObject plate){
   var skin=R.Required(R.Find(root.transform,"lp_turret_cast001").GetComponent<SkinnedMeshRenderer>(),"Original T34 visible turret renderer missing");
   var donor=R.Required(skin.sharedMaterial,"Original T34 exterior material missing");
   if(!donor.HasProperty("_Albedo"))throw new InvalidOperationException("T34 exterior material has no native albedo");
   var material=new Material(donor);material.name="T3485Q30MOD_T34_CroppedPaint";material.hideFlags=HideFlags.DontUnloadUnusedAsset;
   var properties=new List<string>();var sources=new List<Texture2D>();var crops=new List<Texture2D>();
   foreach(string property in AtlasProperties){
    if(!donor.HasProperty(property))continue;
    var source=donor.GetTexture(property) as Texture2D;if(source==null)throw new InvalidOperationException("T34 exterior atlas is missing "+property);
    var patch=Crop(source,property);material.SetTexture(property,patch);material.SetTextureScale(property,Vector2.one);material.SetTextureOffset(property,Vector2.zero);
    properties.Add(property);sources.Add(source);crops.Add(patch);
   }
   var state=plate.AddComponent<ShieldPaintState>();state.SourceMaterial=donor;state.Properties=properties.ToArray();state.SourceTextures=sources.ToArray();state.CroppedTextures=crops.ToArray();
   plate.GetComponent<MeshRenderer>().sharedMaterial=material;
  }
  public static string Audit(Transform plate){
   var state=R.Required(plate.GetComponent<ShieldPaintState>(),"Shield native texture crop state missing");
   var material=R.Required(plate.GetComponent<MeshRenderer>().sharedMaterial,"Shield material missing");
   if(material==state.SourceMaterial||material.shader!=state.SourceMaterial.shader||!material.shader.isSupported)throw new InvalidOperationException("Shield must own a copy of the original supported T34 exterior shader");
   if(state.Properties.Length!=5||state.CroppedTextures.Length!=5||state.SourceTextures.Length!=5)throw new InvalidOperationException("Shield must crop all five native paint maps");
   for(int i=0;i<state.Properties.Length;i++){
    string property=state.Properties[i];var source=state.SourceTextures[i];var patch=state.CroppedTextures[i];var r=CropRegion(source.width,source.height);
    if(patch==source||material.GetTexture(property)!=patch||patch.width!=r[2]||patch.height!=r[3]||patch.graphicsFormat!=source.graphicsFormat)throw new InvalidOperationException("Shield texture crop is missing, incorrectly sized or color converted: "+property);
    if(state.SourceMaterial.GetTexture(property)!=source)throw new InvalidOperationException("Original T34 texture was modified: "+property);
   }
   // All native shader settings and auxiliary textures stay unchanged; no RGB override.
   var shader=material.shader;
   for(int i=0;i<shader.GetPropertyCount();i++){
    string property=shader.GetPropertyName(i);var type=shader.GetPropertyType(i);
    if(type==UnityEngine.Rendering.ShaderPropertyType.Color&&material.GetColor(property)!=state.SourceMaterial.GetColor(property))throw new InvalidOperationException("Shield overrode native T34 color: "+property);
    if((type==UnityEngine.Rendering.ShaderPropertyType.Float||type==UnityEngine.Rendering.ShaderPropertyType.Range)&&material.GetFloat(property)!=state.SourceMaterial.GetFloat(property))throw new InvalidOperationException("Shield overrode native T34 shading: "+property);
   }
   var uv=plate.GetComponent<MeshFilter>().sharedMesh.uv;float minX=1,maxX=0,minY=1,maxY=0;
   foreach(var p in uv){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
   if(maxX-minX<0.9f||maxY-minY<0.9f)throw new InvalidOperationException("Shield UVs collapsed to a single color");
   return "PASS shield paint: original visible T34 exterior material; five native texture maps cropped at matching UV region; T34_co 64x64 pixels from (80,384); original shader/color/shading preserved; full UVs; source textures unchanged.";
  }
 }
}
