using System.Collections.Generic;
using Moodium.Characters;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Hands;
namespace Moodium.NatureWorld
{
    public struct BubblePlayHand { public bool Valid,Pinching,Started,Open;public Vector3 Tip,Palm,Up,Index; }
    public sealed class BubblePlayHands
    {
        readonly List<XRHandSubsystem> systems=new();XRHandSubsystem system;XROrigin origin;
        readonly bool[] pinched=new bool[2],initialized=new bool[2];
        public readonly BubblePlayHand[] Hands=new BubblePlayHand[2];
        public static bool PinchState(bool before,float distance)=>distance<(before?.025f:.015f);
        public static bool CanCatch(Vector3 palm,Vector3 up,Vector3 point,int held)=>held<5&&Vector3.Dot(up,Vector3.up)>.6f&&Vector3.Distance(point,palm)<.07f&&Vector3.Dot(point-palm,Vector3.up)>=0;
        public void Sample()
        {
            if(system==null||!system.running){systems.Clear();SubsystemManager.GetSubsystems(systems);system=systems.Find(s=>s.running);}
            if(origin==null)origin=Object.FindFirstObjectByType<XROrigin>();
            for(int i=0;i<2;i++)
            {
                Hands[i]=default;if(system==null)continue;var hand=i==0?system.leftHand:system.rightHand;
                if(!hand.isTracked||!Joint(hand,XRHandJointID.ThumbTip,out var thumb)||!Joint(hand,XRHandJointID.IndexTip,out var index)||!Joint(hand,XRHandJointID.Wrist,out var wrist)||!Joint(hand,XRHandJointID.IndexMetacarpal,out var im)||!Joint(hand,XRHandJointID.MiddleMetacarpal,out var mm)||!Joint(hand,XRHandJointID.LittleMetacarpal,out var lm)){initialized[i]=false;pinched[i]=false;continue;}
                bool now=PinchState(pinched[i],Vector3.Distance(thumb,index));
                Joint(hand,XRHandJointID.MiddleProximal,out var knuckle);
                Hands[i]=new BubblePlayHand{Valid=true,Pinching=now,Started=initialized[i]&&!pinched[i]&&now,Open=Vector3.Distance(thumb,index)>.05f,Tip=(thumb+index)*.5f,Index=index,Palm=Vector3.Lerp(wrist,knuckle,.7f),Up=MoodiHandCompanion.PalmFacing(wrist,im,mm,lm,i==1)};
                initialized[i]=true;pinched[i]=now;
            }
        }
        bool Joint(XRHand h,XRHandJointID id,out Vector3 p){p=default;if(!h.GetJoint(id).TryGetPose(out var pose))return false;p=origin!=null?origin.Origin.transform.TransformPoint(pose.position):pose.position;return true;}
    }
    public static class BubblePlayVisuals
    {
        public static Material Material(string html,float alpha=1){ColorUtility.TryParseHtmlString(html,out var c);c.a=alpha;return BubbleWorldData.CreateUnlit("Bubble Play",null,alpha<1,UnityEngine.Rendering.CullMode.Off,c);}
        public static AudioClip Clip(string id)=>Resources.Load<AudioClip>("MoodiumBubblePlay/Audio/"+id);
        public static void Release(Object o){if(o==null)return;if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
    }
}
