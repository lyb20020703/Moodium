using UnityEngine;

namespace Moodium.Opening
{
    /// <summary>
    /// Keeps a VideoPlayer RenderTexture synchronized with the PolySpatial host.
    /// PolySpatial does not automatically detect VideoPlayer texture updates.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PolySpatialVideoRenderTextureUpdater : MonoBehaviour
    {
        RenderTexture m_TargetTexture;

        public RenderTexture TargetTexture => m_TargetTexture;

        public void Configure(RenderTexture targetTexture)
        {
            m_TargetTexture = targetTexture;
        }

        public static bool ShouldTransfer(RenderTexture targetTexture)
        {
            return targetTexture != null && targetTexture.IsCreated();
        }

        void LateUpdate()
        {
            if (!ShouldTransfer(m_TargetTexture))
                return;

            Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(m_TargetTexture);
        }

        void OnDisable()
        {
            m_TargetTexture = null;
        }
    }
}
