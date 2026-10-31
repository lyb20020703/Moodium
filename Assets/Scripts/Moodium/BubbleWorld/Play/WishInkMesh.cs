using System.Collections.Generic;
using UnityEngine;
namespace Moodium.NatureWorld
{
 public sealed class WishInkMesh : MonoBehaviour
 {
  readonly List<Material> owned=new();readonly List<Mesh> meshes=new();Transform paper;WishPaperFrame frame;Mesh current,done;MeshRenderer currentRenderer,doneRenderer;
  public void Initialize(Transform strip,WishPaperFrame mapping,bool candy)
  {
   paper=strip;frame=mapping;transform.SetParent(strip,false);
   var ink=BubblePlayVisuals.Material(candy?"#C93D78":"#3E2A16");var glow=BubblePlayVisuals.Material(candy?"#FF78B4":"#FFE98C",.35f);owned.Add(ink);owned.Add(glow);
   current=Create("Current Ink",ink,glow,out currentRenderer);done=Create("Completed Ink",ink,glow,out doneRenderer);
  }
  Mesh Create(string name,Material ink,Material glow,out MeshRenderer renderer)
  {var go=new GameObject(name);go.transform.SetParent(transform,false);var m=new Mesh{name=name};m.MarkDynamic();meshes.Add(m);go.AddComponent<MeshFilter>().sharedMesh=m;renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{glow,ink};renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return m;}
  public void Active(List<float[]> stroke){if(stroke!=null&&stroke.Count>75)stroke=stroke.GetRange(stroke.Count-75,75);Build(current,stroke==null?new():new List<List<float[]>>{stroke});}
  public void Completed(List<List<float[]>> strokes)=>Build(done,strokes);
  public void Alpha(float value){foreach(var m in owned){var c=m.GetColor("_BaseColor");c.a=(m==owned[1]?.35f:1f)*value;m.SetColor("_BaseColor",c);}}
  public void Show(bool value){currentRenderer.enabled=doneRenderer.enabled=value;}
  void Build(Mesh mesh,List<List<float[]>> strokes)
  {
   var verts=new List<Vector3>();var triangles=new[]{new List<int>(),new List<int>()};
   for(int pass=0;pass<2;pass++)foreach(var stroke in strokes)
   {
    if(stroke.Count<2)continue;int first=verts.Count;
    for(int i=0;i<stroke.Count;i++)
    {
     var uv=new Vector2(stroke[i][0],stroke[i][1]);var p=frame.Surface(uv);var prev=frame.Surface(new Vector2(stroke[Mathf.Max(0,i-1)][0],stroke[Mathf.Max(0,i-1)][1]));var next=frame.Surface(new Vector2(stroke[Mathf.Min(stroke.Count-1,i+1)][0],stroke[Mathf.Min(stroke.Count-1,i+1)][1]));
     var side=Vector3.Cross(frame.Normal,next-prev).normalized*(pass==0?.0024f:.0008f);p+=frame.Normal*(pass==0?.0003f:.0004f);verts.Add(frame.Surface(frame.Project(p-side))+frame.Normal*(pass==0?.0003f:.0004f));verts.Add(frame.Surface(frame.Project(p+side))+frame.Normal*(pass==0?.0003f:.0004f));
     if(i==0)continue;int a=first+(i-1)*2;triangles[pass].AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});
    }
   }
   mesh.Clear();mesh.SetVertices(verts);mesh.subMeshCount=2;mesh.SetTriangles(triangles[0],0);mesh.SetTriangles(triangles[1],1);mesh.RecalculateBounds();
  }
  void OnDestroy(){foreach(var m in owned)BubblePlayVisuals.Release(m);foreach(var m in meshes)BubblePlayVisuals.Release(m);}
 }
}
