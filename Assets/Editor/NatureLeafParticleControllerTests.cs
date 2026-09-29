using Moodium.CandyWorld;
using Moodium.Interaction;
using Moodium.NatureWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class NatureLeafParticleControllerTests
{
    [Test]
    public void Leaves_StartOnlyWhenNatureWorldReachesCompletion()
    {
        Assert.That(NatureLeafParticleController.ShouldEmitLeaves(.99f), Is.False);
        Assert.That(NatureLeafParticleController.ShouldEmitLeaves(1f), Is.True);
    }

    [Test]
    public void EmptyVariantList_IsInitializedWhenLeavesStart()
    {
        Assert.That(NatureLeafParticleController.NeedsVariantInitialization(0), Is.True);
        Assert.That(NatureLeafParticleController.NeedsVariantInitialization(3), Is.False);
    }

    [Test]
    public void Release_IsOfferedOnlyAfterCompletionAndBeforeItHasBeenUsed()
    {
        Assert.That(EnergyReleasePolicy.ShouldOffer(.99f, false), Is.False);
        Assert.That(EnergyReleasePolicy.ShouldOffer(1f, false), Is.True);
        Assert.That(EnergyReleasePolicy.ShouldOffer(1f, true), Is.False);
    }

    [Test]
    public void LeafVariantsSplitTheTotalEmissionBudgetInsteadOfTriplingIt()
    {
        Assert.That(NatureLeafParticleController.GetEmissionRatePerVariant(18f, 3), Is.EqualTo(6f));
        Assert.That(NatureLeafParticleController.GetEmissionRatePerVariant(18f, 0), Is.EqualTo(0f));
    }

    [Test]
    public void EachLeafEffectGetsItsOwnShareOfTheInitialEmission()
    {
        Assert.That(NatureLeafParticleController.GetInitialEmissionCount(16, 3, 0), Is.EqualTo(6));
        Assert.That(NatureLeafParticleController.GetInitialEmissionCount(16, 3, 1), Is.EqualTo(5));
        Assert.That(NatureLeafParticleController.GetInitialEmissionCount(16, 3, 2), Is.EqualTo(5));
    }

    [Test]
    public void NatureLeavesUseOneOriginalLeafSystemRegardlessOfAdditionalTextures()
    {
        Assert.That(NatureLeafParticleController.GetLeafSystemCount(1), Is.EqualTo(1));
        Assert.That(NatureLeafParticleController.GetLeafSystemCount(3), Is.EqualTo(1));
    }

    [Test]
    public void ReleaseRainUsesWorldSpecificScaleMultipliers()
    {
        Assert.That(CandyRainRewardController.ReleaseScaleMultiplier(false), Is.EqualTo(.15f));
        Assert.That(CandyRainRewardController.ReleaseScaleMultiplier(true), Is.EqualTo(.375f));
    }

    [Test]
    public void ReleaseRainKeepsThirtyFiveRewardsAndExpiresGroundedRewardsAfterFiveSeconds()
    {
        Assert.That(CandyRainRewardController.DefaultMaxActiveRewards, Is.EqualTo(35));
        Assert.That(ReleaseRewardLifecycle.ShouldExpireAfterLanding(4.99f, 5f), Is.False);
        Assert.That(ReleaseRewardLifecycle.ShouldExpireAfterLanding(5f, 5f), Is.True);
    }

    [TestCase("Chocolate_Capsule", true)]
    [TestCase("StarPrefab", false)]
    [TestCase("MacaronPrefab", false)]
    public void CandyRelease_OnlyChocolateCapsulesUseTheShellCrackTouchPath(
        string prefabName, bool expected)
    {
        Assert.That(CandyRainRewardController.IsShellCrackReward(prefabName), Is.EqualTo(expected));
    }

    [Test]
    public void SoftSqueezeFeedbackCreatesTheExistingWhiteCircularBurstAtTheSoftCenter()
    {
        var expectedCenter = new Vector3(.24f, 1.15f, -.72f);

        var burst = CandyWorldManager.SpawnSoftSqueezeFeedback(expectedCenter);

        Assert.That(burst, Is.Not.Null);
        Assert.That(burst.name, Is.EqualTo("Moodium White Candy Burst"));
        Assert.That(burst.transform.position, Is.EqualTo(expectedCenter));
        Object.DestroyImmediate(burst);
    }

    [Test]
    public void WhiteBurstCircleTextureHasTransparentCornersAndAnOpaqueCenter()
    {
        var texture = ParticleBurstEffect.CreateRuntimeCircleTexture(16);

        Assert.That(texture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(.001f));
        Assert.That(texture.GetPixel(8, 8).a, Is.GreaterThan(.95f));

        Object.DestroyImmediate(texture);
    }
}
