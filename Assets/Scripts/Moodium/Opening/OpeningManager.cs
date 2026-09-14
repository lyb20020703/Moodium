using System.Collections;
using Moodium.Flow;
using Moodium.Audio;
using Moodium.Interaction;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Moodium.Opening
{
    [DefaultExecutionOrder(-10000)]
    public sealed class OpeningManager : MonoBehaviour
    {
        public enum OpeningState
        {
            Opening_Idle,
            Candy_Transform,
            Logo_Fade_In,
            Logo_Hold,
            Opening_Finished
        }

        [SerializeField] OpeningAnimationController m_AnimationController;
        [SerializeField] CandyInteraction m_CandyInteraction;
        [SerializeField, Min(0.2f)] float m_DistanceFromUser = 1f;
        [SerializeField] float m_HeightOffset = 0f;
        [SerializeField, Min(0f)] float m_HeadPoseSettleTime = 1f;
        [SerializeField] GameObject m_TouchPrompt;
        [SerializeField] GameObject m_GestureGuidePrefab;
        [SerializeField, Min(0f)] float m_GestureGuideDelay = 3f;
        [SerializeField] Vector3 m_GestureGuideLocalOffset = new(0f, 0f, -0.18f);
        [SerializeField, Range(0.5f, 4f)] float m_LogoFadeDuration = 1.8f;
        [SerializeField, Min(0f)] float m_FinalLogoHold = 2.8f;
        [SerializeField, Min(0f)] float m_HandoffDelay = 0.15f;

        MoodiumAppFlowController m_AppFlow;
        OpeningHandTrailController m_HandTrail;
        Coroutine m_GestureGuideRoutine;
        GameObject m_GestureGuideInstance;
        bool m_Triggered;

        public OpeningState State { get; private set; } = OpeningState.Opening_Idle;

        void Awake()
        {
            m_HandTrail = GetComponent<OpeningHandTrailController>();
            if (m_HandTrail == null)
                m_HandTrail = gameObject.AddComponent<OpeningHandTrailController>();
            m_HandTrail.SetTrailActive(false);
            m_AppFlow = FindFirstObjectByType<MoodiumAppFlowController>();
            if (m_AppFlow != null)
            {
                m_AppFlow.PrepareForOpening();
                m_AppFlow.enabled = false;
            }
            if (m_CandyInteraction != null)
                m_CandyInteraction.Touched += BeginOpening;
        }

        IEnumerator Start()
        {
            var timeout = Time.realtimeSinceStartup + 10f;
            while (Camera.main == null && Time.realtimeSinceStartup < timeout)
                yield return null;
            // Re-anchor during the short tracking warm-up. This prevents one early
            // camera pose (often world origin) from placing the Candy near the floor.
            var settleUntil = Time.realtimeSinceStartup + m_HeadPoseSettleTime;
            while (Time.realtimeSinceStartup < settleUntil)
            {
                PlaceInFrontOfUser(false);
                yield return null;
            }
            PlaceInFrontOfUser(true);
            m_AnimationController?.ResetOpening();
            m_HandTrail?.SetTrailActive(false);
            m_CandyInteraction?.ResetInteraction();
            if (m_TouchPrompt != null)
                m_TouchPrompt.SetActive(true);
            m_GestureGuideRoutine = StartCoroutine(ShowGestureGuideAfterDelay());
            State = OpeningState.Opening_Idle;
        }

        void OnDestroy()
        {
            HideGestureGuide();
            m_HandTrail?.SetTrailActive(false);
            if (m_CandyInteraction != null)
                m_CandyInteraction.Touched -= BeginOpening;
        }

        void PlaceInFrontOfUser(bool logPlacement)
        {
            var camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null)
                return;
            // Keep the object at eye height even if the user glances downward.
            var viewForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            if (viewForward.sqrMagnitude < 0.01f)
                viewForward = camera.transform.forward.normalized;
            transform.position = camera.transform.position + viewForward * m_DistanceFromUser +
                                 Vector3.up * m_HeightOffset;
            transform.rotation = Quaternion.LookRotation(viewForward, Vector3.up);
            if (logPlacement)
                Debug.Log($"[Moodium Opening] Placed from head pose. Camera={camera.transform.position}, " +
                          $"Forward={viewForward}, Candy={transform.position}");
        }

        public void BeginOpening()
        {
            if (m_Triggered || State != OpeningState.Opening_Idle)
                return;
            m_Triggered = true;
            HideGestureGuide();
            m_HandTrail?.SetTrailActive(true);
            MoodiumAudioManager.Play(MoodiumAudioCue.OpeningCandyTouch, transform.position);
            if (m_TouchPrompt != null)
                m_TouchPrompt.SetActive(false);
            StartCoroutine(OpeningSequence());
        }

        IEnumerator ShowGestureGuideAfterDelay()
        {
            if (m_GestureGuideDelay > 0f)
                yield return new WaitForSeconds(m_GestureGuideDelay);
            m_GestureGuideRoutine = null;
            if (!m_Triggered && State == OpeningState.Opening_Idle)
                ShowGestureGuide();
        }

        void ShowGestureGuide()
        {
            if (m_GestureGuidePrefab == null || m_CandyInteraction == null || m_GestureGuideInstance != null)
                return;

            m_GestureGuideInstance = Instantiate(
                m_GestureGuidePrefab,
                m_CandyInteraction.transform,
                false);
            m_GestureGuideInstance.name = "Opening Gesture Guide";
            m_GestureGuideInstance.transform.localPosition = m_GestureGuideLocalOffset;
            m_GestureGuideInstance.transform.localRotation = Quaternion.identity;
            m_GestureGuideInstance.SetActive(true);
        }

        void HideGestureGuide()
        {
            if (m_GestureGuideRoutine != null)
            {
                StopCoroutine(m_GestureGuideRoutine);
                m_GestureGuideRoutine = null;
            }

            if (m_GestureGuideInstance == null)
                return;

            m_GestureGuideInstance.SetActive(false);
            if (Application.isPlaying)
                Destroy(m_GestureGuideInstance);
            m_GestureGuideInstance = null;
        }

        IEnumerator OpeningSequence()
        {
            State = OpeningState.Candy_Transform;
            MoodiumAudioManager.Play(MoodiumAudioCue.OpeningCandyCrack, transform.position);
            m_AnimationController.PlayCandyTransform();
            yield return new WaitForSeconds(m_AnimationController.CandyTransformDuration);

            State = OpeningState.Logo_Fade_In;
            MoodiumAudioManager.Play(MoodiumAudioCue.SpriteReveal, transform.position);
            MoodiumAudioManager.Play(MoodiumAudioCue.LogoAppear);
            yield return m_AnimationController.FadeInLogoText(m_LogoFadeDuration);

            State = OpeningState.Logo_Hold;
            yield return new WaitForSeconds(m_FinalLogoHold);

            yield return m_AnimationController.HideAll(m_HandoffDelay);
            m_HandTrail?.SetTrailActive(false);
            State = OpeningState.Opening_Finished;

            if (m_AppFlow != null)
                m_AppFlow.enabled = true;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Opening-only, world-space hand trails made from the same native URP round
    /// particle material already used by Moodium's verified visionOS feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OpeningHandTrailController : MonoBehaviour
    {
        [SerializeField, Range(0.001f, 0.02f)] float m_MinimumMovement = 0.0025f;

        XRHandSubsystem m_HandSubsystem;
        Vector3 m_LastLeftPosition;
        Vector3 m_LastRightPosition;
        bool m_HasLeftPosition;
        bool m_HasRightPosition;
        bool m_TrailActive;
        int m_ActivationFrame;

        public void SetTrailActive(bool active)
        {
            m_TrailActive = active;
            m_ActivationFrame = Time.frameCount + 2;
            m_HasLeftPosition = false;
            m_HasRightPosition = false;
            if (active)
            {
                Debug.Log("[Moodium Opening] Hand trails enabled.");
            }
        }

        void Update()
        {
            if (!m_TrailActive || Time.frameCount < m_ActivationFrame)
                return;
            if (!TryGetHandSubsystem())
                return;

            if (!MoodiumInteractionVFXManager.IsSharedParticleReady)
                return;

            m_HandSubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
            UpdateHandTrail(0, m_HandSubsystem.leftHand, ref m_LastLeftPosition, ref m_HasLeftPosition);
            UpdateHandTrail(1, m_HandSubsystem.rightHand, ref m_LastRightPosition, ref m_HasRightPosition);
        }

        bool TryGetHandSubsystem()
        {
            if (m_HandSubsystem == null)
                m_HandSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader
                    ?.GetLoadedSubsystem<XRHandSubsystem>();
            return m_HandSubsystem != null && m_HandSubsystem.running;
        }

        void UpdateHandTrail(
            int handIndex,
            XRHand hand,
            ref Vector3 lastPosition,
            ref bool hasPosition)
        {
            if (!hand.isTracked)
            {
                hasPosition = false;
                return;
            }
            // The trail belongs to the user's drawing fingertip rather than the
            // palm, so the visible path matches the finger passing by the logo.
            var fingertip = hand.GetJoint(XRHandJointID.IndexTip);
            if (fingertip.trackingState == XRHandJointTrackingState.None || !fingertip.TryGetPose(out var pose))
            {
                hasPosition = false;
                return;
            }

            var currentPosition = pose.position;
            if (!hasPosition)
            {
                lastPosition = currentPosition;
                hasPosition = true;
                return;
            }

            var movement = currentPosition - lastPosition;
            var distance = movement.magnitude;
            if (distance < m_MinimumMovement)
                return;

            var speed = distance / Mathf.Max(Time.deltaTime, 0.001f);
            var speed01 = Mathf.InverseLerp(0.015f, 0.45f, speed);
            var oppositeMotion = -movement.normalized;
            MoodiumInteractionVFXManager.EmitHandTrail(
                handIndex,
                currentPosition,
                oppositeMotion * Mathf.Lerp(0.002f, 0.010f, speed01),
                speed01,
                new Color(1f, 1f, 1f, 0.96f));
            lastPosition = currentPosition;
        }

        void OnDisable()
        {
            m_TrailActive = false;
        }
    }
}
