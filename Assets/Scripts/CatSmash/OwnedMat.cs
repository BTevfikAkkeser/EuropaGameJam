using UnityEngine;

public class OwnedMat : MonoBehaviour
{
    void OnDestroy()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
            Destroy(renderer.sharedMaterial);
    }
}
