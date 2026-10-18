using UnityEngine;

public class DepthSort : MonoBehaviour
{
    public float bias;

    void LateUpdate()
    {
        float y = transform.parent != null ? transform.parent.position.y : transform.position.y;
        var local = transform.localPosition;
        local.z = y * 0.02f + bias;
        transform.localPosition = local;
    }
}
