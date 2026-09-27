using UnityEngine;

namespace Moodium.Reality
{
    /// <summary>Pure, allocation-free crack classification helpers kept separate from XR callbacks for testability.</summary>
    public static class CandyWrapperCrackMath
    {
        public static bool IsLargeCluster(int contactCount, bool hasPalm, int minimumJointCountForLarge)
            => hasPalm || contactCount >= Mathf.Max(1, minimumJointCountForLarge);

        public static bool IsDuplicateSpawn(
            Vector3 point, Vector3 lastPoint, float now, float lastSpawnTime,
            float minimumDistance, float minimumTime)
        {
            return now - lastSpawnTime < minimumTime &&
                   Vector3.Distance(point, lastPoint) < Mathf.Max(0f, minimumDistance);
        }

        public static float TowardSurfaceVelocity(Vector3 velocity, Vector3 surfaceNormal)
        {
            if (surfaceNormal.sqrMagnitude < 0.000001f)
                return 0f;
            return Mathf.Max(0f, Vector3.Dot(velocity, -surfaceNormal.normalized));
        }

        public static float Impact01(float towardSurfaceVelocity, float lowVelocity, float highVelocity)
        {
            return Mathf.InverseLerp(Mathf.Max(0f, lowVelocity), Mathf.Max(lowVelocity + .0001f, highVelocity),
                Mathf.Max(0f, towardSurfaceVelocity));
        }
    }
}
