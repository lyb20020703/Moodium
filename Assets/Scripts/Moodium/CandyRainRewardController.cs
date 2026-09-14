using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Moodium.CandyWorld
{
    [DisallowMultipleComponent]
    public sealed class CandyRainRewardController : MonoBehaviour
    {
        [SerializeField, Range(1, 12)] int m_FirstWaveCount = 5;
        [SerializeField, Range(1, 20)] int m_SecondWaveCount = 9;
        [SerializeField, Range(0.2f, 1f)] float m_WaveDelay = 0.4f;
        [SerializeField, Range(0.01f, 0.2f)] float m_RealityScaleMultiplier = 0.05f;
        [SerializeField, Range(0.4f, 1.5f)] float m_DistanceFromUser = 0.9f;
        [SerializeField, Range(0.3f, 1.2f)] float m_HeightAboveView = 0.62f;
        [SerializeField, Range(0.1f, 0.8f)] float m_HorizontalSpread = 0.42f;

        readonly List<GameObject> m_Prefabs = new();
        readonly List<GameObject> m_ActiveRewardCandies = new();
        Camera m_Camera;
        Coroutine m_RainRoutine;

        public bool IsPlaying => m_RainRoutine != null;
        public int ActiveCandyCount
        {
            get
            {
                m_ActiveRewardCandies.RemoveAll(item => item == null);
                return m_ActiveRewardCandies.Count;
            }
        }

        public void Configure(Camera camera, GameObject[] prefabs)
        {
            if (camera != null)
                m_Camera = camera;
            m_Prefabs.Clear();
            if (prefabs == null) return;
            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;
                var normalized = prefab.name.Replace("Prefab", string.Empty).Replace("_", string.Empty);
                if (normalized.Equals("Chocolate", System.StringComparison.OrdinalIgnoreCase) ||
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
            Debug.Log("[Candy Rain Reward] Energy 100%. Starting gentle miniature candy rain.");
            yield return SpawnWave(m_FirstWaveCount);
            yield return new WaitForSeconds(m_WaveDelay);
            yield return SpawnWave(m_SecondWaveCount);
            m_RainRoutine = null;
        }

        IEnumerator SpawnWave(int count)
        {
            var cameraTransform = m_Camera.transform;
            var flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = cameraTransform.forward.normalized;
            var right = Vector3.Cross(Vector3.up, flatForward).normalized;
            var center = cameraTransform.position + flatForward * m_DistanceFromUser +
                         Vector3.up * m_HeightAboveView;

            for (var i = 0; i < count; i++)
            {
                var prefab = m_Prefabs[Random.Range(0, m_Prefabs.Count)];
                var position = center + right * Random.Range(-m_HorizontalSpread, m_HorizontalSpread) +
                               flatForward * Random.Range(-0.18f, 0.18f) +
                               Vector3.up * Random.Range(-0.05f, 0.12f);
                var instance = Instantiate(prefab, position, Random.rotation);
                instance.name = $"{prefab.name} (Reality Energy Reward Mini)";
                var projectile = instance.GetComponent<CandyProjectile>();
                if (projectile == null) projectile = instance.AddComponent<CandyProjectile>();
                var drift = right * Random.Range(-0.08f, 0.08f) + flatForward * Random.Range(-0.05f, 0.05f);
                projectile.Launch(Vector3.down * Random.Range(0.02f, 0.08f) + drift, m_RealityScaleMultiplier);
                m_ActiveRewardCandies.Add(instance);
                yield return new WaitForSeconds(Random.Range(0.045f, 0.085f));
            }
        }

        void OnDisable() => StopAndClear();
    }
}
