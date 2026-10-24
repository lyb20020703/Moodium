using System;
using System.Collections.Generic;
using Moodium.Flow;
using TMPro;
using UnityEngine;

namespace Moodium.MoodCheck
{
    public enum MoodCheckState { Hidden, Dragging, Confirming, Recommending, Comparing, Done }

    [DisallowMultipleComponent]
    public sealed class MoodCheckController : MonoBehaviour
    {
        [SerializeField] GameObject m_PanelPrefab;
        [SerializeField] Camera m_Camera;
        [SerializeField] Vector2 m_ComparisonTarget = new(.5f, -.1f);

        GameObject m_PanelInstance;
        MoodCheckAvailability m_Availability;
        MoodCheckPhase m_Phase;
        MoodInputMethod m_InputMethod;
        MoodCheckSample m_PreSample;
        MoodVisitMetrics m_Metrics;
        readonly Dictionary<MoodKind, MoodiumSpatialButton> m_MoodChipButtons = new();
        bool m_English;
        bool m_CompletionSent;
        bool m_SkipSent;

        public event Action<MoodCheckSample> Completed;
        public event Action Skipped;

        public MoodCheckState State { get; private set; } = MoodCheckState.Hidden;
        public MoodCheckPlate Plate { get; private set; }
        public MoodCheckSample PendingSample { get; private set; }
        public Transform PanelTransform => m_PanelInstance != null ? m_PanelInstance.transform : null;
        public bool IsVisible => m_PanelInstance != null && m_PanelInstance.activeSelf;

        public void Configure(GameObject panelPrefab, Camera camera)
        {
            m_PanelPrefab = panelPrefab;
            m_Camera = camera;
        }

        public void BeginPreCheck(MoodCheckAvailability availability, bool english)
        {
            Begin(MoodCheckPhase.Pre, english);
            m_Availability = availability;
            m_PreSample = null;
            m_Metrics = default;
            Plate.SetNormalizedPosition(Vector2.zero, true);
            SetTitle("plate.title.pre");
            ShowDraggingFooter();
        }

        public void BeginPostCheck(MoodCheckSample pre, MoodVisitMetrics metrics, bool english)
        {
            Begin(MoodCheckPhase.Post, english);
            m_PreSample = pre;
            m_Metrics = metrics;
            Plate.SetNormalizedPosition(pre != null ? pre.Position : Vector2.zero, true);
            SetTitle("plate.title.post");
            ShowDraggingFooter();
        }

        public void QuickSelect(MoodKind mood)
        {
            if (State is not (MoodCheckState.Dragging or MoodCheckState.Confirming))
                return;
            m_InputMethod = MoodInputMethod.Quick;
            Plate.SetNormalizedPosition(MoodClassifier.Preset(mood), false);
            EnterConfirming();
        }

        public void Adjust()
        {
            if (State != MoodCheckState.Confirming)
                return;
            State = MoodCheckState.Dragging;
            ShowDraggingFooter();
        }

        public void Confirm()
        {
            if (State != MoodCheckState.Confirming)
                return;
            PendingSample = BuildSample();
            if (m_Phase == MoodCheckPhase.Post)
            {
                State = MoodCheckState.Comparing;
                ShowComparison();
            }
            else
            {
                State = MoodCheckState.Recommending;
                ShowRecommendation(false);
            }
        }

        public void Retry()
        {
            if (State != MoodCheckState.Recommending)
                return;
            PendingSample = null;
            State = MoodCheckState.Dragging;
            ShowDraggingFooter();
        }

        public void AcceptRecommendation()
        {
            if (State != MoodCheckState.Recommending || m_CompletionSent)
                return;
            State = MoodCheckState.Done;
            SendCompleted();
            // The recommendation card is the final mood-check surface. Once the
            // user accepts it, return to world selection instead of leaving a
            // third compact recommendation-summary panel on screen.
            DismissImmediate();
        }

        public void ContinueComparison()
        {
            if (State != MoodCheckState.Comparing || m_CompletionSent)
                return;
            State = MoodCheckState.Done;
            SendCompleted();
        }

        public void Skip()
        {
            if (State == MoodCheckState.Hidden || m_SkipSent || m_CompletionSent)
                return;
            m_SkipSent = true;
            DismissPanel();
            State = MoodCheckState.Hidden;
            Skipped?.Invoke();
        }

        public void DismissImmediate()
        {
            DismissPanel();
            State = MoodCheckState.Hidden;
            PendingSample = null;
        }

        void Begin(MoodCheckPhase phase, bool english)
        {
            DismissPanel();
            m_Phase = phase;
            m_English = english;
            m_InputMethod = MoodInputMethod.Plate;
            m_CompletionSent = false;
            m_SkipSent = false;
            PendingSample = null;
            if (m_PanelPrefab == null)
                m_PanelPrefab = Resources.Load<GameObject>("Moodium/MoodCheck/MoodCheckPanel");
            m_PanelInstance = m_PanelPrefab != null ? Instantiate(m_PanelPrefab) : BuildFallbackPanel();
            m_PanelInstance.name = phase == MoodCheckPhase.Pre ? "Mood Check - Pre" : "Mood Check - Post";
            m_PanelInstance.transform.SetParent(transform, true);
            PlacePanel();
            var plateRoot = m_PanelInstance.transform.Find("Plate");
            Plate = plateRoot != null ? plateRoot.GetComponent<MoodCheckPlate>() : null;
            if (Plate == null && plateRoot != null)
            {
                Plate = plateRoot.gameObject.AddComponent<MoodCheckPlate>();
                Plate.Configure(.112f);
            }
            var handle = plateRoot != null ? plateRoot.GetComponent<MoodCheckPlateHandle>() : null;
            if (handle == null && plateRoot != null)
                handle = plateRoot.gameObject.AddComponent<MoodCheckPlateHandle>();
            if (handle != null)
            {
                handle.Configure(Plate);
                handle.Released += OnPlateReleased;
            }
            if (Plate != null)
            {
                Plate.Changed += OnPlateChanged;
                OnPlateChanged(Plate.NormalizedPosition, Plate.CurrentMood);
            }
            WireButtons();
            ApplyLanguage();
            State = MoodCheckState.Dragging;
        }

        void OnPlateReleased(Vector2 _)
        {
            if (State != MoodCheckState.Dragging)
                return;
            m_InputMethod = MoodInputMethod.Plate;
            EnterConfirming();
        }

        void OnPlateChanged(Vector2 _, MoodKind mood)
        {
            var copy = MoodCheckCopy.Get("mood." + mood.ToString().ToLowerInvariant(), m_English);
            var separator = copy.IndexOf(" · ", StringComparison.Ordinal);
            SetText("Readout/MoodName", separator >= 0 ? copy.Substring(0, separator) : copy);
            SetText("Readout/MoodDescription", separator >= 0 ? copy.Substring(separator + 3) : string.Empty);
            UpdateMoodChipSelection(mood);
        }

        void EnterConfirming()
        {
            State = MoodCheckState.Confirming;
            SetActive("Footer/Hint", false);
            SetActive("Footer/ConfirmTitle", true);
            SetActive("Footer/SkipButton", false);
            SetActive("Footer/AdjustButton", false);
            SetActive("Footer/ConfirmButton", true);
            SetText("Footer/ConfirmTitle", MoodCheckCopy.Format("confirm.title", m_English,
                ("mood", MoodName(Plate.CurrentMood))));
        }

        MoodCheckSample BuildSample()
        {
            var position = Plate.NormalizedPosition;
            var mood = MoodClassifier.Classify(position.x, position.y);
            return new MoodCheckSample
            {
                phase = m_Phase,
                valence = position.x,
                arousal = position.y,
                mood = mood,
                intensity = MoodClassifier.Intensity(position.x, position.y),
                inputMethod = m_InputMethod,
                recommendedWorldId = m_Phase == MoodCheckPhase.Pre
                    ? MoodClassifier.Recommend(mood, m_Availability.CandyAvailable, m_Availability.NatureAvailable)
                    : m_PreSample?.recommendedWorldId,
                timestampUtc = DateTime.UtcNow.ToString("O"),
                language = m_English ? "en" : "zh-CN",
                durationInWorldSeconds = m_Phase == MoodCheckPhase.Post ? m_Metrics.DurationInWorldSeconds : 0f,
                pinchCount = m_Phase == MoodCheckPhase.Post ? m_Metrics.PinchCount : 0
            };
        }

        void SendCompleted()
        {
            if (m_CompletionSent || PendingSample == null)
                return;
            m_CompletionSent = true;
            Completed?.Invoke(PendingSample);
        }

        void ShowDraggingFooter()
        {
            SetActive("PanelGlass", true);
            SetCoreVisible(true);
            SetActive("RecommendationCard", false);
            SetActive("ComparisonCard", false);
            SetActive("Footer/Hint", true);
            SetActive("Footer/ConfirmTitle", false);
            SetActive("Footer/RecommendationSummary", false);
            SetActive("Footer/SkipButton", false);
            SetActive("Footer/AdjustButton", false);
            SetActive("Footer/ConfirmButton", false);
            SetActive("Footer/RetryButton", false);
            SetActive("Footer/ContinueButton", false);
        }

        void ShowRecommendation(bool compact)
        {
            SetActive("PanelGlass", false);
            SetCoreVisible(false);
            SetActive("RecommendationCard", !compact);
            SetActive("Footer/Hint", false);
            SetActive("Footer/ConfirmTitle", false);
            SetActive("Footer/RecommendationSummary", compact);
            SetActive("Footer/SkipButton", false);
            SetActive("Footer/AdjustButton", false);
            SetActive("Footer/ConfirmButton", false);
            SetActive("Footer/RetryButton", false);
            SetActive("Footer/ContinueButton", false);
            SetText("Footer/ContinueButton/Label", MoodCheckCopy.Get("rec.cta", m_English));
            SetText("RecommendationCard/Badge", MoodCheckCopy.Format("rec.today", m_English,
                ("mood", MoodName(PendingSample.mood))));
            SetText("RecommendationCard/WorldName", WorldName(PendingSample.recommendedWorldId));
            SetText("RecommendationCard/Recommendation",
                MoodCheckCopy.Get("rec." + PendingSample.mood.ToString().ToLowerInvariant(), m_English));
            SetText("RecommendationCard/StickerText", MoodCheckCopy.Get("rec.sticker", m_English));
            SetText("RecommendationCard/Other", MoodCheckCopy.Get("rec.other", m_English));
            SetText("Footer/RecommendationSummary",
                $"Moodi 推荐 · {WorldName(PendingSample?.recommendedWorldId)}\n{MoodCheckCopy.Get("rec." + PendingSample?.mood.ToString().ToLowerInvariant(), m_English)}");
            var card = m_PanelInstance.transform.Find("RecommendationCard");
            if (card != null)
            {
                var view = card.GetComponent<MoodRecommendationView>() ?? card.gameObject.AddComponent<MoodRecommendationView>();
                var title = m_PanelInstance.transform.Find("Header/Title")?.GetComponent<TMP_Text>();
                view.Show(PendingSample, m_English, title != null ? title.font : TMP_Settings.defaultFontAsset,
                    AcceptRecommendation, Retry);
            }
        }

        void ShowComparison()
        {
            SetCoreVisible(false);
            SetActive("ComparisonCard", true);
            SetActive("Footer/Hint", false);
            SetActive("Footer/ConfirmTitle", false);
            SetActive("Footer/SkipButton", false);
            SetActive("Footer/AdjustButton", false);
            SetActive("Footer/ConfirmButton", false);
            SetActive("Footer/RetryButton", false);
            SetActive("Footer/ContinueButton", true);
            var comparison = MoodClassifier.Compare(m_PreSample != null ? m_PreSample.Position : Vector2.zero,
                PendingSample.Position, m_ComparisonTarget);
            SetText("ComparisonCard/Result", MoodCheckCopy.Get("compare." + comparison.ToString().ToLowerInvariant(), m_English));
            SetText("ComparisonCard/MoodTransition",
                $"{MoodCheckCopy.Get("compare.before", m_English)} {MoodName(m_PreSample?.mood ?? MoodKind.Neutral)} → " +
                $"{MoodCheckCopy.Get("compare.now", m_English)} {MoodName(PendingSample.mood)}");
            SetText("ComparisonCard/Metrics",
                $"{MoodCheckCopy.Get("compare.time", m_English)} {Mathf.Max(0f, m_Metrics.DurationInWorldSeconds) / 60f:0.#} min · " +
                $"{MoodCheckCopy.Get("compare.squeezes", m_English)} {m_Metrics.PinchCount}");
            SetText("ComparisonCard/Privacy", MoodCheckCopy.Get("compare.privacy", m_English));
        }

        void SetCoreVisible(bool visible)
        {
            SetActive("Header", visible);
            SetActive("Readout", visible);
            SetActive("Plate", visible);
            SetActive("AxisLabels", visible);
            SetActive("QuickChips", visible);
        }

        void PlacePanel()
        {
            var camera = m_Camera != null ? m_Camera : Camera.main;
            if (camera == null)
                return;
            var horizontal = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (horizontal.sqrMagnitude < .0001f)
                horizontal = Vector3.forward;
            horizontal.Normalize();
            m_PanelInstance.transform.position = camera.transform.position + horizontal * .75f + Vector3.down * .12f;
            m_PanelInstance.transform.rotation = Quaternion.LookRotation(horizontal, Vector3.up);
        }

        void WireButtons()
        {
            SetActive("Footer/SkipButton", false);
            SetActive("Footer/AdjustButton", false);
            ConfigureButton("Footer/ConfirmButton", Confirm);
            ConfigureButton("Footer/ContinueButton", m_Phase == MoodCheckPhase.Post ? ContinueComparison : AcceptRecommendation);
            var moods = new[] { MoodKind.Happy, MoodKind.Calm, MoodKind.Neutral, MoodKind.Low, MoodKind.Tense };
            foreach (var mood in moods)
            {
                var captured = mood;
                ConfigureButton($"QuickChips/{mood}Chip", () => QuickSelect(captured));
                var chip = m_PanelInstance.transform.Find($"QuickChips/{mood}Chip")?.GetComponent<MoodiumSpatialButton>();
                if (chip != null)
                    m_MoodChipButtons[mood] = chip;
            }
            UpdateMoodChipSelection(Plate != null ? Plate.CurrentMood : MoodKind.Neutral);
        }

        void UpdateMoodChipSelection(MoodKind selected)
        {
            foreach (var pair in m_MoodChipButtons)
                pair.Value?.SetSelected(pair.Key == selected);
        }

        void ConfigureButton(string path, Action action)
        {
            var target = m_PanelInstance.transform.Find(path);
            var button = target != null ? target.GetComponent<MoodiumSpatialButton>() : null;
            if (button == null) return;
            var label = target.Find("Label")?.GetComponent<TMP_Text>();
            var background = target.Find("Background")?.GetComponent<Renderer>();
            button.Configure(label, background, action);
        }

        void ApplyLanguage()
        {
            SetText("Header/Kicker", MoodCheckCopy.Get("plate.kicker", m_English));
            SetText("Footer/Hint", MoodCheckCopy.Get("plate.hint", m_English));
            SetText("Footer/SkipButton/Label", MoodCheckCopy.Get("skip", m_English));
            SetText("Footer/AdjustButton/Label", MoodCheckCopy.Get("confirm.adjust", m_English));
            SetText("Footer/ConfirmButton/Label", MoodCheckCopy.Get("confirm.yes", m_English));
            SetText("Footer/ContinueButton/Label", MoodCheckCopy.Get("compare.continue", m_English));
            SetText("AxisLabels/Up", MoodCheckCopy.Get("axis.up", m_English));
            SetText("AxisLabels/Down", MoodCheckCopy.Get("axis.down", m_English));
            SetText("AxisLabels/Left", MoodCheckCopy.Get("axis.left", m_English));
            SetText("AxisLabels/Right", MoodCheckCopy.Get("axis.right", m_English));
        }

        void SetTitle(string key) => SetText("Header/Title", MoodCheckCopy.Get(key, m_English));

        void SetText(string path, string value)
        {
            var target = m_PanelInstance != null ? m_PanelInstance.transform.Find(path) : null;
            var text = target != null ? target.GetComponent<TMP_Text>() : null;
            if (text != null) text.text = value ?? string.Empty;
        }

        void SetActive(string path, bool active)
        {
            var target = m_PanelInstance != null ? m_PanelInstance.transform.Find(path) : null;
            if (target != null) target.gameObject.SetActive(active);
        }

        string MoodName(MoodKind mood)
        {
            var value = MoodCheckCopy.Get("mood." + mood.ToString().ToLowerInvariant(), m_English);
            var separator = value.IndexOf(" · ", StringComparison.Ordinal);
            return separator >= 0 ? value.Substring(0, separator) : value;
        }

        string WorldName(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return string.Empty;
            if (m_English) return worldId == "candy" ? "Candy World" : "Nature World";
            return worldId == "candy" ? "糖果世界" : "自然世界";
        }

        GameObject BuildFallbackPanel()
        {
            Debug.LogWarning("[Mood Check] Panel prefab is missing; using a functional fallback.");
            var panel = new GameObject("MoodCheckPanel Fallback");
            var plate = new GameObject("Plate");
            plate.transform.SetParent(panel.transform, false);
            return panel;
        }

        void DismissPanel()
        {
            if (m_PanelInstance != null)
            {
                m_PanelInstance.SetActive(false);
                if (Application.isPlaying) Destroy(m_PanelInstance);
                else DestroyImmediate(m_PanelInstance);
            }
            m_PanelInstance = null;
            Plate = null;
        }

        void OnDestroy() => DismissPanel();
    }
}
