using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BreakableItem : MonoBehaviour
{
    [SerializeField] BreakableDefinition definition;

    public int Tier => definition != null ? definition.tier : 0;
    public int Xp => definition != null ? definition.xp : 0;
    public int Hp { get; private set; }
    public bool Dead { get; private set; }
    public BreakableDefinition Definition => definition;

    Rigidbody2D body;
    Transform visual;
    FurnitureBreak furniture;
    Vector3 visualScale = Vector3.one;
    float punch;
    float nextToast;

    public void Bind(BreakableDefinition def)
    {
        definition = def;
    }

    void Awake()
    {
        visual = transform.Find("Visual");
        if (visual != null) visualScale = visual.localScale;
        body = GetComponent<Rigidbody2D>();
        furniture = GetComponent<FurnitureBreak>();
        if (definition == null)
        {
            Debug.LogError(name + " için BreakableDefinition yok.", this);
            enabled = false;
            return;
        }
        Hp = Mathf.Max(1, definition.hp);
        if (body != null) body.mass = definition.mass;
    }

    void Update()
    {
        if (Dead) return;
        if (CatGame.I != null && !CatGame.InsideRooms(transform.position))
        {
            Discard();
            return;
        }
        if (visual == null) return;
        punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 7f);
        float swell = 1f + Mathf.Sin(punch * Mathf.PI) * 0.2f;
        visual.localScale = visualScale * swell;
    }

    public int Hit(Vector2 direction, int level, bool rage, float force)
    {
        if (Dead || definition == null || body == null) return 0;
        var balance = CatGame.I != null ? CatGame.I.Balance : null;
        int need = definition.tier + 1;
        bool allowed = level >= need || (rage && level + 1 >= need);
        Wake();
        float scale = allowed || balance == null ? 1f : balance.blockedForceScale;
        float impulse = force * Mathf.Max(1f, body.mass * 0.85f) * scale;
        body.AddForce(direction * impulse, ForceMode2D.Impulse);
        body.AddTorque(Random.Range(-1f, 1f) * impulse * 0.45f, ForceMode2D.Impulse);
        punch = 1f;

        if (!allowed)
        {
            if (Time.unscaledTime >= nextToast && CatGame.I != null)
            {
                nextToast = Time.unscaledTime + 0.45f;
                string line = level + 1 < need ? "too tough" : "grow stronger";
                CatGame.I.Float(transform.position, line, new Color(1f, 0.86f, 0.7f));
            }
            return 1;
        }

        if (furniture != null)
        {
            if (!furniture.Advance(direction)) return 2;
            Break(direction, rage);
            return 3;
        }

        int damage = rage && balance != null ? Mathf.Max(1, balance.rageDamage) : 1;
        Hp -= damage;
        if (Hp > 0) return 2;
        Break(direction, rage);
        return 3;
    }

    void Wake()
    {
        if (body.bodyType == RigidbodyType2D.Dynamic) return;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.linearDamping = 2.4f;
        body.angularDamping = 1.5f;
    }

    void Break(Vector2 direction, bool rage)
    {
        Dead = true;
        if (CatGame.I != null)
        {
            CatGame.I.AddXp(definition.xp, transform.position);
            CatGame.I.Burst(transform.position, direction, definition, rage);
            CatGame.I.NotifyBroken();
        }
        Destroy(gameObject);
    }

    public void Discard()
    {
        if (Dead) return;
        Dead = true;
        if (CatGame.I != null) CatGame.I.NotifyBroken();
        Destroy(gameObject);
    }
}
