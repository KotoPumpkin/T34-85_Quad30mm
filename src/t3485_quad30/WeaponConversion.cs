using System;
using System.Collections.Generic;
using GHPC;
using GHPC.Weapons;
using GHPC.Weaponry;
using UnityEngine;
using Clip=AmmoType.AmmoClip;
using NativeRack=GHPC.Weapons.AmmoRack;
using U=UnityEngine.Object;
namespace T3485Quad30 {
 internal static class WeaponConversion {
  public static WeaponSystem FindGun(GameObject root,float caliber){foreach(var w in root.GetComponentsInChildren<WeaponSystem>(true))if(w.WeaponData!=null&&Mathf.Abs(w.WeaponData.CaliberMm-caliber)<0.01f)return w;throw new InvalidOperationException("Native gun not found: "+caliber);}
  public static void Upgrade85(GameObject root,AmmoCatalog catalog){
   foreach(var rack in root.GetComponentsInChildren<NativeRack>(true)){
    Clip[] clips=rack.ClipTypes;if(clips==null)continue;var own=new Clip[clips.Length];for(int i=0;i<clips.Length;i++)own[i]=catalog.Clone85Clip(clips[i]);rack.ClipTypes=own;
   }
   // Include all loaded 85mm definitions, including optional mission ammunition, without touching 76.2mm or 30mm.
   foreach(var codex in Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>())if(codex!=null&&codex.AmmoType!=null&&Mathf.Abs(codex.AmmoType.Caliber-85f)<0.01f)catalog.Upgrade85(codex.AmmoType);
  }
  public static WeaponSystem InstallCoax(GameObject root,GameObject bmp,WeaponSystem main,float extension){
   var weapons=R.Required(root.GetComponent<WeaponsManager>(),"T-34 WeaponsManager missing");
   WeaponSystem coax=null;foreach(var info in weapons.Weapons)if(info.Weapon!=main){coax=info.Weapon;break;}
   R.Required(coax,"T-34 coax weapon missing");WeaponSystem source=FindGun(bmp,30);
   foreach(var info in weapons.Weapons)if(info.Weapon==main)info.Name="85mm D-5T Extended";
   NativeRack rack=R.Required(coax.Feed.ReadyRack,"T-34 coax rack missing");
   var ap=FindAmmo("ammo_3UBR6");var he=FindAmmo("ammo_3UOR6");
   Clip apClip=AmmoCatalog.Own30Clip(new Clip {Name="3UBR6 APBC-T 160rd feed",MinimalPattern=new[]{ap},Capacity=160});
   Clip heClip=AmmoCatalog.Own30Clip(new Clip {Name="3UOR6 HE-T 340rd feed",MinimalPattern=new[]{he},Capacity=340});
   rack.ClipCapacity=2;rack.UseVisibleRounds=false;rack.UseFixedAmmoLoad=true;rack.ClipTypes=new[]{apClip,heClip};R.Set(rack,"_initialClipCounts",new[]{1,1});
   CopyFeed(coax.Feed,source.Feed);coax.Feed.ReadyRack=rack;
   var codex=U.Instantiate(source.WeaponData);codex.name="weapon_t3485mod_2a42";codex.FriendlyName="30mm coaxial 2A42";R.Set(coax,"CodexEntry",codex);
   coax.BaseDeviationAngle=source.BaseDeviationAngle;coax.UsesLoopingEffects=source.UsesLoopingEffects;coax.RestartEffectsOnly=source.RestartEffectsOnly;
   coax.Impulse=source.Impulse;coax.ImpulseDelay=source.ImpulseDelay;coax.RecoilBlurMultiplier=source.RecoilBlurMultiplier;coax.TriggerHoldTime=source.TriggerHoldTime;
   foreach(string f in new[]{"_useBursts","_minBurstSeconds","_maxBurstSeconds","_minPauseSeconds","_maxPauseSeconds"})R.Field(typeof(WeaponSystem),f).SetValue(coax,R.Field(typeof(WeaponSystem),f).GetValue(source));
   Transform oldMuzzle=coax.MuzzleIdentity;Vector3 point=oldMuzzle.position+oldMuzzle.forward*extension;Quaternion rotation=oldMuzzle.rotation;
   var tip=new GameObject("T3485Q30MOD_30_Muzzle").transform;tip.SetParent(main.Feed.ReadyRack.transform.parent,false);tip.SetPositionAndRotation(point,rotation);
   // Both weapon script roots are reparented by the native T-34 Reparent component after activation.
   // Parent the new muzzle to the actual gun-elevation bone so it stays aligned with pitch and traverse.
   tip.SetParent(R.Required(R.Find(root.transform,"MANTLET"),"mantlet missing"),true);
   coax.transform.SetPositionAndRotation(point,rotation);R.Set(coax,"_muzzleIdentity",tip);
   foreach(var p in coax.MuzzleEffects)if(p!=null)p.gameObject.SetActive(false);
   var particles=new List<ParticleSystem>();
   foreach(var p in source.MuzzleEffects){if(p==null)continue;var own=U.Instantiate(p.gameObject,tip,false);own.transform.localPosition=Vector3.zero;own.transform.localRotation=Quaternion.identity;particles.Add(own.GetComponent<ParticleSystem>());}
   R.Set(coax,"_muzzleEffects",particles.ToArray());coax.MuzzleEffectPrefabs=source.MuzzleEffectPrefabs;
   if(coax.WeaponSound!=null)coax.WeaponSound.gameObject.SetActive(false);
   if(source.WeaponSound!=null){var sound=U.Instantiate(source.WeaponSound.gameObject,coax.transform,false);coax.WeaponSound=sound.GetComponent(source.WeaponSound.GetType()) as GHPC.Weapons.WeaponAudio;}
   foreach(var info in weapons.Weapons)if(info.Weapon==coax){info.Name="30mm coaxial 2A42";info.ExcludeFromFcsUpdates=false;}
   ModelChanges.Install2A42Model(root,bmp,source,tip);coax.gameObject.name="30mm Coaxial Gun 2A42";coax.DamageName="30mm coaxial gun";
   return coax;
  }
  static AmmoCodexScriptable FindAmmo(string name){foreach(var a in Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>())if(a.name.Equals(name,StringComparison.OrdinalIgnoreCase))return a;throw new InvalidOperationException("Required original ammunition not loaded: "+name);}
  static void CopyFeed(AmmoFeed dst,AmmoFeed src){
   dst.AutoFeed=src.AutoFeed;dst.ReloadMode=src.ReloadMode;dst.DoCycleWhenEmpty=src.DoCycleWhenEmpty;dst.ReloadDuringMissileTracking=src.ReloadDuringMissileTracking;dst.DualFeed=true;dst.OpenBolt=src.OpenBolt;dst.HumanLoaded=src.HumanLoaded;dst.AnnounceReloads=false;dst.SlowReloadMultiplier=src.SlowReloadMultiplier;
   dst.RoundCycleStages=PerformanceChanges.Boost30Cycle(src.RoundCycleStages);dst.ClipReloadStages=CloneStages(src.ClipReloadStages);
  }
  static AmmoFeed.ReloadStage[] CloneStages(AmmoFeed.ReloadStage[] source){var result=new AmmoFeed.ReloadStage[source.Length];for(int i=0;i<source.Length;i++)result[i]=(AmmoFeed.ReloadStage)RegistryRules.CloneObject(source[i]);return result;}
 }
}


