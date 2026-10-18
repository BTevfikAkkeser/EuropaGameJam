using UnityEngine;

public class UnscaledLife : MonoBehaviour
{
    public float life = 0.08f;

    void Update()
    {
        life -= Time.unscaledDeltaTime;
        if (life <= 0f) Destroy(gameObject);
    }
}
