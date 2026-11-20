using System;
using System.Collections.Generic;
using UnityEngine;
namespace T3485Quad30 {
 public static class EraMath {
  public static Vector3[] Axes(Quaternion rotation){return new[]{rotation*Vector3.right,rotation*Vector3.up,rotation*Vector3.forward};}
  public static bool BoxesOverlap(Vector3 a,Quaternion ar,Vector3 ah,Vector3 b,Quaternion br,Vector3 bh,float padding){
   var aa=Axes(ar);var ba=Axes(br);Vector3 d=b-a;
   var axes=new Vector3[15];for(int i=0;i<3;i++){axes[i]=aa[i];axes[i+3]=ba[i];for(int j=0;j<3;j++)axes[6+i*3+j]=Vector3.Cross(aa[i],ba[j]);}
   foreach(var axis in axes){if(axis.sqrMagnitude<0.00000001f)continue;
    float ra=(ah.x+padding)*Math.Abs(Vector3.Dot(aa[0],axis))+(ah.y+padding)*Math.Abs(Vector3.Dot(aa[1],axis))+(ah.z+padding)*Math.Abs(Vector3.Dot(aa[2],axis));
    float rb=(bh.x+padding)*Math.Abs(Vector3.Dot(ba[0],axis))+(bh.y+padding)*Math.Abs(Vector3.Dot(ba[1],axis))+(bh.z+padding)*Math.Abs(Vector3.Dot(ba[2],axis));
    if(Math.Abs(Vector3.Dot(d,axis))>ra+rb)return false;
   }return true;
  }
  // Ray parity defines a solid only for a closed mesh. Weld positional seam
  // duplicates before checking edges; partial hatch/cast-turret masks stay open.
  public static bool IsClosedMesh(Vector3[] vertices,int[] triangles){
   var welded=new int[vertices.Length];var positions=new Dictionary<string,int>();
   for(int i=0;i<vertices.Length;i++){var p=vertices[i];string key=Math.Round(p.x*10000f)+","+Math.Round(p.y*10000f)+","+Math.Round(p.z*10000f);int index;if(!positions.TryGetValue(key,out index)){index=positions.Count;positions.Add(key,index);}welded[i]=index;}
   var edges=new Dictionary<long,int>();
   for(int i=0;i<triangles.Length;i+=3){int a=welded[triangles[i]],b=welded[triangles[i+1]],c=welded[triangles[i+2]];if(a==b||b==c||a==c)continue;
    foreach(var edge in new[]{new[]{a,b},new[]{b,c},new[]{c,a}}){long key=((long)Math.Min(edge[0],edge[1])<<32)|(uint)Math.Max(edge[0],edge[1]);int count;edges.TryGetValue(key,out count);edges[key]=count+1;}
   }
   if(edges.Count==0)return false;foreach(var count in edges.Values)if(count!=2)return false;return true;
  }
  // Three independent double-sided ray parity tests also detect a box wholly inside
  // a nonconvex collider, where face penetration alone can report no contact.
  public static bool PointInsideMesh(Vector3 point,Vector3[] vertices,int[] triangles){
   int votes=0;
   foreach(var direction in new[]{new Vector3(1f,0.371f,0.529f),new Vector3(-0.337f,1f,0.613f),new Vector3(0.417f,0.223f,1f)}){
    var hits=new List<float>();
    for(int i=0;i<triangles.Length;i+=3){
     Vector3 a=vertices[triangles[i]],edge1=vertices[triangles[i+1]]-a,edge2=vertices[triangles[i+2]]-a;
     Vector3 cross=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,cross);if(Math.Abs(determinant)<0.0000001f)continue;
     float inverse=1f/determinant;Vector3 offset=point-a;float u=Vector3.Dot(offset,cross)*inverse;if(u < -0.00001f||u>1.00001f)continue;
     Vector3 q=Vector3.Cross(offset,edge1);float v=Vector3.Dot(direction,q)*inverse;if(v < -0.00001f||u+v>1.00001f)continue;
     float distance=Vector3.Dot(edge2,q)*inverse;if(distance>0.00001f)hits.Add(distance);
    }
    hits.Sort();int intersections=0;float previous=float.NegativeInfinity;
    foreach(float hit in hits)if(hit-previous>0.0001f){intersections++;previous=hit;}
    if((intersections&1)!=0)votes++;
   }
   return votes>=2;
  }
  public static Bounds BoxBounds(Vector3 center,Quaternion rotation,Vector3 half){
   var axes=Axes(rotation);Vector3 ext=new Vector3(Math.Abs(axes[0].x)*half.x+Math.Abs(axes[1].x)*half.y+Math.Abs(axes[2].x)*half.z,Math.Abs(axes[0].y)*half.x+Math.Abs(axes[1].y)*half.y+Math.Abs(axes[2].y)*half.z,Math.Abs(axes[0].z)*half.x+Math.Abs(axes[1].z)*half.y+Math.Abs(axes[2].z)*half.z);
   return new Bounds(center,ext*2f);
  }
 }
}
