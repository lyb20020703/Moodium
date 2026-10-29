using System.Linq;
using Moodium.UI;
using Moodium.CandyWorld;
using Moodium.Calibration;
using Moodium.Opening;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
public sealed class UIRefreshTests
{
    GameObject root;
    [SetUp] public void Setup(){root=new GameObject("UI Refresh Test");}
    [TearDown] public void Cleanup(){Object.DestroyImmediate(root);}
    [Test] public void All51SpritesLoadAtDesignScale()
    {
        var folders=new[]{"Assets/Resources/MoodiumUIRefresh","Assets/Resources/MoodiumOpening"};
        foreach(var pair in new[]{(folders[0],30),(folders[1],21)})
        {
            var paths=System.IO.Directory.GetFiles(pair.Item1,"*.png");Assert.That(paths.Length,Is.EqualTo(pair.Item2));
            foreach(var path in paths){var id=path.Substring("Assets/Resources/".Length);id=id.Substring(0,id.Length-4);var sprite=Resources.Load<Sprite>(id);Assert.That(sprite,Is.Not.Null,id);Assert.That(sprite.pixelsPerUnit,Is.EqualTo(UIRefreshAssetPostprocessor.PixelsPerUnit).Within(.01),id);}
        }
    }
    [Test] public void EnergyBoundariesAndRenderedState()
    {
        Assert.That(MoodiumUIRefreshLayout.EnergyDotPosition(39).y,Is.EqualTo(MoodiumUIRefreshLayout.EnergyDotRadius).Within(.00001));
        var ring=root.AddComponent<FloatingSqueezeProgress>();var soft=new GameObject("Soft");soft.transform.SetParent(root.transform);
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("5d946757aa5b8410aba5b97ed2216bfa"));
        ring.ConfigureFonts(font,font);ring.Bind(soft.transform,null,true);
        foreach(var value in new[]{0f,.37f,.5f,1f})
        {
            ring.SetProgress(value);Assert.That(ring.LitCount,Is.EqualTo(Mathf.FloorToInt(value*40)));
            var dots=root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Energy Dot ")).ToArray();
            Assert.That(dots.Count(r=>r.sprite.name!="energy_dot_off"),Is.EqualTo(ring.LitCount));
            Assert.That(ring.DisplayText,Does.StartWith(Mathf.RoundToInt(value*100).ToString()));
        }
        Assert.That(root.GetComponentInChildren<TextMeshPro>().font,Is.EqualTo(font));
        Assert.That(MoodiumUIRefreshLayout.EnergyHeadIndex(0),Is.EqualTo(-1));Assert.That(MoodiumUIRefreshLayout.EnergyHeadIndex(1),Is.EqualTo(-1));
        for(int i=0;i<4;i++){float threshold=new[]{.2f,.5f,.8f,1f}[i];Assert.That(MoodiumUIRefreshLayout.EnergyGiftReached(i,threshold),Is.True);Assert.That(MoodiumUIRefreshLayout.EnergyGiftReached(i,threshold-.001f),Is.False);}
    }
    [Test] public void SettingsSelectsSemanticValuesAndChangesSprites()
    {
        bool en=false,bag=true;var panel=root.AddComponent<SelectionSettingsVisual>();panel.Initialize(false,true,(e,b)=>{en=e;bag=b;},()=>{});
        panel.Choose(0,1);Assert.That(en,Is.True);panel.Choose(1,1);Assert.That(bag,Is.False);
        Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="Settings Panel").sprite.name,Is.EqualTo("settings_panel_en"));
        Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="Settings Option 0-1").color,Is.EqualTo(MoodiumUIRefreshLayout.Ink));
    }
    [TestCase(2.9f,0f)][TestCase(3.1f,3.1f)][TestCase(-2f,0f)][TestCase(370f,10f)]
    public void RotationSnap(float input,float expected)=>Assert.That(MoodiumUIRefreshLayout.SnapYaw(input),Is.EqualTo(expected).Within(.001));
    [Test] public void RotationFormat(){Assert.That(MoodiumUIRefreshLayout.FormatYaw(32),Is.EqualTo("+32°"));Assert.That(MoodiumUIRefreshLayout.FormatYaw(-12),Is.EqualTo("−12°"));}
    [Test] public void RotationDragUsesExistingCentreAndSnapsBackWithoutResizing()
    {
        var anchor=new GameObject("Anchor");anchor.transform.SetParent(root.transform,false);
        var calibrator=root.AddComponent<SensoryBagCalibrator>();var settings=calibrator.CreateInspectorSettings();
        calibrator.Begin(anchor.transform,SensoryBagCalibrationData.CreateDefault("test",SensoryBagWorldKind.Nature,settings),settings);
        var centre=calibrator.FrameRoot.position;var width=calibrator.CurrentData.Width;
        var start=centre+new Vector3(.2f,.05f,-.1f);
        calibrator.BeginPointerDrag(1,CalibrationHandleKind.Rotate,start);
        calibrator.BeginPointerDrag(2,CalibrationHandleKind.Frame,start);
        calibrator.MovePointerDrag(1,centre+Quaternion.AngleAxis(32,Vector3.up)*(start-centre));
        Assert.That(calibrator.RotationDegrees,Is.EqualTo(32).Within(.001));
        Assert.That(Vector3.Distance(calibrator.FrameRoot.position,centre),Is.LessThan(.0001));
        Assert.That(calibrator.CurrentData.Width,Is.EqualTo(width));
        calibrator.MovePointerDrag(2,start+Vector3.right);
        Assert.That(Vector3.Distance(calibrator.FrameRoot.position,centre),Is.LessThan(.0001));
        calibrator.MovePointerDrag(1,centre+Quaternion.AngleAxis(2,Vector3.up)*(start-centre));
        Assert.That(calibrator.RotationDegrees,Is.Zero.Within(.001));calibrator.EndPointerDrag(1);
    }

    [Test] public void OpeningUsesSpritesAtPhysicalHitSize()
    {
        root.AddComponent<RectTransform>();var choice=root.AddComponent<OpeningVideoHardwareChoice>();choice.Configure(null,null,true);
        var buttons=root.GetComponentsInChildren<OpeningVideoSpatialButton>();Assert.That(buttons.Length,Is.EqualTo(2));
        foreach(var b in buttons){Assert.That(b.GetComponent<SpriteRenderer>().sprite.name,Does.Contain("_en_idle"));Assert.That(b.GetComponent<BoxCollider>().size,Is.EqualTo(new Vector3(.178f,.061f,.01f)));}
    }
}
