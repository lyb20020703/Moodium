using Moodium.NatureWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class BubbleWorldAmbientWindTests
{
    [Test]
    public void WindChangesOverTimeWithoutAccumulatingOrExceedingOneCentimetre()
    {
        var phase = new Vector2(.7f, 1.3f);
        var first = BubbleWorldAmbientWind.Offset(phase, 1, .007f, 0);
        Assert.That(Vector3.Distance(first, BubbleWorldAmbientWind.Offset(phase, 1, .007f, 2)), Is.GreaterThan(.0001f));
        for (var t = 0; t < 1000; t++)
            Assert.That(BubbleWorldAmbientWind.Offset(phase, 1, .007f, t * .13f).magnitude, Is.LessThan(.011f));
        Assert.That(BubbleWorldAmbientWind.Offset(phase, 1, .007f, 0), Is.EqualTo(first));
    }

    [Test]
    public void ZeroStrengthIsStillAndNearbyPointsMoveTogether()
    {
        Assert.That(BubbleWorldAmbientWind.Offset(Vector2.one, 1, 0, 5), Is.EqualTo(Vector3.zero));
        var a = BubbleWorldAmbientWind.Offset(Vector2.one, 1, .007f, 5);
        var b = BubbleWorldAmbientWind.Offset(Vector2.one * 1.001f, 1, .007f, 5);
        Assert.That(Vector3.Distance(a, b), Is.LessThan(.0001f));
    }
}
