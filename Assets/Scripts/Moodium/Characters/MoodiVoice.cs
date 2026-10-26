using System.Collections.Generic;
using UnityEngine;

namespace Moodium.Characters
{
    /// <summary>
    /// Moodi's voice (MiniMax speech-2.8-hd, voice "Cute Spirit", Chinese and English).
    /// Clips: Resources/MoodiumVoice/{zh|en}/moodi_vo_{id}_{zh|en}.
    ///
    /// Rules:
    /// - The language follows PlayerPrefs "Moodium.Language" ("English" → en, anything else → zh).
    /// - Teaching lines (A2, A3, C1, C6, D3, E2) play at most once per session, and never after
    ///   MarkLearned(id). If Moodi is talking, a teaching line waits and plays 1 s after it ends.
    /// - Reaction lines never interrupt: if Moodi is talking they are skipped.
    /// - Some lines have their own cooldown (C5 20 s, D5 15 s, E1 60 s).
    /// - Variant groups (B1, C2, D4) pick a random line and never repeat the previous pick.
    /// The voice comes from Moodi's own position (3D AudioSource).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MoodiVoice : MonoBehaviour
    {
        public const string ResourceFolder = "MoodiumVoice";

        /// <summary>Every line id, in script order. Each has a zh and an en clip.</summary>
        public static readonly string[] AllIds =
        {
            "A1", "A2", "A3",
            "B1a", "B1b", "B2",
            "C1", "C2a", "C2b", "C2c", "C3", "C4", "C5", "C6",
            "D1", "D2", "D3", "D4a", "D4b", "D5", "D6n", "D6c", "D7",
            "E1", "E2"
        };

        static readonly HashSet<string> TeachingIds = new() { "A2", "A3", "C1", "C6", "D3", "E2" };

        static readonly Dictionary<string, float> Cooldowns = new() { { "C5", 20f }, { "D5", 15f }, { "E1", 60f } };

        static readonly Dictionary<string, string[]> Variants = new()
        {
            { "B1", new[] { "B1a", "B1b" } },
            { "C2", new[] { "C2a", "C2b", "C2c" } },
            { "D4", new[] { "D4a", "D4b" } }
        };

        static readonly HashSet<string> s_Taught = new();

        [SerializeField] AudioSource m_Source;
        [SerializeField, Range(0f, 1f)] float m_Volume = .9f;
        [Tooltip("Seconds to wait after Moodi stops talking before a queued teaching line starts.")]
        [SerializeField, Range(0f, 3f)] float m_TeachingGap = 1f;

        readonly Dictionary<string, AudioClip> m_Clips = new();
        readonly Dictionary<string, float> m_NextAllowed = new();
        readonly Dictionary<string, string> m_LastVariant = new();
        string m_Pending;
        string m_Current;
        float m_SpeechEndsAt = -10f;

        public static string LanguageCode =>
            PlayerPrefs.GetString("Moodium.Language", "Chinese") == "English" ? "en" : "zh";

        public static bool IsTeaching(string id) => TeachingIds.Contains(id);

        public static string ClipResourcePath(string id, string language) =>
            $"{ResourceFolder}/{language}/moodi_vo_{id}_{language}";

        /// <summary>Forget which teaching lines were already said (call when a new world session starts).</summary>
        public static void ResetSession() => s_Taught.Clear();

        /// <summary>The user already did what this teaching line explains, so never say it this session.</summary>
        public static void MarkLearned(string id) => s_Taught.Add(id);

        public bool IsSpeaking => m_Source != null && m_Source.isPlaying;
        public string CurrentLine => IsSpeaking ? m_Current : null;

        void Awake() => EnsureSource();

        void Update()
        {
            if (m_Pending == null || IsSpeaking || Time.time < m_SpeechEndsAt + m_TeachingGap) return;
            var id = m_Pending;
            m_Pending = null;
            Play(id);
        }

        /// <summary>Say one line by id. Returns true if it started (or was queued, for a teaching line).</summary>
        public bool Say(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (Variants.ContainsKey(id)) return SayOneOf(id);
            if (TeachingIds.Contains(id))
            {
                if (s_Taught.Contains(id)) return false;
                var busy = IsSpeaking || Time.time < m_SpeechEndsAt + m_TeachingGap;
                if (busy && m_Pending != null) return false; // one queued teaching line at a time; ask again later
                s_Taught.Add(id);
                if (busy) { m_Pending = id; return true; }
                return Play(id);
            }
            if (IsSpeaking) return false;
            if (m_NextAllowed.TryGetValue(id, out var next) && Time.time < next) return false;
            return Play(id);
        }

        /// <summary>Say a random line from a variant group ("B1", "C2" or "D4"), never the same one twice in a row.</summary>
        public bool SayOneOf(string group)
        {
            if (!Variants.TryGetValue(group, out var ids)) return Say(group);
            if (IsSpeaking) return false;
            m_LastVariant.TryGetValue(group, out var last);
            var pick = ids[Random.Range(0, ids.Length)];
            if (ids.Length > 1 && pick == last) pick = ids[(System.Array.IndexOf(ids, pick) + 1) % ids.Length];
            m_LastVariant[group] = pick;
            return Play(pick);
        }

        /// <summary>D6 for the world you are in.</summary>
        public bool SayGardenFound(bool candy) => Say(candy ? "D6c" : "D6n");

        public void Stop()
        {
            m_Pending = null;
            if (m_Source != null) m_Source.Stop();
        }

        bool Play(string id)
        {
            var clip = Load(id);
            if (clip == null) return false;
            EnsureSource();
            m_Source.Stop();
            m_Source.clip = clip;
            m_Source.volume = m_Volume;
            m_Source.Play();
            m_Current = id;
            m_SpeechEndsAt = Time.time + clip.length;
            if (Cooldowns.TryGetValue(id, out var cooldown)) m_NextAllowed[id] = Time.time + cooldown;
            return true;
        }

        AudioClip Load(string id)
        {
            var path = ClipResourcePath(id, LanguageCode);
            if (m_Clips.TryGetValue(path, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"[Moodi Voice] Missing clip {path}");
            m_Clips[path] = clip;
            return clip;
        }

        void EnsureSource()
        {
            if (m_Source != null) return;
            m_Source = gameObject.AddComponent<AudioSource>();
            m_Source.playOnAwake = false;
            m_Source.loop = false;
            m_Source.spatialBlend = 1f;
            m_Source.rolloffMode = AudioRolloffMode.Logarithmic;
            m_Source.minDistance = .4f;
            m_Source.maxDistance = 6f;
            m_Source.dopplerLevel = 0f;
        }
    }
}
