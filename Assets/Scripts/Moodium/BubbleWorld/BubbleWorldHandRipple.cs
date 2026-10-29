using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using Moodium.Interaction;

namespace Moodium.NatureWorld
{
    // One tracking sampler per active bubble world; drives the original splats, not extra particles.
    public sealed class BubbleWorldHandRipple : MonoBehaviour
    {
        readonly Vector3[] previous = new Vector3[2];
        readonly Vector3[] smoothed = new Vector3[2];
        readonly bool[] tracked = new bool[2];
        BubbleWorldAmbientWind[] winds;
        XRHandSubsystem hands;
        XROrigin origin;
        Camera viewer;

        public void Initialize(string folder, Camera camera)
        {
            viewer = camera;
            winds = transform.parent.GetComponentsInChildren<BubbleWorldAmbientWind>(true);
            origin = FindFirstObjectByType<XROrigin>();
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (dt <= 0 || dt > .1f) { ResetTracking(); return; }
            if (viewer == null) viewer = Camera.main;
            if (viewer == null) { ResetTracking(); return; }
            if (hands == null || !hands.running)
                hands = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            if (hands == null || !hands.running) { ResetTracking(); return; }
            if (origin == null) origin = FindFirstObjectByType<XROrigin>();
            Sample(hands.leftHand, 0, dt);
            Sample(hands.rightHand, 1, dt);
        }

        void Sample(XRHand hand, int index, float dt)
        {
            if (!hand.isTracked || !hand.GetJoint(XRHandJointID.Palm).TryGetPose(out var pose))
            { tracked[index] = false; smoothed[index] = Vector3.zero; return; }
            var world = PalmPressGestureController.TransformTrackingPose(pose,
                origin != null ? origin.Origin.transform : null).position;
            var velocity = tracked[index] ? (world - previous[index]) / dt : Vector3.zero;
            previous[index] = world;
            tracked[index] = true;
            // Reject reacquisition jumps instead of interpreting them as strong gestures.
            if (velocity.magnitude > 3f) { smoothed[index] = Vector3.zero; return; }
            smoothed[index] = Vector3.Lerp(smoothed[index], velocity, 1 - Mathf.Exp(-12f * dt));
            foreach (var wind in winds)
                if (wind != null && wind.isActiveAndEnabled)
                    wind.ApplyHand(world, smoothed[index], viewer.transform.position, dt);
        }

        void ResetTracking()
        {
            for (var i = 0; i < 2; i++) { tracked[i] = false; smoothed[i] = Vector3.zero; }
        }
        void OnDisable() { ResetTracking(); }
    }
}
