using UnityEngine;
using UnityEngine.Rendering;

namespace Moodium.NatureWorld
{
    /// <summary>Each world owns independent motion and fade parameters.</summary>
    public static class BubbleSplatMaterial
    {
        public const string ResourcePath = "MoodiumBubbleWorld/BubbleSplatMotion";
        public const string ShaderName = "Moodium/BubbleSplatMotion";

        public static Material Create(Texture colors, float eyeHeight, bool candy)
        {
            var source = Resources.Load<Material>(ResourcePath);
            Material material;
            if (source != null && source.shader != null && source.shader.name == ShaderName)
            {
                material = Object.Instantiate(source);
                material.name = "Bubble World Points (GPU Motion)";
                material.SetTexture("_BaseMap", colors);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_EyeHeight", eyeHeight);
                material.SetFloat("_Style", candy ? 1f : 0f);
                material.SetFloat("_WindStrength", 0f);
                material.SetVector("_HandAmount", Vector4.zero);
                material.renderQueue = 3000;
            }
            else
            {
                Debug.LogWarning("[Bubble Motion] Resources material missing or invalid; falling back to URP/Unlit.");
                material = BubbleWorldData.CreateUnlit("Bubble World Points", colors, true, CullMode.Off, Color.white);
                BubbleWorldData.ConfigureRoundPoints(material);
                BubbleWorldController.ConfigureTransparentPoints(material);
            }
            Debug.Log("[Bubble Motion] point shader = " + material.shader.name);
            return material;
        }
    }
}
