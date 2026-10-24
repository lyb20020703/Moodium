using System.Collections.Generic;
using System.IO;
using Moodium.Flow;
using Moodium.MoodCheck;
using TMPro;
using Unity.PolySpatial;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MoodCheckUISetup
{
    const string Root = "Assets/UI/Moodium/MoodCheck";
    const string Generated = Root + "/Generated";
    const string PrefabPath = Generated + "/MoodCheckPanel.prefab";
    const string ResourceFolder = "Assets/Resources/Moodium/MoodCheck";
    const string ResourcePrefabPath = ResourceFolder + "/MoodCheckPanel.prefab";
    const string SurfacePath = Generated + "/MoodCheckPanel_Surface.asset";
    const string BorderPath = Generated + "/MoodCheckPanel_Border.asset";
    const string ButtonPath = Generated + "/MoodCheckButton_Surface.asset";
    const string LineMaterialPath = Generated + "/MoodCheckLine_White.mat";
    const string GlassPath = "Assets/Materials/Moodium_Glass_UI.mat";
    const string EdgePath = "Assets/Materials/Moodium_Glass_UI_Edge.mat";
    const string ChineseFontPath = "Assets/UI/Moodium/Fonts/AlibabaPuHuiTi Moodium SDF.asset";
    const string EnglishFontPath = "Assets/UI/Moodium/Fonts/Inter-Regular SDF.asset";
    const float PanelTextScale = .1f;

    [MenuItem("Moodium/Build Mood Check UI")]
    public static void BuildOrUpdate()
    {
        EnsureFolder(Generated);
        EnsureFolder(ResourceFolder);
        ConfigureTextures();
        var surface = ReplaceMesh(SurfacePath, CreateRoundedRectMesh(.462f, .566f, .034f, 12));
        var border = ReplaceMesh(BorderPath, CreateRoundedBorderMesh(.462f, .566f, .034f, .0014f, 12));
        var button = ReplaceMesh(ButtonPath, CreateRoundedRectMesh(.11f, .036f, .018f, 8));
        var root = BuildPrefab(surface, border, button);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(root, ResourcePrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void ConfigureTextures()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/png" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            var name = Path.GetFileNameWithoutExtension(path);
            importer.maxTextureSize = name.StartsWith("moodi_") || name is "disc_mask" or "zone_ring_dashed" ? 512 : 256;
            importer.SaveAndReimport();
        }
    }

    static GameObject BuildPrefab(Mesh surface, Mesh border, Mesh buttonMesh)
    {
        var root = new GameObject("MoodCheckPanel");
        var glass = Child(root.transform, "PanelGlass");
        CreateMeshVisual(glass.transform, "Glass Surface", surface,
            AssetDatabase.LoadAssetAtPath<Material>(GlassPath), 0);
        CreateMeshVisual(glass.transform, "Soft Edge Highlight", border,
            AssetDatabase.LoadAssetAtPath<Material>(EdgePath), 1);

        var chinese = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
        var english = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EnglishFontPath);
        var header = Child(root.transform, "Header");
        CreateText(header.transform, "Kicker", "Moodi 想知道", new Vector3(0f, .247f, -.004f), .010f, chinese);
        CreateText(header.transform, "Title", "你现在的心情怎么样？", new Vector3(0f, .225f, -.004f), .018f, chinese);
        var readout = Child(root.transform, "Readout");
        CreateText(readout.transform, "MoodName", "一般", new Vector3(-.025f, .190f, -.004f), .022f, chinese);
        CreateText(readout.transform, "MoodDescription", "说不上好坏", new Vector3(.045f, .190f, -.004f), .011f, chinese);

        var plate = Child(root.transform, "Plate");
        plate.transform.localPosition = new Vector3(0f, .035f, -.003f);
        CreateSprite(plate.transform, "PlateWell", "disc_mask", .28f, new Color(1f, 1f, 1f, .05f), 11);
        CreateSprite(plate.transform, "PlateTintBase", "disc_mask", .28f, new Color(.84f, .86f, .91f, .10f), 12);
        CreateSprite(plate.transform, "PlateGlow", "glow_radial", .224f, new Color(.84f, .86f, .91f, .22f), 12);
        var axes = Child(plate.transform, "Axes");
        CreateLine(axes.transform, "Horizontal", new Vector2(.252f, .0007f), 13);
        CreateLine(axes.transform, "Vertical", new Vector2(.0007f, .252f), 13);
        CreateSprite(plate.transform, "NeutralZone", "zone_ring_dashed", .123f, new Color(1f, 1f, 1f, .26f), 13);
        var icons = Child(plate.transform, "QuadrantIcons");
        CreateIcon(icons.transform, "HappyIcon", "icon_mood_happy", new Vector2(.061f, .061f));
        CreateIcon(icons.transform, "CalmIcon", "icon_mood_calm", new Vector2(.061f, -.061f));
        CreateIcon(icons.transform, "LowIcon", "icon_mood_low", new Vector2(-.061f, -.061f));
        CreateIcon(icons.transform, "TenseIcon", "icon_mood_tense", new Vector2(-.061f, .061f));
        CreateLine(plate.transform, "Trail", new Vector2(.0005f, .112f), 15).SetActive(false);
        var plateCollider = Child(plate.transform, "PlateCollider");
        plateCollider.AddComponent<BoxCollider>().size = new Vector3(.28f, .28f, .012f);
        var shadow = CreateSprite(plate.transform, "TokenShadow", "glow_radial", .043f,
            new Color(24f / 255f, 14f / 255f, 48f / 255f, .34f), 18);
        shadow.transform.localScale = new Vector3(shadow.transform.localScale.x, shadow.transform.localScale.y * .23f, 1f);
        shadow.transform.localPosition = new Vector3(0f, -.027f, -.001f);
        var token = Child(plate.transform, "Token");
        token.transform.localPosition = new Vector3(0f, 0f, -.012f);
        CreateSprite(token.transform, "MoodiSprite", "moodi_neutral", .073f, Color.white, 22);
        var tokenCollider = Child(token.transform, "TokenCollider");
        tokenCollider.AddComponent<SphereCollider>().radius = .035f;
        var tokenHover = tokenCollider.AddComponent<VisionOSHoverEffect>();
        tokenHover.Type = VisionOSHoverEffect.EffectType.Highlight;
        var moodPlate = plate.AddComponent<MoodCheckPlate>();
        moodPlate.Configure(.112f);
        moodPlate.ConfigureVisuals(new[]
        {
            LoadSprite("moodi_happy"), LoadSprite("moodi_calm"), LoadSprite("moodi_neutral"),
            LoadSprite("moodi_low"), LoadSprite("moodi_tense")
        });
        var plateHandle = plate.AddComponent<MoodCheckPlateHandle>();
        plateHandle.Configure(moodPlate);

        var labels = Child(root.transform, "AxisLabels");
        CreateText(labels.transform, "Up", "精力充沛", new Vector3(0f, .184f, -.006f), .009f, chinese);
        CreateText(labels.transform, "Down", "有点累", new Vector3(0f, -.118f, -.006f), .009f, chinese);
        CreateText(labels.transform, "Left", "不太好", new Vector3(-.18f, .035f, -.006f), .009f, chinese);
        CreateText(labels.transform, "Right", "还不错", new Vector3(.18f, .035f, -.006f), .009f, chinese);

        var chips = Child(root.transform, "QuickChips");
        var moods = new[] { "Happy", "Calm", "Neutral", "Low", "Tense" };
        var words = new[] { "开心", "平静", "一般", "低落", "烦躁" };
        for (var i = 0; i < moods.Length; i++)
            CreateButton(chips.transform, moods[i] + "Chip", words[i], new Vector3((i - 2) * .082f, -.165f, -.004f),
                new Vector2(.074f, .036f), buttonMesh, chinese);

        var footer = Child(root.transform, "Footer");
        CreateText(footer.transform, "Hint", "拖动 Moodi，或直接点一个心情", new Vector3(-.065f, -.245f, -.004f), .011f, chinese);
        CreateText(footer.transform, "ConfirmTitle", "你现在感觉一般，对吗？", new Vector3(-.065f, -.245f, -.004f), .013f, chinese).SetActive(false);
        CreateText(footer.transform, "RecommendationSummary", "Moodi 推荐", new Vector3(-.065f, -.245f, -.004f), .011f, chinese).SetActive(false);
        CreateButton(footer.transform, "SkipButton", "跳过", new Vector3(.15f, -.245f, -.004f), new Vector2(.08f, .036f), buttonMesh, chinese);
        CreateButton(footer.transform, "AdjustButton", "再调调", new Vector3(.095f, -.245f, -.004f), new Vector2(.09f, .036f), buttonMesh, chinese).SetActive(false);
        CreateButton(footer.transform, "ConfirmButton", "对", new Vector3(.17f, -.245f, -.004f), new Vector2(.06f, .036f), buttonMesh, chinese).SetActive(false);
        CreateButton(footer.transform, "RetryButton", "再选一次", new Vector3(.145f, -.245f, -.004f), new Vector2(.10f, .036f), buttonMesh, chinese).SetActive(false);
        CreateButton(footer.transform, "ContinueButton", "继续", new Vector3(.15f, -.245f, -.004f), new Vector2(.08f, .036f), buttonMesh, chinese).SetActive(false);

        var recommendation = Child(root.transform, "RecommendationCard");
        var recommendationGlass = CreateMeshVisual(recommendation.transform, "Glass", surface,
            AssetDatabase.LoadAssetAtPath<Material>(GlassPath), 2);
        recommendationGlass.transform.localScale = new Vector3(.392f / .462f, .78f, 1f);
        CreateText(recommendation.transform, "Kicker", "Moodi 推荐", new Vector3(-.12f, .18f, -.006f), .010f, chinese);
        CreateText(recommendation.transform, "Badge", "现在：一般", new Vector3(.105f, .18f, -.006f), .010f, chinese);
        CreateSprite(recommendation.transform, "Portrait", "moodi_neutral", .053f, Color.white, 22)
            .transform.localPosition = new Vector3(-.12f, .11f, -.008f);
        CreateText(recommendation.transform, "WorldName", "自然世界", new Vector3(.025f, .11f, -.008f), .031f, chinese);
        CreateText(recommendation.transform, "Recommendation", "都可以哦，要不先去森林里走走？",
            new Vector3(0f, .045f, -.008f), .014f, chinese);
        CreateText(recommendation.transform, "StickerText", "找到这张贴纸，贴在袋子上",
            new Vector3(0f, -.055f, -.008f), .011f, chinese);
        CreateText(recommendation.transform, "Other", "也想去另一个？完全可以",
            new Vector3(-.08f, -.145f, -.008f), .010f, chinese);
        recommendation.SetActive(false);
        var comparison = Child(root.transform, "ComparisonCard");
        var comparisonGlass = CreateMeshVisual(comparison.transform, "Glass", surface,
            AssetDatabase.LoadAssetAtPath<Material>(GlassPath), 2);
        comparisonGlass.transform.localScale = new Vector3(.56f / .462f, .49f / .566f, 1f);
        CreateText(comparison.transform, "Kicker", "在世界待了一会儿之后",
            new Vector3(0f, .19f, -.008f), .010f, chinese);
        CreateText(comparison.transform, "MoodTransition", "进入前 → 现在",
            new Vector3(.08f, .09f, -.008f), .018f, chinese);
        CreateText(comparison.transform, "Result", "谢谢你陪自己待了一会儿",
            new Vector3(.08f, .035f, -.008f), .014f, chinese);
        CreateText(comparison.transform, "Metrics", "停留 0 分钟 · 慢慢捏了 0 次",
            new Vector3(.08f, -.035f, -.008f), .011f, chinese);
        CreateText(comparison.transform, "Privacy", "这条记录只保存在这台设备上",
            new Vector3(-.08f, -.17f, -.008f), .010f, chinese);
        comparison.SetActive(false);
        var fonts = Child(root.transform, "FontReferences");
        var fontRefs = fonts.AddComponent<MoodCheckFontReferences>();
        fontRefs.Configure(chinese, english);
        fonts.SetActive(false);
        return root;
    }

    static GameObject CreateButton(Transform parent, string name, string text, Vector3 position,
        Vector2 size, Mesh mesh, TMP_FontAsset font)
    {
        var root = Child(parent, name);
        root.transform.localPosition = position;
        var collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(size.x, Mathf.Max(.045f, size.y), .02f);
        var visual = CreateMeshVisual(root.transform, "Background", mesh,
            AssetDatabase.LoadAssetAtPath<Material>(EdgePath), 20);
        visual.transform.localScale = new Vector3(size.x / .11f, size.y / .036f, 1f);
        var label = CreateText(root.transform, "Label", text, new Vector3(0f, 0f, -.003f), .011f, font)
            .GetComponent<TextMeshPro>();
        var spatial = root.AddComponent<MoodiumSpatialButton>();
        spatial.Configure(label, visual.GetComponent<Renderer>(), null);
        var hover = visual.AddComponent<VisionOSHoverEffect>();
        hover.Type = VisionOSHoverEffect.EffectType.Highlight;
        return root;
    }

    static GameObject CreateIcon(Transform parent, string name, string spriteName, Vector2 position)
    {
        var icon = CreateSprite(parent, name, spriteName, .021f, new Color(1f, 1f, 1f, .26f), 14);
        icon.transform.localPosition = new Vector3(position.x, position.y, -.004f);
        return icon;
    }

    static GameObject CreateSprite(Transform parent, string name, string spriteName, float size, Color color, int order)
    {
        var root = Child(parent, name);
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/png/{spriteName}.png");
        renderer.color = color;
        renderer.sortingOrder = order;
        if (renderer.sprite != null)
        {
            var spriteSize = renderer.sprite.bounds.size;
            root.transform.localScale = new Vector3(size / spriteSize.x, size / spriteSize.y, 1f);
        }
        return root;
    }

    static Sprite LoadSprite(string spriteName) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/png/{spriteName}.png");

    static GameObject CreateLine(Transform parent, string name, Vector2 size, int order)
    {
        var line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        line.name = name;
        line.transform.SetParent(parent, false);
        Object.DestroyImmediate(line.GetComponent<Collider>());
        line.transform.localScale = new Vector3(size.x, size.y, 1f);
        line.transform.localPosition = new Vector3(0f, 0f, -.004f);
        var renderer = line.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GetOrCreateWhiteLineMaterial();
        renderer.sortingOrder = order;
        return line;
    }

    static GameObject CreateText(Transform parent, string name, string value, Vector3 position,
        float worldHeight, TMP_FontAsset font)
    {
        var root = Child(parent, name);
        root.transform.localPosition = position;
        var text = root.AddComponent<TextMeshPro>();
        text.font = font;
        text.text = value;
        text.fontSize = 1f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, .96f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(.4f, worldHeight * 1.5f);
        text.rectTransform.localScale = Vector3.one * PanelTextScale;
        text.renderer.sortingOrder = 30;
        return root;
    }

    static Material GetOrCreateWhiteLineMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
        if (material != null)
            return material;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        material = new Material(shader) { name = "Mood Check White Line" };
        material.SetColor("_BaseColor", new Color(1f, 1f, 1f, .42f));
        material.SetColor("_Color", new Color(1f, 1f, 1f, .42f));
        AssetDatabase.CreateAsset(material, LineMaterialPath);
        return material;
    }

    static GameObject CreateMeshVisual(Transform parent, string name, Mesh mesh, Material material, int order)
    {
        var root = Child(parent, name);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return root;
    }

    static GameObject Child(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    static Mesh ReplaceMesh(string path, Mesh replacement)
    {
        replacement.name = Path.GetFileNameWithoutExtension(path);
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(replacement, path);
            return replacement;
        }
        EditorUtility.CopySerialized(replacement, existing);
        Object.DestroyImmediate(replacement);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static Mesh CreateRoundedRectMesh(float width, float height, float radius, int segments)
    {
        var perimeter = BuildPerimeter(width, height, radius, segments);
        var vertices = new List<Vector3> { Vector3.zero };
        vertices.AddRange(perimeter);
        var triangles = new List<int>();
        for (var i = 0; i < perimeter.Count; i++)
        {
            triangles.Add(0);
            triangles.Add(1 + (i + 1) % perimeter.Count);
            triangles.Add(1 + i);
        }
        return CreateMesh(vertices, triangles);
    }

    static Mesh CreateRoundedBorderMesh(float width, float height, float radius, float thickness, int segments)
    {
        var outer = BuildPerimeter(width, height, radius, segments);
        var inner = BuildPerimeter(width - thickness * 2f, height - thickness * 2f,
            Mathf.Max(.001f, radius - thickness), segments);
        var vertices = new List<Vector3>(outer);
        vertices.AddRange(inner);
        var triangles = new List<int>();
        for (var i = 0; i < outer.Count; i++)
        {
            var next = (i + 1) % outer.Count;
            triangles.Add(i); triangles.Add(next); triangles.Add(outer.Count + i);
            triangles.Add(next); triangles.Add(outer.Count + next); triangles.Add(outer.Count + i);
        }
        return CreateMesh(vertices, triangles);
    }

    static List<Vector3> BuildPerimeter(float width, float height, float radius, int segments)
    {
        var points = new List<Vector3>(segments * 4);
        var centers = new[]
        {
            new Vector2(width * .5f - radius, height * .5f - radius),
            new Vector2(-width * .5f + radius, height * .5f - radius),
            new Vector2(-width * .5f + radius, -height * .5f + radius),
            new Vector2(width * .5f - radius, -height * .5f + radius)
        };
        var starts = new[] { 0f, 90f, 180f, 270f };
        for (var corner = 0; corner < 4; corner++)
        for (var step = 0; step < segments; step++)
        {
            var angle = (starts[corner] + step * 90f / segments) * Mathf.Deg2Rad;
            var point = centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            points.Add(new Vector3(point.x, point.y, 0f));
        }
        return points;
    }

    static Mesh CreateMesh(List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        var normals = new Vector3[vertices.Count];
        for (var i = 0; i < normals.Length; i++) normals[i] = Vector3.back;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        return mesh;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
