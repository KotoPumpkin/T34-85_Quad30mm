using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
namespace T3485Quad30 {
 // A real Addressables location allows every native spawner path to use the prepared prefab.
 // Release deliberately leaves lifetime with the inactive persistent template owner.
 internal sealed class PrefabProvider:ResourceProviderBase {
  internal static GameObject Template;
  public override string ProviderId {get{return "local.T3485Quad30.PrefabProvider";}}
  public override void Provide(ProvideHandle handle){
   GameObject p=Template;handle.Complete<GameObject>(p,p!=null,p==null?new InvalidOperationException("T3485Q30MOD prefab is not ready"):null);
  }
  public override void Release(IResourceLocation location,object asset){}
 }
 internal sealed class PrefabLocator:IResourceLocator {
  internal const string Guid="a64d9b711f7348c4b6e44d0f5f0c8e31";
  readonly List<IResourceLocation> locations;
  public PrefabLocator(string provider){locations=new List<IResourceLocation>{new ResourceLocationBase(Guid,"runtime://T3485Q30MOD",provider,typeof(GameObject),new IResourceLocation[0])};}
  public string LocatorId {get{return "T3485Quad30.Runtime";}}
  public IEnumerable<object> Keys {get{return new object[]{Guid,ModCore.TargetKey};}}
  public bool Locate(object key,Type type,out IList<IResourceLocation> found){
   string name=key as string;if((name==Guid||name==ModCore.TargetKey)&&(type==null||type.IsAssignableFrom(typeof(GameObject)))){found=locations;return true;}found=null;return false;
  }
  public static AssetReference Install(GameObject template){PrefabProvider.Template=template;var provider=new PrefabProvider();Addressables.ResourceManager.ResourceProviders.Add(provider);Addressables.AddResourceLocator(new PrefabLocator(provider.ProviderId));return new AssetReference(Guid);}
 }
}
