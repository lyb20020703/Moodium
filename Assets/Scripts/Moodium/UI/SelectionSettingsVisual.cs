using System;
using System.Collections;
using Unity.PolySpatial;
using UnityEngine;
namespace Moodium.UI
{
    public sealed class SelectionSettingsVisual : MonoBehaviour
    {
        SpriteRenderer m_Panel; readonly SpriteRenderer[] m_Thumbs=new SpriteRenderer[2];
        readonly SpriteRenderer[,] m_Labels=new SpriteRenderer[2,2];
        Action<bool,bool> m_Apply; Action m_Close; bool m_English,m_HasBag,m_Closing;
        Coroutine m_Transition,m_Show; Transform m_Content;
        readonly System.Collections.Generic.List<SpriteRenderer> m_Ghosts=new();
        public void Initialize(bool english,bool hasBag,Action<bool,bool> apply,Action close)
        {
            m_English=english;m_HasBag=hasBag;m_Apply=apply;m_Close=close;
            gameObject.AddComponent<VisionOSSortingGroup>();
            m_Content=new GameObject("Settings Panel Content").transform;m_Content.SetParent(transform,false);
            m_Panel=UIRefreshVisuals.Sprite(m_Content,"Settings Panel",english?"settings_panel_en":"settings_panel_zh",Vector3.zero,20);
            var closeSprite=UIRefreshVisuals.Sprite(m_Content,"Settings Close","settings_close",MoodiumUIRefreshLayout.SettingsClose,23);
            UIRefreshVisuals.Button(closeSprite,Vector2.one*(44*MoodiumUIRefreshLayout.DesignPxToMeters),Close);
            var gear=UIRefreshVisuals.Gear(transform,new Vector3(0,-MoodiumUIRefreshLayout.SettingsPanelAboveKey,0),Close);gear.SetOpen(true);gear.ShowPressedFeedback();
            for(int row=0;row<2;row++)
            {
                var centre=row==0?MoodiumUIRefreshLayout.SettingsLanguageTrack:MoodiumUIRefreshLayout.SettingsBagTrack;
                UIRefreshVisuals.Sprite(m_Content,"Settings Track "+row,"settings_seg_track",centre,21);
                m_Thumbs[row]=UIRefreshVisuals.Sprite(m_Content,"Settings Thumb "+row,"settings_seg_thumb",MoodiumUIRefreshLayout.SettingsOption(centre,Selected(row)),22);
                for(int col=0;col<2;col++)
                {
                    int r=row,c=col;
                    var label=UIRefreshVisuals.Sprite(m_Content,"Settings Option "+row+"-"+col,LabelId(row,col),MoodiumUIRefreshLayout.SettingsOption(centre,col),23);
                    m_Labels[row,col]=label;label.color=MoodiumUIRefreshLayout.SettingsLabelColor(col==Selected(row));
                    UIRefreshVisuals.Button(label,new Vector2(.081f,.036f),()=>Choose(r,c));
                }
            }
            gameObject.AddComponent<Moodium.Flow.SensoryBagDirectTouchInput>();
            if(Application.isPlaying)m_Show=StartCoroutine(Show());
        }
        int Selected(int row)=>row==0?(m_English?1:0):(m_HasBag?0:1);
        string LabelId(int row,int col)=>row==0?(col==0?"settings_label_lang_zh":"settings_label_lang_en"):
            "settings_label_bag_"+(col==0?"has_":"none_")+(m_English?"en":"zh");
        public void Choose(int row,int option)
        {
            if(m_Closing||Selected(row)==option)return;
            var languageChanged=row==0;m_English=row==0?option==1:m_English;m_HasBag=row==1?option==0:m_HasBag;
            if(m_Show!=null){StopCoroutine(m_Show);m_Show=null;m_Content.localScale=Vector3.one;SetAlpha(1);}
            if(m_Transition!=null)StopCoroutine(m_Transition);
            foreach(var ghost in m_Ghosts)if(ghost!=null)Destroy(ghost.gameObject);m_Ghosts.Clear();
            if(Application.isPlaying)m_Transition=StartCoroutine(Change(languageChanged));else ApplyVisuals();
            m_Apply?.Invoke(m_English,m_HasBag);
        }
        void ApplyVisuals()
        {
            m_Panel.sprite=UIRefreshVisuals.Load(m_English?"settings_panel_en":"settings_panel_zh");
            for(int r=0;r<2;r++){m_Thumbs[r].transform.localPosition=MoodiumUIRefreshLayout.SettingsOption(r==0?MoodiumUIRefreshLayout.SettingsLanguageTrack:MoodiumUIRefreshLayout.SettingsBagTrack,Selected(r));for(int c=0;c<2;c++){m_Labels[r,c].sprite=UIRefreshVisuals.Load(LabelId(r,c));m_Labels[r,c].color=MoodiumUIRefreshLayout.SettingsLabelColor(c==Selected(r));}}
        }
        IEnumerator Change(bool language)
        {
            var starts=new[]{m_Thumbs[0].transform.localPosition,m_Thumbs[1].transform.localPosition};
            var colors=new Color[2,2];for(int r=0;r<2;r++)for(int c=0;c<2;c++)colors[r,c]=m_Labels[r,c].color;
            // Keep the old baked language visible while the new sprites fade in.
            var ghosts=m_Ghosts;
            if(language){foreach(var original in new[]{m_Panel,m_Labels[1,0],m_Labels[1,1]}){var ghost=UIRefreshVisuals.Sprite(m_Content,"Language Crossfade","settings_panel_zh",original.transform.localPosition,original.sortingOrder);ghost.sprite=original.sprite;ghost.color=original.color;ghosts.Add(ghost);}}
            m_Panel.sprite=UIRefreshVisuals.Load(m_English?"settings_panel_en":"settings_panel_zh");
            for(int c=0;c<2;c++)m_Labels[1,c].sprite=UIRefreshVisuals.Load(LabelId(1,c));
            float elapsed=0;
            while(elapsed<.25f)
            {
                elapsed+=Time.unscaledDeltaTime;var t=UIRefreshVisuals.Ease(elapsed/.25f);var fade=Mathf.Clamp01(elapsed/.2f);
                for(int r=0;r<2;r++) {m_Thumbs[r].transform.localPosition=Vector3.Lerp(starts[r],MoodiumUIRefreshLayout.SettingsOption(r==0?MoodiumUIRefreshLayout.SettingsLanguageTrack:MoodiumUIRefreshLayout.SettingsBagTrack,Selected(r)),t);for(int c=0;c<2;c++){var color=Color.Lerp(colors[r,c],MoodiumUIRefreshLayout.SettingsLabelColor(c==Selected(r)),fade);if(language&&r==1)color.a*=fade;m_Labels[r,c].color=color;}}
                if(language){m_Panel.color=new Color(1,1,1,fade);foreach(var ghost in ghosts){var color=ghost.color;color.a=1-fade;ghost.color=color;}}
                yield return null;
            }
            foreach(var ghost in ghosts)Destroy(ghost.gameObject);ghosts.Clear();ApplyVisuals();m_Panel.color=Color.white;m_Transition=null;
        }
        IEnumerator Show(){float e=0;while(e<.3f){e+=Time.unscaledDeltaTime;float t=UIRefreshVisuals.Ease(e/.3f);m_Content.localScale=Vector3.one*Mathf.Lerp(.92f,1,t);SetAlpha(t);yield return null;}m_Content.localScale=Vector3.one;SetAlpha(1);}
        void SetAlpha(float a){foreach(var r in m_Content.GetComponentsInChildren<SpriteRenderer>()){var c=r.color;c.a=a;if(r.name.StartsWith("Settings Option")){for(int row=0;row<2;row++)for(int col=0;col<2;col++)if(m_Labels[row,col]==r)c.a*=MoodiumUIRefreshLayout.SettingsLabelColor(col==Selected(row)).a;}r.color=c;}}
        public void Close(){if(m_Closing)return;m_Closing=true;foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;StopAllCoroutines();if(Application.isPlaying)StartCoroutine(Hide());else m_Close?.Invoke();}
        IEnumerator Hide(){float e=0;while(e<.2f){e+=Time.unscaledDeltaTime;SetAlpha(1-UIRefreshVisuals.Ease(e/.2f));yield return null;}m_Close?.Invoke();}
    }
}
