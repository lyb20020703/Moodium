using UnityEngine;
using UnityEngine.Rendering;

namespace Moodium.NatureWorld
{
    /// <summary>Continuous wind on the actual splats, using RealityKit-compatible skinning.
    /// Only bone transforms change each frame; the shared baked mesh is never edited.</summary>
    [DisallowMultipleComponent]
    public sealed class BubbleWorldAmbientWind : MonoBehaviour
    {
        const int Sectors = 12, Rows = 3;
        Mesh m_Mesh;
        MeshRenderer m_Static;
        SkinnedMeshRenderer m_Skin;
        Transform[] m_Bones;
        Vector3[] m_HandOffsets, m_ControlCentres;
        Quaternion m_ToSource;
        float m_Strength, m_Fade;
        bool m_Animating;

        public void Configure(int layer, bool candy, Quaternion sourceFrame = default)
        {
            var source = GetComponent<MeshFilter>().sharedMesh;
            if (source == null || !source.isReadable) return;
            if (sourceFrame == default) sourceFrame = Quaternion.identity;
            m_ToSource = Quaternion.Inverse(sourceFrame);
            m_Strength = candy ? .045f : .06f;
            m_Static = GetComponent<MeshRenderer>();
            m_Bones = new Transform[Sectors * Rows];
            m_HandOffsets = new Vector3[m_Bones.Length];
            m_ControlCentres = new Vector3[m_Bones.Length];
            var totals = new float[m_Bones.Length];
            var poses = new Matrix4x4[m_Bones.Length];
            for (var i = 0; i < m_Bones.Length; i++)
            {
                m_Bones[i] = new GameObject("Wind " + i).transform;
                m_Bones[i].SetParent(transform, false);
                poses[i] = Matrix4x4.identity;
            }
            var vertices = source.vertices;
            var weights = new BoneWeight[vertices.Length];
            for (var v = 0; v + 3 < vertices.Length; v += 4)
            {
                // Every corner gets identical weights: each splat translates without stretching.
                var centre = sourceFrame * ((vertices[v] + vertices[v + 2]) * .5f);
                var weight = Weights(centre);
                for (var corner = 0; corner < 4; corner++) weights[v + corner] = weight;
                Accumulate(weight.boneIndex0, weight.weight0, centre, totals);
                Accumulate(weight.boneIndex1, weight.weight1, centre, totals);
                Accumulate(weight.boneIndex2, weight.weight2, centre, totals);
                Accumulate(weight.boneIndex3, weight.weight3, centre, totals);
            }
            for (var i = 0; i < m_ControlCentres.Length; i++)
                m_ControlCentres[i] = totals[i] > 0 ? m_ToSource * (m_ControlCentres[i] / totals[i]) : Vector3.zero;
            m_Mesh = Instantiate(source);
            m_Mesh.name = source.name + " (wind skin)";
            m_Mesh.bindposes = poses;
            m_Mesh.boneWeights = weights;
            var bounds = source.bounds;
            bounds.Expand(.7f);
            m_Mesh.bounds = bounds;
            var skinObject = new GameObject("Wind splats");
            skinObject.layer = gameObject.layer;
            skinObject.transform.SetParent(transform, false);
            m_Skin = skinObject.AddComponent<SkinnedMeshRenderer>();
            m_Skin.sharedMesh = m_Mesh;
            m_Skin.sharedMaterials = m_Static.sharedMaterials;
            m_Skin.bones = m_Bones;
            m_Skin.rootBone = transform;
            m_Skin.localBounds = bounds;
            m_Skin.quality = SkinQuality.Bone4;
            m_Skin.shadowCastingMode = ShadowCastingMode.Off;
            m_Skin.receiveShadows = false;
            m_Skin.enabled = false;
        }

        public static BoneWeight Weights(Vector3 centre)
        {
            var sector = Mathf.Repeat(Mathf.Atan2(centre.z, centre.x) / (2 * Mathf.PI) + 1, 1) * Sectors;
            var a = Mathf.FloorToInt(sector) % Sectors;
            var b = (a + 1) % Sectors;
            var x = Mathf.Clamp01(sector - Mathf.Floor(sector));
            var height = Mathf.InverseLerp(-1.5f, 3f, centre.y) * (Rows - 1);
            var row = Mathf.Min(Mathf.FloorToInt(height), Rows - 2);
            var y = Mathf.Clamp01(height - row);
            return new BoneWeight {
                boneIndex0 = row * Sectors + a, boneIndex1 = row * Sectors + b,
                boneIndex2 = (row + 1) * Sectors + a, boneIndex3 = (row + 1) * Sectors + b,
                weight0 = (1-x)*(1-y), weight1 = x*(1-y), weight2 = (1-x)*y, weight3 = x*y
            };
        }

        public void SetAnimating(bool active)
        {
            if (m_Skin == null || m_Animating == active) return;
            m_Animating = active;
            m_Static.enabled = !active;
            m_Skin.enabled = active;
            m_Fade = 0;
            foreach (var bone in m_Bones) bone.localPosition = Vector3.zero;
            System.Array.Clear(m_HandOffsets, 0, m_HandOffsets.Length);
        }

        void Accumulate(int index, float weight, Vector3 centre, float[] totals)
        {
            m_ControlCentres[index] += centre * weight;
            totals[index] += weight;
        }

        // Project the hand's direction onto the panorama, so distant scenery responds too.
        public static float HandInfluence(Vector3 pointDirection, Vector3 handDirection)
        {
            if (pointDirection.sqrMagnitude < .0001f || handDirection.sqrMagnitude < .0001f) return 0;
            var alignment = Vector3.Dot(pointDirection.normalized, handDirection.normalized);
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.80f, .995f, alignment));
        }

        public void ApplyHand(Vector3 position, Vector3 velocity, Vector3 eye, float dt)
        {
            if (!m_Animating || velocity.magnitude < .08f) return;
            var direction = position - eye;
            if (direction.magnitude < .15f) return;
            var push = Vector3.ClampMagnitude(velocity, 1.5f) * (.55f * Mathf.Min(dt, .05f));
            for (var i = 0; i < m_Bones.Length; i++)
            {
                var influence = HandInfluence(transform.TransformPoint(m_ControlCentres[i]) - eye, direction);
                var worldOffset = transform.TransformVector(m_HandOffsets[i]) + push * influence;
                m_HandOffsets[i] = transform.InverseTransformVector(Vector3.ClampMagnitude(worldOffset, .12f));
            }
        }

        public static Vector3 Offset(Vector2 phase, float weight, float strength, float time)
        {
            var gust = .7f + .3f * Mathf.Sin(time * .23f);
            return new Vector3(
                Mathf.Sin(time * .85f + phase.x) + .25f * Mathf.Sin(time * 1.43f + phase.y),
                .25f * Mathf.Sin(time * .67f + phase.y),
                .65f * Mathf.Sin(time * .73f + phase.y + phase.x * .4f)) * (weight * strength * gust);
        }

        void Update()
        {
            if (!m_Animating) return;
            m_Fade = Mathf.MoveTowards(m_Fade, 1, Time.deltaTime / 1.5f);
            for (var row = 0; row < Rows; row++)
            for (var sector = 0; sector < Sectors; sector++)
            {
                var angle = sector * (2 * Mathf.PI / Sectors);
                // Periodic spatial phases avoid a seam at the panorama's 0/360 boundary.
                var phase = new Vector2(Mathf.Cos(angle) * 2 + row * .4f, Mathf.Sin(angle) * 2);
                var weight = Mathf.Lerp(.08f, 1, row / (float)(Rows - 1));
                var i = row * Sectors + sector;
                m_HandOffsets[i] *= Mathf.Exp(-2f * Time.deltaTime);
                m_Bones[i].localPosition = m_ToSource *
                    Offset(phase, weight, m_Strength * Mathf.SmoothStep(0, 1, m_Fade), Time.time) + m_HandOffsets[i];
            }
        }

        void OnDestroy()
        {
            if (m_Mesh != null)
            {
                if (Application.isPlaying) Destroy(m_Mesh); else DestroyImmediate(m_Mesh);
            }
        }
    }
}
