using Unity.Profiling;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using TMPro;
using Unity.PolySpatial;
using UnityEngine;
namespace Moodium.NatureWorld
{
 public sealed class WishChimeGarden : MonoBehaviour
 {
  static readonly ProfilerMarker s_Profile=new("BubblePlay.WishChimeGarden");

  public BubblePlayHands Hands{get;private set;}public Camera Viewer{get;private set;}public bool Candy=>BubbleWorldData.IsCandy(Folder);public string Folder{get;private set;}public int Layer=>Folder==BubbleWorldData.CandyFolder||Folder==BubbleWorldData.NatureFolder?0:1;
  public TMP_FontAsset ChineseFont{get;private set;}public WishChime Writing{get;set;}
  public bool IsWriting=>Writing!=null;
  Transform scan;BubbleWorldNarrator narrator;WishChimeDocument document;readonly List<WishChime> chimes=new();WishChime blank;
  float introEnded=-1,nextBlank,nextLod;Vector3 previousHead;bool running,taught,b7,restoring,hadSaved;
  public void Initialize(string folder,Transform world,Camera camera,BubblePlayHands hands,BubbleWorldNarrator voice)
  {
   Folder=folder;scan=world;Viewer=camera;Hands=hands;narrator=voice;
   // Existing world UI font reference, without making an unreadable TMP material copy.
   var fonts=FindFirstObjectByType<Moodium.MoodCheck.MoodCheckFontReferences>(FindObjectsInactive.Include);var manager=FindFirstObjectByType<Moodium.CandyWorld.CandyWorldManager>();ChineseFont=fonts!=null?fonts.Chinese:manager!=null&&manager.BubblePlayChineseFont!=null?manager.BubblePlayChineseFont:TMP_Settings.defaultFontAsset;
  }
  public void Enter()
  {
   running=true;hadSaved=false;introEnded=-1;nextBlank=float.PositiveInfinity;previousHead=Viewer.transform.position;StartCoroutine(LoadSaved());
  }
  public void Leave(){StopAllCoroutines();running=false;Writing=null;blank=null;foreach(var c in chimes)if(c!=null)BubblePlayVisuals.Release(c.gameObject);chimes.Clear();}
  IEnumerator LoadSaved()
  {
   restoring=true;var path=WishChimeStore.DefaultPath;var task=Task.Run(()=>WishChimeStore.Load(path));
   while(!task.IsCompleted)yield return null;
   document=task.Status==TaskStatus.RanToCompletion?task.Result:new WishChimeDocument();
   var records=document.chimes.Where(r=>r.folder==Folder).OrderBy(r=>Vector3.Distance(scan.TransformPoint(new Vector3(r.position[0],r.position[1],r.position[2])),Viewer.transform.position)).ToArray();
   hadSaved=records.Length>0;
   for(int i=0;i<records.Length;i++){Create(records[i],i>=40);yield return null;}
   restoring=false;
  }
  WishChime Create(WishChimeRecord record,bool lite=false)
  {
   var go=new GameObject(record==null?"Blank Wish Chime":"Saved Wish Chime");go.transform.SetParent(scan,false);
   if(record!=null){go.transform.localPosition=new Vector3(record.position[0],record.position[1],record.position[2]);go.transform.localRotation=Quaternion.Euler(0,record.yaw,0);}
   else go.transform.position=Viewer.transform.position+Vector3.ProjectOnPlane(Viewer.transform.forward,Vector3.up).normalized*.55f-Vector3.up*.25f;
   var sorting=scan.GetComponent<VisionOSSortingGroup>();
   if(sorting!=null){for(int j=sorting.Renderers.Count-1;j>=0;j--)if(sorting.Renderers[j].Renderer==null)sorting.Renderers.RemoveAt(j);sorting.Renderers.Add(new VisionOSSortingGroup.RendererSorting{Renderer=go,Order=4,ApplyToDescendants=true});}
   var c=go.AddComponent<WishChime>();c.Initialize(this,record,lite);chimes.Add(c);return c;
  }
  void Update()
  {
   using var timing=s_Profile.Auto();
   if(!running||Viewer==null)return;
   float speed=(Viewer.transform.position-previousHead).magnitude/Mathf.Max(.001f,Time.deltaTime);previousHead=Viewer.transform.position;
   if(introEnded<0&&(narrator==null||!narrator.IsSpeaking)){introEnded=Time.time;nextBlank=Time.time+12;}
   if(introEnded>=0&&!restoring&&hadSaved&&!b7){b7=true;Say("B7");}
   if(!restoring&&blank==null&&!IsWriting&&introEnded>=0&&Time.time>=nextBlank&&speed<.15f){blank=Create(null);Say("B3");}
   if(Time.time>=nextLod){nextLod=Time.time+2;UpdateNearest();}
  }
  void UpdateNearest()
  {
   var sorted=chimes.Where(c=>c!=null&&c.Record!=null&&c.State==WishChime.Stage.Hanging).OrderBy(c=>Vector3.Distance(c.transform.position,Viewer.transform.position)).ToArray();
   for(int i=0;i<sorted.Length;i++)sorted[i].SetFull(i<40);
  }
  public bool Near(Vector3 position){foreach(var c in chimes)if(c!=null&&c.Near(position))return true;return false;}
  public bool CanWrite(WishChime chime)=>Writing==null||Writing==chime;
  public void TeachWriting(){if(!taught&&PlayerPrefs.GetInt("Moodium.WishWritingTaught",0)==0){taught=true;PlayerPrefs.SetInt("Moodium.WishWritingTaught",1);Say("B4");}}
  public void Say(string id)=>narrator?.SayLine(id);
  public Task<bool> PersistAsync(WishChimeRecord record)
  {
   var path=WishChimeStore.DefaultPath;
   return Task.Run(()=>WishChimeStore.Append(path,record));
  }
  public void Released(WishChime chime){blank=null;nextBlank=Time.time+20;}
  public bool TryDestination(out Vector3 local)
  {
   var occupied=new List<Vector3>();foreach(var c in chimes)if(c!=null&&c.Record!=null)occupied.Add(c.transform.position);
   var forward=Vector3.ProjectOnPlane(Viewer.transform.forward,Vector3.up).normalized;
   for(int attempt=0;attempt<128;attempt++)
   {
    float angle=UnityEngine.Random.Range(0,360)*Mathf.Deg2Rad;var direction=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));if(Vector3.Dot(direction,forward)>Mathf.Cos(30*Mathf.Deg2Rad))continue;
    var p=scan.position+direction*UnityEngine.Random.Range(.8f,1.6f);p.y=Viewer.transform.position.y+UnityEngine.Random.Range(.3f,.8f);
    if(WishChimeStore.FarEnough(p,occupied)){local=scan.InverseTransformPoint(p);return true;}
   }
   local=default;return false;
  }
  void OnDisable()=>Leave();
 }
}
