using Moodium.NatureWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class BubbleWorldAmbientWindTests
{
    [Test]
    public void HandInfluenceIsLocalToDirectionAndIndependentOfSceneDistance()
    {
        Assert.That(BubbleWorldAmbientWind.HandInfluence(Vector3.forward * 20, Vector3.forward), Is.EqualTo(1));
        Assert.That(BubbleWorldAmbientWind.HandInfluence(Vector3.back, Vector3.forward), Is.Zero);
        Assert.That(BubbleWorldAmbientWind.HandInfluence(Vector3.right, Vector3.forward), Is.Zero);
        Assert.That(BubbleWorldAmbientWind.HandInfluence(Vector3.zero, Vector3.forward), Is.Zero);
        var near = BubbleWorldAmbientWind.HandInfluence(new Vector3(.2f, 0, 1), Vector3.forward);
        var far = BubbleWorldAmbientWind.HandInfluence(new Vector3(.5f, 0, 1), Vector3.forward);
        Assert.That(near, Is.GreaterThan(far));
    }

    [Test]
    public void SkinWeightsAreNormalizedAndContinuousAcrossPanoramaSeam()
    {
        for (var y = -5; y <= 8; y++)
        for (var angle = 0; angle < 360; angle++)
        {
            var r = angle * Mathf.Deg2Rad;
            var w = BubbleWorldAmbientWind.Weights(new Vector3(Mathf.Cos(r), y, Mathf.Sin(r)));
            Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1).Within(.00001));
            Assert.That(w.weight0, Is.GreaterThanOrEqualTo(0));
            Assert.That(w.weight1, Is.GreaterThanOrEqualTo(0));
            Assert.That(w.weight2, Is.GreaterThanOrEqualTo(0));
            Assert.That(w.weight3, Is.GreaterThanOrEqualTo(0));
            Assert.That(w.boneIndex3, Is.InRange(0, 35));
        }
        var a = BubbleWorldAmbientWind.Weights(new Vector3(1, -1.5f, .000001f));
        var b = BubbleWorldAmbientWind.Weights(new Vector3(1, -1.5f, -.000001f));
        Assert.That(a.boneIndex0, Is.EqualTo(b.boneIndex1));
        Assert.That(a.weight0, Is.EqualTo(b.weight1).Within(.00001));
    }

    [Test]
    public void SkinMovesActualSplatAndRestoresBakedRenderer()
    {
        var go = new GameObject("wind test");
        var source = new Mesh();
        var baked = new Mesh();
        try
        {
            source.vertices = new[] { new Vector3(1, 0, 0), new Vector3(1.02f, 0, 0),
                new Vector3(1.02f, .02f, 0), new Vector3(1, .02f, 0) };
            source.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            go.AddComponent<MeshFilter>().sharedMesh = source;
            var original = go.AddComponent<MeshRenderer>();
            var wind = go.AddComponent<BubbleWorldAmbientWind>();
            wind.Configure(0, false, Quaternion.Euler(0, 0, 60));
            wind.SetAnimating(true);
            var skin = go.GetComponentInChildren<SkinnedMeshRenderer>();
            foreach (var bone in skin.bones) bone.localPosition = new Vector3(.03f, 0, 0);
            skin.BakeMesh(baked);
            for (var i = 0; i < 4; i++)
                Assert.That(Vector3.Distance(baked.vertices[i], source.vertices[i] + new Vector3(.03f, 0, 0)), Is.LessThan(.00001));
            Assert.That(original.enabled, Is.False);
            Assert.That(source.boneWeights.Length, Is.Zero);
            wind.SetAnimating(false);
            Assert.That(original.enabled, Is.True);
            Assert.That(skin.enabled, Is.False);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(source); Object.DestroyImmediate(baked); }
    }

    [Test]
    public void WindChangesOverTimeWithoutAccumulatingOrExceedingOneCentimetre()
    {
        var phase = new Vector2(.7f, 1.3f);
        var first = BubbleWorldAmbientWind.Offset(phase, 1, .007f, 0);
        Assert.That(Vector3.Distance(first, BubbleWorldAmbientWind.Offset(phase, 1, .007f, 2)), Is.GreaterThan(.0001f));
        for (var t = 0; t < 1000; t++)
            Assert.That(BubbleWorldAmbientWind.Offset(phase, 1, .007f, t * .13f).magnitude, Is.LessThan(.011f));
        Assert.That(BubbleWorldAmbientWind.Offset(phase, 1, .007f, 0), Is.EqualTo(first));
    }

    [Test]
    public void ZeroStrengthIsStillAndNearbyPointsMoveTogether()
    {
        Assert.That(BubbleWorldAmbientWind.Offset(Vector2.one, 1, 0, 5), Is.EqualTo(Vector3.zero));
        var a = BubbleWorldAmbientWind.Offset(Vector2.one, 1, .007f, 5);
        var b = BubbleWorldAmbientWind.Offset(Vector2.one * 1.001f, 1, .007f, 5);
        Assert.That(Vector3.Distance(a, b), Is.LessThan(.0001f));
    }
}
