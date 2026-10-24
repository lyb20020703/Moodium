using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Moodium.Opening;
using Moodium.MoodCheck;
using Moodium.UI;

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
        [SerializeField] GameObject m_MoodiMascotPrefab;
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
        MoodiumSpatialButton m_MoodCheckButton;
        MoodCheckSample m_MoodCheckSummary;
        MoodCheckContext m_MoodCheckContext;
        Material m_EntryGlassMaterial;
        string m_RecommendedWorldId;
        bool m_Active;
        bool m_English;

        public event Action<StickerWorldEntry> WorldEntrySelected;
        public event Action MoodCheckRequested;
        public int ActiveEntryCount => m_Entries.Count;
        public bool IsSelectionActive => m_Active;

        public GameObject CandyWorldUiPrefab => m_CandyWorldUiPrefab;
        public GameObject NatureWorldUiPrefab => m_NatureWorldUiPrefab;
        public Vector3 EntryLocalPosition => m_EntryLocalPosition;
        public MoodiumSpatialButton MoodCheckButton => m_MoodCheckButton;

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

        public void ConfigureMascotPrefab(GameObject prefab) => m_MoodiMascotPrefab = prefab;

        public void ConfigureMoodCheck(MoodCheckContext context) => m_MoodCheckContext = context;

        public void ConfigureEntryGlassMaterial(Material material) => m_EntryGlassMaterial = material;

        public void SetMoodCheckSummary(MoodCheckSample sample)
        {
            m_MoodCheckSummary = sample;
            m_RecommendedWorldId = sample?.recommendedWorldId;
            CreateOrUpdateMoodCheckButton();
            UpdateAllRecommendationTags();
        }

        public void SetRecommendedWorld(string worldId)
        {
            m_RecommendedWorldId = worldId;
            UpdateAllRecommendationTags();
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
            if (m_MoodCheckButton != null)
                Destroy(m_MoodCheckButton.gameObject);
            m_MoodCheckButton = null;
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
                MoodiSelectionMascotController.AttachTo(root);
                CreateEnterButton(root.transform, trackableId, world);
                entry = new StickerWorldEntry(trackableId, referenceImageName, world, anchor, root);
                m_Entries[trackableId] = entry;
                UpdateRecommendationTag(entry);
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

        void CreateEnterButton(Transform parent, TrackableId trackableId, MoodiumWorldDefinition world)
        {
            var buttonRoot = new GameObject("Enter World Slider");
            buttonRoot.transform.SetParent(parent, false);
            buttonRoot.transform.localPosition = new Vector3(0f, m_ButtonLocalHeight, m_ButtonLocalForwardOffset);
            var isNature = world != null && string.Equals(world.WorldId, "nature", StringComparison.OrdinalIgnoreCase);
            var slider = buttonRoot.AddComponent<GlassSlideToEnter>();
            slider.Configure(
                EntryButtonLabel(world),
                isNature ? GlassSlideToEnter.Theme.Nature : GlassSlideToEnter.Theme.Candy,
                m_English ? m_EnglishFont : m_ChineseFont,
                m_EntryGlassMaterial,
                m_English ? "Entering…" : "正在进入…");
            slider.Completed += () => SelectEntry(trackableId);
        }

        string EntryButtonLabel(MoodiumWorldDefinition world)
        {
            if (world == null)
                return m_English ? "Enter" : "进入世界";
            if (m_English)
                return $"Enter {world.DisplayName}";
            var displayName = world.WorldId switch
            {
                "candy" => "糖果世界",
                "nature" => "自然世界",
                _ => world.DisplayName
            };
            return $"进入{displayName}";
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
                if (Application.isPlaying)
                {
                    m_Instruction.outlineWidth = 0.08f;
                    m_Instruction.outlineColor = new Color(0.08f, 0.02f, 0.16f, 0.85f);
                }
            }
            m_Instruction.text = m_English ? "Look at the sticker" : "请对准贴纸";
            CreateOrUpdateMoodCheckButton();
        }

        void CreateOrUpdateMoodCheckButton()
        {
            if (m_Session?.DisplayRoot == null)
                return;
            if (m_MoodCheckButton == null)
            {
                var root = new GameObject("Mood Check Entry", typeof(BoxCollider));
                root.transform.SetParent(m_Session.DisplayRoot, false);
                root.transform.localPosition = new Vector3(0f, .34f, -.04f);
                root.transform.localScale = GetCompensatedLocalScale(.045f, m_Session.DisplayRoot.localScale);
                root.GetComponent<BoxCollider>().size = new Vector3(12f, 2.2f, .8f);
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Button Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(4.25f, 1.05f, .2f);
                var primitiveCollider = visual.GetComponent<Collider>();
                if (Application.isPlaying) Destroy(primitiveCollider);
                else DestroyImmediate(primitiveCollider);
                var renderer = visual.GetComponent<Renderer>();
                renderer.enabled = false;
                var buttonColor = new Color(0.08f, 0.15f, 0.32f, 0.94f);
                var propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor("_BaseColor", buttonColor);
                propertyBlock.SetColor("_Color", buttonColor);
                renderer.SetPropertyBlock(propertyBlock);
                var labelRoot = new GameObject("Label", typeof(TextMeshPro));
                labelRoot.transform.SetParent(root.transform, false);
                labelRoot.transform.localPosition = new Vector3(0f, 0f, -.45f);
                var label = labelRoot.GetComponent<TextMeshPro>();
                label.font = m_English ? m_EnglishFont : m_ChineseFont;
                label.fontSize = 7f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.rectTransform.sizeDelta = new Vector2(8f, 2f);
                label.outlineWidth = 0.04f;
                label.outlineColor = new Color(0f, 0f, 0f, .35f);
                m_MoodCheckButton = root.AddComponent<MoodiumSpatialButton>();
                m_MoodCheckButton.Configure(label, null, () => MoodCheckRequested?.Invoke());
            }
            var text = MoodCheckCopy.Get("prompt.entry", m_English);
            if (m_MoodCheckSummary != null)
            {
                var mood = ShortMoodName(m_MoodCheckSummary.mood);
                var world = WorldName(m_MoodCheckSummary.recommendedWorldId);
                text = m_English ? $"Now: {mood} · {world} recommended" : $"现在：{mood} · 推荐{world}";
            }
            if (m_MoodCheckButton.Label != null)
                m_MoodCheckButton.Label.text = text;
        }

        void UpdateAllRecommendationTags()
        {
            foreach (var entry in m_Entries.Values)
                UpdateRecommendationTag(entry);
        }

        void UpdateRecommendationTag(StickerWorldEntry entry)
        {
            if (entry?.Root == null)
                return;
            var existing = entry.Root.transform.Find("Moodi Recommendation Tag");
            var matches = entry.World != null && !string.IsNullOrEmpty(m_RecommendedWorldId) &&
                          string.Equals(entry.World.WorldId, m_RecommendedWorldId, StringComparison.OrdinalIgnoreCase);
            if (!matches)
            {
                if (existing != null) Destroy(existing.gameObject);
                return;
            }
            if (existing != null)
                return;
            var tag = new GameObject("Moodi Recommendation Tag", typeof(TextMeshPro));
            tag.transform.SetParent(entry.Root.transform, false);
            tag.transform.localPosition = new Vector3(0f, m_ButtonLocalHeight + .055f, m_ButtonLocalForwardOffset);
            tag.transform.localScale = Vector3.one;
            var label = tag.GetComponent<TextMeshPro>();
            label.text = "★ " + MoodCheckCopy.Get("tag.pick", m_English);
            label.font = m_English ? m_EnglishFont : m_ChineseFont;
            label.fontSize = .025f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 1f, 1f, .95f);
            label.rectTransform.sizeDelta = new Vector2(.18f, .025f);
            tag.AddComponent<MoodRecommendationTagPulse>();
        }

        string ShortMoodName(MoodKind mood)
        {
            var value = MoodCheckCopy.Get("mood." + mood.ToString().ToLowerInvariant(), m_English);
            var separator = value.IndexOf(" · ", StringComparison.Ordinal);
            return separator >= 0 ? value.Substring(0, separator) : value;
        }

        string WorldName(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return m_English ? "either world" : "任一世界";
            if (m_English) return worldId == "candy" ? "Candy World" : "Nature World";
            return worldId == "candy" ? "糖果世界" : "自然世界";
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

    }
}
