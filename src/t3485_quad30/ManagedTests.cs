using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
using GHPC.Mission;
using Clip=AmmoType.AmmoClip;
public class ManagedTests {
 static string bin;
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static int Main(string[] args){
  bin=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..",".."));
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
   string name=new AssemblyName(e.Name).Name+".dll";
   foreach(string dir in new[]{Path.Combine(bin,"GHPC_Data","Managed"),Path.Combine(bin,"MelonLoader","net35"),AppDomain.CurrentDomain.BaseDirectory}){
    string f=Path.Combine(dir,name);if(File.Exists(f))return Assembly.LoadFrom(f);
   }return null;
  };
  try{return Run(args.Length>0?args[0]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"T3485Quad30.dll"));}catch(Exception e){Console.Error.WriteLine("FAIL: "+e);return 1;}
 }
 [MethodImpl(MethodImplOptions.NoInlining)] static int Run(string file){
  Check(File.Exists(file),"Compiled mod missing");
  Assembly a=Assembly.LoadFrom(Path.GetFullPath(file));
  Type reg=a.GetType("T3485Quad30.RegistryRules");Check(reg!=null,"Independent registration rules missing");
  var donor=new UnitPrefabLookupScriptable.UnitPrefabMetadata {Name="T3485",FriendlyName="T-34-85M",AllowInCustomizer=false,UseDecalLayout=true};
  var v=(UnitPrefabLookupScriptable.UnitPrefabMetadata[])reg.GetMethod("Append").Invoke(null,new object[]{new[]{donor}});
  Check(v.Length==2&&v[1].Name=="T3485Q30MOD"&&v[1].FriendlyName=="T-34-85M Quad 30mm"&&v[1].AllowInCustomizer,"Wrong menu entry");
  Check(donor.Name=="T3485"&&!donor.AllowInCustomizer&&donor.UseDecalLayout,"Original metadata modified");
  Check(Object.ReferenceEquals(reg.GetMethod("Append").Invoke(null,new object[]{v}),v),"Duplicate registration");
  Check(reg.GetMethod("Append").Invoke(null,new object[]{null})==null,"Null lookup handling");
  Console.WriteLine("PASS actual game metadata: new entry, donor isolation, idempotence, null handling");
  Type ball=a.GetType("T3485Quad30.AmmoCatalog");Check(ball!=null,"Ammunition clone rules missing");object catalog=Activator.CreateInstance(ball);MethodInfo clone=ball.GetMethod("Upgrade85");
  var cases=new[]{
   new {Name="BR-365 APHE-T",Cal=85f,P=140f,V=800f,T=0.15f,WantP=370f,WantV=1300f,WantT=0.15f},
   new {Name="BR-365K APHE-T",Cal=85f,P=150f,V=800f,T=0.1f,WantP=380f,WantV=1300f,WantT=0.1f},
   new {Name="BR-365P HVAP-T",Cal=85f,P=200f,V=1050f,T=0f,WantP=430f,WantV=1550f,WantT=0f},
   new {Name="O-365 HE-T",Cal=85f,P=20f,V=793f,T=0.74f,WantP=20f,WantV=993f,WantT=5.74f},
   new {Name="85mm reduced HE-T",Cal=85f,P=20f,V=645f,T=0.74f,WantP=20f,WantV=845f,WantT=5.74f},
   new {Name="BK-85 HEAT-FS-T",Cal=85f,P=300f,V=800f,T=0.5f,WantP=530f,WantV=1300f,WantT=0.5f},
   new {Name="OF-350 HE-FRAG",Cal=76.2f,P=15f,V=680f,T=0.7f,WantP=15f,WantV=680f,WantT=0.7f},
   new {Name="BR-354N APCR-T",Cal=76.2f,P=150f,V=950f,T=0f,WantP=150f,WantV=950f,WantT=0f},
   new {Name="3UBR6 APBC-T",Cal=30f,P=51f,V=970f,T=0f,WantP=51f,WantV=970f,WantT=0f}
  };
  foreach(var c in cases){
   var original=new AmmoType {Name=c.Name,Caliber=c.Cal,RhaPenetration=c.P,MuzzleVelocity=c.V,TntEquivalentKg=c.T,CachedIndex=10};
   var copy=(AmmoType)clone.Invoke(catalog,new object[]{original});
   Check(copy.RhaPenetration==c.WantP&&copy.MuzzleVelocity==c.WantV&&Math.Abs(copy.TntEquivalentKg-c.WantT)<0.0001f,"Wrong cumulative upgrade for "+c.Name);
   Check(original.RhaPenetration==c.P&&original.MuzzleVelocity==c.V&&original.TntEquivalentKg==c.T&&original.Name==c.Name,"Original ammunition mutated");
   Check(Object.ReferenceEquals(clone.Invoke(catalog,new object[]{original}),copy),"Same original upgraded twice");
   Check(Object.ReferenceEquals(clone.Invoke(catalog,new object[]{copy}),copy),"Already upgraded ammunition upgraded again");
   if(c.Cal==85f)Check(copy.Name!=original.Name&&copy.CachedIndex==-1,"Ballistic cache identity not isolated");
  }
  Console.WriteLine("PASS real AmmoType: cumulative nonHE pen+230/velocity+500; HE velocity+200/TNT+5kg; 30mm/76.2mm untouched; original isolation and exact-once caching");
  var sameName1=new AmmoType {Name="duplicate",Caliber=85,RhaPenetration=140,MuzzleVelocity=790};var sameName2=new AmmoType {Name="duplicate",Caliber=85,RhaPenetration=150,MuzzleVelocity=800};
  Check(((AmmoType)clone.Invoke(catalog,new object[]{sameName1} )).RhaPenetration==370&&((AmmoType)clone.Invoke(catalog,new object[]{sameName2})).RhaPenetration==380,"Distinct same-name ammunition collapsed");
  Console.WriteLine("PASS same-name source object identity");
  Type payload=a.GetType("T3485Quad30.MeshPayload");Check(payload!=null,"Native model reader missing");
  object data=payload.GetMethod("Read").Invoke(null,new object[]{a.GetManifestResourceStream("T3485Quad30.2a42.meshbin")});
  float[] vertices=(float[])payload.GetField("Vertices").GetValue(data);int[][] sub=(int[][])payload.GetField("Submeshes").GetValue(data);
  Check(vertices.Length==734*8&&sub.Sum(x=>x.Length)==623*3,"Wrong embedded native 2A42 geometry");
  Check(sub.SelectMany(x=>x).All(x=>x>=0&&x<734),"Model indices invalid");
  bool bad=false;try{payload.GetMethod("Read").Invoke(null,new object[]{new MemoryStream(new byte[12])});}catch(TargetInvocationException e){bad=e.InnerException is InvalidDataException;}Check(bad,"Invalid model header not rejected");
  Console.WriteLine("PASS embedded original 2A42 payload: 734 vertices / 623 triangles; index bounds and bad-header rejection");
  Type sight=a.GetType("T3485Quad30.ThermalSight");Check(sight!=null,"Thermal installer missing");
  MethodInfo geometry=sight.GetMethod("CopyAimGeometry",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);Check(geometry!=null,"Thermal geometry binding must copy the configured day sight axes");
  var day=(GHPC.Equipment.Optics.UsableOptic)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GHPC.Equipment.Optics.UsableOptic));
  var night=(GHPC.Equipment.Optics.UsableOptic)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GHPC.Equipment.Optics.UsableOptic));
  day.RotateAzimuth=true;day.RotateElevation=false;night.RotateAzimuth=false;night.RotateElevation=true;
  geometry.Invoke(null,new object[]{day,night});Check(night.RotateAzimuth&&!night.RotateElevation,"Thermal axes retained M1 defaults instead of following the configured point sight");
  Check(day.RotateAzimuth&&!day.RotateElevation,"Day sight settings changed");
  Console.WriteLine("PASS thermal point-sight axis binding on actual UsableOptic field objects");
  Type model=a.GetType("T3485Quad30.ModelChanges");
  MethodInfo boundsRule=model.GetMethod("PreserveTurretBounds",BindingFlags.Public|BindingFlags.Static);
  Check(boundsRule!=null,"Missing root-bone-space turret bounds repair");
  var nativeBounds=new UnityEngine.Bounds(new UnityEngine.Vector3(0f,1.58756f,1.450276f),new UnityEngine.Vector3(2.020158f,3.337322f,5.956352f));
  var repaired=(UnityEngine.Bounds)boundsRule.Invoke(null,new object[]{nativeBounds,1.2f});
  Check(repaired.center==nativeBounds.center,"Turret culling center moved into mesh coordinates");
  Check(repaired.extents.x>=nativeBounds.extents.x+1.2f&&repaired.extents.y>=nativeBounds.extents.y+1.2f&&repaired.extents.z>=nativeBounds.extents.z+1.2f,"Turret bounds can exclude the extended barrel at traverse/elevation limits");
  Console.WriteLine("PASS turret root-bone bounds retain the entire native turret and accommodate barrel extension in every aim direction");
  Type stab=a.GetType("T3485Quad30.Stabilization");
  var copyMode=stab.GetMethod("CopyNativeMode",BindingFlags.Public|BindingFlags.Static);Check(copyMode!=null,"Missing native Leopard lead configuration copier");
  var nativeFcs=(GHPC.Weapons.FireControlSystem)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GHPC.Weapons.FireControlSystem));
  var targetFcs=(GHPC.Weapons.FireControlSystem)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GHPC.Weapons.FireControlSystem));
  nativeFcs.RecordTraverseRateBuffer=true;nativeFcs.DynamicLead=true;nativeFcs.AimSlowDownRate=1.5f;
  copyMode.Invoke(null,new object[]{targetFcs,nativeFcs});
  Check(targetFcs.RecordTraverseRateBuffer&&targetFcs.DynamicLead&&targetFcs.AimSlowDownRate==1.5f,"Native lead acquisition cannot record traverse rate");
  var initialRange=typeof(GHPC.Weapons.FireControlSystem).GetField("_currentRange",BindingFlags.Instance|BindingFlags.NonPublic);
  initialRange.SetValue(nativeFcs,1000f);copyMode.Invoke(null,new object[]{targetFcs,nativeFcs});
  Check(targetFcs.CurrentRange==1000f,"WorldPoint inherits a zero T-34 initial range instead of Leopard 1000m");
  Console.WriteLine("PASS native lead acquisition records traverse rate and copies the Leopard tracking slowdown");
  Type mount=a.GetType("T3485Quad30.MissileLauncher");var top=mount.GetMethod("TopSurfaceHeight",BindingFlags.Public|BindingFlags.Static);
  Check(top!=null,"Hatch contact must use the face under the mount, not the highest hinge vertex");
  var plateVertices=new[]{new UnityEngine.Vector3(-1,0,-1),new UnityEngine.Vector3(1,0,-1),new UnityEngine.Vector3(0,0,1),new UnityEngine.Vector3(1.5f,2,1.5f),new UnityEngine.Vector3(2,2,1.5f),new UnityEngine.Vector3(1.5f,2,2)};
  float topY=(float)top.Invoke(null,new object[]{plateVertices,new[]{0,1,2,3,4,5},new[]{true,true,true,true,true,true},0f,0f});
  Check(topY==0f,"Raised hinge away from the mounting point makes the base float");
  Console.WriteLine("PASS hatch contact plane selects the actual face under the mount and excludes raised peripheral hinges");
  Type perf=a.GetType("T3485Quad30.PerformanceChanges");Check(perf!=null,"Engine/coax performance upgrade rules missing");
  var setEngine=perf.GetMethod("SetEnginePower");Check(setEngine!=null,"Engine power upgrade missing");
  var donorEngine=(NWH.VehiclePhysics.Engine)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(NWH.VehiclePhysics.Engine));
  var ownEngine=(NWH.VehiclePhysics.Engine)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(NWH.VehiclePhysics.Engine));donorEngine.maxPower=432f;ownEngine.maxPower=432f;
  setEngine.Invoke(null,new object[]{ownEngine,donorEngine});float boosted=ownEngine.maxPower;
  Check(Math.Abs((boosted-donorEngine.maxPower)*1.341f-250f)<0.001f&&donorEngine.maxPower==432f,"Power must add 250hp using the native kW-to-hp conversion");
  setEngine.Invoke(null,new object[]{ownEngine,donorEngine});Check(ownEngine.maxPower==boosted,"Engine increase compounded on repeat application");
  var durationField=typeof(GHPC.Weapons.AmmoFeed.ReloadStage).GetField("Duration",BindingFlags.NonPublic|BindingFlags.Instance);
  var cycles=new[]{new GHPC.Weapons.AmmoFeed.ReloadStage()};durationField.SetValue(cycles[0],0.1f);
  var boostCycle=perf.GetMethod("Boost30Cycle");Check(boostCycle!=null,"30mm cycle upgrade missing");
  var faster=(GHPC.Weapons.AmmoFeed.ReloadStage[])boostCycle.Invoke(null,new object[]{cycles});
  Check(Math.Abs(60f/faster.Sum(x=>(float)durationField.GetValue(x))-850f)<0.01f&&(float)durationField.GetValue(cycles[0])==0.1f&&!Object.ReferenceEquals(faster[0],cycles[0]),"600rpm feed must become 850rpm without mutating the donor");
  Console.WriteLine("PASS native Engine power +250hp in kW, repeat-safe; native 2A42 cycle 600 -> 850rpm with donor stages isolated");
  Type eraMath=a.GetType("T3485Quad30.EraMath");Check(eraMath!=null,"ERA geometry safety rules missing");
  var overlaps=eraMath.GetMethod("BoxesOverlap");Check(overlaps!=null,"ERA oriented-box overlap test missing");
  var zero=new UnityEngine.Vector3(0,0,0);var half=new UnityEngine.Vector3(0.16f,0.08f,0.03f);var identity=new UnityEngine.Quaternion(0,0,0,1);
  Func<UnityEngine.Vector3,UnityEngine.Quaternion,bool> overlap=(center,rotation)=>(bool)overlaps.Invoke(null,new object[]{zero,identity,half,center,rotation,half,0.005f});
  Check(overlap(new UnityEngine.Vector3(0.1f,0,0),identity),"Intersecting ERA tiles accepted");
  Check(overlap(new UnityEngine.Vector3(0.325f,0,0),identity),"ERA minimum gap not enforced");
  Check(!overlap(new UnityEngine.Vector3(0.36f,0,0),identity),"Separated ERA tiles rejected");
  var tilted=new UnityEngine.Quaternion(0,0,0.38268343f,0.92387953f);Check(overlap(new UnityEngine.Vector3(0.15f,0,0),tilted),"Rotated ERA collision missed");
  Console.WriteLine("PASS ERA box separation: overlaps and rotated intersections rejected; required gap enforced");
  var inside=eraMath.GetMethod("PointInsideMesh");Check(inside!=null,"ERA enclosed-volume fallback missing");
  var cube=new[]{new UnityEngine.Vector3(-1,-1,-1),new UnityEngine.Vector3(1,-1,-1),new UnityEngine.Vector3(1,1,-1),new UnityEngine.Vector3(-1,1,-1),new UnityEngine.Vector3(-1,-1,1),new UnityEngine.Vector3(1,-1,1),new UnityEngine.Vector3(1,1,1),new UnityEngine.Vector3(-1,1,1)};
  var faces=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
  Check((bool)inside.Invoke(null,new object[]{zero,cube,faces}),"Fully enclosed tile center escapes containment detection");
  Check(!(bool)inside.Invoke(null,new object[]{new UnityEngine.Vector3(1.1f,0,0),cube,faces}),"Exterior point incorrectly classified inside armor");
  var closed=eraMath.GetMethod("IsClosedMesh");Check(closed!=null,"Open renderer skins must not be treated as closed solids");
  Check((bool)closed.Invoke(null,new object[]{cube,faces}),"Closed armor shell topology not recognized");
  Check(!(bool)closed.Invoke(null,new object[]{cube,faces.Skip(6).ToArray()}),"Open armor shell wrongly enables solid containment");
  var hollow=cube.Concat(cube.Select(vertex=>vertex*0.5f)).ToArray();var shells=faces.Concat(faces.Select(i=>i+8)).ToArray();
  Check(!(bool)inside.Invoke(null,new object[]{zero,hollow,shells}),"Empty interior cavity treated as solid mesh");
  Check((bool)inside.Invoke(null,new object[]{new UnityEngine.Vector3(0.75f,0,0),hollow,shells}),"Closed mesh wall containment missed");
  Console.WriteLine("PASS ERA containment: enclosed volume detected; exterior and hollow cavity remain clear");
  var samplePitch=a.GetType("T3485Quad30.ReactiveArmor").GetMethod("PitchSamples",BindingFlags.NonPublic|BindingFlags.Static);
  var launcherElevation=(GHPC.AimablePlatform)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GHPC.AimablePlatform));
  launcherElevation.LocalEulerLimits=new UnityEngine.Vector2(-10f,20f);launcherElevation.ReverseLocalEuler=true;
  var pitchValues=(float[])samplePitch.Invoke(null,new object[]{launcherElevation});
  Check(pitchValues.Contains(-20f)&&pitchValues.Contains(10f)&&pitchValues.Contains(0f),"Launcher clearance misses its native reversed10-degree endpoint");
  Console.WriteLine("PASS ERA motion sampling includes reversed native launcher endpoints and neutral elevation");
  Type armor=a.GetType("T3485Quad30.ArmorChanges");Check(armor!=null,"T-34 armor upgrade rules missing");
  var applyUniform=armor.GetMethod("ApplyUniform",BindingFlags.Public|BindingFlags.Static);var applyVariable=armor.GetMethod("ApplyVariable",BindingFlags.Public|BindingFlags.Static);
  Check(applyUniform!=null&&applyVariable!=null,"Armor upgrade helpers missing");
  var uniform=(GHPC.UniformArmor)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GHPC.UniformArmor));uniform.PrimarySabotRha=45f;uniform.PrimaryHeatRha=45f;applyUniform.Invoke(null,new object[]{uniform,50f});Check(uniform.PrimarySabotRha==95f&&uniform.PrimaryHeatRha==95f,"Uniform hull armor did not gain exactly50mm");
  var variable=(GHPC.VariableArmor)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GHPC.VariableArmor));var average=typeof(GHPC.VariableArmor).GetField("AverageRha",BindingFlags.Instance|BindingFlags.NonPublic);average.SetValue(variable,50f);applyVariable.Invoke(null,new object[]{variable,50f});Check((float)average.GetValue(variable)==100f,"Variable turret armor did not gain exactly50mm");
  Console.WriteLine("PASS T-34 front/side/turret armor helpers add exactly50mm to uniform and variable armor");
  Type quad=a.GetType("T3485Quad30.QuadWeaponConversion");Check(quad!=null,"Quad 30mm installer missing");
  var cycle=quad.GetProperty("AggregateCycleSeconds",BindingFlags.Public|BindingFlags.Static);Check(cycle!=null,"Quad aggregate cycle property missing");
  float aggregate=(float)cycle.GetValue(null,null);Check(Math.Abs(aggregate-60f/3400f)<0.000001f,"Four 850rpm barrels must fire one left-to-right shot every 60/3400 seconds");
  Type core=a.GetType("T3485Quad30.ModCore");Check((float)core.GetField("PoweredTraverseDegreesPerSecond").GetValue(null)==27f&&(float)core.GetField("PoweredElevationDegreesPerSecond").GetValue(null)==31f,"Quad horizontal/vertical powered rates are wrong");
  Console.WriteLine("PASS quad 30mm: four native barrel cycle is 3400rpm aggregate / 850rpm per barrel; powered rates27/31 deg/s");
  Type geometryRules=a.GetType("T3485Quad30.QuadGeometry");Check(geometryRules!=null,"Quad mount/legacy geometry rules missing");
  var legacyRule=geometryRules.GetMethod("IsLegacyPart",BindingFlags.Public|BindingFlags.Static);
  Check((bool)legacyRule.Invoke(null,new object[]{"gun breech AAR"})&&(bool)legacyRule.Invoke(null,new object[]{"mantlet armor"}),"Old85mm breech and shield must be removed");
  Check(!(bool)legacyRule.Invoke(null,new object[]{"MANTLET"})&&!(bool)legacyRule.Invoke(null,new object[]{"MUZZLE"})&&!(bool)legacyRule.Invoke(null,new object[]{"T34_Turret_armour"}),"Legacy cleanup must keep drive bones and turret armor active");
  var retainTriangles=geometryRules.GetMethod("RetainTriangles",BindingFlags.Public|BindingFlags.Static);
  var retained=(int[])retainTriangles.Invoke(null,new object[]{new[]{0,1,2,1,3,2},new[]{true,false,false,false}});
  Check(retained.SequenceEqual(new[]{1,3,2}),"Removing old85mm triangles must preserve unrelated turret faces");
  var mountZ=geometryRules.GetMethod("MuzzleOffset",BindingFlags.Public|BindingFlags.Static);
  float zMount=(float)mountZ.Invoke(null,new object[]{-2.299316f,0.17f,0.015f});
  Check(Math.Abs(zMount-2.454316f)<0.00001f,"2A42 rear must be embedded15mm into the mount face");
  var mountWidth=geometryRules.GetField("MountWidth",BindingFlags.Public|BindingFlags.Static);var mountHeight=geometryRules.GetField("MountHeight",BindingFlags.Public|BindingFlags.Static);var mountDepth=geometryRules.GetField("MountDepth",BindingFlags.Public|BindingFlags.Static);var mountCenter=geometryRules.GetField("MountCenterOffset",BindingFlags.Public|BindingFlags.Static);
  Check(Math.Abs((float)mountWidth.GetValue(null)-1.25f)<0.0001f&&Math.Abs((float)mountHeight.GetValue(null)-0.52f)<0.0001f&&Math.Abs((float)mountDepth.GetValue(null)-0.23f)<0.0001f&&Math.Abs((float)mountCenter.GetValue(null)-0.055f)<0.0001f,"Compact quad breech dimensions or front-face compensation are wrong");
  Type paint=a.GetType("T3485Quad30.ShieldPaint");Check(paint!=null,"Shield paint must crop an actual original T34 texture patch instead of using a hand-picked solid color");
  var cropRegion=paint.GetMethod("CropRegion",BindingFlags.Public|BindingFlags.Static);Check(cropRegion!=null,"Shield texture crop rules missing");
  Check(((int[])cropRegion.Invoke(null,new object[]{2048,2048})).SequenceEqual(new[]{80,384,64,64}),"2048px vehicle atlas must crop only the selected paint patch");
  Check(((int[])cropRegion.Invoke(null,new object[]{1024,1024})).SequenceEqual(new[]{40,192,32,32}),"Paint-mask crop must follow the same normalized region at its own resolution");
  bool invalidCropRejected=false;try{cropRegion.Invoke(null,new object[]{128,128});}catch(TargetInvocationException e){invalidCropRejected=e.InnerException is ArgumentException;}Check(invalidCropRejected,"BC-compressed crops must reject a region that is not block-aligned");
  Check(geometryRules.GetField("T34SolidColor",BindingFlags.Public|BindingFlags.Static)==null,"Hand-tuned solid-color shield path must be removed");
  Console.WriteLine("PASS shield texture region: native paint and mask resolutions stay aligned; invalid BC block crops rejected; no hand-tuned solid color");
  var outer=quad.GetField("OuterBarrelOffset",BindingFlags.Public|BindingFlags.Static);var inner=quad.GetField("InnerBarrelOffset",BindingFlags.Public|BindingFlags.Static);Check(Math.Abs((float)outer.GetValue(null)-0.40f)<0.0001f&&Math.Abs((float)inner.GetValue(null)-0.22f)<0.0001f,"Quad barrels are not using the compact horizontal layout");
  Console.WriteLine("PASS quad mount: legacy geometry removed without disabling MANTLET; unrelated triangles retained; compact 1.25m breech plate; horizontal barrel spacing tightened; receiver rear embeds15mm into plate");
  var cloneRound=quad.GetMethod("CloneAmmo",BindingFlags.NonPublic|BindingFlags.Static);
  var fragSource=new AmmoType();fragSource.Name="native30mm";fragSource.RhaPenetration=51f;fragSource.MuzzleVelocity=970f;fragSource.TntEquivalentKg=0.01f;fragSource.DetonateSpallCount=37;fragSource.SpallMultiplier=2.3f;fragSource.SphericalSpall=true;fragSource.MinSpallRha=3f;fragSource.MaxSpallRha=9f;fragSource.ForcedSpallAngle=51f;fragSource.MicroFragScaling=1.7f;
  var apRound=(AmmoType)cloneRound.Invoke(null,new object[]{fragSource,"3UBR11",163f,1210f,0f});
  var heRound=(AmmoType)cloneRound.Invoke(null,new object[]{fragSource,"3UOF21",10f,920f,0.049f});
  foreach(var round in new[]{apRound,heRound})Check(round.DetonateSpallCount==37&&round.SpallMultiplier==2.3f&&round.SphericalSpall&&round.MinSpallRha==3f&&round.MaxSpallRha==9f&&round.ForcedSpallAngle==51f&&round.MicroFragScaling==1.7f,"Cloning quad ammunition changed inherited native30mm fragment parameters");
  Check(fragSource.RhaPenetration==51f&&fragSource.MuzzleVelocity==970f&&fragSource.TntEquivalentKg==0.01f&&heRound.TntEquivalentKg==0.049f&&apRound.RhaPenetration==163f,"Quad ammunition modified its native donor");
  Console.WriteLine("PASS quad ammunition: specified ballistic values and TNT; all native30mm fragmentation fields retained; donor isolated");
  return 0;
 }
}
