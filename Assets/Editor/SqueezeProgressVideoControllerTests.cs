using Moodium.Opening;
using Moodium.Reality;
using NUnit.Framework;
using UnityEngine;

public sealed class SqueezeProgressVideoControllerTests
{
    [TestCase(0f, 12d, 0d)]
    [TestCase(0.25f, 12d, 3d)]
    [TestCase(0.5f, 12d, 6d)]
    [TestCase(1f, 12d, 12d)]
    [TestCase(1.5f, 12d, 12d)]
    public void ProgressToVideoTime_ClampsAndMapsNormalizedProgress(float progress, double duration, double expected)
    {
        Assert.That(SqueezeProgressVideoController.ProgressToVideoTime(progress, duration),
            Is.EqualTo(expected).Within(0.0001d));
    }

    [Test]
    public void HasReachedTarget_UsesSmallToleranceBeforePausing()
    {
        Assert.That(SqueezeProgressVideoController.HasReachedTarget(4.995d, 5d, 0.01d), Is.True);
        Assert.That(SqueezeProgressVideoController.HasReachedTarget(4.8d, 5d, 0.01d), Is.False);
    }

    [Test]
    public void UprightFacingRotation_IgnoresCameraPitchAndKeepsVideoVertical()
    {
        var rotation = OpeningVideoDisplayPlacement.BuildUprightFacingRotation(new Vector3(0.2f, -0.65f, 0.73f));

        Assert.That(Vector3.Angle(rotation * Vector3.up, Vector3.up), Is.LessThan(0.01f));
        Assert.That(Vector3.Dot((rotation * Vector3.forward).normalized,
            new Vector3(0.2f, 0f, 0.73f).normalized), Is.GreaterThan(0.999f));
    }

    [Test]
    public void SqueezeVideoUsesAForegroundRenderQueue()
    {
        Assert.That(SqueezeProgressVideoController.ForegroundRenderQueue, Is.GreaterThan(4000));
    }

    [Test]
    public void SqueezeVideoPlacementPrefersTheLiveMainCameraOverTheCachedCamera()
    {
        var cached = new GameObject("Cached squeeze camera").AddComponent<Camera>();
        var live = new GameObject("Live squeeze camera").AddComponent<Camera>();

        Assert.That(SqueezeProgressVideoController.SelectPlacementCamera(live, cached), Is.SameAs(live));

        Object.DestroyImmediate(cached.gameObject);
        Object.DestroyImmediate(live.gameObject);
    }
}
