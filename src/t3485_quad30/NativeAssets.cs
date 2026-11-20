using System;
using System.Collections.Generic;
using GHPC.Mission;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace T3485Quad30 {
 internal sealed class NativeAssets {
  readonly Dictionary<string,AsyncOperationHandle<GameObject>> pending=new Dictionary<string,AsyncOperationHandle<GameObject>>();
  readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
  readonly string[] required={"T3485","BMP2","LEO2A3","BMP1P"};
  public bool Poll(UnitPrefabLookupScriptable[] lookups){
   foreach(string key in required){
    if(prefabs.ContainsKey(key))continue;
    AsyncOperationHandle<GameObject> handle;
    if(!pending.TryGetValue(key,out handle)){
     UnitPrefabLookupScriptable.UnitPrefabMetadata entry=null;
     foreach(var lookup in lookups){if(lookup==null||lookup.AllUnits==null)continue;foreach(var e in lookup.AllUnits)if(e!=null&&e.Name==key){entry=e;break;}if(entry!=null)break;}
     if(entry==null||entry.PrefabReference==null)continue;
     handle=Addressables.LoadAssetAsync<GameObject>(entry.PrefabReference.RuntimeKey);pending.Add(key,handle);
    }
    if(!handle.IsDone)continue;
    if(handle.Status!=AsyncOperationStatus.Succeeded||handle.Result==null)throw new InvalidOperationException("Cannot load native prefab "+key,handle.OperationException);
    prefabs.Add(key,handle.Result);
   }
   return prefabs.Count==required.Length;
  }
  public GameObject this[string key]{get{return prefabs[key];}}
  public void Release(){foreach(var pair in pending)if(pair.Value.IsValid())Addressables.Release(pair.Value);pending.Clear();prefabs.Clear();}
 }
}
