using System.Collections;
using System.Collections.Generic;
using Moodium.Interaction;
using Moodium.Reality;
using UnityEngine;

namespace Moodium.CandyWorld
{
    [DisallowMultipleComponent]
    public sealed class CandyRainRewardController : MonoBehaviour
    {
        public const int DefaultMaxActiveRewards = 35;

        [SerializeField, Min(0.1f)] float m_ReleaseSpawnRate = 8f;
        [SerializeField, Range(1, 64)] int m_MaxActiveRewards = DefaultMaxActiveRewards;
        [SerializeField, Min(0.1f)] float m_GroundedRewardLifetime = 5f;
        [SerializeField, Range(0.01f, 0.2f)] float m_RealityScaleMultiplier = 0.05f;
        [SerializeField, Range(0.4f, 1.5f)] float m_DistanceFromUser = 0.9f;
        [SerializeField, Range(0.3f, 1.2f)] float m_HeightAboveView = 0.62f;
        [SerializeField, Range(0.1f, 0.8f)] float m_HorizontalSpread = 0.42f;

        readonly List<GameObject> m_Prefabs = new();
        readonly List<GameObject> m_ActiveRewardCandies = new();
        Camera m_Camera;
        Coroutine m_RainRoutine;
        bool m_NatureRewardPool;

        public bool IsPlaying => m_RainRoutine != null;
        public int ActiveCandyCount
        {
            get
            {
                m_ActiveRewardCandies.RemoveAll(item => item == null);
                return m_ActiveRewardCandies.Count;
            }
        }

        public static float ReleaseScaleMultiplier(bool natureWorld) => natureWorld ? .375f : .15f;

        /// <summary>Only the authored Chocolate_Capsule reward uses its shell-crack animation on hand contact.</summary>
        public static bool IsShellCrackReward(string prefabName)
        {
            var normalized = string.IsNullOrWhiteSpace(prefabName)
                ? string.Empty
                : prefabName.Replace("Prefab", string.Empty).Replace("_", string.Empty)
                    .Replace(" ", string.Empty);
            return normalized.Equals("ChocolateCapsule", System.StringComparison.OrdinalIgnoreCase);
        }

        public void Configure(Camera camera, GameObject[] prefabs, bool acceptAllPrefabs = false)
        {
            if (camera != null)
                m_Camera = camera;
            m_NatureRewardPool = acceptAllPrefabs;
            m_Prefabs.Clear();
            if (prefabs == null) return;
            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;
                var normalized = prefab.name.Replace("Prefab", string.Empty).Replace("_", string.Empty);
                if (acceptAllPrefabs ||
                    normalized.Equals("Chocolate", System.StringComparison.OrdinalIgnoreCase) ||
                    IsShellCrackReward(prefab.name) ||
                    normalized.Equals("Cookie", System.StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("Heart", System.StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("Macaron", System.StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("Star", System.StringComparison.OrdinalIgnoreCase))
                    m_Prefabs.Add(prefab);
            }
        }

        public void PlayReward()
        {
            if (m_RainRoutine != null || m_Camera == null || m_Prefabs.Count == 0)
                return;
            m_RainRoutine = StartCoroutine(PlayCandyRain());
        }

        public void StopAndClear()
        {
            if (m_RainRoutine != null)
                StopCoroutine(m_RainRoutine);
            m_RainRoutine = null;
            foreach (var candy in m_ActiveRewardCandies)
                if (candy != null) Destroy(candy);
            m_ActiveRewardCandies.Clear();
        }

        IEnumerator PlayCandyRain()
        {
            Debug.Log($"[Moodium Energy Release] Starting continuous {(m_NatureRewardPool ? "Nature" : "Candy")} reward rain.");
            var delay = new WaitForSeconds(1f / Mathf.Max(.1f, m_ReleaseSpawnRate));
            while (true)
            {
                m_ActiveRewardCandies.RemoveAll(item => item == null);
                if (m_ActiveRewardCandies.Count < m_MaxActiveRewards)
                    SpawnReward();
                yield return delay;
            }
        }

        void SpawnReward()
        {
            var cameraTransform = m_Camera.transform;
            var flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = cameraTransform.forward.normalized;
            var right = Vector3.Cross(Vector3.up, flatForward).normalized;
            var center = cameraTransform.position + flatForward * m_DistanceFromUser +
                         Vector3.up * m_HeightAboveView;

            var prefab = m_Prefabs[Random.Range(0, m_Prefabs.Count)];
            var position = center + right * Random.Range(-m_HorizontalSpread, m_HorizontalSpread) +
                           flatForward * Random.Range(-0.18f, 0.18f) +
                           Vector3.up * Random.Range(-0.05f, 0.12f);
            var instance = Instantiate(prefab, position, Random.rotation);
            instance.name = $"{prefab.name} (Reality Energy Reward Mini)";
            var projectile = instance.GetComponent<CandyProjectile>();
            if (projectile == null) projectile = instance.AddComponent<CandyProjectile>();
            var touch = instance.GetComponent<DestroyOnTouch>();
            if (touch == null)
                touch = instance.AddComponent<DestroyOnTouch>();
            touch.ConfigureRewardBurstFeedback();
            if (IsShellCrackReward(prefab.name))
                touch.ConfigureShellCrackFeedback(instance.GetComponent<ChocolateCapsuleInteraction>());

            var lifecycle = instance.GetComponent<ReleaseRewardLifecycle>();
            if (lifecycle == null)
                lifecycle = instance.AddComponent<ReleaseRewardLifecycle>();
            lifecycle.Configure(touch, m_GroundedRewardLifetime);

            var drift = right * Random.Range(-0.08f, 0.08f) + flatForward * Random.Range(-0.05f, 0.05f);
            var multiplier = ReleaseScaleMultiplier(m_NatureRewardPool);
            projectile.Launch(Vector3.down * Random.Range(0.02f, 0.08f) + drift, multiplier);
            m_ActiveRewardCandies.Add(instance);
        }

        void OnDisable() => StopAndClear();
    }

    /// <summary>
    /// Release rewards remain collectible on a physical surface briefly, then disappear so the
    /// continuous rain can keep replenishing its capped pool. This component is never attached
    /// to normal squeeze-burst projectiles.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReleaseRewardLifecycle : MonoBehaviour
    {
        DestroyOnTouch m_Touch;
        float m_GroundedLifetime = 5f;
        bool m_HasLanded;
        float m_LandedAt;

        public static bool ShouldExpireAfterLanding(float elapsed, float lifetime) =>
            elapsed >= Mathf.Max(0f, lifetime);

        public void Configure(DestroyOnTouch touch, float groundedLifetime)
        {
            m_Touch = touch;
            m_GroundedLifetime = Mathf.Max(.1f, groundedLifetime);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (m_HasLanded || collision == null ||
                collision.collider.GetComponentInParent<SpatialTableSurface>() == null)
                return;

            m_HasLanded = true;
            m_LandedAt = Time.time;
        }

        void Update()
        {
            if (!m_HasLanded || !ShouldExpireAfterLanding(Time.time - m_LandedAt, m_GroundedLifetime))
                return;

            m_HasLanded = false;
            if (m_Touch == null)
                m_Touch = GetComponent<DestroyOnTouch>();
            if (m_Touch != null)
                m_Touch.DisappearAutomatically();
            else
                Destroy(gameObject);
        }
    }
}
