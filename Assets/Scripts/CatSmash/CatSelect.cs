using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class CatSelect : MonoBehaviour
{
    [SerializeField] CatSkin komur;
    [SerializeField] CatAnimatorDriver preview;
    [SerializeField] GameObject panel;
    [SerializeField] CatPlayer player;

    public bool Chosen { get; private set; }

    void Awake()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }

    void Start()
    {
        if (player != null) player.HideForSelect();
        if (preview != null && komur != null)
            preview.Use(komur.controller, false, 0);
    }

    void Update()
    {
        if (Chosen || FastestBoard.Blocking) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && (
            keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame ||
            keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            Choose();

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (OverBoard(mouse.position.ReadValue())) return;
        Choose();
    }

    static bool OverBoard(Vector2 screen)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screen };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i].gameObject.GetComponentInParent<FastestBoard>() != null)
                return true;
        }
        return false;
    }

    public void Choose()
    {
        if (Chosen || komur == null || player == null) return;
        Chosen = true;
        player.ApplySkin(komur);
        if (panel != null) panel.SetActive(false);
        if (preview != null) preview.gameObject.SetActive(false);
        FastestBoard.HidePicker();
        if (CatGame.I != null) CatGame.I.Begin();
    }
}
