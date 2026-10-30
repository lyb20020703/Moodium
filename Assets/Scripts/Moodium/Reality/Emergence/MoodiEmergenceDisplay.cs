using System;
using System.Linq;
using Moodium.Interaction;
using Moodium.Characters;
using UnityEngine;
using UnityEngine.Video;

namespace Moodium.Reality
{
    public sealed class MoodiEmergenceDisplay : MonoBehaviour, ISqueezeProgressDisplay
    {
        [SerializeField] float m_SmoothTime=.25f, m_MaxSpeed=.3f, m_PopThreshold=.995f;
        [SerializeField] float m_PressLift=.26f, m_ReboundLift=.07f;
        [SerializeField] float m_BlinkClose=.1f, m_BlinkOpen=.1f, m_ClosedScale=.08f;
        Transform mouth, rig, oldParent, stem;
        Animator mouthAnimator, moodi;
        MoodiCompanionAnimator companion;
        MoodiVoice voice;
        readonly float[] voiceSamples=new float[64];
        bool companionEnabled, popped, suspended, pressed, attached, peeking;
        ChocolateCapsuleInteraction capsule;
        float target, progress, velocity, spring, springVelocity, blink=-100f, blinkAgain=-100f;
        int presses;
        Transform[] eyes;
        Vector3[] eyeScales;
        Vector3 alignedPosition;
        Vector3 originalPosition,originalScale;
        Quaternion originalRotation;
        float boundAt;
        GameObject fallback;
        ParticleSystem particles;
        bool natureWorld;
        bool resourcesAvailable;
        public bool HasStory { get; set; } = true;
        [SerializeField] int m_FallParticles=6,m_PopParticles=14;
        public float Squash=>spring;
        public bool Ready=>resourcesAvailable && mouth!=null && mouthAnimator!=null && mouthAnimator.runtimeAnimatorController!=null;
        public bool Attached=>attached;
        public event Action<Vector3> Popped;
        public static Transform FindMouth(Transform root)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Mouth_Rig");
        public void Configure(Camera c, VideoClip a, VideoClip b) { }
        public void Begin(bool nature) { ResetAndHide();natureWorld=nature; }
        public void BindToSoft(ChocolateCapsuleInteraction c)
        {
            if(capsule==c && Ready)return;
            capsule=c;boundAt=Time.time; mouth=c!=null?FindMouth(c.transform):null;
            mouthAnimator=mouth!=null?mouth.GetComponentInParent<Animator>():null;
            var prefab=Resources.Load<GameObject>("MoodiEmergence/Moodi");
            var animator=prefab!=null?prefab.GetComponentInChildren<Animator>(true):null;
            resourcesAvailable=animator!=null && animator.runtimeAnimatorController!=null &&
                animator.runtimeAnimatorController.animationClips.Any(clip=>clip.name=="Emerge") &&
                Resources.Load<Material>("MoodiEmergence/"+(natureWorld?"Nature":"Candy")+"Particles")!=null;
            if(Ready){mouthAnimator.Play("Emerge",0,0);mouthAnimator.SetFloat("Progress",0);}
        }
        public bool TryGetDomeCenter(out Vector3 center,out Vector3 up)
        {center=mouth!=null?mouth.position:Vector3.zero;up=mouth!=null?mouth.forward:Vector3.up;return Ready;}
        public bool TryGetBurstExclusion(out Vector3 c,out float r){c=Vector3.zero;r=0;return false;}
        public bool OverlapsBurstVideo(Bounds b)=>false;
        public void SetBubbleSuspended(bool value)
        {suspended=value;if(value&&particles!=null)particles.Clear();if(moodi!=null)foreach(var r in moodi.GetComponentsInChildren<Renderer>())r.enabled=!value;}
        public void SetProgress(float p){target=Mathf.Max(target,Mathf.Clamp01(p));}
        public void Splash(float amount){spring=amount;springVelocity=0;Emit(m_FallParticles);}
        void Emit(int count)
        {
            if(mouth==null)return;
            if(particles==null)
            {
                var mat=Resources.Load<Material>("MoodiEmergence/"+(natureWorld?"Nature":"Candy")+"Particles");if(mat==null)return;
                var obj=new GameObject("Moodi emergence particles");obj.transform.SetParent(transform,false);
                particles=obj.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.8f;main.startSize=.006f;main.gravityModifier=.12f;main.maxParticles=32;
                var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.enabled=false;
                var fade=particles.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
                obj.GetComponent<ParticleSystemRenderer>().sharedMaterial=mat;
            }
            particles.Play();
            for(int i=0;i<count;i++){var direction=UnityEngine.Random.insideUnitCircle.normalized;particles.Emit(new ParticleSystem.EmitParams{position=mouth.position,velocity=mouth.forward*.4f+(mouth.right*direction.x+mouth.up*direction.y)*.2f},1);}
        }
        public void SetPeeking(bool value)
        {peeking=value;if(moodi!=null){moodi.SetBool("Peeking",value);moodi.SetBool("Emerging",!value);}}
        public void AttachMoodi(Animator animator)
        {
            if(!Ready||animator==null)return;
            moodi=animator;rig=moodi.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Moodi_Rig");
            if(rig==null){moodi=null;return;}
            companion=moodi.GetComponentInParent<MoodiCompanionAnimator>();
            voice=moodi.GetComponentInParent<MoodiVoice>();
            if(companion!=null){companionEnabled=companion.enabled;companion.enabled=false;}
            oldParent=moodi.transform.parent;
            originalPosition=moodi.transform.localPosition;originalScale=moodi.transform.localScale;originalRotation=moodi.transform.localRotation;
            var relative=moodi.transform.worldToLocalMatrix*rig.localToWorldMatrix;
            var inverse=relative.inverse;
            moodi.transform.SetParent(mouth,false);
            moodi.transform.localPosition=inverse.GetColumn(3);
            moodi.transform.localRotation=inverse.rotation;
            moodi.transform.localScale=inverse.lossyScale;
            alignedPosition=moodi.transform.localPosition;
            eyes=new[]{rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Eye_L"),rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Eye_R")};
            eyeScales=eyes.Select(t=>t!=null?t.localScale:Vector3.one).ToArray();
            attached=true; moodi.SetBool("Emerging",false);moodi.SetBool("Peeking",false);moodi.Play("FallIn",0,0);
            stem=rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Sprout_Stem");
        }
        public void DetachMoodi(bool immediate=false)
        {
            if(eyes!=null)for(int i=0;i<eyes.Length;i++)if(eyes[i]!=null)eyes[i].localScale=eyeScales[i];
            if(moodi!=null){moodi.SetBool("Emerging",false);moodi.SetBool("Peeking",false);moodi.SetFloat("Press",0);
                moodi.transform.SetParent(oldParent,true);moodi.CrossFadeInFixedTime("Happy",.1f);
                if(!immediate && Application.isPlaying && isActiveAndEnabled)StartCoroutine(RestoreSize(moodi.transform,originalPosition,originalRotation,originalScale));
                else{moodi.transform.localPosition=originalPosition;moodi.transform.localRotation=originalRotation;moodi.transform.localScale=originalScale;}}
            if(companion!=null && companionEnabled)companion.ResumeAfterRescue();
            attached=false;moodi=null;rig=null;companion=null;voice=null;eyes=null;
        }
        public void Blink(){blink=Time.time;}
        System.Collections.IEnumerator RestoreSize(Transform model,Vector3 position,Quaternion rotation,Vector3 scale)
        {
            var p=model.localPosition;var r=model.localRotation;var s=model.localScale;
            for(float t=0;t<.6f;t+=Time.deltaTime){if(model==null)yield break;var k=Mathf.SmoothStep(0,1,t/.6f);model.localPosition=Vector3.Lerp(p,position,k);model.localRotation=Quaternion.Slerp(r,rotation,k);model.localScale=Vector3.Lerp(s,scale,k);yield return null;}
            if(model!=null){model.localPosition=position;model.localRotation=rotation;model.localScale=scale;}
        }
        public void NotifyStage(int stage){if(stage==20){Blink();blinkAgain=Time.time+.3f;}}
        public static float Advance(float current,float target,ref float velocity,float time,float speed,float dt)
            =>Mathf.Max(current,Mathf.SmoothDamp(current,target,ref velocity,time,speed,dt));
        void Update()
        {
            if(!Ready||suspended||popped)return;
            if(!attached && fallback==null && (!HasStory || Time.time-boundAt>8f))
            {
                var prefab=Resources.Load<GameObject>("MoodiEmergence/Moodi");
                if(prefab!=null){fallback=Instantiate(prefab,transform);AttachMoodi(fallback.GetComponentInChildren<Animator>(true));SetPeeking(false);}
            }
            var down=capsule!=null&&capsule.IsPinched;
            if(down&&!pressed){springVelocity+=2.5f;presses++;if(presses==1||presses==5||presses==10||presses==15)Blink();}
            pressed=down;
            spring=SqueezeProgressVideoController.StepSpring(spring,ref springVelocity,down?1:0,down?2500:220,down?.85f:.32f,Time.deltaTime);
            progress=Advance(progress,target,ref velocity,m_SmoothTime,m_MaxSpeed,Time.deltaTime);
            mouthAnimator.SetFloat("Progress",progress);mouthAnimator.SetFloat("Press",Mathf.Max(0,spring));
            if(moodi!=null){moodi.SetFloat("EmergeProgress",progress);moodi.SetFloat("Press",Mathf.Max(0,spring));}
            if(target>=1&&progress>=m_PopThreshold){popped=true;mouthAnimator.SetFloat("Press",0);mouthAnimator.Play("Pop",0,0);Emit(m_PopParticles);Popped?.Invoke(mouth.position);if(fallback!=null){DetachMoodi();Destroy(fallback);fallback=null;}}
        }
        void LateUpdate()
        {
            if(!attached||moodi==null)return;
            // Blender rig coordinates are Z-up; FBX stores one authored unit as 1 cm.
            moodi.transform.localPosition=alignedPosition+Vector3.forward*.01f*(spring>=0?m_PressLift*spring:m_ReboundLift*spring);
            if(peeking&&stem!=null)
            {
                var amplitude=voice!=null && voice.TryGetOutputLevel(voiceSamples,out var rms)?Mathf.Lerp(2f,14f,Mathf.Clamp01(rms*12f)):2f;
                stem.localRotation*=Quaternion.Euler(0,0,Mathf.Sin(Time.time*7f*2*Mathf.PI)*amplitude);
            }
            if(Time.time>=blinkAgain&&blinkAgain>0){blink=Time.time;blinkAgain=-100;}
            var t=Time.time-blink;
            var close=t>=0&&t<m_BlinkClose?Mathf.Lerp(1,m_ClosedScale,t/m_BlinkClose):t<m_BlinkClose+m_BlinkOpen?Mathf.Lerp(m_ClosedScale,1,(t-m_BlinkClose)/m_BlinkOpen):1;
            for(var i=0;i<eyes.Length;i++)if(eyes[i]!=null)eyes[i].localScale=Vector3.Scale(eyeScales[i],new Vector3(1,close,1));
        }
        public void ResetAndHide()
        {
            StopAllCoroutines();DetachMoodi(true);target=progress=velocity=spring=springVelocity=0;popped=suspended=pressed=peeking=false;presses=0;blink=blinkAgain=-100f;
            if(fallback!=null){Destroy(fallback);fallback=null;}
            if(particles!=null)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(Ready){mouthAnimator.SetFloat("Progress",0);mouthAnimator.SetFloat("Press",0);mouthAnimator.Play("Emerge",0,0);}
            capsule=null;mouth=null;mouthAnimator=null;
        }
        void OnDisable()=>ResetAndHide();
    }
}
