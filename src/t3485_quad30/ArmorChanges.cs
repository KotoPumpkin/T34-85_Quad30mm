using System;
using System.Collections.Generic;
using GHPC;
using UnityEngine;
namespace T3485Quad30 {
 public sealed class ArmorUpgradeState:MonoBehaviour {
  public float BonusMm;
  public int ComponentCount;
  public string Summary;
 }
 internal static class ArmorChanges {
  public const float BonusMm=50f;
  static readonly HashSet<string> HullFrontNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"front glacis","hull lumps","driver's hatch","shutters"};
  static readonly HashSet<string> HullSideNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"45mm side plate","10mm sponson plate"};
  internal sealed class Snapshot {
   internal readonly Dictionary<string,Value> Values;
   internal Snapshot(Dictionary<string,Value> values){Values=values;}
  }
  internal struct Value {
   internal bool Variable;internal float Sabot;internal float Heat;internal float Average;
  }
  sealed class Slot {
   internal string Key;internal string Group;internal string Part;internal UniformArmor Uniform;internal VariableArmor Variable;
  }
  public static void ApplyUniform(UniformArmor armor,float bonus){if(System.Object.ReferenceEquals(armor,null))throw new ArgumentNullException("armor");armor.PrimarySabotRha+=bonus;armor.PrimaryHeatRha+=bonus;}
  public static void ApplyVariable(VariableArmor armor,float bonus){if(System.Object.ReferenceEquals(armor,null))throw new ArgumentNullException("armor");R.Set(armor,"AverageRha",R.Get<float>(armor,"AverageRha")+bonus);}
  static Transform Group(GameObject root,string name){return R.Required(R.Find(root.transform,name),"T-34 armor group missing: "+name);}
  static void AddSlot(List<Slot> slots,string group,Transform part){
   var uniform=part.GetComponent<UniformArmor>();var variable=part.GetComponent<VariableArmor>();if(uniform==null&&variable==null)return;
   slots.Add(new Slot{Key=group+"/"+part.name,Group=group,Part=part.name,Uniform=uniform,Variable=variable});
  }
  static List<Slot> Collect(GameObject root){
   var slots=new List<Slot>();var hull=Group(root,"T34_Hull_armour");
   foreach(Transform part in hull)if(HullFrontNames.Contains(part.name)||HullSideNames.Contains(part.name))AddSlot(slots,"HullFrontSide",part);
   var turret=Group(root,"T34_Turret_armour");foreach(Transform part in turret)AddSlot(slots,"TurretAll",part);
   return slots;
  }
  internal static Snapshot Capture(GameObject root){
   var values=new Dictionary<string,Value>();foreach(var slot in Collect(root)){if(values.ContainsKey(slot.Key))throw new InvalidOperationException("Duplicate T-34 armor slot: "+slot.Key);if(slot.Uniform!=null)values.Add(slot.Key,new Value{Variable=false,Sabot=slot.Uniform.PrimarySabotRha,Heat=slot.Uniform.PrimaryHeatRha});else values.Add(slot.Key,new Value{Variable=true,Average=R.Get<float>(slot.Variable,"AverageRha")});}return new Snapshot(values);
  }
  public static string Install(GameObject root){
   if(root.GetComponent<ArmorUpgradeState>()!=null)throw new InvalidOperationException("T-34 armor upgrade already installed");
   var slots=Collect(root);if(slots.Count!=18)throw new InvalidOperationException("Expected 18 T-34 front/side/turret armor components, found "+slots.Count);
   foreach(var slot in slots)if(slot.Uniform!=null)ApplyUniform(slot.Uniform,BonusMm);else ApplyVariable(slot.Variable,BonusMm);
   var state=root.AddComponent<ArmorUpgradeState>();state.BonusMm=BonusMm;state.ComponentCount=slots.Count;state.Summary="HullFrontSide=6; TurretAll=12; +50mm";return "PASS T-34 armor: front/side structural hull and all T34_Turret_armour components +50mm; 18 components; donor isolated.";
  }
  internal static void AssertUnchanged(Snapshot snapshot,GameObject donor){var now=Capture(donor);if(now.Values.Count!=snapshot.Values.Count)throw new InvalidOperationException("Original T-34 armor component count changed");foreach(var pair in snapshot.Values){Value value;if(!now.Values.TryGetValue(pair.Key,out value)||value.Variable!=pair.Value.Variable||Mathf.Abs(value.Sabot-pair.Value.Sabot)>0.001f||Mathf.Abs(value.Heat-pair.Value.Heat)>0.001f||Mathf.Abs(value.Average-pair.Value.Average)>0.001f)throw new InvalidOperationException("Original T-34 armor changed: "+pair.Key);}}
  public static string Audit(GameObject root){
   var state=R.Required(root.GetComponent<ArmorUpgradeState>(),"T-34 armor upgrade state missing");if(Mathf.Abs(state.BonusMm-BonusMm)>0.001f||state.ComponentCount!=18)throw new InvalidOperationException("T-34 armor upgrade metadata is wrong");var slots=Collect(root);if(slots.Count!=18)throw new InvalidOperationException("T-34 armor target set changed: "+slots.Count);
   foreach(var slot in slots){if(slot.Uniform!=null){if(Mathf.Abs(slot.Uniform.PrimarySabotRha-ExpectedUniform(slot.Key,true))>0.001f||Mathf.Abs(slot.Uniform.PrimaryHeatRha-ExpectedUniform(slot.Key,false))>0.001f)throw new InvalidOperationException("T-34 uniform armor bonus missing: "+slot.Key);}else{float expected=ExpectedVariable(slot.Key);if(Mathf.Abs(R.Get<float>(slot.Variable,"AverageRha")-expected)>0.001f)throw new InvalidOperationException("T-34 variable armor bonus missing: "+slot.Key);}}
   return "PASS T-34 armor: front/side structural hull and all turret armor +50mm; 18 components; front glacis=60mm, side plate=95mm, turret casting=100mm.";
  }
  static float ExpectedUniform(string key,bool sabot){
   if(key.EndsWith("/45mm side plate",StringComparison.OrdinalIgnoreCase))return 95f;
   if(key.EndsWith("/10mm sponson plate",StringComparison.OrdinalIgnoreCase))return 60f;
   if(key.EndsWith("/hull lumps",StringComparison.OrdinalIgnoreCase))return 55f;
   if(key.StartsWith("TurretAll/",StringComparison.OrdinalIgnoreCase)){if(key.EndsWith("/commander hatch",StringComparison.OrdinalIgnoreCase)||key.EndsWith("/loader hatch",StringComparison.OrdinalIgnoreCase)||key.EndsWith("/mantlet plate",StringComparison.OrdinalIgnoreCase))return 70f;return 60f;}
   throw new InvalidOperationException("Unknown T-34 uniform armor target: "+key);
  }
  static float ExpectedVariable(string key){
   if(key.EndsWith("/front glacis",StringComparison.OrdinalIgnoreCase)||key.EndsWith("/driver's hatch",StringComparison.OrdinalIgnoreCase)||key.EndsWith("/shutters",StringComparison.OrdinalIgnoreCase))return 60f;
   if(key.EndsWith("/Object079",StringComparison.OrdinalIgnoreCase))return 60f;
   if(key.EndsWith("/turret casting",StringComparison.OrdinalIgnoreCase))return 100f;
   throw new InvalidOperationException("Unknown T-34 variable armor target: "+key);
  }
 }
}
