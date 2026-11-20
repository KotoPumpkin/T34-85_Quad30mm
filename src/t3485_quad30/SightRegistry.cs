using System;
using GHPC.Camera;
using GHPC.Vehicle;
namespace T3485Quad30 {
 internal static class SightRegistry {
  public static void Install(Vehicle vehicle,CameraSlot day,CameraSlot launcher){
   var commander=R.Required(vehicle.DefaultCameraSlot,"T-34 default commander camera missing");
   R.Required(day,"Leopard day camera missing");R.Required(launcher,"Launcher camera missing");
   day.AllowFreeLook=false;day.IsExterior=false;day.DirectAttachOnly=false;
   // Leopard optics hide their own objects in Start; native active-child scans cannot discover them afterward.
   // The native Leopard prefab solves this by serializing its selectable day slots explicitly.
   R.Set(vehicle,"_designatedCameraSlots",new[]{commander,day,launcher});
  }
 }
}
