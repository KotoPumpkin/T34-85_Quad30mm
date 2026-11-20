using System;
using System.Collections.Generic;
using GHPC;
using UnityEngine;
using U=UnityEngine.Object;
namespace T3485Quad30 {
 public sealed class QuadMountState:MonoBehaviour {
  public Transform Plate;
  public MeshRenderer[] Guns;
  public int RemovedTriangles;
  public float EmbeddedMetres;
 }
 internal static class QuadGeometry {
  public const float FrontOffset=0.17f,ReceiverEmbed=0.015f;
  public const float MountWidth=1.25f,MountHeight=0.52f,MountDepth=0.23f,MountCenterOffset=FrontOffset-MountDepth*0.5f;
  static readonly HashSet<string> LegacyNames=new HashSet<string>(StringComparer.Ordinal){"gun barrel","gun barrel armor","mantlet","mantlet plate","mantlet armor","gun breech","gun breech AAR","gun assembly"};
  public static bool IsLegacyPart(string name){return name!=null&&LegacyNames.Contains(name);}
  public static float MuzzleOffset(float receiverRear,float face,float embed){return face-embed-receiverRear;}
  public static int[] RetainTriangles(int[] source,bool[] remove){var keep=new List<int>(source.Length);for(int i=0;i<source.Length;i+=3)if(!remove[source[i]]&&!remove[source[i+1]]&&!remove[source[i+2]]){keep.Add(source[i]);keep.Add(source[i+1]);keep.Add(source[i+2]);}return keep.ToArray();}
  static float Influence(BoneWeight w,int bone){return (w.boneIndex0==bone?w.weight0:0f)+(w.boneIndex1==bone?w.weight1:0f)+(w.boneIndex2==bone?w.weight2:0f)+(w.boneIndex3==bone?w.weight3:0f);}
  public static void RemoveLegacy(GameObject root){
   var skin=R.Required(R.Find(root.transform,"lp_turret_cast001").GetComponent<SkinnedMeshRenderer>(),"T-34 turret skin missing");var source=R.Required(skin.sharedMesh,"T-34 turret mesh missing");if(!source.isReadable)throw new InvalidOperationException("Cannot remove legacy85mm faces from unreadable turret mesh");
   var weights=source.boneWeights;var remove=new bool[source.vertexCount];bool found=false;
   for(int bone=0;bone<skin.bones.Length;bone++)if(skin.bones[bone]!=null&&(skin.bones[bone].name=="MANTLET"||skin.bones[bone].name=="MUZZLE")){found=true;for(int i=0;i<weights.Length;i++)if(Influence(weights[i],bone)>0.001f)remove[i]=true;}
   if(!found)throw new InvalidOperationException("Legacy main-gun bones not found");
   var own=U.Instantiate(source);own.name="T3485Q30MOD_turret_without_old_gun";int removed=0;
   for(int sub=0;sub<source.subMeshCount;sub++){var triangles=source.GetTriangles(sub);var retained=RetainTriangles(triangles,remove);removed+=(triangles.Length-retained.Length)/3;own.SetTriangles(retained,sub,false);}
   if(removed<20)throw new InvalidOperationException("Legacy85mm triangle removal found too few faces");own.hideFlags=HideFlags.DontUnloadUnusedAsset;own.RecalculateBounds();skin.sharedMesh=own;
   var state=root.AddComponent<QuadMountState>();state.RemovedTriangles=removed;state.EmbeddedMetres=ReceiverEmbed;
   foreach(Transform part in root.GetComponentsInChildren<Transform>(true)){
    if(part.GetComponent<AimablePlatform>()!=null)continue;
    if(IsLegacyPart(part.name)){foreach(var c in part.GetComponents<Collider>())c.enabled=false;foreach(var r in part.GetComponents<Renderer>())r.enabled=false;part.gameObject.SetActive(false);}
   }
   // Damage/armor meshes remain operational, but do not become exterior artwork.
   foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))if(r.gameObject.layer==8||r.GetComponent<UniformArmor>()!=null||r.GetComponent<VariableArmor>()!=null)r.enabled=false;
  }
  public static Transform AddMount(GameObject root,Quaternion orientation){
   var pivot=R.Required(R.Find(root.transform,"MANTLET"),"Quad elevation pivot missing");pivot.gameObject.SetActive(true);
   var state=R.Required(root.GetComponent<QuadMountState>(),"Quad geometry state missing");
   var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="T3485Q30MOD_QuadMountPlate";plate.transform.SetParent(pivot,false);plate.transform.SetPositionAndRotation(pivot.position+orientation*new Vector3(0,0,MountCenterOffset),orientation);plate.transform.localScale=new Vector3(MountWidth,MountHeight,MountDepth);
   ShieldPaint.Install(root,plate);state.Plate=plate.transform;return pivot;
  }
  public static string Audit(GameObject root){
   var state=R.Required(root.GetComponent<QuadMountState>(),"Quad geometry state missing");var pivot=R.Required(R.Find(root.transform,"MANTLET"),"Quad pivot missing");if(!pivot.gameObject.activeSelf)throw new InvalidOperationException("Legacy cleanup disabled live elevation pivot");if(state.Plate==null||state.Guns==null||state.Guns.Length!=4||state.RemovedTriangles<20)throw new InvalidOperationException("Quad mount geometry is incomplete");var scale=state.Plate.localScale;if(Mathf.Abs(scale.x-MountWidth)>0.002f||Mathf.Abs(scale.y-MountHeight)>0.002f||Mathf.Abs(scale.z-MountDepth)>0.002f)throw new InvalidOperationException("Quad breech plate is not using compact dimensions: "+scale);
   string paintAudit=ShieldPaint.Audit(state.Plate);
   foreach(var gun in state.Guns){if(gun==null||!gun.enabled||!gun.gameObject.activeSelf||!gun.transform.IsChildOf(pivot))throw new InvalidOperationException("A quad cannon was hidden with legacy geometry");for(Transform t=gun.transform;t!=root.transform;t=t.parent)if(t==null||!t.gameObject.activeSelf)throw new InvalidOperationException("Quad gun has an inactive ancestor");var mesh=gun.GetComponent<MeshFilter>().sharedMesh;Vector3 rear=gun.transform.TransformPoint(new Vector3(mesh.bounds.center.x,mesh.bounds.center.y,mesh.bounds.min.z));Vector3 platePoint=state.Plate.TransformPoint(new Vector3(0,0,0.5f));float gap=Vector3.Dot(rear-platePoint,state.Plate.forward);if(Mathf.Abs(gap+ReceiverEmbed)>0.002f)throw new InvalidOperationException("Quad receiver does not contact plate: "+gap);foreach(var material in gun.sharedMaterials)if(material==null||material.shader==null||!material.shader.isSupported)throw new InvalidOperationException("Quad gun has a missing/unsupported material");}
   foreach(Transform part in root.GetComponentsInChildren<Transform>(true))if(IsLegacyPart(part.name)&&part.GetComponent<AimablePlatform>()==null&&part.gameObject.activeSelf)throw new InvalidOperationException("Old85mm part still active: "+part.name);
   var turretArmor=R.Required(R.Find(root.transform,"T34_Turret_armour"),"Turret armor missing");if(!turretArmor.gameObject.activeSelf)throw new InvalidOperationException("Turret armor was disabled by visual cleanup");
   return "PASS quad mounting: old85mm mantle/barrel/breech faces removed; live MANTLET and turret armor retained; compact 1.25x0.52x0.23m breech plate; horizontal barrel offsets outer0.40m/inner0.22m; four receivers seated15mm into cropped original T34 paint mounting plate; no missing gun materials."+Environment.NewLine+paintAudit;
  }
 }
}
