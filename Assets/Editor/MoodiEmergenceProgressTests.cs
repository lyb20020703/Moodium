using Moodium.Reality;
using NUnit.Framework;
using UnityEngine;

public sealed class MoodiEmergenceProgressTests
{
    [Test]
    public void Progress_MapsLinearlyAcrossTheWholeAnimationClip()
    {
        const float clipLength = 4f;

        Assert.That(MoodiEmergenceProgress.ToAnimationTime(0f, clipLength), Is.EqualTo(0f));
        Assert.That(MoodiEmergenceProgress.ToAnimationTime(.25f, clipLength), Is.EqualTo(1f));
        Assert.That(MoodiEmergenceProgress.ToAnimationTime(.5f, clipLength), Is.EqualTo(2f));
        Assert.That(MoodiEmergenceProgress.ToAnimationTime(1f, clipLength), Is.EqualTo(4f));
    }

    [Test]
    public void Visibility_StaysHiddenAtZeroAndAppearsAfterProgressBegins()
    {
        Assert.That(MoodiEmergenceProgress.ShouldBeVisible(0f), Is.False);
        Assert.That(MoodiEmergenceProgress.ShouldBeVisible(.001f), Is.True);
    }

    [Test]
    public void Emergence_UsesTheVisualPrefabOrientation_NotTheTrackingAnchor()
    {
        var visualOrientation = Quaternion.identity;

        Assert.That(
            Quaternion.Angle(MoodiEmergenceProgress.ResolveVisualOrientation(visualOrientation), Quaternion.identity),
            Is.LessThan(.01f));
    }

    [Test]
    public void Emergence_UsesTheApprovedNegativeNinetyDegreeLocalXRotation()
    {
        Assert.That(
            Quaternion.Angle(
                MoodiEmergenceProgress.AuthoredLocalRotation,
                Quaternion.Euler(-90f, 0f, 0f)),
            Is.LessThan(.01f));
    }

    [Test]
    public void PreviewLoop_AdvancesFromHiddenToFullyEmergedBeforeRestarting()
    {
        Assert.That(MoodiEmergencePreviewController.AutoPreviewProgress(0f, 8f), Is.EqualTo(0f));
        Assert.That(MoodiEmergencePreviewController.AutoPreviewProgress(2f, 8f), Is.EqualTo(.25f));
        Assert.That(MoodiEmergencePreviewController.AutoPreviewProgress(8f, 8f), Is.EqualTo(0f));
    }
}
