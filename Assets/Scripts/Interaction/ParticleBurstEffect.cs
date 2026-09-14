using UnityEngine;
using UnityEngine.Rendering;

namespace Moodium.Interaction
{
    /// <summary>Creates a short, lightweight white candy-pop particle burst.</summary>
    public sealed class ParticleBurstEffect : MonoBehaviour
    {
        ParticleSystem m_Particles;
        Material m_RuntimeMaterial;

        public static GameObject Spawn(Vector3 position)
        {
            var effectObject = new GameObject("Moodium White Candy Burst");
            effectObject.transform.position = position;
            effectObject.AddComponent<ParticleBurstEffect>().BuildAndPlay();
            return effectObject;
        }

        void BuildAndPlay()
        {
            m_Particles = gameObject.AddComponent<ParticleSystem>();
            var main = m_Particles.main;
            main.duration = 0.12f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.26f, 0.46f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.018f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.95f),
                new Color(0.76f, 0.86f, 1f, 0.8f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            main.stopAction = ParticleSystemStopAction.None;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var emission = m_Particles.emission;
            emission.rateOverTime = 140f;
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());

            var shape = m_Particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.015f;
            shape.radiusThickness = 0.2f;

            var velocity = m_Particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.radial = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);

            var colorOverLifetime = m_Particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.7f, 0.82f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var renderer = m_Particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                m_RuntimeMaterial = new Material(shader) { name = "Moodium Burst Material (Runtime)" };
                var color = Color.white;
                if (m_RuntimeMaterial.HasProperty("_BaseColor"))
                    m_RuntimeMaterial.SetColor("_BaseColor", color);
                if (m_RuntimeMaterial.HasProperty("_Color"))
                    m_RuntimeMaterial.SetColor("_Color", color);
                renderer.sharedMaterial = m_RuntimeMaterial;
            }

            m_Particles.Play();
            Destroy(gameObject, 1.25f);
        }

        void OnDestroy()
        {
            if (m_RuntimeMaterial != null)
                Destroy(m_RuntimeMaterial);
        }
    }
}
