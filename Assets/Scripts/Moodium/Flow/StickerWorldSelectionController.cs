using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Moodium.Opening;

namespace Moodium.Flow
{
    /// <summary>
    /// Displays world-entry models for every recognized Moodium sticker while the
    /// opening video is in its post-tutorial selection loop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StickerWorldSelectionController : MonoBehaviour
    {
        [Header("Image Tracking")]
        [SerializeField] ARTrackedImageManager m_TrackedImageManager;
        [SerializeField] string m_CandyReferenceImageName = "CandyWorldPhoneTracking";
        [SerializeField] string m_NatureReferenceImageName = "NatureWorldPhoneTracking";

        [Header("World Entries")]
        [SerializeField] GameObject m_CandyWorldUiPrefab;
        [SerializeField] GameObject m_NatureWorldUiPrefab;
        [SerializeField] MoodiumWorldDefinition m_CandyWorld;
        [SerializeField] MoodiumWorldDefinition m_NatureWorld;
        [SerializeField] TMP_FontAsset m_ChineseFont;
        [SerializeField] TMP_FontAsset m_EnglishFont;
        [SerializeField] Vector3 m_EntryLocalPosition = new(0f, 0.12f, 0.10f);
        [SerializeField] Vector3 m_EntryLocalEulerAngles;
        [SerializeField] Vector3 m_EntryLocalScale = Vector3.one;
        [SerializeField] float m_ButtonLocalHeight = 0.18f;
        [SerializeField] float m_ButtonLocalForwardOffset = -0.03f;

        readonly Dictionary<TrackableId, StickerWorldEntry> m_Entries = new();
        OpeningVideoSession m_Session;
        TextMeshPro m_Instruction;
        bool m_Active;
        bool m_English;
        static Material s_RuntimeButtonMaterial;

        public event Action<StickerWorldEntry> WorldEntrySelected;
        public int ActiveEntryCount => m_Entries.Count;
        public bool IsSelectionActive => m_Active;

        public GameObject CandyWorldUiPrefab => m_CandyWorldUiPrefab;
        public GameObject NatureWorldUiPrefab => m_NatureWorldUiPrefab;
        public Vector3 EntryLocalPosition => m_EntryLocalPosition;

        public static Vector3 GetCompensatedLocalScale(float targetWorldScale, Vector3 parentScale) => new(
            SafeScaleDivide(targetWorldScale, parentScale.x),
            SafeScaleDivide(targetWorldScale, parentScale.y),
            SafeScaleDivide(targetWorldScale, parentScale.z));

        public void Configure(
            ARTrackedImageManager trackedImageManager,
            GameObject candyWorldUiPrefab,
            GameObject natureWorldUiPrefab,
            MoodiumWorldDefinition candyWorld,
            MoodiumWorldDefinition natureWorld)
        {
            if (m_Active && m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
            m_TrackedImageManager = trackedImageManager;
            m_CandyWorldUiPrefab = candyWorldUiPrefab;
            m_NatureWorldUiPrefab = natureWorldUiPrefab;
            m_CandyWorld = candyWorld;
            m_NatureWorld = natureWorld;
            if (m_Active && m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }

        public void ConfigureForTests(
            GameObject candyWorldUiPrefab,
            GameObject natureWorldUiPrefab,
            MoodiumWorldDefinition candyWorld,
            MoodiumWorldDefinition natureWorld)
        {
            Configure(null, candyWorldUiPrefab, natureWorldUiPrefab, candyWorld, natureWorld);
        }

        public void ConfigureFonts(TMP_FontAsset chineseFont, TMP_FontAsset englishFont)
        {
            m_ChineseFont = chineseFont;
            m_EnglishFont = englishFont;
        }

        public void Begin(OpeningVideoSession session)
        {
            m_Session = session;
            m_English = session != null && session.English;
            if (m_Active)
                return;
            m_Active = true;
            CreateOrUpdateInstruction();
            SeekSelectionLoopStart();
            if (m_TrackedImageManager != null)
            {
                m_TrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
                RefreshTrackedImages();
                Debug.Log($"[Moodium Sticker Selection] Subscribed before Image Tracking is enabled. Existing anchors: {m_TrackedImageManager.trackables.count}.");
            }
        }

        /// <summary>
        /// Rehydrates world entries from ARFoundation's current trackable collection.
        /// visionOS can publish an image anchor before a runtime listener is attached,
        /// so the app-flow calls this over the first few enabled frames as a safe catch-up.
        /// </summary>
        public void RefreshTrackedImages()
        {
            if (!m_Active || m_TrackedImageManager == null)
                return;

            foreach (var image in m_TrackedImageManager.trackables)
                HandleTrackedImage(image);
        }

        public void End()
        {
            if (m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
            m_Active = false;
            m_Session = null;
            if (m_Instruction != null)
                Destroy(m_Instruction.gameObject);
            m_Instruction = null;
            foreach (var entry in m_Entries.Values)
                if (entry.Root != null)
                    Destroy(entry.Root);
            m_Entries.Clear();
        }

        void OnDestroy()
        {
            End();
        }

        void Update()
        {
            if (!m_Active || m_Session?.Player == null)
                return;
            var player = m_Session.Player;
            var loop = OpeningVideoTimeline.StickerSelectionLoop;
            var frame = player.frame >= 0
                ? player.frame
                : (long)Math.Round(player.time * OpeningVideoTimeline.FramesPerSecond);
            if (frame >= loop.EndFrame || !player.isPlaying)
                SeekSelectionLoopStart();
        }

        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
        {
            Debug.Log($"[Moodium Sticker Selection] Image update received: added={changes.added.Count}, updated={changes.updated.Count}, removed={changes.removed.Count}.");
            foreach (var image in changes.added)
                HandleTrackedImage(image);
            foreach (var image in changes.updated)
                HandleTrackedImage(image);
            foreach (var image in changes.removed)
                RemoveEntry(image.Value.trackableId);
        }

        void HandleTrackedImage(ARTrackedImage image)
        {
            Debug.Log($"[Moodium Sticker Selection] Anchor '{image.referenceImage.name}' state={image.trackingState}, id={image.trackableId}.");
            HandleTrackedImageUpdate(image.trackableId, image.referenceImage.name, image.transform, image.trackingState);
        }

        public void HandleTrackedImageUpdate(
            TrackableId trackableId,
            string referenceImageName,
            Transform anchor,
            TrackingState trackingState)
        {
            if (!m_Active || !TryGetDefinition(referenceImageName, out var prefab, out var world))
            {
                if (m_Active)
                    Debug.LogWarning($"[Moodium Sticker Selection] Ignored image '{referenceImageName}'. No configured Moodium world prefab/definition was available.");
                return;
            }

            if (trackingState == TrackingState.None)
            {
                if (m_Entries.TryGetValue(trackableId, out var hidden) && hidden.Root != null)
                    hidden.Root.SetActive(false);
                return;
            }

            if (!m_Entries.TryGetValue(trackableId, out var entry) || entry.Root == null)
            {
                var root = Instantiate(prefab, anchor);
                root.name = $"Sticker World Entry - {world.DisplayName}";
                root.transform.localPosition = m_EntryLocalPosition;
                root.transform.localRotation = Quaternion.Euler(m_EntryLocalEulerAngles);
                root.transform.localScale = m_EntryLocalScale;
                root.AddComponent<StickerWorldEntryUprightFollower>()
                    .Configure(anchor, m_EntryLocalPosition, m_EntryLocalScale);
                CreateEnterButton(root.transform, trackableId);
                entry = new StickerWorldEntry(trackableId, referenceImageName, world, anchor, root);
                m_Entries[trackableId] = entry;
                Debug.Log($"[Moodium Sticker Selection] Spawned '{world.DisplayName}' entry for '{referenceImageName}' ({trackableId}).");
            }
            else if (entry.Root.transform.parent != anchor)
            {
                entry.Root.transform.SetParent(anchor, false);
                entry.Root.transform.localPosition = m_EntryLocalPosition;
                entry.Root.transform.localRotation = Quaternion.Euler(m_EntryLocalEulerAngles);
                entry.Root.transform.localScale = m_EntryLocalScale;
                var follower = entry.Root.GetComponent<StickerWorldEntryUprightFollower>() ??
                    entry.Root.AddComponent<StickerWorldEntryUprightFollower>();
                follower.Configure(anchor, m_EntryLocalPosition, m_EntryLocalScale);
            }

            entry.Root.SetActive(true);
        }

        public StickerWorldEntry GetEntry(TrackableId trackableId)
        {
            m_Entries.TryGetValue(trackableId, out var entry);
            return entry;
        }

        public void SelectEntry(TrackableId trackableId)
        {
            if (!m_Active || !m_Entries.TryGetValue(trackableId, out var entry))
                return;
            WorldEntrySelected?.Invoke(entry);
        }

        void RemoveEntry(TrackableId trackableId)
        {
            if (!m_Entries.Remove(trackableId, out var entry) || entry.Root == null)
                return;
            Destroy(entry.Root);
        }

        bool TryGetDefinition(string referenceImageName, out GameObject prefab, out MoodiumWorldDefinition world)
        {
            if (MoodiumImageReferenceName.Matches(referenceImageName, m_CandyReferenceImageName))
            {
                prefab = m_CandyWorldUiPrefab;
                world = m_CandyWorld;
            }
            else if (MoodiumImageReferenceName.Matches(referenceImageName, m_NatureReferenceImageName))
            {
                prefab = m_NatureWorldUiPrefab;
                world = m_NatureWorld;
            }
            else
            {
                prefab = null;
                world = null;
                return false;
            }

            return prefab != null && world != null;
        }

        void CreateEnterButton(Transform parent, TrackableId trackableId)
        {
            var buttonRoot = new GameObject("Enter World Button", typeof(BoxCollider));
            buttonRoot.name = "Enter World Button";
            buttonRoot.transform.SetParent(parent, false);
            buttonRoot.transform.localPosition = new Vector3(0f, m_ButtonLocalHeight, m_ButtonLocalForwardOffset);
            var collider = buttonRoot.GetComponent<BoxCollider>();
            collider.size = new Vector3(0.18f, 0.065f, 0.035f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Button Visual";
            visual.transform.SetParent(buttonRoot.transform, false);
            visual.transform.localScale = new Vector3(0.11f, 0.035f, 0.012f);
            var renderer = visual.GetComponent<Renderer>();
            renderer.enabled = false;
            renderer.sharedMaterial = GetRuntimeButtonMaterial();
            var buttonColor = new Color(0.08f, 0.15f, 0.32f, 0.94f);
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_BaseColor", buttonColor);
            propertyBlock.SetColor("_Color", buttonColor);
            renderer.SetPropertyBlock(propertyBlock);

            var labelRoot = new GameObject("Button Label", typeof(TextMeshPro));
            labelRoot.transform.SetParent(buttonRoot.transform, false);
            labelRoot.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            labelRoot.transform.localRotation = Quaternion.identity;
            labelRoot.transform.localScale = Vector3.one * 0.03f;
            var label = labelRoot.GetComponent<TextMeshPro>();
            label.text = m_English ? "Enter" : "进入世界";
            label.font = m_English ? m_EnglishFont : m_ChineseFont;
            label.fontSize = 6f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.outlineWidth = 0.08f;
            label.outlineColor = new Color(0.03f, 0.04f, 0.10f, 0.9f);
            label.rectTransform.sizeDelta = new Vector2(8f, 2.5f);

            var spatialButton = buttonRoot.AddComponent<MoodiumSpatialButton>();
            spatialButton.Configure(label, renderer, () => SelectEntry(trackableId));
        }

        void CreateOrUpdateInstruction()
        {
            if (m_Session?.DisplayRoot == null)
                return;
            if (m_Instruction == null)
            {
                var root = new GameObject("Sticker Selection Instruction", typeof(TextMeshPro));
                root.transform.SetParent(m_Session.DisplayRoot, false);
                root.transform.localPosition = new Vector3(0f, -0.36f, -0.04f);
                root.transform.localScale = GetCompensatedLocalScale(0.045f, m_Session.DisplayRoot.localScale);
                m_Instruction = root.GetComponent<TextMeshPro>();
                m_Instruction.font = m_English ? m_EnglishFont : m_ChineseFont;
                m_Instruction.fontSize = 7f;
                m_Instruction.alignment = TextAlignmentOptions.Center;
                m_Instruction.rectTransform.sizeDelta = new Vector2(24f, 4f);
                m_Instruction.color = Color.white;
                m_Instruction.outlineWidth = 0.08f;
                m_Instruction.outlineColor = new Color(0.08f, 0.02f, 0.16f, 0.85f);
            }
            m_Instruction.text = m_English ? "Look at the sticker" : "请对准贴纸";
        }

        void SeekSelectionLoopStart()
        {
            if (m_Session?.Player == null)
                return;
            m_Session.Player.time = OpeningVideoTimeline.FrameToSeconds(
                OpeningVideoTimeline.StickerSelectionLoop.StartFrame);
            m_Session.Player.Play();
        }

        static float SafeScaleDivide(float value, float divisor) =>
            Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;

        static Material GetRuntimeButtonMaterial()
        {
            if (s_RuntimeButtonMaterial != null)
                return s_RuntimeButtonMaterial;
            s_RuntimeButtonMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"))
            { name = "Moodium Runtime Entry Button" };
            return s_RuntimeButtonMaterial;
        }
    }
}
