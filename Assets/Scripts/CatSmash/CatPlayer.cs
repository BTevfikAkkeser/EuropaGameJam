using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CatPlayer : MonoBehaviour
{
    [SerializeField] Material calmMaterial;
    [SerializeField] Material happyMaterial;

    public bool Busy { get; private set; }

    Rigidbody2D body;
    Transform visual;
    Renderer face;
    CatAnimatorDriver driver;
    int facingIndex;
    Vector3 baseScale;
    Vector3 visualScale = Vector3.one;
    Vector2 wish;
    Vector2 facing = Vector2.up;
    float nextAttack;
    float squash;
    readonly List<BreakableItem> seen = new List<BreakableItem>(16);

    public void Place(Vector2 spot)
    {
        transform.position = spot;
        if (body != null)
        {
            body.position = spot;
            body.linearVelocity = Vector2.zero;
        }
    }

    public void Bind(Material calm, Material happy)
    {
        calmMaterial = calm;
        happyMaterial = happy;
    }

    void Awake()
    {
        baseScale = transform.localScale;
        visual = transform.Find("Visual");
        if (visual != null)
        {
            visualScale = visual.localScale;
            face = visual.GetComponent<MeshRenderer>();
            driver = visual.GetComponent<CatAnimatorDriver>();
        }
        body = GetComponent<Rigidbody2D>();
    }

    public void HideForSelect()
    {
        if (visual == null) return;
        var sprite = visual.GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.enabled = false;
        if (face != null) face.enabled = false;
    }

    public void ApplySkin(CatSkin skin)
    {
        if (visual == null) return;
        var sprite = visual.GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.enabled = true;
        if (face != null) face.enabled = false;
        if (driver == null) driver = visual.GetComponent<CatAnimatorDriver>();
        if (driver != null && skin != null) driver.Use(skin.controller, false, facingIndex);
    }

    public void Grow(int level)
    {
        float step = CatGame.I != null && CatGame.I.Balance != null ? CatGame.I.Balance.growPerLevel : 0.07f;
        float scale = 1f + (level - 1) * step;
        var next = baseScale * scale;
        next.z = 1f;
        transform.localScale = next;
    }

    void Update()
    {
        if (CatGame.I == null || !CatGame.I.Playing) return;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        wish = Vector2.zero;
        if (!Busy && keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) wish.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) wish.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) wish.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) wish.x += 1f;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            if (wish.sqrMagnitude > 0.01f)
            {
                facing = wish;
                facingIndex = FacingFrom(wish);
                CatGame.I.NotifyActivity();
            }
        }

        bool attack = false;
        if (!Busy && !CatGame.I.Won)
        {
            if (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.jKey.isPressed)) attack = true;
            if (mouse != null && mouse.leftButton.isPressed) attack = true;
        }

        if (attack) TryAttack(mouse);
        if (!Busy && keyboard != null && keyboard.eKey.wasPressedThisFrame) TryPet();

        if (driver != null) driver.Show(wish.sqrMagnitude > 0.01f && !Busy, facingIndex);

        squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 8f);
        if (visual != null)
        {
            float breathe = 1f + Mathf.Sin(Time.time * 3.2f) * 0.025f;
            float sx = (1f + squash) * breathe;
            float sy = (1f - squash * 0.8f) * breathe;
            visual.localScale = new Vector3(visualScale.x * sx, visualScale.y * sy, 1f);
        }
    }

    void FixedUpdate()
    {
        if (body == null || CatGame.I == null) return;
        var balance = CatGame.I.Balance;
        float speed = balance != null
            ? balance.baseSpeed + (CatGame.I.Level - 1) * balance.speedPerLevel
            : 5.1f;
        body.linearVelocity = (Busy ? Vector2.zero : wish) * speed;
        if (driver != null)
        {
            body.MoveRotation(0f);
            if (visual != null)
            {
                float petWobble = Busy ? Mathf.Sin(Time.time * 46f) * 5f : 0f;
                visual.localRotation = Quaternion.Euler(0f, 0f, petWobble);
            }
            return;
        }
        if (facing.sqrMagnitude < 0.001f) return;
        float angle = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;
        float wobble = Busy ? Mathf.Sin(Time.time * 46f) * 5f : 0f;
        body.MoveRotation(-angle + wobble);
    }

    static int FacingFrom(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x < 0f ? 2 : 3;
        return direction.y < 0f ? 0 : 1;
    }

    void TryPet()
    {
        var human = CatGame.I.Human;
        if (human == null || !human.InRange(transform.position)) return;
        StartCoroutine(Pet());
    }

    IEnumerator Pet()
    {
        Busy = true;
        wish = Vector2.zero;
        var human = CatGame.I.Human;
        Vector2 to = (Vector2)human.transform.position - (Vector2)transform.position;
        if (to.sqrMagnitude > 0.01f)
        {
            facing = to.normalized;
            facingIndex = FacingFrom(facing);
        }
        SetHappy(true);
        human.StartPet();
        CatGame.I.StartPurr();
        float duration = CatGame.I.Balance != null ? CatGame.I.Balance.petDuration : 2.35f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        CatGame.I.StopPurr();
        human.StopPet();
        SetHappy(false);
        CatGame.I.AddRage(1f);
        Busy = false;
    }

    void SetHappy(bool happy)
    {
        if (face == null) return;
        var material = happy ? happyMaterial : calmMaterial;
        if (material != null) face.sharedMaterial = material;
    }

    void TryAttack(Mouse mouse)
    {
        var balance = CatGame.I.Balance;
        if (balance == null || body == null) return;
        if (Time.unscaledTime < nextAttack) return;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && CatGame.I.Cam != null)
        {
            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = CatGame.I.Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 12f));
            Vector2 aim = (Vector2)world - (Vector2)transform.position;
            if (aim.sqrMagnitude > 0.04f)
            {
                facing = aim.normalized;
                facingIndex = FacingFrom(facing);
            }
        }

        bool rage = CatGame.I.ConsumeRage();
        nextAttack = Time.unscaledTime + (rage ? balance.rageCooldown : balance.attackCooldown);
        CatGame.I.NotifyActivity();
        squash = rage ? 0.45f : 0.22f;
        body.position += facing * (rage ? balance.rageLunge : balance.lunge);

        Vector2 point = (Vector2)transform.position + facing * (rage ? balance.rageReach : balance.attackReach);
        float radius = rage ? balance.rageRadius : balance.attackRadius;
        CatGame.I.Slash(point, facing, rage);

        var hits = Physics2D.OverlapCircleAll(point, radius);
        seen.Clear();
        int damaged = 0;
        int blocked = 0;
        int broken = 0;
        float force = rage ? balance.rageForce : balance.attackForce;
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null || hit.attachedRigidbody == body) continue;
            var item = hit.GetComponent<BreakableItem>();
            if (item == null)
            {
                var other = hit.attachedRigidbody;
                if (other != null && other.bodyType == RigidbodyType2D.Dynamic)
                    other.AddForce(facing * (rage ? 3.5f : 1.4f), ForceMode2D.Impulse);
                continue;
            }
            if (seen.Contains(item)) continue;
            seen.Add(item);
            int result = item.Hit(facing, CatGame.I.Level, rage, force);
            if (result == 1) blocked++;
            else if (result == 2) damaged++;
            else if (result == 3)
            {
                damaged++;
                broken++;
            }
        }

        if (damaged > 0)
            CatGame.I.AddRage(Mathf.Min(balance.rageSwingCap, balance.ragePerHit + damaged * balance.ragePerExtra));
        else if (blocked > 0)
            CatGame.I.AddRage(balance.rageOnBlock);

        if (broken > 0)
        {
            CatGame.I.PlayBreak(rage);
            CatGame.I.Shake(rage ? 0.22f : 0.1f);
            CatGame.I.HitStop(rage ? 0.07f : 0.04f);
            CatGame.I.Flash(rage ? new Color(1f, 0.45f, 0.15f, 0.28f) : new Color(1f, 0.95f, 0.85f, 0.12f));
        }
        else if (damaged > 0)
        {
            CatGame.I.PlayHit(rage);
            CatGame.I.Shake(rage ? 0.16f : 0.05f);
            CatGame.I.HitStop(0.025f);
            CatGame.I.Sparks(point, facing, rage ? 8 : 4);
        }
        else if (blocked > 0)
        {
            CatGame.I.PlayThunk();
            CatGame.I.Shake(0.04f);
        }
        else CatGame.I.PlaySwish(rage);
    }
}
