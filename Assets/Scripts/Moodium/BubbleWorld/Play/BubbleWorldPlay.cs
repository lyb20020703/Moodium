using Unity.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Moodium.NatureWorld
{
 [DefaultExecutionOrder(-20)]
 public sealed class BubbleWorldPlay : MonoBehaviour
 {
  static readonly ProfilerMarker s_Profile=new("BubblePlay.BubbleWorldPlay");

  public readonly BubblePlayHands Hands=new();
  WishChimeGarden garden;
  BubbleLightRipple ripple;BubbleCatchFall catchFall;BubbleWorldNarrator narrator;
  Func<Vector3,bool> blocked;string folder;bool active;float metricAt,worst,sum;int frames;
  public void Initialize(string world,Transform scan,Camera viewer,IList<GameObject> layers,float[] radii,Material material,Action<float> sparkle,Func<Vector3,bool> exclusion)
  {
   folder=world;blocked=exclusion;narrator=FindFirstObjectByType<BubbleWorldNarrator>();
   ripple=gameObject.AddComponent<BubbleLightRipple>();ripple.Initialize(layers,radii,material,scan,BubbleWorldData.IsCandy(folder),sparkle);
   if(folder==BubbleWorldData.NestedNatureFolder||folder==BubbleWorldData.NestedCandyFolder){catchFall=gameObject.AddComponent<BubbleCatchFall>();catchFall.Initialize(Hands,viewer,BubbleWorldData.IsCandy(folder));}
   garden=gameObject.AddComponent<WishChimeGarden>();garden.Initialize(folder,scan,viewer,Hands,narrator);
   gameObject.SetActive(false);
  }
  public void SetInside(bool value)
  {
   if(active==value)return;Hands.Reset();active=value;gameObject.SetActive(value);if(value){garden.Enter();StartCoroutine(Hints());}
  }
  IEnumerator Hints()
  {
   yield return null;while(narrator!=null&&narrator.IsSpeaking)yield return null;
   yield return new WaitForSeconds(2);
   narrator?.SayLine(folder==BubbleWorldData.NestedCandyFolder?"B2c":folder==BubbleWorldData.NestedNatureFolder?"B2n":"B1");
  }
  void Update(){
   using var timing=s_Profile.Auto();   float ms=Time.unscaledDeltaTime*1000;worst=Mathf.Max(worst,ms);sum+=ms;frames++;
   if(Time.unscaledTime-metricAt>5){Debug.Log($"[Bubble Play] {folder} writing={garden.IsWriting} frames={frames} avg={sum/Mathf.Max(1,frames):0.0}ms worst={worst:0.0}ms");metricAt=Time.unscaledTime;frames=0;worst=sum=0;}
   Hands.Sample();foreach(var h in Hands.Hands)if(h.Valid&&h.Started)ripple.Trigger((blocked?.Invoke(h.Tip)==true)||garden.IsWriting||garden.Near(h.Tip));}
  void OnDisable(){StopAllCoroutines();}
 }
}
