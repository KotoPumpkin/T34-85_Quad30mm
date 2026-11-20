using System;
using System.IO;
namespace T3485Quad30 {
 public sealed class MeshPayload {
  public float[] Vertices;
  public int[][] Submeshes;
  public static MeshPayload Read(Stream stream){
   if(stream==null)throw new InvalidDataException("Native 2A42 model payload missing");
   using(var r=new BinaryReader(stream)){
    if(r.ReadInt32()!=0x43343354)throw new InvalidDataException("Wrong 2A42 mesh payload header");
    int vertices=r.ReadInt32(),submeshes=r.ReadInt32();if(vertices<50||vertices>50000||submeshes<1||submeshes>32)throw new InvalidDataException("Invalid 2A42 mesh size");
    var result=new MeshPayload {Vertices=new float[vertices*8],Submeshes=new int[submeshes][]};
    for(int i=0;i<result.Vertices.Length;i++){float v=r.ReadSingle();if(Single.IsNaN(v)||Single.IsInfinity(v))throw new InvalidDataException("Non-finite model coordinate");result.Vertices[i]=v;}
    for(int s=0;s<submeshes;s++){
     int count=r.ReadInt32();if(count<0||count>300000||count%3!=0)throw new InvalidDataException("Invalid 2A42 triangle list");result.Submeshes[s]=new int[count];
     for(int i=0;i<count;i++){int index=r.ReadInt32();if(index<0||index>=vertices)throw new InvalidDataException("2A42 triangle index out of bounds");result.Submeshes[s][i]=index;}
    }
    if(stream.Position!=stream.Length)throw new InvalidDataException("Extra model payload data");return result;
   }
  }
 }
}
