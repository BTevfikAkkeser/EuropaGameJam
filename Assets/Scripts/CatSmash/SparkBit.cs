using UnityEngine;

public class SparkBit : MonoBehaviour
{
    public Vector2 velocity;
    public float life = 0.26f;
    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
        velocity *= 0.9f;
        life -= Time.unscaledDeltaTime;
        transform.localScale = baseScale * Mathf.Clamp01(life / 0.26f);
        if (life <= 0f) Destroy(gameObject);
    }
}
