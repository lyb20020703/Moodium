using System.Collections.Generic;
using UnityEngine;
namespace Moodium.NatureWorld
{
 public sealed class BubbleCatchFall : MonoBehaviour
 {
  sealed class Bit {public GameObject Object;public Vector3 Base,Offset;public float Speed,Age,Phase;public int Hand=-1;}
  readonly List<Bit> bits=new();readonly List<Material> materials=new();BubblePlayHands hands;Camera viewer;bool candy;float spawnAt;AudioSource sound;
  public void Initialize(BubblePlayHands input,Camera camera,bool isCandy)
  {
   hands=input;viewer=camera;candy=isCandy;var audioObject=new GameObject("Catch Audio");audioObject.transform.SetParent(transform,false);sound=audioObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=1;sound.volume=.5f;
   var prefab=Resources.Load<GameObject>("MoodiumBubblePlay/CatchBits");if(prefab==null)return;
   var snow=BubblePlayVisuals.Material("#F2FBFF");var chocolate=BubblePlayVisuals.Material("#5A3524");var mint=BubblePlayVisuals.Material("#BFF2DD",.85f);materials.AddRange(new[]{snow,chocolate,mint});
   var parts=prefab.GetComponentsInChildren<Transform>(true);
   for(int i=0;i<36;i++)
   {
    var name=candy?(i%2==0?"ChocoChip":"MintShard"):"Snowflake";var part=System.Array.Find(parts,t=>t.name==name);if(part==null)continue;
    var go=Instantiate(part.gameObject,transform);foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterial=!candy?snow:i%2==0?chocolate:mint;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
    go.SetActive(false);bits.Add(new Bit{Object=go,Phase=Random.value*Mathf.PI*2});
   }
  }
  void Update()
  {
   if(viewer==null)return;int[] count=new int[2];foreach(var b in bits)if(b.Object.activeSelf&&b.Hand>=0)count[b.Hand]++;
   if(Time.time>=spawnAt){spawnAt=Time.time+.3f;var b=bits.Find(x=>!x.Object.activeSelf);if(b!=null){var d=Random.insideUnitCircle*1.2f;b.Base=viewer.transform.position+new Vector3(d.x,1,d.y);b.Hand=-1;b.Age=0;b.Speed=Random.Range(.12f,.22f);b.Object.transform.localScale=Vector3.one;b.Object.SetActive(true);}}
   foreach(var b in bits)
   {
    if(!b.Object.activeSelf)continue;float dt=Time.deltaTime;
    if(b.Hand>=0)
    {
     var h=hands.Hands[b.Hand];if(!h.Valid||Vector3.Dot(h.Up,Vector3.up)<=.6f){count[b.Hand]--;b.Hand=-1;b.Base=b.Object.transform.position;continue;}
     b.Age+=dt;b.Object.transform.position=h.Palm+Vector3.up*.015f+b.Offset;
     if(!candy)b.Object.transform.localScale=Vector3.one*Mathf.Max(0,1-b.Age/2.5f);
     if(b.Age>=(candy?3:2.5f)){if(candy)Moodium.CandyWorld.CandyWorldManager.SpawnSoftSqueezeFeedback(b.Object.transform.position);b.Object.SetActive(false);count[b.Hand]--;b.Hand=-1;}
    }
    else
    {
     b.Base+=Vector3.down*b.Speed*dt;b.Object.transform.position=b.Base+new Vector3(Mathf.Sin(Time.time*2.5f+b.Phase)*.03f,0,0);b.Object.transform.Rotate(Vector3.up,dt*18,Space.World);
     if(b.Base.y<viewer.transform.position.y-1.2f){b.Object.SetActive(false);continue;}
     for(int i=0;i<2;i++){var h=hands.Hands[i];if(h.Valid&&BubblePlayHands.CanCatch(h.Palm,h.Up,b.Object.transform.position,count[i])){b.Hand=i;b.Age=0;b.Offset=Random.insideUnitSphere*.015f;count[i]++;sound.transform.position=h.Palm;sound.PlayOneShot(BubblePlayVisuals.Clip((candy?"catch_choco_":"catch_snow_")+Random.Range(0,3)));break;}}
    }
   }
  }
  public void Clear(){foreach(var b in bits){b.Object.SetActive(false);b.Hand=-1;}}
  void OnDisable(){Clear();if(sound!=null)sound.Stop();}
  void OnDestroy(){foreach(var m in materials)BubblePlayVisuals.Release(m);}
 }
}
