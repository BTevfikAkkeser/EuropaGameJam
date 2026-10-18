using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FastestBoardBaker
{
    const string SpritePath = "Assets/Textures/UiWhite.png";

    [MenuItem("CatSmash/Bake Leaderboard")]
    public static void Bake()
    {
        var canvas = Object.FindAnyObjectByType<CatGame>() != null
            ? new SerializedObject(Object.FindAnyObjectByType<CatGame>()).FindProperty("canvasRect").objectReferenceValue as RectTransform
            : null;
        if (canvas == null)
        {
            var hud = GameObject.Find("HUD");
            if (hud != null) canvas = hud.GetComponent<RectTransform>();
        }
        if (canvas == null)
        {
            Debug.LogError("HUD canvas was not found.");
            return;
        }

        var existing = Object.FindAnyObjectByType<FastestBoard>(FindObjectsInactive.Include);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

        var root = new GameObject("FastestBoard", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        var board = root.AddComponent<FastestBoard>();

        var open = Button(root.transform, "FastestButton", "FASTEST", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(168f, 64f), font, sprite, new Color(0.55f, 0.22f, 0.12f, 0.95f));

        var card = Panel(root.transform, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 640f), sprite, new Color(0.12f, 0.07f, 0.06f, 0.94f));
        Label(card.transform, "Title", "Fastest", 36, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(500f, 52f), font, new Color(1f, 0.93f, 0.82f), TextAnchor.MiddleCenter);
        Label(card.transform, "Hint", "Your name shows on the board. Letters and numbers, no spaces.", 16, new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(500f, 36f), font, new Color(1f, 0.86f, 0.72f, 0.85f), TextAnchor.MiddleCenter);
        var field = Field(card.transform, font, sprite);
        var save = Button(card.transform, "Save", "Save", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -128f), new Vector2(120f, 48f), font, sprite, new Color(0.55f, 0.22f, 0.12f, 0.95f));
        var close = Button(card.transform, "Close", "Close", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(120f, 44f), font, sprite, new Color(0.55f, 0.22f, 0.12f, 0.95f));
        var refresh = Button(card.transform, "Refresh", "Refresh", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(140f, 44f), font, sprite, new Color(0.55f, 0.22f, 0.12f, 0.95f));
        var rows = Label(card.transform, "Rows", "", 24, new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(500f, 320f), font, new Color(1f, 0.95f, 0.88f), TextAnchor.UpperLeft);
        rows.rectTransform.pivot = new Vector2(0.5f, 1f);
        rows.rectTransform.anchoredPosition = new Vector2(0f, -196f);
        var status = Label(card.transform, "Status", "", 16, new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(280f, 36f), font, new Color(1f, 0.75f, 0.55f), TextAnchor.MiddleCenter);

        var notice = Panel(root.transform, "Notice", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -86f), new Vector2(720f, 64f), sprite, new Color(0.45f, 0.12f, 0.08f, 0.94f));
        notice.GetComponent<Image>().raycastTarget = false;
        var noticeText = Label(notice.transform, "NoticeText", "New record", 32, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 56f), font, new Color(1f, 0.95f, 0.82f), TextAnchor.MiddleCenter);

        UnityEventTools.AddPersistentListener(open.onClick, board.ToggleBoard);
        UnityEventTools.AddPersistentListener(save.onClick, board.SaveName);
        UnityEventTools.AddPersistentListener(close.onClick, board.CloseBoard);
        UnityEventTools.AddPersistentListener(refresh.onClick, board.RefreshBoard);

        var boardObject = new SerializedObject(board);
        boardObject.FindProperty("openButton").objectReferenceValue = open.gameObject;
        boardObject.FindProperty("card").objectReferenceValue = card;
        boardObject.FindProperty("nameField").objectReferenceValue = field;
        boardObject.FindProperty("rows").objectReferenceValue = rows;
        boardObject.FindProperty("status").objectReferenceValue = status;
        boardObject.FindProperty("notice").objectReferenceValue = noticeText;
        boardObject.FindProperty("noticeRoot").objectReferenceValue = notice;
        boardObject.ApplyModifiedPropertiesWithoutUndo();

        card.SetActive(false);
        notice.SetActive(false);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Debug.Log("Leaderboard UI saved on the HUD canvas.");
    }

    static GameObject Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return go;
    }

    static Button Button(Transform parent, string name, string caption, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Font font, Sprite sprite, Color color)
    {
        var go = Panel(parent, name, anchorMin, anchorMax, pivot, position, size, sprite, color);
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var label = Label(go.transform, "Label", caption, 22, new Vector2(0.5f, 0.5f), Vector2.zero, size, font, new Color(1f, 0.95f, 0.86f), TextAnchor.MiddleCenter);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return button;
    }

    static InputField Field(Transform parent, Font font, Sprite sprite)
    {
        var go = Panel(parent, "Name", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -128f), new Vector2(360f, 48f), sprite, new Color(0.22f, 0.14f, 0.1f, 1f));
        var field = go.AddComponent<InputField>();
        field.characterLimit = 16;
        var text = Label(go.transform, "Text", "", 22, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(336f, 40f), font, Color.white, TextAnchor.MiddleLeft);
        Stretch(text.rectTransform, 12f);
        var placeholder = Label(go.transform, "Placeholder", "Your name", 22, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(336f, 40f), font, new Color(1f, 1f, 1f, 0.35f), TextAnchor.MiddleLeft);
        placeholder.fontStyle = FontStyle.Italic;
        Stretch(placeholder.rectTransform, 12f);
        field.textComponent = text;
        field.placeholder = placeholder;
        return field;
    }

    static void Stretch(RectTransform rect, float pad)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(pad, 4f);
        rect.offsetMax = new Vector2(-pad, -4f);
    }

    static Text Label(Transform parent, string name, string message, int size, Vector2 anchor, Vector2 position, Vector2 box, Font font, Color color, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = box;
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.text = message;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
