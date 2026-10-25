using System.Collections.Generic;
using Moodium.Audio;
using Moodium.Interaction;
using Moodium.Flow;
using Unity.PolySpatial;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;

namespace Moodium.CandyWorld
{
    /// <summary>
    /// Wrist UI v2 — "Mood Band".
    ///
    /// One slim glass bracelet hugs the wrist. Energy is a coloured light thread running inside the
    /// band across the back of the wrist; four gem sockets sit on the thread at 20/50/80/100%, so
    /// progress and collected rewards read as one object. A small glass card (title, percentage,
    /// gem dots, Release and Exit) appears only while the user looks at their wrist, and Exit needs
    /// a second confirming press within a short window.
    ///
    /// Drop-in replacement for the v1 component: class name, public events and the methods used by
    /// CandyWorldManager and the existing EditMode tests are unchanged.
    /// </summary>
    public sealed class CandyEnergyWristUI : MonoBehaviour
    {
        const float FullCircleDegrees = 360f;
        const string ResourceFolder = "MoodiumWristUI/";
        static readonly float[] SocketProgressValues = { .2f, .5f, .8f, 1f };
        static readonly Color CandyRingColor = new(1f, 91f / 255f, 157f / 255f, 1f);
        static readonly Color NatureRingColor = new(141f / 255f, 220f / 255f, 135f / 255f, 1f);
        static readonly Color ExitArmedTint = new(1f, .78f, .45f, .92f);

        [Header("Band (metres, wrist-local)")]
        [Tooltip("Offset from the wrist joint. +Y runs along the forearm towards the fingers.")]
        [SerializeField] Vector3 m_WristPositionOffset = new(0f, -.012f, 0f);
        [Tooltip("Half-width of the band across the wrist (thumb to little-finger side).")]
        [SerializeField, Range(.02f, .06f)] float m_BandRadiusX = .034f;
        [Tooltip("Half-depth of the band from back of hand to palm side.")]
        [SerializeField, Range(.015f, .05f)] float m_BandRadiusZ = .027f;
        [SerializeField, Range(.001f, .008f)] float m_BandTubeRadius = .003f;
        [SerializeField, Range(.0005f, .004f)] float m_ThreadTubeRadius = .0016f;
        [Tooltip("Ellipse angle where the progress thread starts. 90° is the centre of the back of the wrist.")]
        [SerializeField, Range(100f, 180f)] float m_ThreadStartDegrees = 150f;
        [SerializeField, Range(0f, 80f)] float m_ThreadEndDegrees = 30f;
        [Tooltip("Flip if, on device, the sockets appear on the palm side instead of the back of the hand.")]
        [SerializeField] bool m_FlipDorsal;
        [SerializeField, Range(24, 120)] int m_BandSegments = 72;
        [SerializeField, Range(4, 16)] int m_TubeSides = 10;
        [SerializeField, Min(1f)] float m_FollowResponsiveness = 14f;

        [Header("Raised wrist (head direction, not raw eye gaze)")]
        [SerializeField, Range(10f, 60f)] float m_GlanceEnterDegrees = 45f;
        [SerializeField, Range(12f, 70f)] float m_GlanceExitDegrees = 55f;
        [SerializeField, Range(.3f, 1.2f)] float m_GlanceMaxDistance = .65f;
        [SerializeField, Range(.1f,.8f)] float m_RaisedWristDrop = .45f;
        [SerializeField, Range(0f,1f)] float m_DorsalFacingDot = .25f;
        [SerializeField, Range(0f, 2f)] float m_MinCardSeconds = .6f;
        [Tooltip("Height of the card centre above the top of the band, in metres.")]
        [SerializeField, Range(.03f, .15f)] float m_CardLift = .072f;

        [Header("Exit")]
        [SerializeField, Range(1f, 5f)] float m_ExitConfirmWindow = 2f;

        const float CanvasScale = .00025f;   // 1 canvas unit = 0.25 mm
        const float CardWidth = 420f;
        const float CardHeight = 236f;

        Camera m_Camera;
        XRHandSubsystem m_HandSubsystem;
        XROrigin m_XROrigin;
        TMP_FontAsset m_Font;
        Sprite m_FallbackRounded;
        Sprite m_FallbackCircle;

        Transform m_BandRoot;
        Mesh m_BandMesh;
        Mesh m_ThreadMesh;
        Material m_BandMaterial;
        Material m_ThreadMaterial;
        SpriteRenderer m_ThreadHead;
        SpriteRenderer m_BandGlow;
        readonly Transform[] m_Sockets = new Transform[4];
        readonly SpriteRenderer[] m_SocketRings = new SpriteRenderer[4];
        readonly float[] m_SocketFlash = new float[4];

        RectTransform m_CardRect;
        CanvasGroup m_CardGroup;
        Image m_CardMoodi;
        TMP_Text m_Title;
        TMP_Text m_Subtitle;
        TMP_Text m_Percentage;
        readonly Image[] m_GemDots = new Image[4];
        GameObject m_ReleaseButtonRoot;
        Image m_ReleaseChip;
        Image m_ReleaseKnob;
        TMP_Text m_ReleaseLabel;
        GameObject m_ExitButtonRoot;
        Image m_ExitChip;
        Image m_ExitIcon;
        Image m_ExitCountdown;
        TMP_Text m_ExitLabel;

        RectTransform m_ToastRect;
        CanvasGroup m_ToastGroup;
        TMP_Text m_ToastLabel;
        float m_ToastUntil;

        WristExitConfirmation m_ExitConfirmation;
        float m_TargetProgress;
        float m_DisplayedProgress;
        float m_TargetEnergy;
        float m_DisplayedEnergy;
        float m_LastThreadProgress = -1f;
        float m_VisualAlpha;
        float m_Glance;
        bool m_Glancing;
        bool m_InputReady;
        VisionOSSortingGroup m_Sorting;
        float m_GlanceSince;
        bool m_Completed;
        bool m_ReleaseAvailable;
        bool m_ReleaseEnabled;
        bool m_ExitAvailable;
        bool m_Built;
        bool m_NatureWorld;
        int m_CollectedGems;

        public float DisplayedProgress => m_DisplayedProgress;
        public bool HandTracked { get; private set; }
        public bool IsGlancing => m_Glancing;
        public int SocketCount => m_Sockets.Length;
        public event System.Action ReleasePressed;
        /// <summary>Fires only after the second, confirming press of the Exit button.</summary>
        public event System.Action ExitPressed;

        // ---------- Pure helpers (EditMode-testable; v1 helpers kept) ----------

        public static float ProgressToArcDegrees(float normalizedProgress) => Mathf.Clamp01(normalizedProgress) * FullCircleDegrees;
        public static Color GetProgressRingColor(bool natureWorld) => natureWorld ? NatureRingColor : CandyRingColor;

        public static float CalculateOverlayVerticalAnchor(float ringRadius, float ringThickness, float clearance, float canvasScale)
        {
            return (Mathf.Max(0f, ringRadius) + Mathf.Max(0f, ringThickness) * .5f + Mathf.Max(0f, clearance)) /
                   Mathf.Max(.000001f, canvasScale);
        }

        public static string GetExitButtonLabel(bool english) => english ? "Exit World" : "退出世界";
        public static string GetExitConfirmLabel(bool english) => english ? "Tap again to exit" : "再点一次退出";
        public static string GetWorldEnergyTitle(bool natureWorld, bool english) =>
            english ? natureWorld ? "Nature Energy" : "Candy Energy" : natureWorld ? "自然能量" : "糖果能量";
        public static string GetReleaseToggleLabel(bool enabled, bool english) =>
            english ? enabled ? "Release: On" : "Release: Off" : enabled ? "释放：开启" : "释放：关闭";

        public static string GetNextGiftLabel(int collected, bool english)
        {
            if (collected >= 4)
                return english ? "All 4 gifts collected" : "4 件礼物都收齐啦";
            return english ? $"Next gift · {collected + 1} of 4" : $"下一件礼物 · 第 {collected + 1} 件";
        }

        /// <summary>Progress values at which the four gem sockets sit on the thread.</summary>
        public static float GetSocketProgress(int index) => SocketProgressValues[Mathf.Clamp(index, 0, SocketProgressValues.Length - 1)];

        /// <summary>Ellipse angle (degrees) of the thread tip for a progress value.</summary>
        public static float ThreadAngleForProgress(float progress, float startDegrees, float endDegrees) =>
            Mathf.Lerp(startDegrees, endDegrees, Mathf.Clamp01(progress));

        /// <summary>Glance with hysteresis: harder to enter than to stay.</summary>
        public static bool EvaluateGlance(bool wasGlancing, float angleDegrees, float distance,
            float enterDegrees, float exitDegrees, float maxDistance)
        {
            if (distance > maxDistance)
                return false;
            return wasGlancing ? angleDegrees <= exitDegrees : angleDegrees <= enterDegrees;
        }

        public static bool EvaluateWristPresentation(bool wasGlancing,float headAngle,float distance,
            float heightRelativeToEyes,float dorsalFacingDot,float enterDegrees=45f,float exitDegrees=55f,
            float maxDistance=.65f,float raisedDrop=.45f,float minDorsalDot=.25f)
        {
            return heightRelativeToEyes>=-(raisedDrop+(wasGlancing?.1f:0))&&
                dorsalFacingDot>=(wasGlancing?Mathf.Max(0,minDorsalDot-.15f):minDorsalDot)&&
                EvaluateGlance(wasGlancing,headAngle,distance,enterDegrees,exitDegrees,maxDistance);
        }

        // ---------- Public API (unchanged from v1 + additions) ----------

        public void ConfigureLanguageFonts(TMP_FontAsset chineseFont, TMP_FontAsset englishFont)
        {
            var selected = IsEnglish() ? englishFont : chineseFont;
            if (selected == null) return;
            m_Font = selected;
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                text.font = selected;
            RefreshTexts();
        }

        public void Build(Camera camera, TMP_FontAsset font, Sprite roundedSprite, Sprite circleSprite)
        {
            if(m_Built)return;
            m_Camera = camera != null ? camera : Camera.main;
            m_Font = font;
            m_FallbackRounded = roundedSprite;
            m_FallbackCircle = circleSprite;
            m_ExitConfirmation = new WristExitConfirmation(m_ExitConfirmWindow);
            m_Sorting=gameObject.AddComponent<VisionOSSortingGroup>();
            BuildBand();
            BuildCard();
            BuildToast();
            m_Built = true;
            SetProgress(0f, 0f);
            RefreshTexts();
            ApplyColors();
            SetButtonsInteractable(false);
        }

        public void SetReleaseAvailable(bool available)
        {
            m_ReleaseAvailable = available;
            if (!available) m_ReleaseEnabled = false;
            if (m_ReleaseButtonRoot != null) m_ReleaseButtonRoot.SetActive(available);
            LayoutButtons();
            RefreshTexts();
            SetButtonsInteractable(m_InputReady);
        }

        public void SetReleaseToggleState(bool enabled)
        {
            m_ReleaseEnabled = enabled;
            RefreshTexts();
            ApplyColors();
        }

        public void SetExitAvailable(bool available)
        {
            m_ExitAvailable = available;
            if(!available)m_ExitConfirmation?.Reset();
            if (m_ExitButtonRoot != null) m_ExitButtonRoot.SetActive(available);
            LayoutButtons();
            RefreshTexts();
            SetButtonsInteractable(m_InputReady);
        }

        public void SetWorldTheme(bool natureWorld)
        {
            m_NatureWorld = natureWorld;
            RefreshTexts();
            ApplyColors();
        }

        public void SetProgress(float energy, float normalized)
        {
            var previous = m_TargetProgress;
            m_TargetProgress = Mathf.Clamp01(normalized);
            m_TargetEnergy = Mathf.Max(0f, energy);
            for (var i = 0; i < SocketProgressValues.Length; i++)
                if (previous < SocketProgressValues[i] - .0005f && m_TargetProgress >= SocketProgressValues[i] - .0005f)
                    m_SocketFlash[i] = 1f;
            if (m_TargetProgress < previous - .0005f)
            {
                m_CollectedGems = 0;
                System.Array.Clear(m_SocketFlash,0,m_SocketFlash.Length);
                m_DisplayedProgress=m_TargetProgress;m_DisplayedEnergy=m_TargetEnergy;m_LastThreadProgress=-1;
                m_ExitConfirmation?.Reset();
            }
            if (!m_Completed && m_TargetProgress >= 1f)
            {
                m_Completed = true;
                MoodiumAudioManager.Play(MoodiumAudioCue.CandyEnergyComplete, transform.position);
                MoodiumInteractionVFXManager.PlayInteractionEffect(transform.position, transform.rotation, gameObject);
            }
            else if (m_TargetProgress < 1f) m_Completed = false;
            RefreshTexts();
        }

        /// <summary>Socket transform for gem <paramref name="index"/> (0..3). Parent a RewardOrb here.</summary>
        public Transform GetGemSocket(int index) =>
            index >= 0 && index < m_Sockets.Length ? m_Sockets[index] : null;

        /// <summary>Tell the band how many gems are seated, so it can hide filled socket rings and update the card.</summary>
        public void SetCollectedGems(int count)
        {
            m_CollectedGems = Mathf.Clamp(count, 0, m_Sockets.Length);
            RefreshTexts();
        }

        /// <summary>Small pill above the band, visible without looking at the card (e.g. "第 2 件 · 灌木").</summary>
        public void ShowToast(string text, float seconds = 2.5f)
        {
            if (m_ToastLabel == null || string.IsNullOrEmpty(text)) return;
            m_ToastLabel.text = text;
            m_ToastUntil = Time.time + Mathf.Max(.5f, seconds);
        }

        // ---------- Frame update ----------

        void LateUpdate()
        {
            if (!m_Built) return;
            var dt = Time.deltaTime;
            UpdateWristPose(dt);
            UpdateGlance(dt);
            UpdateProgressAnimation(dt);
            UpdateCardAndToast(dt);
            UpdateExitConfirmation();
            ApplyColors();
        }

        void UpdateWristPose(float dt)
        {
            var tracked = TryGetWristFrame(out var wristPosition, out var armAxis, out var dorsal);
#if UNITY_EDITOR
            if (!tracked && m_Camera != null)
            {
                var forward = Vector3.ProjectOnPlane(m_Camera.transform.forward, Vector3.up).normalized;
                wristPosition = m_Camera.transform.position + forward * .45f - Vector3.up * .22f;
                armAxis = Vector3.Cross(Vector3.up, forward).normalized;
                dorsal = Vector3.up;
                tracked = true;
            }
#endif
            var wasTracked=HandTracked;HandTracked = tracked;
            m_VisualAlpha = Mathf.MoveTowards(m_VisualAlpha, tracked ? 1f : 0f, dt * 8f);
            if (!tracked) return;
            if (m_FlipDorsal) dorsal = -dorsal;
            var rotation = Quaternion.LookRotation(dorsal, armAxis);
            var target = wristPosition + rotation * m_WristPositionOffset;
            var t = wasTracked?1f - Mathf.Exp(-m_FollowResponsiveness * dt):1f;
            transform.position = Vector3.Lerp(transform.position, target, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
        }

        bool TryGetWristFrame(out Vector3 wristPosition, out Vector3 armAxis, out Vector3 dorsal)
        {
            wristPosition = default;
            armAxis = Vector3.up;
            dorsal = Vector3.forward;
            if (m_HandSubsystem == null)
                m_HandSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            if (m_HandSubsystem == null || !m_HandSubsystem.running) return false;
            var flags = m_HandSubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
            if (!TryGetHandFrame(m_HandSubsystem.leftHand, flags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints, out var wrist, out var middle) &&
                !TryGetHandFrame(m_HandSubsystem.rightHand, flags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints, out wrist, out middle))
                return false;
            if (m_XROrigin == null) m_XROrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            var origin = m_XROrigin != null ? m_XROrigin.Origin.transform : null;
            var worldWrist = PalmPressGestureController.TransformTrackingPose(wrist, origin);
            var worldMiddle = PalmPressGestureController.TransformTrackingPose(middle, origin).position;
            wristPosition = worldWrist.position;
            armAxis = (worldMiddle - wristPosition).normalized;
            if (armAxis.sqrMagnitude < .0001f) return false;
            // XR Hands joint "up" points out of the back of the hand.
            dorsal = Vector3.ProjectOnPlane(worldWrist.rotation * Vector3.up, armAxis);
            if (dorsal.sqrMagnitude < .0001f)
                dorsal = Vector3.ProjectOnPlane(Vector3.up, armAxis);
            dorsal.Normalize();
            return true;
        }

        static bool TryGetHandFrame(XRHand hand, XRHandSubsystem.UpdateSuccessFlags updated, out Pose wrist, out Pose middle)
        {
            wrist = Pose.identity;
            middle = Pose.identity;
            if (updated == 0 && !hand.isTracked) return false;
            var wristJoint = hand.GetJoint(XRHandJointID.Wrist);
            var middleJoint = hand.GetJoint(XRHandJointID.MiddleMetacarpal);
            return wristJoint.trackingState != XRHandJointTrackingState.None &&
                   wristJoint.TryGetPose(out wrist) && middleJoint.TryGetPose(out middle);
        }

        void UpdateGlance(float dt)
        {
            var glancing = false;
            if (HandTracked && m_Camera != null)
            {
                var toWrist = BandTopWorld() - m_Camera.transform.position;
                var angle = Vector3.Angle(m_Camera.transform.forward, toWrist);
                var facing=Vector3.Dot(transform.forward,-toWrist.normalized);
                glancing = EvaluateWristPresentation(m_Glancing, angle, toWrist.magnitude,
                    toWrist.y,facing,m_GlanceEnterDegrees,m_GlanceExitDegrees,m_GlanceMaxDistance,
                    m_RaisedWristDrop,m_DorsalFacingDot);
            }
            // Input closes immediately; the minimum display time only holds the visual.
            m_InputReady=glancing;
            if(!m_InputReady)m_ExitConfirmation?.Reset();
            if (glancing && !m_Glancing) m_GlanceSince = Time.time;
            if (HandTracked&&!glancing && m_Glancing && Time.time - m_GlanceSince < m_MinCardSeconds) glancing = true;
            if (!glancing && m_Glancing) m_ExitConfirmation?.Reset();
            m_Glancing = glancing;
            m_Glance = Mathf.MoveTowards(m_Glance, glancing ? 1f : 0f, dt / (glancing ? .22f : .18f));
        }

        void UpdateProgressAnimation(float dt)
        {
            m_DisplayedProgress = Mathf.Lerp(m_DisplayedProgress, m_TargetProgress, 1f - Mathf.Exp(-5.5f * dt));
            m_DisplayedEnergy = Mathf.MoveTowards(m_DisplayedEnergy, m_TargetEnergy, dt * 24f);
            if (Mathf.Abs(m_DisplayedProgress - m_LastThreadProgress) > .002f)
            {
                var end = ThreadAngleForProgress(m_DisplayedProgress, m_ThreadStartDegrees, m_ThreadEndDegrees);
                BuildEllipseTube(m_ThreadMesh, m_ThreadStartDegrees, end, m_ThreadTubeRadius, 0f);
                m_LastThreadProgress = m_DisplayedProgress;
            }
            var tipAngle = ThreadAngleForProgress(m_DisplayedProgress, m_ThreadStartDegrees, m_ThreadEndDegrees);
            m_ThreadHead.transform.localPosition = EllipsePoint(tipAngle, 0f);
            m_ThreadHead.gameObject.SetActive(m_DisplayedProgress > .01f);
            for (var i = 0; i < m_SocketFlash.Length; i++)
                m_SocketFlash[i] = Mathf.MoveTowards(m_SocketFlash[i], 0f, dt / .8f);
            if (m_Percentage != null) m_Percentage.text = $"{Mathf.RoundToInt(m_DisplayedEnergy)}%";
        }

        void UpdateCardAndToast(float dt)
        {
            if (m_Camera == null) return;
            var camPos = m_Camera.transform.position;
            var top = BandTopWorld();

            // Card: floats above the band and turns to the viewer.
            m_CardGroup.alpha = m_Glance * m_VisualAlpha;
            m_CardGroup.blocksRaycasts = m_InputReady&&m_Glance > .5f;
            var cardPos = top + Vector3.up * m_CardLift + (camPos - top).normalized * .01f;
            m_CardRect.position = cardPos;
            m_CardRect.rotation = Quaternion.LookRotation(cardPos - camPos, Vector3.up);
            m_CardRect.localScale = Vector3.one * CanvasScale * Mathf.Lerp(.92f, 1f, m_Glance);
            SetButtonsInteractable(m_InputReady&&m_Glance > .5f);

            // Toast: just above the band, independent of the glance.
            var toastAlpha = Time.time < m_ToastUntil ? 1f : 0f;
            m_ToastGroup.alpha = Mathf.MoveTowards(m_ToastGroup.alpha, toastAlpha * m_VisualAlpha * (1f - m_Glance), dt / .2f);
            var toastPos = top + Vector3.up * .028f;
            m_ToastRect.position = toastPos;
            m_ToastRect.rotation = Quaternion.LookRotation(toastPos - camPos, Vector3.up);

            // Sprites that should always face the viewer.
            Billboard(m_ThreadHead.transform, camPos);
            Billboard(m_BandGlow.transform, camPos);
            foreach (var ring in m_SocketRings) Billboard(ring.transform, camPos);

            // The band itself grows slightly while looked at.
            m_BandRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.1f, m_Glance);
        }

        void UpdateExitConfirmation()
        {
            if (m_ExitConfirmation == null || m_ExitChip == null) return;
            var armed = m_ExitConfirmation.IsArmed(Time.time);
            m_ExitCountdown.gameObject.SetActive(armed);
            m_ExitIcon.gameObject.SetActive(!armed);
            if (armed)
            {
                // WristExitConfirmation does not expose its deadline; track it from the press time.
                var remaining = Mathf.Clamp01(1f - (Time.time - m_ExitArmedAt) / m_ExitConfirmWindow);
                m_ExitCountdown.fillAmount = remaining;
            }
            var label = armed ? GetExitConfirmLabel(IsEnglish()) : GetExitButtonLabel(IsEnglish());
            if (m_ExitLabel.text != label) m_ExitLabel.text = label;
        }

        float m_ExitArmedAt;

        void PressExit()
        {
            if (!m_ExitAvailable || m_ExitConfirmation == null || !HandTracked||!m_InputReady||m_Glance < .5f) return;
            if (m_ExitConfirmation.Press(Time.time))
            {
                ExitPressed?.Invoke();
                return;
            }
            m_ExitArmedAt = Time.time;
        }

        void PressRelease()
        {
            if (!m_ReleaseAvailable || !HandTracked||!m_InputReady||m_Glance < .5f) return;
            ReleasePressed?.Invoke();
        }

        // ---------- Colours & texts ----------

        void ApplyColors()
        {
            if (!m_Built) return;
            var accent = GetProgressRingColor(m_NatureWorld);
            var a = m_VisualAlpha;
            var ambient = Mathf.Lerp(.35f, 1f, m_Glance);

            SetMaterialColor(m_BandMaterial, new Color(1f, 1f, 1f, .22f * a * ambient));
            var thread = accent; thread.a = a * ambient;
            SetMaterialColor(m_ThreadMaterial, thread);
            m_ThreadHead.color = new Color(1f, 1f, 1f, .9f * a * ambient);

            var glow = accent;
            glow.a = a * (m_Completed ? .6f : .12f + .25f * m_Glance);
            m_BandGlow.color = glow;

            for (var i = 0; i < m_SocketRings.Length; i++)
            {
                var filled = i < m_CollectedGems;
                var reached = m_DisplayedProgress >= SocketProgressValues[i] - .001f;
                var ring = reached ? accent : Color.white;
                ring.a = filled ? 0f : a * (Mathf.Lerp(.45f, .9f, m_Glance) + m_SocketFlash[i] * .5f);
                m_SocketRings[i].color = ring;
                m_SocketRings[i].transform.localScale = SpriteScale(m_SocketRings[i].sprite, .012f * (1f + m_SocketFlash[i] * .4f));
            }

            for (var i = 0; i < m_GemDots.Length; i++)
            {
                var filled = i < m_CollectedGems || m_DisplayedProgress >= SocketProgressValues[i] - .001f;
                m_GemDots[i].sprite = filled ? LoadSprite("wrist_gem_dot_filled", m_FallbackCircle) : LoadSprite("wrist_gem_dot_empty", m_FallbackCircle);
                m_GemDots[i].color = filled ? accent : new Color(1f, 1f, 1f, .7f);
            }

            if (m_ReleaseChip != null)
            {
                m_ReleaseChip.sprite = LoadSprite(m_ReleaseEnabled ? "wrist_chip_solid" : "wrist_chip_glass", m_FallbackRounded);
                m_ReleaseChip.color = m_ReleaseEnabled ? new Color(accent.r, accent.g, accent.b, .92f) : Color.white;
                var knob = (RectTransform)m_ReleaseKnob.transform;
                knob.anchoredPosition = new Vector2(m_ReleaseEnabled ? -24f : -44f, 0f);
                m_ReleaseLabel.color = m_ReleaseEnabled ? new Color(.1f, .08f, .14f, 1f) : Color.white;
            }
            if (m_ExitChip != null)
            {
                var armed = m_ExitConfirmation != null && m_ExitConfirmation.IsArmed(Time.time);
                m_ExitChip.sprite = LoadSprite(armed ? "wrist_chip_solid" : "wrist_chip_glass", m_FallbackRounded);
                m_ExitChip.color = armed ? ExitArmedTint : Color.white;
                m_ExitLabel.color = armed ? new Color(.25f, .14f, .04f, 1f) : Color.white;
                m_ExitIcon.color = Color.white;
            }
            if (m_CardMoodi != null)
                m_CardMoodi.sprite = LoadSprite(m_DisplayedProgress >= .5f ? "moodi_happy" : "moodi_calm", m_FallbackCircle);
        }

        void RefreshTexts()
        {
            var english = IsEnglish();
            if (m_Title != null) m_Title.text = GetWorldEnergyTitle(m_NatureWorld, english);
            if (m_Subtitle != null) m_Subtitle.text = m_CollectedGems>0?GetNextGiftLabel(m_CollectedGems,english):
                (english?$"Energy stages · {ReachedSockets()}/4":$"能量阶段 · {ReachedSockets()}/4");
            if (m_ReleaseLabel != null) m_ReleaseLabel.text = GetReleaseToggleLabel(m_ReleaseEnabled, english);
            if (m_ExitLabel != null) m_ExitLabel.text = m_ExitConfirmation!=null&&m_ExitConfirmation.IsArmed(Time.time)?GetExitConfirmLabel(english):GetExitButtonLabel(english);
            var rainIcon = m_ReleaseButtonRoot != null ? m_ReleaseButtonRoot.transform.Find("Icon")?.GetComponent<Image>() : null;
            if (rainIcon != null) rainIcon.sprite = LoadSprite(m_NatureWorld ? "icon_rain_nature" : "icon_rain_candy", m_FallbackCircle);
        }

        int ReachedSockets()
        {
            var count = 0;
            foreach (var p in SocketProgressValues)
                if (m_TargetProgress >= p - .0005f) count++;
            return count;
        }

        static bool IsEnglish() =>
            PlayerPrefs.GetString("Moodium.Language", "Chinese").Equals("English", System.StringComparison.OrdinalIgnoreCase);

        // ---------- Band construction ----------

        void BuildBand()
        {
            var root = new GameObject("Mood Band");
            root.transform.SetParent(transform, false);
            m_BandRoot = root.transform;

            m_ThreadMesh = new Mesh { name = "Mood Band Thread", hideFlags = HideFlags.DontSave };
            m_ThreadMaterial = CreateMaterial("Mood Band Thread", false);
            CreateMeshRenderer("Thread", m_ThreadMesh, m_ThreadMaterial, 1);

            m_BandMesh = new Mesh { name = "Mood Band Glass", hideFlags = HideFlags.DontSave };
            BuildEllipseTube(m_BandMesh, 0f, FullCircleDegrees, m_BandTubeRadius, 0f);
            m_BandMaterial = CreateMaterial("Mood Band Glass", true);
            CreateMeshRenderer("Glass", m_BandMesh, m_BandMaterial, 2);

            m_BandGlow = CreateSprite("Glow", LoadSprite("wrist_band_glow", m_FallbackCircle), 0);
            m_BandGlow.transform.localPosition = EllipsePoint(90f, -.004f);
            m_BandGlow.transform.localScale = SpriteScale(m_BandGlow.sprite, .11f);

            m_ThreadHead = CreateSprite("Thread Head", LoadSprite("wrist_thread_head", m_FallbackCircle), 3);
            m_ThreadHead.transform.localScale = SpriteScale(m_ThreadHead.sprite, .011f);

            for (var i = 0; i < m_Sockets.Length; i++)
            {
                var angle = ThreadAngleForProgress(SocketProgressValues[i], m_ThreadStartDegrees, m_ThreadEndDegrees);
                var socket = new GameObject($"Gem Socket {i + 1}").transform;
                socket.SetParent(m_BandRoot, false);
                socket.localPosition = EllipsePoint(angle, m_BandTubeRadius + .0065f);
                socket.localRotation = Quaternion.LookRotation(EllipseNormal(angle), Vector3.up);
                m_Sockets[i] = socket;
                var ring = CreateSprite($"Socket Ring {i + 1}", LoadSprite("wrist_socket_ring", m_FallbackCircle), 4);
                ring.transform.SetParent(socket, false);
                ring.transform.localPosition = Vector3.zero;
                m_SocketRings[i] = ring;
            }
        }

        Vector3 EllipsePoint(float degrees, float outward)
        {
            var r = degrees * Mathf.Deg2Rad;
            var p = new Vector3(m_BandRadiusX * Mathf.Cos(r), 0f, m_BandRadiusZ * Mathf.Sin(r));
            return p + EllipseNormal(degrees) * outward;
        }

        Vector3 EllipseNormal(float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            return new Vector3(m_BandRadiusZ * Mathf.Cos(r), 0f, m_BandRadiusX * Mathf.Sin(r)).normalized;
        }

        /// <summary>Tube along the band ellipse (local XZ plane, +Y along the forearm) between two angles.</summary>
        void BuildEllipseTube(Mesh mesh, float fromDegrees, float toDegrees, float tubeRadius, float outward)
        {
            mesh.Clear();
            var span = Mathf.Abs(toDegrees - fromDegrees);
            if (span < .05f) return;
            var segments = Mathf.Max(2, Mathf.CeilToInt(m_BandSegments * span / FullCircleDegrees));
            var vertices = new List<Vector3>((segments + 1) * m_TubeSides);
            var normals = new List<Vector3>((segments + 1) * m_TubeSides);
            var triangles = new List<int>(segments * m_TubeSides * 6);
            for (var s = 0; s <= segments; s++)
            {
                var degrees = Mathf.Lerp(fromDegrees, toDegrees, s / (float)segments);
                var center = EllipsePoint(degrees, outward);
                var n = EllipseNormal(degrees);
                for (var side = 0; side < m_TubeSides; side++)
                {
                    var a = Mathf.PI * 2f * side / m_TubeSides;
                    var dir = (n * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)).normalized;
                    vertices.Add(center + dir * tubeRadius);
                    normals.Add(dir);
                }
            }
            var flip = toDegrees < fromDegrees;
            for (var s = 0; s < segments; s++)
            for (var side = 0; side < m_TubeSides; side++)
            {
                var next = (side + 1) % m_TubeSides;
                var a = s * m_TubeSides + side;
                var b = s * m_TubeSides + next;
                var c = (s + 1) * m_TubeSides + side;
                var d = (s + 1) * m_TubeSides + next;
                if (!flip)
                {
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
                else
                {
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(b); triangles.Add(d); triangles.Add(c);
                }
            }
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
        }

        Vector3 BandTopWorld() => m_BandRoot != null
            ? m_BandRoot.TransformPoint(EllipsePoint(90f, m_BandTubeRadius))
            : transform.position;

        void CreateMeshRenderer(string objectName, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(m_BandRoot, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            RegisterSorting(go,order);
        }

        SpriteRenderer CreateSprite(string objectName, Sprite sprite, int order)
        {
            var go = new GameObject(objectName, typeof(SpriteRenderer));
            go.transform.SetParent(m_BandRoot, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            RegisterSorting(go,order);
            return renderer;
        }

        static Vector3 SpriteScale(Sprite sprite, float worldSize)
        {
            if (sprite == null) return Vector3.one * worldSize;
            var units = Mathf.Max(sprite.rect.width, sprite.rect.height) / Mathf.Max(1f, sprite.pixelsPerUnit);
            return Vector3.one * (worldSize / Mathf.Max(.0001f, units));
        }

        static void Billboard(Transform t, Vector3 cameraPosition)
        {
            var away = t.position - cameraPosition;
            if (away.sqrMagnitude > .000001f) t.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        static Material CreateMaterial(string materialName, bool glossy)
        {
            var shader = glossy
                ? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = materialName, hideFlags = HideFlags.DontSave };
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
            if (glossy && material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .95f);
            if (glossy && material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        static void SetMaterialColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        // ---------- Card construction (world-space UGUI, same approach as v1) ----------

        void BuildCard()
        {
            var canvasObject = new GameObject("Mood Band Card", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            m_CardRect = (RectTransform)canvasObject.transform;
            m_CardRect.sizeDelta = new Vector2(CardWidth, CardHeight);
            m_CardRect.localScale = Vector3.one * CanvasScale;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            RegisterSorting(canvasObject,10,true);
            m_CardGroup = canvasObject.GetComponent<CanvasGroup>();
            m_CardGroup.alpha = 0f;
            m_CardGroup.blocksRaycasts=false;

            var background = CreateImage("Card Glass", m_CardRect, Vector2.zero, new Vector2(CardWidth, CardHeight),
                LoadSprite("wrist_card_glass", m_FallbackRounded), Color.white);
            background.type = Image.Type.Sliced;

            m_CardMoodi = CreateImage("Moodi", m_CardRect, new Vector2(-162f, 66f), new Vector2(60f, 60f),
                LoadSprite("moodi_calm", m_FallbackCircle), Color.white);
            m_CardMoodi.preserveAspect = true;
            m_Title = CreateText("Title", m_CardRect, new Vector2(-40f, 80f), new Vector2(180f, 34f), 26f, TextAlignmentOptions.Left, Color.white);
            m_Title.fontStyle = FontStyles.Bold;
            m_Title.enableAutoSizing=true;m_Title.fontSizeMin=18;m_Title.fontSizeMax=26;
            m_Title.textWrappingMode=TextWrappingModes.NoWrap;
            m_Subtitle = CreateText("Subtitle", m_CardRect, new Vector2(-40f, 50f), new Vector2(180f, 26f), 17f, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, .72f));
            m_Percentage = CreateText("Percentage", m_CardRect, new Vector2(140f, 66f), new Vector2(120f, 56f), 40f, TextAlignmentOptions.Right, Color.white);
            m_Percentage.fontStyle = FontStyles.Bold;

            for (var i = 0; i < m_GemDots.Length; i++)
                m_GemDots[i] = CreateImage($"Gem Dot {i + 1}", m_CardRect, new Vector2(-51f + i * 34f, 4f), new Vector2(22f, 22f),
                    LoadSprite("wrist_gem_dot_empty", m_FallbackCircle), Color.white);

            m_ReleaseButtonRoot = CreateChip("Release Button", new Vector2(208f, 64f), out m_ReleaseChip, out var releaseIcon, out m_ReleaseLabel);
            releaseIcon.sprite = LoadSprite("icon_rain_candy", m_FallbackCircle);
            ((RectTransform)releaseIcon.transform).anchoredPosition=new Vector2(-86f,0);
            m_ReleaseLabel.rectTransform.anchoredPosition=new Vector2(-16f,0);
            m_ReleaseLabel.rectTransform.sizeDelta=new Vector2(102f,64f);
            m_ReleaseLabel.enableAutoSizing=true;m_ReleaseLabel.fontSizeMin=12;m_ReleaseLabel.fontSizeMax=17;
            m_ReleaseLabel.textWrappingMode=TextWrappingModes.NoWrap;
            m_ReleaseKnob = CreateImage("Toggle Knob", m_ReleaseButtonRoot.transform, new Vector2(-26f, 0f), new Vector2(40f, 40f),
                LoadSprite("wrist_toggle_knob", m_FallbackCircle), Color.white);
            var knobRect = (RectTransform)m_ReleaseKnob.transform;
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(1f, .5f);
            m_ReleaseButtonRoot.AddComponent<MoodiumSpatialButton>().Configure(m_ReleaseLabel, null, PressRelease);
            m_ReleaseButtonRoot.SetActive(false);

            m_ExitButtonRoot = CreateChip("Exit World Button", new Vector2(168f, 64f), out m_ExitChip, out m_ExitIcon, out m_ExitLabel);
            m_ExitIcon.sprite = LoadSprite("icon_exit", m_FallbackCircle);
            m_ExitLabel.enableAutoSizing=true;m_ExitLabel.fontSizeMin=12;m_ExitLabel.fontSizeMax=20;
            m_ExitLabel.textWrappingMode=TextWrappingModes.NoWrap;
            m_ExitCountdown = CreateImage("Countdown", m_ExitButtonRoot.transform, new Vector2(-56f, 0f), new Vector2(32f, 32f),
                LoadSprite("wrist_countdown_ring", m_FallbackCircle), new Color(.25f, .14f, .04f, 1f));
            m_ExitCountdown.type = Image.Type.Filled;
            m_ExitCountdown.fillMethod = Image.FillMethod.Radial360;
            m_ExitCountdown.fillOrigin = (int)Image.Origin360.Top;
            m_ExitCountdown.fillClockwise = false;
            m_ExitCountdown.gameObject.SetActive(false);
            m_ExitButtonRoot.AddComponent<MoodiumSpatialButton>().Configure(m_ExitLabel, null, PressExit);
            m_ExitButtonRoot.SetActive(false);
            LayoutButtons();
        }

        GameObject CreateChip(string objectName, Vector2 size, out Image chip, out Image icon, out TMP_Text label)
        {
            var root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(BoxCollider));
            root.transform.SetParent(m_CardRect, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            chip = root.GetComponent<Image>();
            chip.sprite = LoadSprite("wrist_chip_glass", m_FallbackRounded);
            chip.type = Image.Type.Sliced;
            chip.raycastTarget = false;
            root.GetComponent<BoxCollider>().size = new Vector3(size.x + 16f, size.y + 16f, 24f);
            root.GetComponent<BoxCollider>().enabled=false;
            root.AddComponent<VisionOSHoverEffect>().Type=VisionOSHoverEffect.EffectType.Highlight;
            icon = CreateImage("Icon", root.transform, new Vector2(-size.x * .5f + 36f, 0f), new Vector2(30f, 30f), null, Color.white);
            label = CreateText("Label", root.transform, new Vector2(14f, 0f), new Vector2(size.x - 64f, size.y), 20f, TextAlignmentOptions.Center, Color.white);
            return root;
        }

        void LayoutButtons()
        {
            if (m_ExitButtonRoot == null || m_ReleaseButtonRoot == null) return;
            var exitRect = (RectTransform)m_ExitButtonRoot.transform;
            var releaseRect = (RectTransform)m_ReleaseButtonRoot.transform;
            if (m_ReleaseAvailable)
            {
                releaseRect.anchoredPosition = new Vector2(-92f, -74f);
                exitRect.sizeDelta = new Vector2(168f, 64f);
                exitRect.anchoredPosition = new Vector2(112f, -74f);
            }
            else
            {
                exitRect.sizeDelta = new Vector2(236f, 64f);
                exitRect.anchoredPosition = new Vector2(0f, -74f);
            }
            exitRect.GetComponent<BoxCollider>().size = new Vector3(exitRect.sizeDelta.x + 16f, exitRect.sizeDelta.y + 16f, 24f);
            var exitIconRect = (RectTransform)m_ExitIcon.transform;
            exitIconRect.anchoredPosition = new Vector2(-exitRect.sizeDelta.x * .5f + 36f, 0f);
            ((RectTransform)m_ExitCountdown.transform).anchoredPosition = exitIconRect.anchoredPosition;
            ((RectTransform)m_ExitLabel.transform).sizeDelta = new Vector2(exitRect.sizeDelta.x - 64f, 64f);
        }

        void SetButtonsInteractable(bool interactable)
        {
            interactable=interactable&&HandTracked&&m_InputReady&&m_Glance>.5f;
            if (m_ReleaseButtonRoot != null) m_ReleaseButtonRoot.GetComponent<BoxCollider>().enabled = interactable && m_ReleaseAvailable;
            if (m_ExitButtonRoot != null) m_ExitButtonRoot.GetComponent<BoxCollider>().enabled = interactable && m_ExitAvailable;
        }

        void BuildToast()
        {
            var canvasObject = new GameObject("Mood Band Toast", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            m_ToastRect = (RectTransform)canvasObject.transform;
            m_ToastRect.sizeDelta = new Vector2(300f, 56f);
            m_ToastRect.localScale = Vector3.one * CanvasScale;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 11;
            RegisterSorting(canvasObject,11,true);
            m_ToastGroup = canvasObject.GetComponent<CanvasGroup>();
            m_ToastGroup.alpha = 0f;
            m_ToastGroup.blocksRaycasts = false;
            // Dark pill: the toast often sits over skin, where light glass + white text is hard to read.
            var pill = CreateImage("Pill", m_ToastRect, Vector2.zero, new Vector2(300f, 56f), LoadSprite("wrist_chip_dark", m_FallbackRounded), Color.white);
            pill.type = Image.Type.Sliced;
            CreateImage("Gift Icon", m_ToastRect, new Vector2(-122f, 0f), new Vector2(26f, 26f), LoadSprite("icon_gift", m_FallbackCircle), Color.white);
            m_ToastLabel = CreateText("Text", m_ToastRect, new Vector2(16f, 0f), new Vector2(244f, 50f), 19f, TextAlignmentOptions.Center, Color.white);
        }

        Image CreateImage(string objectName, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        TMP_Text CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject($"Text - {objectName}", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (m_Font != null) text.font = m_Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        readonly Dictionary<string, Sprite> m_SpriteCache = new();

        Sprite LoadSprite(string resourceName, Sprite fallback)
        {
            if (m_SpriteCache.TryGetValue(resourceName, out var cached))
                return cached != null ? cached : fallback;
            var sprite = Resources.Load<Sprite>(ResourceFolder + resourceName);
            if (sprite == null)
                Debug.LogWarning($"[Mood Band] Missing sprite Resources/{ResourceFolder}{resourceName}. Using fallback.");
            m_SpriteCache[resourceName] = sprite;
            return sprite != null ? sprite : fallback;
        }

        void RegisterSorting(GameObject visual,int order,bool descendants=false)
        {
            m_Sorting.Renderers.Add(new VisionOSSortingGroup.RendererSorting{Renderer=visual,Order=order,ApplyToDescendants=descendants});
        }
        void OnDisable()
        {
            m_ExitConfirmation?.Reset();m_InputReady=m_Glancing=HandTracked=false;m_Glance=m_VisualAlpha=0;
            m_ToastUntil=0;
            if(m_CardGroup!=null){m_CardGroup.alpha=0;m_CardGroup.blocksRaycasts=false;}
            if(m_ToastGroup!=null)m_ToastGroup.alpha=0;
            SetButtonsInteractable(false);
        }
        static void DisposeRuntime(Object owned)
        {
            if(owned==null)return;
            if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);
        }
        void OnDestroy()
        {
            DisposeRuntime(m_BandMesh);DisposeRuntime(m_ThreadMesh);
            DisposeRuntime(m_BandMaterial);DisposeRuntime(m_ThreadMaterial);
        }
    }
}
