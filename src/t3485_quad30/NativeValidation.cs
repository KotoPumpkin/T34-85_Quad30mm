using System;
using System.IO;
using System.Collections.Generic;
using GHPC;
using GHPC.Weapons;
using UnityEngine;
using U=UnityEngine.Object;
namespace T3485Quad30 {
 internal static class NativeValidation {
  public static string CheckOpticRegistration(RuntimeState state){
   var day=R.Required(state.Fcs.AuthoritativeOptic,"Authoritative Leopard day optic missing");var thermal=R.Required(state.Thermal,"Leopard thermal optic missing");
   R.Required(day.slot,"Day CameraSlot missing");R.Required(thermal.slot,"Thermal CameraSlot missing");
   thermal.slot.RegisterOptic(thermal);day.slot.RegisterOptic(day);state.Fcs.RegisterOptic(day);
   if(state.Fcs.MainOptic!=day||state.Fcs.NightOptic!=thermal||day.slot.PairedOptic!=day||thermal.slot.PairedOptic!=thermal)throw new InvalidOperationException("Native camera registration selected the wrong main/night optic");
   return "PASS native camera registration: Leopard day is MainOptic, FLIR is NightOptic, both slots own paired optics.";
  }
  public static string CheckHatchContact(GameObject root){
   var hatch=R.Find(root.transform,"CMD_HATCH");var pedestal=R.Find(root.transform,"T3485Q30MOD_Hatch_Launcher_Base");var plate=R.Find(root.transform,"T3485Q30MOD_Hatch_Support_Base");
   if(pedestal.parent!=hatch)throw new InvalidOperationException("Launcher mount does not follow the physical hatch bone");
   Vector3 old=hatch.localPosition;float largestGap=0f;
   try{
    foreach(float movement in new[]{0f,-0.15f}){
     hatch.localPosition=old+Vector3.up*movement;Vector3 surface=MissileLauncher.HatchSurfacePoint(root);
     var b=plate.GetComponent<MeshFilter>().sharedMesh.bounds;float lowest=float.PositiveInfinity;
     for(int n=0;n<8;n++){
      var p=b.center+Vector3.Scale(b.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1));
      lowest=Mathf.Min(lowest,Vector3.Dot(plate.TransformPoint(p)-surface,root.transform.up));
     }
     if(lowest>0.004f||lowest< -0.025f)throw new InvalidOperationException("Launcher base misses hatch surface by "+lowest+"m");
     largestGap=Mathf.Max(largestGap,lowest);
    }
   }finally{hatch.localPosition=old;}
   return "PASS hatch contact: support base seated on actual skinned hatch surface and follows a 150mm hatch movement; air gap="+largestGap+"m.";
  }
  static GHPC.Camera.CameraManager entryManager;
  static GHPC.Camera.CameraSlot requestedEntry;
  public static bool CaptureEntry(GHPC.Camera.CameraManager __instance,GHPC.Camera.CameraSlot __0){
   if(__instance!=entryManager)return true;requestedEntry=__0;return false;
  }
  public static string CheckHiddenSightEntry(GameObject root){
   var state=root.GetComponent<RuntimeState>();var vehicle=root.GetComponent<GHPC.Vehicle.Vehicle>();
   // UsableOptic.Start hides the entire optic object when its slot is not currently active.
   var hide=HarmonyLib.AccessTools.Method(typeof(GHPC.Equipment.Optics.UsableOptic),"CameraSlot_ActiveChanged");
   hide.Invoke(state.Fcs.AuthoritativeOptic,new object[]{false});hide.Invoke(state.Thermal,new object[]{false});
   if(state.Day.gameObject.activeSelf||state.Thermal.slot.gameObject.activeSelf)throw new InvalidOperationException("Native unused optic hiding was not reproduced");
   var designated=vehicle.DesignatedCameraSlots;
   if(designated==null||Array.IndexOf(designated,state.Day)<0||Array.IndexOf(designated,state.LauncherSight)<0)
    throw new InvalidOperationException("Left Shift cannot find hidden Leopard sight: vehicle has no explicit day/launcher CameraSlot registry");
   var fixture=new GameObject("T3485Q30MOD_CameraEntryFixture");fixture.SetActive(false);
   var manager=fixture.AddComponent<GHPC.Camera.CameraManager>();var crew=fixture.AddComponent<GHPC.Crew.CrewManager>();crew.CrewMembers=new GHPC.Crew.CrewManager.CrewMember[0];
   R.Set(manager,"_crewManager",crew);entryManager=manager;
   var harmony=new HarmonyLib.Harmony("local.T3485Quad30.preflight.camera-entry");
   var attach=HarmonyLib.AccessTools.Method(typeof(GHPC.Camera.CameraManager),"AttachCamToSlot");
   harmony.Patch(attach,prefix:new HarmonyLib.HarmonyMethod(typeof(NativeValidation),"CaptureEntry"));
   try{
    HarmonyLib.AccessTools.Method(typeof(GHPC.Camera.CameraManager),"RescanCamSlots").Invoke(manager,new object[]{designated});
    var exclusion=HarmonyLib.AccessTools.Method(typeof(GHPC.Camera.CameraManager),"UpdateWeaponExclusions");
    foreach(var weapon in new[]{state.Main,state.Launcher}){
     exclusion.Invoke(manager,new object[]{weapon});R.Set(manager,"<ExteriorMode>k__BackingField",true);R.Set(manager,"_interiorSlotIndex",0);requestedEntry=null;
     manager.ToggleCameraSlotSet();
     var expected=weapon==state.Launcher?state.LauncherSight:state.Day;
     if(requestedEntry!=expected)throw new InvalidOperationException("Native Shift camera selection failed for "+weapon.name+"; selected="+(requestedEntry==null?"null":requestedEntry.name));
    }
   }finally{
    HarmonyLib.AccessTools.Method(typeof(GHPC.Camera.CameraManager),"DetachSlotEvents").Invoke(manager,null);
    harmony.Unpatch(attach,HarmonyLib.HarmonyPatchType.Prefix,harmony.Id);entryManager=null;requestedEntry=null;U.DestroyImmediate(fixture);
   }
   return "PASS native Shift entry after optic hiding: quad 30mm -> Leopard day, ATGM -> launcher; explicit owned camera list and native weapon exclusions.";
  }
  public static string CheckPointStartup(FireControlSystem fcs){
   // Exercise native stabilization and aiming methods without a live mission's ballistics/drive side effects.
   fcs.Mounts=new AimablePlatform[0];fcs.SuperelevateWeapon=false;R.Set(fcs,"_initialized",true);R.Set(fcs,"<StabsActive>k__BackingField",false);
   if(fcs.CurrentRange!=1000f)throw new InvalidOperationException("Spawned point FCS did not inherit Leopard initial range");
   Stabilization.EnablePointSight(fcs);
   if(fcs.AimWorldVector.sqrMagnitude<0.5f||(fcs.AimPoint-fcs.ReferenceTransform.position).magnitude<999f)throw new InvalidOperationException("First WorldPoint activation has a zero aim vector");
   Vector3 old=fcs.AimWorldVector;
   fcs.SetAimWorldPosition(fcs.ReferenceTransform.position+fcs.ReferenceTransform.forward*1000f+fcs.ReferenceTransform.right*100f);
   if((fcs.AimWorldVector-old).sqrMagnitude<0.0001f)throw new InvalidOperationException("Native WorldPoint cannot change aim after first activation");
   return "PASS first WorldPoint activation: native Leopard 1000m initial range, nonzero forward aim point and native aim movement.";
  }
  static Transform VisualHierarchy(Transform source,Transform parent,Dictionary<Transform,Transform> map,bool visible){
   var own=new GameObject(source.name);own.layer=31;own.transform.SetParent(parent,false);own.transform.localPosition=source.localPosition;own.transform.localRotation=source.localRotation;own.transform.localScale=source.localScale;
   map.Add(source,own.transform);visible=visible&&source.gameObject.activeSelf;own.SetActive(visible);
   foreach(Transform child in source)VisualHierarchy(child,own.transform,map,visible);return own.transform;
  }
  static Texture2D ReadTextureRegion(Texture2D source,int[] region){
   var oldTarget=RenderTexture.active;bool oldSrgb=GL.sRGBWrite;
   var render=RenderTexture.GetTemporary(region[2],region[3],0,RenderTextureFormat.ARGB32,source.isDataSRGB?RenderTextureReadWrite.sRGB:RenderTextureReadWrite.Linear);
   Texture2D image=null;
   try{
    GL.sRGBWrite=source.isDataSRGB;
    Graphics.Blit(source,render,new Vector2((float)region[2]/source.width,(float)region[3]/source.height),new Vector2((float)region[0]/source.width,(float)region[1]/source.height));
    RenderTexture.active=render;image=new Texture2D(region[2],region[3],TextureFormat.RGBA32,false,!source.isDataSRGB);
    image.ReadPixels(new Rect(0,0,region[2],region[3]),0,0);image.Apply(false,false);return image;
   }catch{if(image!=null)U.DestroyImmediate(image);throw;}
   finally{RenderTexture.active=oldTarget;GL.sRGBWrite=oldSrgb;RenderTexture.ReleaseTemporary(render);}
  }
  public static string CheckShieldTexturePixels(GameObject root,string directory){
   Directory.CreateDirectory(directory);var plate=R.Required(root.GetComponent<QuadMountState>().Plate,"Shield missing");var state=R.Required(plate.GetComponent<ShieldPaintState>(),"Shield crop state missing");int maximumError=0,pixels=0;
   for(int i=0;i<state.Properties.Length;i++){
    var source=state.SourceTextures[i];var crop=state.CroppedTextures[i];Texture2D expected=null,actual=null;
    try{
     expected=ReadTextureRegion(source,ShieldPaint.CropRegion(source.width,source.height));actual=ReadTextureRegion(crop,new[]{0,0,crop.width,crop.height});
     var a=expected.GetPixels32();var b=actual.GetPixels32();if(a.Length!=b.Length)throw new InvalidOperationException("Shield crop pixel count mismatch");
     for(int n=0;n<a.Length;n++){int error=Math.Max(Math.Max(Math.Abs(a[n].r-b[n].r),Math.Abs(a[n].g-b[n].g)),Math.Max(Math.Abs(a[n].b-b[n].b),Math.Abs(a[n].a-b[n].a)));maximumError=Math.Max(maximumError,error);pixels++;}
     if(state.Properties[i]=="_Albedo"){File.WriteAllBytes(Path.Combine(directory,"shield_source_patch.png"),expected.EncodeToPNG());File.WriteAllBytes(Path.Combine(directory,"shield_cropped_patch.png"),actual.EncodeToPNG());}
    }finally{if(expected!=null)U.DestroyImmediate(expected);if(actual!=null)U.DestroyImmediate(actual);}
   }
   if(maximumError>1)throw new InvalidOperationException("Shield GPU crop differs from original vehicle pixels by "+maximumError+"/255");
   return "PASS shield GPU pixels: "+pixels+" pixels across five maps compared against the original T34 texture region; maximum channel error="+maximumError+"/255; source and crop albedo PNGs saved.";
  }
  public static string RenderVehicle(GameObject root,string directory){
   Directory.CreateDirectory(directory);var map=new Dictionary<Transform,Transform>();
   var visual=VisualHierarchy(root.transform,null,map,true);visual.name="T3485Q30MOD_VisualValidation";
   GameObject cameraObject=null,lightObject=null;RenderTexture render=null;Texture2D image=null;
   Color oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
   try{
    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
     if(renderer.gameObject.layer==8||renderer.GetComponent<GHPC.UniformArmor>()!=null||renderer.GetComponent<GHPC.VariableArmor>()!=null)continue;
     var t=map[renderer.transform];var skin=renderer as SkinnedMeshRenderer;var stat=renderer as MeshRenderer;Renderer copy=null;
     if(skin!=null){
      var own=t.gameObject.AddComponent<SkinnedMeshRenderer>();own.sharedMesh=skin.sharedMesh;own.rootBone=skin.rootBone==null?null:map[skin.rootBone];
      var bones=new Transform[skin.bones.Length];for(int i=0;i<bones.Length;i++)bones[i]=skin.bones[i]==null?null:map[skin.bones[i]];own.bones=bones;own.localBounds=skin.localBounds;copy=own;
     }else if(stat!=null){
      var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
      t.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;copy=t.gameObject.AddComponent<MeshRenderer>();
     }
     if(copy!=null){copy.sharedMaterials=renderer.sharedMaterials;copy.enabled=renderer.enabled;copy.shadowCastingMode=renderer.shadowCastingMode;copy.receiveShadows=renderer.receiveShadows;}
    }
    cameraObject=new GameObject("T3485Q30MOD_Validation_Camera");var camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.08f,0.11f,0.15f);camera.nearClipPlane=0.03f;camera.farClipPlane=100f;camera.fieldOfView=50f;
    lightObject=new GameObject("T3485Q30MOD_Validation_Light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35f,-30f,0f);
    RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(0.65f,0.65f,0.65f);
    render=new RenderTexture(1280,960,24);render.Create();camera.targetTexture=render;image=new Texture2D(1280,960,TextureFormat.RGB24,false);
    var positions=new[]{new Vector3(0f,1.1f,6.5f),new Vector3(5f,3.8f,6f),new Vector3(-3.8f,4.4f,0.2f),new Vector3(-6.2f,3f,0.2f),new Vector3(6.2f,3f,0.2f),new Vector3(0f,3.0f,7f),new Vector3(0f,6.8f,1.0f)};
    var names=new[]{"front_low","front_three_quarter","roof_launcher","era_left","era_right","era_front","era_roof"};
    for(int i=0;i<positions.Length;i++){
     camera.transform.position=root.transform.TransformPoint(positions[i]);camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,2.25f,0.5f)));
     camera.Render();var oldRender=RenderTexture.active;RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1280,960),0,0);image.Apply();RenderTexture.active=oldRender;
     File.WriteAllBytes(Path.Combine(directory,names[i]+".png"),image.EncodeToPNG());
    }
    return "PASS offscreen native renderer captures: front_low.png, front_three_quarter.png, roof_launcher.png, era_left.png, era_right.png, era_front.png, era_roof.png; isolated render hierarchy with actual skin bones/bounds/materials.";
   }finally{
    RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;
    if(cameraObject!=null)U.DestroyImmediate(cameraObject);if(lightObject!=null)U.DestroyImmediate(lightObject);if(render!=null){render.Release();U.DestroyImmediate(render);}if(image!=null)U.DestroyImmediate(image);U.DestroyImmediate(visual.gameObject);
   }
  }
  public static string CheckTurretBounds(GameObject root){
   var skin=R.Required(R.Find(root.transform,"lp_turret_cast001").GetComponent<SkinnedMeshRenderer>(),"Turret renderer missing");
   var turret=R.Required(R.Find(root.transform,"TURRET"),"Turret missing");var gun=R.Required(R.Find(root.transform,"MANTLET"),"Gun missing");
   var elevation=R.Required(gun.GetComponent<AimablePlatform>(),"Gun elevation platform missing");float sign=elevation.ReverseLocalEuler?-1f:1f;
   Quaternion oldTurret=turret.localRotation,oldGun=gun.localRotation;var baked=new Mesh();int samples=0;float largestEscape=0;string detail="";
   try{
    foreach(float yaw in new[]{0f,90f,180f,270f})foreach(float pitch in new[]{elevation.LocalEulerLimits.x*sign,0f,elevation.LocalEulerLimits.y*sign}){
     turret.localRotation=oldTurret*Quaternion.AngleAxis(yaw,Vector3.up);gun.localRotation=oldGun*Quaternion.AngleAxis(pitch,Vector3.right);
     // BakeMesh(false) omits renderer scale; the native rig bind poses already include its 6.11x import scale.
     skin.BakeMesh(baked);Bounds bounds=skin.bounds;
     foreach(Vector3 vertex in baked.vertices){Vector3 world=skin.transform.position+skin.transform.rotation*vertex;Vector3 nearest=bounds.ClosestPoint(world);float escape=(nearest-world).magnitude;if(escape>largestEscape){largestEscape=escape;detail=" yaw="+yaw+" pitch="+pitch+" vertex="+vertex+" world="+world+" bounds="+bounds+" mesh="+skin.sharedMesh.bounds+" baked="+baked.bounds+" renderer scale="+skin.transform.lossyScale+" root="+skin.rootBone.name+" root scale="+skin.rootBone.lossyScale;}}
     samples++;
    }
   }finally{turret.localRotation=oldTurret;gun.localRotation=oldGun;U.DestroyImmediate(baked);}
   if(largestEscape>0.025f)throw new InvalidOperationException("Skinned turret vertices escape culling bounds by "+largestEscape+"m;"+detail);
   return "PASS turret skin culling: "+samples+" yaw/elevation poses, all baked vertices inside world render bounds; maximum escape="+largestEscape+"m.";
  }
 }
}
