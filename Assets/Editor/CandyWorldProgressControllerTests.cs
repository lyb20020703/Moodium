using System.Reflection;
using Moodium.CandyWorld;
using Moodium.Reality;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public sealed class CandyWorldProgressControllerTests
{
    GameObject m_Root;

    [SetUp]
    public void SetUp() => m_Root = new GameObject("Candy Progress Test Root");

    [TearDown]
    public void TearDown()
    {
        if (m_Root != null)
            Object.DestroyImmediate(m_Root);
    }

    [Test]
    public void DisabledSpatialTransformationDoesNotStartCandyMeshOverlay()
    {
        var meshManager = m_Root.AddComponent<ARMeshManager>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.That(shader, Is.Not.Null);
        var material = new Material(shader);
        var transformation = m_Root.AddComponent<CandySpatialMeshTransformationController>();
        transformation.Configure(meshManager, material);

        var progress = m_Root.AddComponent<CandyWorldProgressController>();
        progress.Configure(null, transformation, null, null, null);
        progress.SetSpatialTransformationEnabled(false);
        progress.StartProgress();

        var isRunning = (bool)typeof(CandySpatialMeshTransformationController)
            .GetField("m_Running", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(transformation);
        Assert.That(isRunning, Is.False);

        Object.DestroyImmediate(material);
    }
}
