using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace T3485Quad30 {
 // Borrow the loaded Fulda asset and invoke its original armor setup. Never clone
 // the donor GameObject: it can already contain configured ERA MonoBehaviours.
 internal sealed class FuldaEraAdapter {
  const string AssetTypeName="Fulda1989.T64Assets";
  const string KontaktTypeName="Fulda1989.Kontakt1";
  const string TilePath="HULL K1/Cube.119";
  static readonly Vector3 CanonicalSize=new Vector3(0.314f,0.157f,0.055f);
  readonly MethodInfo setup;
  readonly Type eraVisualsType;
  readonly FieldInfo visualField,hideField;
  readonly ArmorCodexScriptable armorCodex;
  readonly Mesh mesh;
  readonly Material[] materials;
  readonly Vector3 meshScale,meshOffset;
  readonly Quaternion meshRotation;
  readonly string sourcePath;

  public Vector3 TileSize {get{return CanonicalSize;}}
  public Mesh TileMesh {get{return mesh;}}
  public Material TileMaterial {get{return materials[0];}}
  public string SourcePath {get{return sourcePath;}}

  FuldaEraAdapter(MethodInfo setup,Type visualsType,FieldInfo visualField,FieldInfo hideField,ArmorCodexScriptable codex,Mesh sourceMesh,Material[] sourceMaterials) {
   this.setup=setup;eraVisualsType=visualsType;this.visualField=visualField;this.hideField=hideField;armorCodex=codex;mesh=sourceMesh;
   sourcePath=AssetTypeName+".t64bv_full/"+TilePath+" [mesh="+mesh.name+"]";
   Bounds bounds=mesh.bounds;
   int width=LargestAxis(bounds.size),normal=SmallestAxis(bounds.size),height=3-width-normal;
   Vector3 right=Axis(width),up=Axis(height);
   // A proper rotation preserves triangle winding. On the installed Cube.121
   // these are already X width, Y height and +Z outward; the back is negative Z.
   meshRotation=Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(right,up),up));
   Vector3 scale=Vector3.zero;
   scale[width]=CanonicalSize.x/bounds.size[width];
   scale[height]=CanonicalSize.y/bounds.size[height];
   scale[normal]=CanonicalSize.z/bounds.size[normal];
   meshScale=scale;
   meshOffset=-(meshRotation*Vector3.Scale(bounds.center,meshScale));
   materials=new Material[sourceMaterials.Length];
   for(int i=0;i<materials.Length;i++) {
    for(int j=0;j<i;j++)if(System.Object.ReferenceEquals(sourceMaterials[i],sourceMaterials[j])){materials[i]=materials[j];break;}
    if(materials[i]!=null)continue;
    materials[i]=new Material(sourceMaterials[i]);
    materials[i].name="T3485Q30MOD "+sourceMaterials[i].name;
    materials[i].hideFlags=HideFlags.HideAndDontSave;
    materials[i].enableInstancing=true;
   }
  }

  public static bool TryCreate(out FuldaEraAdapter adapter) {
   adapter=null;
   Type kontakt=AccessTools.TypeByName(KontaktTypeName),assets=AccessTools.TypeByName(AssetTypeName);
   if(kontakt==null&&assets==null)throw Incompatible("Fulda Expansion is required: load FuldaExpansion1989.dll together with its Fulda1989 asset folder.");
   if(kontakt==null||assets==null||kontakt.Assembly!=assets.Assembly)
    throw Incompatible("Kontakt1 and T64Assets must exist in the same loaded Fulda assembly.");
   Type[] parameters={typeof(Transform),typeof(Transform),typeof(bool),typeof(Material),typeof(string)};
   MethodInfo method=AccessTools.Method(kontakt,"Setup",parameters);
   if(method==null||!method.IsStatic||method.ReturnType!=typeof(void))
    throw Incompatible("Expected static void Kontakt1.Setup(Transform, Transform, bool, Material, string).");
   Type visuals=kontakt.Assembly.GetType("Fulda1989.EraVisuals",false);
   if(visuals==null||!typeof(MonoBehaviour).IsAssignableFrom(visuals))
    throw Incompatible("Fulda1989.EraVisuals must be a MonoBehaviour.");
   FieldInfo visual=RequireField(visuals,"visual",typeof(MeshRenderer),false);
   FieldInfo hide=RequireField(visuals,"hide_on_detonate",typeof(bool),false);
   FieldInfo prefabField=RequireField(assets,"t64bv_full",typeof(GameObject),true);
   GameObject prefab=(GameObject)prefabField.GetValue(null);
   if(prefab==null)return false; // Fulda's static-asset loading has not finished.
   Transform tile=prefab.transform.Find(TilePath);
   if(tile==null)throw Incompatible("Loaded t64bv_full has no single-tile visual at "+TilePath+". The bare bvturret/avturret meshes are not ERA tiles.");
   MeshFilter filter=tile.GetComponent<MeshFilter>();
   MeshRenderer renderer=tile.GetComponent<MeshRenderer>();
   if(filter==null||filter.sharedMesh==null||renderer==null)
    throw Incompatible("Expected a MeshFilter and MeshRenderer on t64bv_full/"+TilePath+".");
   Mesh source=filter.sharedMesh;
   ValidateSingleTile(source);
   Material[] sourceMaterials=renderer.sharedMaterials;
   if(sourceMaterials==null||sourceMaterials.Length!=source.subMeshCount)
    throw Incompatible("The tile's Kontakt-1 and bolt material slots do not match its submeshes.");
   for(int i=0;i<sourceMaterials.Length;i++)if(sourceMaterials[i]==null)
    throw Incompatible("The single Kontakt-1 tile has a missing material at slot "+i+".");
   if(!sourceMaterials[0].name.StartsWith("Kontakt-1",StringComparison.OrdinalIgnoreCase)||
      !sourceMaterials[1].name.StartsWith("K1 BOLTS",StringComparison.OrdinalIgnoreCase))
    throw Incompatible("Expected the original Kontakt-1 T-64BV and K1 BOLTS materials.");
   FieldInfo schemaField=AccessTools.Field(kontakt,"schema");
   if(schemaField==null||!schemaField.IsStatic||schemaField.FieldType.FullName!="Fulda1989.EraSetup")
    throw Incompatible("Kontakt1.schema must be a static Fulda1989.EraSetup.");
   object schema=schemaField.GetValue(null);
   if(schema==null)throw Incompatible("Kontakt1.schema is null.");
   Type schemaType=schemaField.FieldType;
   string schemaName=(string)RequireField(schemaType,"name",typeof(string),false).GetValue(schema);
   float heat=(float)RequireField(schemaType,"heat_rha",typeof(float),false).GetValue(schema);
   float ke=(float)RequireField(schemaType,"ke_rha",typeof(float),false).GetValue(schema);
   ArmorCodexScriptable codex=(ArmorCodexScriptable)RequireField(schemaType,"era_so",typeof(ArmorCodexScriptable),false).GetValue(schema);
   object armor=RequireField(schemaType,"era_armour",typeof(GHPC.Equipment.ArmorType),false).GetValue(schema);
   if(schemaName!="Kontakt-1"||heat!=181f||ke!=7f||codex==null||armor==null)
    throw Incompatible("Expected Kontakt-1 schema with HEAT RHA 181, KE RHA 7 and non-null native armor assets.");
   // Do not assign era_so.ArmorType. FuldaEra.Setup initializes it only when
   // null; an already initialized shared native ArmorType must stay intact.
   adapter=new FuldaEraAdapter(method,visuals,visual,hide,codex,source,sourceMaterials);
   return true;
  }

  public void AddTile(Transform collisionParent,Transform visualParent,string name,Vector3 localPosition,Quaternion localRotation) {
   ValidateGroups(collisionParent,visualParent);
   if(String.IsNullOrEmpty(name))throw new ArgumentException("An ERA tile needs a name.","name");
   float length=Quaternion.Dot(localRotation,localRotation);
   if(!Finite(localPosition)||!Finite(length)||length<0.000001f)
    throw new ArgumentException("An ERA tile needs a finite position and a nonzero finite rotation.");
   float inverseLength=1f/(float)Math.Sqrt(length);
   localRotation=new Quaternion(localRotation.x*inverseLength,localRotation.y*inverseLength,localRotation.z*inverseLength,localRotation.w*inverseLength);
   int index=collisionParent.childCount;
   if(index>0&&collisionParent.GetChild(0).GetComponent<GHPC.UniformArmor>()!=null)
    throw Incompatible("Add every tile before calling SetupGroup; this collision group is already configured.");

   var collision=new GameObject(name);
   collision.transform.SetParent(collisionParent,false);
   collision.transform.localPosition=localPosition;
   collision.transform.localRotation=localRotation;
   collision.transform.localScale=Vector3.one;
   collision.transform.SetSiblingIndex(index);
   var box=collision.AddComponent<BoxCollider>();
   box.center=Vector3.zero;box.size=CanonicalSize;box.isTrigger=false;

   var visual=new GameObject(name);
   visual.transform.SetParent(visualParent,false);
   visual.transform.localPosition=localPosition+localRotation*meshOffset;
   visual.transform.localRotation=localRotation*meshRotation;
   visual.transform.localScale=meshScale;
   visual.transform.SetSiblingIndex(index);
   visual.AddComponent<MeshFilter>().sharedMesh=mesh;
   // Keep both the original casing and bolts, sharing private instanced
   // materials across our tiles without changing Fulda's donor materials.
   visual.AddComponent<MeshRenderer>().sharedMaterials=materials;
  }

  public void SetupGroup(Transform collisionParent,Transform visualParent) {
   ValidateGroups(collisionParent,visualParent);
   if(collisionParent.childCount==0)throw Incompatible("SetupGroup needs at least one matched tile.");
   for(int i=0;i<collisionParent.childCount;i++) {
    Transform collision=collisionParent.GetChild(i),visual=visualParent.GetChild(i);
    BoxCollider box=collision.GetComponent<BoxCollider>();
    MeshFilter filter=visual.GetComponent<MeshFilter>();
    if(box==null||collision.GetComponents<Collider>().Length!=1||box.isTrigger||!Near(box.center,Vector3.zero)||!Near(box.size,CanonicalSize)||!Near(collision.localScale,Vector3.one))
     throw Incompatible("Collision child "+i+" must contain exactly one canonical, centered BoxCollider.");
    if(collision.GetComponent<GHPC.UniformArmor>()!=null||collision.GetComponent(eraVisualsType)!=null)
     throw Incompatible("Collision child "+i+" already has ERA scripts; SetupGroup must run once.");
    if(filter==null||filter.sharedMesh!=mesh||visual.GetComponent<MeshRenderer>()==null||collision.name!=visual.name||
       !Near(visual.localPosition,collision.localPosition+collision.localRotation*meshOffset)||
       !SameRotation(visual.localRotation,collision.localRotation*meshRotation)||!Near(visual.localScale,meshScale))
     throw Incompatible("Visual child "+i+" no longer matches its canonical collision tile and sibling index.");
   }
   var initializedArmor=armorCodex.ArmorType;
   try {
    setup.Invoke(null,new object[]{collisionParent,visualParent,true,null,""});
   }catch(TargetInvocationException ex) {
    Exception cause=ex.InnerException??ex;
    throw new InvalidOperationException("T3485Q30MOD: original Fulda Kontakt1.Setup failed for '"+collisionParent.name+"': "+cause.Message,cause);
   }
   if(initializedArmor!=null&&!System.Object.ReferenceEquals(initializedArmor,armorCodex.ArmorType))
    throw Incompatible("Original Setup replaced an already initialized shared ArmorType.");
   for(int i=0;i<collisionParent.childCount;i++) {
    Transform collision=collisionParent.GetChild(i);
    GHPC.UniformArmor armor=collision.GetComponent<GHPC.UniformArmor>();
    Component component=collision.GetComponent(eraVisualsType);
    var behaviour=component as Behaviour;
    if(armor==null||!armor.IsEra||armor.Name!="Kontakt-1"||armor.PrimaryHeatRha!=181f||armor.PrimarySabotRha!=7f||
       armor.SecondaryHeatRha!=0f||armor.SecondarySabotRha!=0f||armor.ArmorType!=armorCodex||armorCodex.ArmorType==null||
       behaviour==null||behaviour.enabled||!(bool)hideField.GetValue(component)||
       (MeshRenderer)visualField.GetValue(component)!=visualParent.GetChild(i).GetComponent<MeshRenderer>()||
       collision.gameObject.layer!=8||collision.gameObject.tag!="Penetrable")
     throw Incompatible("Original Setup did not install the expected native Kontakt-1 armor and EraVisuals on child "+i+".");
   }
  }

  static void ValidateSingleTile(Mesh source) {
   Bounds bounds=source.bounds;
   if(source.name!="Cube.121"||source.vertexCount!=1000||source.subMeshCount!=2||!Finite(bounds.center)||!Finite(bounds.size)||
      bounds.size.x<=0f||bounds.size.y<=0f||bounds.size.z<=0f)
    throw Incompatible("The source must be the original single Cube.121 Kontakt-1 tile (1000 vertices, 2 submeshes), not hull geometry or a combined array.");
   int width=LargestAxis(bounds.size),normal=SmallestAxis(bounds.size),height=3-width-normal;
   // Metadata from the installed t64bv_full bundle, before its 100x FBX
   // transform. Reject a multi-tile extent instead of shrinking an array.
   if(!ImportedExtent(bounds.size[width],0.0288679004f)||!ImportedExtent(bounds.size[height],0.0128343701f)||
      !ImportedExtent(bounds.size[normal],0.0084934030f))
    throw Incompatible("Cube.121 bounds span more than one Kontakt-1 tile or use an incompatible import scale; expected imported dimensions 0.028868 x 0.0128344 x 0.0084934.");
   // Mesh.bounds/vertexCount/subMeshCount are available on GPU-only meshes.
   // Never request vertices, triangles, GetVertices or RecalculateBounds.
  }

  static void ValidateGroups(Transform collisionParent,Transform visualParent) {
   if(collisionParent==null)throw new ArgumentNullException("collisionParent");
   if(visualParent==null)throw new ArgumentNullException("visualParent");
   if(collisionParent==visualParent||collisionParent.parent==null||collisionParent.parent!=visualParent.parent)
    throw Incompatible("Collision and visual groups must be separate siblings under the same vehicle part.");
   if(collisionParent.gameObject.activeInHierarchy||visualParent.gameObject.activeInHierarchy||collisionParent.parent.gameObject.activeInHierarchy)
    throw Incompatible("Build and configure ERA only while the vehicle part is inactive in the hierarchy, before the whole vehicle is spawned.");
   if(!Identity(collisionParent)||!Identity(visualParent))
    throw Incompatible("Both ERA group transforms must have zero position, identity rotation and unit scale under the same part.");
   if(collisionParent.childCount!=visualParent.childCount)
    throw Incompatible("ERA collision and visual groups must have matching child counts and sibling indices.");
  }
  static bool Identity(Transform t){return Near(t.localPosition,Vector3.zero)&&SameRotation(t.localRotation,Quaternion.identity)&&Near(t.localScale,Vector3.one);}
  static bool Near(Vector3 a,Vector3 b){return (a-b).sqrMagnitude<0.0000000001f;}
  static bool SameRotation(Quaternion a,Quaternion b){return Math.Abs(Math.Abs(Quaternion.Dot(a,b))-1f)<0.00001f;}
  static bool Finite(float n){return !Single.IsNaN(n)&&!Single.IsInfinity(n);}
  static bool Finite(Vector3 v){return Finite(v.x)&&Finite(v.y)&&Finite(v.z);}
  static bool ImportedExtent(float actual,float expected){return Math.Abs(actual-expected)<=expected*0.02f;}
  static int LargestAxis(Vector3 v){return v.x>v.y?(v.x>v.z?0:2):(v.y>v.z?1:2);}
  static int SmallestAxis(Vector3 v){return v.x<v.y?(v.x<v.z?0:2):(v.y<v.z?1:2);}
  static Vector3 Axis(int index){Vector3 v=Vector3.zero;v[index]=1f;return v;}
  static FieldInfo RequireField(Type type,string name,Type fieldType,bool isStatic) {
   FieldInfo field=AccessTools.Field(type,name);
   if(field==null||field.FieldType!=fieldType||field.IsStatic!=isStatic)
    throw Incompatible("Expected "+(isStatic?"static ":"")+fieldType.FullName+" "+type.FullName+"."+name+".");
   return field;
  }
  static InvalidOperationException Incompatible(string message){return new InvalidOperationException("T3485Q30MOD: incompatible Fulda Kontakt-1 integration. "+message);}
 }
}
