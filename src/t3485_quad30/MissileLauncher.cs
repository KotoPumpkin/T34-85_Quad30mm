using System;
using System.Collections.Generic;
using GHPC;
using GHPC.Camera;
using GHPC.Crew;
using GHPC.Weapons;
using GHPC.Equipment.Optics;
using GHPC.Utility;
using UnityEngine;
using U=UnityEngine.Object;
namespace T3485Quad30 {
 internal static class MissileLauncher {
  static Material[] NativeMountMaterials(GameObject target,GameObject assembly){
   var nativeTurret=R.Find(target.transform,"lp_turret_cast001");if(nativeTurret!=null){var targetRenderer=nativeTurret.GetComponent<Renderer>();if(targetRenderer!=null&&targetRenderer.sharedMaterials!=null&&targetRenderer.sharedMaterials.Length>0)return (Material[])targetRenderer.sharedMaterials.Clone();}
   var tripod=R.Find(assembly.transform,"tripod");
   if(tripod!=null){var r=tripod.GetComponent<MeshRenderer>();if(r!=null&&r.sharedMaterials!=null&&r.sharedMaterials.Length>0)return (Material[])r.sharedMaterials.Clone();}
   foreach(var r in assembly.GetComponentsInChildren<MeshRenderer>(true))if(r.sharedMaterials!=null&&r.sharedMaterials.Length>0)return (Material[])r.sharedMaterials.Clone();
   throw new InvalidOperationException("Native BMP-1P mount material missing");
  }
  static GameObject SupportPrimitive(string name,Transform parent,Material[] materials,Vector3 position,Vector3 scale){
   var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.identity;go.transform.localScale=scale;
   var collider=go.GetComponent<Collider>();if(collider!=null)U.DestroyImmediate(collider);
   var renderer=R.Required(go.GetComponent<MeshRenderer>(),"Generated launcher support renderer missing");renderer.sharedMaterials=materials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;return go;
  }
  static GameObject SupportBeam(string name,Transform parent,Material[] materials,Vector3 start,Vector3 end,float width){
   var go=SupportPrimitive(name,parent,materials,(start+end)*0.5f,new Vector3(width,(end-start).magnitude,width));go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,end-start);return go;
  }
  static void AddHatchSupport(Transform pedestal,GameObject target,GameObject assembly){
   var materials=NativeMountMaterials(target,assembly);
   SupportPrimitive("T3485Q30MOD_Hatch_Support_Base",pedestal,materials,new Vector3(0f,0.020f,0f),new Vector3(0.50f,0.06f,0.42f));
   SupportPrimitive("T3485Q30MOD_Hatch_Support_Column",pedestal,materials,new Vector3(0f,0.12f,0f),new Vector3(0.18f,0.16f,0.18f));
   SupportBeam("T3485Q30MOD_Hatch_Support_Left",pedestal,materials,new Vector3(-0.18f,0.05f,-0.02f),new Vector3(-0.09f,0.20f,-0.02f),0.055f);
   SupportBeam("T3485Q30MOD_Hatch_Support_Right",pedestal,materials,new Vector3(0.18f,0.05f,-0.02f),new Vector3(0.09f,0.20f,-0.02f),0.055f);
  }
  public static float TopSurfaceHeight(Vector3[] vertices,int[] triangles,bool[] accepted,float x,float z){
   float top=float.NegativeInfinity;
   for(int i=0;i<triangles.Length;i+=3){
    int ia=triangles[i],ib=triangles[i+1],ic=triangles[i+2];if(!accepted[ia]||!accepted[ib]||!accepted[ic])continue;
    Vector3 a=vertices[ia],b=vertices[ib],c=vertices[ic];float d=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(d)<0.0000001f)continue;
    float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/d,v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/d,w=1f-u-v;
    if(u>=-0.00001f&&v>=-0.00001f&&w>=-0.00001f)top=Mathf.Max(top,u*a.y+v*b.y+w*c.y);
   }
   if(float.IsNegativeInfinity(top))throw new InvalidOperationException("No hatch surface below launcher mount");return top;
  }
  public static Vector3 HatchSurfacePoint(GameObject target){
   var hatch=R.Required(R.Find(target.transform,"CMD_HATCH"),"T-34 hatch bone missing");
   var skin=R.Required(R.Find(target.transform,"lp_turret_cast001").GetComponent<SkinnedMeshRenderer>(),"T-34 hatch skin missing");
   int bone=Array.IndexOf(skin.bones,hatch);if(bone<0)throw new InvalidOperationException("Commander hatch has no skin bone");
   var weights=skin.sharedMesh.boneWeights;var baked=new Mesh();
   try{
    skin.BakeMesh(baked);var vertices=baked.vertices;var local=new Vector3[vertices.Length];var accepted=new bool[vertices.Length];if(weights.Length!=vertices.Length)throw new InvalidOperationException("Hatch skin weights do not match baked mesh");
    Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);int count=0;
    for(int i=0;i<vertices.Length;i++){
     var w=weights[i];float influence=(w.boneIndex0==bone?w.weight0:0f)+(w.boneIndex1==bone?w.weight1:0f)+(w.boneIndex2==bone?w.weight2:0f)+(w.boneIndex3==bone?w.weight3:0f);
     if(influence<0.9f)continue;
     Vector3 point=target.transform.InverseTransformPoint(skin.transform.position+skin.transform.rotation*vertices[i]);local[i]=point;accepted[i]=true;min=Vector3.Min(min,point);max=Vector3.Max(max,point);count++;
    }
    if(count<4)throw new InvalidOperationException("No commander hatch surface vertices found");
    float x=(min.x+max.x)*0.5f,z=(min.z+max.z)*0.5f;float y=TopSurfaceHeight(local,baked.triangles,accepted,x,z);
    return target.transform.TransformPoint(new Vector3(x,y,z));
   }finally{U.DestroyImmediate(baked);}
  }
  public static Vector3 FootPoint(GameObject assembly,Vector3 up){
   var skin=R.Required(R.Find(assembly.transform,"launcher").GetComponent<SkinnedMeshRenderer>(),"Native launcher body renderer missing");
   var mesh=new Mesh();
   try{
    skin.BakeMesh(mesh);Vector3[] vertices=mesh.vertices;float bottom=float.PositiveInfinity;
    foreach(var vertex in vertices)bottom=Mathf.Min(bottom,Vector3.Dot(skin.transform.position+skin.transform.rotation*vertex,up));
    Vector3 center=Vector3.zero;int count=0;
    foreach(var vertex in vertices){Vector3 point=skin.transform.position+skin.transform.rotation*vertex;if(Vector3.Dot(point,up)<=bottom+0.025f){center+=point;count++;}}
    if(count==0)throw new InvalidOperationException("Could not identify native launcher pedestal");
    return center/count;
   }finally{U.DestroyImmediate(mesh);}
  }
  public static WeaponSystem Install(GameObject target,GameObject bmp,out FireControlSystem fireControl,out CameraSlot sight){
   Transform source=R.Required(R.Find(bmp.transform,"9K111"),"Native BMP-1P 9P135M assembly missing");
   Transform turret=R.Required(R.Find(target.transform,"TURRET"),"T-34 turret missing");
   Transform hatch=R.Required(R.Find(target.transform,"CMD_HATCH"),"T-34 commander hatch missing");
   var pedestal=new GameObject("T3485Q30MOD_Hatch_Launcher_Base").transform;pedestal.SetParent(hatch,false);
   pedestal.SetPositionAndRotation(HatchSurfacePoint(target),target.transform.rotation);
   var own=U.Instantiate(source.gameObject,pedestal,false);own.name="T3485Q30MOD_BMP1P_9P135M";own.transform.localPosition=Vector3.zero;own.transform.localRotation=Quaternion.identity;own.transform.localScale=Vector3.one;
   // Native launcher skin has its own rig-space offset. Seat its actual foot, rather than its prefab origin, on the hatch.
   own.transform.position+=pedestal.position-FootPoint(own,target.transform.up);
   AddHatchSupport(pedestal,target,own.gameObject);
   // All native moving bones, sights, rack, effects and guidance references are within this assembly.
   // Only its root Reparent points outside the cloned branch in the BMP prefab.
   var rootParent=R.Required(own.GetComponent<Reparent>(),"Native launcher root Reparent missing");rootParent.NewParent=pedestal;
   foreach(var reparent in own.GetComponentsInChildren<Reparent>(true))if(reparent.NewParent!=pedestal&&(reparent.NewParent==null||!reparent.NewParent.IsChildOf(own.transform)))throw new InvalidOperationException("Launcher Reparent still targets the BMP-1P");
   WeaponSystem weapon=null;foreach(var w in own.GetComponentsInChildren<WeaponSystem>(true))if(w.GuidanceUnit!=null){weapon=w;break;}
   R.Required(weapon,"Native guided launcher weapon missing");
   fireControl=R.Required(own.GetComponentInChildren<FireControlSystem>(true),"Native launcher FCS missing");
   fireControl.CurrentStabMode=StabilizationMode.None;fireControl.DynamicLead=false;fireControl.PointStabIncludesVertical=false;
   foreach(var mount in fireControl.Mounts){mount.Stabilized=false;mount.ParentPlatform=null;}
   fireControl.LinkedWeaponSystems=new[]{weapon};
   var optic=R.Required(own.GetComponentInChildren<UsableOptic>(true),"Native launcher sight missing");optic.FCS=fireControl;optic.LevelTransform=target.transform;
   sight=R.Required(optic.slot,"Native launcher camera slot missing");sight.ExclusiveWeapons=new[]{weapon};sight.LinkedNightSight=null;sight.LinkedDaySight=null;sight.IsLinkedNightSight=false;
   weapon.Feed.ReadyRack.UseFixedAmmoLoad=true;
   var wm=R.Required(target.GetComponent<WeaponsManager>(),"T-34 weapons manager missing");var donor=R.Required(bmp.GetComponent<WeaponsManager>(),"BMP-1P weapons manager missing");
   WeaponSystemInfo sourceInfo=null;foreach(var info in donor.Weapons)if(info.Role==WeaponSystemRole.MountedLauncher&&info.Weapon.transform.IsChildOf(source)){sourceInfo=info;break;}
   if(sourceInfo==null)throw new InvalidOperationException("Native BMP-1P guided weapon registration missing");
   var entry=(WeaponSystemInfo)RegistryRules.CloneObject(sourceInfo);entry.Name="9P135M / 9M113 Konkurs";entry.Weapon=weapon;entry.FCS=fireControl;
   // This roof mount has its own aiming axes; it does not need to align the 85mm gun before use.
   entry.NeedsPreAim=false;entry.NeedsManualRealignment=false;entry.AiOverrideAimDirection=null;
   var weapons=new List<WeaponSystemInfo>(wm.Weapons);weapons.Add(entry);wm.Weapons=weapons.ToArray();
   var unit=R.Required(target.GetComponent<GHPC.Vehicle.Vehicle>(),"T-34 unit missing");
   foreach(var station in unit.CrewStationLayout)if(station.Position==CrewPosition.Gunner)foreach(var stance in station.Stances){
    stance.UsableWeapons.Add(weapon);stance.Sights.Add(sight);
   }
   own.SetActive(true);return weapon;
  }
 }
}
