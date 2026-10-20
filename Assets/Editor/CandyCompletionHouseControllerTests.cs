using Moodium.CandyWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class CandyCompletionHouseControllerTests
{
    [Test]
    public void CompletionHouseUsesAnApproximatelyThreeMeterRearRightOffset()
    {
        Assert.That(CandyCompletionHouseController.GetViewerRelativeSpawnOffset(),
            Is.EqualTo(new Vector2(1.2f, -2.75f)));
    }

    [Test]
    public void MainHouseSpawnsAtEightyPercentOnlyOnce()
    {
        Assert.That(CandyCompletionHouseController.ShouldSpawnCandyHouse(.8f, false), Is.True);
        Assert.That(CandyCompletionHouseController.ShouldSpawnCandyHouse(.79f, false), Is.False);
        Assert.That(CandyCompletionHouseController.ShouldSpawnCandyHouse(1f, true), Is.False);
    }

    [Test]
    public void LittleHouseSpawnsAtFullProgressOnlyOnce()
    {
        Assert.That(CandyCompletionHouseController.ShouldSpawnCandyLittleHouse(1f, false), Is.True);
        Assert.That(CandyCompletionHouseController.ShouldSpawnCandyLittleHouse(.99f, false), Is.False);
        Assert.That(CandyCompletionHouseController.ShouldSpawnCandyLittleHouse(1f, true), Is.False);
    }

    [Test]
    public void CompletionHouseUsesThreeTimesTheSourcePrefabScale()
    {
        Assert.That(CandyCompletionHouseController.GetHouseScaleMultiplier(), Is.EqualTo(3f));
    }
}
