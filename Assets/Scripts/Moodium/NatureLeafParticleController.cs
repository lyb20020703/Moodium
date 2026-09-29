using System.Collections.Generic;
using UnityEngine;

namespace Moodium.NatureWorld
{
    /// <summary>Nature-only completion atmosphere. Leaves begin after the bag reaches 100%.</summary>
    [DisallowMultipleComponent]
    public sealed class NatureLeafParticleController : MonoBehaviour
    {
        [SerializeField] ParticleSystem m_Leaves;
        [SerializeField] Texture2D[] m_LeafTextures;
        [SerializeField, Min(0f)] float m_CompletionRate = 18f;
        [SerializeField, Min(0f)] float m_EmissionHeightAboveViewer = 2.4f;
        [Header("Leaf motion")]
        [SerializeField, Min(0.01f)] float m_MinFallSpeed = .08f;
        [SerializeField, Min(0.01f)] float m_MaxFallSpeed = .22f;
        [SerializeField, Min(0f)] float m_SwayAmplitude = .12f;
        [SerializeField, Min(0f)] float m_SwayFrequency = .55f;
        [SerializeField, Min(0f)] float m_HorizontalDrift = .035f;
        [SerializeField, Min(0f)] float m_DepthDrift = .018f;
        [SerializeField] Vector2 m_RollSpeedX = new(-.55f, .55f);
        [SerializeField] Vector2 m_RollSpeedY = new(-.7f, .7f);
        [SerializeField] Vector2 m_RollSpeedZ = new(-.5f, .5f);
        [SerializeField] Vector2 m_SizeMultiplier = new(.7f, 1.25f);

        struct LeafMotion
        {
            public float Phase;
            public float FallSpeed;
            public float DriftSign;
            public float RollX;
            public float RollY;
            public float RollZ;
            public float SizeMultiplier;
            public float InitialRotationX;
            public float InitialRotationY;
            public float InitialRotationZ;
            public bool SizeApplied;
            public bool RotationApplied;
        }

        readonly Dictionary<uint, LeafMotion> m_MotionBySeed = new();
        readonly List<ParticleSystem> m_LeafVariants = new();
        readonly List<Material> m_RuntimeMaterials = new();
        ParticleSystem.Particle[] m_ParticleBuffer;
        bool m_HasOrigin;
        bool m_Emitting;

        public static bool ShouldEmitLeaves(float normalizedProgress) => normalizedProgress >= .999f;
        public static bool NeedsVariantInitialization(int variantCount) => variantCount == 0;
        // The stable visionOS implementation deliberately renders one leaf system only.
        // Texture variants created overlapping particle renderers on device.
        public static int GetLeafSystemCount(int _) => 1;
        public static float GetEmissionRatePerVariant(float totalRate, int variantCount) =>
            variantCount > 0 ? Mathf.Max(0f, totalRate) / variantCount : 0f;
        public static int GetInitialEmissionCount(int totalCount, int variantCount, int variantIndex)
        {
            if (totalCount <= 0 || variantCount <= 0 || variantIndex < 0 || variantIndex >= variantCount)
                return 0;
            return totalCount / variantCount + (variantIndex < totalCount % variantCount ? 1 : 0);
        }

        public void Configure(ParticleSystem leaves)
        {
            m_Leaves = leaves;
            EnsureLeafVariants();
            StopLeaves();
        }

        // The atmosphere prefab is instantiated by CandyWorldManager rather than
        // created through Configure. Initialize lazily so that path uses the same
        // serialized particle system and texture variants.
        void EnsureLeafVariants()
        {
            if (!NeedsVariantInitialization(m_LeafVariants.Count))
                return;
            BuildLeafVariants();
        }

        void BuildLeafVariants()
        {
            m_LeafVariants.Clear();
            if (m_Leaves == null)
                return;

            // Keep the authored NatureLeafFall material, which is bound to leaf.png.
            // Do not clone particle systems or materials for leaf2 / leaf3: PolySpatial
            // renders their coincident particles as one visually stacked leaf.
            m_LeafVariants.Add(m_Leaves);
        }

        public void StartLeaves()
        {
            m_HasOrigin = false;
            m_Emitting = false;
            if (m_Leaves == null)
                return;
            EnsureLeafVariants();
            foreach (var leaves in m_LeafVariants)
                leaves.Clear(true);
            m_MotionBySeed.Clear();
            foreach (var leaves in m_LeafVariants)
                leaves.Play(true);
            SetEmissionRate(0f);
        }

        public void StopLeaves()
        {
            m_HasOrigin = false;
            m_Emitting = false;
            foreach (var leaves in m_LeafVariants)
                leaves.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void SetOrigin(Vector3 fallbackPosition)
        {
            if (m_HasOrigin)
                return;

            var camera = Camera.main;
            if (camera != null)
            {
                transform.position = camera.transform.position + Vector3.up * m_EmissionHeightAboveViewer;
            }
            else
            {
                transform.position = fallbackPosition + Vector3.up * m_EmissionHeightAboveViewer;
            }
            m_HasOrigin = true;
        }

        public void UpdateLeafIntensity(float normalizedProgress)
        {
            if (!m_HasOrigin || m_LeafVariants.Count == 0)
                return;
            var shouldEmit = ShouldEmitLeaves(normalizedProgress);
            if (shouldEmit == m_Emitting)
                return;
            m_Emitting = shouldEmit;
            SetEmissionRate(m_Emitting ? m_CompletionRate : 0f);
            if (m_Emitting)
                EmitInitialLeaves(16);
        }

        void LateUpdate()
        {
            // World-space particles retain their existing motion, but each new leaf
            // is emitted above the current viewer position. This keeps the effect
            // broad around someone who walks, rather than leaving one small patch.
            if (!m_Emitting || !m_HasOrigin || Camera.main == null)
                return;
            var cameraPosition = Camera.main.transform.position;
            transform.position = new Vector3(cameraPosition.x, cameraPosition.y + m_EmissionHeightAboveViewer, cameraPosition.z);
        }

        void Update()
        {
            if (!m_Emitting || m_LeafVariants.Count == 0)
                return;
            for (var variant = 0; variant < m_LeafVariants.Count; variant++)
                UpdateVariantMotion(m_LeafVariants[variant], variant);
        }

        void UpdateVariantMotion(ParticleSystem leaves, int variant)
        {
            var count = leaves.particleCount;
            if (count <= 0)
                return;
            if (m_ParticleBuffer == null || m_ParticleBuffer.Length < count)
                m_ParticleBuffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(count)];

            count = leaves.GetParticles(m_ParticleBuffer);
            var now = Time.time;
            var delta = Mathf.Max(0f, Time.deltaTime);
            for (var i = 0; i < count; i++)
            {
                var particle = m_ParticleBuffer[i];
                var seed = particle.randomSeed != 0 ? particle.randomSeed : (uint)(i + 1);
                var key = seed ^ (uint)(variant * 100003);
                if (!m_MotionBySeed.TryGetValue(key, out var motion))
                {
                    motion = CreateMotion(key);
                    m_MotionBySeed[key] = motion;
                }

                if (!motion.SizeApplied)
                {
                    particle.startSize3D *= motion.SizeMultiplier;
                    motion.SizeApplied = true;
                }
                if (!motion.RotationApplied)
                {
                    particle.rotation3D = new Vector3(motion.InitialRotationX, motion.InitialRotationY, motion.InitialRotationZ);
                    motion.RotationApplied = true;
                }

                var sway = Mathf.Sin(now * m_SwayFrequency + motion.Phase) * m_SwayAmplitude;
                particle.velocity = new Vector3(
                    motion.DriftSign * m_HorizontalDrift + sway,
                    -motion.FallSpeed,
                    Mathf.Cos(now * m_SwayFrequency * .73f + motion.Phase) * m_DepthDrift);
                particle.rotation3D += new Vector3(motion.RollX, motion.RollY, motion.RollZ) * delta;
                m_ParticleBuffer[i] = particle;
                m_MotionBySeed[key] = motion;
            }
            leaves.SetParticles(m_ParticleBuffer, count);
        }

        LeafMotion CreateMotion(uint seed)
        {
            var random = new System.Random(unchecked((int)seed));
            return new LeafMotion
            {
                Phase = (float)(random.NextDouble() * Mathf.PI * 2f),
                FallSpeed = Mathf.Lerp(m_MinFallSpeed, m_MaxFallSpeed, (float)random.NextDouble()),
                DriftSign = random.Next(0, 2) == 0 ? -1f : 1f,
                RollX = Mathf.Lerp(m_RollSpeedX.x, m_RollSpeedX.y, (float)random.NextDouble()),
                RollY = Mathf.Lerp(m_RollSpeedY.x, m_RollSpeedY.y, (float)random.NextDouble()),
                RollZ = Mathf.Lerp(m_RollSpeedZ.x, m_RollSpeedZ.y, (float)random.NextDouble()),
                SizeMultiplier = Mathf.Lerp(m_SizeMultiplier.x, m_SizeMultiplier.y, (float)random.NextDouble()),
                InitialRotationX = (float)(random.NextDouble() * Mathf.PI * 2f),
                InitialRotationY = (float)(random.NextDouble() * Mathf.PI * 2f),
                InitialRotationZ = (float)(random.NextDouble() * Mathf.PI * 2f)
            };
        }

        void SetEmissionRate(float rate)
        {
            if (m_Leaves == null)
                return;
            var ratePerVariant = GetEmissionRatePerVariant(rate, m_LeafVariants.Count);
            foreach (var leaves in m_LeafVariants)
            {
                var variantEmission = leaves.emission;
                variantEmission.rateOverTime = ratePerVariant;
            }
        }

        void EmitInitialLeaves(int totalCount)
        {
            for (var i = 0; i < m_LeafVariants.Count; i++)
            {
                var count = GetInitialEmissionCount(totalCount, m_LeafVariants.Count, i);
                if (count > 0 && m_LeafVariants[i] != null)
                    m_LeafVariants[i].Emit(count);
            }
        }

    }
}
