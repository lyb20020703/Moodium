using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class MoodiumOpeningPersistenceTests
{
    const string PrefabPath = "Assets/prefab/MoodiumOpening/MoodiumOpening.prefab";

    [Test]
    public void OpeningPrefab_KeepsAllRuntimeIntroAssetsAssigned()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);

        var manager = prefab.GetComponent<Moodium.Opening.OpeningManager>();
        Assert.That(manager, Is.Not.Null);
        var serialized = new SerializedObject(manager);

        var introVideo = serialized.FindProperty("m_PreOpeningVideo").objectReferenceValue;
        Assert.That(introVideo, Is.Not.Null, "The intro video reference must survive editor restarts.");
        Assert.That(AssetDatabase.GetAssetPath(introVideo), Is.EqualTo("Assets/Video/OpenningAni.mp4"));
        var videoHeightOffset = serialized.FindProperty("m_PreOpeningVideoHeightOffset");
        Assert.That(videoHeightOffset, Is.Not.Null,
            "The intro video needs an explicit eye-relative height that matches the portal window.");
        Assert.That(videoHeightOffset.floatValue, Is.EqualTo(0.13f).Within(0.001f));
        Assert.That(serialized.FindProperty("m_LeftHandGuidePrefab").objectReferenceValue,
            Is.TypeOf<GameObject>(), "The left-hand guide prefab must remain assigned.");
        Assert.That(serialized.FindProperty("m_RightHandGuidePrefab").objectReferenceValue,
            Is.TypeOf<GameObject>(), "The right-hand guide prefab must remain assigned.");
        Assert.That(serialized.FindProperty("m_HandPromptFont").objectReferenceValue,
            Is.TypeOf<TMP_FontAsset>(), "The Chinese subtitle font must remain assigned.");
    }
}
