using System;
using System.Collections.Generic;
using System.Text;
using GHPC;
using UnityEngine;
namespace T3485Quad30 {
 public sealed class ReactiveArmorState:MonoBehaviour {
  public BoxCollider[] Tiles;
  public MeshRenderer[] Visuals;
  public string[] Regions;
  public Vector3 TileSize;
  public string LayoutSummary;
  public int MotionPoses;
 }
 internal static class ReactiveArmor {
  internal sealed class Cell {
   public string Region;public Transform Part;public Vector3 LocalPosition;public Quaternion LocalRotation;public bool Safe=true;
  }
  static Vector3 Position(GameObject root,Cell cell){return root.transform.InverseTransformPoint(cell.Part.TransformPoint(cell.LocalPosition));}
  static Quaternion Rotation(GameObject root,Cell cell){return Quaternion.Inverse(root.transform.rotation)*cell.Part.rotation*cell.LocalRotation;}
  static void Offer(GameObject root,EraGeometry geometry,List<Cell> cells,Transform part,string region,Vector3 origin,Vector3 ray,Vector3 up,Vector3 size){
   Vector3 center;Quaternion rotation;var surface=region.StartsWith("Hull",StringComparison.Ordinal)?geometry.Hull:geometry.Turret;
   if(!geometry.TryFit(surface,origin,ray,up,size,out center,out rotation))return;
   // A complete yaw sweep of the gun forms this lower envelope. Hull tiles must remain beneath it.
   if(region.StartsWith("Hull",StringComparison.Ordinal)){
    var pivot=root.transform.InverseTransformPoint(R.Find(root.transform,"TURRET").position);var gun=root.transform.InverseTransformPoint(R.Find(root.transform,"MANTLET").position);var b=EraMath.BoxBounds(center,rotation,size*0.5f);
    float radius=new Vector2(center.x-pivot.x,center.z-pivot.z).magnitude+new Vector2(b.extents.x,b.extents.z).magnitude;
    float lower=gun.y-Mathf.Tan(5f*Mathf.Deg2Rad)*Mathf.Max(0f,radius-0.93f)-0.22f;
    if(b.max.y>lower-0.025f)return;
   }
   foreach(var other in cells)if(EraMath.BoxesOverlap(center,rotation,size*0.5f,Position(root,other),Rotation(root,other),size*0.5f,0.006f))return;
   cells.Add(new Cell{Region=region,Part=part,LocalPosition=part.InverseTransformPoint(root.transform.TransformPoint(center)),LocalRotation=Quaternion.Inverse(part.rotation)*root.transform.rotation*rotation});
  }
  static List<Cell> Layout(GameObject root,EraGeometry geometry,Vector3 size){
   var cells=new List<Cell>();var hull=R.Required(R.Find(root.transform,"HULL"),"Hull bone missing");var turret=R.Required(R.Find(root.transform,"TURRET"),"Turret bone missing");
   float horizontal=size.x+0.014f,vertical=size.y+0.014f;int row=0;
   for(float y=0.98f;y<=1.63f;y+=vertical*0.25f,row++)for(float x=-1.13f+((row/3)%2)*horizontal*0.5f;x<=1.14f;x+=horizontal*0.5f)Offer(root,geometry,cells,hull,"HullFront",new Vector3(x,y,5f),Vector3.back,Vector3.up,size);
   // Fill the clear lower glacis too; spare tracks and towing fittings still
   // reject their own footprints through the same surface/obstruction checks.
   row=0;for(float y=0.50f;y<0.98f;y+=vertical*0.25f,row++)for(float x=-1.13f+((row/3)%2)*horizontal*0.5f;x<=1.14f;x+=horizontal*0.5f)Offer(root,geometry,cells,hull,"HullFront",new Vector3(x,y,5f),Vector3.back,Vector3.up,size);
   for(int side=-1;side<=1;side+=2){row=0;for(float y=1.17f;y<=1.64f;y+=vertical*0.25f,row++)for(float z=-2.2f+((row/3)%2)*horizontal*0.5f;z<=2.71f;z+=horizontal*0.5f)Offer(root,geometry,cells,hull,side<0?"HullLeft":"HullRight",new Vector3(side*4f,y,z),new Vector3(-side,0,0),Vector3.up,size);}
   row=0;for(float y=1.98f;y<=2.55f;y+=vertical*0.25f,row++)for(float x=-0.86f+((row/4)%2)*horizontal*0.5f;x<=0.87f;x+=horizontal*0.5f)Offer(root,geometry,cells,turret,"TurretFront",new Vector3(x,y,3.3f),Vector3.back,Vector3.up,size);
   // Oblique front cheeks have enough cast armor outside the moving mantlet.
   for(int side=-1;side<=1;side+=2){Vector3 direction=new Vector3(-side,0,-1).normalized,tangent=new Vector3(1,0,-side).normalized;
    for(float y=2.00f;y<=2.64f;y+=vertical*0.25f)for(float offset=-0.5f;offset<=0.51f;offset+=horizontal*0.5f){Vector3 target=new Vector3(side*0.7f,y,1.92f)+tangent*offset;Offer(root,geometry,cells,turret,"TurretFront",target-direction*3f,direction,Vector3.up,size);}
   }
   for(int side=-1;side<=1;side+=2){row=0;for(float y=2.00f;y<=2.55f;y+=vertical*0.25f,row++)for(float z=-0.35f+((row/4)%2)*horizontal*0.5f;z<=1.66f;z+=horizontal*0.5f)Offer(root,geometry,cells,turret,side<0?"TurretLeft":"TurretRight",new Vector3(side*3f,y,z),new Vector3(-side,0,0),Vector3.up,size);}
   row=0;for(float z=0.82f;z<=1.66f;z+=vertical*0.5f,row++)for(float x=-0.72f+((row/2)%2)*horizontal*0.5f;x<=0.73f;x+=horizontal*0.5f)Offer(root,geometry,cells,turret,"TurretRoof",new Vector3(x,4f,z),Vector3.down,Vector3.forward,size);
   return cells;
  }
  static void RemoveIntersections(GameObject root,EraGeometry geometry,List<Cell> cells,Vector3 size){
   geometry.Refresh();
   foreach(var cell in cells)if(cell.Safe&&!geometry.Clear(Position(root,cell),Rotation(root,cell),size,cell.Region.StartsWith("Hull",StringComparison.Ordinal)?geometry.Hull:geometry.Turret,false))cell.Safe=false;
   for(int i=0;i<cells.Count;i++)if(cells[i].Safe)for(int j=i+1;j<cells.Count;j++)if(cells[j].Safe&&cells[i].Part!=cells[j].Part&&EraMath.BoxesOverlap(Position(root,cells[i]),Rotation(root,cells[i]),size*0.5f,Position(root,cells[j]),Rotation(root,cells[j]),size*0.5f,0.003f)){
    if(cells[i].Region.StartsWith("Hull",StringComparison.Ordinal)){cells[i].Safe=false;break;}cells[j].Safe=false;
   }
  }
  static float[] PitchSamples(AimablePlatform mount){float sign=mount.ReverseLocalEuler?-1f:1f;float a=mount.LocalEulerLimits.x*sign,b=mount.LocalEulerLimits.y*sign;return new[]{a,a*0.5f,0f,b*0.5f,b};}
  static int MotionFilter(GameObject root,EraGeometry geometry,List<Cell> cells,Vector3 size){
   var turret=R.Find(root.transform,"TURRET");var gun=R.Find(root.transform,"MANTLET");var state=root.GetComponent<RuntimeState>();var az=state.LauncherFcs.Mounts[0].Transform;var el=state.LauncherFcs.Mounts[1].Transform;
   Quaternion tq=turret.localRotation,gq=gun.localRotation,aq=az.localRotation,eq=el.localRotation;int count=0;
   try{
    for(int yaw=0;yaw<360;yaw+=15)foreach(float pitch in PitchSamples(gun.GetComponent<AimablePlatform>())){turret.localRotation=tq*Quaternion.Euler(0,yaw,0);gun.localRotation=gq*Quaternion.Euler(pitch,0,0);RemoveIntersections(root,geometry,cells,size);count++;}
    turret.localRotation=tq;gun.localRotation=gq;
    for(int yaw=0;yaw<360;yaw+=15)foreach(float pitch in PitchSamples(state.LauncherFcs.Mounts[1])){az.localRotation=aq*Quaternion.Euler(0,yaw,0);el.localRotation=eq*Quaternion.Euler(pitch,0,0);RemoveIntersections(root,geometry,cells,size);count++;}
   }finally{turret.localRotation=tq;gun.localRotation=gq;az.localRotation=aq;el.localRotation=eq;geometry.Refresh();}
   return count;
  }
  static Transform Group(Transform part,string name){var go=new GameObject(name);go.transform.SetParent(part,false);return go.transform;}
  public static ReactiveArmorState Install(GameObject root,FuldaEraAdapter adapter){
   var tiles=new List<BoxCollider>();var visuals=new List<MeshRenderer>();var regions=new List<string>();var summary=new Dictionary<string,int>();int poses;
   using(var geometry=new EraGeometry(root)){
    var cells=Layout(root,geometry,adapter.TileSize);MelonLoader.MelonLogger.Msg("[T3485Q30MOD] ERA fitted candidate cells="+cells.Count);poses=MotionFilter(root,geometry,cells,adapter.TileSize);
    var parts=new Dictionary<Transform,List<Cell>>();foreach(var cell in cells)if(cell.Safe){List<Cell> list;if(!parts.TryGetValue(cell.Part,out list)){list=new List<Cell>();parts.Add(cell.Part,list);}list.Add(cell);}
    foreach(var pair in parts){
     var mount=Group(pair.Key,"T3485Q30MOD_ERA_"+pair.Key.name);Transform coll=Group(mount,"Colliders"),vis=Group(mount,"Visuals");
     foreach(var cell in pair.Value){string name=cell.Region+"_"+tiles.Count;adapter.AddTile(coll,vis,name,cell.LocalPosition,cell.LocalRotation);tiles.Add(coll.GetChild(coll.childCount-1).GetComponent<BoxCollider>());visuals.Add(vis.GetChild(vis.childCount-1).GetComponent<MeshRenderer>());regions.Add(cell.Region);if(!summary.ContainsKey(cell.Region))summary[cell.Region]=0;summary[cell.Region]++;}
     adapter.SetupGroup(coll,vis);
    }
   }
   var text=new StringBuilder();foreach(string region in new[]{"HullFront","HullLeft","HullRight","TurretFront","TurretLeft","TurretRight","TurretRoof"}){
    int count;summary.TryGetValue(region,out count);text.Append(region+"="+count+"; ");if(count==0)throw new InvalidOperationException("No safe ERA mounting cells for "+region);
   }
   var state=root.AddComponent<ReactiveArmorState>();state.Tiles=tiles.ToArray();state.Visuals=visuals.ToArray();state.Regions=regions.ToArray();state.TileSize=adapter.TileSize;state.LayoutSummary=text.ToString();state.MotionPoses=poses;return state;
  }
  public static string VerifyClearance(GameObject root){
   var state=root.GetComponent<ReactiveArmorState>();var cells=new List<Cell>();
   for(int i=0;i<state.Tiles.Length;i++)cells.Add(new Cell{Region=state.Regions[i],Part=state.Tiles[i].transform,LocalPosition=Vector3.zero,LocalRotation=Quaternion.identity});
   int poses;using(var geometry=new EraGeometry(root)){poses=MotionFilter(root,geometry,cells,state.TileSize);}
   foreach(var cell in cells)if(!cell.Safe)throw new InvalidOperationException("ERA clearance regression: "+cell.Region+" has motion interference");
   var first=state.Tiles[0].GetComponent<UniformArmor>();var next=state.Tiles[1].GetComponent<UniformArmor>();
   R.Set(first,"_isDetonated",true);
   try{if(first.HeatRha!=0f||first.SabotRha!=0f||next.IsDetonated||next.PrimaryHeatRha!=181f)throw new InvalidOperationException("ERA spent block affects another tile or keeps its active resistance");}
   finally{R.Set(first,"_isDetonated",false);}
   return "PASS ERA clearance: "+state.Tiles.Length+" blocks; 25 footprint samples per placement; "+poses+" turret/gun/launcher motion poses; no mesh penetration, enclosed tile centers or tile overlap in sampled poses; independent native spent state verified.";
  }
  public static string Audit(GameObject root){
   var state=R.Required(root.GetComponent<ReactiveArmorState>(),"ERA state missing");if(state.Tiles.Length<7||state.Tiles.Length!=state.Visuals.Length)throw new InvalidOperationException("ERA tile pairing incomplete");
   for(int i=0;i<state.Tiles.Length;i++){
    var armor=R.Required(state.Tiles[i].GetComponent<UniformArmor>(),"ERA collision block has no native armor");
    if(!armor.IsEra||Mathf.Abs(armor.PrimaryHeatRha-181f)>0.001f||Mathf.Abs(armor.PrimarySabotRha-7f)>0.001f||armor.SecondaryHeatRha!=0f||armor.SecondarySabotRha!=0f)throw new InvalidOperationException("Tile does not use Fulda Kontakt-1 schema");
    if(!state.Tiles[i].transform.IsChildOf(root.transform)||!state.Visuals[i].transform.IsChildOf(root.transform))throw new InvalidOperationException("ERA points to another vehicle");
    var type=HarmonyLib.AccessTools.TypeByName("Fulda1989.EraVisuals");var behavior=state.Tiles[i].GetComponent(type);if(behavior==null||R.Get<MeshRenderer>(behavior,"visual")!=state.Visuals[i])throw new InvalidOperationException("ERA detonation visual mapping failed");
   }
   return "PASS Fulda Kontakt-1: "+state.Tiles.Length+" independently paired ERA tiles; native CE181/KE7, spent CE0/KE0; "+state.LayoutSummary+"motion poses="+state.MotionPoses+"; unsafe mounting cells omitted.";
  }
 }
}
