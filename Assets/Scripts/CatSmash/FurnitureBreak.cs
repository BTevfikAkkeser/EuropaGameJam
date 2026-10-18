using UnityEngine;

public class FurnitureBreak : MonoBehaviour
{
    [SerializeField] Sprite[] stages;
    [SerializeField] Sprite[] chips;
    [SerializeField] BreakableItem chipPrefab;
    [SerializeField] SpriteRenderer view;

    int stage;

    public bool Advance(Vector2 direction)
    {
        if (stages == null || stages.Length == 0) return true;
        if (stage >= stages.Length - 1) return true;

        stage++;
        if (view != null && stages[stage] != null)
            view.sprite = stages[stage];
        SpawnChip(direction);
        return false;
    }

    void SpawnChip(Vector2 direction)
    {
        if (chipPrefab == null || chips == null || chips.Length == 0) return;
        Vector2 fly = direction.sqrMagnitude > 0.01f ? direction.normalized : Random.insideUnitCircle.normalized;
        var chip = Instantiate(chipPrefab, transform.parent);
        Vector2 spot = CatGame.KeepInRoom((Vector2)transform.position + fly * 0.9f);
        chip.transform.position = spot;
        chip.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
        var sprite = chip.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) sprite.sprite = chips[(stage - 1) % chips.Length];
        if (CatGame.I != null) CatGame.I.AddTarget();

        var body = chip.GetComponent<Rigidbody2D>();
        if (body == null) return;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.linearDamping = 2.2f;
        body.angularDamping = 1.2f;
        body.AddForce((fly + Random.insideUnitCircle * 0.35f).normalized * Random.Range(2.4f, 3.8f), ForceMode2D.Impulse);
        body.AddTorque(Random.Range(-4f, 4f), ForceMode2D.Impulse);
    }
}
