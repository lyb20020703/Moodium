using Moodium.Interaction;
using NUnit.Framework;
using UnityEngine;

public sealed class CompletionHouseSoftInteractionTests
{
    [Test]
    public void FullPressSquashesVerticalAxisAndGentlyExpandsHorizontalAxes()
    {
        var result = CompletionHouseSoftInteraction.CalculatePressedScale(Vector3.one, 1f, .08f, .035f);

        Assert.That(result, Is.EqualTo(new Vector3(1.035f, .92f, 1.035f)));
    }

    [Test]
    public void BottomLockOffsetsTheHouseByTheLostVerticalHeight()
    {
        Assert.That(CompletionHouseSoftInteraction.CalculateBottomLockOffset(2f, .92f),
            Is.EqualTo(.16f).Within(.0001f));
    }
}
