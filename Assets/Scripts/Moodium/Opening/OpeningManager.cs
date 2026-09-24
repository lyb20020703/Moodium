using System.Collections;
using Moodium.Flow;
using Moodium.Audio;
using Moodium.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using UnityEngine.Video;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [SerializeField] OpeningVideoSequenceConfig m_VideoSequenceConfig;
        // Legacy fallback keeps older prefabs functional while the sequence asset
        // becomes the single inspector-editable source of video/timing data.
        [SerializeField] VideoClip m_PreOpeningVideo;
        [SerializeField, Min(0f)] float m_PreOpeningVideoTimeout = 30f;
        [SerializeField, Min(0.1f)] float m_InteractiveVideoPreviewDuration = 3.08f;
        [SerializeField] float m_PreOpeningVideoHeightOffset = -0.05f;
        [SerializeField, Min(0.2f)] float m_PreOpeningVideoDistanceFromUser = 1.35f;
        [SerializeField] GameObject m_LeftHandGuidePrefab;
        [SerializeField] GameObject m_RightHandGuidePrefab;
        [SerializeField] TMP_FontAsset m_HandPromptFont;
        [SerializeField] Sprite m_LanguageButtonRoundedSprite;

        MoodiumAppFlowController m_AppFlow;
        OpeningHandTrailController m_HandTrail;
        Coroutine m_GestureGuideRoutine;
        GameObject m_GestureGuideInstance;
        bool m_Triggered;
        bool m_ReadyForCandy;
        OpeningTutorialFlowGate m_TutorialFlowGate;

        public OpeningState State { get; private set; } = OpeningState.Opening_Idle;

#if UNITY_EDITOR
        void OnValidate()
        {
            // These are runtime-created UI pieces. Keep their project references
            // stable if Unity reserializes the opening prefab while scripts change.
            var changed = false;
            if (m_VideoSequenceConfig == null)
            {
                m_VideoSequenceConfig = AssetDatabase.LoadAssetAtPath<OpeningVideoSequenceConfig>(
                    "Assets/Resources/MoodiumOpening/OpeningVideoSequenceConfig.asset");
                changed = true;
            }
            if (m_PreOpeningVideo == null)
            {
                m_PreOpeningVideo = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Video/VideoPLUS.mp4");
                changed = true;
            }
            if (m_LeftHandGuidePrefab == null)
            {
                m_LeftHandGuidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefab/UI-LeftHand.prefab");
                changed = true;
            }
            if (m_RightHandGuidePrefab == null)
            {
                m_RightHandGuidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefab/UI-RightHand.prefab");
                changed = true;
            }
            if (m_HandPromptFont == null)
            {
                m_HandPromptFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Moodium/Fonts/AlibabaPuHuiTi Moodium SDF.asset");
                changed = true;
            }
            if (m_LanguageButtonRoundedSprite == null)
            {
                m_LanguageButtonRoundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Moodium/WristHUD_Rounded.png");
                changed = true;
            }
            if (changed)
                EditorUtility.SetDirty(this);
        }
#endif

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
            }
            if (m_CandyInteraction != null)
                m_CandyInteraction.Touched += BeginOpening;
        }

        IEnumerator Start()
        {
            m_TutorialFlowGate = new OpeningTutorialFlowGate();
            // Do not leave the candy's trigger/input live while the app-launch
            // gesture is still being processed. Otherwise the initial hand proxy
            // can consume the candy before the pre-roll has finished.
            if (m_CandyInteraction != null)
                m_CandyInteraction.enabled = false;
            m_ReadyForCandy = false;
            if (m_TouchPrompt != null)
                m_TouchPrompt.SetActive(false);
            m_AnimationController?.HideOpeningVisuals();

            var timeout = Time.realtimeSinceStartup + 10f;
            while (Camera.main == null && Time.realtimeSinceStartup < timeout)
                yield return null;
            yield return PlayPreOpeningVideo();
            if (m_TutorialFlowGate != null && !m_TutorialFlowGate.CanEnterWorldSelection)
            {
                Debug.LogError("[Moodium Opening] Tutorial video did not complete. World selection remains blocked instead of skipping the tutorial.");
                yield break;
            }
            // The opening video now hands directly to the tracked-object world
            // selection. Portal assets remain available for future use but are
            // intentionally outside the launch path.
            State = OpeningState.Opening_Finished;
            m_AnimationController?.HideOpeningVisuals();
            m_AppFlow?.ShowObjectWorldSelectionFromOpening();
            yield break;
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
            if (m_CandyInteraction != null)
                m_CandyInteraction.enabled = true;
            m_ReadyForCandy = true;
            if (m_TouchPrompt != null)
                m_TouchPrompt.SetActive(true);
            m_GestureGuideRoutine = StartCoroutine(ShowGestureGuideAfterDelay());
            State = OpeningState.Opening_Idle;
        }

        IEnumerator PlayPreOpeningVideo()
        {
            var sequence = m_VideoSequenceConfig;
            var videoClip = sequence != null && sequence.VideoClip != null ? sequence.VideoClip : m_PreOpeningVideo;
            if (videoClip == null)
                yield break;
            var camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null)
                yield break;
            var player = GetComponent<VideoPlayer>();
            if (player == null)
                player = gameObject.AddComponent<VideoPlayer>();
            var renderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
            {
                name = "Moodium Opening Video Render Texture"
            };
            renderTexture.Create();
            var display = GameObject.CreatePrimitive(PrimitiveType.Quad);
            display.name = "Moodium Opening Video Display";
            var viewForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            if (viewForward.sqrMagnitude < 0.01f)
                viewForward = camera.transform.forward.normalized;
            // The sequence asset is the single source of truth for placement.
            // This object is created at runtime, so editing its Play Mode
            // transform would otherwise be discarded on the next launch.
            var videoDistance = sequence != null ? sequence.DistanceFromUser : m_PreOpeningVideoDistanceFromUser;
            var videoHeight = sequence != null ? sequence.HeightOffset : m_PreOpeningVideoHeightOffset;
            var videoPosition = camera.transform.position + viewForward * videoDistance +
                                Vector3.up * videoHeight;
            // Explicit world Y is intentional: the display is a runtime object
            // and the requested placement must not drift with head-pose height.
            if (sequence != null)
                videoPosition.y = sequence.WorldPositionY;
            display.transform.position = videoPosition;
            // A Unity Quad faces its local -Z axis. Align local +Z with the view
            // direction so its rendered face points back toward the viewer.
            display.transform.rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            display.transform.localScale = new Vector3(1.28f, 0.72f, 1f);
            Destroy(display.GetComponent<Collider>());
            // VideoPlayer updates happen outside Camera.Render. PolySpatial cannot
            // detect those RenderTexture changes automatically, so Vision Pro would
            // otherwise receive only the first decoded frame while audio continues.
            var renderTextureUpdater = display.AddComponent<PolySpatialVideoRenderTextureUpdater>();
            renderTextureUpdater.Configure(renderTexture);
            var videoCollider = display.AddComponent<BoxCollider>();
            videoCollider.isTrigger = true;
            videoCollider.size = new Vector3(1f, 1f, 0.04f);
            var videoInteraction = display.AddComponent<OpeningVideoInteraction>();
            var handGuideLayout = new IntroVideoHandGuideLayout();
            var leftGuide = CreateVideoHandGuide(display.transform, m_LeftHandGuidePrefab,
                "Opening Video Left Hand", handGuideLayout.LeftLocalPosition, handGuideLayout.LocalScale,
                handGuideLayout.LeftHandLocalZ);
            var rightGuide = CreateVideoHandGuide(display.transform, m_RightHandGuidePrefab,
                "Opening Video Right Hand", handGuideLayout.RightLocalPosition, handGuideLayout.LocalScale,
                handGuideLayout.RightHandLocalZ);
            var prompt = CreateVideoHandPrompt(display.transform, m_HandPromptFont);
            leftGuide?.SetActive(false);
            rightGuide?.SetActive(false);
            prompt?.SetActive(false);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"));
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", renderTexture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", renderTexture);
            // OpenningAni is HEVC-with-Alpha. Preserve its alpha channel instead
            // of presenting transparent video pixels as an opaque black rectangle.
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            display.GetComponent<Renderer>().sharedMaterial = material;
            player.playOnAwake = false;
            player.isLooping = false;
            player.source = VideoSource.VideoClip;
            player.clip = videoClip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;
            player.Prepare();
            Debug.Log($"[Moodium Opening] Preparing intro video: {videoClip.name}.");
            var deadline = Time.realtimeSinceStartup + m_PreOpeningVideoTimeout;
            while (!player.isPrepared && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!player.isPrepared)
            {
                Debug.LogWarning("[Moodium Opening] Intro video preparation timed out; continuing to the opening animation.");
                Destroy(leftGuide);
                Destroy(rightGuide);
                Destroy(prompt);
                Destroy(display);
                Destroy(material);
                renderTexture.Release();
                Destroy(renderTexture);
                yield break;
            }
            player.Play();
            Debug.Log("[Moodium Opening] Intro video started.");
            var noInteractionElapsed = 0f;
            var guideRevealStart = Time.realtimeSinceStartup + 5f;
            var promptRevealStart = Time.realtimeSinceStartup + 1f;
            var guideFadeDuration = 0.8f;
            var guideAnimationTime = 0f;
            var introStart = sequence != null ? sequence.IntroLoopStartTime : 0f;
            var introEnd = sequence != null ? sequence.IntroLoopEndTime : m_InteractiveVideoPreviewDuration;
            var languageStart = OpeningVideoTimeline.FrameToSeconds(OpeningVideoTimeline.LanguageLoop.StartFrame);
            var languageEnd = OpeningVideoTimeline.FrameToSeconds(OpeningVideoTimeline.LanguageLoop.EndFrame);
            var gate = new IntroVideoGate(introStart, Mathf.Min(introEnd, (float)videoClip.length));
            var previewSeekCompleted = false;
            VideoPlayer.EventHandler onSeekCompleted = _ => previewSeekCompleted = true;
            player.seekCompleted += onSeekCompleted;
            var previewLoopCount = 0;
            while (!gate.IsActivated)
            {
                noInteractionElapsed += Time.unscaledDeltaTime;
                if (previewSeekCompleted && gate.TryCompletePreviewSeek(player.time))
                {
                    previewSeekCompleted = false;
                    player.Play();
                }
                if (videoInteraction.Activated || noInteractionElapsed >= 30f)
                {
                    if (!videoInteraction.Activated)
                        Debug.Log("[Moodium Opening] Intro video auto-activated after 30 seconds without interaction.");
                    gate.Activate();
                    leftGuide?.SetActive(false);
                    rightGuide?.SetActive(false);
                    prompt?.SetActive(false);
                    player.time = gate.ResumeTime;
                    player.Play();
                    break;
                }
                var reveal = Mathf.Clamp01((Time.realtimeSinceStartup - guideRevealStart) / guideFadeDuration);
                guideAnimationTime += Time.unscaledDeltaTime;
                AnimateVideoHandGuide(leftGuide, handGuideLayout.LeftLocalPosition, handGuideLayout.LocalScale,
                    reveal, guideAnimationTime, 0f, handGuideLayout.LeftHandLocalZ);
                AnimateVideoHandGuide(rightGuide, handGuideLayout.RightLocalPosition, handGuideLayout.LocalScale,
                    reveal, guideAnimationTime, Mathf.PI, handGuideLayout.RightHandLocalZ);
                var promptReveal = Mathf.Clamp01((Time.realtimeSinceStartup - promptRevealStart) / 0.5f);
                AnimateVideoHandPrompt(prompt, promptReveal, guideAnimationTime);
                if (gate.TryBeginPreviewLoop(player.time))
                {
                    previewLoopCount++;
                    Debug.Log($"[Moodium Opening] Preview loop {previewLoopCount}: time={player.time:0.000}, playing={player.isPlaying}.");
                    player.time = gate.PreviewStartTime;
                }
                yield return null;
            }

            var languageChoice = CreateVideoLanguageChoice(display.transform, m_HandPromptFont, m_LanguageButtonRoundedSprite);
            languageChoice.SetActive(false);
            var languageChoiceController = languageChoice.GetComponent<OpeningVideoLanguageChoice>();
            var languagePromptShownAt = -1f;
            var languageGate = new IntroVideoGate(languageStart, System.Math.Min(languageEnd, videoClip.length));
            var languageSeekCompleted = false;
            VideoPlayer.EventHandler onLanguageSeekCompleted = _ => languageSeekCompleted = true;
            player.seekCompleted += onLanguageSeekCompleted;
            while (languageGate.ShouldKeepSelectionPromptVisible(
                languageChoiceController.HasSelection))
            {
                // Some visionOS video backends report isPlaying=false for a frame
                // after a seek or when the clip reaches its end. That must not be
                // interpreted as an answer to the language question.
                if (!player.isPlaying)
                {
                    player.time = languageGate.PreviewStartTime;
                    player.Play();
                }
                if (player.time >= languageGate.PreviewStartTime)
                {
                    languageChoice.SetActive(true);
                    if (languagePromptShownAt < 0f)
                        languagePromptShownAt = Time.realtimeSinceStartup;
                }
                if (!languageChoiceController.HasSelection && languagePromptShownAt >= 0f &&
                    Time.realtimeSinceStartup - languagePromptShownAt >=
                    OpeningVideoTimeline.LanguageAutoSelectTimeoutSeconds)
                {
                    Debug.Log("[Moodium Opening] Language choice timed out; temporarily selecting Chinese for video-flow verification.");
                    languageChoiceController.SelectChineseWhenTimedOut();
                }
                if (languageChoiceController.HasSelection)
                {
                    while (!languageChoiceController.IsReadyForVideoTransition)
                        yield return null;
                    var selectedLanguage = languageChoiceController.SelectedLanguage;
                    languageGate.Activate();
                    languageChoice.SetActive(false);
                    yield return PlayHardwareTutorial(
                        player,
                        display.transform,
                        string.Equals(selectedLanguage, "English", System.StringComparison.OrdinalIgnoreCase));
                    break;
                }
                if (languageSeekCompleted && languageGate.TryCompletePreviewSeek(player.time))
                {
                    languageSeekCompleted = false;
                    player.Play();
                }
                if (languageGate.TryBeginPreviewLoop(player.time))
                {
                    player.time = languageGate.PreviewStartTime;
                    player.Play();
                }
                yield return null;
            }
            player.seekCompleted -= onLanguageSeekCompleted;
            player.seekCompleted -= onSeekCompleted;
            if (m_TutorialFlowGate != null && !m_TutorialFlowGate.CanEnterWorldSelection)
            {
                player.Stop();
                ShowTutorialLoadFailure(display.transform, m_HandPromptFont);
                yield break;
            }
            player.Stop();
            Debug.Log("[Moodium Opening] Intro and hardware tutorial completed; opening World selection.");
            player.targetTexture = null;
            Destroy(leftGuide);
            Destroy(rightGuide);
            Destroy(prompt);
            Destroy(languageChoice);
            Destroy(display);
            Destroy(material);
            renderTexture.Release();
            Destroy(renderTexture);
        }

        IEnumerator PlayHardwareTutorial(VideoPlayer player, Transform display, bool english)
        {
            // Reaching the language screen proves this same player has already
            // decoded the combined clip. Do not re-validate transient backend
            // flags here; visionOS can update them around seeks.
            if (player == null || player.clip == null)
            {
                m_TutorialFlowGate?.MarkTutorialFailed();
                Debug.LogError("[Moodium Opening] Combined tutorial video is not prepared.");
                yield break;
            }
            var timeline = english ? OpeningVideoTimeline.English : OpeningVideoTimeline.Chinese;
            // Do not inspect VideoPlayer.frameCount here. visionOS can report
            // partial metadata immediately after a seek despite decoding this
            // same clip successfully. The imported asset is validated before
            // build; runtime playback must continue from the configured frames.
            var choiceObject = CreateVideoHardwareChoice(display, m_HandPromptFont, m_LanguageButtonRoundedSprite, english);
            choiceObject.SetActive(false);

            yield return SeekToFrameAndPlay(player, timeline.EntryFrame);
            var questionDeadline = Time.realtimeSinceStartup +
                                   Mathf.Max(2f, (float)OpeningVideoTimeline.FrameToSeconds(
                                       timeline.QuestionLoop.StartFrame - timeline.EntryFrame) + 5f);
            while (CurrentFrame(player) < timeline.QuestionLoop.StartFrame &&
                   Time.realtimeSinceStartup < questionDeadline)
            {
                if (!player.isPlaying)
                    player.Play();
                yield return null;
            }

            choiceObject.SetActive(true);
            var hardwareChoice = choiceObject.GetComponent<OpeningVideoHardwareChoice>();
            while (!hardwareChoice.HasSelection || !hardwareChoice.IsReadyForVideoTransition)
            {
                if (CurrentFrame(player) >= timeline.QuestionLoop.EndFrame || !player.isPlaying)
                    yield return SeekToFrameAndPlay(player, timeline.QuestionLoop.StartFrame);
                yield return null;
            }

            choiceObject.SetActive(false);
            var branch = hardwareChoice.HasHardware ? timeline.Yes : timeline.No;
            yield return SeekToFrameAndPlay(player, branch.StartFrame);
            var branchDeadline = Time.realtimeSinceStartup + Mathf.Max(2f,
                (float)OpeningVideoTimeline.FrameToSeconds(branch.EndFrame - branch.StartFrame) + 3f);
            while (CurrentFrame(player) < branch.EndFrame && Time.realtimeSinceStartup < branchDeadline)
            {
                if (!player.isPlaying)
                    player.Play();
                yield return null;
            }
            player.Stop();
            Destroy(choiceObject);
            m_TutorialFlowGate?.MarkTutorialCompleted();
        }

        static long CurrentFrame(VideoPlayer player)
        {
            return player.frame >= 0
                ? player.frame
                : (long)System.Math.Round(player.time * OpeningVideoTimeline.FramesPerSecond);
        }

        IEnumerator SeekToFrameAndPlay(VideoPlayer player, long targetFrame)
        {
            var seekCompleted = false;
            VideoPlayer.EventHandler onSeekCompleted = _ => seekCompleted = true;
            player.seekCompleted += onSeekCompleted;
            player.Pause();
            player.frame = targetFrame;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!seekCompleted && CurrentFrame(player) != targetFrame &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            player.seekCompleted -= onSeekCompleted;
            if (CurrentFrame(player) != targetFrame)
                Debug.LogWarning($"[Moodium Opening] Seek settled at frame {CurrentFrame(player)} instead of {targetFrame}.");
            player.Play();
        }

        static void ShowTutorialLoadFailure(Transform display, TMP_FontAsset font)
        {
            var notice = new GameObject("Opening Tutorial Load Failure");
            notice.transform.SetParent(display, false);
            notice.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            notice.transform.localRotation = Quaternion.identity;
            var parentScale = display.localScale;
            notice.transform.localScale = new Vector3(
                SafeScaleDivide(0.04f, parentScale.x),
                SafeScaleDivide(0.04f, parentScale.y),
                SafeScaleDivide(0.04f, parentScale.z));
            var text = notice.AddComponent<TextMeshPro>();
            text.text = "诊断版本 VP-20260924-02：教学流程在设备端被中断。\nDiagnostic VP-20260924-02: the tutorial flow was interrupted on this device.";
            text.font = font;
            text.fontSize = 7f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.outlineWidth = 0.08f;
            text.outlineColor = new Color(.08f, .02f, .16f, .9f);
        }

        static GameObject CreateVideoHandGuide(Transform parent, GameObject prefab, string objectName,
            Vector3 localPosition, Vector3 localScale, float handLocalZ)
        {
            if (prefab == null)
                return null;
            var wrapper = new GameObject(objectName);
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.localPosition = localPosition;
            wrapper.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            var parentScale = parent.localScale;
            wrapper.transform.localScale = new Vector3(
                SafeScaleDivide(localScale.x, parentScale.x),
                SafeScaleDivide(localScale.y, parentScale.y),
                SafeScaleDivide(localScale.z, parentScale.z));
            var guide = Instantiate(prefab, wrapper.transform, false);
            guide.name = prefab.name;
            guide.transform.localPosition = new Vector3(0f, 0f, handLocalZ);
            guide.transform.localRotation = Quaternion.identity;
            guide.transform.localScale = Vector3.one;
            ConfigureVideoHandGuideMaterials(guide);
            guide.SetActive(true);
            return wrapper;
        }

        static float SafeScaleDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }

        static void AnimateVideoHandGuide(GameObject guide, Vector3 basePosition, Vector3 baseScale,
            float reveal, float time, float phase, float handLocalZ)
        {
            if (guide == null)
                return;
            if (reveal <= 0f)
            {
                guide.SetActive(false);
                return;
            }
            guide.SetActive(true);
            // Keep the hand guide within the requested Z ranges:
            // left -1.5..-1.1, right 1.1..1.5.
            var floatingZ = Mathf.Sin(time * Mathf.PI * 0.8f + phase) * 0.2f;
            guide.transform.localPosition = basePosition;
            if (guide.transform.childCount > 0)
            {
                var hand = guide.transform.GetChild(0);
                hand.localPosition = new Vector3(0f, 0f, handLocalZ + floatingZ);
            }
            SetVideoHandGuideAlpha(guide, reveal);
        }

        static GameObject CreateVideoHandPrompt(Transform parent, TMP_FontAsset font)
        {
            var promptObject = new GameObject("Opening Video Hand Prompt");
            promptObject.transform.SetParent(parent, false);
            promptObject.transform.localPosition = new Vector3(0f, -0.39f, -0.02f);
            promptObject.transform.localRotation = Quaternion.identity;
            var parentScale = parent.localScale;
            promptObject.transform.localScale = new Vector3(
                SafeScaleDivide(0.045f, parentScale.x),
                SafeScaleDivide(0.045f, parentScale.y),
                SafeScaleDivide(0.045f, parentScale.z));
            var text = promptObject.AddComponent<TextMeshPro>();
            text.text = "捏一下，打开惊喜\nPinch to discover the surprise";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 8f;
            text.lineSpacing = 12f;
            text.font = font;
            text.color = Color.white;
            text.outlineWidth = 0.08f;
            text.outlineColor = new Color(0.12f, 0.04f, 0.2f, 0.8f);
            return promptObject;
        }

        static GameObject CreateVideoLanguageChoice(Transform parent, TMP_FontAsset font, Sprite roundedSprite)
        {
            var choice = new GameObject("Opening Video Language Choice", typeof(RectTransform));
            choice.transform.SetParent(parent, false);
            choice.transform.localPosition = new Vector3(0f, -0.05f, -0.025f);
            choice.transform.localRotation = Quaternion.identity;
            var parentScale = parent.localScale;
            choice.transform.localScale = new Vector3(
                SafeScaleDivide(0.0012f, parentScale.x),
                SafeScaleDivide(0.0012f, parentScale.y),
                SafeScaleDivide(0.0012f, parentScale.z));
            choice.AddComponent<OpeningVideoLanguageChoice>().Configure(font, roundedSprite);
            return choice;
        }

        static GameObject CreateVideoHardwareChoice(Transform parent, TMP_FontAsset font, Sprite roundedSprite, bool english)
        {
            var choice = new GameObject("Opening Video Hardware Choice", typeof(RectTransform));
            choice.transform.SetParent(parent, false);
            choice.transform.localPosition = new Vector3(0f, -0.05f, -0.025f);
            choice.transform.localRotation = Quaternion.identity;
            var parentScale = parent.localScale;
            choice.transform.localScale = new Vector3(
                SafeScaleDivide(0.0012f, parentScale.x),
                SafeScaleDivide(0.0012f, parentScale.y),
                SafeScaleDivide(0.0012f, parentScale.z));
            choice.AddComponent<OpeningVideoHardwareChoice>().Configure(font, roundedSprite, english);
            return choice;
        }

        static void AnimateVideoHandPrompt(GameObject prompt, float reveal, float time)
        {
            if (prompt == null)
                return;
            prompt.SetActive(reveal > 0f);
            var text = prompt.GetComponent<TextMeshPro>();
            if (text == null)
                return;
            var color = text.color;
            color.a = reveal;
            text.color = color;
            var basePosition = new Vector3(0f, -0.39f, -0.02f);
            prompt.transform.localPosition = basePosition + new Vector3(0f, Mathf.Sin(time * Mathf.PI * 0.8f) * 0.006f, 0f);
        }

        static void ConfigureVideoHandGuideMaterials(GameObject guide)
        {
            foreach (var renderer in guide.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    if (material == null)
                        continue;
                    if (material.HasProperty("_Surface"))
                    {
                        material.SetFloat("_Surface", 1f);
                        material.SetFloat("_Blend", 0f);
                        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetFloat("_ZWrite", 0f);
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    }
                }
                renderer.materials = materials;
            }
        }

        static void SetVideoHandGuideAlpha(GameObject guide, float alpha)
        {
            foreach (var renderer in guide.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    if (material == null)
                        continue;
                    if (material.HasProperty("_BaseColor"))
                    {
                        var color = material.GetColor("_BaseColor");
                        color.a = alpha;
                        material.SetColor("_BaseColor", color);
                    }
                    else if (material.HasProperty("_Color"))
                    {
                        var color = material.color;
                        color.a = alpha;
                        material.color = color;
                    }
                }
            }
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
            if (!m_ReadyForCandy || m_Triggered || State != OpeningState.Opening_Idle)
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
                m_AppFlow.ShowObjectWorldSelectionFromOpening();
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
