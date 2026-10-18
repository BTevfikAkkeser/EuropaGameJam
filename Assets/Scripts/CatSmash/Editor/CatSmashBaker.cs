using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class CatSmashBaker
{
    const string Root = "Assets/Game/CatSmash";

    [MenuItem("CatSmash/Bake Float Popup")]
    public static void BakeFloatPopup()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("FloatPopup", typeof(RectTransform));
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.86f, 0.35f);
        text.text = "+12";
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.88f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 48f);
        var popup = go.AddComponent<FloatPopup>();
        var popupSo = new SerializedObject(popup);
        popupSo.FindProperty("label").objectReferenceValue = text;
        popupSo.ApplyModifiedPropertiesWithoutUndo();

        const string path = "Assets/Prefabs/FloatPopup.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        var game = Object.FindAnyObjectByType<CatGame>();
        if (game != null)
        {
            var gameSo = new SerializedObject(game);
            gameSo.FindProperty("floatPrefab").objectReferenceValue = prefab.GetComponent<FloatPopup>();
            gameSo.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Float popup prefab saved and wired to CatGame.");
    }

    [MenuItem("CatSmash/Bake Scene")]
    public static void Bake()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity")
            scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        Ensure(Root);
        Ensure(Root + "/Textures");
        Ensure(Root + "/Materials");
        Ensure(Root + "/Prefabs");
        Ensure(Root + "/Data");
        Ensure(Root + "/Physics");

        var bounce = Phys("Bounce", 0.32f, 0.42f);
        var grip = Phys("Grip", 0f, 0.15f);
        var balance = Balance();
        var catalog = new List<BreakableDefinition>();

        var yarn = Breakable("Yumak", PixelKit.Yarn(), 0.5f, 0, true, catalog);
        var mug = Breakable("Kupa", PixelKit.Mug(), 0.52f, 0, true, catalog);
        var book = Breakable("Kitap", PixelKit.Book(), 0.62f, 0, false, catalog);
        var shoe = Breakable("Ayakkabi", PixelKit.Shoe(), 0.72f, 0, false, catalog);
        var remote = Breakable("Kumanda", PixelKit.Remote(), 0.4f, 0, false, catalog);
        var bowl = Breakable("Kase", PixelKit.Bowl(), 0.52f, 0, true, catalog);
        var plant = Breakable("Saksi", PixelKit.Plant(), 0.88f, 0, false, catalog);
        var vase = Breakable("Vazo", PixelKit.Vase(), 0.62f, 0, false, catalog);
        var lamp = Breakable("Lamba", PixelKit.Lamp(), 0.8f, 1, false, catalog);
        var chair = Breakable("Sandalye", PixelKit.Chair(), 1.05f, 1, false, catalog);
        var speaker = Breakable("Hoparlor", PixelKit.Speaker(), 0.7f, 1, false, catalog);
        var crate = Breakable("Kasa", PixelKit.Crate(), 0.95f, 1, false, catalog);
        var monitor = Breakable("Monitor", PixelKit.Monitor(), 1.12f, 1, false, catalog);
        var trash = Breakable("Cop", PixelKit.Trash(), 0.72f, 1, true, catalog);
        var shelf = Breakable("Kitaplik", PixelKit.Shelf(), 1.5f, 2, false, catalog);
        var tv = Breakable("Televizyon", PixelKit.Tv(), 1.75f, 2, false, catalog);
        var table = Breakable("Masa", PixelKit.Table(), 1.9f, 2, false, catalog);
        var desk = Breakable("CalismaMasasi", PixelKit.Desk(), 1.95f, 2, false, catalog);
        var fridge = Breakable("Buzdolabi", PixelKit.Fridge(), 1.32f, 3, false, catalog);
        var wardrobe = Breakable("Gardrop", PixelKit.Wardrobe(), 1.48f, 3, false, catalog);

        var couch = Block("Koltuk", PixelKit.Couch(), 3.35f, 0f, grip, new Vector2(0f, 0.16f), new Vector2(0.9f, 0.42f));
        var side = Block("Sehpa", PixelKit.SideTable(), 0.95f, 0f, grip, Vector2.zero, new Vector2(0.8f, 0.75f));
        var rug = Decor("Hali", PixelKit.Rug(), 3.8f, 0.4f);
        var picture = Decor("Tablo", PixelKit.Picture(), 0.9f, 0f);
        var hand = Decor("El", PixelKit.Hand(), 0.42f, -0.12f);
        var cat = CatPrefab();
        var human = HumanPrefab();
        var slash = Fx("Pati", PixelKit.Slash(), typeof(UnscaledLife), false);
        var spark = Fx("Kivilcim", PixelKit.Spark(), typeof(SparkBit), false);
        var debris = Fx("Parca", WhiteStamp(), typeof(DebrisBit), true, bounce);

        var list = AssetDatabase.LoadAssetAtPath<BreakableCatalog>(Root + "/Data/BreakableCatalog.asset");
        if (list == null)
        {
            list = ScriptableObject.CreateInstance<BreakableCatalog>();
            AssetDatabase.CreateAsset(list, Root + "/Data/BreakableCatalog.asset");
        }
        list.items = catalog.ToArray();
        EditorUtility.SetDirty(list);

        Clear("Room 1");
        Clear("HUD");
        Clear("Managers");

        var room = new GameObject("Room 1");
        float halfW = 9.2f;
        float halfH = 6.15f;
        float thick = 0.62f;
        var floorMat = TexMat("Zemin", PixelKit.Floor(), true, new Vector2(12f, 8f));
        var wallMat = TexMat("Duvar", PixelKit.Wall(), true, new Vector2(8f, 2f));
        SceneQuad("Floor", floorMat, Vector2.zero, new Vector2(halfW * 2f - 0.2f, halfH * 2f - 0.2f), 3f, room.transform);
        Wall("WallNorth", wallMat, new Vector2(0f, halfH), new Vector2(halfW * 2f + thick, thick), bounce, room.transform);
        Wall("WallSouth", wallMat, new Vector2(0f, -halfH), new Vector2(halfW * 2f + thick, thick), bounce, room.transform);
        Wall("WallEast", wallMat, new Vector2(halfW, 0f), new Vector2(thick, halfH * 2f + thick), bounce, room.transform);
        Wall("WallWest", wallMat, new Vector2(-halfW, 0f), new Vector2(thick, halfH * 2f + thick), bounce, room.transform);

        Spawn(rug, "Rug", new Vector2(0.15f, 1.15f), room.transform);
        Spawn(picture, "Picture", new Vector2(-0.05f, 5.45f), room.transform);
        Spawn(yarn, "Yarn", new Vector2(0.05f, -1.02f), room.transform);
        Spawn(mug, "Mug", new Vector2(-2.15f, -0.65f), room.transform);
        Spawn(book, "Book", new Vector2(2.05f, -0.4f), room.transform);
        Spawn(shoe, "Shoe", new Vector2(-3.45f, 0.2f), room.transform);
        Spawn(remote, "Remote", new Vector2(1.2f, 0.42f), room.transform);
        Spawn(bowl, "Bowl", new Vector2(2.65f, 2.15f), room.transform);
        Spawn(plant, "Plant", new Vector2(5.15f, 1.75f), room.transform);
        Spawn(vase, "Vase", new Vector2(-5.05f, 4.25f), room.transform);
        Spawn(lamp, "Lamp", new Vector2(-6.85f, 0.75f), room.transform);
        Spawn(chair, "Chair", new Vector2(-4.25f, 2.05f), room.transform);
        Spawn(chair, "Chair", new Vector2(-2.15f, 3.4f), room.transform);
        Spawn(speaker, "Speaker", new Vector2(-2.35f, 4.8f), room.transform);
        Spawn(speaker, "Speaker", new Vector2(2.65f, 4.8f), room.transform);
        Spawn(crate, "Crate", new Vector2(3.75f, 3.2f), room.transform);
        Spawn(monitor, "Monitor", new Vector2(5.55f, -0.45f), room.transform);
        Spawn(trash, "Trash", new Vector2(7.25f, 0.05f), room.transform);
        Spawn(shelf, "Bookshelf", new Vector2(-6.65f, 3.35f), room.transform);
        Spawn(tv, "Television", new Vector2(0.25f, 4.2f), room.transform);
        Spawn(table, "Table", new Vector2(0.15f, 1.35f), room.transform);
        Spawn(desk, "Desk", new Vector2(5.45f, -1.9f), room.transform);
        Spawn(fridge, "Fridge", new Vector2(6.75f, 3.15f), room.transform);
        Spawn(wardrobe, "Wardrobe", new Vector2(-6.55f, -2.35f), room.transform);
        Spawn(couch, "Couch", new Vector2(0.1f, -4.22f), room.transform);
        Spawn(side, "SideTable", new Vector2(3.2f, -4.05f), room.transform);
        var humanGo = Spawn(human, "Person", new Vector2(0.28f, -4.28f), room.transform);
        var handGo = Spawn(hand, "Hand", new Vector2(0.66f, -4.2f), room.transform);
        humanGo.GetComponent<CouchHuman>().SetHand(handGo.transform);
        var catGo = Spawn(cat, "Cat", new Vector2(0.02f, -2.32f), room.transform);

        var hud = BuildHud(out var refs);
        var managers = new GameObject("Managers");
        var gameGo = new GameObject("CatGame");
        gameGo.transform.SetParent(managers.transform, false);
        var game = gameGo.AddComponent<CatGame>();
        var sfx = gameGo.AddComponent<AudioSource>();
        var purrSource = gameGo.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        purrSource.playOnAwake = false;
        sfx.spatialBlend = 0f;
        purrSource.spatialBlend = 0f;

        var so = new SerializedObject(game);
        so.FindProperty("balance").objectReferenceValue = balance;
        so.FindProperty("catalog").objectReferenceValue = list;
        so.FindProperty("cam").objectReferenceValue = Camera.main;
        so.FindProperty("cat").objectReferenceValue = catGo.GetComponent<CatPlayer>();
        so.FindProperty("human").objectReferenceValue = humanGo.GetComponent<CouchHuman>();
        so.FindProperty("hitClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hit.wav");
        so.FindProperty("breakClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Break.wav");
        so.FindProperty("swishClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Swish.wav");
        so.FindProperty("thunkClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Thunk.wav");
        so.FindProperty("levelClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level.wav");
        so.FindProperty("meowClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Meow.wav");
        so.FindProperty("purrClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Purr.wav");
        so.FindProperty("slashPrefab").objectReferenceValue = slash;
        so.FindProperty("sparkPrefab").objectReferenceValue = spark;
        so.FindProperty("debrisPrefab").objectReferenceValue = debris;
        so.FindProperty("levelText").objectReferenceValue = refs.level;
        so.FindProperty("objectiveText").objectReferenceValue = refs.objective;
        so.FindProperty("timerText").objectReferenceValue = refs.timer;
        so.FindProperty("leftText").objectReferenceValue = refs.left;
        so.FindProperty("rageText").objectReferenceValue = refs.rage;
        so.FindProperty("promptText").objectReferenceValue = refs.prompt;
        so.FindProperty("hintText").objectReferenceValue = refs.hint;
        so.FindProperty("endTitle").objectReferenceValue = refs.endTitle;
        so.FindProperty("endBody").objectReferenceValue = refs.endBody;
        so.FindProperty("endHint").objectReferenceValue = refs.endHint;
        so.FindProperty("rageFill").objectReferenceValue = refs.rageFill;
        so.FindProperty("xpFill").objectReferenceValue = refs.xpFill;
        so.FindProperty("flash").objectReferenceValue = refs.flash;
        so.FindProperty("endPanel").objectReferenceValue = refs.endPanel;
        so.FindProperty("canvasRect").objectReferenceValue = hud.GetComponent<RectTransform>();
        so.ApplyModifiedPropertiesWithoutUndo();

        TuneCamera();
        var volume = GameObject.Find("Global Volume");
        if (volume != null) volume.SetActive(false);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Cat Smash scene, prefabs, and ScriptableObject data saved.");
    }

    struct HudRefs
    {
        public Text level, objective, timer, left, rage, prompt, hint, endTitle, endBody, endHint;
        public Image rageFill, xpFill, flash;
        public GameObject endPanel;
    }

    static GameObject BuildHud(out HudRefs refs)
    {
        var sprite = UiSprite();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasGo = new GameObject("HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<UnityEngine.EventSystems.EventSystem>();
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        refs = new HudRefs
        {
            level = Label(canvasGo.transform, "Level", font, sprite, 42, TextAnchor.UpperLeft, new Color(1f, 0.93f, 0.78f),
                new Vector2(0f, 1f), new Vector2(36f, -78f), new Vector2(760f, -18f)),
            objective = Label(canvasGo.transform, "Goal", font, sprite, 26, TextAnchor.UpperLeft, new Color(1f, 0.82f, 0.62f),
                new Vector2(0f, 1f), new Vector2(40f, -148f), new Vector2(820f, -82f)),
            timer = Label(canvasGo.transform, "Timer", font, sprite, 48, TextAnchor.UpperCenter, Color.white,
                new Vector2(0.5f, 1f), new Vector2(-220f, -78f), new Vector2(220f, -16f)),
            left = Label(canvasGo.transform, "Left", font, sprite, 28, TextAnchor.UpperRight, new Color(0.95f, 0.9f, 0.82f),
                new Vector2(1f, 1f), new Vector2(-420f, -72f), new Vector2(-36f, -24f)),
            rage = Label(canvasGo.transform, "Rage", font, sprite, 28, TextAnchor.LowerCenter, new Color(1f, 0.75f, 0.62f),
                new Vector2(0.5f, 0f), new Vector2(-280f, 86f), new Vector2(280f, 124f)),
            prompt = Label(canvasGo.transform, "Pet", font, sprite, 34, TextAnchor.LowerCenter, new Color(1f, 0.9f, 0.75f),
                new Vector2(0.5f, 0f), new Vector2(-420f, 150f), new Vector2(420f, 210f)),
            hint = Label(canvasGo.transform, "Hint", font, sprite, 24, TextAnchor.LowerCenter, new Color(0.9f, 0.86f, 0.78f, 0.9f),
                new Vector2(0.5f, 0f), new Vector2(-640f, 214f), new Vector2(640f, 258f))
        };
        refs.level.text = "LV 1";
        refs.objective.text = "Smash the small stuff";
        refs.timer.text = "00:00";
        refs.left.text = "Left";
        refs.rage.text = "RAGE";
        refs.hint.text = "WASD move     SPACE / click     E pet";

        var xpBack = Bar(canvasGo.transform, sprite, new Color(0.15f, 0.1f, 0.08f, 0.85f), new Vector2(0f, 1f), new Vector2(36f, -168f), new Vector2(396f, -148f));
        refs.xpFill = Bar(xpBack.transform, sprite, new Color(0.95f, 0.72f, 0.28f), Vector2.zero, Vector2.zero, Vector2.zero);
        refs.xpFill.type = Image.Type.Filled;
        refs.xpFill.fillMethod = Image.FillMethod.Horizontal;
        refs.xpFill.fillAmount = 0f;
        var rageBack = Bar(canvasGo.transform, sprite, new Color(0.2f, 0.08f, 0.07f, 0.9f), new Vector2(0.5f, 0f), new Vector2(-280f, 42f), new Vector2(280f, 78f));
        refs.rageFill = Bar(rageBack.transform, sprite, new Color(0.86f, 0.22f, 0.16f), Vector2.zero, Vector2.zero, Vector2.zero);
        refs.rageFill.type = Image.Type.Filled;
        refs.rageFill.fillMethod = Image.FillMethod.Horizontal;
        refs.rageFill.fillAmount = 0f;

        refs.endPanel = new GameObject("Son", typeof(RectTransform));
        refs.endPanel.transform.SetParent(canvasGo.transform, false);
        Stretch(refs.endPanel.GetComponent<RectTransform>());
        var dim = refs.endPanel.AddComponent<Image>();
        dim.sprite = sprite;
        dim.color = new Color(0.05f, 0.03f, 0.03f, 0.62f);
        dim.raycastTarget = false;
        refs.endTitle = Label(refs.endPanel.transform, "Baslik", font, sprite, 64, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.75f),
            new Vector2(0.5f, 0.5f), new Vector2(-700f, 40f), new Vector2(700f, 180f));
        refs.endBody = Label(refs.endPanel.transform, "Govde", font, sprite, 36, TextAnchor.MiddleCenter, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(-700f, -70f), new Vector2(700f, 40f));
        refs.endHint = Label(refs.endPanel.transform, "Yeniden", font, sprite, 28, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.6f),
            new Vector2(0.5f, 0.5f), new Vector2(-700f, -150f), new Vector2(700f, -70f));
        refs.endPanel.SetActive(false);

        var flashGo = new GameObject("Flash", typeof(RectTransform));
        flashGo.transform.SetParent(canvasGo.transform, false);
        Stretch(flashGo.GetComponent<RectTransform>());
        refs.flash = flashGo.AddComponent<Image>();
        refs.flash.sprite = sprite;
        refs.flash.color = new Color(1f, 1f, 1f, 0f);
        refs.flash.raycastTarget = false;
        return canvasGo;
    }

    static void TuneCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = 6.45f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.08f, 0.07f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 40f;
        cam.transform.position = new Vector3(0f, 0f, -12f);
        cam.transform.rotation = Quaternion.identity;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        var extra = cam.GetComponent<UniversalAdditionalCameraData>();
        if (extra == null) return;
        extra.renderPostProcessing = false;
        extra.antialiasing = AntialiasingMode.None;
    }

    static GameObject CatPrefab()
    {
        var calm = TexMat("Kedi", PixelKit.Cat(false).Tex, false, null);
        var happy = TexMat("KediMutlu", PixelKit.Cat(true).Tex, false, null);
        var root = VisualRoot("Kedi", calm, 1.42f, -0.01f);
        var body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.mass = 1f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var circle = root.AddComponent<CircleCollider2D>();
        circle.radius = 0.28f;
        circle.sharedMaterial = Phys("Grip", 0f, 0.15f);
        root.AddComponent<CatPlayer>().Bind(calm, happy);
        return SavePrefab(root, "Kedi");
    }

    static GameObject HumanPrefab()
    {
        var calmStamp = PixelKit.Human(false);
        var smileStamp = PixelKit.Human(true);
        var calm = TexMat("Insan", calmStamp.Tex, false, null);
        var smile = TexMat("InsanGuler", smileStamp.Tex, false, null);
        var root = VisualRoot("Insan", calm, 1.05f, 0f);
        root.AddComponent<CouchHuman>().Bind(null, calm, smile);
        return SavePrefab(root, "Insan");
    }

    static GameObject Breakable(string name, Stamp stamp, float size, int tier, bool round, List<BreakableDefinition> catalog)
    {
        var mat = TexMat(name, stamp.Tex, false, null);
        var def = Definition(name, tier, size, round, stamp.A, stamp.B, stamp.C);
        catalog.Add(def);
        var root = VisualRoot(name, mat, size, 0f);
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.mass = def.mass;
        if (def.mass >= 1.2f)
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var bounce = Phys("Bounce", 0.32f, 0.42f);
        if (round)
        {
            var circle = root.AddComponent<CircleCollider2D>();
            circle.radius = 0.36f;
            circle.sharedMaterial = bounce;
        }
        else
        {
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.74f, 0.72f);
            box.sharedMaterial = bounce;
        }
        root.AddComponent<BreakableItem>().Bind(def);
        return SavePrefab(root, name);
    }

    static GameObject Decor(string name, Stamp stamp, float size, float bias)
    {
        var mat = TexMat(name, stamp.Tex, false, null);
        return SavePrefab(VisualRoot(name, mat, size, bias), name);
    }

    static GameObject Block(string name, Stamp stamp, float size, float bias, PhysicsMaterial2D material, Vector2 offset, Vector2 collider)
    {
        var mat = TexMat(name, stamp.Tex, false, null);
        var root = VisualRoot(name, mat, size, bias);
        var box = root.AddComponent<BoxCollider2D>();
        box.offset = offset;
        box.size = collider;
        box.sharedMaterial = material;
        return SavePrefab(root, name);
    }

    static GameObject Fx(string name, Stamp stamp, System.Type behaviour, bool physical, PhysicsMaterial2D bounce = null)
    {
        var mat = TexMat(name, stamp.Tex, false, null);
        var root = VisualRoot(name, mat, 1f, -0.05f);
        if (physical)
        {
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.mass = 0.25f;
            body.linearDamping = 1.3f;
            body.angularDamping = 0.8f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.7f, 0.7f);
            box.sharedMaterial = bounce;
        }
        root.AddComponent(behaviour);
        return SavePrefab(root, name);
    }

    static Stamp WhiteStamp()
    {
        var pix = new Pix(4, 4);
        var c = Pix.C(255, 255, 255);
        pix.Rect(0, 0, 4, 4, c);
        return new Stamp(pix.ToTex("parca"), c, c, c);
    }

    static GameObject VisualRoot(string name, Material mat, float worldSize, float bias)
    {
        var tex = mat.GetTexture("_BaseMap") as Texture2D;
        float aspect = tex != null ? tex.height / (float)tex.width : 1f;
        var root = new GameObject(name);
        var vis = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        vis.name = "Visual";
        vis.transform.SetParent(root.transform, false);
        root.transform.localScale = new Vector3(worldSize, worldSize * aspect, 1f);
        var renderer = vis.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        vis.AddComponent<DepthSort>().bias = bias;
        return root;
    }

    static GameObject SavePrefab(GameObject root, string name)
    {
        string path = Root + "/Prefabs/" + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject Spawn(GameObject prefab, string name, Vector2 pos, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go;
    }

    static GameObject SceneQuad(string name, Material mat, Vector2 pos, Vector2 size, float z, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, z);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go;
    }

    static void Wall(string name, Material mat, Vector2 pos, Vector2 size, PhysicsMaterial2D bounce, Transform parent)
    {
        var go = SceneQuad(name, mat, pos, size, 1.6f, parent);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        box.sharedMaterial = bounce;
    }

    static string EnglishName(string name)
    {
        switch (name)
        {
            case "Yumak": return "Yarn";
            case "Kupa": return "Mug";
            case "Kitap": return "Book";
            case "Ayakkabi": return "Shoe";
            case "Kumanda": return "Remote";
            case "Kase": return "Bowl";
            case "Saksi": return "Plant";
            case "Vazo": return "Vase";
            case "Lamba": return "Lamp";
            case "Sandalye": return "Chair";
            case "Hoparlor": return "Speaker";
            case "Kasa": return "Crate";
            case "Cop": return "Trash";
            case "Kitaplik": return "Bookshelf";
            case "Televizyon": return "Television";
            case "Masa": return "Table";
            case "CalismaMasasi": return "Desk";
            case "Buzdolabi": return "Fridge";
            case "Gardrop": return "Wardrobe";
            default: return name;
        }
    }

    static BreakableDefinition Definition(string name, int tier, float size, bool round, Color32 a, Color32 b, Color32 c)
    {
        string path = Root + "/Data/" + name + ".asset";
        var def = AssetDatabase.LoadAssetAtPath<BreakableDefinition>(path);
        bool fresh = def == null;
        if (fresh)
        {
            def = ScriptableObject.CreateInstance<BreakableDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }
        def.worldSize = size;
        def.round = round;
        def.debrisA = a;
        def.debrisB = b;
        def.debrisC = c;
        if (fresh)
        {
            def.displayName = EnglishName(name);
            def.tier = tier;
            def.xp = tier switch { 0 => 12, 1 => 26, 2 => 55, _ => 100 };
            def.hp = tier switch { 0 => 1, 1 => 2, 2 => 3, _ => 4 };
            def.mass = 0.45f + tier * 0.95f;
        }
        EditorUtility.SetDirty(def);
        return def;
    }

    static SmashBalance Balance()
    {
        string path = Root + "/Data/SmashBalance.asset";
        var balance = AssetDatabase.LoadAssetAtPath<SmashBalance>(path);
        if (balance != null) return balance;
        balance = ScriptableObject.CreateInstance<SmashBalance>();
        AssetDatabase.CreateAsset(balance, path);
        return balance;
    }

    static PhysicsMaterial2D Phys(string name, float bounciness, float friction)
    {
        string path = Root + "/Physics/" + name + ".physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D(name) { bounciness = bounciness, friction = friction };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
        mat.bounciness = bounciness;
        mat.friction = friction;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material TexMat(string name, Texture2D source, bool repeat, Vector2? scale)
    {
        string texPath = Root + "/Textures/" + name + ".png";
        Directory.CreateDirectory(Path.GetDirectoryName(Abs(texPath)) ?? Root);
        File.WriteAllBytes(Abs(texPath), source.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.textureType = TextureImporterType.Default;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        string matPath = Root + "/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        var shader = Shader.Find("Cat/Pixel");
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.shader = shader;
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        if (scale.HasValue) mat.SetTextureScale("_BaseMap", scale.Value);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Sprite UiSprite()
    {
        string path = Root + "/Textures/UiWhite.png";
        if (!File.Exists(Abs(path)))
        {
            var pix = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            pix.SetPixels32(pixels);
            pix.Apply(false, false);
            Directory.CreateDirectory(Path.GetDirectoryName(Abs(path)) ?? Root);
            File.WriteAllBytes(Abs(path), pix.EncodeToPNG());
            Object.DestroyImmediate(pix);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Text Label(Transform parent, string name, Font font, Sprite sprite, int size, TextAnchor anchor, Color color, Vector2 anchorPoint, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.88f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorPoint;
        rect.anchorMax = anchorPoint;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return text;
    }

    static Image Bar(Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("Bar", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        if (anchor == Vector2.zero && offsetMin == Vector2.zero && offsetMax == Vector2.zero)
            Stretch(rect);
        else
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
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

    static void Ensure(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string leaf = Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            Ensure(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static string Abs(string assetPath)
    {
        return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
    }
}
