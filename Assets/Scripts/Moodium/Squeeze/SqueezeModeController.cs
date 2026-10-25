using System;
using System.Collections.Generic;
using Moodium.Audio;
using Moodium.CandyWorld;
using Moodium.Interaction;
using Moodium.MoodCheck;
using UnityEngine;
namespace Moodium.Squeeze
{
    [DisallowMultipleComponent]
    public sealed class SqueezeModeController : MonoBehaviour
    {
        readonly SqueezeTracker tracker=new();
        readonly HandClosureSampler closureSampler=new();
        readonly Dictionary<int,ChocolateCapsuleInteraction> accepted=new();
        readonly HashSet<string> modesUsed=new();
        readonly double[] timeInMode=new double[3];
        CandyEnergyController energy;
        MoodCheckContext context;
        Func<ChocolateCapsuleInteraction,bool> validSource;
        SqueezeModeState state;
        ISqueezeScorer scorer;
        SqueezeParameters defaults;
        SqueezeLog log;
        SqueezeTableView view;
        bool running,nature,finishing;
        double began,modeBegan,lastAudio=double.NaN,audioLoops,beatTime,lastTick,nextFind,contactEnergy;
        double accuracySum,completedAt=-1,breathSeconds;
        int squeezeCount,rhythmCount,totalBreaths;
        bool adaptiveUsed;
        SqueezeSample contact;
        public SqueezeMode Mode=>state?.Current??SqueezeMode.Free;
        public SqueezeMode Recommended=>state?.Recommended??SqueezeMode.Free;
        public SqueezeMode? Pending=>state?.Pending;
        public SqueezeParameters Parameters {get;private set;}
        public RhythmScorer Rhythm=>scorer as RhythmScorer;
        public BreathScorer Breath=>scorer as BreathScorer;
        public bool IsHeld=>tracker.IsHeld;
        public double HeldSeconds=>IsHeld?Math.Max(0,Time.unscaledTimeAsDouble-tracker.StartedAt):0;
        public double SinceRelease {get;private set;}=double.PositiveInfinity;
        public double Elapsed=>Math.Max(0,Time.unscaledTimeAsDouble-began);
        public bool Complete=>energy!=null&&energy.NormalizedEnergy>=1f;
        public double RhythmPhase
        {
            get
            {
                var t=Rhythm!=null&&Rhythm.Adaptive?(Time.unscaledTimeAsDouble-Rhythm.LastStart)/Rhythm.AdaptiveInterval:
                    (beatTime-Parameters.BeatOffset-Parameters.InputLatency)/Parameters.RhythmPeriod;
                return t-Math.Floor(t);
            }
        }
        public event Action ModeChanged;
        public event Action<SqueezeSample> ContactReleased;
        public event Action RhythmHit;
        double releasedAt=double.NaN;

        public void BeginSession(CandyEnergyController target,MoodCheckContext moodContext,bool isNature,
            Func<ChocolateCapsuleInteraction,bool> accepts)
        {
            StopSession();energy=target;context=moodContext;nature=isNature;validSource=accepts;
            var tuning=Resources.Load<SqueezeTuning>("MoodiumSqueezeModes/SqueezeTuning");
            defaults=tuning!=null?tuning.parameters.Copy():new SqueezeParameters();
            state=new SqueezeModeState(context?.PreSample?.mood);
            began=modeBegan=lastTick=Time.unscaledTimeAsDouble;beatTime=0;lastAudio=double.NaN;audioLoops=0;
            accepted.Clear();modesUsed.Clear();Array.Clear(timeInMode,0,timeInMode.Length);
            squeezeCount=totalBreaths=rhythmCount=0;accuracySum=breathSeconds=0;completedAt=-1;adaptiveUsed=false;releasedAt=double.NaN;
            tracker.Reset();tracker.Started+=OnStarted;tracker.Held+=OnHeld;tracker.Released+=OnReleased;
            ChocolateCapsuleInteraction.AnyCapsuleHeld+=OnContactHeld;
            ChocolateCapsuleInteraction.AnyCapsuleReleased+=OnContactReleased;
            ChocolateCapsuleInteraction.AnyCapsuleTrackingLost+=OnTrackingLost;
            log=new SqueezeLog(context?.ActiveVisitId,Application.isPlaying);
            running=true;finishing=false;ResetScorer();
            view=gameObject.AddComponent<SqueezeTableView>();view.Initialize(this,nature);
            enabled=true;
        }
        void ResetScorer()
        {
            Parameters=state.ManualOverride?defaults.Copy():SqueezeTuning.ForMood(defaults,context?.PreSample?.mood);
            scorer=Mode switch { SqueezeMode.Rhythm=>new RhythmScorer(Parameters),SqueezeMode.Breath=>new BreathScorer(Parameters),_=>new FreeScorer() };
            modesUsed.Add(Mode.ToString());context?.RecordSqueezeMode(Mode.ToString());
        }
        public void AcceptPinch(ChocolateCapsuleInteraction capsule)
        {
            if(!running||Complete||capsule==null||validSource?.Invoke(capsule)!=true)return;
            accepted[capsule.GetInstanceID()]=capsule;view.Bind(capsule);
            tracker.Contact(capsule.GetInstanceID(),Time.unscaledTimeAsDouble);
            context?.RegisterPinch();
            if(Mode==SqueezeMode.Free)Pay(5,"legacy");
        }
        void OnContactHeld(ChocolateCapsuleInteraction capsule,Vector3 position)
        {
            if(!running||(Complete&&!tracker.IsHeld)||capsule==null||!accepted.ContainsKey(capsule.GetInstanceID()))return;
            tracker.Contact(capsule.GetInstanceID(),Time.unscaledTimeAsDouble);
        }
        void OnContactReleased(ChocolateCapsuleInteraction capsule)
        {if(capsule!=null&&accepted.ContainsKey(capsule.GetInstanceID()))tracker.Release(capsule.GetInstanceID(),Time.unscaledTimeAsDouble);}
        void OnTrackingLost(ChocolateCapsuleInteraction capsule)
        {if(capsule!=null&&accepted.ContainsKey(capsule.GetInstanceID()))tracker.Release(capsule.GetInstanceID(),Time.unscaledTimeAsDouble,true);}
        SqueezeSample Enrich(SqueezeSample sample)=>new(sample.Time,sample.HoldSeconds,sample.GapSeconds,closureSampler.Sample(),beatTime);
        void OnStarted(SqueezeSample sample)
        {
            contact=Enrich(sample);contactEnergy=0;squeezeCount++;SinceRelease=double.PositiveInfinity;
            if(Mode!=SqueezeMode.Free)Pay(scorer.OnStarted(contact),"start");
            if(Rhythm!=null)
            {accuracySum+=Rhythm.LastAccuracy;rhythmCount++;adaptiveUsed|=Rhythm.Adaptive;context?.RecordRhythmAccuracy((float)Rhythm.LastAccuracy);RhythmHit?.Invoke();}
        }
        void OnHeld(SqueezeSample sample,double delta)=>Pay(scorer.OnHeld(Enrich(sample),delta),"hold");
        void OnReleased(SqueezeSample sample)
        {
            Pay(scorer.OnReleased(Enrich(sample)),"release");releasedAt=sample.Time;
            if(Breath!=null&&Breath.LastQuality>0){totalBreaths++;breathSeconds+=sample.HoldSeconds+(double.IsFinite(contact.GapSeconds)?contact.GapSeconds:0);context?.RecordBreath();}
            WriteContact(sample);ContactReleased?.Invoke(sample);
            if(!finishing&&state.Pending.HasValue){var old=Mode;if(state.Released())CommitMode(old);}
        }
        void Pay(double amount,string reason)
        {
            if(energy==null||!double.IsFinite(amount)||amount<=0)return;
            var previous=energy.Energy;
            energy.AddEnergy((float)(amount*EnergyAssist.Multiplier(Elapsed,Mode)),Mode==SqueezeMode.Free?"legacy":"squeeze-"+reason);
            contactEnergy+=energy.Energy-previous;
            if(Complete&&completedAt<0)completedAt=Elapsed;
        }
        public void RequestMode(SqueezeMode mode)
        {
            if(!running||Complete)return;
            var old=Mode;if(state.Request(mode,tracker.IsHeld))CommitMode(old);
        }
        void CommitMode(SqueezeMode old)
        {
            timeInMode[(int)old]+=Time.unscaledTimeAsDouble-modeBegan;modeBegan=Time.unscaledTimeAsDouble;
            log?.Write(new SqueezeLog.Entry{type="switch",visitId=context?.ActiveVisitId,world=nature?"nature":"candy",from=old.ToString(),to=Mode.ToString(),tStart=Elapsed,energy=energy.Energy});
            ResetScorer();releasedAt=double.NaN;SinceRelease=double.PositiveInfinity;ModeChanged?.Invoke();
        }
        void Update()
        {
            if(!running)return;
            var now=Time.unscaledTimeAsDouble;
            if(!nature&&MoodiumAudioManager.TryGetBackgroundMusicTime(out var seconds))
            {
                if(!double.IsNaN(lastAudio)&&seconds<lastAudio-.2)audioLoops+=MoodiumAudioManager.BackgroundMusicDuration;
                lastAudio=seconds;beatTime=seconds+audioLoops;
            }
            else beatTime+=Math.Max(0,now-lastTick);
            lastTick=now;
            foreach(var pair in accepted)
                if(pair.Value==null||!pair.Value.gameObject.activeInHierarchy)tracker.Release(pair.Key,now,true);
            tracker.Tick(now);
            SinceRelease=double.IsNaN(releasedAt)?double.PositiveInfinity:Math.Max(0,now-releasedAt);
            if(view!=null&&!view.IsBound&&now>=nextFind)
            {
                nextFind=now+.5;
                foreach(var capsule in FindObjectsByType<ChocolateCapsuleInteraction>(FindObjectsSortMode.None))
                    if(validSource?.Invoke(capsule)==true){view.Bind(capsule);break;}
            }
        }
        void WriteContact(SqueezeSample end)
        {
            log?.Write(new SqueezeLog.Entry{type="squeeze",visitId=context?.ActiveVisitId,world=nature?"nature":"candy",mode=Mode.ToString(),
                tStart=Math.Max(0,contact.Time-began),holdSeconds=end.HoldSeconds,gapSeconds=double.IsFinite(contact.GapSeconds)?contact.GapSeconds:0,
                closureAvailable=contact.Closure.HasValue,closure=contact.Closure??0,beatError=Rhythm?.LastError??0,combo=Rhythm?.Combo??0,
                breathQuality=Breath?.LastQuality??0,exhaleQuality=Breath?.LastExhaleQuality??0,energyGained=contactEnergy,
                assistMult=EnergyAssist.Multiplier(Elapsed,Mode)});
        }
        public void StageFeedback(int stage)=>view?.StageFeedback(stage);
        public void StopSession()
        {
            if(running)
            {
                finishing=true;
                if(tracker.IsHeld){tracker.ReleaseAllForExit(Time.unscaledTimeAsDouble);tracker.Tick(Time.unscaledTimeAsDouble+1);}
                timeInMode[(int)Mode]+=Math.Max(0,Time.unscaledTimeAsDouble-modeBegan);
                log?.Write(new SqueezeLog.Entry{type="summary",visitId=context?.ActiveVisitId,world=nature?"nature":"candy",squeezeCount=squeezeCount,
                    timeTo100=completedAt,modesUsed=new List<string>(modesUsed).ToArray(),timeInMode=(double[])timeInMode.Clone(),
                    avgBeatAccuracy=rhythmCount>0?accuracySum/rhythmCount:0,breathCount=totalBreaths,
                    avgBreathCycleSeconds=totalBreaths>0?breathSeconds/totalBreaths:0,adaptiveModeUsed=adaptiveUsed,hintsShown=view!=null?view.HintsShown:Array.Empty<string>()});
            }
            running=false;
            tracker.Started-=OnStarted;tracker.Held-=OnHeld;tracker.Released-=OnReleased;tracker.Reset();accepted.Clear();
            ChocolateCapsuleInteraction.AnyCapsuleHeld-=OnContactHeld;ChocolateCapsuleInteraction.AnyCapsuleReleased-=OnContactReleased;
            ChocolateCapsuleInteraction.AnyCapsuleTrackingLost-=OnTrackingLost;
            if(view!=null){view.Shutdown();if(Application.isPlaying)Destroy(view);else DestroyImmediate(view);}view=null;
        }
        void OnDisable()=>StopSession();
    }
}
