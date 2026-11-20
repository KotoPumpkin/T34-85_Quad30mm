using System;
using System.Collections.Generic;
using GHPC.Equipment.Optics;
using UnityEngine;
using U=UnityEngine.Object;
namespace T3485Quad30 {
 internal sealed class EraGeometry:IDisposable {
  internal sealed class Proxy {
   public Collider Collider;public Mesh Mesh;public Transform Source;public SkinnedMeshRenderer Skin;public MeshFilter Filter;public bool[] Mask;public bool AnyMasked;public Vector3[] Vertices;public int[] Triangles;public bool Closed;
  }
  public readonly GameObject Vehicle;
  readonly GameObject owner;
  readonly List<Proxy> all=new List<Proxy>();
  public readonly List<Proxy> Obstacles=new List<Proxy>();
  public readonly List<Bounds> Reserved=new List<Bounds>();
  public Proxy Hull,Turret;
  public BoxCollider Tester;
  public EraGeometry(GameObject vehicle){
   Vehicle=vehicle;owner=new GameObject("T3485Q30MOD_ERA_TemporaryGeometry");owner.hideFlags=HideFlags.HideAndDontSave;
   try{
   var hull=R.Required(R.Find(vehicle.transform,"lp_hull004"),"Native T-34 hull visual mesh missing");Hull=Add(hull.GetComponent<MeshFilter>(),null,null,false,"HullSurface");
   var turret=R.Required(R.Find(vehicle.transform,"lp_turret_cast001").GetComponent<SkinnedMeshRenderer>(),"Native T-34 turret skin missing");
   var weights=turret.sharedMesh.boneWeights;int bone=Array.IndexOf(turret.bones,R.Find(vehicle.transform,"TURRET"));var body=new bool[weights.Length];var extras=new bool[weights.Length];
   for(int i=0;i<weights.Length;i++){body[i]=Influence(weights[i],bone)>0.99f;extras[i]=!body[i];}
   Turret=Add(null,turret,body,false,"TurretSurface");Obstacles.Add(Add(null,turret,extras,true,"TurretAttachments"));
   var rig=R.Find(vehicle.transform,"T34_rig");
   foreach(var f in rig.GetComponentsInChildren<MeshFilter>(true)){
    if(f.transform==hull||f.sharedMesh==null||f.GetComponentInParent<UsableOptic>(true)!=null||UnderEra(f.transform)||!Visible(f.transform,vehicle.transform))continue;
    var renderer=f.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled)continue;
    if(!f.sharedMesh.isReadable){Obstacles.Add(AddBounds(f));continue;}
    Obstacles.Add(Add(f,null,null,false,"Part_"+f.name));
   }
   foreach(var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
    if(skin==turret||skin.sharedMesh==null||UnderEra(skin.transform)||skin.GetComponentInParent<UsableOptic>(true)!=null||!Visible(skin.transform,vehicle.transform)||!skin.enabled)continue;
    // Wheels and tracks are below the selected mounting bands and must remain clear too.
    var p=Add(null,skin,null,false,"Part_"+skin.name);Obstacles.Add(p);
    if(skin.name=="lp_driverhatch001"){Bounds b=p.Mesh.bounds;b.Expand(0.18f);Reserved.Add(b);}
   }
   // Reserve every separate hatch and periscope bound in the cast turret, including its immediate opening clearance.
   var raw=ReadVertices(null,turret);
   for(int j=0;j<turret.bones.Length;j++){
    string name=turret.bones[j].name;if(name.IndexOf("HATCH",StringComparison.OrdinalIgnoreCase)<0&&name.IndexOf("PERI",StringComparison.OrdinalIgnoreCase)<0)continue;
    bool first=true;Bounds b=new Bounds();for(int i=0;i<weights.Length;i++)if(Influence(weights[i],j)>0.5f){if(first){b=new Bounds(raw[i],Vector3.zero);first=false;}else b.Encapsulate(raw[i]);}
    if(!first){b.Expand(0.10f);Reserved.Add(b);}
   }
   var optic=vehicle.GetComponent<RuntimeState>();
   if(optic!=null&&optic.Day!=null){Vector3 pos=vehicle.transform.InverseTransformPoint(optic.Day.transform.position);Reserved.Add(new Bounds(pos+Vector3.forward*0.35f,new Vector3(0.36f,0.32f,0.9f)));}
   var test=new GameObject("ERA_tile_test");test.layer=31;test.transform.SetParent(owner.transform,false);Tester=test.AddComponent<BoxCollider>();Tester.isTrigger=true;
   Physics.SyncTransforms();
   }catch{Dispose();throw;}
  }
  static bool UnderEra(Transform t){for(;t!=null;t=t.parent)if(t.name.StartsWith("T3485Q30MOD_ERA",StringComparison.Ordinal))return true;return false;}
  static bool Visible(Transform t,Transform root){for(;t!=null&&t!=root;t=t.parent)if(!t.gameObject.activeSelf)return false;return true;}
  static float Influence(BoneWeight w,int bone){return (w.boneIndex0==bone?w.weight0:0f)+(w.boneIndex1==bone?w.weight1:0f)+(w.boneIndex2==bone?w.weight2:0f)+(w.boneIndex3==bone?w.weight3:0f);}
  Vector3[] ReadVertices(MeshFilter filter,SkinnedMeshRenderer skin){
   Vector3[] vertices;
   if(skin!=null){var temp=new Mesh();try{skin.BakeMesh(temp);vertices=temp.vertices;}finally{U.DestroyImmediate(temp);}for(int i=0;i<vertices.Length;i++)vertices[i]=Vehicle.transform.InverseTransformPoint(skin.transform.position+skin.transform.rotation*vertices[i]);}
   else{if(filter==null||!filter.sharedMesh.isReadable)throw new InvalidOperationException("ERA surface mesh is not readable");vertices=filter.sharedMesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=Vehicle.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));}
   return vertices;
  }
  Proxy Add(MeshFilter filter,SkinnedMeshRenderer skin,bool[] mask,bool any,string name){
   Mesh source=skin!=null?skin.sharedMesh:filter.sharedMesh;int[] triangles;
   if(source.isReadable)triangles=source.triangles;else{var temp=new Mesh();try{skin.BakeMesh(temp);triangles=temp.triangles;}finally{U.DestroyImmediate(temp);}}
   var accepted=new List<int>();
   for(int i=0;i<triangles.Length;i+=3){if(mask!=null){bool a=mask[triangles[i]],b=mask[triangles[i+1]],c=mask[triangles[i+2]];if(any?!(a||b||c):!(a&&b&&c))continue;}accepted.Add(triangles[i]);accepted.Add(triangles[i+1]);accepted.Add(triangles[i+2]);}
   var mesh=new Mesh();var p=new Proxy{Mesh=mesh,Source=skin!=null?skin.transform:filter.transform,Skin=skin,Filter=filter,Mask=mask,AnyMasked=any};all.Add(p);
   mesh.name="ERA_probe_"+name;p.Vertices=ReadVertices(filter,skin);p.Triangles=accepted.ToArray();mesh.vertices=p.Vertices;mesh.triangles=p.Triangles;mesh.RecalculateBounds();p.Closed=EraMath.IsClosedMesh(p.Vertices,p.Triangles);
   var go=new GameObject(name);go.layer=31;go.transform.SetParent(owner.transform,false);var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;p.Collider=collider;return p;
  }
  Proxy AddBounds(MeshFilter source){
   var go=new GameObject("GPU_only_"+source.name);go.layer=31;go.transform.SetParent(owner.transform,false);var box=go.AddComponent<BoxCollider>();var p=new Proxy{Collider=box,Source=source.transform,Filter=source};all.Add(p);RefreshBounds(p);return p;
  }
  void RefreshBounds(Proxy p){
   Bounds source=p.Filter.sharedMesh.bounds;bool first=true;Bounds target=new Bounds();
   for(int i=0;i<8;i++){Vector3 local=source.center+Vector3.Scale(source.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));Vector3 point=Vehicle.transform.InverseTransformPoint(p.Source.TransformPoint(local));if(first){target=new Bounds(point,Vector3.zero);first=false;}else target.Encapsulate(point);}
   var box=(BoxCollider)p.Collider;box.center=target.center;box.size=target.size;
  }
  public void Refresh(){foreach(var p in all){if(p.Mesh==null){RefreshBounds(p);continue;}var c=(MeshCollider)p.Collider;c.sharedMesh=null;p.Vertices=ReadVertices(p.Filter,p.Skin);p.Mesh.vertices=p.Vertices;p.Mesh.RecalculateBounds();c.sharedMesh=p.Mesh;}Physics.SyncTransforms();}
  public bool Hit(Proxy surface,Ray ray,out RaycastHit hit){return surface.Collider.Raycast(ray,out hit,8f);}
  static bool Contains(Proxy proxy,Vector3 point){return proxy.Mesh!=null&&proxy.Closed&&proxy.Mesh.bounds.Contains(point)&&EraMath.PointInsideMesh(point,proxy.Vertices,proxy.Triangles);}
  public bool Clear(Vector3 position,Quaternion rotation,Vector3 size,Proxy surface,bool reservations=true){
   Tester.size=size;Vector3 direction;float distance;
   if(Contains(surface,position))return false;
   if(Physics.ComputePenetration(Tester,position,rotation,surface.Collider,Vector3.zero,Quaternion.identity,out direction,out distance)&&distance>0.0001f)return false;
   foreach(var p in Obstacles){if(Contains(p,position))return false;if(Physics.ComputePenetration(Tester,position,rotation,p.Collider,Vector3.zero,Quaternion.identity,out direction,out distance)&&distance>0.0001f)return false;}
   var other=surface==Hull?Turret:Hull;if(Contains(other,position))return false;
   if(Physics.ComputePenetration(Tester,position,rotation,other.Collider,Vector3.zero,Quaternion.identity,out direction,out distance)&&distance>0.0001f)return false;
   if(reservations)foreach(var b in Reserved)if(EraMath.BoxesOverlap(position,rotation,size*0.5f,b.center,Quaternion.identity,b.extents,0.02f))return false;
   return true;
  }
  public bool TryFit(Proxy surface,Vector3 origin,Vector3 direction,Vector3 up,Vector3 size,out Vector3 center,out Quaternion rotation){
   center=Vector3.zero;rotation=Quaternion.identity;RaycastHit first;if(!Hit(surface,new Ray(origin,direction),out first)||Vector3.Dot(first.normal,-direction)<0.35f)return false;
   Vector3 normal=first.normal.normalized;up=Vector3.ProjectOnPlane(up,normal).normalized;if(up.sqrMagnitude<0.1f)return false;rotation=Quaternion.LookRotation(normal,up);Vector3 right=rotation*Vector3.right;float highest=float.NegativeInfinity;
   for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++){
    Vector3 p=first.point+right*(x*size.x*0.25f)+up*(y*size.y*0.25f);var ray=new Ray(p+normal*0.7f,-normal);RaycastHit hit;
    if(!Hit(surface,ray,out hit)||Vector3.Dot(hit.normal,normal)<0.65f)return false;
    float height=Vector3.Dot(hit.point-first.point,normal);highest=Mathf.Max(highest,height);
    foreach(var obstacle in Obstacles){
     RaycastHit blocked;
     if(obstacle.Collider.Raycast(ray,out blocked,0.72f)&&blocked.distance<=hit.distance+0.025f){
      float above=hit.distance-blocked.distance;
      if(above<=0.20f)return false; // Attached fittings; distant overhead weapons have a separate motion check.
      RaycastHit entry;
      if(obstacle.Collider.Raycast(new Ray(hit.point-normal*0.35f,normal),out entry,0.50f))return false; // A closed fitting can enclose the whole tile without crossing its faces.
     }
    }
   }
   if(highest>0.07f)return false;center=first.point+normal*(highest+0.0015f+size.z*0.5f);
   return Clear(center,rotation,size,surface);
  }
  public void Dispose(){U.DestroyImmediate(owner);foreach(var p in all)if(p.Mesh!=null)U.DestroyImmediate(p.Mesh);all.Clear();}
 }
}
