using System;
using System.Collections;
using Moodium.Calibration;
using Moodium.Interaction;
using Moodium.Reality;
using UnityEngine;
namespace Moodium.Squeeze
{
    public sealed class SqueezeTableView : MonoBehaviour
    {
        SqueezeModeController mode;
        ChocolateCapsuleInteraction capsule;
        CalibratedSensoryBagPresentation presentation;
        Transform anchor,root,uiRoot;
        Quaternion uiOffset;
        float depth=.05f;
        bool nature,wasHeld;
        TableVisuals visuals;
        ModeButtonBar buttons;
        RhythmGuideView rhythm;
        BreathGuideView breath;
        TableTextSlot text;
        TabletopMotePool motes;
        readonly GuideHintView hints=new();
        string overrideHint;
        float overrideUntil,completeAt=-1,lastPhase,stageAt=-100;
        int cycles;
        AudioSource audioSource,ambient;
        AudioClip note,air,rhythmHit;
        float rhythmHitVolume=.22f;
        bool ownsAir;
        public bool IsBound=>capsule!=null&&capsule.gameObject.activeInHierarchy&&root!=null;
        public string[] HintsShown=>hints.Shown;
        public Transform TableRoot=>root;
        public void Initialize(SqueezeModeController controller,bool isNature)
        {
            mode=controller;nature=isNature;
            mode.ModeChanged+=OnModeChanged;mode.RhythmHit+=OnRhythmHit;mode.ContactReleased+=OnReleased;
        }
        public void Bind(ChocolateCapsuleInteraction target)
        {
            if(target==null||target==capsule&&root!=null)return;
            ClearRoot();capsule=target;presentation=target.GetComponentInParent<CalibratedSensoryBagPresentation>();
            var follower=target.GetComponent<TrackedObjectPoseFollower>();
            anchor=presentation!=null?presentation.transform:follower!=null&&follower.Anchor!=null?follower.Anchor:target.transform;
            var data=presentation!=null?presentation.CurrentCalibration:default;
            depth=data.Depth>0?data.Depth:.05f;
            var half=new Vector2(data.Width>0?data.Width*.5f:.125f,data.Height>0?data.Height*.5f:.085f);
            root=new GameObject("Squeeze Tabletop Modes").transform;
            UpdatePose();
            var viewerDirection=Camera.main!=null?Camera.main.transform.position-root.position:root.forward;
            uiRoot=new GameObject("Bag-aligned Table UI").transform;uiRoot.SetParent(root,false);
            uiOffset=BuildAlignedUIRotation(root.rotation,viewerDirection);uiRoot.localRotation=uiOffset;
            visuals=new TableVisuals(root,half,nature);motes=new TabletopMotePool(visuals);
            buttons=new ModeButtonBar(visuals,uiRoot,mode);text=new TableTextSlot(visuals,uiRoot);
            rhythm=new RhythmGuideView(visuals,mode,motes);breath=new BreathGuideView(visuals,mode,motes);
            var sound=new GameObject("Tabletop Squeeze Audio");sound.transform.SetParent(root,false);
            audioSource=sound.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=1;audioSource.minDistance=.2f;audioSource.maxDistance=3;
            ambient=sound.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.loop=true;ambient.spatialBlend=1;ambient.minDistance=.2f;ambient.maxDistance=2;
            note=Resources.Load<AudioClip>("MoodiumWalkTrail/walk_step_01");
            var tuning=Resources.Load<SqueezeTuning>("MoodiumSqueezeModes/SqueezeTuning");
            rhythmHit=tuning!=null?tuning.RhythmHitClip:null;
            rhythmHitVolume=tuning!=null?tuning.RhythmHitVolume:.22f;
            air=BuildAir();ownsAir=true;ambient.clip=air;ambient.volume=0;ambient.Play();
            completeAt=-1;OnModeChanged();
        }
        public static Quaternion BuildAlignedUIRotation(Quaternion bagRotation,Vector3 viewerDirection)
        {
            // Preserve the bag's edge alignment. Only choose its near side; an arbitrary
            // camera-facing yaw would skew the whole button row relative to the bag.
            var localDirection=Quaternion.Inverse(bagRotation)*viewerDirection;
            return localDirection.z<0?Quaternion.Euler(0,180,0):Quaternion.identity;
        }
        void UpdatePose()
        {
            if(anchor==null||root==null)return;
            var rotation=presentation!=null?presentation.AnchorOrientation:anchor.rotation;
            var normal=rotation*Vector3.up;if(Vector3.Dot(normal,Vector3.up)<0)rotation=rotation*Quaternion.Euler(180,0,0);
            root.SetPositionAndRotation((presentation!=null?presentation.CenterWorldPosition:anchor.position)-rotation*Vector3.up*depth,rotation);
        }
        void Update()
        {
            if(root==null)return;
            root.gameObject.SetActive(capsule!=null&&capsule.gameObject.activeInHierarchy);
            if(!root.gameObject.activeSelf)return;
            UpdatePose();
            if(mode.Complete&&completeAt<0)completeAt=Time.unscaledTime;
            var alpha=completeAt<0?1:1-Mathf.Clamp01((Time.unscaledTime-completeAt)/.4f);
            buttons.Tick(alpha);motes.Tick(alpha);
            rhythm.Tick(mode.Mode==SqueezeMode.Rhythm&&alpha>0,alpha);
            breath.Tick(mode.Mode==SqueezeMode.Breath&&alpha>0,alpha);
            if(alpha<=0){text.Clear();ambient.volume=0;return;}
            if(mode.IsHeld&&!wasHeld){hints.Starts++;if(mode.Mode!=SqueezeMode.Free)Play(.035f,1);}
            wasHeld=mode.IsHeld;
            if(mode.Mode==SqueezeMode.Breath)
            {
                if(mode.IsHeld)text.ShowWord("inhale",breath.Intensity);
                else if(double.IsFinite(mode.SinceRelease))text.ShowWord("exhale",breath.Intensity);
                text.Count(mode.Breath?.BreathCount??0,true);
                if(mode.HeldSeconds>6)OverrideOnce("let_go");
                var hint=hints.Starts==0?"ready":mode.IsHeld&&hints.Starts<=2?"hold_hint":!mode.IsHeld&&hints.Starts<=2?"release_hint":null;
                text.ShowHint(Time.unscaledTime<overrideUntil?overrideHint:hints.Show(hint));
            }
            else if(mode.Mode==SqueezeMode.Rhythm)
            {
                var phase=(float)mode.RhythmPhase;
                if(phase<lastPhase){cycles++;if(nature)Play(.025f,1);}
                lastPhase=phase;
                if(mode.Rhythm.Adaptive)OverrideOnce("follow");
                text.ShowHint(Time.unscaledTime<overrideUntil?overrideHint:cycles<3?hints.Show("rhythm_hint"):null);
            }
            text.Tick(mode.Mode==SqueezeMode.Breath&&(mode.IsHeld||double.IsFinite(mode.SinceRelease)));
            ambient.volume=Mathf.MoveTowards(ambient.volume,mode.Mode==SqueezeMode.Breath&&mode.IsHeld?.025f:0,Time.unscaledDeltaTime*.04f);
        }
        void OverrideOnce(string key)
        {
            var message=hints.Once(key);if(message==null)return;overrideHint=message;overrideUntil=Time.unscaledTime+4;
        }
        void OnRhythmHit()
        {
            rhythm?.Hit();
            if(mode.Rhythm?.LastAccuracy>0&&audioSource!=null&&rhythmHit!=null)
            {
                audioSource.pitch=1+.055f*Math.Min(8,mode.Rhythm.Combo);
                audioSource.PlayOneShot(rhythmHit,rhythmHitVolume);
            }
            if(mode.Rhythm?.LastRush==true)StartCoroutine(RushNotes());
            if(mode.Rhythm!=null)
            {
                var now=Time.unscaledTime;hints.Rapid=now-lastSqueeze<.55f?hints.Rapid+1:0;lastSqueeze=now;
                if(hints.Rapid>=5)OverrideOnce("slow");
            }
        }
        float lastSqueeze=-100;
        IEnumerator RushNotes(){for(int i=0;i<4;i++){Play(.06f,1+i*.14f);yield return new WaitForSecondsRealtime(.08f);}}
        void OnReleased(SqueezeSample sample)
        {
            if(mode.Mode!=SqueezeMode.Breath)return;
            breath?.Released(sample);Play(.05f,(float)(.9+(mode.Breath?.LastQuality??0)*.35));
            hints.ShortHolds=sample.HoldSeconds<1.2?hints.ShortHolds+1:0;
            if(hints.ShortHolds>=3)OverrideOnce("longer");
        }
        void OnModeChanged()
        {
            if(root==null)return;
            hints.Reset();cycles=0;lastPhase=0;overrideHint=null;overrideUntil=0;wasHeld=false;
            text.Clear();text.Toast(mode.Mode);rhythm.Reset();breath.Reset();motes.Clear();
            StopAllCoroutines();Play(.04f,1);
        }
        public void StageFeedback(int stage)
        {
            if(root==null||stage>=100||Time.unscaledTime-stageAt<.1f)return;
            stageAt=Time.unscaledTime;motes.Emit("squeeze_sparkle",20,false,0,.9f,.025f);Play(.1f,1+stage/150f);
        }
        void Play(float volume,float pitch){if(audioSource==null||note==null)return;audioSource.pitch=pitch;audioSource.PlayOneShot(note,volume);}
        static AudioClip BuildAir()
        {
            const int sampleRate=16000;var data=new float[sampleRate];var rng=new System.Random(639);float smooth=0;
            for(int i=0;i<data.Length;i++){smooth=Mathf.Lerp(smooth,(float)(rng.NextDouble()*2-1),.06f);data[i]=smooth*.2f;}
            var clip=AudioClip.Create("Squeeze Soft Air",sampleRate,1,sampleRate,false);clip.SetData(data,0);return clip;
        }
        void ClearRoot()
        {
            StopAllCoroutines();visuals?.Dispose();visuals=null;
            if(root!=null){root.gameObject.SetActive(false);if(Application.isPlaying)Destroy(root.gameObject);else DestroyImmediate(root.gameObject);}root=null;
            if(ownsAir&&air!=null){if(Application.isPlaying)Destroy(air);else DestroyImmediate(air);}air=null;
        }
        public void Shutdown()
        {
            if(mode!=null){mode.ModeChanged-=OnModeChanged;mode.RhythmHit-=OnRhythmHit;mode.ContactReleased-=OnReleased;}
            ClearRoot();
        }
        void OnDestroy()=>Shutdown();
    }
}
