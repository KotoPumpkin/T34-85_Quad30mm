using System;
using GHPC;
using GHPC.Camera;
using GHPC.Weapons;
using GHPC.Equipment.Optics;
using UnityEngine;
using UnityEngine.Events;
using U=UnityEngine.Object;
namespace T3485Quad30 {
 internal static class ThermalSight {
  public static void CopyAimGeometry(UsableOptic day,UsableOptic thermal){
   thermal.Alignment=day.Alignment;thermal.LocalElevationLimits=day.LocalElevationLimits;thermal.LocalAzimuthLimits=day.LocalAzimuthLimits;
   thermal.RotateAzimuth=day.RotateAzimuth;thermal.RotateElevation=day.RotateElevation;
  }
  static void CopySharedCanvas(UsableOptic source,UsableOptic thermal,Transform target){
   if(thermal.RangeText==null||thermal.RangeText.transform.IsChildOf(target))return;
   Canvas nativeCanvas=R.Required(source.RangeText.GetComponentInParent<Canvas>(true),"Leopard 2A3 range display has no canvas");
   GameObject own=U.Instantiate(nativeCanvas.gameObject,target,false);own.name="T3485Q30MOD_EMES15_Canvas";
   string textName=source.RangeText.name;
   thermal.RangeText=null;foreach(var text in own.GetComponentsInChildren<TMPro.TMP_Text>(true))if(text.name==textName){thermal.RangeText=text;break;}
   R.Required(thermal.RangeText,"Cloned Leopard 2A3 range display missing");
   GameObject[] items=thermal.slot.ExclusiveItems;
   if(items!=null){
    var privateItems=(GameObject[])items.Clone();
    for(int i=0;i<privateItems.Length;i++){
     if(privateItems[i]==nativeCanvas.gameObject)privateItems[i]=own;
     else if(privateItems[i]!=null&&!privateItems[i].transform.IsChildOf(target))throw new InvalidOperationException("Unmapped external Leopard thermal display: "+privateItems[i].name);
    }
    thermal.slot.ExclusiveItems=privateItems;
   }
   own.SetActive(false);
  }
  static UsableOptic CloneOptic(UsableOptic source,Transform parent,Vector3 position,Quaternion rotation,GameObject target,FireControlSystem fcs,WeaponSystem main,WeaponSystem coax,CameraSlot oldSlot,string name){
   GameObject go=U.Instantiate(source.gameObject,parent,false);go.name=name;
   go.transform.localPosition=position;go.transform.localRotation=rotation;go.transform.localScale=Vector3.one;
   var optic=R.Required(go.GetComponent<UsableOptic>(),"Cloned Leopard optic missing");
   optic.FCS=fcs;optic.RegisterAtStart=source.RegisterAtStart;optic.LevelTransform=target.transform;
   CopySharedCanvas(source,optic,go.transform);
   // These indicators live on the donor's external shared canvas. Our cloned range canvas owns its display.
   optic.ReadyToFireObject=null;optic.UnreadyToFireObject=null;optic.FaultObject=null;optic.OverridingObject=null;
   optic.slot.ExclusiveWeapons=coax==null?new[]{main}:new[]{main,coax};optic.slot.HitZone=oldSlot.HitZone;
   optic.slot.LinkedNightSight=null;optic.slot.LinkedDaySight=null;optic.slot.IsLinkedNightSight=false;
   optic.slot.ZoomChangedEvents=new UnityEvent();optic.slot.ThermalPolarityChangedEvents=new UnityEvent<bool>();
   foreach(var o in go.GetComponentsInChildren<UsableOptic>(true)){o.FCS=fcs;o.LevelTransform=target.transform;if(o.slot!=null)o.slot.ExclusiveWeapons=coax==null?new[]{main}:new[]{main,coax};}
   go.SetActive(true);return optic;
  }
  public static UsableOptic Install(GameObject target,GameObject leopard,FireControlSystem fcs,WeaponSystem main,WeaponSystem coax){
   UsableOptic source=null;
   foreach(var o in leopard.GetComponentsInChildren<UsableOptic>(true))if(o.slot!=null&&o.slot.VisionType==NightVisionType.Thermal&&o.FCS!=null&&o.FCS.LinkedWeaponSystems.Length>1){source=o;break;}
   R.Required(source,"Original Leopard 2A3 thermal gunner optic missing");
   UsableOptic sourceDay=null;foreach(var o in leopard.GetComponentsInChildren<UsableOptic>(true))if(o.slot!=null&&o.slot.LinkedNightSight==source.slot){sourceDay=o;break;}
   R.Required(sourceDay,"Original Leopard 2A3 day gunner optic missing");
   var old=R.Required(fcs.GetComponentInChildren<UsableOptic>(true),"T-34 day optic missing");var oldSlot=R.Required(old.slot,"T-34 camera slot missing");
   Transform parent=old.transform.parent;Vector3 position=old.transform.localPosition;Quaternion rotation=old.transform.localRotation;
   var day=CloneOptic(sourceDay,parent,position,rotation,target,fcs,main,coax,oldSlot,"T3485Q30MOD_Leopard2A3_Day");
   var thermal=CloneOptic(source,parent,position,rotation,target,fcs,main,coax,oldSlot,"T3485Q30MOD_Leopard2A3_FLIR");
   CopyAimGeometry(day,thermal);
   day.slot.LinkedNightSight=thermal.slot;day.slot.NightSightAtNightOnly=false;day.slot.SlotToUseOnDestroy=oldSlot.SlotToUseOnDestroy==oldSlot?null:oldSlot.SlotToUseOnDestroy;
   thermal.slot.LinkedDaySight=day.slot;thermal.slot.IsLinkedNightSight=true;thermal.slot.SlotToUseOnDestroy=day.slot;
   var unit=R.Required(target.GetComponent<GHPC.Vehicle.Vehicle>(),"T-34 unit missing");
   foreach(var station in unit.CrewStationLayout)foreach(var stance in station.Stances)for(int i=0;i<stance.Sights.Count;i++)if(stance.Sights[i]==oldSlot)stance.Sights[i]=day.slot;
   fcs.AuthoritativeOptic=day;fcs.ReferenceTransform=day.transform;fcs.LaserOrigin=day.transform;
   old.RegisterAtStart=false;oldSlot.LinkedNightSight=null;
   // The original T-34 CameraSlot sits on the FCS parent, so remove only that component.
   U.DestroyImmediate(oldSlot);U.DestroyImmediate(old.gameObject);
   R.Set(target.GetComponent<GHPC.Vehicle.Vehicle>(),"_nightVisionType",NightVisionType.Thermal);
   return thermal;
  }
 }
}
