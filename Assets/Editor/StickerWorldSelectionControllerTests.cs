using Moodium.Flow;
using Moodium.Opening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.ARSubsystems;

public sealed class StickerWorldSelectionControllerTests
{
    static readonly TrackableId CandyId = new(1, 2);
    static readonly TrackableId NatureId = new(3, 4);
    GameObject m_Root;
    StickerWorldSelectionController m_Controller;
    MoodiumWorldDefinition m_CandyWorld;
    MoodiumWorldDefinition m_NatureWorld;

    [SetUp]
    public void SetUp()
    {
        m_Root = new GameObject("Sticker World Selection Test Root");
        m_Controller = m_Root.AddComponent<StickerWorldSelectionController>();
        m_CandyWorld = ScriptableObject.CreateInstance<MoodiumWorldDefinition>();
        m_NatureWorld = ScriptableObject.CreateInstance<MoodiumWorldDefinition>();
        m_Controller.ConfigureForTests(
            new GameObject("Candy UI Prefab"),
            new GameObject("Nature UI Prefab"),
            m_CandyWorld,
            m_NatureWorld);
        // The mapping tests do not need the runtime instruction text, whose TMP outline
        // material is intentionally created only in the player-facing display hierarchy.
        m_Controller.Begin(new OpeningVideoSession(m_Root.AddComponent<VideoPlayer>(), null, null, false, () => { }));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(m_CandyWorld);
        Object.DestroyImmediate(m_NatureWorld);
        Object.DestroyImmediate(m_Root);
    }

    [Test]
    public void TrackedCandyAndNatureImages_CreateIndependentMatchingEntries()
    {
        var candyAnchor = new GameObject("Candy Anchor").transform;
        var natureAnchor = new GameObject("Nature Anchor").transform;
        candyAnchor.SetParent(m_Root.transform);
        natureAnchor.SetParent(m_Root.transform);

        m_Controller.HandleTrackedImageUpdate(CandyId, "CandyWorldPhoneTracking", candyAnchor, TrackingState.Tracking);
        m_Controller.HandleTrackedImageUpdate(NatureId, "NatureWorldPhoneTracking", natureAnchor, TrackingState.Tracking);

        Assert.That(m_Controller.ActiveEntryCount, Is.EqualTo(2));
        Assert.That(m_Controller.GetEntry(CandyId).World, Is.SameAs(m_CandyWorld));
        Assert.That(m_Controller.GetEntry(NatureId).World, Is.SameAs(m_NatureWorld));
    }

    [Test]
    public void VisionOSUuidSuffixedReferenceImage_CreatesMatchingNatureEntry()
    {
        var anchor = new GameObject("Nature Anchor").transform;
        anchor.SetParent(m_Root.transform);

        m_Controller.HandleTrackedImageUpdate(
            NatureId,
            "NatureWorldPhoneTracking_8FEF101E-8126-4652-B006-F4793992AFA9",
            anchor,
            TrackingState.Tracking);

        Assert.That(m_Controller.ActiveEntryCount, Is.EqualTo(1));
        Assert.That(m_Controller.GetEntry(NatureId).World, Is.SameAs(m_NatureWorld));
    }

    [Test]
    public void TrackingLoss_HidesOnlyAffectedEntry_AndRecoveryReusesIt()
    {
        var candyAnchor = new GameObject("Candy Anchor").transform;
        var natureAnchor = new GameObject("Nature Anchor").transform;
        candyAnchor.SetParent(m_Root.transform);
        natureAnchor.SetParent(m_Root.transform);
        m_Controller.HandleTrackedImageUpdate(CandyId, "CandyWorldPhoneTracking", candyAnchor, TrackingState.Tracking);
        m_Controller.HandleTrackedImageUpdate(NatureId, "NatureWorldPhoneTracking", natureAnchor, TrackingState.Tracking);
        var originalCandyRoot = m_Controller.GetEntry(CandyId).Root;

        m_Controller.HandleTrackedImageUpdate(CandyId, "CandyWorldPhoneTracking", candyAnchor, TrackingState.None);

        Assert.That(originalCandyRoot.activeSelf, Is.False);
        Assert.That(m_Controller.GetEntry(NatureId).Root.activeSelf, Is.True);

        m_Controller.HandleTrackedImageUpdate(CandyId, "CandyWorldPhoneTracking", candyAnchor, TrackingState.Tracking);

        Assert.That(m_Controller.ActiveEntryCount, Is.EqualTo(2));
        Assert.That(m_Controller.GetEntry(CandyId).Root, Is.SameAs(originalCandyRoot));
        Assert.That(originalCandyRoot.activeSelf, Is.True);
    }

    [Test]
    public void UnsupportedReferenceImage_DoesNotCreateEntry()
    {
        var anchor = new GameObject("Unsupported Anchor").transform;
        anchor.SetParent(m_Root.transform);

        m_Controller.HandleTrackedImageUpdate(new TrackableId(7, 8), "NotMoodium", anchor, TrackingState.Tracking);

        Assert.That(m_Controller.ActiveEntryCount, Is.EqualTo(0));
    }

    [Test]
    public void CompensatedTextScale_CancelsNonUniformParentScale()
    {
        var result = StickerWorldSelectionController.GetCompensatedLocalScale(
            .045f, new Vector3(1.28f, .72f, 1f));

        Assert.That(result, Is.EqualTo(new Vector3(.045f / 1.28f, .045f / .72f, .045f)));
    }

    [Test]
    public void DefaultEntryOffset_IsRaisedAndRecessedFromSticker()
    {
        Assert.That(m_Controller.EntryLocalPosition.y, Is.GreaterThanOrEqualTo(.12f));
        Assert.That(m_Controller.EntryLocalPosition.z, Is.GreaterThanOrEqualTo(.10f));
    }

    [Test]
    public void UprightEntryRotation_KeepsWorldUp_WhenStickerIsVertical()
    {
        var rotation = StickerWorldEntryUprightFollower.CalculateUprightRotation(
            Vector3.zero, new Vector3(1f, 1f, 2f));

        Assert.That(Vector3.Angle(rotation * Vector3.up, Vector3.up), Is.LessThan(0.01f));
        Assert.That(Vector3.Dot(rotation * Vector3.forward, Vector3.up), Is.EqualTo(0f).Within(0.001f));
        Assert.That(Vector3.Dot(rotation * Vector3.back, new Vector3(1f, 0f, 2f).normalized),
            Is.GreaterThan(0.99f));
    }
}
