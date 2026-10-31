using System;
using System.Collections;
using Moodium.Flow;
using Moodium.Interaction;
using Unity.PolySpatial;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Moodium.CandyWorld
{
    /// <summary>
    /// Floating world dock: two liquid-glass buttons, [Candy Shower | Meadow Shower] [Exit World], that hover in the lower part
    /// of the view during the room stage of Candy / Nature world. Replaces the wrist band (CandyEnergyWristUI),
    /// which first-time users did not find and found hard to press.
    ///
    /// Placement: 0.50 m ahead and 0.21 m below the eyes (about 23° down), turned to face the eyes — inside arm's
    /// reach for poke, out of the centre of the view. It follows lazily: only when the head has been turned more
    /// than 35° away for 0.4 s, or the user walked 0.3 m, does it glide (0.6 s) to the new front. It never moves
    /// while a fingertip is within 12 cm of it or within 1 s after a press, so a poke target does not slide away.
    ///
    /// Input: each button is a capsule mesh with a BoxCollider + MoodiumSpatialButton, so the shared
    /// MoodiumSpatialInteractionController presses it for gaze + pinch (IndirectPinch), direct pinch and poke
    /// (Touch) alike. The capsule also carries VisionOSHoverEffect, shaped like the glass (not a rectangle).
    ///
    /// Same public API as the wrist UI for the calls CandyWorldManager makes, so the swap is mechanical.
    /// </summary>
    public sealed class WorldActionDock : MonoBehaviour
    {
        public const string Folder = "MoodiumWorldDock/";

        // Layout, metres.
        public const float Distance = .50f, Drop = .21f;
        public const float ButtonWidth = .10f, ButtonHeight = .037f, Gap = .02f;
        /// <summary>Pressable margin on every side of a capsule, and the collider depth a poking finger enters.</summary>
        public const float HitPad = .006f, HitDepth = .03f;
        public const float ToastLift = .055f;
        // Progress line inside the locked Shower capsule.
        public const float ProgressWidth = .064f, ProgressHeight = .0022f, ProgressY = -.0138f;

        // Lazy follow.
        public const float RecenterDegrees = 35f, RecenterDelay = .4f, GlideSeconds = .6f;
        public const float WalkRecenterMetres = .30f, HeightRecenterMetres = .15f;
        public const float HandNearMetres = .12f, PressHoldsFollowSeconds = 1f;

        // Input safety.
        public const float InputSettleSeconds = .5f, PressCooldown = .4f, ExitConfirmSeconds = 3f;
        /// <summary>Wait before "energy full" is announced, so it does not land on top of Moodi's thank-you.</summary>
        public const float ReadyToastDelay = 2.5f;

        [Tooltip("Exit needs a second press within 3 s. Off by default: the user chose one-press exit for the wrist; " +
                 "turn on only if real-device testing shows accidental exits.")]
        [SerializeField] bool m_ConfirmExit;
        [Tooltip("System gaze highlight on the capsule-shaped hit mesh. Turn off if it looks wrong on device.")]
        [SerializeField] bool m_SystemHover = true;

        public enum ShowerLook { Locked, Ready, On }

        public event Action ReleasePressed;
        public event Action ExitPressed;

        /// <summary>Hidden (fades out, no input) while true — set by CandyWorldManager while in a bubble world.</summary>
        bool m_Suppressed;
        public bool Suppressed { get => m_Suppressed; set { m_Suppressed=value; RefreshInput(); } }
        public bool Visible => m_Alpha > .5f;
        /// <summary>Where collected energy can fly to (the Shower button).</summary>
        public Transform ReleaseAnchor => m_Shower != null ? m_Shower.transform : transform;

        Camera m_Camera;
        bool m_English, m_Nature, m_Built;
        bool m_ReleaseAvailable, m_ReleaseOn, m_ExitAvailable, m_RoomInteraction = true;
        bool m_ReadyAnnounced, m_Placed, m_Gliding;
        float m_AvailableAt = -100f, m_Progress, m_Alpha, m_ShownAt = -100f, m_LastPress = -100f, m_OffAxisFor, m_GlideStart, m_GlideEndAt = -100f;
        Vector3 m_GlideFrom, m_GlideTo, m_AnchorEye;
        WorldDockButton m_Shower, m_Exit;
        SpriteRenderer m_Toast, m_ProgressTrack, m_ProgressFill;
        float m_ToastUntil;
        readonly WristExitConfirmation m_Confirm = new(ExitConfirmSeconds);
        XRHandSubsystem m_Hands;
        XROrigin m_Origin;

        // ---------- pure helpers (EditMode tests) ----------

        public static Vector3 FlatForward(Vector3 forward)
        {
            var f = Vector3.ProjectOnPlane(forward, Vector3.up);
            return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
        }

        public static Vector3 TargetPosition(Vector3 eye, Vector3 headForward) =>
            eye + FlatForward(headForward) * Distance + Vector3.down * Drop;

        /// <summary>Turned to the eyes. Project convention: the panel's +Z points away from the viewer.</summary>
        public static Quaternion FaceEye(Vector3 position, Vector3 eye)
        {
            var away = position - eye;
            return away.sqrMagnitude < 1e-6f ? Quaternion.identity : Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        public static float OffAxisDegrees(Vector3 eye, Vector3 headForward, Vector3 dock) =>
            Vector3.Angle(FlatForward(headForward), Vector3.ProjectOnPlane(dock - eye, Vector3.up));

        /// <summary>Glide when the head has been turned away long enough, or the user walked / stood up / sat down.</summary>
        public static bool ShouldRecenter(float offAxisDegrees, float offAxisSeconds, Vector3 eye, Vector3 anchorEye)
        {
            var flat = Vector3.ProjectOnPlane(eye - anchorEye, Vector3.up).magnitude;
            return (offAxisDegrees > RecenterDegrees && offAxisSeconds >= RecenterDelay) ||
                   flat > WalkRecenterMetres || Mathf.Abs(eye.y - anchorEye.y) > HeightRecenterMetres;
        }

        public static bool FollowHeld(float fingertipDistance, float sincePress) =>
            fingertipDistance < HandNearMetres || sincePress < PressHoldsFollowSeconds;

        public static bool CanPress(bool visible, float sinceShown, float sinceGlideEnd, bool gliding, float sinceLastPress) =>
            visible && !gliding && sinceShown >= InputSettleSeconds && sinceGlideEnd >= InputSettleSeconds && sinceLastPress >= PressCooldown;

        public static ShowerLook ShowerState(bool available, bool on) => !available ? ShowerLook.Locked : on ? ShowerLook.On : ShowerLook.Ready;

        public static string ShowerSprite(ShowerLook look, bool nature, bool english, string state) =>
            look == ShowerLook.Locked ? $"{Folder}dock_shower_locked_{(nature ? "nature" : "candy")}_{Lang(english)}"
            : $"{Folder}dock_{(look == ShowerLook.On ? "stop" : "shower")}_{(nature ? "nature" : "candy")}_{Lang(english)}_{state}";

        public static string ExitSprite(bool confirming, bool english, string state) =>
            $"{Folder}dock_exit{(confirming ? "_confirm" : "")}_{Lang(english)}_{state}";

        /// <summary>The "energy full" toast names the world's shower; the "keep squeezing" toast is shared.</summary>
        public static string ToastSprite(bool ready, bool nature, bool english) =>
            ready ? $"{Folder}dock_toast_ready_{(nature ? "nature" : "candy")}_{Lang(english)}" : $"{Folder}dock_toast_locked_{Lang(english)}";

        /// <summary>Button centres along the dock's local X: Shower left, Exit right.</summary>
        public static float ButtonX(bool exit) => (exit ? 1f : -1f) * (ButtonWidth + Gap) / 2f;

        static string Lang(bool english) => english ? "en" : "zh";

        // ---------- setup (same calls as the wrist UI) ----------

        public void Build(Camera camera, bool english)
        {
            m_Camera = camera; m_English = english;
            if (m_Built) { RefreshAll(); return; }
            m_Built = true;
            Moodium.UI.UIRefreshVisuals.UnitScale(transform);   // 1 local unit = 1 m for every offset below
            if (GetComponent<VisionOSSortingGroup>() == null) gameObject.AddComponent<VisionOSSortingGroup>();
            m_Shower = WorldDockButton.Create(transform, "Shower", new Vector3(ButtonX(false), 0, 0), m_SystemHover, PressShower);
            m_Exit = WorldDockButton.Create(transform, "Exit World", new Vector3(ButtonX(true), 0, 0), m_SystemHover, PressExit);
            m_ProgressTrack = Line(m_Shower.transform, "Energy Track", 2);
            m_ProgressFill = Line(m_Shower.transform, "Energy Fill", 3);
            m_Toast = Moodium.UI.UIRefreshVisuals.Sprite(transform, "Dock Toast", ToastSprite(true, m_Nature, english),
                new Vector3(0, ToastLift, 0), 1);
            Moodium.UI.UIRefreshVisuals.UnitScale(m_Toast.transform);
            m_Toast.color = Color.clear;
            SetAlpha(0f);
            RefreshAll();
        }

        static SpriteRenderer Line(Transform parent, string name, int order)
        {
            var r = Moodium.UI.UIRefreshVisuals.Sprite(parent, name, Folder + "dock_progress", new Vector3(0, ProgressY, -.0005f), order);
            r.drawMode = SpriteDrawMode.Sliced;
            Moodium.UI.UIRefreshVisuals.UnitScale(r.transform);
            return r;
        }

        public void SetLanguage(bool english) { m_English = english; RefreshAll(); }

        public void SetWorldTheme(bool natureWorld)
        {
            m_Nature = natureWorld; m_ReadyAnnounced = false; m_Confirm.Reset();
            RefreshAll();
        }

        public void SetProgress(float energy, float normalized) => SetProgress(normalized);

        public void SetProgress(float normalized)
        {
            m_Progress = Mathf.Clamp01(normalized);
            RefreshProgress();
        }

        public void SetReleaseAvailable(bool available)
        {
            if (available && !m_ReleaseAvailable) m_AvailableAt = Time.time;
            m_ReleaseAvailable = available;
            if (!available) m_ReleaseOn = false;
            RefreshAll();
        }

        public void SetReleaseToggleState(bool on) { m_ReleaseOn = on; RefreshAll(); }

        public void SetExitAvailable(bool available) { m_ExitAvailable = available; if (!available) m_Confirm.Reset(); RefreshAll(); RefreshInput(); }

        public void SetRoomInteractionEnabled(bool enabled) { m_RoomInteraction = enabled; RefreshInput(); }

        // ---------- per frame ----------

        void LateUpdate()
        {
            if(m_Built&&m_Camera!=null)Tick(Time.time,Time.deltaTime,NearestFingertip());
        }

        // Clock and fingertip distance are explicit so the real follow state machine can be tested deterministically.
        void Tick(float now,float dt,float fingertipDistance)
        {
            if (!m_Built || m_Camera == null) return;
            var eye = m_Camera.transform.position;
            var forward = m_Camera.transform.forward;
            var want = !Suppressed && m_RoomInteraction && m_ExitAvailable;
            if (want && !m_Placed) { Snap(eye, forward,now); }
            if (want && m_Alpha <= 0f) m_ShownAt = now;
            SetAlpha(Mathf.MoveTowards(m_Alpha, want ? 1f : 0f, dt / (want ? .25f : .18f)));
            if (!want) { if (m_Alpha <= 0f) m_Placed = false; return; }
            if (m_Confirm.IsArmed(now) == false && m_Exit != null && m_Exit.Confirming) RefreshAll(now);
            // Energy full: say so once per world visit, as soon as the dock is fully shown and settled.
            if (m_ReleaseAvailable && !m_ReadyAnnounced && m_Alpha >= 1f && now - m_ShownAt >= InputSettleSeconds &&
                now - m_AvailableAt >= ReadyToastDelay)
            {
                m_ReadyAnnounced = true; ShowToast(true, 3.5f,now); m_Shower.Pulse();
                Debug.Log("[World Dock] energy full, shower ready");
            }

            var held = FollowHeld(fingertipDistance, now - m_LastPress);
            if(held)
            {
                // Stop even a glide already in flight: the target must not move away from a reaching finger.
                if(m_Gliding){m_Gliding=false;m_GlideEndAt=now;}
                m_OffAxisFor=0;UpdateToast(now,dt);return;
            }
            if (m_Gliding)
            {
                var t = Mathf.Clamp01((now - m_GlideStart) / GlideSeconds);
                transform.position = Vector3.Lerp(m_GlideFrom, m_GlideTo, t * t * (3f - 2f * t));
                if (t >= 1f) { m_Gliding = false; m_GlideEndAt = now; }
            }
            else
            {
                var off = OffAxisDegrees(eye, forward, transform.position);
                m_OffAxisFor = off > RecenterDegrees ? m_OffAxisFor + dt : 0f;
                if (!held && ShouldRecenter(off, m_OffAxisFor, eye, m_AnchorEye))
                {
                    m_Gliding = true; m_GlideStart = now; m_OffAxisFor = 0f; m_Confirm.Reset();
                    m_GlideFrom = transform.position; m_GlideTo = TargetPosition(eye, forward); m_AnchorEye = eye;
                    Debug.Log($"[World Dock] glide to front (off {off:0}°)");
                }
            }
            transform.rotation = FaceEye(transform.position, eye);
            UpdateToast(now,dt);
        }

        void Snap(Vector3 eye, Vector3 forward,float now)
        {
            transform.position = TargetPosition(eye, forward);
            transform.rotation = FaceEye(transform.position, eye);
            m_AnchorEye = eye; m_Placed = true; m_Gliding = false; m_GlideEndAt = now;
        }

        void UpdateToast(float now,float dt)
        {
            if (m_Toast == null) return;
            var a = now < m_ToastUntil ? 1f : 0f;
            var c = m_Toast.color;
            c = Color.white * new Color(1, 1, 1, Mathf.MoveTowards(c.a, a * m_Alpha, dt / .2f));
            m_Toast.color = c;
        }

        void ShowToast(bool ready, float seconds,float now)
        {
            if (m_Toast == null) return;
            m_Toast.sprite = Moodium.UI.UIRefreshVisuals.Load(ToastSprite(ready, m_Nature, m_English));
            m_ToastUntil = now + seconds;
        }

        // ---------- presses ----------

        bool Pressable(float now) => isActiveAndEnabled&&!Suppressed&&m_RoomInteraction&&m_ExitAvailable&&CanPress(Visible, now - m_ShownAt, now - m_GlideEndAt, m_Gliding, now - m_LastPress);

        void PressShower()=>PressShowerAt(Time.time);
        void PressShowerAt(float now)
        {
            if (!Pressable(now) || !m_RoomInteraction) return;
            m_LastPress = now;
            if (!m_ReleaseAvailable)
            {
                // Locked: explain instead of doing nothing.
                ShowToast(false, 2.5f,now);
                m_Shower.Shake();
                Debug.Log("[World Dock] shower pressed while locked");
                return;
            }
            m_Shower.Flash();
            Debug.Log("[World Dock] shower " + (m_ReleaseOn ? "stop" : "start"));
            ReleasePressed?.Invoke();
        }

        void PressExit()=>PressExitAt(Time.time);
        void PressExitAt(float now)
        {
            if (!Pressable(now) || !m_ExitAvailable) return;
            m_LastPress = now;
            m_Exit.Flash();
            if (m_ConfirmExit && !m_Confirm.Press(now)) { RefreshAll(now); Debug.Log("[World Dock] exit armed"); return; }
            Debug.Log("[World Dock] exit");
            ExitPressed?.Invoke();
        }

        // ---------- visuals ----------

        void RefreshAll(float? at=null)
        {
            if (!m_Built) return;
            var look = ShowerState(m_ReleaseAvailable, m_ReleaseOn);
            m_Shower.SetSprites(state => ShowerSprite(look, m_Nature, m_English, state), look == ShowerLook.Locked);
            var confirming = m_ConfirmExit && m_Confirm.IsArmed(at??Time.time);
            m_Exit.Confirming = confirming;
            m_Exit.SetSprites(state => ExitSprite(confirming, m_English, state), false);
            RefreshProgress();
        }

        void RefreshProgress()
        {
            if (m_ProgressFill == null) return;
            var locked = !m_ReleaseAvailable;
            var tint = m_Nature ? new Color(.59f, .89f, .47f) : new Color(1f, .52f, .75f);
            m_ProgressTrack.gameObject.SetActive(locked);
            m_ProgressFill.gameObject.SetActive(locked && m_Progress > .01f);
            // Sliced sprites take their size in local units; the parent holder is unit-scaled (1 = 1 m).
            m_ProgressTrack.size = new Vector2(ProgressWidth, ProgressHeight);
            var w = Mathf.Max(ProgressHeight, ProgressWidth * m_Progress);
            m_ProgressFill.size = new Vector2(w, ProgressHeight);
            m_ProgressFill.transform.localPosition = new Vector3(-ProgressWidth / 2f + w / 2f, ProgressY, -.0005f);
            m_ProgressTrack.color = new Color(1, 1, 1, .28f * m_Alpha);
            m_ProgressFill.color = new Color(tint.r, tint.g, tint.b, m_Alpha);
        }

        void SetAlpha(float a)
        {
            m_Alpha = a;
            m_Shower?.SetAlpha(a); m_Exit?.SetAlpha(a);
            RefreshInput();
            if (m_ProgressTrack != null) RefreshProgress();
        }

        void RefreshInput()
        {
            bool interactive=isActiveAndEnabled&&!Suppressed&&m_RoomInteraction&&m_ExitAvailable&&m_Alpha>.9f;
            m_Shower?.SetInteractive(interactive);m_Exit?.SetInteractive(interactive);
        }
        void OnDisable()
        {
            m_Placed=m_Gliding=false;m_OffAxisFor=0;m_Confirm.Reset();m_ToastUntil=0;
            SetAlpha(0);if(m_Toast!=null)m_Toast.color=Color.clear;
        }

        float NearestFingertip()
        {
            if (m_Hands == null || !m_Hands.running) m_Hands = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            if (m_Hands == null || !m_Hands.running) return float.PositiveInfinity;
            if (m_Origin == null) m_Origin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            var origin = m_Origin != null ? m_Origin.Origin.transform : null;
            var best = float.PositiveInfinity;
            foreach (var hand in new[] { m_Hands.leftHand, m_Hands.rightHand })
            {
                if (!hand.isTracked || !hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var tip)) continue;
                var world = PalmPressGestureController.TransformTrackingPose(tip, origin).position;
                best = Mathf.Min(best, m_Shower.DistanceTo(world),m_Exit.DistanceTo(world));
            }
            return best;
        }
    }
}
