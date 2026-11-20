using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace T3485Quad30 {
 public static class ModCore {
  public const string TargetKey="T3485Q30MOD", DonorKey="T3485", DisplayName="T-34-85M Quad 30mm";
  public const float OriginalTraverseDegreesPerSecond=15f, PoweredTraverseDegreesPerSecond=27f, PoweredElevationDegreesPerSecond=31f;
  public const float Penetration85mmBonus=230f,NonHEVelocityBonus=500f,HEVelocityBonus=200f,HEExplosiveBonusKg=5f;
  public const float AdditionalEngineHorsepower=250f,EngineHorsepowerPerKilowatt=1.341f,AdditionalCoaxRoundsPerMinute=250f;
  public struct Ballistics { public float Penetration,Velocity; public Ballistics(float p,float v){Penetration=p;Velocity=v;} }
  public static bool IsHighExplosive(string name){
   if(String.IsNullOrEmpty(name))return false;
   string n=name.ToUpperInvariant();
   foreach(string token in n.Split(new[]{' ','_','/'},StringSplitOptions.RemoveEmptyEntries))
    if(token=="HE"||token=="HE-T"||token=="HE-FRAG"||token=="HEF-FS-T"||token=="HEFI"||token=="HEI-T")return true;
   return n=="O-365"||n.StartsWith("O-365 ")||n=="OF-350"||n.StartsWith("OF-350 ");
  }
  public static Ballistics Upgrade85mm(float penetration,float velocity,string ammoName){return IsHighExplosive(ammoName)?new Ballistics(penetration,velocity+HEVelocityBonus):new Ballistics(penetration+Penetration85mmBonus,velocity+NonHEVelocityBonus);}
 }
 public static class RegistryRules {
  public static Array Append(Array source){
   if(source==null)return source;object donor=null;
   foreach(object entry in source){if(entry==null)continue;string key=(string)entry.GetType().GetField("Name").GetValue(entry);if(key==ModCore.TargetKey)return source;if(key==ModCore.DonorKey)donor=entry;}
   if(donor==null)return source;
   object own=CloneObject(donor);Type t=donor.GetType();
   t.GetField("Name").SetValue(own,ModCore.TargetKey);t.GetField("FriendlyName").SetValue(own,ModCore.DisplayName);t.GetField("AllowInCustomizer").SetValue(own,true);
   Array result=Array.CreateInstance(source.GetType().GetElementType(),source.Length+1);Array.Copy(source,result,source.Length);result.SetValue(own,source.Length);return result;
  }
  public static object CloneObject(object source){return typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(source,null);}
 }
 public sealed class RefEquality<T>:IEqualityComparer<T> where T:class {
  public bool Equals(T a,T b){return Object.ReferenceEquals(a,b);}public int GetHashCode(T o){return RuntimeHelpers.GetHashCode(o);}
 }
}
