using TMPro;
using UnityEngine;

namespace Moodium.Reality
{
    /// <summary>One-shot world-space instruction. It deliberately has no panel so a transparent guide video can replace it later.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldEnvironmentPrompt : MonoBehaviour
    {
        const float WorldScale = .035f;
        const float VerticalOffset = .18f;

        TextMeshPro m_Text;
        Camera m_Camera;
        TMP_FontAsset m_ChineseFont;
        TMP_FontAsset m_EnglishFont;

        public void Configure(Camera camera, TMP_FontAsset chineseFont, TMP_FontAsset englishFont)
        {
            m_Camera = camera;
            m_ChineseFont = chineseFont;
            m_EnglishFont = englishFont;
        }

        public void Show(bool natureWorld, bool english, Vector3 worldAnchor)
        {
            // TMP font sizes are authored in local units.  Keep the prompt close to
            // subtitle scale in the real world instead of several metres wide.
            transform.localScale = Vector3.one * WorldScale;
            if (m_Text == null)
            {
                m_Text = gameObject.AddComponent<TextMeshPro>();
                m_Text.alignment = TextAlignmentOptions.Center;
                m_Text.fontSize = 4.8f;
                m_Text.rectTransform.sizeDelta = new Vector2(18f, 2.8f);
                m_Text.color = Color.white;
                m_Text.outlineWidth = .08f;
                m_Text.outlineColor = new Color(.06f, .02f, .12f, .9f);
            }
            m_Text.font = english ? m_EnglishFont : m_ChineseFont;
            m_Text.text = WorldEnvironmentPromptPolicy.Message(natureWorld, english);
            transform.position = worldAnchor + Vector3.up * VerticalOffset;
            gameObject.SetActive(true);
            FaceCamera();
        }

        void LateUpdate() => FaceCamera();

        void FaceCamera()
        {
            if (m_Camera == null)
                m_Camera = Camera.main;
            if (m_Camera == null)
                return;
            // TMP's front face is its local +Z, so it must point back toward the viewer.
            var direction = m_Camera.transform.position - transform.position;
            if (direction.sqrMagnitude > .0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, m_Camera.transform.up);
        }
    }
}
