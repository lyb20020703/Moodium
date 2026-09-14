using UnityEngine;
using Moodium.Interaction;

namespace Moodium.Flow
{
    public static class MoodiumRuntimeObjectSetup
    {
        public static MoodiumManipulable Configure(
            GameObject instance,
            bool allowMove,
            bool allowScale,
            bool disablePhysics)
        {
            if (instance == null)
                return null;

            if (disablePhysics)
            {
                foreach (var body in instance.GetComponentsInChildren<Rigidbody>(true))
                {
                    body.useGravity = false;
                    body.isKinematic = true;
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }

            EnsureCollider(instance);

            var manipulable = instance.GetComponent<MoodiumManipulable>();
            if (manipulable == null)
                manipulable = instance.AddComponent<MoodiumManipulable>();
            manipulable.Configure(allowMove, allowScale);

            if (disablePhysics && instance.GetComponent<MoodiumObjectInteraction>() == null)
                instance.AddComponent<MoodiumObjectInteraction>();
            if (disablePhysics && instance.GetComponent<SoftTouchFeedback>() == null)
                instance.AddComponent<SoftTouchFeedback>();
            return manipulable;
        }

        static void EnsureCollider(GameObject instance)
        {
            if (instance.GetComponentInChildren<Collider>(true) != null)
                return;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var scale = instance.transform.lossyScale;
            var collider = instance.AddComponent<BoxCollider>();
            collider.center = instance.transform.InverseTransformPoint(bounds.center);
            collider.size = new Vector3(
                SafeDivide(bounds.size.x, scale.x),
                SafeDivide(bounds.size.y, scale.y),
                SafeDivide(bounds.size.z, scale.z));
        }

        static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / Mathf.Abs(divisor) : value;
        }
    }
}
