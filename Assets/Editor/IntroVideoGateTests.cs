using Moodium.Opening;
using NUnit.Framework;

public sealed class IntroVideoGateTests
{
    [Test]
    public void Gate_LoopsPreviewUntilVideoIsExplicitlyActivated()
    {
        var gate = new IntroVideoGate(3d);

        Assert.That(gate.ShouldLoopAt(3d), Is.True);
        Assert.That(gate.ShouldLoopAt(4.2d), Is.True);
    }

    [Test]
    public void Gate_ContinuesFromPreviewEndAfterActivation()
    {
        var gate = new IntroVideoGate(3d);

        gate.Activate();

        Assert.That(gate.ShouldLoopAt(3d), Is.False);
        Assert.That(gate.ResumeTime, Is.EqualTo(3d));
    }

    [Test]
    public void Gate_BeginsOnlyOnePreviewSeekUntilThatSeekCompletes()
    {
        var gate = new IntroVideoGate(2d);

        Assert.That(gate.TryBeginPreviewLoop(2d), Is.True);
        Assert.That(gate.TryBeginPreviewLoop(2d), Is.False);

        Assert.That(gate.TryCompletePreviewSeek(2d), Is.False);
        Assert.That(gate.TryCompletePreviewSeek(0.05d), Is.True);

        Assert.That(gate.TryBeginPreviewLoop(2d), Is.True);
    }

    [Test]
    public void HandGuideLayout_PlacesHandsSymmetricallyOnVideoSides()
    {
        var layout = new IntroVideoHandGuideLayout();

        Assert.That(layout.LeftLocalPosition.x, Is.LessThan(0f));
        Assert.That(layout.RightLocalPosition.x, Is.GreaterThan(0f));
        Assert.That(layout.LeftLocalPosition.x, Is.EqualTo(-layout.RightLocalPosition.x).Within(0.001f));
        Assert.That(layout.LeftLocalPosition.y, Is.EqualTo(layout.RightLocalPosition.y).Within(0.001f));
    }
}
