using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
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
  internal static MelonPreferences_Entry<float> BarrelExtension,CoaxExtension;
  static NativeAssets assets;
  static FuldaEraAdapter eraAdapter;
  static GameObject owner,template;
  static AssetReference prefabReference;
  static bool failed,registered,preflight;
  static float nextPoll;
  static string assemblyDir;
  static readonly List<Action> donorAudits=new List<Action>();
  public override void OnInitializeMelon(){
   assemblyDir=Path.GetDirectoryName(GetType().Assembly.Location);
   var c=MelonPreferences.CreateCategory("T3485Quad30");
   BarrelExtension=c.CreateEntry("BarrelExtensionMetres",1.2f,description:"Extra native 85mm barrel length, applied to the private template once after restart.");
   CoaxExtension=c.CreateEntry("CoaxMuzzleExtensionMetres",1.65f,description:"30mm muzzle position ahead of the original coax muzzle, applied after restart.");
   assets=new NativeAssets();
   var h=new HarmonyLib.Harmony("local.T3485Quad30");
   Type panel=AccessTools.TypeByName("GHPC.UI.Menu.MissionConfigMenuPanel");
   MethodInfo refresh=panel==null?null:AccessTools.DeclaredMethod(panel,"RefreshPanelData");
   if(refresh==null){failed=true;MelonLogger.Error("[T3485Q30MOD] Mission customization API unavailable; mod disabled");return;}
   h.Patch(refresh,prefix:new HarmonyMethod(typeof(Mod),"BeforeMenu"));
   h.Patch(AccessTools.Method(typeof(GHPC.Player.PlayerInput),"QueueAmmoIndex"),prefix:new HarmonyMethod(typeof(NativeControls),"BeforeQueueAmmo"));
   preflight=Array.IndexOf(Environment.GetCommandLineArgs(),"--t3485mod-preflight")>=0;
   MelonLogger.Msg("[T3485Q30MOD] loaded; warming original T3485/BMP2/LEO2A3/BMP1P assets before independent registration");
  }
  public override void OnUpdate(){if(failed||Time.unscaledTime<nextPoll)return;nextPoll=Time.unscaledTime+1f;Poll();}
  public override void OnSceneWasLoaded(int buildIndex,string name){nextPoll=0;}
  public static void BeforeMenu(){if(!failed)Poll();}
  static void Poll(){
   try{
    var lookups=Resources.FindObjectsOfTypeAll<UnitPrefabLookupScriptable>();
    if(template==null){if(!assets.Poll(lookups))return;if(eraAdapter==null&&!FuldaEraAdapter.TryCreate(out eraAdapter))return;BuildTemplate();}
    int changed=0;
    foreach(var lookup in lookups){
     if(lookup==null)continue;Array result=RegistryRules.Append(lookup.AllUnits);if(System.Object.ReferenceEquals(result,lookup.AllUnits))continue;
     var list=(Meta[])result;list[list.Length-1].PrefabReference=prefabReference;lookup.AllUnits=list;changed++;
     var cache=AccessTools.Field(typeof(UnitSpawner),"_prefabLookupCached");
     foreach(var spawner in Resources.FindObjectsOfTypeAll<UnitSpawner>())if(spawner.PrefabLookup==lookup&&cache!=null)cache.SetValue(spawner,null);
    }
    if(changed>0){
     Type panel=AccessTools.TypeByName("GHPC.UI.Menu.MissionConfigMenuPanel");var f=AccessTools.Field(panel,"_unitKeysByClassCached");var keys=f==null?null:f.GetValue(null) as IDictionary;if(keys!=null)keys.Clear();
     registered=true;MelonLogger.Msg("[T3485Q30MOD] registered independent unit T3485Q30MOD / T-34-85M Quad 30mm in "+changed+" lookup(s); original T3485 preserved");
    }
    if(preflight&&registered){RunPreflight();preflight=false;Application.Quit(0);}
   }catch(Exception e){
    failed=true;MelonLogger.Error("[T3485Q30MOD] preparation failed; independent unit not offered: "+e);
    if(preflight){File.WriteAllText(Path.Combine(assemblyDir,"t3485mod-preflight-error.txt"),e.ToString());Application.Quit(2);}
   }
  }
  static void BuildTemplate(){
   CaptureDonors();var donorArmor=ArmorChanges.Capture(assets["T3485"]);
   owner=new GameObject("T3485Q30MOD_TemplateOwner");owner.SetActive(false);U.DontDestroyOnLoad(owner);owner.hideFlags=HideFlags.HideAndDontSave;
   var candidate=U.Instantiate(assets[ModCore.DonorKey],owner.transform,false);candidate.name=ModCore.TargetKey;candidate.SetActive(true);
   try{
    var v=R.Required(candidate.GetComponent<Vehicle>(),"T-34 vehicle component missing");
    R.Set(v,"_uniqueName",ModCore.TargetKey);R.Set(v,"_friendlyName",ModCore.DisplayName);
    ArmorChanges.Install(candidate);donorAudits.Add(()=>ArmorChanges.AssertUnchanged(donorArmor,assets["T3485"]));
    PerformanceChanges.InstallEngine(candidate,assets["T3485"]);
    var catalog=new AmmoCatalog();WeaponConversion.Upgrade85(candidate,catalog);
    WeaponSystem main=WeaponConversion.FindGun(candidate,85);
    int changed=ModelChanges.Lengthen85(candidate,main,Mathf.Clamp(BarrelExtension.Value,0.2f,3f));
    WeaponSystem coax=WeaponConversion.InstallCoax(candidate,assets["BMP2"],main,Mathf.Clamp(CoaxExtension.Value,0.5f,2.5f));
    FireControlSystem fcs=Stabilization.Install(candidate,assets["LEO2A3"]);
    var thermal=ThermalSight.Install(candidate,assets["LEO2A3"],fcs,main,coax);
    FireControlSystem launcherFcs;GHPC.Camera.CameraSlot launcherSight;
    var launcher=MissileLauncher.Install(candidate,assets["BMP1P"],out launcherFcs,out launcherSight);
    SightRegistry.Install(v,fcs.AuthoritativeOptic.slot,launcherSight);
    var state=candidate.AddComponent<RuntimeState>();state.Fcs=fcs;state.Main=main;state.Coax=coax;state.Day=fcs.AuthoritativeOptic.slot;state.Thermal=thermal;state.Launcher=launcher;state.LauncherFcs=launcherFcs;state.LauncherSight=launcherSight;
    var era=ReactiveArmor.Install(candidate,eraAdapter);
    MelonLogger.Msg("[T3485Q30MOD] Kontakt-1 layout: "+era.LayoutSummary+"; motion poses="+era.MotionPoses);
    Audit(candidate);
    foreach(var audit in donorAudits)audit();
    template=candidate;prefabReference=PrefabLocator.Install(template);
    MelonLogger.Msg("[T3485Q30MOD] private prefab prepared while inactive: barrel vertices="+changed+", 2A42=3UBR6/3UOR6, WorldPoint stabilization, native Leopard 2A3 E/lead and day/FLIR, unstabilized BMP1P 9M113");
   }catch{U.Destroy(candidate);U.Destroy(owner);throw;}
  }
  internal static void Audit(GameObject root){
   var v=R.Required(root.GetComponent<Vehicle>(),"Missing vehicle");if(v.UniqueName!=ModCore.TargetKey)throw new InvalidOperationException("Wrong modified vehicle identity");
   var s=R.Required(root.GetComponent<RuntimeState>(),"Missing modernized controller");
   if(s.Fcs.CurrentStabMode!=StabilizationMode.WorldPoint||!s.Fcs.PointStabIncludesVertical)throw new InvalidOperationException("Point stabilization not configured");
   if(s.Fcs.TurretPlatform.SpeedPowered!=21f)throw new InvalidOperationException("Traverse target not configured");
   if(s.Day.LinkedNightSight!=s.Thermal.slot||s.Thermal.slot.VisionType!=NightVisionType.Thermal||s.Thermal.slot.FLIRBlitMaterialOverride==null)throw new InvalidOperationException("Native Leopard 2A3 thermal path incomplete");
   UsableOptic dayOptic=null;foreach(var o in s.Fcs.GetComponentsInChildren<UsableOptic>(true))if(o.slot==s.Day){dayOptic=o;break;}
   if(dayOptic==null||s.Thermal.RotateAzimuth!=dayOptic.RotateAzimuth||s.Thermal.RotateElevation!=dayOptic.RotateElevation)throw new InvalidOperationException("Thermal aim axes do not match the configured point-stabilized day sight");
   if(Mathf.Abs(s.Thermal.slot.DefaultFov-9f)>0.001f||s.Thermal.slot.OtherFovs.Length!=1||Mathf.Abs(s.Thermal.slot.OtherFovs[0]-2.5f)>0.001f)throw new InvalidOperationException("Leopard 2A3 thermal FOV pair was not preserved");
   if(s.Thermal.FCS!=s.Fcs||s.Thermal.LevelTransform!=root.transform)throw new InvalidOperationException("Thermal still points to donor");
   var engine=root.GetComponent<NWH.VehiclePhysics.VehicleController>().engine;var originalEngine=assets["T3485"].GetComponent<NWH.VehiclePhysics.VehicleController>().engine;
   if(System.Object.ReferenceEquals(engine,originalEngine)||Mathf.Abs((engine.maxPower-originalEngine.maxPower)*ModCore.EngineHorsepowerPerKilowatt-250f)>0.01f)throw new InvalidOperationException("Engine power bonus is not 250hp");
   float nativeCoaxCycle=PerformanceChanges.CycleSeconds(WeaponConversion.FindGun(assets["BMP2"],30).Feed.RoundCycleStages);
   if(Mathf.Abs(60f/PerformanceChanges.CycleSeconds(s.Coax.Feed.RoundCycleStages)-(60f/nativeCoaxCycle+250f))>0.05f)throw new InvalidOperationException("30mm rate bonus is not 250rpm");
   if(s.Coax.WeaponData.CaliberMm!=30||!s.Coax.Feed.DualFeed)throw new InvalidOperationException("Coax weapon is not a dual-feed 2A42");
   var source85=WeaponConversion.FindGun(assets["T3485"],85).Feed.ReadyRack.ClipTypes;var own85=s.Main.Feed.ReadyRack.ClipTypes;
   if(source85.Length!=own85.Length)throw new InvalidOperationException("85mm clip count changed");
   for(int c=0;c<source85.Length;c++)for(int j=0;j<source85[c].MinimalPattern.Length;j++){
    var original=source85[c].MinimalPattern[j].AmmoType;var upgraded=own85[c].MinimalPattern[j].AmmoType;bool he=ModCore.IsHighExplosive(original.Name);
    float pen=original.RhaPenetration+(he?0f:230f),velocity=original.MuzzleVelocity+(he?200f:500f),tnt=original.TntEquivalentKg+(he?5f:0f);
    if(System.Object.ReferenceEquals(original,upgraded)||Mathf.Abs(upgraded.RhaPenetration-pen)>0.001f||Mathf.Abs(upgraded.MuzzleVelocity-velocity)>0.001f||Mathf.Abs(upgraded.TntEquivalentKg-tnt)>0.001f)throw new InvalidOperationException("Wrong upgraded 85mm definition: "+original.Name);
   }
   var types=s.Coax.Feed.ReadyRack.ClipTypes;if(types.Length!=2||!types[0].MinimalPattern[0].AmmoType.Name.StartsWith("3UBR6")||!types[1].MinimalPattern[0].AmmoType.Name.StartsWith("3UOR6"))throw new InvalidOperationException("Wrong 30mm ammunition");
   if(R.Find(root.transform,"T3485Q30MOD_2A42_Model")==null)throw new InvalidOperationException("2A42 model missing");
   if(s.Thermal.post!=null&&!s.Thermal.post.transform.IsChildOf(root.transform))throw new InvalidOperationException("Thermal post still points to donor");
   if(s.Thermal.postAdditional!=null)foreach(var post in s.Thermal.postAdditional)if(post!=null&&!post.transform.IsChildOf(root.transform))throw new InvalidOperationException("Thermal secondary post still points to donor");
   if(s.Thermal.RangeText!=null&&!s.Thermal.RangeText.transform.IsChildOf(root.transform))throw new InvalidOperationException("Thermal range display still points to donor");
   if(s.Thermal.reticleMesh!=null&&!s.Thermal.reticleMesh.transform.IsChildOf(root.transform))throw new InvalidOperationException("Thermal reticle still points to donor");
   foreach(var item in s.Thermal.slot.ExclusiveItems)if(item!=null&&!item.transform.IsChildOf(root.transform))throw new InvalidOperationException("Thermal UI activation still points to donor");
   foreach(var group in R.Get<Array>(s.Thermal,"FovLimitedItems"))foreach(var item in R.Get<GameObject[]>(group,"ExclusiveObjects"))if(item!=null&&!item.transform.IsChildOf(root.transform))throw new InvalidOperationException("Thermal zoom display still points to donor");
   if(s.Fcs.CurrentRange!=1000f)throw new InvalidOperationException("Leopard initial range missing");
   if(!s.Fcs.RecordTraverseRateBuffer||!s.Fcs.DynamicLead||!R.Get<bool>(s.Fcs,"_useSeparateLead"))throw new InvalidOperationException("Native Leopard lead acquisition incomplete");
   if(s.Day.DefaultFov!=5f||s.Day.GetComponent<UsableOptic>()!=s.Fcs.AuthoritativeOptic)throw new InvalidOperationException("Native Leopard day sight missing");
   if(!s.Fcs.AuthoritativeOptic.RegisterAtStart||s.Thermal.RegisterAtStart)throw new InvalidOperationException("Day/thermal optic registration order is wrong: thermal must not replace the main optic");
   if(!s.Fcs.AuthoritativeOptic.slot.transform.IsChildOf(root.transform)||!s.Thermal.slot.transform.IsChildOf(root.transform)||s.Fcs.AuthoritativeOptic.slot==s.Thermal.slot)throw new InvalidOperationException("Day/thermal CameraSlots are not independent owned slots");
   foreach(var optic in new[]{s.Fcs.AuthoritativeOptic,s.Thermal}){
    if(optic.RangeText==null||!optic.RangeText.transform.IsChildOf(root.transform)||optic.FCS!=s.Fcs)throw new InvalidOperationException("Day/thermal range canvas binding incomplete");
    if(optic.reticleMesh!=null&&!optic.reticleMesh.transform.IsChildOf(root.transform))throw new InvalidOperationException("Day/thermal reticle points to donor");
   }
   bool dayInCrew=false,launcherInCrew=false;
   foreach(var station in root.GetComponent<GHPC.Vehicle.Vehicle>().CrewStationLayout)foreach(var stance in station.Stances){
    if(stance.Sights.Contains(s.Day))dayInCrew=true;
    if(stance.Sights.Contains(s.LauncherSight)&&stance.UsableWeapons.Contains(s.Launcher))launcherInCrew=true;
   }
   if(!dayInCrew||!launcherInCrew)throw new InvalidOperationException("Native crew sight/weapon registration incomplete");
   if(s.Launcher==null||s.Launcher.GuidanceUnit==null||!s.Launcher.GuidanceUnit.AimElement.IsChildOf(root.transform))throw new InvalidOperationException("Native missile guidance binding incomplete");
   if(R.Find(root.transform,"T3485Q30MOD_Hatch_Support_Base")==null||R.Find(root.transform,"T3485Q30MOD_Hatch_Support_Column")==null)throw new InvalidOperationException("Launcher has no visible hatch mounting support");
   Transform launcherRoot=R.Find(root.transform,"T3485Q30MOD_BMP1P_9P135M"),pedestal=R.Find(root.transform,"T3485Q30MOD_Hatch_Launcher_Base");
   if((MissileLauncher.FootPoint(launcherRoot.gameObject,root.transform.up)-pedestal.position).magnitude>0.03f)throw new InvalidOperationException("Launcher foot is not seated on the hatch");
   if(s.LauncherFcs==s.Fcs||s.LauncherFcs.CurrentStabMode!=StabilizationMode.None||s.LauncherFcs.Mounts.Length!=2)throw new InvalidOperationException("Launcher must use its own unstabilized FCS");
   foreach(var mount in s.LauncherFcs.Mounts)if(mount.Stabilized||!mount.Transform.IsChildOf(root.transform)||Array.IndexOf(s.Fcs.Mounts,mount)>=0)throw new InvalidOperationException("Launcher shares or stabilizes the main-gun axes");
   if(R.Get<int[]>(s.Launcher.Feed.ReadyRack,"_initialClipCounts").Length!=1||R.Get<int[]>(s.Launcher.Feed.ReadyRack,"_initialClipCounts")[0]!=5)throw new InvalidOperationException("Wrong native missile load");
   if(s.Launcher.Feed.ReadyRack.ClipTypes[0].MinimalPattern[0].AmmoType.Name!="9M113 Konkurs")throw new InvalidOperationException("Wrong BMP-1P missile");
   var skin=R.Find(root.transform,"lp_turret_cast001").GetComponent<SkinnedMeshRenderer>();
   var nativeSkin=R.Find(assets["T3485"].transform,"lp_turret_cast001").GetComponent<SkinnedMeshRenderer>();
   Bounds expected=ModelChanges.PreserveTurretBounds(nativeSkin.localBounds,Mathf.Clamp(BarrelExtension.Value,0.2f,3f));
   if((skin.localBounds.center-expected.center).sqrMagnitude>0.001f||(skin.localBounds.size-expected.size).sqrMagnitude>0.001f)throw new InvalidOperationException("Turret culling no longer uses native root-bone bounds");
   var designated=v.DesignatedCameraSlots;
   if(designated==null||designated.Length!=3||Array.IndexOf(designated,s.Day)<0||Array.IndexOf(designated,s.LauncherSight)<0||Array.IndexOf(designated,v.DefaultCameraSlot)<0)throw new InvalidOperationException("Selectable camera registry missing");
   foreach(var slot in designated)if(slot==null||!slot.transform.IsChildOf(root.transform))throw new InvalidOperationException("Selectable camera registry points outside vehicle instance");
   ReactiveArmor.Audit(root);ArmorChanges.Audit(root);
   if(root.activeInHierarchy)throw new InvalidOperationException("Template activated before setup completed");
  }
  static void CaptureDonors(){
   donorAudits.Clear();
   foreach(string key in new[]{"T3485","BMP2","LEO2A3","BMP1P"}){
    var v=assets[key].GetComponent<Vehicle>();string unique=v.UniqueName,friendly=v.FriendlyName;
    donorAudits.Add(()=>{if(v.UniqueName!=unique||v.FriendlyName!=friendly)throw new InvalidOperationException("Native donor identity changed: "+key);});
   }
   foreach(string key in new[]{"T3485","BMP1P","LEO2A3"}){
    var originalVehicle=assets[key].GetComponent<Vehicle>();int weaponsCount=originalVehicle.GetComponent<WeaponsManager>().Weapons.Length;
    donorAudits.Add(()=>{if(originalVehicle.GetComponent<WeaponsManager>().Weapons.Length!=weaponsCount)throw new InvalidOperationException("Donor weapons registration changed: "+key);});
    foreach(var station in originalVehicle.CrewStationLayout)foreach(var stance in station.Stances){
     int sightsCount=stance.Sights.Count,usableCount=stance.UsableWeapons.Count;
     donorAudits.Add(()=>{if(stance.Sights.Count!=sightsCount||stance.UsableWeapons.Count!=usableCount)throw new InvalidOperationException("Donor crew layout changed: "+key);});
    }
   }
   var t34=assets["T3485"];var gun=WeaponConversion.FindGun(t34,85);var types=gun.Feed.ReadyRack.ClipTypes;
   foreach(var clip in types){
    string name=clip.Name;var original=clip;
    donorAudits.Add(()=>{if(original.Name!=name)throw new InvalidOperationException("Original T-34 clip changed");});
    foreach(var c in clip.MinimalPattern){var a=c.AmmoType;string n=a.Name;float p=a.RhaPenetration,v=a.MuzzleVelocity,t=a.TntEquivalentKg;donorAudits.Add(()=>{if(a.Name!=n||a.RhaPenetration!=p||a.MuzzleVelocity!=v||a.TntEquivalentKg!=t)throw new InvalidOperationException("Original T-34 ammo changed: "+n);});}
   }
   var engine=t34.GetComponent<NWH.VehiclePhysics.VehicleController>().engine;float power=engine.maxPower;
   donorAudits.Add(()=>{if(engine.maxPower!=power)throw new InvalidOperationException("Original T-34 engine changed");});
   var nativeCycle=WeaponConversion.FindGun(assets["BMP2"],30).Feed.RoundCycleStages;float cycle=PerformanceChanges.CycleSeconds(nativeCycle);
   donorAudits.Add(()=>{if(PerformanceChanges.CycleSeconds(nativeCycle)!=cycle)throw new InvalidOperationException("Original BMP-2 cycle changed");});
   var turret=R.Find(t34.transform,"TURRET").GetComponent<GHPC.AimablePlatform>();float speed=turret.SpeedPowered;
   donorAudits.Add(()=>{if(turret.SpeedPowered!=speed)throw new InvalidOperationException("Original T-34 traverse changed");});
  }
  static void RunPreflight(){
   Audit(template);foreach(var audit in donorAudits)audit();
   // Unity serialization must remap the private prefab's component references for every spawned instance.
   var copy=U.Instantiate(template,owner.transform,false);copy.name="T3485Q30MOD_PreflightClone";Audit(copy);
   var originalState=template.GetComponent<RuntimeState>();var clonedState=copy.GetComponent<RuntimeState>();
   if(clonedState.Fcs==originalState.Fcs||clonedState.Coax==originalState.Coax||clonedState.Launcher==originalState.Launcher||!clonedState.Launcher.GuidanceUnit.AimElement.IsChildOf(copy.transform)||!clonedState.Thermal.transform.IsChildOf(copy.transform))throw new InvalidOperationException("Instance component remapping failed");
   string optics=NativeValidation.CheckOpticRegistration(clonedState);
   string entry=NativeValidation.CheckHiddenSightEntry(copy);
   string contact=NativeValidation.CheckHatchContact(copy);
   string era=ReactiveArmor.VerifyClearance(copy);
   string startup=NativeValidation.CheckPointStartup(clonedState.Fcs);
   U.Destroy(copy);
   string culling=NativeValidation.CheckTurretBounds(template);string armor=ArmorChanges.Audit(template);
   var handle=Addressables.LoadAssetAsync<GameObject>(prefabReference.RuntimeKey);
   var loaded=handle.WaitForCompletion();if(loaded!=template)throw new InvalidOperationException("Runtime Addressables provider returned wrong template");Addressables.Release(handle);
   var s=template.GetComponent<RuntimeState>();var report=new System.Text.StringBuilder();
   report.AppendLine("PASS native Unity preflight: T-34 front/side/turret armor +50mm; 1.3 Fulda Kontakt-1 and no-clipping layout; 1.2 engine/ammunition/rate upgrades; native E/lead, Leopard day/FLIR, native ammo command, root-bone turret bounds, independent unstabilized BMP1P 9M113; independent metadata, inactive prefab, WorldPoint stabilization, 21 deg/s, Leopard 2A3 thermal, native 2A42 model and 3UBR6/3UOR6 feeds; Addressables runtime loading; per-instance component remapping; all donor identities/ammo unchanged; thermal/day aim-axis compatibility.");
   report.AppendLine(optics);
   report.AppendLine(entry);
   report.AppendLine(contact);
   report.AppendLine(ReactiveArmor.Audit(template));
   report.AppendLine(armor);
   report.AppendLine(era);
   report.AppendLine(startup);
   report.AppendLine(culling);
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"--t3485mod-render")>=0)report.AppendLine(NativeValidation.RenderVehicle(template,Path.Combine(assemblyDir,"T3485Quad30","validation")));
   report.AppendLine("Leopard 2A3 day: "+s.Day.DefaultFov+" deg; native reticle, post and independent range canvas; traverse-rate lead recording enabled.");
   report.AppendLine("Leopard 2A3 thermal: "+s.Thermal.slot.DefaultFov+" / "+s.Thermal.slot.OtherFovs[0]+" deg; "+s.Thermal.slot.FLIRWidth+"x"+s.Thermal.slot.FLIRHeight+"; native material, post and reticle hierarchy.");
   var engine=template.GetComponent<NWH.VehiclePhysics.VehicleController>().engine;var donorEngine=assets["T3485"].GetComponent<NWH.VehiclePhysics.VehicleController>().engine;
   report.AppendLine("Engine: "+donorEngine.maxPower+" -> "+engine.maxPower+" kW; "+(donorEngine.maxPower*ModCore.EngineHorsepowerPerKilowatt)+" -> "+(engine.maxPower*ModCore.EngineHorsepowerPerKilowatt)+" hp; +250hp; native conversion 1.341hp/kW.");
   report.AppendLine("2A42 cycle: "+PerformanceChanges.CycleSeconds(s.Coax.Feed.RoundCycleStages)+"s; "+(60f/PerformanceChanges.CycleSeconds(s.Coax.Feed.RoundCycleStages))+" rpm; +250rpm; magazine reload stages unchanged.");
   foreach(var clip in s.Main.Feed.ReadyRack.ClipTypes)foreach(var c in clip.MinimalPattern)report.AppendLine(c.AmmoType.Name+" | "+c.AmmoType.RhaPenetration+" mm | "+c.AmmoType.MuzzleVelocity+" m/s | "+c.AmmoType.TntEquivalentKg+" kg TNT equivalent");
   foreach(var clip in s.Coax.Feed.ReadyRack.ClipTypes)report.AppendLine(clip.Name+" | "+clip.Capacity+" rounds | "+clip.MinimalPattern[0].AmmoType.RhaPenetration+" mm | "+clip.MinimalPattern[0].AmmoType.MuzzleVelocity+" m/s");
   report.AppendLine("BMP1P: "+s.Launcher.Feed.ReadyRack.ClipTypes[0].Name+"; 5 missiles; 2 separate unstabilized axes; native sight and wire guidance.");
   string oldError=Path.Combine(assemblyDir,"t3485mod-preflight-error.txt");if(File.Exists(oldError))File.Delete(oldError);
   File.WriteAllText(Path.Combine(assemblyDir,"t3485mod-preflight.txt"),report.ToString());MelonLogger.Msg("[T3485Q30MOD] "+report);
  }
  public override void OnApplicationQuit(){if(assets!=null)assets.Release();}
 }
}


