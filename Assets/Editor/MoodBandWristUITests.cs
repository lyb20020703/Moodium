using Moodium.CandyWorld;
using NUnit.Framework;
using UnityEngine;
using Moodium.Flow;
using System.Reflection;

/// <summary>Tests for the v2 "Mood Band" helpers. The existing CandyEnergyWristRingTests must keep passing too.</summary>
public sealed class MoodBandWristUITests
{
    [Test]
    public void RaisedWristRequiresPresentationNotJustHeadDirection()
    {
        Assert.That(CandyEnergyWristUI.EvaluateWristPresentation(false,40,.5f,-.2f,.8f),Is.True);
        Assert.That(CandyEnergyWristUI.EvaluateWristPresentation(false,10,.5f,-.6f,.8f),Is.False,"Low hands must not open the card.");
        Assert.That(CandyEnergyWristUI.EvaluateWristPresentation(false,10,.5f,-.2f,-.3f),Is.False,"Palm facing viewer must not open dorsal card.");
        Assert.That(CandyEnergyWristUI.EvaluateWristPresentation(false,10,.8f,-.2f,.8f),Is.False);
        Assert.That(CandyEnergyWristUI.EvaluateWristPresentation(true,50,.5f,-.5f,.2f),Is.True,"Keep comfort hysteresis after opening.");
    }

    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object ui,string field,object value)=>ui.GetType().GetField(field,Private).SetValue(ui,value);
    static T Get<T>(object ui,string field)=>(T)ui.GetType().GetField(field,Private).GetValue(ui);
    static void Call(object ui,string method,params object[] args)=>ui.GetType().GetMethod(method,Private).Invoke(ui,args);

    static CandyEnergyWristUI Create(out GameObject root,out Camera camera)
    {
        root=new GameObject("Wrist test");camera=new GameObject("Wrist camera").AddComponent<Camera>();
        camera.transform.position=new Vector3(0,1.6f,0);
        var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/Moodium/Fonts/AlibabaPuHuiTi Moodium SDF.asset");
        var ui=root.AddComponent<CandyEnergyWristUI>();ui.Build(camera,font,null,null);
        root.transform.SetPositionAndRotation(new Vector3(0,1.4f,.45f),Quaternion.LookRotation(Vector3.back,Vector3.up));
        return ui;
    }
    static void Present(CandyEnergyWristUI ui,bool tracked)
    {
        typeof(CandyEnergyWristUI).GetProperty("HandTracked").SetValue(ui,tracked);
        Call(ui,"UpdateGlance",.3f);Call(ui,"UpdateCardAndToast",.3f);
    }
    [Test] public void CardInputIsDisabledInitiallyAndImmediatelyOnTrackingLoss()
    {
        var ui=Create(out var root,out var camera);
        try
        {
            ui.SetExitAvailable(true);
            var exit=Get<GameObject>(ui,"m_ExitButtonRoot").GetComponent<BoxCollider>();
            Assert.That(exit.enabled,Is.False,"Hidden card must never have live colliders.");
            Present(ui,true);Assert.That(exit.enabled,Is.True);
            Call(ui,"PressExit");Assert.That(Get<WristExitConfirmation>(ui,"m_ExitConfirmation").IsArmed(Time.time),Is.True);
            Present(ui,false);Assert.That(exit.enabled,Is.False);
            Assert.That(Get<WristExitConfirmation>(ui,"m_ExitConfirmation").IsArmed(Time.time),Is.False);
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);}
    }
    [Test] public void ExitNeedsTwoVisiblePressesAndAvailabilityRefreshDoesNotCancelIt()
    {
        var ui=Create(out var root,out var camera);
        try
        {
            ui.SetExitAvailable(true);Present(ui,true);int exits=0;ui.ExitPressed+=()=>exits++;
            Call(ui,"PressExit");Assert.That(exits,Is.Zero);
            ui.SetExitAvailable(true);ui.SetReleaseAvailable(false);
            Assert.That(Get<WristExitConfirmation>(ui,"m_ExitConfirmation").IsArmed(Time.time),Is.True);
            Call(ui,"PressExit");Assert.That(exits,Is.EqualTo(1));
            root.SetActive(false);
            Assert.That(Get<WristExitConfirmation>(ui,"m_ExitConfirmation").IsArmed(Time.time),Is.False);
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);}
    }
    [Test] public void ProgressResetClearsSocketsAndShowsRealEnergyStagesNotUncollectedGifts()
    {
        var ui=Create(out var root,out var camera);
        try
        {
            ui.SetProgress(62,.62f);
            Assert.That(Get<TMPro.TMP_Text>(ui,"m_Subtitle").text,Does.Contain("2/4"));
            ui.SetCollectedGems(2);ui.SetProgress(0,0);
            Assert.That(ui.DisplayedProgress,Is.Zero);
            Assert.That(Get<int>(ui,"m_CollectedGems"),Is.Zero);
            Assert.That(Get<TMPro.TMP_Text>(ui,"m_Subtitle").text,Does.Contain("0/4"));
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);}
    }
    [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
    public void ReleaseTextDoesNotOverlapItsIconOrToggleKnob(bool english,bool on)
    {
        var priorLanguage=PlayerPrefs.GetString("Moodium.Language","Chinese");
        PlayerPrefs.SetString("Moodium.Language",english?"English":"Chinese");
        var ui=Create(out var root,out var camera);
        try
        {
            ui.SetReleaseAvailable(true);ui.SetReleaseToggleState(on);
            var chip=Get<GameObject>(ui,"m_ReleaseButtonRoot").transform;
            var text=RectTransformUtility.CalculateRelativeRectTransformBounds(chip,Get<TMPro.TMP_Text>(ui,"m_ReleaseLabel").transform);
            var knob=RectTransformUtility.CalculateRelativeRectTransformBounds(chip,Get<UnityEngine.UI.Image>(ui,"m_ReleaseKnob").transform);
            var icon=RectTransformUtility.CalculateRelativeRectTransformBounds(chip,chip.Find("Icon"));
            Assert.That(text.max.x,Is.LessThan(knob.min.x));
            Assert.That(icon.max.x,Is.LessThan(text.min.x));
        }
        finally{PlayerPrefs.SetString("Moodium.Language",priorLanguage);Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);}
    }
    [Test] public void EnglishTitleRemainsOneLineAboveTheSubtitle()
    {
        var prior=PlayerPrefs.GetString("Moodium.Language","Chinese");PlayerPrefs.SetString("Moodium.Language","English");
        var ui=Create(out var root,out var camera);
        try
        {
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/Moodium/Fonts/Inter-Regular SDF.asset");
            ui.ConfigureLanguageFonts(null,font);
            var title=Get<TMPro.TMP_Text>(ui,"m_Title");title.ForceMeshUpdate(true,true);
            Assert.That(title.textInfo.lineCount,Is.EqualTo(1));
        }
        finally{PlayerPrefs.SetString("Moodium.Language",prior);Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);}
    }
    [Test]
    public void Sockets_SitAtTheFourRewardStages()
    {
        Assert.That(CandyEnergyWristUI.GetSocketProgress(0), Is.EqualTo(.2f));
        Assert.That(CandyEnergyWristUI.GetSocketProgress(1), Is.EqualTo(.5f));
        Assert.That(CandyEnergyWristUI.GetSocketProgress(2), Is.EqualTo(.8f));
        Assert.That(CandyEnergyWristUI.GetSocketProgress(3), Is.EqualTo(1f));
    }

    [Test]
    public void Thread_RunsAcrossTheBackOfTheWrist()
    {
        Assert.That(CandyEnergyWristUI.ThreadAngleForProgress(0f, 150f, 30f), Is.EqualTo(150f));
        Assert.That(CandyEnergyWristUI.ThreadAngleForProgress(.5f, 150f, 30f), Is.EqualTo(90f), "Halfway must be the centre of the back of the wrist.");
        Assert.That(CandyEnergyWristUI.ThreadAngleForProgress(1f, 150f, 30f), Is.EqualTo(30f));
        Assert.That(CandyEnergyWristUI.ThreadAngleForProgress(2f, 150f, 30f), Is.EqualTo(30f));
    }

    [Test]
    public void Glance_UsesHysteresisAndADistanceLimit()
    {
        Assert.That(CandyEnergyWristUI.EvaluateGlance(false, 30f, .4f, 26f, 34f, .65f), Is.False, "30° is not enough to open the card.");
        Assert.That(CandyEnergyWristUI.EvaluateGlance(false, 20f, .4f, 26f, 34f, .65f), Is.True);
        Assert.That(CandyEnergyWristUI.EvaluateGlance(true, 30f, .4f, 26f, 34f, .65f), Is.True, "An open card stays open until 34°.");
        Assert.That(CandyEnergyWristUI.EvaluateGlance(true, 40f, .4f, 26f, 34f, .65f), Is.False);
        Assert.That(CandyEnergyWristUI.EvaluateGlance(true, 10f, .9f, 26f, 34f, .65f), Is.False, "A wrist held far away never opens the card.");
    }

    [Test]
    public void Texts_AreLocalized()
    {
        Assert.That(CandyEnergyWristUI.GetExitConfirmLabel(false), Is.EqualTo("再点一次退出"));
        Assert.That(CandyEnergyWristUI.GetExitConfirmLabel(true), Is.EqualTo("Tap again to exit"));
        Assert.That(CandyEnergyWristUI.GetNextGiftLabel(1, false), Is.EqualTo("下一件礼物 · 第 2 件"));
        Assert.That(CandyEnergyWristUI.GetNextGiftLabel(4, true), Is.EqualTo("All 4 gifts collected"));
        Assert.That(CandyEnergyWristUI.GetWorldEnergyTitle(true, true), Is.EqualTo("Nature Energy"));
    }
}
