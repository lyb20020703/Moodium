using UnityEngine;
using Moodium.Reality;

namespace Moodium.CandyWorld
{
    public sealed class CandyProjectile : MonoBehaviour
    {
        [SerializeField, Min(3f)] float m_MaxLifetime = 20f;
        [SerializeField, Range(0.1f, 1f)] float m_ColliderScale = 0.35f;

        Collider[] m_Colliders;

        public Collider[] Colliders => m_Colliders ?? System.Array.Empty<Collider>();

        public void Launch(Vector3 velocity, float scaleMultiplier)
        {
            transform.localScale *= scaleMultiplier;
            EnsureCollider();
            m_Colliders = GetComponentsInChildren<Collider>(true);
            TightenColliders();
            var landingFeedback = GetComponent<CandyPhysicsObject>();
            if (landingFeedback == null)
                landingFeedback = gameObject.AddComponent<CandyPhysicsObject>();
            landingFeedback.ConfigureFromPrefabFeedback();
            var body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.mass = 0.08f;
            body.linearDamping = 0.04f;
            body.maxLinearVelocity = 3f;
            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * 5f;
            Destroy(gameObject, m_MaxLifetime);
        }

        public void IgnoreCollisionsWith(Collider[] others)
        {
            if (others == null)
                return;
            foreach (var own in Colliders)
            foreach (var other in others)
            {
                if (own != null && other != null && own != other)
                    Physics.IgnoreCollision(own, other, true);
            }
        }

        void TightenColliders()
        {
            foreach (var collider in Colliders)
            {
                switch (collider)
                {
                    case BoxCollider box:
                        box.size *= m_ColliderScale;
                        break;
                    case SphereCollider sphere:
                        sphere.radius *= m_ColliderScale;
                        break;
                    case CapsuleCollider capsule:
                        capsule.radius *= m_ColliderScale;
                        capsule.height *= m_ColliderScale;
                        break;
                }
            }
        }

        void EnsureCollider()
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            if (colliders.Length > 0)
            {
                foreach (var collider in colliders)
                {
                    collider.enabled = true;
                    if (collider is MeshCollider meshCollider)
                        meshCollider.convex = true;
                }
                return;
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            var colliderToAdd = gameObject.AddComponent<BoxCollider>();
            colliderToAdd.center = transform.InverseTransformPoint(bounds.center);
            var scale = transform.lossyScale;
            colliderToAdd.size = new Vector3(
                Divide(bounds.size.x, scale.x),
                Divide(bounds.size.y, scale.y),
                Divide(bounds.size.z, scale.z));
        }

        static float Divide(float value, float divisor) =>
            Mathf.Abs(divisor) > 0.0001f ? value / Mathf.Abs(divisor) : value;
    }
}
