using System;
using System.Collections.Generic;
using Moodium.Interaction;
using Moodium.Reality;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Moodium
{
    [DisallowMultipleComponent]
    public sealed class ImageTrackingCapsuleSpawner : MonoBehaviour
    {
        [SerializeField] ARTrackedImageManager m_TrackedImageManager;
        [SerializeField] GameObject m_ChocolatePrefab;
        [SerializeField] Vector3 m_LocalPosition;
        [SerializeField] Vector3 m_LocalEulerAngles;

        readonly Dictionary<TrackableId, GameObject> m_Instances = new();
        readonly HashSet<TrackableId> m_Available = new();

        public event Action<ChocolateCapsuleInteraction> CapsuleAvailable;
        public event Action<ChocolateCapsuleInteraction> CapsuleUnavailable;

        public void Configure(ARTrackedImageManager manager, GameObject chocolatePrefab)
        {
            if (isActiveAndEnabled && m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
            m_TrackedImageManager = manager;
            m_ChocolatePrefab = chocolatePrefab;
            if (isActiveAndEnabled && m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }

        public void ClearRuntimeInstances()
        {
            foreach (var pair in m_Instances)
            {
                ReportUnavailable(pair.Key, pair.Value);
                if (pair.Value != null)
                    Destroy(pair.Value);
            }
            m_Instances.Clear();
            m_Available.Clear();
        }

        void OnEnable()
        {
            if (m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }

        void OnDisable()
        {
            if (m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
            foreach (var pair in m_Instances)
                ReportUnavailable(pair.Key, pair.Value);
        }

        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
        {
            foreach (var trackedImage in changes.added)
                ReportTracking(trackedImage.trackableId, trackedImage.transform, trackedImage.trackingState);
            foreach (var trackedImage in changes.updated)
                ReportTracking(trackedImage.trackableId, trackedImage.transform, trackedImage.trackingState);
            foreach (var removed in changes.removed)
                Remove(removed.Value.trackableId);
        }

        GameObject ReportTracking(TrackableId id, Transform anchor, TrackingState state)
        {
            if (state == TrackingState.None)
            {
                if (m_Instances.TryGetValue(id, out var hidden))
                {
                    ReportUnavailable(id, hidden);
                    if (hidden != null)
                        hidden.SetActive(false);
                }
                return hidden;
            }

            if (!m_Instances.TryGetValue(id, out var instance) || instance == null)
            {
                instance = TrackedCapsuleRuntimeFactory.Create(
                    m_ChocolatePrefab,
                    anchor,
                    m_LocalPosition,
                    Quaternion.Euler(m_LocalEulerAngles),
                    "Chocolate_Capsule (Tracked Image)");
                if (instance == null)
                    return null;
                m_Instances[id] = instance;
            }
            else
            {
                var follower = instance.GetComponent<TrackedObjectPoseFollower>();
                follower?.Configure(anchor, m_LocalPosition, Quaternion.Euler(m_LocalEulerAngles));
            }

            instance.SetActive(true);
            if (m_Available.Add(id))
                CapsuleAvailable?.Invoke(instance.GetComponent<ChocolateCapsuleInteraction>());
            return instance;
        }

        void Remove(TrackableId id)
        {
            if (!m_Instances.Remove(id, out var instance))
                return;
            ReportUnavailable(id, instance);
            if (instance != null)
                Destroy(instance);
        }

        void ReportUnavailable(TrackableId id, GameObject instance)
        {
            if (!m_Available.Remove(id) || instance == null)
                return;
            CapsuleUnavailable?.Invoke(instance.GetComponent<ChocolateCapsuleInteraction>());
        }
    }
}
