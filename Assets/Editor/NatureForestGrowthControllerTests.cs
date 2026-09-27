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

    [Test]
    public void SpawnedForestTreeUsesOneSixthOfItsPrefabScale()
    {
        m_TreePrefab.transform.localScale = new Vector3(2f, 4f, 6f);
        var controller = m_Root.AddComponent<NatureForestGrowthController>();
        var viewer = new GameObject("Nature Viewer").transform;
        viewer.SetParent(m_Root.transform);
        viewer.position = Vector3.zero;
        viewer.forward = Vector3.forward;
        controller.Configure(viewer, new[] { m_TreePrefab });
        controller.StartGrowth();

        controller.SetProgress(0.35f);

        var forestRoot = controller.ForestRoot;
        Assert.That(forestRoot, Is.Not.Null);
        Assert.That(forestRoot.childCount, Is.EqualTo(1));
        Assert.That(forestRoot.GetChild(0).localScale, Is.EqualTo(new Vector3(1f / 3f, 2f / 3f, 1f)));
    }

    [Test]
    public void FullGrowthPlacesTreesAroundViewerWithClearDistantFrontSpace()
    {
        var controller = m_Root.AddComponent<NatureForestGrowthController>();
        var viewer = new GameObject("Nature Viewer").transform;
        viewer.SetParent(m_Root.transform);
        viewer.position = Vector3.zero;
        viewer.forward = Vector3.forward;
        controller.Configure(viewer, new[] { m_TreePrefab });
        controller.StartGrowth();

        controller.SetProgress(1f);

        var forestRoot = controller.ForestRoot;
        Assert.That(forestRoot.childCount, Is.EqualTo(5));

        // The first two trees leave the central forward view open and stay well beyond
        // the interactive bag area. The final tree completes the ring behind the viewer.
        Assert.That(forestRoot.GetChild(0).position.z, Is.GreaterThan(3.5f));
        Assert.That(forestRoot.GetChild(1).position.z, Is.GreaterThan(3.5f));
        Assert.That(Mathf.Abs(forestRoot.GetChild(0).position.x), Is.GreaterThan(1.5f));
        Assert.That(Mathf.Abs(forestRoot.GetChild(1).position.x), Is.GreaterThan(1.5f));
        Assert.That(forestRoot.GetChild(4).position.z, Is.LessThan(-2f));
    }

    [Test]
    public void MissingSpatialMeshFallsBackToFloorLevelInsteadOfViewerHeight()
    {
        var controller = m_Root.AddComponent<NatureForestGrowthController>();
        var viewer = new GameObject("Nature Viewer").transform;
        viewer.SetParent(m_Root.transform);
        viewer.position = new Vector3(0f, 1.65f, 0f);
        viewer.forward = Vector3.forward;
        controller.Configure(viewer, new[] { m_TreePrefab });
        controller.StartGrowth();

        controller.SetProgress(0.35f);

        var tree = controller.ForestRoot.GetChild(0);
        Assert.That(tree.position.y, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void SpawnedTreesRemainWorldUpright_WhenControllerParentIsRotated()
    {
        m_Root.transform.rotation = Quaternion.Euler(35f, 20f, 15f);
        var controller = m_Root.AddComponent<NatureForestGrowthController>();
        var viewer = new GameObject("Nature Viewer").transform;
        viewer.position = Vector3.zero;
        viewer.forward = Vector3.forward;
        controller.Configure(viewer, new[] { m_TreePrefab });
        controller.StartGrowth();

        controller.SetProgress(0.35f);

        Assert.That(Vector3.Dot(controller.ForestRoot.GetChild(0).up, Vector3.up), Is.GreaterThan(.999f));
    }
}
