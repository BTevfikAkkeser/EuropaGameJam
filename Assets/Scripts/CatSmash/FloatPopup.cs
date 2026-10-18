using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FloatPopup : MonoBehaviour
{
    [SerializeField] Text label;
    [SerializeField] float duration = 0.75f;
    [SerializeField] float rise = 0.7f;

    public void Show(string message, Color color, Vector2 world, Camera cam, RectTransform canvas)
    {
        if (label == null) label = GetComponent<Text>();
        if (label != null)
        {
            label.text = message;
            label.color = color;
        }
        StartCoroutine(Rise(color, world, cam, canvas));
    }

    IEnumerator Rise(Color color, Vector2 world, Camera cam, RectTransform canvas)
    {
        var rect = (RectTransform)transform;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            if (cam != null && canvas != null)
            {
                Vector3 screen = cam.WorldToScreenPoint(world + Vector2.up * (rise * (t / duration)));
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out var local))
                    rect.anchoredPosition = local;
            }
            if (label != null)
            {
                var c = color;
                c.a = 1f - t / duration;
                label.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
