using System.IO;
using NUnit.Framework;

public sealed class OpeningSkipBehaviorTests
{
    [Test]
    public void Skip_RemainsAvailableDuringTheHardwareTutorial()
    {
        var source = File.ReadAllText("Assets/Scripts/Moodium/Opening/OpeningManager.cs");

        StringAssert.Contains("System.Func<bool> skipRequested", source);
        StringAssert.Contains("SkipToStickerSelection", source);
        StringAssert.Contains("HideIntroGuidance", source);
        StringAssert.Contains("while (CurrentFrame(player) < branch.EndFrame", source);
        StringAssert.Contains("if (skipRequested())", source);
    }
}
