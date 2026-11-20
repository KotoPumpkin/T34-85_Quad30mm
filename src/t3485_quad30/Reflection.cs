using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace T3485Quad30 {
 internal static class R {
  public static FieldInfo Field(Type type,string name){var f=AccessTools.Field(type,name);if(f==null)throw new MissingFieldException(type.FullName,name);return f;}
  public static T Get<T>(object obj,string name){return (T)Field(obj.GetType(),name).GetValue(obj);}
  public static void Set(object obj,string name,object value){Field(obj.GetType(),name).SetValue(obj,value);}
  public static Transform Find(Transform root,string exact){foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==exact)return t;return null;}
  public static T Required<T>(T value,string message) where T:UnityEngine.Object {if(value==null)throw new InvalidOperationException(message);return value;}
 }
}
