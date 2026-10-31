using Unity.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;
using Moodium.Flow;
using TMPro;
using UnityEngine;
namespace Moodium.NatureWorld
{
 public sealed class WishChime : MonoBehaviour
 {
  static readonly ProfilerMarker s_Profile=new("BubblePlay.WishChime");

  public enum Stage{Blank,Writing,Flying,Hanging}
  public Stage State{get;private set;}public WishChimeRecord Record{get;private set;}
  public Transform Strip{get;private set;}
  Transform bell;
  WishActionButton rewriteButton,releaseButton;float lastStrokeEnd=-10f;int lastWriter=-1;
  public int LastWriter=>lastWriter;
  public Vector3 StripCentre=>Strip.TransformPoint(frame.Surface(Vector2.one*.5f));
  public static bool ActionBlocked(bool stroke,float now,float ended,bool erase,bool persist)=>stroke||now-ended<.35f||erase||persist;
  bool PressBlocked()=>ActionBlocked(current!=null,Time.time,lastStrokeEnd,clearing,saving);
  public bool HasInk=>WishChimeStore.CanRelease(strokes);
  public bool IsWriting=>State==Stage.Writing;
  WishChimeGarden garden;WishPaperFrame frame;WishInkMesh ink;Camera viewer;BubblePlayHands input;bool candy,lite,fullVisible=true;
  readonly List<Material> materials=new();List<List<float[]>> strokes=new();List<float[]> current;Vector2? currentTip,displayedTip;int total,writer=-1;
  AudioSource sound,writeSound;GameObject buttons,cursor,model;Vector3 stripScale,stripPosition,flyStripScale,flyStripPosition;Quaternion baseRotation,lastRotation;float lastAction,ringAt,patAt,shakeAt;bool b5,b8,clearing,grabbing,saving;
  readonly float[] liftAt={-1,-1},liftY=new float[2];readonly bool[] wasOpen=new bool[2];
  public void Initialize(WishChimeGarden owner,WishChimeRecord record,bool minimal=false)
  {
   garden=owner;Record=record;candy=owner.Candy;viewer=owner.Viewer;input=owner.Hands;lite=minimal;
   var path="MoodiumBubblePlay/";frame=Resources.Load<WishPaperFrame>(path+"PaperFrame_"+(candy?"Candy":"Nature"));
   sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=1;sound.volume=.25f;
   if(minimal){model=GameObject.CreatePrimitive(PrimitiveType.Sphere);BubblePlayVisuals.Release(model.GetComponent<Collider>());model.transform.SetParent(transform,false);model.transform.localScale=Vector3.one*.008f;model.GetComponent<Renderer>().sharedMaterial=Mat(candy?"#FFE7B0":"#D9FF8A");Moodium.UI.UIRefreshVisuals.Register(transform,model,WishInkMesh.PaperOrder);State=Stage.Hanging;return;}
   model=Instantiate(Resources.Load<GameObject>(path+"WishChime_"+(candy?"Candy":"Nature")),transform);
   foreach(var t in model.GetComponentsInChildren<Transform>(true))
   {
    if(t.name=="Bell")bell=t;if(t.name=="Strip")Strip=t;var r=t.GetComponent<Renderer>();if(r==null)continue;
    Moodium.UI.UIRefreshVisuals.Register(transform,r.gameObject,WishInkMesh.PaperOrder);
    r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    r.sharedMaterial=t.name=="Bell"?Mat(candy?"#FFB3D1":"#E8FFF3",candy?.35f:.25f):t.name=="Glow"?Mat(candy?"#FFE7B0":"#D9FF8A"):t.name=="Clapper"?Mat("#FFFFFF",.6f):t.name=="Strip"?Mat(candy?"#FFE6F0":"#C9A574"):t.name.StartsWith("Deco_")||t.name=="Leaf_Deco"?Mat(candy?"#FFB3D1":"#6FCF8E"):Mat(candy?"#E98FB5":"#7A5A3A");
   }
   if(Strip==null||frame==null)throw new InvalidOperationException("Missing wish paper mapping");
   var leaf=System.Array.Find(model.GetComponentsInChildren<Transform>(true),t=>t.name=="Leaf_Deco");if(leaf!=null)leaf.SetParent(Strip,true);
   stripScale=Strip.localScale;stripPosition=Strip.localPosition;
   ink=new GameObject("Wish Ink").AddComponent<WishInkMesh>();ink.Initialize(Strip,frame,candy);
   var collider=Strip.gameObject.AddComponent<BoxCollider>();collider.center=Strip.GetComponent<MeshFilter>().sharedMesh.bounds.center;collider.size=Strip.GetComponent<MeshFilter>().sharedMesh.bounds.size+Vector3.one*.01f;
   Strip.gameObject.AddComponent<MoodiumSpatialButton>().Configure(null,null,BeginWrite);
   var audioObject=new GameObject("Writing Audio");audioObject.transform.SetParent(transform,false);writeSound=audioObject.AddComponent<AudioSource>();writeSound.playOnAwake=false;writeSound.loop=true;writeSound.spatialBlend=1;writeSound.volume=0;writeSound.clip=BubblePlayVisuals.Clip("chime_write_loop");
   if(record!=null){strokes=record.strokes;foreach(var s in strokes)total+=s.Count;ink.Completed(strokes);State=Stage.Hanging;baseRotation=transform.localRotation;ringAt=Time.time+UnityEngine.Random.Range(25f,60f);}
   else{State=Stage.Blank;FaceViewer();baseRotation=transform.localRotation;lastAction=Time.time;StartCoroutine(Grow());}
  }
  GameObject lodGlow;
  public void SetFull(bool full)
  {
   fullVisible=full;if(!full&&sound!=null)sound.Stop();
   if(lite&&full)
   {
    // Reconstruct only on crossing the nearest-40 boundary; all ink remains in the record.
    foreach(var m in materials)BubblePlayVisuals.Release(m);materials.Clear();BubblePlayVisuals.Release(model);model=null;lite=false;
    var record=Record;var owner=garden;Destroy(sound);Initialize(owner,record,false);return;
   }
   if(lite)return;
   if(model!=null)model.SetActive(full);if(ink!=null)ink.Show(full);
   if(lodGlow==null&&!full){lodGlow=GameObject.CreatePrimitive(PrimitiveType.Sphere);BubblePlayVisuals.Release(lodGlow.GetComponent<Collider>());lodGlow.transform.SetParent(transform,false);lodGlow.transform.localScale=Vector3.one*.008f;lodGlow.GetComponent<Renderer>().sharedMaterial=Mat(candy?"#FFE7B0":"#D9FF8A");Moodium.UI.UIRefreshVisuals.Register(transform,lodGlow,WishInkMesh.PaperOrder);}
   if(lodGlow!=null)lodGlow.SetActive(!full);
  }
  Material Mat(string color,float alpha=1){var m=BubblePlayVisuals.Material(color,alpha);materials.Add(m);return m;}
  public bool Near(Vector3 p,float distance=.15f)
  {
   if(Vector3.Distance(p,transform.position)<distance)return true;
   if(bell!=null&&bell.TryGetComponent<MeshFilter>(out var filter)&&Vector3.Distance(p,bell.TransformPoint(filter.sharedMesh.bounds.ClosestPoint(bell.InverseTransformPoint(p))))<distance)return true;
   return Strip!=null&&Vector3.Distance(p,Strip.TransformPoint(frame.Surface(frame.Project(Strip.InverseTransformPoint(p)))))<distance;
  }
  Vector3 BellCenter=>bell!=null&&bell.TryGetComponent<MeshFilter>(out var filter)?bell.TransformPoint(filter.sharedMesh.bounds.center):transform.position;
  void FaceViewer()
  {
   var normal=Vector3.ProjectOnPlane(Strip.TransformDirection(frame.Normal),Vector3.up);var toward=Vector3.ProjectOnPlane(viewer.transform.position-transform.position,Vector3.up);
   if(normal.sqrMagnitude>.00001f&&toward.sqrMagnitude>.00001f)transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(normal,toward,Vector3.up),Vector3.up)*transform.rotation;
  }
  IEnumerator Grow(){sound.PlayOneShot(Resources.Load<AudioClip>(candy?"MoodiumGrowth/grow_candy":"MoodiumGrowth/grow_nature"),.7f/.25f);for(float t=0;t<.9f;t+=Time.deltaTime){transform.localScale=Vector3.one*Mathf.SmoothStep(0,1,t/.9f);yield return null;}transform.localScale=Vector3.one;}
  public void BeginWrite()
  {
   if(State!=Stage.Blank||!garden.CanWrite(this))return;StopAllCoroutines();transform.localScale=Vector3.one;State=Stage.Writing;garden.Writing=this;
   var attachment=Strip.TransformPoint(frame.Surface(new Vector2(.5f,1)));Strip.localScale=stripScale*2.5f;Strip.position+=attachment-Strip.TransformPoint(frame.Surface(new Vector2(.5f,1)));
   AlignWriting();
   lastAction=Time.time;lastRotation=transform.rotation;garden.TeachWriting();
   cursor=GameObject.CreatePrimitive(PrimitiveType.Sphere);BubblePlayVisuals.Release(cursor.GetComponent<Collider>());cursor.name="Ink Cursor";cursor.transform.SetParent(transform,false);cursor.transform.localScale=Vector3.one*.004f;cursor.GetComponent<Renderer>().sharedMaterial=Mat(candy?"#FF78B4":"#FFE98C");Moodium.UI.UIRefreshVisuals.Register(transform,cursor,WishInkMesh.InkOrder+1);cursor.SetActive(false);
   buttons=new GameObject("Wish Actions");buttons.transform.SetParent(transform,false);
   bool en=Moodium.Characters.MoodiVoice.LanguageCode=="en";
   rewriteButton=AddAction("Rewrite",WishActionButton.Prefix(false,candy,en),-WishActionButton.Offset,Rewrite);
   releaseButton=AddAction("Release",WishActionButton.Prefix(true,candy,en),WishActionButton.Offset,ReleaseWish);
   rewriteButton.SetInteractable(HasInk);releaseButton.SetInteractable(HasInk);
  }
  void AlignWriting()
  {
   var target=viewer.transform.position+viewer.transform.forward*.4f-Vector3.up*.08f;
   var basis=Quaternion.LookRotation(Strip.TransformDirection(frame.Normal),Strip.TransformDirection(frame.Up));
   transform.rotation=Quaternion.LookRotation((viewer.transform.position-target).normalized,viewer.transform.up)*Quaternion.Inverse(basis)*transform.rotation;
   transform.position+=target-Strip.TransformPoint(frame.Surface(Vector2.one*.5f));
  }
  WishActionButton AddAction(string name,string prefix,float offset,Action action)
  {
   var holder=new GameObject(name).transform;holder.SetParent(buttons.transform,false);
   holder.position=StripCentre+viewer.transform.right*offset;holder.rotation=Quaternion.LookRotation(viewer.transform.forward,Vector3.up);
   Moodium.UI.UIRefreshVisuals.UnitScale(holder);
   return WishActionButton.Create(holder,name+" Glass",prefix,action,PressBlocked,5);
  }
  void Update()
  {
   using var timing=s_Profile.Auto();
   if(lite||!fullVisible)return;
   if(State==Stage.Blank||State==Stage.Hanging)
   {
    transform.localRotation=baseRotation*Quaternion.Euler(Mathf.Sin(Time.time*2*Mathf.PI/2.4f)*2,0,0);
    if(State==Stage.Hanging&&Time.time>=ringAt){Ring();ringAt=Time.time+UnityEngine.Random.Range(25,60);}
    foreach(var h in input.Hands)if(h.Valid)
    {
     if(State==Stage.Blank&&h.Started&&Vector3.Distance(h.Tip,Strip.TransformPoint(frame.Surface(Vector2.one*.5f)))<.08f){BeginWrite();break;}
     if(State==Stage.Hanging&&Near(h.Index,.04f)&&Time.time-patAt>1.5f){patAt=Time.time;Ring();StartCoroutine(PatSway());}
    }
    return;
   }
   if(State!=Stage.Writing)return;
   releaseButton?.SetInteractable(HasInk);rewriteButton?.SetInteractable(HasInk||current!=null);
   if(saving)return;
   bool drew=false,visible=false,grabbed=false;float speed=0;
   for(int hand=0;hand<2;hand++)
   {
    var h=input.Hands[hand];if(!h.Valid)continue;var local=Strip.InverseTransformPoint(h.Tip);var uv=frame.Project(local);var surface=frame.Surface(uv);var distance=Strip.TransformVector(local-surface).magnitude;bool bounds=uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1;
    if(bounds&&distance<.06f){visible=true;cursor.transform.position=Strip.TransformPoint(surface+frame.Normal*.0006f);}
    if(!clearing&&h.Pinching&&bounds&&distance<.03f&&total<4000&&(writer<0||writer==hand))
    {
     if(current==null){current=new List<float[]>();writer=hand;lastWriter=hand;}
     var previous=current.Count>0?new Vector2(current[^1][0],current[^1][1]):uv;
     bool sampled=WishChimeStore.AddPoint(current,uv,2.5f);
     if(sampled){total++;lastAction=Time.time;speed=Vector2.Scale(uv-previous,new Vector2(.085f,.275f)).magnitude/Mathf.Max(.001f,Time.deltaTime);}
     if(sampled||!displayedTip.HasValue||(uv-displayedTip.Value).sqrMagnitude>.00000025f){ink.Active(current,uv);displayedTip=uv;}
     currentTip=uv;
     drew=true;
     if(current.Count>=74){var last=currentTip.HasValue?new[]{currentTip.Value.x,currentTip.Value.y}:current[^1];FinishStroke();if(total<4000){current=new List<float[]>{last};total++;writer=hand;ink.Active(current);}}
    }
    if(HasInk&&Vector3.Dot(h.Up,Vector3.up)>.6f)
    {
     if(liftAt[hand]<0&&Vector3.Distance(h.Palm,BellCenter)<.12f&&h.Palm.y<=BellCenter.y){liftAt[hand]=Time.time;liftY[hand]=h.Palm.y;wasOpen[hand]=h.Open;}
     if(liftAt[hand]>=0&&Time.time-liftAt[hand]<=1&&(h.Palm.y-liftY[hand]>.1f||h.Open&&!wasOpen[hand])){ReleaseWish();return;}
    }
    if(liftAt[hand]>=0&&Time.time-liftAt[hand]>1)liftAt[hand]=-1;
    if(h.Pinching&&h.Up.sqrMagnitude>.001f&&Vector3.Distance(h.Tip,BellCenter)<.06f&&current==null)
    {
     var q=Quaternion.LookRotation(-h.Up,viewer.transform.up);float angular=grabbing?Quaternion.Angle(lastRotation,q)/Mathf.Max(.001f,Time.deltaTime):0;transform.rotation=q;grabbed=true;
     if(angular>350&&Time.time-shakeAt>.8f){shakeAt=Time.time;Rewrite();}lastRotation=q;
    }
   }
   grabbing=grabbed;if(!drew)FinishStroke();cursor.SetActive(visible);
   if(drew&&speed>.001f){if(!writeSound.isPlaying)writeSound.Play();writeSound.volume=Mathf.Clamp(speed*2,0,.35f);}else writeSound.volume=0;
   if(HasInk&&!b5&&Time.time-lastAction>=3){b5=true;garden.Say("B5");}
  }
  void FinishStroke(){if(current==null)return;if(currentTip.HasValue&&current.Count>0&&current.Count<75&&total<4000){var last=current[^1];var lastUV=new Vector2(last[0],last[1]);if(Vector3.Distance(Strip.TransformPoint(frame.Surface(lastUV)),Strip.TransformPoint(frame.Surface(currentTip.Value)))>.0002f){current.Add(new[]{currentTip.Value.x,currentTip.Value.y});total++;}}currentTip=displayedTip=null;if(current.Count>=2)strokes.Add(current);else total-=current.Count;current=null;writer=-1;ink.Active(null);ink.Completed(strokes);lastStrokeEnd=Time.time;}
  public void Rewrite(){if(State!=Stage.Writing||clearing||saving)return;FinishStroke();if(!b8){b8=true;garden.Say("B8");}StartCoroutine(Erase());}
  IEnumerator Erase(){clearing=true;for(float t=0;t<.5f;t+=Time.deltaTime){ink.Alpha(1-t/.5f);yield return null;}strokes.Clear();total=0;ink.Completed(strokes);ink.Active(null);ink.Alpha(1);clearing=false;lastAction=Time.time;}
  public void ReleaseWish()
  {
   if(State!=Stage.Writing||clearing||saving)return;FinishStroke();if(!HasInk)return;
   if(!garden.TryDestination(out var target))return;
   var record=new WishChimeRecord{id=Guid.NewGuid().ToString("N"),world=candy?"candy":"nature",layer=garden.Layer,folder=garden.Folder,createdAt=DateTimeOffset.Now.ToString("o"),position=new[]{target.x,target.y,target.z},yaw=transform.localEulerAngles.y,strokes=strokes};
   saving=true;writeSound.Stop();StartCoroutine(SaveAndFly(record,target));
  }
  IEnumerator SaveAndFly(WishChimeRecord record,Vector3 target)
  {
   var task=garden.PersistAsync(record);while(!task.IsCompleted)yield return null;saving=false;
   if(task.Status!=System.Threading.Tasks.TaskStatus.RanToCompletion||!task.Result)yield break;
   Record=record;State=Stage.Flying;garden.Writing=null;garden.Released(this);flyStripScale=Strip.localScale;flyStripPosition=Strip.localPosition;buttons.SetActive(false);cursor.SetActive(false);writeSound.Stop();sound.PlayOneShot(BubblePlayVisuals.Clip("chime_release"));StartCoroutine(Fly(target));
  }
  IEnumerator Fly(Vector3 target)
  {
   var start=transform.localPosition;var rotation=transform.localRotation;
   for(float t=0;t<2.6f;t+=Time.deltaTime){Strip.localScale=Vector3.Lerp(flyStripScale,stripScale,Mathf.SmoothStep(0,1,t/.3f));Strip.localPosition=Vector3.Lerp(flyStripPosition,stripPosition,Mathf.SmoothStep(0,1,t/.3f));transform.localPosition=Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,t/2.6f));transform.localRotation=rotation*Quaternion.Euler(0,0,Mathf.Sin(t*5)*4);yield return null;}
   transform.localPosition=target;transform.localRotation=Quaternion.Euler(0,Record.yaw,0);baseRotation=transform.localRotation;State=Stage.Hanging;ringAt=Time.time+UnityEngine.Random.Range(25,60);Ring();garden.Say("B6");
  }
  void Ring(){sound.PlayOneShot(BubblePlayVisuals.Clip("chime_ring_"+(candy?"candy_":"nature_")+UnityEngine.Random.Range(0,3)));}
  IEnumerator PatSway(){var q=baseRotation;for(float t=0;t<1.5f;t+=Time.deltaTime){transform.localRotation=q*Quaternion.Euler(Mathf.Sin(t*12)*8*Mathf.Exp(-t*2),0,0);yield return null;}}
  void OnDisable(){if(writeSound!=null)writeSound.Stop();if(sound!=null)sound.Stop();}
  void OnDestroy(){foreach(var m in materials)BubblePlayVisuals.Release(m);}
 }
}
