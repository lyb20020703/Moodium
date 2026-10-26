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
        Assert.That(MoodiVoice.AllIds.Length, Is.EqualTo(25));
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
}
