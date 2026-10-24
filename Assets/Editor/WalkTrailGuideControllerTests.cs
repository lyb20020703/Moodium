using Moodium.NatureWorld;
using NUnit.Framework;

public sealed class WalkTrailGuideControllerTests
{
    [Test]
    public void WalkingInvitationWaitsForRewardRainOrTwentyFiveSecondFallback()
    {
        Assert.That(WalkTrailGuideController.ShouldOfferWalk(false, false, 0f), Is.False);
        Assert.That(WalkTrailGuideController.ShouldOfferWalk(false, false, 24f), Is.False);
        Assert.That(WalkTrailGuideController.ShouldOfferWalk(false, false, 25f), Is.True);
        Assert.That(WalkTrailGuideController.ShouldOfferWalk(true, true, 30f), Is.True);
        Assert.That(WalkTrailGuideController.ShouldOfferWalk(true, false, 12f), Is.True);
    }

    [Test]
    public void MelodyClimbsThenLoopsFromFourthNote()
    {
        Assert.That(WalkTrailGuideController.NextNoteIndex(0), Is.EqualTo(1));
        Assert.That(WalkTrailGuideController.NextNoteIndex(6), Is.EqualTo(7));
        Assert.That(WalkTrailGuideController.NextNoteIndex(7), Is.EqualTo(3));
    }
}
