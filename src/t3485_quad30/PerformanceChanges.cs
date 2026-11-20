using System;
using GHPC.Weapons;
using NWH.VehiclePhysics;
using UnityEngine;
namespace T3485Quad30 {
 public static class PerformanceChanges {
  public static void SetEnginePower(Engine target,Engine donor){
   if(System.Object.ReferenceEquals(target,donor))throw new InvalidOperationException("Engine upgrade requires a private instance");
   target.maxPower=donor.maxPower+ModCore.AdditionalEngineHorsepower/ModCore.EngineHorsepowerPerKilowatt;
  }
  public static void InstallEngine(GameObject target,GameObject donor){
   var own=R.Required(target.GetComponent<VehicleController>(),"T-34 vehicle controller missing");
   var source=R.Required(donor.GetComponent<VehicleController>(),"Native T-34 vehicle controller missing");
   own.engine=(Engine)RegistryRules.CloneObject(own.engine);SetEnginePower(own.engine,source.engine);
  }
  public static float CycleSeconds(AmmoFeed.ReloadStage[] stages){
   float seconds=0f;foreach(var stage in stages)seconds+=R.Get<float>(stage,"Duration");return seconds;
  }
  public static AmmoFeed.ReloadStage[] Boost30Cycle(AmmoFeed.ReloadStage[] source){
   float seconds=CycleSeconds(source);if(!(seconds>0f))throw new InvalidOperationException("Native 2A42 cycle duration is invalid");
   float targetSeconds=60f/(60f/seconds+ModCore.AdditionalCoaxRoundsPerMinute),scale=targetSeconds/seconds;
   var own=new AmmoFeed.ReloadStage[source.Length];
   for(int i=0;i<own.Length;i++){
    own[i]=(AmmoFeed.ReloadStage)RegistryRules.CloneObject(source[i]);R.Set(own[i],"Duration",R.Get<float>(source[i],"Duration")*scale);own[i].ResetLiveDuration();
   }return own;
  }
 }
}
