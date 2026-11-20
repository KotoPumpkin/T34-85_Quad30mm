using System;
using System.Collections;
using System.Collections.Generic;
using GHPC;
using GHPC.Utility;
using MelonLoader;
using GHPC.Weapons;
using GHPC.Weaponry;
using UnityEngine;
using U=UnityEngine.Object;
using Clip=AmmoType.AmmoClip;
using NativeRack=GHPC.Weapons.AmmoRack;
namespace T3485Quad30 {
 internal sealed class MainGunPose { public Vector3 Position; public Quaternion Rotation; }
 internal static class QuadWeaponConversion {
  public const float OuterBarrelOffset=0.40f,InnerBarrelOffset=0.22f,OuterBarrelHeight=0.095f,InnerBarrelHeight=-0.095f;
  const float PerBarrelRpm=850f;
  public static float AggregateCycleSeconds {get{return 60f/(PerBarrelRpm*4f);}}
  public static void EnsureMultiBarrel(WeaponSystem quad){if(quad==null)return;var barrels=R.Get<Array>(quad,"MultiBarrels");if(barrels==null||barrels.Length!=4)return;if(!R.Get<bool>(quad,"_isMultiBarrel"))R.Set(quad,"_isMultiBarrel",true);int index=R.Get<int>(quad,"_multiBarrelIndex");if(index<0||index>=4)R.Set(quad,"_multiBarrelIndex",0);quad.ResetBarrelIndexOnRelease=false;}
  public static MainGunPose Remove85(GameObject root){
   var weapons=R.Required(root.GetComponent<WeaponsManager>(),"T-34 WeaponsManager missing");WeaponSystem main=null;
   foreach(var info in weapons.Weapons)if(info.Weapon!=null&&info.Weapon.WeaponData!=null&&Mathf.Abs(info.Weapon.WeaponData.CaliberMm-85f)<0.01f){main=info.Weapon;break;}
   R.Required(main,"Native 85mm main weapon missing");Transform muzzle=R.Required(main.MuzzleIdentity,"Native 85mm muzzle missing");var pose=new MainGunPose{Position=muzzle.position,Rotation=muzzle.rotation};
   var list=new List<WeaponSystemInfo>(weapons.Weapons);list.RemoveAll(x=>x==null||x.Weapon==main);weapons.Weapons=list.ToArray();
   foreach(var station in root.GetComponent<GHPC.Vehicle.Vehicle>().CrewStationLayout)foreach(var stance in station.Stances){stance.UsableWeapons.Remove(main);}
   QuadGeometry.RemoveLegacy(root);U.DestroyImmediate(main.gameObject);return pose;
  }
  public static WeaponSystem InstallQuad30(GameObject root,GameObject bmp,MainGunPose pose){
   var weapons=R.Required(root.GetComponent<WeaponsManager>(),"T-34 WeaponsManager missing");WeaponSystem quad=null;
   foreach(var info in weapons.Weapons)if(info!=null&&info.Weapon!=null){quad=info.Weapon;break;}
   R.Required(quad,"Native coax weapon base missing");WeaponSystem source=FindGun(bmp,30);
   var rack=R.Required(quad.Feed.ReadyRack,"Quad 30mm rack missing");var ap=FindAmmo("ammo_3UBR6").AmmoType;var he=FindAmmo("ammo_3UOR6").AmmoType;
   AmmoType apOwn=CloneAmmo(ap,"3UBR11 APFSDS-T [T3485Q30MOD]",163f,1210f,0f),heOwn=CloneAmmo(he,"3UOF21 HEI-T [T3485Q30MOD]",10f,920f,0.049f);
   Clip apClip=MakeClip("3UBR11 APFSDS-T 600rd feed",apOwn,600),heClip=MakeClip("3UOF21 HEI-T 1200rd feed",heOwn,1200);
   rack.ClipCapacity=2;rack.UseVisibleRounds=false;rack.UseFixedAmmoLoad=true;rack.ClipTypes=new[]{apClip,heClip};R.Set(rack,"_initialClipCounts",new[]{1,1});
   CopyFeed(quad.Feed,source.Feed,rack);
   var codex=U.Instantiate(source.WeaponData);codex.name="weapon_t3485q30_quad_2a42";codex.FriendlyName="Quad 30mm 2A42";R.Set(quad,"CodexEntry",codex);
   quad.BaseDeviationAngle=source.BaseDeviationAngle;quad.UsesLoopingEffects=source.UsesLoopingEffects;quad.RestartEffectsOnly=source.RestartEffectsOnly;quad.Impulse=source.Impulse;quad.ImpulseDelay=source.ImpulseDelay;quad.RecoilBlurMultiplier=source.RecoilBlurMultiplier;quad.TriggerHoldTime=source.TriggerHoldTime;
   foreach(string f in new[]{"_useBursts","_minBurstSeconds","_maxBurstSeconds","_minPauseSeconds","_maxPauseSeconds"})R.Field(typeof(WeaponSystem),f).SetValue(quad,R.Field(typeof(WeaponSystem),f).GetValue(source));
   Transform mantlet=QuadGeometry.AddMount(root,pose.Rotation);var mount=new GameObject("T3485Q30MOD_QuadMount").transform;mount.SetParent(mantlet,false);mount.SetPositionAndRotation(mantlet.position,pose.Rotation);
   quad.transform.SetParent(mount,false);quad.transform.localPosition=Vector3.zero;quad.transform.localRotation=Quaternion.identity;var reparent=quad.GetComponent<Reparent>();if(reparent!=null)reparent.NewParent=mount;
   foreach(var r in quad.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var p in quad.MuzzleEffects)if(p!=null)p.gameObject.SetActive(false);
   var offsets=new[]{new Vector3(-OuterBarrelOffset,OuterBarrelHeight,0f),new Vector3(-InnerBarrelOffset,InnerBarrelHeight,0f),new Vector3(InnerBarrelOffset,InnerBarrelHeight,0f),new Vector3(OuterBarrelOffset,OuterBarrelHeight,0f)};var models=new MeshRenderer[4];
   Type barrelType=typeof(WeaponSystem).Assembly.GetType("GHPC.Weapons.BarrelInfo",true);Array barrels=Array.CreateInstance(barrelType,4);
   for(int i=0;i<4;i++){
    var tip=new GameObject("T3485Q30MOD_30mm_Muzzle_"+(i+1)).transform;tip.SetParent(mount,false);tip.localPosition=offsets[i];tip.localRotation=Quaternion.identity;
    models[i]=ModelChanges.Install2A42Model(root,bmp,source,tip);tip.localPosition+=Vector3.forward*QuadGeometry.MuzzleOffset(models[i].GetComponent<MeshFilter>().sharedMesh.bounds.min.z,QuadGeometry.FrontOffset,QuadGeometry.ReceiverEmbed);
    var particles=new List<ParticleSystem>();foreach(var donor in source.MuzzleEffects){if(donor==null)continue;var own=U.Instantiate(donor.gameObject,tip,false);own.transform.localPosition=Vector3.zero;own.transform.localRotation=Quaternion.identity;particles.Add(own.GetComponent<ParticleSystem>());}
    object info=Activator.CreateInstance(barrelType);R.Field(barrelType,"MuzzleIdentity").SetValue(info,tip);R.Field(barrelType,"ImpulseLocation").SetValue(info,tip);R.Field(barrelType,"MuzzleEffects").SetValue(info,particles.ToArray());R.Field(barrelType,"RoundLoadedObject").SetValue(info,null);R.Field(barrelType,"Destructible").SetValue(info,null);barrels.SetValue(info,i);
   }
   quad.MultiBarrels=null;R.Set(quad,"MultiBarrels",barrels);R.Set(quad,"_isMultiBarrel",true);R.Set(quad,"_multiBarrelIndex",0);quad.ResetBarrelIndexOnRelease=false;MelonLogger.Msg("[T3485Q30MOD] multi barrel field set: array="+((Array)R.Get<object>(quad,"MultiBarrels")).Length+" isMulti="+R.Get<bool>(quad,"_isMultiBarrel"));quad.MuzzleEffectPrefabs=source.MuzzleEffectPrefabs;quad.gameObject.name="T3485Q30MOD_Quad_2A42";quad.DamageName="Quad 30mm cannon";
   foreach(var info in weapons.Weapons)if(info!=null&&info.Weapon==quad){info.Name="Quad 30mm 2A42";info.Role=WeaponSystemRole.MainGun;info.ExcludeFromFcsUpdates=false;}
   foreach(var station in root.GetComponent<GHPC.Vehicle.Vehicle>().CrewStationLayout)foreach(var stance in station.Stances){if(!stance.UsableWeapons.Contains(quad))stance.UsableWeapons.Add(quad);}
   root.GetComponent<QuadMountState>().Guns=models;
   return quad;
  }
  static WeaponSystem FindGun(GameObject root,float caliber){foreach(var w in root.GetComponentsInChildren<WeaponSystem>(true))if(w.WeaponData!=null&&Mathf.Abs(w.WeaponData.CaliberMm-caliber)<0.01f)return w;throw new InvalidOperationException("Native gun not found: "+caliber);}
  static AmmoCodexScriptable FindAmmo(string name){foreach(var a in Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>())if(a.name.Equals(name,StringComparison.OrdinalIgnoreCase))return a;throw new InvalidOperationException("Required original ammunition not loaded: "+name);}
  static AmmoType CloneAmmo(AmmoType source,string name,float penetration,float velocity,float tnt){var own=(AmmoType)RegistryRules.CloneObject(source);own.Name=name;own.RhaPenetration=penetration;own.MuzzleVelocity=velocity;own.TntEquivalentKg=tnt;own.CachedIndex=-1;return own;}
  static Clip MakeClip(string name,AmmoType ammo,int capacity){var codex=ScriptableObject.CreateInstance<AmmoCodexScriptable>();codex.name="ammo_t3485q30_"+name;codex.hideFlags=HideFlags.DontUnloadUnusedAsset;codex.AmmoType=ammo;return new Clip{Name=name,MinimalPattern=new[]{codex},Capacity=capacity};}
  static void CopyFeed(AmmoFeed dst,AmmoFeed src,NativeRack rack){dst.AutoFeed=src.AutoFeed;dst.ReloadMode=src.ReloadMode;dst.DoCycleWhenEmpty=src.DoCycleWhenEmpty;dst.ReloadDuringMissileTracking=src.ReloadDuringMissileTracking;dst.DualFeed=true;dst.OpenBolt=src.OpenBolt;dst.HumanLoaded=src.HumanLoaded;dst.AnnounceReloads=false;dst.SlowReloadMultiplier=src.SlowReloadMultiplier;dst.RoundCycleStages=CloneCycle(src.RoundCycleStages,AggregateCycleSeconds);dst.ClipReloadStages=CloneStages(src.ClipReloadStages);dst.ReadyRack=rack;R.Set(dst,"_totalCycleTime",0f);dst.TotalCycleTime.ToString();}
  static AmmoFeed.ReloadStage[] CloneCycle(AmmoFeed.ReloadStage[] source,float total){float old=0f;foreach(var s in source)old+=R.Get<float>(s,"Duration");var result=new AmmoFeed.ReloadStage[source.Length];for(int i=0;i<source.Length;i++){result[i]=(AmmoFeed.ReloadStage)RegistryRules.CloneObject(source[i]);R.Set(result[i],"Duration",R.Get<float>(source[i],"Duration")*total/old);result[i].ResetLiveDuration();}return result;}
  static AmmoFeed.ReloadStage[] CloneStages(AmmoFeed.ReloadStage[] source){var result=new AmmoFeed.ReloadStage[source.Length];for(int i=0;i<source.Length;i++)result[i]=(AmmoFeed.ReloadStage)RegistryRules.CloneObject(source[i]);return result;}
 }
}
