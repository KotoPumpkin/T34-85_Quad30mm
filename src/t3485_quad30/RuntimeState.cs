using GHPC.Camera;
using GHPC.Weapons;
using GHPC.Equipment.Optics;
using UnityEngine;
using MelonLoader;
namespace T3485Quad30 {
 public sealed class RuntimeState:MonoBehaviour {
  public FireControlSystem Fcs,LauncherFcs;
  public WeaponSystem Main,Coax,Launcher;
  public UsableOptic Thermal;
  public CameraSlot Day,LauncherSight;
  bool initialized;
  public void Update(){
   if(initialized||Fcs==null||!R.Get<bool>(Fcs,"_initialized"))return;
   var day=Fcs.AuthoritativeOptic;
   Day.RegisterOptic(day);Thermal.slot.RegisterOptic(Thermal);Fcs.RegisterOptic(day);
   HarmonyLib.AccessTools.Method(typeof(FireControlSystem),"SetManualMode").Invoke(Fcs,new object[]{false});Stabilization.EnablePointSight(Fcs);initialized=true;
   var unit=GetComponent<GHPC.Vehicle.Vehicle>();
   MelonLogger.Msg("[T3485Q30MOD] ready 0.1.0: mainOptic="+(Fcs.MainOptic==day)+", nightOptic="+(Fcs.NightOptic==Thermal)+", designatedSlots="+unit.DesignatedCameraSlots.Length+", dayRegistered="+(System.Array.IndexOf(unit.DesignatedCameraSlots,Day)>=0)+", hatchSupport=surface-bound: "+gameObject.name);

  }
 }
}
