using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the floating world dock sprites in Assets/Resources/MoodiumWorldDock/.
/// Buttons and toasts are exported at 3x with 26 design px = 1 cm, so 780 px = 0.10 m: PPU = 7800.
/// A button sprite (1140 x 648 px) is 0.146 x 0.083 m; the glass capsule inside is 0.100 x 0.037 m.
/// dock_progress.png (300 x 60 px) is the 9-sliced energy line: 60 px = 2.2 mm, border = half its height.
/// Re-import the folder after copying the PNGs in.
/// </summary>
public sealed class WorldDockAssetPostprocessor : AssetPostprocessor
{
    const string Folder = "Assets/Resources/MoodiumWorldDock/";
    public const float PixelsPerUnit = 780f / .10f;          // 7800
    public const float ProgressPixelsPerUnit = 60f / .0022f; // 27272.7

    void OnPreprocessTexture()
    {
        var path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(Folder))
            return;
        var progress = path.EndsWith("/dock_progress.png");
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = progress ? ProgressPixelsPerUnit : PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        importer.spriteBorder = progress ? new Vector4(30, 30, 30, 30) : Vector4.zero;
    }
}
