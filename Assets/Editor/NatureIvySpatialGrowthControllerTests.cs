using Artngame.TreeGEN.ProceduralIvy;
using Moodium.NatureWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class NatureIvySpatialGrowthControllerTests
{
    GameObject m_Root;
    GameObject m_GeneratorObject;
    GameObject m_Surface;

    [SetUp]
    public void SetUp()
    {
        m_Root = new GameObject("Nature Ivy Test Root");
        m_GeneratorObject = new GameObject("Ivy Generator");
        m_GeneratorObject.transform.SetParent(m_Root.transform);
        m_GeneratorObject.AddComponent<MeshFilter>();
        m_GeneratorObject.AddComponent<MeshRenderer>();
        var generator = m_GeneratorObject.AddComponent<IvyGeneratorTREANT>();
        generator.branchCount = 1;
        generator.maxBranchPositions = 3;
        generator.branchPartLength = 0.08f;
        generator.addLeaves = false;
        generator.addColliderPerIvy = false;
        generator.growIvy = false;

        m_Surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m_Surface.name = "Spatial Mesh Surface";
        m_Surface.transform.SetParent(m_Root.transform);
        m_Surface.transform.position = new Vector3(0f, 0f, 1f);
        m_Surface.transform.localScale = new Vector3(2f, 2f, 0.1f);
    }

    [TearDown]
    public void TearDown()
    {
        if (m_Root != null)
            Object.DestroyImmediate(m_Root);
    }

    [Test]
    public void SpatialHitGrowsOneIvyStrokeAndHonorsTheSessionBudget()
    {
        var controller = m_Root.AddComponent<NatureIvySpatialGrowthController>();
        controller.Configure(m_GeneratorObject.GetComponent<IvyGeneratorTREANT>(), 1);
        controller.StartGrowth();
        Physics.SyncTransforms();
        Assert.That(Physics.Raycast(new Ray(Vector3.zero, Vector3.forward), out var hit, 3f), Is.True);

        Assert.That(controller.TryGrowAtSpatialHit(hit), Is.True);
        Assert.That(controller.GrownStrokeCount, Is.EqualTo(1));
        Assert.That(controller.TryGrowAtSpatialHit(hit), Is.False);
    }
}
