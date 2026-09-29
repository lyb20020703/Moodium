using System.Collections.Generic;
using Moodium.NatureWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class NatureIvySurfaceResolverTests
{
    GameObject m_Root;

    [SetUp]
    public void SetUp() => m_Root = new GameObject("Nature Ivy Surface Resolver Tests");

    [TearDown]
    public void TearDown()
    {
        if (m_Root != null)
            Object.DestroyImmediate(m_Root);
    }

    [Test]
    public void FindsUpwardSpatialSurfaceAndIgnoresNearerVirtualCollider()
    {
        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.transform.SetParent(m_Root.transform);
        table.transform.position = Vector3.zero;
        table.transform.localScale = new Vector3(2f, .1f, 2f);

        var virtualBlocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        virtualBlocker.transform.SetParent(m_Root.transform);
        virtualBlocker.transform.position = new Vector3(0f, .5f, 0f);
        virtualBlocker.transform.localScale = new Vector3(.5f, .1f, .5f);
        Physics.SyncTransforms();

        var resolver = new NatureIvySurfaceResolver(
            new[] { table.GetComponent<Collider>() }, .65f, 2f, .08f, 8);

        Assert.That(resolver.TryFindSurfaceBelow(new Vector3(0f, 1f, 0f), out var hit), Is.True);
        Assert.That(hit.collider, Is.EqualTo(table.GetComponent<Collider>()));
        Assert.That(Vector3.Dot(hit.normal, Vector3.up), Is.GreaterThanOrEqualTo(.65f));
    }

    [Test]
    public void RejectsWallWhenNoUpwardSpatialSurfaceExists()
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(m_Root.transform);
        wall.transform.position = new Vector3(0f, .5f, .1f);
        wall.transform.localScale = new Vector3(2f, 2f, .1f);
        Physics.SyncTransforms();

        var resolver = new NatureIvySurfaceResolver(
            new List<Collider> { wall.GetComponent<Collider>() }, .65f, 2f, .08f, 8);

        Assert.That(resolver.TryFindSurfaceBelow(new Vector3(0f, 1f, 0f), out _), Is.False);
    }
}
