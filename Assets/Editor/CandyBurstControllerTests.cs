using Moodium.CandyWorld;
using NUnit.Framework;

public sealed class CandyBurstControllerTests
{
    [Test]
    public void NatureCandyPoolDiagnostic_UsesCandyPoolOnlyForNatureWorldWhenEnabled()
    {
        Assert.That(CandyBurstController.ShouldUseNaturePrefabPool(true, true), Is.False);
        Assert.That(CandyBurstController.ShouldUseNaturePrefabPool(true, false), Is.True);
        Assert.That(CandyBurstController.ShouldUseNaturePrefabPool(false, true), Is.False);
    }

    [Test]
    public void NatureBurstWrapper_DisablesLegacyMaterialAndBoundsCompatibility()
    {
        Assert.That(CandyBurstController.ShouldApplyNatureBurstCompatibility(true, true, false), Is.False);
        Assert.That(CandyBurstController.ShouldApplyNatureBurstCompatibility(true, false, false), Is.True);
        Assert.That(CandyBurstController.ShouldApplyNatureBurstCompatibility(false, false, false), Is.False);
        Assert.That(CandyBurstController.ShouldApplyNatureBurstCompatibility(true, false, true), Is.False);
    }

    [TestCase("flower", NatureBurstDebugPrefabSelection.Flower, true)]
    [TestCase("Four-leaf clover", NatureBurstDebugPrefabSelection.Clover, true)]
    [TestCase("mushroom", NatureBurstDebugPrefabSelection.Mushroom, true)]
    [TestCase("mushroom", NatureBurstDebugPrefabSelection.Clover, false)]
    [TestCase("flower", NatureBurstDebugPrefabSelection.Random, true)]
    public void PureNatureBurstDebugSelection_OnlyKeepsTheRequestedPrefab(
        string prefabName, NatureBurstDebugPrefabSelection selection, bool expected)
    {
        Assert.That(CandyBurstController.MatchesNatureDebugPrefab(prefabName, selection), Is.EqualTo(expected));
    }

    [TestCase("flower", "Moodium/NatureBurst/Flower")]
    [TestCase("Four-leaf clover", "Moodium/NatureBurst/Clover")]
    [TestCase("mushroom", "Moodium/NatureBurst/Mushroom")]
    public void NatureBurstPrefab_ResolvesItsExplicitVisionOSSafeMaterial(string prefabName, string expectedPath)
    {
        Assert.That(CandyBurstController.GetNatureBurstMaterialResourcePath(prefabName), Is.EqualTo(expectedPath));
    }
}
