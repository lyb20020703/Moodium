using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class MoodiEmergenceSetup
{
    const string Output="Assets/Animations/Moodi/";
    public static void PreserveOriginalClips()
    {
        const string source="Assets/Editor/MoodiOriginalVerification.fbx";
        if(!File.Exists(source))return;
        Directory.CreateDirectory(Output+"OriginalClips");
        foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>())
        {
            var name=MoodiAnimatedSetup.ShortClipName(clip.name);
            if(!new[]{"Idle","Walk","Touched","Happy","Wave"}.Contains(name))continue;
            var path=Output+"OriginalClips/"+name+".anim";
            if(AssetDatabase.LoadAssetAtPath<AnimationClip>(path)!=null)continue;
            var copy=UnityEngine.Object.Instantiate(clip);copy.name=name;
            var settings=AnimationUtility.GetAnimationClipSettings(copy);settings.loopTime=name=="Idle"||name=="Walk";
            AnimationUtility.SetAnimationClipSettings(copy,settings);AssetDatabase.CreateAsset(copy,path);
        }
        AssetDatabase.SaveAssets();
    }
    public static void UseOriginalClips(Dictionary<string,AnimationClip> clips)
    {
        foreach(var name in new[]{"Idle","Walk","Touched","Happy","Wave"})
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output+"OriginalClips/"+name+".anim");
            if(clip!=null)clips[name]=clip;
        }
    }
    public static void AugmentMoodi(AnimatorController c,Dictionary<string,AnimationClip> clips)
    {
        if(!clips.ContainsKey("Emerge"))return;
        Param(c,"EmergeProgress",AnimatorControllerParameterType.Float);Param(c,"Press",AnimatorControllerParameterType.Float);
        Param(c,"Emerging",AnimatorControllerParameterType.Bool);Param(c,"Peeking",AnimatorControllerParameterType.Bool);
        Param(c,"FallIn",AnimatorControllerParameterType.Trigger);
        var sm=c.layers[0].stateMachine;
        var emerge=State(sm,"Emerge",clips["Emerge"],"EmergeProgress");
        var peek=State(sm,"Peek",clips["Peek"]);
        var fall=State(sm,"FallIn",clips["FallIn"]);
        // Exported clips report 24 fps, while the supplied animation is authored at 30.
        fall.speed=clips["FallIn"].length/.5f;
        peek.speed=clips["Peek"].length/1.2f;
        foreach(var pair in new[]{(emerge,"Emerging"),(peek,"Peeking"),(fall,"FallIn")})
        {var t=sm.AddAnyStateTransition(pair.Item1);t.hasExitTime=false;t.duration=.06f;t.canTransitionToSelf=false;t.AddCondition(AnimatorConditionMode.If,0,pair.Item2);}
        var idle=sm.states.First(s=>s.state.name=="Idle").state;
        foreach(var s in new[]{emerge,peek}){var t=s.AddTransition(idle);t.hasExitTime=false;t.duration=.1f;t.AddCondition(AnimatorConditionMode.IfNot,0,s==emerge?"Emerging":"Peeking");}
        var back=fall.AddTransition(peek);back.hasExitTime=true;back.exitTime=1;back.duration=0;
        AddLayer(c,"Strain",clips["Strain"],"Press");
    }
    static void Param(AnimatorController c,string name,AnimatorControllerParameterType type)
    {if(!c.parameters.Any(p=>p.name==name))c.AddParameter(name,type);}
    static AnimatorState State(AnimatorStateMachine sm,string name,AnimationClip clip,string time=null)
    {
        var s=sm.states.FirstOrDefault(x=>x.state.name==name).state??sm.AddState(name);
        s.transitions=Array.Empty<AnimatorStateTransition>();s.motion=clip;
        s.timeParameterActive=time!=null;if(time!=null){s.timeParameter=time;s.speed=0;}
        EditorUtility.SetDirty(s);return s;
    }
    static void AddLayer(AnimatorController c,string name,AnimationClip clip,string time)
    {
        var layers=c.layers.ToList();var layer=layers.FirstOrDefault(l=>l.name==name);
        if(layer==null){var sm=new AnimatorStateMachine{name=name};AssetDatabase.AddObjectToAsset(sm,c);layer=new AnimatorControllerLayer{name=name,stateMachine=sm};layers.Add(layer);}
        layer.blendingMode=AnimatorLayerBlendingMode.Additive;layer.defaultWeight=1;
        layer.stateMachine.defaultState=State(layer.stateMachine,name,clip,time);c.layers=layers.ToArray();
    }
    [MenuItem("Moodium/Moodi/Setup Squeeze Out")]
    public static void Setup()
    {
        PreserveOriginalClips();
        MoodiAnimatedSetup.SetupAll();
        Directory.CreateDirectory("Assets/Resources/MoodiEmergence");
        var fallback=PrefabUtility.LoadPrefabContents("Assets/prefab/Moodi meshy.prefab");
        try{PrefabUtility.SaveAsPrefabAsset(fallback,"Assets/Resources/MoodiEmergence/Moodi.prefab");}finally{PrefabUtility.UnloadPrefabContents(fallback);}
        foreach(var world in new[]{"Nature","Candy"})
        {
            var particlePath=$"Assets/Resources/MoodiEmergence/{world}Particles.mat";
            var particleMaterial=AssetDatabase.LoadAssetAtPath<Material>(particlePath);
            if(particleMaterial==null){particleMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(particleMaterial,particlePath);}
            particleMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(world=="Nature"?"Assets/Moodium/image/leaf.png":"Assets/Epic Toon FX/Textures/sparkle3.png"));
            Moodium.Opening.OpeningVideoSurfaceSettings.TransparentAlpha.ApplyTo(particleMaterial);EditorUtility.SetDirty(particleMaterial);
            var path=$"Assets/Model/Soft/{world}Soft_Mouth.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects=false;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
            importer.SaveAndReimport();
            var defs=importer.defaultClipAnimations;
            foreach(var clip in defs){clip.name=MoodiAnimatedSetup.ShortClipName(clip.takeName);clip.loopTime=false;if(clip.name=="Mouth_Pucker"){clip.hasAdditiveReferencePose=true;clip.additiveReferencePoseFrame=0;}}
            importer.clipAnimations=defs;importer.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__preview__")).ToDictionary(x=>x.name);
            var cp=Output+world+"MouthEmergence.controller";
            var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp)??AnimatorController.CreateAnimatorControllerAtPath(cp);
            Param(c,"Progress",AnimatorControllerParameterType.Float);Param(c,"Press",AnimatorControllerParameterType.Float);Param(c,"Pop",AnimatorControllerParameterType.Trigger);
            var sm=c.layers[0].stateMachine;
            sm.defaultState=State(sm,"Emerge",clips["Mouth_Emerge"],"Progress");
            var pop=State(sm,"Pop",clips["Mouth_Pop"]);
            pop.speed=clips["Mouth_Pop"].length/.8f;
            foreach(var t in sm.anyStateTransitions)sm.RemoveAnyStateTransition(t);
            var transition=sm.AddAnyStateTransition(pop);transition.hasExitTime=false;transition.duration=0;transition.AddCondition(AnimatorConditionMode.If,0,"Pop");
            AddLayer(c,"Pucker",clips["Mouth_Pucker"],"Press");EditorUtility.SetDirty(c);
            var prefabPath=$"Assets/prefab/{world}Soft.prefab";var root=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var old=root.transform.Cast<Transform>().FirstOrDefault(t=>t.GetComponentInChildren<Renderer>(true)!=null);
                if(old==null)throw new InvalidOperationException("Missing soft model");
                var materials=(old.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name.Contains("_Static"))??old.GetComponentInChildren<Renderer>(true)).sharedMaterials;
                var pos=old.localPosition;var rot=old.localRotation;var scale=old.localScale;
                // The new export includes Blender's Z-up to Unity Y-up conversion.
                // Remove the old mesh-only import correction exactly once.
                if(Quaternion.Angle(rot,Quaternion.Euler(-90,0,0))<.01f)rot=Quaternion.identity;
                UnityEngine.Object.DestroyImmediate(old.gameObject);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                model.transform.localPosition=pos;model.transform.localRotation=rot;model.transform.localScale=scale;
                model.name=world+"Soft_Mouth";
                var animator=model.GetComponent<Animator>()??model.AddComponent<Animator>();animator.runtimeAnimatorController=c;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    if(!r.name.Contains("Collar"))r.sharedMaterials=materials;
                    else
                    {
                        var shader=AssetDatabase.LoadAssetAtPath<Shader>($"Assets/Shaders/{world}MouthCollar.shadergraph");
                        if(shader==null)throw new InvalidOperationException("Missing collar graph");
                        var mp=Output+world+"MouthCollar.mat";
                        var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
                        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,mp);}else mat.shader=shader;
                        r.sharedMaterial=mat;
                    }
                    if(r is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
                }
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
}
