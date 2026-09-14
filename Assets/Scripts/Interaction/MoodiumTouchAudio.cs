using UnityEngine;

namespace Moodium.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MoodiumTouchAudio : MonoBehaviour
    {
        [SerializeField] AudioClip m_TouchSound;
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.7f;
        [SerializeField] Vector2 m_PitchRange = new(0.96f, 1.04f);

        AudioSource m_Source;

        public AudioClip TouchSound => m_TouchSound;

        public void Configure(AudioClip touchSound) => m_TouchSound = touchSound;

        void Awake()
        {
            m_Source = GetComponent<AudioSource>();
            m_Source.playOnAwake = false;
            m_Source.loop = false;
            m_Source.spatialBlend = 1f;
            m_Source.minDistance = 0.1f;
            m_Source.maxDistance = 4f;
            m_Source.rolloffMode = AudioRolloffMode.Linear;
        }

        public void Play()
        {
            if (m_TouchSound == null)
                return;
            if (m_Source == null)
                m_Source = GetComponent<AudioSource>();
            m_Source.pitch = Random.Range(
                Mathf.Min(m_PitchRange.x, m_PitchRange.y),
                Mathf.Max(m_PitchRange.x, m_PitchRange.y));
            m_Source.PlayOneShot(m_TouchSound, m_Volume);
            Debug.Log($"[Moodium Touch Audio] {name}: {m_TouchSound.name}");
        }
    }
}
