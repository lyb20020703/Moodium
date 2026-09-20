using System.IO;
using NUnit.Framework;

public sealed class MoodiumPortalFlowTests
{
    const string FlowPath = "Assets/Scripts/Moodium/MoodiumAppFlowController.cs";
    const string OpeningPath = "Assets/Scripts/Moodium/Opening/OpeningManager.cs";

    [Test]
    public void RuntimeStartupRoutesThroughPortalAndSkipsCreativeUiConstruction()
    {
        var source = File.ReadAllText(FlowPath);

        StringAssert.Contains("ShowPortalEntrance();", source);
        StringAssert.DoesNotContain("BuildModeSelection();", source);
        StringAssert.DoesNotContain("BuildWorldSelectionPanel();", source);
        StringAssert.DoesNotContain("BuildNoObjectPanel();", source);
    }

    [Test]
    public void OpeningHandsControlToPortalFlow()
    {
        var source = File.ReadAllText(OpeningPath);

        StringAssert.Contains("BeginPortalFlow", source);
    }

    [Test]
    public void RealityBackReturnsToRealityWorldSelection()
    {
        var source = File.ReadAllText(FlowPath);

        StringAssert.Contains("ShowObjectWorldSelection);", source);
    }
}
