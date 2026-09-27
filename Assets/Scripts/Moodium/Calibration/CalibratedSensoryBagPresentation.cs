using UnityEngine;
using Moodium.Flow;
using Moodium.Interaction;

namespace Moodium.Calibration
{
    [DisallowMultipleComponent]
    public sealed class CalibratedSensoryBagPresentation : MonoBehaviour
    {
        Transform m_Anchor;
        SensoryBagCalibrationData m_Data;
        public Transform VisualRoot { get; private set; }
        public Transform InteractionRoot { get; private set; }
        public SensoryBagInteractionVolume InteractionVolume { get; private set; }
        public SensoryBagCalibrationData CurrentCalibration => m_Data;
        public Vector3 CenterWorldPosition => transform.position;
        public Quaternion AnchorOrientation => m_Anchor != null ? m_Anchor.rotation : transform.rotation;

        public void Configure(Transform anchor, SensoryBagCalibrationData data, GameObject visualPrefab)
        {
            m_Anchor = anchor;
            m_Data = data;
            EnsureRoots();
            ClearVisual();
            if (visualPrefab != null)
            {
                var visual = Instantiate(visualPrefab, VisualRoot);
                visual.name = visualPrefab.name;
                FitVisualToCalibration(visual);
                MoodiumRuntimeObjectSetup.Configure(visual, allowMove: false, allowScale: false, disablePhysics: true);
                if (visual.GetComponent<SoftTouchDeformationController>() == null)
                    visual.AddComponent<SoftTouchDeformationController>();
                var capsule = visual.GetComponent<ChocolateCapsuleInteraction>() ?? visual.AddComponent<ChocolateCapsuleInteraction>();
                capsule.SetInteractionEnabled(true);
                // Keep the established gaze/pinch path active. Hand-collision remains
                // available as an additional route, rather than replacing it.
                capsule.SetSpatialPointerEnabled(true);
                if (visual.GetComponent<TrackedCapsuleHandCollision>() == null)
                    visual.AddComponent<TrackedCapsuleHandCollision>();
            }
            InteractionVolume.Configure(data.Width, data.Height, data.Depth);
            SyncNow();
            SetTrackingVisible(true);
        }

        public void SetTrackingVisible(bool visible)
        {
            if (VisualRoot != null) VisualRoot.gameObject.SetActive(visible);
            if (InteractionRoot != null) InteractionRoot.gameObject.SetActive(visible);
        }

        public void SyncNow()
        {
            if (m_Anchor == null) return;
            transform.SetPositionAndRotation(
                m_Anchor.TransformPoint(new Vector3(m_Data.CenterOffset.x, m_Data.PlaneLocalOffset.y, m_Data.CenterOffset.y)),
                // The confirmed model must occupy the exact same X/Z bag plane as
                // the calibration frame. Keeping it anchor-only made Width/Height
                // scale against the prefab's X/Y plane instead, rotating Candy's
                // long edge and breaking both worlds' calibrated proportions.
                m_Anchor.rotation * m_Data.LocalOrientation);
            // Image tracking and the calibration frame own the interaction-plane rotation.
            // The visible CandySoft/NatureSoft prefab must *not* inherit that correction:
            // its authored transform is already the approved world-facing pose. Keeping
            // this root at world identity preserves that pose exactly while the parent
            // still follows the tracked anchor's calibrated center position.
            if (VisualRoot != null)
                VisualRoot.rotation = Quaternion.identity;
        }

        void EnsureRoots()
        {
            if (VisualRoot == null)
            {
                VisualRoot = new GameObject("VisualRoot").transform;
                VisualRoot.SetParent(transform, false);
            }
            if (InteractionRoot == null)
            {
                InteractionRoot = new GameObject("InteractionRoot").transform;
                InteractionRoot.SetParent(transform, false);
                InteractionVolume = InteractionRoot.gameObject.AddComponent<SensoryBagInteractionVolume>();
            }
        }

        void ClearVisual()
        {
            for (var i = VisualRoot.childCount - 1; i >= 0; i--)
                Destroy(VisualRoot.GetChild(i).gameObject);
            VisualRoot.localScale = Vector3.one;
        }

        void FitVisualToCalibration(GameObject visual)
        {
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            var bounds = CalculateLocalRendererBounds(visual.transform);
            // CandySoft/NatureSoft are authored as tabletop bags: X/Z is their
            // visible bag face, and Y is their vertical depth/detail axis. The
            // calibration frame is also an X/Z table plane. Mapping Height onto
            // Y made a wide bag become a tall, narrow slab on-device.
            VisualRoot.localScale = CalculateTablePlaneFitScale(m_Data.Width, m_Data.Height, bounds);
        }

        /// <summary>
        /// Converts the two-dimensional calibration frame to the authored bag
        /// plane. Kept public for the regression test because both Candy and
        /// Nature use this common presentation component.
        /// </summary>
        public static Vector3 CalculateTablePlaneFitScale(float calibratedWidth, float calibratedHeight, Bounds prefabBounds)
        {
            var authoredWidth = Mathf.Max(.0001f, prefabBounds.size.x);
            var authoredHeight = Mathf.Max(.0001f, prefabBounds.size.z);
            return new Vector3(calibratedWidth / authoredWidth, 1f, calibratedHeight / authoredHeight);
        }

        static Bounds CalculateLocalRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var result = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var renderer in renderers)
            {
                var local = root.InverseTransformPoint(renderer.bounds.center);
                var extents = root.InverseTransformVector(renderer.bounds.extents);
                var bounds = new Bounds(local, new Vector3(Mathf.Abs(extents.x), Mathf.Abs(extents.y), Mathf.Abs(extents.z)) * 2f);
                if (hasBounds) result.Encapsulate(bounds); else { result = bounds; hasBounds = true; }
            }
            return hasBounds ? result : new Bounds(Vector3.zero, Vector3.one);
        }

        void LateUpdate() => SyncNow();
    }
}
