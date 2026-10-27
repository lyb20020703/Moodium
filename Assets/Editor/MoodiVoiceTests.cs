using Moodium.Characters;
using NUnit.Framework;
using UnityEngine;

/// <summary>Moodi voice: every line has a zh and an en clip, and teaching lines play only once.</summary>
public sealed class MoodiVoiceTests
{
    GameObject m_Host;
    string m_PreviousLanguage;

    [SetUp]
    public void SetUp()
    {
        m_PreviousLanguage = PlayerPrefs.GetString("Moodium.Language", "Chinese");
        MoodiVoice.ResetSession();
    }

    [TearDown]
    public void TearDown()
    {
        if (m_Host != null) Object.DestroyImmediate(m_Host);
        PlayerPrefs.SetString("Moodium.Language", m_PreviousLanguage);
        MoodiVoice.ResetSession();
    }

    [Test]
    public void EveryLineHasAChineseAndAnEnglishClip()
    {
        Assert.That(MoodiVoice.AllIds.Length, Is.EqualTo(28));
        foreach (var language in new[] { "zh", "en" })
        foreach (var id in MoodiVoice.AllIds)
        {
            var path = MoodiVoice.ClipResourcePath(id, language);
            var clip = Resources.Load<AudioClip>(path);
            Assert.That(clip, Is.Not.Null, path);
            Assert.That(clip.length, Is.InRange(.4f, 5f), path);
            Assert.That(clip.channels, Is.EqualTo(1), path + ": reimport the folder");
        }
    }

    [Test]
    public void LanguageFollowsTheAppSetting()
    {
        PlayerPrefs.SetString("Moodium.Language", "English");
        Assert.That(MoodiVoice.LanguageCode, Is.EqualTo("en"));
        PlayerPrefs.SetString("Moodium.Language", "Chinese");
        Assert.That(MoodiVoice.LanguageCode, Is.EqualTo("zh"));
    }

    [Test]
    public void TeachingLinesAreSaidOnlyOnceAndNotAfterTheUserLearned()
    {
        m_Host = new GameObject("Moodi");
        var voice = m_Host.AddComponent<MoodiVoice>();
        Assert.That(MoodiVoice.IsTeaching("A2"), Is.True);
        Assert.That(MoodiVoice.IsTeaching("B2"), Is.False);
        Assert.That(voice.Say("A2"), Is.True);
        voice.Stop();
        Assert.That(voice.Say("A2"), Is.False, "teaching line repeats");
        MoodiVoice.MarkLearned("C1");
        Assert.That(voice.Say("C1"), Is.False, "learned teaching line still plays");
    }

    [Test]
    public void StickerVoiceUsesSessionLanguageInsteadOfPlayerPreferences()
    {
        m_Host=new GameObject("Sticker Moodi");
        var voice=m_Host.AddComponent<MoodiVoice>();
        PlayerPrefs.SetString("Moodium.Language","Chinese");
        voice.EnglishOverride=true;
        var load=typeof(MoodiVoice).GetMethod("Load",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(((AudioClip)load.Invoke(voice,new object[]{"S1c"})).name,Is.EqualTo("moodi_vo_S1c_en"));
        voice.EnglishOverride=false;
        Assert.That(((AudioClip)load.Invoke(voice,new object[]{"S1n"})).name,Is.EqualTo("moodi_vo_S1n_zh"));
    }

    [Test]
    public void HidingOrStoppingMoodiCancelsItsDelayedInvitation()
    {
        m_Host=new GameObject("Sticker Moodi");var voice=m_Host.AddComponent<MoodiVoice>();
        typeof(MoodiVoice).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(voice,null);
        voice.SayAfter("S1c",0f);m_Host.SetActive(false);
        // Ordinary MonoBehaviours do not receive OnDisable from EditMode tests.
        typeof(MoodiVoice).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(voice,null);
        m_Host.SetActive(true);
        var update=typeof(MoodiVoice).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        update.Invoke(voice,null);
        Assert.That(m_Host.GetComponent<AudioSource>().clip,Is.Null,"Hidden entry must discard the old invitation.");
        voice.SayAfter("S1n",0f);voice.Stop();update.Invoke(voice,null);
        Assert.That(m_Host.GetComponent<AudioSource>().clip,Is.Null,"Stop must cancel delayed speech too.");
    }
}
