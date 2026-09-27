using System.Collections.Generic;
using Moodium.Calibration;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Moodium.Reality
{
    /// <summary>
    /// Evaluates Moodi's authored emergence clip at the current world-progress time.
    /// It never auto-plays: the user's accumulated bag interaction is the timeline.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MoodiEmergenceController : MonoBehaviour
    {
        static readonly HashSet<MoodiEmergenceController> ActiveControllers = new();

        CalibratedSensoryBagPresentation m_Presentation;
        Transform m_FollowRoot;
        GameObject m_Moodi;
        AnimationClip m_Clip;
        PlayableGraph m_Graph;
        AnimationClipPlayable m_Playable;
        bool m_Configured;
        Renderer[] m_Renderers;

        public float Progress { get; private set; }

        public static void SetAllProgress(float normalizedProgress)
        {
            foreach (var controller in ActiveControllers)
                if (controller != null)
                    controller.SetProgress(normalizedProgress);
        }

        public void Configure(
            CalibratedSensoryBagPresentation presentation,
            GameObject moodiPrefab,
            AnimationClip clip,
            float scale)
        {
            if (presentation == null || moodiPrefab == null || clip == null)
                return;
            m_Presentation = presentation;
            m_Clip = clip;
            m_FollowRoot = new GameObject("Moodi Emergence Root").transform;
            m_FollowRoot.SetParent(presentation.transform.parent, false);
            SyncToBagCenter();
            CreateAndConfigureMoodi(moodiPrefab, scale);
        }

        /// <summary>
        /// Uses the same authored-clip graph as production, without enabling the
        /// XR interaction stack. This is intentionally limited to the editor
        /// orientation preview.
        /// </summary>
        public void ConfigurePreview(Transform followRoot, GameObject moodiPrefab, AnimationClip clip, float scale)
        {
            if (followRoot == null || moodiPrefab == null || clip == null)
                return;

            m_Clip = clip;
            m_FollowRoot = followRoot;
            CreateAndConfigureMoodi(moodiPrefab, scale);
        }

        public void SetProgress(float normalizedProgress)
        {
            if (!m_Configured || m_Moodi == null || m_Clip == null)
                return;
            Progress = Mathf.Clamp01(normalizedProgress);
            m_Playable.SetTime(MoodiEmergenceProgress.ToAnimationTime(Progress, m_Clip.length));
            m_Graph.Evaluate(0f);
            // Keep the GameObject active.  Deactivating it at progress zero can stop
            // Playables from applying their first visible pose when it is re-enabled.
            SetRenderersVisible(MoodiEmergenceProgress.ShouldBeVisible(Progress));
        }

        void OnEnable() => ActiveControllers.Add(this);

        void OnDisable() => ActiveControllers.Remove(this);

        void LateUpdate() => SyncToBagCenter();

        void CreateAndConfigureMoodi(GameObject moodiPrefab, float scale)
        {
            if (m_Moodi != null)
                Destroy(m_Moodi);

            m_Moodi = Instantiate(moodiPrefab, m_FollowRoot);
            m_Moodi.name = "Moodi_V2 Emergence";
            m_Moodi.transform.localPosition = Vector3.zero;
            // Moodi's authored model needs this local correction relative to the
            // approved CandySoft/NatureSoft pose. Keep it centralised so preview
            // and the Vision runtime cannot diverge again.
            m_Moodi.transform.localRotation = MoodiEmergenceProgress.AuthoredLocalRotation;
            m_Moodi.transform.localScale = Vector3.one * Mathf.Max(.001f, scale);

            var animator = m_Moodi.GetComponentInChildren<Animator>(true) ?? m_Moodi.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            m_Graph = PlayableGraph.Create("Moodi Emergence Progress Graph");
            var output = AnimationPlayableOutput.Create(m_Graph, "Moodi Emergence", animator);
            m_Playable = AnimationClipPlayable.Create(m_Graph, m_Clip);
            m_Playable.SetSpeed(0d);
            output.SetSourcePlayable(m_Playable);
            m_Graph.Play();
            m_Configured = true;
            m_Renderers = m_Moodi.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"[Moodi Emergence] Configured clip='{m_Clip.name}' length={m_Clip.length:F3}s " +
                      $"scale={m_Moodi.transform.localScale.x:F3}, renderers={m_Renderers.Length}.");
            SetProgress(0f);
        }

        void SyncToBagCenter()
        {
            if (m_Presentation == null || m_FollowRoot == null)
                return;
            m_FollowRoot.SetPositionAndRotation(
                m_Presentation.CenterWorldPosition,
                MoodiEmergenceProgress.ResolveVisualOrientation(m_Presentation.VisualRoot.rotation));
        }

        void SetRenderersVisible(bool visible)
        {
            if (m_Renderers == null)
                return;
            foreach (var renderer in m_Renderers)
                if (renderer != null)
                    renderer.enabled = visible;
        }

        void OnDestroy()
        {
            ActiveControllers.Remove(this);
            if (m_Graph.IsValid())
                m_Graph.Destroy();
            if (m_FollowRoot != null)
                Destroy(m_FollowRoot.gameObject);
        }
    }

    public static class MoodiEmergenceProgress
    {
        public static Quaternion AuthoredLocalRotation => Quaternion.Euler(-90f, 0f, 0f);

        public static float ToAnimationTime(float normalizedProgress, float clipLength) =>
            Mathf.Clamp01(normalizedProgress) * Mathf.Max(0f, clipLength);

        public static bool ShouldBeVisible(float normalizedProgress) => normalizedProgress > 0f;

        /// <summary>Emergence shares the authored world orientation of the fitted bag visual.</summary>
        public static Quaternion ResolveVisualOrientation(Quaternion visualOrientation) => visualOrientation;
    }
}
