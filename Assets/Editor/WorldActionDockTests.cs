using System.IO;
using Moodium.CandyWorld;
using Moodium.Flow;
using NUnit.Framework;
using UnityEngine;

public sealed class WorldActionDockTests
{
    static readonly Vector3 Eye = new(0f, 1.5f, 0f);

    // ---------- placement ----------

    [Test]
    public void SitsLowInFrontAndWithinReach()
    {
        var p = WorldActionDock.TargetPosition(Eye, Vector3.forward);
        Assert.That(Vector3.ProjectOnPlane(p - Eye, Vector3.up).magnitude, Is.EqualTo(WorldActionDock.Distance).Within(1e-4f));
        Assert.That(Eye.y - p.y, Is.EqualTo(WorldActionDock.Drop).Within(1e-4f));
        var below = Mathf.Atan2(WorldActionDock.Drop, WorldActionDock.Distance) * Mathf.Rad2Deg;
        Assert.That(below, Is.InRange(20f, 32f), "visible low in the view, not in its centre");
        Assert.That(Vector3.Distance(p, Eye), Is.InRange(.45f, .62f), "a finger can reach it without stepping forward");
    }

    [Test]
    public void LookingDownOrUpDoesNotMoveTheTarget()
    {
        var level = WorldActionDock.TargetPosition(Eye, Vector3.forward);
        var down = WorldActionDock.TargetPosition(Eye, Quaternion.Euler(40f, 0f, 0f) * Vector3.forward);
        var up = WorldActionDock.TargetPosition(Eye, Quaternion.Euler(-30f, 0f, 0f) * Vector3.forward);
        Assert.That(Vector3.Distance(level, down), Is.LessThan(1e-4f));
        Assert.That(Vector3.Distance(level, up), Is.LessThan(1e-4f));
    }

    [Test]
    public void FacesTheEyes()
    {
        var p = WorldActionDock.TargetPosition(Eye, Vector3.right);
        var r = WorldActionDock.FaceEye(p, Eye);
        Assert.That(Vector3.Dot(r * Vector3.forward, (p - Eye).normalized), Is.GreaterThan(.999f), "+Z points away from the viewer");
        Assert.That((r * Vector3.up).y, Is.GreaterThan(.8f), "upright, tilted back towards the eyes");
    }

    // ---------- lazy follow ----------

    [Test]
    public void FollowsOnlyAfterARealTurnOrMove()
    {
        Assert.That(WorldActionDock.ShouldRecenter(30f, 5f, Eye, Eye), Is.False, "a glance to the side");
        Assert.That(WorldActionDock.ShouldRecenter(40f, .2f, Eye, Eye), Is.False, "too short");
        Assert.That(WorldActionDock.ShouldRecenter(40f, .45f, Eye, Eye), Is.True);
        Assert.That(WorldActionDock.ShouldRecenter(0f, 0f, Eye + new Vector3(.35f, 0, 0), Eye), Is.True, "walked");
        Assert.That(WorldActionDock.ShouldRecenter(0f, 0f, Eye + new Vector3(0, -.2f, 0), Eye), Is.True, "sat down");
        Assert.That(WorldActionDock.ShouldRecenter(0f, 0f, Eye + new Vector3(.1f, .05f, 0), Eye), Is.False);
    }

    [Test]
    public void OffAxisIsMeasuredFlat()
    {
        var dock = WorldActionDock.TargetPosition(Eye, Vector3.forward);
        Assert.That(WorldActionDock.OffAxisDegrees(Eye, Quaternion.Euler(50f, 0f, 0f) * Vector3.forward, dock), Is.LessThan(.01f), "looking down at it");
        Assert.That(WorldActionDock.OffAxisDegrees(Eye, Quaternion.Euler(0f, 60f, 0f) * Vector3.forward, dock), Is.EqualTo(60f).Within(.01f));
    }

    [Test]
    public void NeverSlidesAwayFromAFinger()
    {
        Assert.That(WorldActionDock.FollowHeld(.08f, 10f), Is.True, "fingertip near");
        Assert.That(WorldActionDock.FollowHeld(.5f, .3f), Is.True, "just pressed");
        Assert.That(WorldActionDock.FollowHeld(.5f, 2f), Is.False);
        Assert.That(WorldActionDock.FollowHeld(float.PositiveInfinity, 2f), Is.False, "hands not tracked");
    }

    // ---------- input ----------

    [Test]
    public void PressesWaitUntilTheDockHasSettled()
    {
        Assert.That(WorldActionDock.CanPress(true, 1f, 1f, false, 1f), Is.True);
        Assert.That(WorldActionDock.CanPress(false, 1f, 1f, false, 1f), Is.False, "hidden");
        Assert.That(WorldActionDock.CanPress(true, .2f, 1f, false, 1f), Is.False, "just appeared");
        Assert.That(WorldActionDock.CanPress(true, 1f, 1f, true, 1f), Is.False, "gliding");
        Assert.That(WorldActionDock.CanPress(true, 1f, .1f, false, 1f), Is.False, "just arrived");
        Assert.That(WorldActionDock.CanPress(true, 1f, 1f, false, .2f), Is.False, "double press");
    }

    [Test]
    public void ButtonsAndTheirHitAreasDoNotOverlap()
    {
        var gap = WorldActionDock.ButtonX(true) - WorldActionDock.ButtonX(false) - (WorldActionDock.ButtonWidth + 2 * WorldActionDock.HitPad);
        Assert.That(gap, Is.GreaterThan(.005f));
        Assert.That(WorldActionDock.ButtonX(false), Is.LessThan(0f), "the shower on the left");
        Assert.That(WorldActionDock.ButtonHeight + 2 * WorldActionDock.HitPad, Is.GreaterThanOrEqualTo(.045f), "big enough for a fingertip");
    }

    // ---------- states and art ----------

    [Test]
    public void ShowerStates()
    {
        Assert.That(WorldActionDock.ShowerState(false, true), Is.EqualTo(WorldActionDock.ShowerLook.Locked));
        Assert.That(WorldActionDock.ShowerState(true, false), Is.EqualTo(WorldActionDock.ShowerLook.Ready));
        Assert.That(WorldActionDock.ShowerState(true, true), Is.EqualTo(WorldActionDock.ShowerLook.On));
        Assert.That(WorldActionDock.ShowerSprite(WorldActionDock.ShowerLook.Locked, true, false, "idle"), Is.EqualTo("MoodiumWorldDock/dock_shower_locked_nature_zh"));
        Assert.That(WorldActionDock.ShowerSprite(WorldActionDock.ShowerLook.Locked, false, true, ""), Is.EqualTo("MoodiumWorldDock/dock_shower_locked_candy_en"));
        Assert.That(WorldActionDock.ToastSprite(true, false, false), Is.EqualTo("MoodiumWorldDock/dock_toast_ready_candy_zh"));
        Assert.That(WorldActionDock.ToastSprite(false, true, true), Is.EqualTo("MoodiumWorldDock/dock_toast_locked_en"), "the keep-squeezing toast is shared");
        Assert.That(WorldActionDock.ShowerSprite(WorldActionDock.ShowerLook.Ready, true, true, "gaze"), Is.EqualTo("MoodiumWorldDock/dock_shower_nature_en_gaze"));
        Assert.That(WorldActionDock.ShowerSprite(WorldActionDock.ShowerLook.On, false, false, "pressed"), Is.EqualTo("MoodiumWorldDock/dock_stop_candy_zh_pressed"));
        Assert.That(WorldActionDock.ExitSprite(true, false, "idle"), Is.EqualTo("MoodiumWorldDock/dock_exit_confirm_zh_idle"));
    }

    [Test]
    public void AllButtonSpritesImportAtTheRightSize()
    {
        int count = 0;
        foreach (var en in new[] { false, true })
        {
            foreach (var nature in new[] { false, true })
            {
                AssertButton(WorldActionDock.ShowerSprite(WorldActionDock.ShowerLook.Locked, nature, en, "")); count++;
                Assert.That(Resources.Load<Sprite>(WorldActionDock.ToastSprite(true, nature, en)), Is.Not.Null);
            }
            foreach (var state in new[] { "idle", "gaze", "pressed" })
            {
                foreach (var nature in new[] { false, true })
                {
                    AssertButton(WorldActionDock.ShowerSprite(WorldActionDock.ShowerLook.Ready, nature, en, state));
                    AssertButton(WorldActionDock.ShowerSprite(WorldActionDock.ShowerLook.On, nature, en, state));
                    count += 2;
                }
                AssertButton(WorldActionDock.ExitSprite(false, en, state));
                AssertButton(WorldActionDock.ExitSprite(true, en, state));
                count += 2;
            }
            Assert.That(Resources.Load<Sprite>(WorldActionDock.ToastSprite(false, false, en)), Is.Not.Null);
        }
        Assert.That(count, Is.EqualTo(40));
        var line = Resources.Load<Sprite>(WorldActionDock.Folder + "dock_progress");
        Assert.That(line, Is.Not.Null);
        Assert.That(line.border.x, Is.EqualTo(30f).Within(.5f), "9-sliced");
        Assert.That(line.bounds.size.y, Is.EqualTo(WorldActionDock.ProgressHeight).Within(.0002f));
    }

    static void AssertButton(string id)
    {
        var sprite = Resources.Load<Sprite>(id);
        Assert.That(sprite, Is.Not.Null, id);
        // 1140 x 648 px at 7800 px/m; the capsule inside is 0.100 x 0.037 m.
        Assert.That(sprite.bounds.size.x, Is.EqualTo(1140f / 7800f).Within(.002f), id);
        Assert.That(sprite.bounds.size.y, Is.EqualTo(648f / 7800f).Within(.002f), id);
    }

    // ---------- wiring ----------

    [Test]
    public void TheWorldUsesTheDockInsteadOfTheWristBand()
    {
        var manager = File.ReadAllText("Assets/Scripts/Moodium/CandyWorldManager.cs");
        Assert.That(manager, Does.Contain("AddComponent<WorldActionDock>()"));
        Assert.That(manager, Does.Not.Contain("AddComponent<CandyEnergyWristUI>()"), "no wrist band any more");
        Assert.That(manager, Does.Contain("m_Dock.ReleasePressed += ReleaseAccumulatedEnergy"));
        Assert.That(manager, Does.Contain("m_Dock.ExitPressed += RequestExit"));
        var guide = File.ReadAllText("Assets/Scripts/Moodium/WalkTrailGuideController.cs");
        Assert.That(guide, Does.Contain("GetComponent<WorldActionDock>()"));
    }

    [Test]
    public void DockIsHiddenInsideBubbleWorlds()
    {
        Assert.That(CandyWorldManager.WristHiddenFor(true, WorldJourneyStage.Bubble), Is.True);
        Assert.That(CandyWorldManager.WristHiddenFor(true, WorldJourneyStage.Explore), Is.False);
    }

    [Test]
    public void BuildCreatesTwoPressableCapsules()
    {
        var camera = new GameObject("Camera").AddComponent<Camera>();
        var go = new GameObject("Dock");
        try
        {
            var dock = go.AddComponent<WorldActionDock>();
            dock.Build(camera, false);
            var buttons = go.GetComponentsInChildren<Moodium.Flow.MoodiumSpatialButton>(true);
            Assert.That(buttons.Length, Is.EqualTo(2));
            foreach (var b in buttons)
            {
                var box = b.GetComponent<BoxCollider>();
                Assert.That(box, Is.Not.Null, "pinch and poke both need a collider on the button");
                Assert.That(box.size.z, Is.GreaterThanOrEqualTo(.02f), "deep enough for a poking finger");
                Assert.That(b.GetComponentInChildren<SpriteRenderer>(true), Is.Null, "glass sprite is a sibling, so the system highlight stays capsule-shaped");
            }
            Assert.That(go.GetComponent<Unity.PolySpatial.VisionOSSortingGroup>(), Is.Not.Null);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(camera.gameObject); }
    }
}

public sealed class WorldDockNamingTests
{
    [Test]
    public void OldSkyShowerArtIsGone()
    {
        foreach (var old in new[] { "dock_shower_locked_zh", "dock_shower_locked_en", "dock_toast_ready_zh", "dock_toast_ready_en" })
            Assert.That(Resources.Load<Sprite>(WorldActionDock.Folder + old), Is.Null, old + " should have been deleted");
    }
}
