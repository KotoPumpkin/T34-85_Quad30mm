using System;
using GHPC;
using GHPC.Weapons;
using GHPC.Equipment.Optics;
using UnityEngine;
namespace T3485Quad30 {
 internal static class Stabilization {
  public static void CopyNativeMode(FireControlSystem target,FireControlSystem donor){
   foreach(string name in new[]{"CurrentStabMode","ImperfectFixedRangePointStab","PointStabIncludesVertical","PointStabAffectsLead","InertiaLeadIncludesVertical","DynamicLead","InertialCompensation","SuperelevateWeapon","SuperleadWeapon","RefreshAimOnAmmoChange","ClampDegreeAdjust","ClampVectorAdjust","_counterMotionLeadAlwaysOn","_horizontalLeadOnly","_useSeparateLead","_autoDumpViaPalmSwitches","RecordTraverseRateBuffer","AimSlowDownRate","FireGateAngle","_fixParallaxForVectorMode","_currentRange"}){
    var field=R.Field(typeof(FireControlSystem),name);field.SetValue(target,field.GetValue(donor));
   }
  }
  public static void EnablePointSight(FireControlSystem fcs){
   float range=fcs.CurrentRange;
   if(!(range>0f)||float.IsInfinity(range)){range=1000f;R.Set(fcs,"_currentRange",range);}
   fcs.TargetRange=range;fcs.SetStabsActive(true);
   // A prefab or native startup path can already mark stabs active without creating a forward WorldPoint.
   if(fcs.AimWorldVector.sqrMagnitude<0.25f||(fcs.AimPoint-fcs.ReferenceTransform.position).sqrMagnitude<1f)
    fcs.SetAimWorldPosition(fcs.ReferenceTransform.position+fcs.ReferenceTransform.forward*range);
  }
  public static FireControlSystem Install(GameObject vehicle,GameObject leo){
   var fcs=R.Required(vehicle.GetComponentInChildren<FireControlSystem>(true),"T-34 FCS missing");
   FireControlSystem donor=null;foreach(var f in leo.GetComponentsInChildren<FireControlSystem>(true))if(f.CurrentStabMode==StabilizationMode.WorldPoint){donor=f;break;}
   R.Required(donor,"Leopard 2A3 WorldPoint FCS missing");
   var turret=R.Required(R.Find(vehicle.transform,"TURRET"),"T-34 turret bone missing");
   var traverse=R.Required(turret.GetComponent<AimablePlatform>(),"T-34 turret platform missing");
   traverse.SpeedPowered=ModCore.PoweredTraverseDegreesPerSecond;
   foreach(var mount in fcs.Mounts)if(mount!=null)mount.Stabilized=true;
   CopyNativeMode(fcs,donor);
   fcs.WeaponAuthoritative=false;fcs.ComputerNeedsPower=false;
   fcs.LaserAim=donor.LaserAim;fcs.MaxLaserRange=4000;fcs.RegisteredRangeLimits=new Vector2(0,4000);fcs.RangeGranularity=10;fcs.RangeStep=100;fcs.DisplayRangeIncrement=10;
   fcs.MarkRangeInvalidBelow=donor.MarkRangeInvalidBelow;fcs.IgnoreRangeBelow=donor.IgnoreRangeBelow;fcs.UseDeltaD=donor.UseDeltaD;
   R.Set(fcs,"_autoModeOnLase",true);
   // Detach the optic/FCS pivot from the elevation bone, matching the donor's independent sight layout.
   fcs.transform.SetParent(turret,true);
   var optic=R.Required(fcs.GetComponentInChildren<UsableOptic>(true),"T-34 day optic missing");
   UsableOptic donorOptic=null;foreach(var o in leo.GetComponentsInChildren<UsableOptic>(true))if(o.slot!=null&&o.slot.VisionType==NightVisionType.None&&o.FCS==donor){donorOptic=o;break;}
   if(donorOptic!=null){optic.Alignment=donorOptic.Alignment;optic.RotateElevation=donorOptic.RotateElevation;optic.RotateAzimuth=donorOptic.RotateAzimuth;optic.ForceHorizontalReticleAlign=donorOptic.ForceHorizontalReticleAlign;optic.CantCorrect=donorOptic.CantCorrect;optic.CantCorrectMaxSpeed=donorOptic.CantCorrectMaxSpeed;}
   optic.LevelTransform=vehicle.transform;fcs.LaserOrigin=optic.transform;fcs.ReferenceTransform=optic.transform;fcs.TurretPlatform=traverse;
   return fcs;
  }
 }
}
