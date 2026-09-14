using System;
using System.Collections.Generic;
using Moodium.Flow;
using Moodium.Interaction;
using Moodium.Reality;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Moodium
{
    public sealed class TissueObjectTrackingSpawner : MonoBehaviour
    {
        public static event Action<ARTrackedObject, GameObject> TrackedTissueAvailable;
        public event Action<ARTrackedObject> TissueDetected;

        [SerializeField] ARTrackedObjectManager m_TrackedObjectManager;
        [SerializeField] GameObject m_ChocolatePrefab;
        [SerializeField] string m_ReferenceObjectName = "Tissue";
        [SerializeField] Vector3 m_LocalPosition;
        [SerializeField] Vector3 m_LocalEulerAngles;

        readonly Dictionary<TrackableId, GameObject> m_SpawnedChocolate = new();
        readonly HashSet<TrackableId> m_PendingDetections = new();
        bool m_DeferSpawning;

        public void Configure(ARTrackedObjectManager manager, GameObject chocolatePrefab)
        {
            m_TrackedObjectManager = manager;
            m_ChocolatePrefab = chocolatePrefab;
        }

        /// <summary>Allows the short Reality Enhancement guidance flow to finish before spawning.</summary>
        public void SetDeferredSpawning(bool defer)
        {
            m_DeferSpawning = defer;
            if (!defer)
                m_PendingDetections.Clear();
        }

        public GameObject SpawnDeferred(ARTrackedObject trackedObject)
        {
            if (trackedObject == null || !IsTissue(trackedObject))
                return null;
            if (m_SpawnedChocolate.TryGetValue(trackedObject.trackableId, out var existing))
                return existing;
            m_PendingDetections.Remove(trackedObject.trackableId);
            return SpawnChocolate(trackedObject);
        }

        public void ReleaseDeferredDetection(ARTrackedObject trackedObject)
        {
            if (trackedObject != null)
                m_PendingDetections.Remove(trackedObject.trackableId);
        }

        public void ClearRuntimeInstances()
        {
            foreach (var pair in m_SpawnedChocolate)
                if (pair.Value != null)
                    Destroy(pair.Value);
            m_SpawnedChocolate.Clear();
            m_PendingDetections.Clear();
        }

        void OnEnable()
        {
            if (m_TrackedObjectManager != null)
                m_TrackedObjectManager.trackablesChanged.AddListener(OnTrackedObjectsChanged);
        }

        void OnDisable()
        {
            if (m_TrackedObjectManager != null)
                m_TrackedObjectManager.trackablesChanged.RemoveListener(OnTrackedObjectsChanged);
        }

        void OnTrackedObjectsChanged(ARTrackablesChangedEventArgs<ARTrackedObject> changes)
        {
            foreach (var trackedObject in changes.added)
                HandleAdded(trackedObject);

            foreach (var trackedObject in changes.updated)
                HandleUpdated(trackedObject);

            foreach (var removedPair in changes.removed)
                RemoveChocolate(removedPair.Value.trackableId);
        }

        void HandleAdded(ARTrackedObject trackedObject)
        {
            if (!IsTissue(trackedObject) || m_SpawnedChocolate.ContainsKey(trackedObject.trackableId))
                return;

            if (m_DeferSpawning)
            {
                QueueDetection(trackedObject);
                return;
            }

            SpawnChocolate(trackedObject);
        }

        void QueueDetection(ARTrackedObject trackedObject)
        {
            if (!m_PendingDetections.Add(trackedObject.trackableId))
                return;
            Debug.Log(" Tissue detected ");
            Debug.Log("[Moodium Tracking] Tissue detected; waiting for the guidance flow to spawn the overlay.");
            TissueDetected?.Invoke(trackedObject);
        }

        GameObject SpawnChocolate(ARTrackedObject trackedObject)
        {
            if (m_ChocolatePrefab == null)
            {
                Debug.LogError("[Moodium Tracking] Chocolate_Capsule prefab is missing.");
                return null;
            }

            Debug.Log(" Tissue detected ");

            var instance = Instantiate(m_ChocolatePrefab, trackedObject.transform);
            instance.name = "Chocolate_Capsule (Tracked Tissue)";
            var localRotation = Quaternion.Euler(m_LocalEulerAngles);
            instance.transform.SetLocalPositionAndRotation(m_LocalPosition, localRotation);
            MoodiumRuntimeObjectSetup.Configure(
                instance,
                allowMove: false,
                allowScale: true,
                disablePhysics: true);
            var poseFollower = instance.GetComponent<TrackedObjectPoseFollower>();
            if (poseFollower == null)
                poseFollower = instance.AddComponent<TrackedObjectPoseFollower>();
            poseFollower.Configure(trackedObject.transform, m_LocalPosition, localRotation);
            var chocolateInteraction = instance.GetComponent<ChocolateCapsuleInteraction>();
            if (chocolateInteraction == null)
                chocolateInteraction = instance.AddComponent<ChocolateCapsuleInteraction>();
            chocolateInteraction.SetInteractionEnabled(true);
            chocolateInteraction.SetSpatialPointerEnabled(false);
            m_SpawnedChocolate.Add(trackedObject.trackableId, instance);
            TrackedTissueAvailable?.Invoke(trackedObject, instance);

            var animator = instance.GetComponentInChildren<Animator>(true);
            Debug.Log(
                "[Moodium Tracking] Chocolate spawned from " +
                "Assets/prefab/Chocolate_Capsule.prefab\n" +
                $"name={instance.transform.name}\n" +
                $"localScale={instance.transform.localScale}\n" +
                $"lossyScale={instance.transform.lossyScale}\n" +
                $"position={instance.transform.position}\n" +
                $"rotation={instance.transform.rotation}\n" +
                $"anchor={trackedObject.transform.name}\n" +
                $"parent={instance.transform.parent?.name}\n" +
                $"animator={(animator != null ? "present" : "missing")}\n" +
                $"controller={(animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "missing")}");
            HandleUpdated(trackedObject);
            return instance;
        }

        void HandleUpdated(ARTrackedObject trackedObject)
        {
            if (!m_SpawnedChocolate.TryGetValue(trackedObject.trackableId, out var instance))
            {
                if (trackedObject.trackingState != TrackingState.None)
                {
                    if (m_DeferSpawning)
                        QueueDetection(trackedObject);
                    else
                        HandleAdded(trackedObject);
                }
                return;
            }

            // Limited still provides a usable anchor pose. Keeping the enhancement visible
            // prevents a delayed scan flow from spawning and immediately hiding the capsule.
            var isTracking = trackedObject.trackingState != TrackingState.None;
            instance.SetActive(isTracking);
            if (isTracking)
            {
                var poseFollower = instance.GetComponent<TrackedObjectPoseFollower>();
                if (poseFollower == null)
                    poseFollower = instance.AddComponent<TrackedObjectPoseFollower>();
                poseFollower.Configure(
                    trackedObject.transform,
                    m_LocalPosition,
                    Quaternion.Euler(m_LocalEulerAngles));
                TrackedTissueAvailable?.Invoke(trackedObject, instance);
            }
        }

        bool IsTissue(ARTrackedObject trackedObject)
        {
            return trackedObject.referenceObject.name.Equals(
                m_ReferenceObjectName,
                System.StringComparison.OrdinalIgnoreCase);
        }

        void RemoveChocolate(TrackableId id)
        {
            m_PendingDetections.Remove(id);
            if (!m_SpawnedChocolate.Remove(id, out var instance))
                return;

            Destroy(instance);
        }
    }
}
