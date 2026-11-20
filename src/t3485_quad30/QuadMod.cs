using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using GHPC;
using GHPC.Mission;
using GHPC.Vehicle;
using GHPC.Weapons;
using GHPC.Equipment.Optics;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
using U=UnityEngine.Object;
using Meta=GHPC.Mission.UnitPrefabLookupScriptable.UnitPrefabMetadata;
[assembly:MelonInfo(typeof(T3485Quad30.Mod),"T-34-85M Quad 30mm","0.1.0","Local build")]
[assembly:MelonGame("Radian Simulations LLC","GHPC")]
namespace T3485Quad30 {
 public sealed class Mod:MelonMod {
  static NativeAssets assets;static FuldaEraAdapter eraAdapter;static GameObject owner,template;static AssetReference prefabReference;static bool failed,registered,preflight;static float nextPoll;static string assemblyDir;static readonly List<Action> donorAudits=new List<Action>();
  public override void OnInitializeMelon(){
   assemblyDir=Path.GetDirectoryName(GetType().Assembly.Location);assets=new NativeAssets();var h=new HarmonyLib.Harmony("local.T3485Quad30");
   Type panel=AccessTools.TypeByName("GHPC.UI.Menu.MissionConfigMenuPanel");MethodInfo refresh=panel==null?null:AccessTools.DeclaredMethod(panel,"RefreshPanelData");
   if(refresh==null){failed=true;MelonLogger.Error("[T3485Q30MOD] Mission customization API unavailable; mod disabled");return;}
   h.Patch(refresh,prefix:new HarmonyMethod(typeof(Mod),"BeforeMenu"));h.Patch(AccessTools.Method(typeof(GHPC.Player.PlayerInput),"QueueAmmoIndex"),prefix:new HarmonyMethod(typeof(NativeControls),"BeforeQueueAmmo"));
   preflight=Array.IndexOf(Environment.GetCommandLineArgs(),"--t3485q30-preflight")>=0;MelonLogger.Msg("[T3485Q30MOD] loaded; warming native T3485/BMP2/LEO2A3/BMP1P assets");
  }
  public override void OnUpdate(){if(failed||Time.unscaledTime<nextPoll)return;nextPoll=Time.unscaledTime+1f;Poll();}
  public override void OnSceneWasLoaded(int buildIndex,string name){nextPoll=0;}
  public static void BeforeMenu(){if(!failed)Poll();}
  static void Poll(){try{
   var lookups=Resources.FindObjectsOfTypeAll<UnitPrefabLookupScriptable>();
   if(template==null){if(!assets.Poll(lookups))return;if(eraAdapter==null&&!FuldaEraAdapter.TryCreate(out eraAdapter))return;BuildTemplate();}
   int changed=0;foreach(var lookup in lookups){if(lookup==null)continue;Array result=RegistryRules.Append(lookup.AllUnits);if(System.Object.ReferenceEquals(result,lookup.AllUnits))continue;var list=(Meta[])result;list[list.Length-1].PrefabReference=prefabReference;lookup.AllUnits=list;changed++;var cache=AccessTools.Field(typeof(UnitSpawner),"_prefabLookupCached");foreach(var spawner in Resources.FindObjectsOfTypeAll<UnitSpawner>())if(spawner.PrefabLookup==lookup&&cache!=null)cache.SetValue(spawner,null);}
   if(changed>0){Type panel=AccessTools.TypeByName("GHPC.UI.Menu.MissionConfigMenuPanel");var f=AccessTools.Field(panel,"_unitKeysByClassCached");var keys=f==null?null:f.GetValue(null) as IDictionary;if(keys!=null)keys.Clear();registered=true;MelonLogger.Msg("[T3485Q30MOD] registered independent unit T3485Q30MOD / T-34-85M Quad 30mm in "+changed+" lookup(s); original T3485 preserved");}
   if(preflight&&registered){RunPreflight();preflight=false;Application.Quit(0);}
  }catch(Exception e){failed=true;MelonLogger.Error("[T3485Q30MOD] preparation failed; independent unit not offered: "+e);if(preflight){File.WriteAllText(Path.Combine(assemblyDir,"t3485q30-preflight-error.txt"),e.ToString());Application.Quit(2);}}}
  static void BuildTemplate(){
   CaptureDonors();var donorArmor=ArmorChanges.Capture(assets["T3485"]);owner=new GameObject("T3485Q30MOD_TemplateOwner");owner.SetActive(false);U.DontDestroyOnLoad(owner);owner.hideFlags=HideFlags.HideAndDontSave;
   var candidate=U.Instantiate(assets[ModCore.DonorKey],owner.transform,false);candidate.name=ModCore.TargetKey;candidate.SetActive(true);
   try{
    var v=R.Required(candidate.GetComponent<Vehicle>(),"T-34 vehicle component missing");R.Set(v,"_uniqueName",ModCore.TargetKey);R.Set(v,"_friendlyName",ModCore.DisplayName);
    PerformanceChanges.InstallEngine(candidate,assets["T3485"]);var pose=QuadWeaponConversion.Remove85(candidate);var fcs=Stabilization.Install(candidate,assets["LEO2A3"]);var quad=QuadWeaponConversion.InstallQuad30(candidate,assets["BMP2"],pose);fcs.LinkedWeaponSystems=new[]{quad};
    var elevation=R.Required(R.Find(candidate.transform,"MANTLET").GetComponent<AimablePlatform>(),"T-34 gun elevation platform missing");elevation.SpeedPowered=ModCore.PoweredElevationDegreesPerSecond;
    var thermal=ThermalSight.Install(candidate,assets["LEO2A3"],fcs,quad,null);FireControlSystem launcherFcs;GHPC.Camera.CameraSlot launcherSight;var launcher=MissileLauncher.Install(candidate,assets["BMP1P"],out launcherFcs,out launcherSight);SightRegistry.Install(v,fcs.AuthoritativeOptic.slot,launcherSight);
    var state=candidate.AddComponent<RuntimeState>();state.Fcs=fcs;state.Main=quad;state.Coax=quad;state.Day=fcs.AuthoritativeOptic.slot;state.Thermal=thermal;state.Launcher=launcher;state.LauncherFcs=launcherFcs;state.LauncherSight=launcherSight;
    ArmorChanges.Install(candidate);var era=ReactiveArmor.Install(candidate,eraAdapter);MelonLogger.Msg("[T3485Q30MOD] Kontakt-1 layout: "+era.LayoutSummary+"; motion poses="+era.MotionPoses);donorAudits.Add(()=>ArmorChanges.AssertUnchanged(donorArmor,assets["T3485"]));Audit(candidate);template=candidate;prefabReference=PrefabLocator.Install(template);MelonLogger.Msg("[T3485Q30MOD] private prefab prepared: quad 30mm 2A42, four native MultiBarrels, 850rpm each, left-to-right sequential fire, 27/31 deg/s powered traverse/elevation");
   }catch{U.Destroy(candidate);U.Destroy(owner);throw;}
  }
  internal static void Audit(GameObject root){
   var preState=root.GetComponent<RuntimeState>();if(preState!=null)QuadWeaponConversion.EnsureMultiBarrel(preState.Main);
   var v=R.Required(root.GetComponent<Vehicle>(),"Missing vehicle");if(v.UniqueName!=ModCore.TargetKey)throw new InvalidOperationException("Wrong quad vehicle identity");var s=R.Required(root.GetComponent<RuntimeState>(),"Missing quad controller");
   if(s.Main==null||s.Coax!=s.Main||s.Main.WeaponData==null||Mathf.Abs(s.Main.WeaponData.CaliberMm-30f)>0.01f||!s.Main.Feed.DualFeed)throw new InvalidOperationException("Quad weapon is not the native 30mm dual-feed system");
   var barrels=R.Get<Array>(s.Main,"MultiBarrels");if(barrels==null||barrels.Length!=4||!R.Get<bool>(s.Main,"_isMultiBarrel"))throw new InvalidOperationException("Quad weapon does not have four native sequential barrels: array="+(barrels==null?"null":barrels.Length.ToString())+" isMulti="+R.Get<bool>(s.Main,"_isMultiBarrel"));
   if(Mathf.Abs(PerformanceChanges.CycleSeconds(s.Main.Feed.RoundCycleStages)-QuadWeaponConversion.AggregateCycleSeconds)>0.0001f)throw new InvalidOperationException("Quad aggregate cycle is not 3400rpm / 850rpm per barrel");
   var clips=s.Main.Feed.ReadyRack.ClipTypes;if(clips.Length!=2||clips[0].Capacity!=600||clips[1].Capacity!=1200||!clips[0].MinimalPattern[0].AmmoType.Name.StartsWith("3UBR11")||!clips[1].MinimalPattern[0].AmmoType.Name.StartsWith("3UOF21"))throw new InvalidOperationException("Quad ammunition capacity or names are wrong");
   var ap=clips[0].MinimalPattern[0].AmmoType;var he=clips[1].MinimalPattern[0].AmmoType;if(Mathf.Abs(ap.RhaPenetration-163f)>0.001f||Mathf.Abs(ap.MuzzleVelocity-1210f)>0.001f||Mathf.Abs(ap.TntEquivalentKg)>0.001f||Mathf.Abs(he.RhaPenetration-10f)>0.001f||Mathf.Abs(he.MuzzleVelocity-920f)>0.001f||Mathf.Abs(he.TntEquivalentKg-0.049f)>0.0001f)throw new InvalidOperationException("Quad ammunition values are wrong");
   if(s.Fcs.CurrentStabMode!=StabilizationMode.WorldPoint||!s.Fcs.PointStabIncludesVertical||s.Fcs.TurretPlatform.SpeedPowered!=ModCore.PoweredTraverseDegreesPerSecond)throw new InvalidOperationException("Quad point stabilization or horizontal traverse is wrong");var elevation=R.Find(root.transform,"MANTLET").GetComponent<AimablePlatform>();if(elevation==null||Mathf.Abs(elevation.SpeedPowered-ModCore.PoweredElevationDegreesPerSecond)>0.001f)throw new InvalidOperationException("Quad elevation speed is wrong");
   if(s.Day.LinkedNightSight!=s.Thermal.slot||s.Thermal.slot.VisionType!=NightVisionType.Thermal||s.Thermal.FCS!=s.Fcs||s.Thermal.LevelTransform!=root.transform)throw new InvalidOperationException("Quad Leopard optics incomplete");
   if(s.Launcher==null||s.Launcher.GuidanceUnit==null||s.LauncherFcs==s.Fcs||s.LauncherFcs.CurrentStabMode!=StabilizationMode.None)throw new InvalidOperationException("Quad launcher integration incomplete");
   foreach(var info in v.GetComponent<WeaponsManager>().Weapons)if(info!=null&&info.Weapon!=null&&info.Weapon.WeaponData!=null&&Mathf.Abs(info.Weapon.WeaponData.CaliberMm-85f)<0.01f)throw new InvalidOperationException("85mm weapon was not removed");
   var engine=root.GetComponent<NWH.VehiclePhysics.VehicleController>().engine;var donorEngine=assets["T3485"].GetComponent<NWH.VehiclePhysics.VehicleController>().engine;if(Mathf.Abs((engine.maxPower-donorEngine.maxPower)*ModCore.EngineHorsepowerPerKilowatt-250f)>0.01f)throw new InvalidOperationException("Engine power bonus is wrong");
   ArmorChanges.Audit(root);ReactiveArmor.Audit(root);QuadGeometry.Audit(root);if(root.activeInHierarchy)throw new InvalidOperationException("Quad template activated before setup completed");
  }
  static void CaptureDonors(){donorAudits.Clear();foreach(string key in new[]{"T3485","BMP2","LEO2A3","BMP1P"}){var vehicle=assets[key].GetComponent<Vehicle>();string unique=vehicle.UniqueName,friendly=vehicle.FriendlyName;donorAudits.Add(()=>{if(vehicle.UniqueName!=unique||vehicle.FriendlyName!=friendly)throw new InvalidOperationException("Native donor identity changed: "+key);});}var t34=assets["T3485"];int weapons=t34.GetComponent<WeaponsManager>().Weapons.Length;donorAudits.Add(()=>{if(t34.GetComponent<WeaponsManager>().Weapons.Length!=weapons)throw new InvalidOperationException("Original T-34 weapons changed");});}
  static string ValidationDirectory(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--t3485q30-output");return i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):Path.Combine(assemblyDir,"T3485Quad30","validation");}
  static void RunPreflight(){Audit(template);foreach(var audit in donorAudits)audit();var copy=U.Instantiate(template,owner.transform,false);copy.name="T3485Q30MOD_PreflightClone";Audit(copy);var original=template.GetComponent<RuntimeState>();var cloned=copy.GetComponent<RuntimeState>();if(cloned.Fcs==original.Fcs||cloned.Main==original.Main||cloned.Launcher==original.Launcher||!cloned.Launcher.GuidanceUnit.AimElement.IsChildOf(copy.transform))throw new InvalidOperationException("Quad instance remapping failed");string optics=NativeValidation.CheckOpticRegistration(cloned);string entry=NativeValidation.CheckHiddenSightEntry(copy);string contact=NativeValidation.CheckHatchContact(copy);string era=ReactiveArmor.VerifyClearance(copy);string startup=NativeValidation.CheckPointStartup(cloned.Fcs);U.Destroy(copy);string culling=NativeValidation.CheckTurretBounds(template);if(PrefabProvider.Template!=template||PrefabProvider.Template.GetComponent<Vehicle>().UniqueName!=ModCore.TargetKey)throw new InvalidOperationException("Quad Addressables provider template is not the independent quad unit");var s=template.GetComponent<RuntimeState>();var report=new System.Text.StringBuilder();report.AppendLine("PASS native quad-30mm preflight: removed 85mm and old coax; native BMP2 2A42 mesh x4; native four-barrel left-to-right MultiBarrels; each barrel 850rpm, aggregate 3400rpm; 27 deg/s horizontal and 31 deg/s vertical powered movement; independent armor/ERA/optics/launcher preserved.");report.AppendLine(optics);report.AppendLine(entry);report.AppendLine(contact);report.AppendLine(ArmorChanges.Audit(template));report.AppendLine(ReactiveArmor.Audit(template));report.AppendLine(era);report.AppendLine(QuadGeometry.Audit(template));report.AppendLine(NativeValidation.CheckShieldTexturePixels(template,ValidationDirectory()));report.AppendLine(startup);report.AppendLine(culling);if(Array.IndexOf(Environment.GetCommandLineArgs(),"--t3485q30-render")>=0)report.AppendLine(NativeValidation.RenderVehicle(template,ValidationDirectory()));report.AppendLine("Quad 30mm: 4 barrels; 850rpm each; aggregate cycle "+QuadWeaponConversion.AggregateCycleSeconds+"s; left-to-right sequence; 3UBR11 600 rounds 163mm/1210m/s/0kg; 3UOF21 1200 rounds 10mm/920m/s/0.049kg; original 30mm fragment fields inherited.");report.AppendLine("Engine: +250hp; Fulda Kontakt-1: "+template.GetComponent<ReactiveArmorState>().LayoutSummary);File.WriteAllText(Path.Combine(assemblyDir,"t3485q30-preflight.txt"),report.ToString());MelonLogger.Msg("[T3485Q30MOD] "+report);}
  public override void OnApplicationQuit(){if(assets!=null)assets.Release();}
 }
}
