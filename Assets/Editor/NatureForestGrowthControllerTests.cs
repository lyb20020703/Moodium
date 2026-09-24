using Moodium.NatureWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class NatureForestGrowthControllerTests
{
    GameObject m_Root;
    GameObject m_TreePrefab;

    [SetUp]
    public void SetUp()
    {
        m_Root = new GameObject("Nature Forest Test Root");
        m_TreePrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        m_TreePrefab.name = "LOD Tree Test Prefab";
        m_TreePrefab.SetActive(false);
    }

    [TearDown]
    public void TearDown()
    {
        if (m_Root != null)
            Object.DestroyImmediate(m_Root);
        if (m_TreePrefab != null)
            Object.DestroyImmediate(m_TreePrefab);
    }

    [Test]
    public void ProgressThresholdsGrowOneForestTreeAtATime()
    {
        var controller = m_Root.AddComponent<NatureForestGrowthController>();
        var viewer = new GameObject("Nature Viewer").transform;
        viewer.SetParent(m_Root.transform);
        viewer.position = Vector3.zero;
        viewer.forward = Vector3.forward;
        controller.Configure(viewer, new[] { m_TreePrefab });
        controller.StartGrowth();

        controller.SetProgress(0.34f);
        Assert.That(controller.SpawnedTreeCount, Is.EqualTo(0));

        controller.SetProgress(0.35f);
        Assert.That(controller.SpawnedTreeCount, Is.EqualTo(1));

        controller.SetProgress(0.55f);
        Assert.That(controller.SpawnedTreeCount, Is.EqualTo(2));
    }

    [Test]
    public void StopAndClearRemovesSpawnedForestTrees()
    {
        var controller = m_Root.AddComponent<NatureForestGrowthController>();
        var viewer = new GameObject("Nature Viewer").transform;
        viewer.SetParent(m_Root.transform);
        controller.Configure(viewer, new[] { m_TreePrefab });
        controller.StartGrowth();
        controller.SetProgress(1f);

        controller.StopAndClear();

        Assert.That(controller.SpawnedTreeCount, Is.EqualTo(0));
    }
}
