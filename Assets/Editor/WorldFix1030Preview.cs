using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Moodium.Reality;
using Moodium.Calibration;
using Moodium.Interaction;
public static class WorldFix1030Preview
{
    public static void Capture()
    {
        Directory.CreateDirectory("docs/verification/world-fix-1030/previews");
        foreach(var world in new[]{"Nature","Candy"})foreach(var p in new[]{0f,.5f,1f})
        {
            var preview=new PreviewRenderUtility();var table=new GameObject("table");var host=new GameObject("rise");var rescue=new GameObject("rescue");
            try
            {
                var presentation=table.AddComponent<CalibratedSensoryBagPresentation>();presentation.Configure(table.transform,new SensoryBagCalibrationData{Width=.4f,Height=.25f,Depth=.05f,LocalOrientation=Quaternion.identity},AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/prefab/{world}Soft.prefab"));
                var actor=Object.Instantiate(Resources.Load<GameObject>("MoodiEmergence/Moodi"),rescue.transform);
                var renderers=actor.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);actor.transform.localScale*=.04f/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
                var display=host.AddComponent<MoodiRiseDisplay>();display.Begin(world=="Nature");display.BindToSoft(table.GetComponentInChildren<ChocolateCapsuleInteraction>());
                var animator=actor.GetComponentInChildren<Animator>();display.AttachMoodi(animator);
                var emerge=AssetDatabase.LoadAllAssetsAtPath("Assets/Model/Moodi_Rigged.fbx").OfType<AnimationClip>().First(c=>c.name=="Emerge");emerge.SampleAnimation(animator.gameObject,p*emerge.length);
                typeof(MoodiRiseDisplay).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(display,null);
                display.TryGetDomeCenter(out var center,out var up);
                preview.AddSingleGO(table);preview.AddSingleGO(rescue);
                preview.camera.transform.position=center+up*.15f+Vector3.back*.24f;preview.camera.transform.LookAt(center+up*.015f,up);preview.camera.nearClipPlane=.001f;preview.camera.farClipPlane=10;preview.camera.fieldOfView=42;
                preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(40,25,0);preview.ambientColor=Color.gray;
                preview.BeginStaticPreview(new Rect(0,0,1000,750));preview.Render(true);var image=preview.EndStaticPreview();File.WriteAllBytes($"docs/verification/world-fix-1030/previews/{world}-{p*100:0}.png",image.EncodeToPNG());Object.DestroyImmediate(image);
            }finally{Object.DestroyImmediate(host);preview.Cleanup();if(table!=null)Object.DestroyImmediate(table);if(rescue!=null)Object.DestroyImmediate(rescue);}
        }
    }
}
