using UnityEngine;

public class DebrisBit : MonoBehaviour
{
    public float life = 1.7f;
    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life < 0.4f)
            transform.localScale = baseScale * Mathf.Max(0.04f, life / 0.4f);
        if (life <= 0f) Destroy(gameObject);
    }
}
