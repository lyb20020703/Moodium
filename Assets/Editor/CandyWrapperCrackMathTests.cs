using Moodium.Reality;
using NUnit.Framework;
using UnityEngine;

public sealed class CandyWrapperCrackMathTests
{
    [Test]
    public void PalmOrThreeTips_ProducesLargeCluster()
    {
        var large = CandyWrapperCrackMath.IsLargeCluster(
            contactCount: 3,
            hasPalm: false,
            minimumJointCountForLarge: 3);

        Assert.That(large, Is.True);
        Assert.That(CandyWrapperCrackMath.IsLargeCluster(1, true, 3), Is.True);
    }

    [Test]
    public void NearbyRecentSpawn_IsDeduplicated()
    {
        Assert.That(
            CandyWrapperCrackMath.IsDuplicateSpawn(
                new Vector3(.02f, 0f, 0f), Vector3.zero,
                now: .12f, lastSpawnTime: 0f,
                minimumDistance: .06f, minimumTime: .2f),
            Is.True);
    }

    [Test]
    public void ApproachVelocity_UsesOnlyMotionTowardSurface()
    {
        var toward = CandyWrapperCrackMath.TowardSurfaceVelocity(
            new Vector3(0f, 0f, -0.2f), Vector3.forward);
        var parallel = CandyWrapperCrackMath.TowardSurfaceVelocity(
            Vector3.right * .5f, Vector3.forward);

        Assert.That(toward, Is.EqualTo(.2f).Within(.0001f));
        Assert.That(parallel, Is.Zero);
    }
}
