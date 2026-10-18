using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CatArtBaker
{
    const string AnimRoot = "Assets/Animations/Cats";

    [MenuItem("CatSmash/Bake Cats")]
    public static void Bake()
    {
        Directory.CreateDirectory(Abs(AnimRoot + "/Komur"));
        Directory.CreateDirectory(Abs("Assets/Data/Cats"));
        AssetDatabase.Refresh();

        Repeat("Assets/Textures/Zemin.png");
        Repeat("Assets/Textures/Duvar.png");

        SliceStrip("Assets/Textures/komur_theCat.png", "Komur");

        var komur = Sprites("Assets/Textures/komur_theCat.png");
        var komurController = Controller(
            AnimRoot + "/Komur/Komur.controller",
            Clip("Komur/IdleDown", Row(komur, "Komur", 0), 6f),
            Clip("Komur/IdleUp", Row(komur, "Komur", 3), 6f),
            Clip("Komur/IdleLeft", Row(komur, "Komur", 2), 6f),
            Clip("Komur/IdleRight", Row(komur, "Komur", 1), 6f),
            Clip("Komur/WalkDown", Rows(komur, "Komur", 5, 6), 10f),
            Clip("Komur/WalkUp", Rows(komur, "Komur", 11, 12), 10f),
            Clip("Komur/WalkLeft", Rows(komur, "Komur", 7, 8), 10f),
            Clip("Komur/WalkRight", Rows(komur, "Komur", 9, 10), 10f));

        var komurSkin = Skin("Assets/Data/Cats/Komur.asset", "Komur", komurController, false);
        var material = SpriteMaterial();
        DressCatPrefab(material);

        var game = Object.FindAnyObjectByType<CatGame>();
        if (game == null)
        {
            Debug.LogError("CatGame is not in the open scene.");
            return;
        }

        var gameSo = new SerializedObject(game);
        var canvas = gameSo.FindProperty("canvasRect").objectReferenceValue as RectTransform;
        var player = gameSo.FindProperty("cat").objectReferenceValue as CatPlayer;
        if (player != null) player.transform.rotation = Quaternion.identity;

        Clear("CatSelect");
        Clear("SelectCats");

        var selectRoot = new GameObject("SelectCats");
        var preview = Preview(selectRoot.transform, "KomurPreview", new Vector3(0f, 0.35f, -3f), komur["Komur_0_0"], komurController, material);

        var panel = new GameObject("CatSelect", typeof(RectTransform));
        panel.transform.SetParent(canvas, false);
        Stretch(panel.GetComponent<RectTransform>());
        var dim = panel.AddComponent<Image>();
        dim.color = new Color(0.05f, 0.03f, 0.03f, 0.45f);
        dim.raycastTarget = false;
        var select = panel.AddComponent<CatSelect>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Label(panel.transform, "Title", "Komur", 42, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(700f, 64f), font, new Color(1f, 0.93f, 0.82f));
        Label(panel.transform, "Credit", "Room graphics by Penzilla Design.", 16, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(900f, 28f), font, new Color(1f, 0.9f, 0.8f, 0.8f));

        var selectSo = new SerializedObject(select);
        selectSo.FindProperty("komur").objectReferenceValue = komurSkin;
        selectSo.FindProperty("preview").objectReferenceValue = preview;
        selectSo.FindProperty("panel").objectReferenceValue = panel;
        selectSo.FindProperty("player").objectReferenceValue = player;
        selectSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Cat select, animators, and Penzilla room art saved.");
    }

    static void DressCatPrefab(Material material)
    {
        const string path = "Assets/Prefabs/Kedi.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var visual = root.transform.Find("Visual");
        var mesh = visual.GetComponent<MeshRenderer>();
        if (mesh != null) Object.DestroyImmediate(mesh);
        var filter = visual.GetComponent<MeshFilter>();
        if (filter != null) Object.DestroyImmediate(filter);
        var sprite = visual.GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = visual.gameObject.AddComponent<SpriteRenderer>();
        sprite.sharedMaterial = material;
        if (visual.GetComponent<Animator>() == null) visual.gameObject.AddComponent<Animator>();
        if (visual.GetComponent<CatAnimatorDriver>() == null) visual.gameObject.AddComponent<CatAnimatorDriver>();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static CatAnimatorDriver Preview(Transform parent, string name, Vector3 position, Sprite sprite, RuntimeAnimatorController controller, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 4.4f;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return go.AddComponent<CatAnimatorDriver>();
    }

    static Material SpriteMaterial()
    {
        const string path = "Assets/Materials/CatSprite.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Cat/Pixel"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find("Cat/Pixel");
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Cutoff", 0.15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static CatSkin Skin(string path, string displayName, RuntimeAnimatorController controller, bool walks)
    {
        var skin = AssetDatabase.LoadAssetAtPath<CatSkin>(path);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<CatSkin>();
            AssetDatabase.CreateAsset(skin, path);
        }
        skin.displayName = displayName;
        skin.controller = controller;
        skin.previewWalks = walks;
        EditorUtility.SetDirty(skin);
        return skin;
    }

    static AnimatorController Controller(string path, params AnimationClip[] clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Facing", AnimatorControllerParameterType.Int);
        var machine = controller.layers[0].stateMachine;
        foreach (var clip in clips)
        {
            var state = machine.AddState(clip.name);
            state.motion = clip;
            if (clip.name == "IdleDown") machine.defaultState = state;
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static AnimationClip Clip(string name, Sprite[] frames, float fps)
    {
        string path = AnimRoot + "/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { frameRate = fps };
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.name = Path.GetFileName(name);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static Sprite[] Row(Dictionary<string, Sprite> map, string prefix, int row)
    {
        return Enumerable.Range(0, 4).Select(column => map[prefix + "_" + row + "_" + column]).ToArray();
    }

    static Sprite[] Rows(Dictionary<string, Sprite> map, string prefix, int first, int second)
    {
        return Row(map, prefix, first).Concat(Row(map, prefix, second)).ToArray();
    }

    static Sprite[] Line(Dictionary<string, Sprite> map, string prefix)
    {
        return Enumerable.Range(0, 4).Select(column => map[prefix + "_0_" + column]).ToArray();
    }

    static Sprite[] Hold(Dictionary<string, Sprite> map, string prefix)
    {
        return new[] { map[prefix + "_0_0"] };
    }

    static void SliceStrip(string path, string prefix)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        var sheet = new List<SpriteMetaData>();
        for (int i = 0; i < 52; i++)
        {
            int row = i / 4;
            int column = i % 4;
            sheet.Add(new SpriteMetaData
            {
                name = prefix + "_" + row + "_" + column,
                rect = new Rect(i * 32, 0, 32, 32),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }
        importer.spritesheet = sheet.ToArray();
        importer.SaveAndReimport();
    }

    static void Slice(string path, string prefix, int columns, int rows)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        var sheet = new List<SpriteMetaData>();
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                sheet.Add(new SpriteMetaData
                {
                    name = prefix + "_" + row + "_" + column,
                    rect = new Rect(column * 32, (rows - 1 - row) * 32, 32, 32),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }
        }
        importer.spritesheet = sheet.ToArray();
        importer.SaveAndReimport();
    }

    static Dictionary<string, Sprite> Sprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name);
    }

    static void Repeat(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    static Button HalfButton(Transform parent, string name, float min, float max)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(min, 0f);
        rect.anchorMax = new Vector2(max, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.02f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        return button;
    }

    static void Label(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 pos, Vector2 box, Font font, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = box;
        var label = go.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Clear(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static string Abs(string assetPath)
    {
        return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
    }
}
