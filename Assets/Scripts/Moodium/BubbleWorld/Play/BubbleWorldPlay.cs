using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Moodium.NatureWorld
{
 public sealed class BubbleWorldPlay : MonoBehaviour
 {
  public readonly BubblePlayHands Hands=new();
  BubbleLightRipple ripple;BubbleCatchFall catchFall;BubbleWorldNarrator narrator;
  Func<Vector3,bool> blocked;string folder;bool active;
  public void Initialize(string world,Transform scan,Camera viewer,IList<GameObject> layers,float[] radii,Material material,Action<float> sparkle,Func<Vector3,bool> exclusion)
  {
   folder=world;blocked=exclusion;narrator=FindFirstObjectByType<BubbleWorldNarrator>();
   ripple=gameObject.AddComponent<BubbleLightRipple>();ripple.Initialize(layers,radii,material,scan,BubbleWorldData.IsCandy(folder),sparkle);
   if(folder==BubbleWorldData.NestedNatureFolder||folder==BubbleWorldData.NestedCandyFolder){catchFall=gameObject.AddComponent<BubbleCatchFall>();catchFall.Initialize(Hands,viewer,BubbleWorldData.IsCandy(folder));}
   gameObject.SetActive(false);
  }
  public void SetInside(bool value)
  {
   if(active==value)return;active=value;gameObject.SetActive(value);if(value)StartCoroutine(Hints());
  }
  IEnumerator Hints()
  {
   yield return null;while(narrator!=null&&narrator.IsSpeaking)yield return null;
   yield return new WaitForSeconds(2);
   narrator?.SayLine(folder==BubbleWorldData.NestedCandyFolder?"B2c":folder==BubbleWorldData.NestedNatureFolder?"B2n":"B1");
  }
  void Update(){Hands.Sample();foreach(var h in Hands.Hands)if(h.Valid&&h.Started)ripple.Trigger(blocked?.Invoke(h.Tip)==true);}
  void OnDisable(){StopAllCoroutines();}
 }
}
