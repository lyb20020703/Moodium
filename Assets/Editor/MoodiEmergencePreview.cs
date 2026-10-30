using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Moodium.Reality;
using Moodium.Interaction;
public static class MoodiEmergencePreview
{
    public static void Capture()
    {
        Directory.CreateDirectory("docs/verification/moodi-emergence");
        foreach(var world in new[]{"Nature","Candy"})foreach(var p in new[]{0f,.5f,1f})
        {
            var preview=new PreviewRenderUtility();
            var soft=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/prefab/{world}Soft.prefab"));
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefab/Moodi meshy.prefab"));
            var host=new GameObject("preview display");
            try
            {
                var cap=soft.AddComponent<ChocolateCapsuleInteraction>();var display=host.AddComponent<MoodiEmergenceDisplay>();
                display.BindToSoft(cap);var animator=actor.GetComponentInChildren<Animator>(true);display.AttachMoodi(animator);
                var mouth=MoodiEmergenceDisplay.FindMouth(soft.transform);
                var mouthAnimator=mouth.GetComponentInParent<Animator>();
                var clip=AssetDatabase.LoadAllAssetsAtPath($"Assets/Model/Soft/{world}Soft_Mouth.fbx").OfType<AnimationClip>().First(c=>c.name=="Mouth_Emerge");
                clip.SampleAnimation(mouthAnimator.gameObject,p*clip.length);
                var emerge=AssetDatabase.LoadAllAssetsAtPath("Assets/Model/Moodi_Rigged.fbx").OfType<AnimationClip>().First(c=>c.name=="Emerge");
                emerge.SampleAnimation(animator.gameObject,p*emerge.length);
                if(p==1f)
                {
                    var pop=AssetDatabase.LoadAllAssetsAtPath($"Assets/Model/Soft/{world}Soft_Mouth.fbx").OfType<AnimationClip>().First(c=>c.name=="Mouth_Pop");
                    pop.SampleAnimation(mouthAnimator.gameObject,.12f);
                    animator.transform.position+=mouth.forward*.03f;
                }
                preview.AddSingleGO(soft);preview.AddSingleGO(actor);
                var renderers=soft.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                var distance=Mathf.Max(bounds.size.x,bounds.size.z)*.45f;
                preview.camera.transform.position=mouth.position+mouth.forward*distance*.8f-mouth.up*distance;
                preview.camera.transform.LookAt(mouth.position+mouth.forward*distance*.1f,mouth.forward);preview.camera.nearClipPlane=.001f;preview.camera.farClipPlane=1000;preview.camera.fieldOfView=45;
                preview.lights[0].intensity=1;preview.lights[0].transform.rotation=Quaternion.Euler(45,45,0);preview.ambientColor=Color.gray;
                preview.BeginStaticPreview(new Rect(0,0,900,700));preview.Render(true);var image=preview.EndStaticPreview();
                File.WriteAllBytes($"docs/verification/moodi-emergence/{world}-{p*100:0}.png",image.EncodeToPNG());Object.DestroyImmediate(image);
                Debug.Log($"[Moodi Emergence] preview {world} {p}: mouth {mouth.position}, scale {mouth.lossyScale}, bounds {bounds}");
            }finally{Object.DestroyImmediate(host);preview.Cleanup();if(soft!=null)Object.DestroyImmediate(soft);if(actor!=null)Object.DestroyImmediate(actor);}
        }
    }
}
