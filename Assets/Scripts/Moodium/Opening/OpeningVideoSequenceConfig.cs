using UnityEngine;
using UnityEngine.Video;

namespace Moodium.Opening
{
    /// <summary>Inspector-editable timing and media settings for the app's opening video.</summary>
    [CreateAssetMenu(fileName = "OpeningVideoSequenceConfig", menuName = "Moodium/Opening Video Sequence")]
    public sealed class OpeningVideoSequenceConfig : ScriptableObject
    {
        [SerializeField] VideoClip m_VideoClip;
        [SerializeField, Min(0f)] float m_IntroLoopStartTime;
        [SerializeField, Min(0.1f)] float m_IntroLoopEndTime = 3.08f;
        [SerializeField, Min(0f)] float m_LanguageLoopStartTime = 24.14f;
        [SerializeField, Min(0.1f)] float m_LanguageLoopEndTime = 32f;
        [Header("Eye-relative placement")]
        [SerializeField, Min(0.2f)] float m_DistanceFromUser = 1.35f;
        [SerializeField] float m_HeightOffset = -0.05f;
        [SerializeField] float m_WorldPositionY = 1f;

        public VideoClip VideoClip => m_VideoClip;
        public float IntroLoopStartTime => m_IntroLoopStartTime;
        public float IntroLoopEndTime => Mathf.Max(m_IntroLoopStartTime + 0.01f, m_IntroLoopEndTime);
        public float LanguageLoopStartTime => m_LanguageLoopStartTime;
        public float LanguageLoopEndTime => Mathf.Max(m_LanguageLoopStartTime + 0.01f, m_LanguageLoopEndTime);
        public float DistanceFromUser => m_DistanceFromUser;
        public float HeightOffset => m_HeightOffset;
        public float WorldPositionY => m_WorldPositionY;
    }
}
