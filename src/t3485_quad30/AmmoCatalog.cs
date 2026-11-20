using System;
using System.Collections.Generic;
using GHPC.Weaponry;
using UnityEngine;
using Clip=AmmoType.AmmoClip;
namespace T3485Quad30 {
 public sealed class AmmoCatalog {
  readonly Dictionary<AmmoType,AmmoType> upgraded=new Dictionary<AmmoType,AmmoType>(new RefEquality<AmmoType>());
  readonly HashSet<AmmoType> owned=new HashSet<AmmoType>(new RefEquality<AmmoType>());
  readonly Dictionary<AmmoCodexScriptable,AmmoCodexScriptable> codexes=new Dictionary<AmmoCodexScriptable,AmmoCodexScriptable>(new RefEquality<AmmoCodexScriptable>());
  public AmmoType Upgrade85(AmmoType source){
   if(source==null||owned.Contains(source))return source;
   AmmoType found;if(upgraded.TryGetValue(source,out found))return found;
   if(Math.Abs(source.Caliber-85f)>0.01f){upgraded[source]=source;return source;}
   found=(AmmoType)RegistryRules.CloneObject(source);
   ModCore.Ballistics values=ModCore.Upgrade85mm(source.RhaPenetration,source.MuzzleVelocity,source.Name);
   found.RhaPenetration=values.Penetration;found.MuzzleVelocity=values.Velocity;
   if(ModCore.IsHighExplosive(source.Name))found.TntEquivalentKg=source.TntEquivalentKg+ModCore.HEExplosiveBonusKg;
   found.Name=source.Name+" [T3485Q30MOD]";found.CachedIndex=-1;
   // New reference and cache key: original ammo and other tanks keep their original ballistics.
   upgraded[source]=found;owned.Add(found);return found;
  }
  public Clip Clone85Clip(Clip source){
   if(source==null)return null;
   var result=(Clip)RegistryRules.CloneObject(source);result.MinimalPattern=(AmmoCodexScriptable[])source.MinimalPattern.Clone();
   bool changed=false;
   for(int i=0;i<result.MinimalPattern.Length;i++){
    AmmoCodexScriptable original=result.MinimalPattern[i];if(original==null)continue;
    AmmoType ammo=Upgrade85(original.AmmoType);if(System.Object.ReferenceEquals(ammo,original.AmmoType))continue;
    AmmoCodexScriptable copy;
    if(!codexes.TryGetValue(original,out copy)){
     copy=ScriptableObject.CreateInstance<AmmoCodexScriptable>();copy.name="ammo_t3485mod_"+original.name;copy.AmmoType=ammo;copy.hideFlags=HideFlags.DontUnloadUnusedAsset;codexes.Add(original,copy);
    }
    result.MinimalPattern[i]=copy;changed=true;
   }
   if(changed)result.Name=source.Name+" [T3485Q30MOD]";
   return result;
  }
  public static Clip Own30Clip(Clip source){
   var result=(Clip)RegistryRules.CloneObject(source);result.MinimalPattern=new AmmoCodexScriptable[source.MinimalPattern.Length];result.Name=source.Name+" [T3485Q30MOD 2A42]";
   for(int i=0;i<result.MinimalPattern.Length;i++){
    var original=source.MinimalPattern[i];var codex=ScriptableObject.CreateInstance<AmmoCodexScriptable>();codex.name="ammo_t3485mod_30_"+original.name;codex.hideFlags=HideFlags.DontUnloadUnusedAsset;
    var ammo=(AmmoType)RegistryRules.CloneObject(original.AmmoType);ammo.Name=original.AmmoType.Name+" [T3485Q30MOD 2A42]";ammo.CachedIndex=-1;codex.AmmoType=ammo;result.MinimalPattern[i]=codex;
   }return result;
  }
 }
}

