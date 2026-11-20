using System;
using System.Collections.Generic;
using GHPC;
using GHPC.Weapons;
using MelonLoader;
using UnityEngine;
using U=UnityEngine.Object;
namespace T3485Quad30 {
 internal static class ModelChanges {
  public static Bounds PreserveTurretBounds(Bounds nativeBounds,float extension){
   // Skinned bounds are expressed in root-bone space, not the mesh's import coordinates.
   return new Bounds(nativeBounds.center,nativeBounds.size+Vector3.one*(2f*(Mathf.Abs(extension)+0.1f)));
  }
  public static int Lengthen85(GameObject vehicle,WeaponSystem main,float extension){
   Transform mantlet=R.Required(R.Find(vehicle.transform,"MANTLET"),"T-34 mantlet bone missing");
   SkinnedMeshRenderer skin=null;foreach(var s in vehicle.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(s.name=="lp_turret_cast001"){skin=s;break;}
   R.Required(skin,"T-34 turret mesh missing");Mesh source=R.Required(skin.sharedMesh,"T-34 turret has no mesh");
   if(!source.isReadable)throw new InvalidOperationException("Native T-34 turret mesh is not readable");
   int bone=Array.IndexOf(skin.bones,mantlet);if(bone<0)throw new InvalidOperationException("Mantlet bone not bound to T-34 turret mesh");
   Transform oldMuzzle=R.Required(main.MuzzleIdentity,"85mm muzzle missing");
   Vector3 axis=mantlet.InverseTransformDirection(oldMuzzle.forward).normalized;
   Vector3 muzzle=mantlet.InverseTransformPoint(oldMuzzle.position);float end=Vector3.Dot(muzzle,axis);
   if(end<0.5f)throw new InvalidOperationException("Unexpected 85mm barrel axis/length: "+end);
   Vector3 center=muzzle-axis*end;Matrix4x4 toBone=source.bindposes[bone];
   Vector3 delta=toBone.inverse.MultiplyVector(axis*extension);Vector3[] vertices=source.vertices;int changed=0;
   for(int i=0;i<vertices.Length;i++){
    Vector3 p=toBone.MultiplyPoint3x4(vertices[i])-center;float z=Vector3.Dot(p,axis);
    if(z<=0.35f||z>end+0.25f||(p-axis*z).sqrMagnitude>0.18f*0.18f)continue;
    vertices[i]+=delta*Mathf.Clamp01((z-0.35f)/(end-0.35f));changed++;
   }
   if(changed<20)throw new InvalidOperationException("Could not isolate the native 85mm barrel; refusing a partial model edit");
   Mesh mesh=U.Instantiate(source);mesh.name="T3485Q30MOD_extended_barrel_turret";mesh.vertices=vertices;mesh.RecalculateBounds();mesh.hideFlags=HideFlags.DontUnloadUnusedAsset;
   Bounds nativeBounds=skin.localBounds;skin.sharedMesh=mesh;skin.localBounds=PreserveTurretBounds(nativeBounds,extension);
   // Leave original skin bones in their bind pose. The shooting and FX origin move with the extended tip.
   Vector3 point=oldMuzzle.position+oldMuzzle.forward*extension;
   var tip=new GameObject("T3485Q30MOD_85_Muzzle").transform;tip.SetParent(mantlet,false);tip.SetPositionAndRotation(point,oldMuzzle.rotation);
   main.transform.SetPositionAndRotation(point,oldMuzzle.rotation);R.Set(main,"_muzzleIdentity",tip);
   return changed;
  }
  public static MeshRenderer Install2A42Model(GameObject target,GameObject donor,WeaponSystem donorWeapon,Transform muzzle){
   SkinnedMeshRenderer skin=null;
   foreach(var s in donor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
    foreach(var bone in s.bones)if(bone!=null&&bone.name=="Main gun"){skin=s;break;}if(skin!=null)break;
   }
   R.Required(skin,"Original BMP-2 gun materials missing");
   var assembly=typeof(ModelChanges).Assembly;
   MeshPayload data=MeshPayload.Read(assembly.GetManifestResourceStream("T3485Quad30.2a42.meshbin"));
   int count=data.Vertices.Length/8;var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
   for(int i=0;i<count;i++){int b=i*8;vertices[i]=new Vector3(data.Vertices[b],data.Vertices[b+1],data.Vertices[b+2]);normals[i]=new Vector3(data.Vertices[b+3],data.Vertices[b+4],data.Vertices[b+5]);uv[i]=new Vector2(data.Vertices[b+6],data.Vertices[b+7]);}
   var mesh=new Mesh();mesh.name="T3485Q30MOD_native_BMP2_2A42";mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.subMeshCount=data.Submeshes.Length;
   for(int sub=0;sub<data.Submeshes.Length;sub++)mesh.SetTriangles(data.Submeshes[sub],sub);mesh.RecalculateTangents();mesh.RecalculateBounds();mesh.hideFlags=HideFlags.DontUnloadUnusedAsset;
   var model=new GameObject("T3485Q30MOD_2A42_Model");model.layer=skin.gameObject.layer;model.transform.SetParent(muzzle,false);
   model.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=model.AddComponent<MeshRenderer>();var donorMaterials=skin.sharedMaterials;var materials=new Material[mesh.subMeshCount];for(int i=0;i<materials.Length;i++)materials[i]=donorMaterials.Length==0?null:donorMaterials[Mathf.Min(i,donorMaterials.Length-1)];renderer.sharedMaterials=materials;renderer.shadowCastingMode=skin.shadowCastingMode;renderer.receiveShadows=skin.receiveShadows;MelonLogger.Msg("[T3485Q30MOD] 2A42 model "+model.name+" active="+model.activeInHierarchy+" enabled="+renderer.enabled+" bounds="+mesh.bounds+" materials="+materials.Length+"/"+donorMaterials.Length);
   return renderer;
  }
 }
}
