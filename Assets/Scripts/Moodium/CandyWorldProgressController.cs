using System.Collections.Generic;
using Moodium.Reality;
using UnityEngine;

namespace Moodium.CandyWorld
{
    /// <summary>
    /// Presentation-only orchestration for the Candy Energy journey. Energy calculation
    /// remains owned by CandyEnergyController; this component only coordinates atmosphere.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CandyWorldProgressController : MonoBehaviour
    {
        [Header("Ambient candy emergence")]
        [SerializeField, Range(0.7f, 0.95f)] float m_CandyEmergenceStart = 0.8f;
        [SerializeField, Range(0.01f, 0.12f)] float m_AmbientCandyScale = 0.04f;
        [SerializeField, Range(1, 8)] int m_MaxAmbientCandies = 4;
        [SerializeField, Range(0.4f, 1.5f)] float m_DistanceFromUser = 0.85f;

        readonly List<GameObject> m_Prefabs = new();
        readonly List<GameObject> m_AmbientCandies = new();
        Camera m_Camera;
        CandySpatialMeshTransformationController m_SpatialTransformation;
        CandyFrostParticleController m_Frost;
        CandyRainRewardController m_RainReward;
        float m_NextCandyProgress;
        bool m_Running;
        bool m_CompletionTriggered;

        public void Configure(
            Camera camera,
            CandySpatialMeshTransformationController spatialTransformation,
            CandyFrostParticleController frost,
            CandyRainRewardController rainReward,
            GameObject[] prefabs)
        {
            m_Camera = camera;
            m_SpatialTransformation = spatialTransformation;
            m_Frost = frost;
            m_RainReward = rainReward;
            m_Prefabs.Clear();
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
                    m_Prefabs.Add(prefab);
            }
        }

        public void StartProgress()
        {
            StopAndClear();
            m_Running = true;
            m_CompletionTriggered = false;
            m_NextCandyProgress = m_CandyEmergenceStart;
            m_SpatialTransformation?.StartTransformation();
            m_Frost?.StartFrost();
            SetProgress(0f, 0f);
            Debug.Log("[Candy Progress] Environment journey started at natural reality state.");
        }

        public void SetOrigin(Vector3 worldPosition)
        {
            if (!m_Running)
                return;
            m_SpatialTransformation?.SetOrigin(worldPosition);
            m_Frost?.SetOrigin(worldPosition);
        }

        public void SetProgress(float energy, float normalizedProgress)
        {
            if (!m_Running)
                return;
            var progress = Mathf.Clamp01(normalizedProgress);
            m_SpatialTransformation?.SetProgress(energy, progress);
            m_Frost?.UpdateFrostIntensity(energy, progress);

            if (progress >= m_NextCandyProgress && m_AmbientCandies.Count < m_MaxAmbientCandies)
            {
                SpawnAmbientCandy(progress);
                m_NextCandyProgress += 0.06f;
            }

            if (!m_CompletionTriggered && progress >= 0.999f)
            {
                m_CompletionTriggered = true;
                m_RainReward?.PlayReward();
                Debug.Log("[Candy Progress] Candy World Awakening reached 100%.");
            }
        }

        public void StopAndClear()
        {
            m_Running = false;
            m_SpatialTransformation?.StopTransformation();
            m_Frost?.StopFrost();
            m_RainReward?.StopAndClear();
            foreach (var candy in m_AmbientCandies)
                if (candy != null) Destroy(candy);
            m_AmbientCandies.Clear();
        }

        void SpawnAmbientCandy(float progress)
        {
            m_AmbientCandies.RemoveAll(item => item == null);
            if (m_Camera == null || m_Prefabs.Count == 0 || m_AmbientCandies.Count >= m_MaxAmbientCandies)
                return;

            var cameraTransform = m_Camera.transform;
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = cameraTransform.forward.normalized;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var position = cameraTransform.position + forward * m_DistanceFromUser +
                           right * Random.Range(-0.34f, 0.34f) +
                           Vector3.up * Random.Range(0.25f, 0.48f);
            var prefab = m_Prefabs[Random.Range(0, m_Prefabs.Count)];
            var instance = Instantiate(prefab, position, Random.rotation);
            instance.name = $"{prefab.name} (Candy Atmosphere Mini)";
            var projectile = instance.GetComponent<CandyProjectile>();
            if (projectile == null)
                projectile = instance.AddComponent<CandyProjectile>();
            var drift = right * Random.Range(-0.025f, 0.025f) + Vector3.up * Random.Range(0.015f, 0.045f);
            projectile.Launch(drift, m_AmbientCandyScale);
            m_AmbientCandies.Add(instance);
            Debug.Log($"[Candy Progress] Gentle ambient candy appeared at {progress:P0}: {prefab.name}.");
        }

        void OnDisable() => StopAndClear();
    }
}
