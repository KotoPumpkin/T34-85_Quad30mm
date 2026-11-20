using GHPC.Player;
using GHPC.Weapons;
namespace T3485Quad30 {
 internal static class NativeControls {
  // Called by the game's normal ammo-selection command, including rebinds and controller actions.
  // The native no-loadout branch only queues ammo for weapon index zero; the converted coax is index one.
  public static bool BeforeQueueAmmo(PlayerInput __instance,int __0){
   if(__instance.CurrentPlayerUnit==null)return true;
   var state=__instance.CurrentPlayerUnit.GetComponent<RuntimeState>();
   if(state==null)return true;
   var info=__instance.CurrentPlayerWeapon;if(info==null||info.Weapon!=state.Coax)return true;
   if(!__instance.AllowPlayerStrictLiveInput||__instance.SuspendGunnerInputs||__instance.BlockKeyboardInput)return false;
   var types=state.Coax.Feed.ReadyRack.ClipTypes;
   if(__0>=0&&__0<types.Length)__instance.CurrentPlayerCrewBrain.WeaponsModule.QueueClipType(types[__0],false);
   return false;
  }
 }
}
