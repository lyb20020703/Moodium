using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class MoodiumOpeningPersistenceTests
{
    const string PrefabPath = "Assets/prefab/MoodiumOpening/MoodiumOpening.prefab";
    const string SequenceConfigPath = "Assets/Resources/MoodiumOpening/OpeningVideoSequenceConfig.asset";

    [Test]
    public void OpeningPrefab_KeepsAllRuntimeIntroAssetsAssigned()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);

        var manager = prefab.GetComponent<Moodium.Opening.OpeningManager>();
        Assert.That(manager, Is.Not.Null);
        var serialized = new SerializedObject(manager);

        var sequenceConfig = serialized.FindProperty("m_VideoSequenceConfig").objectReferenceValue;
        Assert.That(sequenceConfig, Is.Not.Null, "The intro sequence configuration must survive editor restarts.");
        Assert.That(AssetDatabase.GetAssetPath(sequenceConfig), Is.EqualTo(SequenceConfigPath));

        var sequence = AssetDatabase.LoadAssetAtPath<Moodium.Opening.OpeningVideoSequenceConfig>(SequenceConfigPath);
        Assert.That(sequence, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(sequence.VideoClip), Is.EqualTo("Assets/Video/OPENANI.mp4"));
        Assert.That(sequence.IntroLoopStartTime, Is.EqualTo(0f));
        Assert.That(sequence.IntroLoopEndTime, Is.EqualTo(3.08f).Within(0.001f));
        Assert.That(sequence.LanguageLoopStartTime, Is.EqualTo(24.14f).Within(0.001f));
        Assert.That(sequence.LanguageLoopEndTime, Is.EqualTo(32f));
        Assert.That(AssetDatabase.GetAssetPath(sequence.VideoClip), Is.EqualTo("Assets/Video/hiimmoodi.mp4"));
        Assert.That(sequence.DistanceFromUser, Is.EqualTo(1.35f).Within(0.001f),
            "The video must use the same eye-relative distance as the portal window.");
        Assert.That(sequence.HeightOffset, Is.EqualTo(-0.05f).Within(0.001f),
            "The video must use the same eye-relative height as the portal window.");
        Assert.That(sequence.WorldPositionY, Is.EqualTo(1f).Within(0.001f),
            "The runtime-created opening video display must use the requested world Pos Y.");

        var previewDuration = serialized.FindProperty("m_InteractiveVideoPreviewDuration");
        Assert.That(previewDuration, Is.Not.Null,
            "The intro video needs a persisted preview duration so editor restarts cannot restore an old loop length.");
        Assert.That(previewDuration.floatValue, Is.EqualTo(3.08f).Within(0.001f));
        Assert.That(serialized.FindProperty("m_LeftHandGuidePrefab").objectReferenceValue,
            Is.TypeOf<GameObject>(), "The left-hand guide prefab must remain assigned.");
        Assert.That(serialized.FindProperty("m_RightHandGuidePrefab").objectReferenceValue,
            Is.TypeOf<GameObject>(), "The right-hand guide prefab must remain assigned.");
        Assert.That(serialized.FindProperty("m_HandPromptFont").objectReferenceValue,
            Is.TypeOf<TMP_FontAsset>(), "The Chinese subtitle font must remain assigned.");
    }
}
