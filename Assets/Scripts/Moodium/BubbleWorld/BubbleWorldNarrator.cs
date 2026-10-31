using System.Collections;
using System.Collections.Generic;
using Moodium.Characters;
using UnityEngine;

namespace Moodium.NatureWorld
{
    /// <summary>
    /// Moodi's spoken welcome when a bubble world layer opens around the viewer (one short, calm intro per layer).
    /// Moodi is hidden inside the bubble, so this plays as a non-spatial voice close to the listener instead of
    /// through MoodiVoice's 3D emitter. Each layer is introduced once per world session.
    /// Clips: Resources/MoodiumVoice/{zh|en}/moodi_vo_{W1c|W2c|W1n|W2n}_{lang}.wav
    /// </summary>
    public sealed class BubbleWorldNarrator : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float m_Volume = .9f;
        [SerializeField] float m_Delay = 1f;
        const float FadeSeconds = .5f;

        AudioSource m_Source;
        Coroutine m_Routine;
        readonly HashSet<string> m_Said = new HashSet<string>();

        /// <summary>Intro line for a bubble world folder: W1c candy marshmallow, W2c candy mint chocolate,
        /// W1n green forest, W2n snowy forest. Null for an unknown folder.</summary>
        public static string IntroId(string folder) => folder switch
        {
            BubbleWorldData.CandyFolder => "W1c",
            BubbleWorldData.NestedCandyFolder => "W2c",
            BubbleWorldData.NatureFolder => "W1n",
            BubbleWorldData.NestedNatureFolder => "W2n",
            _ => null,
        };

        public static readonly string[] AllIds = { "W1c", "W2c", "W1n", "W2n" };

        /// <summary>True from the moment an intro is scheduled until it has finished (or was stopped).</summary>
        public bool IsSpeaking => m_Routine != null;

        /// <summary>Forget which layers were introduced (call when a new world session starts).</summary>
        public void ResetSession() { Stop(); m_Said.Clear(); }

        /// <summary>Introduce the layer that just opened. Stops any intro still playing from the outer layer.</summary>
        public bool PlayIntro(string folder)
        {
            var id = IntroId(folder);
            if (id == null || m_Said.Contains(id)) return false;
            var clip = Resources.Load<AudioClip>(MoodiVoice.ClipResourcePath(id, MoodiVoice.LanguageCode));
            if (clip == null) { Debug.LogWarning($"[Bubble World] Missing intro voice {id} ({MoodiVoice.LanguageCode})."); return false; }
            m_Said.Add(id);
            StopRoutine();
            m_Routine = StartCoroutine(Speak(clip));
            return true;
        }

        /// <summary>Fade the intro out (the user did something Moodi needs to answer, or left the bubble).</summary>
        public void Stop()
        {
            if (m_Routine == null) return;
            StopRoutine();
            if (m_Source != null && m_Source.isPlaying && isActiveAndEnabled) m_Routine = StartCoroutine(FadeOut());
            else if (m_Source != null) m_Source.Stop();
        }

        IEnumerator Speak(AudioClip clip)
        {
            yield return new WaitForSeconds(m_Delay);
            var source = EnsureSource();
            source.Stop();
            source.clip = clip;
            source.volume = m_Volume;
            source.Play();
            while (source.isPlaying) yield return null;
            m_Routine = null;
        }

        IEnumerator FadeOut()
        {
            var start = m_Source.volume;
            for (var t = 0f; t < FadeSeconds && m_Source.isPlaying; t += Time.deltaTime)
            {
                m_Source.volume = Mathf.Lerp(start, 0f, t / FadeSeconds);
                yield return null;
            }
            m_Source.Stop();
            m_Routine = null;
        }

        void StopRoutine()
        {
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = null;
        }

        AudioSource EnsureSource()
        {
            if (m_Source != null) return m_Source;
            m_Source = gameObject.AddComponent<AudioSource>();
            m_Source.playOnAwake = false;
            m_Source.loop = false;
            m_Source.spatialBlend = 0f;
            m_Source.priority = 0;
            return m_Source;
        }

        void OnDisable()
        {
            StopRoutine();
            if (m_Source != null) m_Source.Stop();
        }
    }
}
