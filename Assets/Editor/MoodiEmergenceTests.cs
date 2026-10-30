using System.Linq;
using Moodium.Reality;
using Moodium.Interaction;
using UnityEditor;
using UnityEngine;
using NUnit.Framework;
public class MoodiEmergenceTests
{
    sealed class FakeDisplay : ISqueezeProgressDisplay
    {
        public event System.Action<Vector3> Popped;
        public float Squash=>0;
        public void Pop()=>Popped?.Invoke(Vector3.zero);
        public void Configure(Camera c,UnityEngine.Video.VideoClip a,UnityEngine.Video.VideoClip b){}
        public void Begin(bool n){} public void BindToSoft(ChocolateCapsuleInteraction c){} public void SetProgress(float p){}
        public void ResetAndHide(){} public void Splash(float a){} public void SetBubbleSuspended(bool s){}
        public bool TryGetDomeCenter(out Vector3 c,out Vector3 u){c=Vector3.zero;u=Vector3.up;return true;}
        public bool TryGetBurstExclusion(out Vector3 c,out float r){c=Vector3.zero;r=0;return false;}
        public bool OverlapsBurstVideo(Bounds b)=>false;
    }

    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator RescueHandoverIsOneShotAndFollowsPop()
    {
        var settings=Unity.PolySpatial.PolySpatialUserSettings.Instance;
        SessionState.SetBool("MoodiEmergence.RestoreDevice",true);
        SessionState.SetBool("MoodiEmergence.PreviousDevice",settings.ConnectToPlayToDevice);
        typeof(Unity.PolySpatial.PolySpatialUserSettings).GetProperty("ConnectToPlayToDevice").SetValue(settings,false);
        yield return new UnityEngine.TestTools.EnterPlayMode();
        yield return VerifyRuntimeLifecycle();
        yield return new UnityEngine.TestTools.ExitPlayMode();
    }
    [UnityEngine.TestTools.UnityTearDown]
    public System.Collections.IEnumerator RestoreDevicePreference()
    {
        if(!SessionState.GetBool("MoodiEmergence.RestoreDevice",false))yield break;
        if(Application.isPlaying)yield return new UnityEngine.TestTools.ExitPlayMode();
        typeof(Unity.PolySpatial.PolySpatialUserSettings).GetProperty("ConnectToPlayToDevice").SetValue(Unity.PolySpatial.PolySpatialUserSettings.Instance,SessionState.GetBool("MoodiEmergence.PreviousDevice",false));
        SessionState.EraseBool("MoodiEmergence.RestoreDevice");
    }
    static System.Collections.IEnumerator VerifyRuntimeLifecycle()
    {
        var host=new GameObject("rescue lifecycle test");var prefab=GameObject.CreatePrimitive(PrimitiveType.Cube);
        var director=host.AddComponent<MoodiRescueDirector>();var display=new FakeDisplay();
        var events=new System.Collections.Generic.List<string>();
        display.Popped+=_=>events.Add("Popped");director.IntroFinished+=()=>events.Add("IntroFinished");director.Rescued+=_=>events.Add("Rescued");
        director.Begin(prefab,display,null);
        director.StopAllCoroutines();
        typeof(MoodiRescueDirector).GetMethod("FinishIntro",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(director,null);
        display.Pop();
        var deadline=Time.realtimeSinceStartup+2f;
        while(Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(events,Is.EqualTo(new[]{"IntroFinished","Popped","Rescued"}));
        display.Pop();yield return null;
        Assert.That(events.Count(e=>e=="Rescued"),Is.EqualTo(1));
        director.StopAndClear();Object.Destroy(host);Object.Destroy(prefab);
    }

    [TestCase("Idle",3f)][TestCase("Walk",.8f)][TestCase("Touched",1f)][TestCase("Happy",1.4f)][TestCase("Wave",1.6f)]
    public void OriginalActionsKeepTheirOriginalDuration(string name,float duration)
    {
        var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Moodi/MoodiCompanion.controller");
        Assert.That(controller.animationClips.Single(c=>c.name==name).length,Is.EqualTo(duration).Within(.0001f));
    }
    [Test]
    public void MoodiHasAllNineteenBonesAndNineClips()
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/Moodi_Rigged.fbx");
        var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.That(skin.bones.Length,Is.EqualTo(19));
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Model/Moodi_Rigged.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"));
        Assert.That(clips.Select(c=>c.name),Is.EquivalentTo(new[]{"Idle","Walk","Happy","Touched","Wave","Emerge","Strain","FallIn","Peek"}));
    }

    [TestCase("Nature")][TestCase("Candy")]
    public void AnimatorProgressMovesBodyAndMouthTogether(string world)
    {
        var soft=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/prefab/{world}Soft.prefab"));
        var actor=Object.Instantiate(Resources.Load<GameObject>("MoodiEmergence/Moodi"));
        var host=new GameObject("sample test");
        try
        {
            var display=host.AddComponent<MoodiEmergenceDisplay>();display.BindToSoft(soft.AddComponent<ChocolateCapsuleInteraction>());
            var animator=actor.GetComponentInChildren<Animator>(true);display.AttachMoodi(animator);display.SetPeeking(false);
            animator.Play("Emerge",0,0);animator.SetFloat("EmergeProgress",0);animator.Update(0);
            var body=animator.GetComponentsInChildren<Transform>().First(t=>t.name=="Body");var start=body.position;
            animator.SetFloat("EmergeProgress",.95f);animator.Update(.01f);
            display.TryGetDomeCenter(out _,out var up);
            Assert.That(Vector3.Dot(body.position-start,up),Is.GreaterThan(.005f));
        }finally{Object.DestroyImmediate(host);Object.DestroyImmediate(actor);Object.DestroyImmediate(soft);}
    }

    [TestCase(0f,1f)][TestCase(.1f,.08f)][TestCase(.2f,1f)][TestCase(.4f,1f)]
    public void QuickBlinkClosesAndFullyReopens(float seconds,float scale)
        =>Assert.That(MoodiLandingBlink.ScaleAt(seconds),Is.EqualTo(scale).Within(.0001f));

    [TestCase("Nature")][TestCase("Candy")]
    public void SoftSurfaceIsHorizontalAndMouthUpPointsAboveTable(string world)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/prefab/{world}Soft.prefab");
        var mouth=MoodiEmergenceDisplay.FindMouth(prefab.transform);
        Assert.That(Vector3.Dot(mouth.forward,Vector3.up),Is.GreaterThan(.999f));
        var renderer=prefab.GetComponentsInChildren<Renderer>().First(r=>r.name.Contains("_Static"));
        Assert.That(renderer.bounds.size.z,Is.GreaterThan(renderer.bounds.size.y));
    }

    [TestCase("Nature")][TestCase("Candy")]
    public void MouthAssetsHaveRigAndAnimations(string world)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Model/Soft/{world}Soft_Mouth.fbx");
        var rig=MoodiEmergenceDisplay.FindMouth(model.transform);Assert.That(rig,Is.Not.Null);
        Assert.That(rig.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Edge_")||t.name.StartsWith("Mid_")||t.name=="Mouth_Root"),Is.EqualTo(25));
        var clips=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model)).OfType<AnimationClip>().Select(c=>c.name);
        foreach(var n in new[]{"Mouth_Emerge","Mouth_Pucker","Mouth_Pop"})Assert.That(clips,Does.Contain(n));
    }
    [Test]
    public void PoppedIsOneShotAndResetAllowsAnotherSession()
    {
        var soft=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefab/NatureSoft.prefab"));
        var host=new GameObject("pop test");
        try
        {
            var cap=soft.AddComponent<ChocolateCapsuleInteraction>();var display=host.AddComponent<MoodiEmergenceDisplay>();int count=0;display.Popped+=_=>count++;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            for(int session=0;session<2;session++)
            {
                display.BindToSoft(cap);display.SetProgress(1);
                typeof(MoodiEmergenceDisplay).GetField("progress",flags).SetValue(display,1f);
                typeof(MoodiEmergenceDisplay).GetMethod("Update",flags).Invoke(display,null);
                typeof(MoodiEmergenceDisplay).GetMethod("Update",flags).Invoke(display,null);
                Assert.That(count,Is.EqualTo(session+1));display.ResetAndHide();
            }
        }finally{Object.DestroyImmediate(host);Object.DestroyImmediate(soft);}
    }
    [Test]
    public void ProgressNeverReversesAndFullEmergenceTakesAtLeastThreeSeconds()
    {
        float p=0,v=0;
        for(int i=0;i<180;i++)p=MoodiEmergenceDisplay.Advance(p,1,ref v,.25f,.3f,1f/60);
        Assert.That(p,Is.LessThan(.995f));
        var before=p;p=MoodiEmergenceDisplay.Advance(p,0,ref v,.25f,.3f,1f/60);Assert.That(p,Is.GreaterThanOrEqualTo(before));
    }
    [TestCase("Nature")][TestCase("Candy")]
    public void BindingAndAttachmentPreserveTheMouthFrame(string world)
    {
        var soft=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/prefab/{world}Soft.prefab"));
        var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefab/Moodi meshy.prefab"));
        var host=new GameObject("test display");
        try
        {
            var cap=soft.AddComponent<ChocolateCapsuleInteraction>();var display=host.AddComponent<MoodiEmergenceDisplay>();
            display.BindToSoft(cap);Assert.That(display.Ready,Is.True);Assert.That(display.TryGetDomeCenter(out _,out _),Is.True);
            display.AttachMoodi(actor.GetComponentInChildren<Animator>(true));Assert.That(display.Attached,Is.True);
            var mouth=MoodiEmergenceDisplay.FindMouth(soft.transform);
            var rig=soft.GetComponentsInChildren<Transform>().First(t=>t.name=="Moodi_Rig");
            for(int i=0;i<16;i++)Assert.That(rig.localToWorldMatrix[i],Is.EqualTo(mouth.localToWorldMatrix[i]).Within(.0001));
            display.ResetAndHide();Assert.That(display.Attached,Is.False);
        }finally{Object.DestroyImmediate(host);Object.DestroyImmediate(actor);Object.DestroyImmediate(soft);}
    }
}
