using Moodium.CandyWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class CandyEnergyWristRingTests
{
    [TestCase(0f, 0f)]
    [TestCase(.25f, 90f)]
    [TestCase(.5f, 180f)]
    [TestCase(.75f, 270f)]
    [TestCase(1f, 360f)]
    public void ProgressMapsToTheExpectedRingArc(float progress, float expectedDegrees)
    {
        Assert.That(CandyEnergyWristUI.ProgressToArcDegrees(progress), Is.EqualTo(expectedDegrees));
    }

    [Test]
    public void WorldThemeSelectsTheSpecifiedRingColors()
    {
        Assert.That(CandyEnergyWristUI.GetProgressRingColor(false),
            Is.EqualTo(new Color(1f, 91f / 255f, 157f / 255f, 1f)));
        Assert.That(CandyEnergyWristUI.GetProgressRingColor(true),
            Is.EqualTo(new Color(141f / 255f, 220f / 255f, 135f / 255f, 1f)));
    }

    [Test]
    public void OverlayAnchorSitsAboveTheOuterEdgeOfTheWristRing()
    {
        var canvasUnits = CandyEnergyWristUI.CalculateOverlayVerticalAnchor(
            ringRadius: .055f,
            ringThickness: .010f,
            clearance: .025f,
            canvasScale: .00023f);

        Assert.That(canvasUnits, Is.EqualTo(.085f / .00023f).Within(.01f));
    }
}
