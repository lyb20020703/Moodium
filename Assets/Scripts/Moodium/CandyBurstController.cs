using System.Collections;
using System.Collections.Generic;
using Moodium.Interaction;
using UnityEngine;

namespace Moodium.CandyWorld
{
    public sealed class CandyBurstController : MonoBehaviour
    {
        [SerializeField, Range(1, 8)] int m_MinCandyPerBurst = 2;
        [SerializeField, Range(1, 10)] int m_MaxCandyPerBurst = 4;
        [SerializeField, Range(0.01f, 0.5f)] float m_RealityScaleMultiplier = 0.05f;
        [SerializeField] Vector2 m_UpwardSpeed = new(0.55f, 0.85f);
        [SerializeField] float m_OutwardSpeed = 0.24f;
        [SerializeField] float m_SpawnInterval = 0.045f;

        readonly List<GameObject> m_CandyPrefabs = new();
        readonly List<CandyProjectile> m_ActiveProjectiles = new();

        public void Configure(GameObject[] prefabs)
        {
            m_CandyPrefabs.Clear();
            if (prefabs == null)
                return;
            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                    continue;
                var normalized = prefab.name.Replace("Prefab", string.Empty).Replace("_", string.Empty);
                if (normalized.Equals("Chocolate", System.StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("Cookie", System.StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("Heart", System.StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("Macaron", System.StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("Star", System.StringComparison.OrdinalIgnoreCase))
                    m_CandyPrefabs.Add(prefab);
            }
        }

        public void Burst(Vector3 center, Transform source)
        {
            if (source == null || m_CandyPrefabs.Count == 0)
            {
                Debug.LogWarning("[Candy Burst] Chocolate Capsule source or prefab list is not ready.");
                return;
            }
            // ChocolateCapsuleInteraction already plays the unified Moodium touch VFX.
            // Do not add a second legacy/debug particle layer here.
            var sourceColliders = source.GetComponentsInChildren<Collider>(true);
            StartCoroutine(SpawnBurst(center, source, sourceColliders, Random.Range(m_MinCandyPerBurst, m_MaxCandyPerBurst + 1)));
        }

        public void StopAndClear()
        {
            StopAllCoroutines();
            var removed = 0;
            foreach (var projectile in m_ActiveProjectiles)
            {
                if (projectile == null)
                    continue;
                Destroy(projectile.gameObject);
                removed++;
            }
            m_ActiveProjectiles.Clear();
            Debug.Log($"[Candy Burst] Cleared {removed} Reality Enhancement candy instances.");
        }

        IEnumerator SpawnBurst(Vector3 center, Transform source, Collider[] sourceColliders, int count)
        {
            Debug.Log($"[Candy Burst] Spawning {count} miniature candies from Chocolate Capsule center at {center}.");
            for (var i = 0; i < count; i++)
            {
                var prefab = m_CandyPrefabs[Random.Range(0, m_CandyPrefabs.Count)];
                if (source == null)
                    yield break;
                var radial = Random.onUnitSphere;
                radial.y = Mathf.Abs(radial.y) * 0.55f + 0.25f;
                radial.Normalize();
                var origin = center + radial * Random.Range(0.006f, 0.015f);
                var instance = Instantiate(prefab, origin, Random.rotation);
                instance.name = $"{prefab.name} (Reality Capsule Burst Mini)";
                var outward = radial * Random.Range(m_OutwardSpeed * 0.55f, m_OutwardSpeed);
                var velocity = Vector3.up * Random.Range(m_UpwardSpeed.x, m_UpwardSpeed.y) + outward;
                var projectile = instance.GetComponent<CandyProjectile>();
                if (projectile == null)
                    projectile = instance.AddComponent<CandyProjectile>();
                projectile.Launch(velocity, m_RealityScaleMultiplier);
                projectile.IgnoreCollisionsWith(sourceColliders);
                m_ActiveProjectiles.RemoveAll(item => item == null);
                foreach (var previous in m_ActiveProjectiles)
                {
                    projectile.IgnoreCollisionsWith(previous.Colliders);
                    previous.IgnoreCollisionsWith(projectile.Colliders);
                }
                m_ActiveProjectiles.Add(projectile);
                yield return new WaitForSeconds(m_SpawnInterval);
            }
        }

        void OnDisable() => StopAndClear();
    }
}
