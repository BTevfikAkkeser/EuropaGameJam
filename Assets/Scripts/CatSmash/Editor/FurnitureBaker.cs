using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FurnitureBaker
{
    const string Sheet = "Assets/Textures/furnuture.png";
    const string MaterialPath = "Assets/Materials/CatSprite.mat";

    [MenuItem("CatSmash/Bake Breakable Furniture")]
    public static void Bake()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Sheet);
        importer.spritePixelsPerUnit = 32;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        var sheet = importer.spritesheet;
        for (int i = 0; i < sheet.Length; i++)
        {
            sheet[i].alignment = (int)SpriteAlignment.BottomCenter;
            sheet[i].pivot = new Vector2(0.5f, 0f);
        }
        importer.spritesheet = sheet;
        importer.SaveAndReimport();

        var sprites = new System.Collections.Generic.Dictionary<string, Sprite>();
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(Sheet))
        {
            if (sprite is Sprite s) sprites[s.name] = s;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var scrap = Definition("Scrap", "Scrap", 0, 8, 1, 0.22f, new Color(0.72f, 0.42f, 0.18f), new Color(0.95f, 0.9f, 0.78f), new Color(0.35f, 0.16f, 0.08f));
        var chip = Chip(scrap, sprites["ChairChip"], material);
        if (chip == null) return;

        Place("Loveseat", Piece("BreakCouch", "Loveseat", 2, 55, 3.2f, 1.85f, material, chip,
            Sprites(sprites, "Couch", "CouchBreak1", "CouchBreak2", "CouchBreak3", "CouchBreak4"),
            Sprites(sprites, "CouchArm", "CushionBit", "CouchScrap", "Ottoman")), new Vector2(3.15f, -2.05f));
        Place("Armchair", Piece("BreakArmchair", "Armchair", 1, 26, 1.7f, 1.7f, material, chip,
            Sprites(sprites, "Armchair", "ArmchairBreak1", "ArmchairBreak2"),
            Sprites(sprites, "ArmchairChip", "ArmchairChip2")), new Vector2(-3.35f, -0.35f));
        Place("Seat", Piece("BreakChair", "Seat", 1, 26, 1.45f, 1.65f, material, chip,
            Sprites(sprites, "Chair", "ChairBreak1", "ChairBreak2", "ChairBreak3", "ChairBreak4"),
            Sprites(sprites, "ChairChip", "ChairChip2", "CushionBit")), new Vector2(4.35f, 2.15f));
        Place("Side Chair", Piece("BreakChairSide", "Side Chair", 1, 26, 1.35f, 1.7f, material, chip,
            Sprites(sprites, "ChairSide", "ChairSideBreak1", "ChairSideBreak2", "ChairSideBreak3", "ChairSideBreak4"),
            Sprites(sprites, "ChairChip", "ChairChip2", "CouchArm")), new Vector2(-1.7f, -1.35f));

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Breakable couches saved in the room.");
    }

    static void Place(string objectName, GameObject prefab, Vector2 position)
    {
        if (GameObject.Find(objectName) != null) return;
        var room = GameObject.Find("Room 1");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, room != null ? room.transform : null);
        go.name = objectName;
        go.transform.position = new Vector3(position.x, position.y, 0f);
    }

    static GameObject Piece(string file, string display, int tier, int xp, float mass, float scale, Material material, BreakableItem chip, Sprite[] stages, Sprite[] chips)
    {
        string path = "Assets/Prefabs/" + file + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var definition = Definition(file, display, tier, xp, stages.Length, mass, new Color(0.62f, 0.33f, 0.12f), new Color(0.9f, 0.82f, 0.62f), new Color(0.28f, 0.12f, 0.06f));
        var root = new GameObject(display);
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = root.AddComponent<BoxCollider2D>();
        var item = root.AddComponent<BreakableItem>();
        var furniture = root.AddComponent<FurnitureBreak>();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = stages[0];
        renderer.sharedMaterial = material;
        visual.AddComponent<DepthSort>();

        float width = stages[0].rect.width / stages[0].pixelsPerUnit;
        float height = stages[0].rect.height / stages[0].pixelsPerUnit;
        box.size = new Vector2(width * 0.82f, height * 0.78f);
        box.offset = new Vector2(0f, height * 0.4f);
        root.transform.localScale = new Vector3(scale, scale, 1f);

        var itemObject = new SerializedObject(item);
        itemObject.FindProperty("definition").objectReferenceValue = definition;
        itemObject.ApplyModifiedPropertiesWithoutUndo();
        var furnitureObject = new SerializedObject(furniture);
        furnitureObject.FindProperty("stages").arraySize = stages.Length;
        for (int i = 0; i < stages.Length; i++)
            furnitureObject.FindProperty("stages").GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        furnitureObject.FindProperty("chips").arraySize = chips.Length;
        for (int i = 0; i < chips.Length; i++)
            furnitureObject.FindProperty("chips").GetArrayElementAtIndex(i).objectReferenceValue = chips[i];
        furnitureObject.FindProperty("chipPrefab").objectReferenceValue = chip;
        furnitureObject.FindProperty("view").objectReferenceValue = renderer;
        furnitureObject.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static BreakableItem Chip(BreakableDefinition definition, Sprite sprite, Material material)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BreakChip.prefab");
        if (existing != null) return existing.GetComponent<BreakableItem>();

        var root = new GameObject("Scrap");
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var circle = root.AddComponent<CircleCollider2D>();
        circle.radius = 0.22f;
        var item = root.AddComponent<BreakableItem>();
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        visual.AddComponent<DepthSort>();
        root.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

        var itemObject = new SerializedObject(item);
        itemObject.FindProperty("definition").objectReferenceValue = definition;
        itemObject.ApplyModifiedPropertiesWithoutUndo();

        string path = "Assets/Prefabs/BreakChip.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<BreakableItem>();
    }

    static BreakableDefinition Definition(string file, string display, int tier, int xp, int hp, float mass, Color a, Color b, Color c)
    {
        string path = "Assets/Data/" + file + ".asset";
        var definition = AssetDatabase.LoadAssetAtPath<BreakableDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BreakableDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }
        definition.displayName = display;
        definition.tier = tier;
        definition.xp = xp;
        definition.hp = hp;
        definition.mass = mass;
        definition.debrisA = a;
        definition.debrisB = b;
        definition.debrisC = c;
        EditorUtility.SetDirty(definition);
        return definition;
    }

    static Sprite[] Sprites(System.Collections.Generic.Dictionary<string, Sprite> sprites, params string[] names)
    {
        var result = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            if (!sprites.TryGetValue(names[i], out result[i]))
                Debug.LogError("Missing furniture sprite " + names[i]);
        }
        return result;
    }
}
