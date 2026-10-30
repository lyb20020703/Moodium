using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Moodium.Reality;
using Moodium.Calibration;
using Moodium.Interaction;
using Moodium.UI;
public class WorldFix1030Tests
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    [TestCase(false)][TestCase(true)]
    public void OpeningHitboxesClearVideoAndDragHandle(bool hardware)
    {
        var display=new GameObject("display");display.transform.localScale=new Vector3(1.28f,.72f,1);
        var video=display.AddComponent<BoxCollider>();video.size=new Vector3(1,1,.04f);
        var handle=new GameObject("handle");handle.transform.SetParent(display.transform,false);handle.transform.localPosition=new Vector3(0,-.54f,-.012f);var drag=handle.AddComponent<BoxCollider>();drag.size=new Vector3(.23f,.052f,.025f);
        var choice=new GameObject("choice",typeof(RectTransform));choice.transform.SetParent(display.transform,false);
        try
        {
            var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("5d946757aa5b8410aba5b97ed2216bfa"));
            if(hardware)choice.AddComponent<Moodium.Opening.OpeningVideoHardwareChoice>().Configure(font,null,false);
            else choice.AddComponent<Moodium.Opening.OpeningVideoLanguageChoice>().Configure(font,null);
            var hits=choice.GetComponentsInChildren<BoxCollider>();Assert.That(hits.Length,Is.EqualTo(2));
            foreach(var hit in hits){Assert.That(hit.bounds.Intersects(video.bounds),Is.False);Assert.That(hit.bounds.Intersects(drag.bounds),Is.False);Assert.That(hit.bounds.max.z,Is.LessThan(video.bounds.min.z));}
            Assert.That(hits[0].bounds.Intersects(hits[1].bounds),Is.False);
        }finally{Object.DestroyImmediate(display);}
    }
    [Test] public void DisabledRiseRoutesToVideo()
    {
        var host=new GameObject("router");var soft=new GameObject("soft");
        try{var router=host.AddComponent<SqueezeProgressDisplayRouter>();router.Configure(null,null,null);typeof(SqueezeProgressDisplayRouter).GetField("m_UseMoodiRise",Flags).SetValue(router,false);router.Begin(false);router.BindToSoft(soft.AddComponent<ChocolateCapsuleInteraction>());Assert.That(typeof(SqueezeProgressDisplayRouter).GetField("active",Flags).GetValue(router),Is.TypeOf<SqueezeProgressVideoController>());}
        finally{Object.DestroyImmediate(host);Object.DestroyImmediate(soft);}
    }
    [TestCase("Nature")][TestCase("Candy")]
    public void RisePreservesParentAndAlignsRigAtSurface(string world)
    {
        var anchor=new GameObject("anchor");var host=new GameObject("rise");var root=new GameObject("rescue");
        var actor=Object.Instantiate(Resources.Load<GameObject>("MoodiEmergence/Moodi"),root.transform);
        var renderers=actor.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        actor.transform.localScale*=.04f/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
        try
        {
            var presentation=anchor.AddComponent<CalibratedSensoryBagPresentation>();
            presentation.Configure(anchor.transform,new SensoryBagCalibrationData{Width=.4f,Height=.25f,Depth=.05f,LocalOrientation=Quaternion.identity},AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/prefab/{world}Soft.prefab"));
            var rise=host.AddComponent<MoodiRiseDisplay>();rise.Begin(world=="Nature");rise.BindToSoft(presentation.VisualRoot.GetComponentInChildren<ChocolateCapsuleInteraction>());
            Assert.That(rise.Ready,Is.True);var animator=actor.GetComponentInChildren<Animator>();var parent=animator.transform.parent;
            rise.AttachMoodi(animator);typeof(MoodiRiseDisplay).GetMethod("LateUpdate",Flags).Invoke(rise,null);
            var rig=actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Moodi_Rig");rise.TryGetDomeCenter(out var surface,out _);
            Assert.That(Vector3.Distance(rig.position,surface),Is.LessThan(.001f));Assert.That(animator.transform.parent,Is.SameAs(parent));
            var before=rig.position;typeof(MoodiRiseDisplay).GetField("spring",Flags).SetValue(rise,1f);typeof(MoodiRiseDisplay).GetMethod("LateUpdate",Flags).Invoke(rise,null);
            Assert.That(Vector3.Distance(rig.position,before),Is.LessThan(.0001f));
            int pops=0;rise.Popped+=_=>pops++;
            for(int i=0;i<2;i++)
            {
                rise.SetProgress(1);typeof(MoodiRiseDisplay).GetField("progress",Flags).SetValue(rise,1f);
                typeof(MoodiRiseDisplay).GetMethod("Update",Flags).Invoke(rise,null);typeof(MoodiRiseDisplay).GetMethod("Update",Flags).Invoke(rise,null);
                Assert.That(pops,Is.EqualTo(i+1));rise.ResetAndHide();Assert.That(actor.transform.localPosition,Is.EqualTo(Vector3.zero));
                rise.BindToSoft(presentation.VisualRoot.GetComponentInChildren<ChocolateCapsuleInteraction>());
            }
        }finally{Object.DestroyImmediate(host);Object.DestroyImmediate(root);Object.DestroyImmediate(anchor);}
    }
    [Test] public void ProgressNeverReversesOrCompletesInUnderThreeSeconds()
    {float p=0,v=0;for(int i=0;i<180;i++)p=MoodiRiseDisplay.Advance(p,1,ref v,.25f,.3f,1f/60);Assert.That(p,Is.LessThan(.995f));Assert.That(MoodiRiseDisplay.Advance(p,0,ref v,.25f,.3f,.016f),Is.GreaterThanOrEqualTo(p));}
    [Test] public void MissingEmergeStateIsNotSupported()
    {var go=new GameObject("empty");try{Assert.That(MoodiRiseDisplay.SupportsRise(go.AddComponent<Animator>()),Is.False);}finally{Object.DestroyImmediate(go);}}
    [Test] public void GazeUsesShaderAndOwnsTransparentTexture()
    {
        var go=new GameObject("gaze");try{var r=go.AddComponent<SpriteRenderer>();r.sprite=UIRefreshVisuals.Load("settings_btn_idle");UIRefreshVisuals.GazeHover(r,"settings_btn_gaze");Assert.That(go.GetComponent<Unity.PolySpatial.VisionOSHoverEffect>().Type,Is.EqualTo(Unity.PolySpatial.VisionOSHoverEffect.EffectType.Shader));Assert.That(r.sharedMaterial.GetTexture("_GazeTex"),Is.Not.Null);}finally{Object.DestroyImmediate(go);}
    }
}
