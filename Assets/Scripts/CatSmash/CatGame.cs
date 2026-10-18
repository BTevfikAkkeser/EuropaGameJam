using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CatGame : MonoBehaviour
{
    public static CatGame I { get; private set; }

    public SmashBalance Balance => balance;
    public BreakableCatalog Catalog => catalog;
    public Camera Cam => cam;
    public CouchHuman Human => human;
    public bool Won { get; private set; }
    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }
    public float Rage { get; private set; }
    public int Alive { get; private set; }
    public int Total { get; private set; }
    public bool Playing { get; private set; }

    [SerializeField] SmashBalance balance;
    [SerializeField] BreakableCatalog catalog;
    [SerializeField] Camera cam;
    [SerializeField] CatPlayer cat;
    [SerializeField] CouchHuman human;
    [SerializeField] GameObject slashPrefab;
    [SerializeField] GameObject sparkPrefab;
    [SerializeField] GameObject debrisPrefab;
    [SerializeField] FloatPopup floatPrefab;
    [SerializeField] Text levelText;
    [SerializeField] Text objectiveText;
    [SerializeField] Text timerText;
    [SerializeField] Text leftText;
    [SerializeField] Text rageText;
    [SerializeField] Text promptText;
    [SerializeField] Text hintText;
    [SerializeField] Text endTitle;
    [SerializeField] Text endBody;
    [SerializeField] Text endHint;
    [SerializeField] Image rageFill;
    [SerializeField] Image xpFill;
    [SerializeField] Image flash;
    [SerializeField] GameObject endPanel;
    [SerializeField] RectTransform canvasRect;

    AudioSource sfx;
    AudioSource purr;
    [SerializeField] AudioClip hitClip;
    [SerializeField] AudioClip breakClip;
    [SerializeField] AudioClip swishClip;
    [SerializeField] AudioClip thunkClip;
    [SerializeField] AudioClip levelClip;
    [SerializeField] AudioClip meowClip;
    [SerializeField] AudioClip purrClip;
    Vector2 shake;
    float shakeMag;
    Vector3 camHome;
    Vector3 camFocus;
    Coroutine hitStop;
    bool clock;
    float elapsed;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        Physics2D.gravity = Vector2.zero;
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            camHome = cam.transform.position;
            camFocus = camHome;
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = Vector3.up;
        }
        Playing = FindAnyObjectByType<CatSelect>() == null;
        var sources = GetComponents<AudioSource>();
        sfx = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        purr = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        purr.playOnAwake = false;
        sfx.spatialBlend = 0f;
        purr.spatialBlend = 0f;
        if (hitClip == null) BuildAudio();
        CountRoom();
    }

    void OnDestroy()
    {
        if (I == this) I = null;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (clock && !Won) elapsed += Time.deltaTime;
        if (Won && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        shakeMag = Mathf.Lerp(shakeMag, 0f, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        shake = Vector2.Lerp(shake, Random.insideUnitCircle * shakeMag, 1f - Mathf.Exp(-30f * Time.unscaledDeltaTime));
        if (cam != null)
        {
            camFocus = Vector3.Lerp(camFocus, camHome, 1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));
            cam.transform.position = camFocus + new Vector3(shake.x, shake.y, 0f);
            cam.transform.rotation = Quaternion.identity;
        }
        RefreshHud();
    }

    void CountRoom()
    {
        var items = FindObjectsByType<BreakableItem>(FindObjectsSortMode.None);
        Alive = items.Length;
        Total = items.Length;
    }

    public void NotifyActivity()
    {
        if (!Won) clock = true;
    }

    public void AddXp(int amount, Vector2 where)
    {
        int before = Level;
        Xp += amount;
        Float(where, "+" + amount, new Color(1f, 0.86f, 0.35f));
        int now = balance != null ? balance.LevelFromXp(Xp) : before;
        if (now != before)
        {
            Level = now;
            if (cat != null) cat.Grow(Level);
            Float(where + Vector2.up * 0.45f, "LEVEL " + Level + "!", new Color(1f, 0.95f, 0.55f));
            Play(levelClip, 0.55f, 1f);
            Shake(0.16f);
        }
    }

    public void AddRage(float amount)
    {
        if (Rage >= 1f || Won) return;
        Rage = Mathf.Clamp01(Rage + amount);
    }

    public bool ConsumeRage()
    {
        if (Rage < 1f) return false;
        Rage = 0f;
        return true;
    }

    public static bool InsideRooms(Vector2 p)
    {
        bool room1 = p.x >= -9.8f && p.x <= 10.2f && p.y >= -6.6f && p.y <= 6.6f;
        bool room2 = p.x >= 16f && p.x <= 36.2f && p.y >= -6.6f && p.y <= 6.6f;
        return room1 || room2;
    }

    public static Vector2 KeepInRoom(Vector2 p)
    {
        if (p.x < 13f)
            return new Vector2(Mathf.Clamp(p.x, -8.3f, 8.3f), Mathf.Clamp(p.y, -5.3f, 5.3f));
        return new Vector2(Mathf.Clamp(p.x, 18.1f, 34.5f), Mathf.Clamp(p.y, -5.3f, 5.3f));
    }

    public void NotifyBroken()
    {
        Recount();
    }

    public void MoveCamera(Vector3 home)
    {
        home.z = camHome.z;
        camHome = home;
    }

    public void AddTarget()
    {
        if (Won) return;
        Total++;
        Recount();
    }

    void Recount()
    {
        if (Won) return;
        var items = FindObjectsByType<BreakableItem>(FindObjectsSortMode.None);
        int left = 0;
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null || item.Dead) continue;
            if (!InsideRooms(item.transform.position)) continue;
            left++;
        }
        Alive = left;
        if (clock && left == 0) Win();
    }

    public void Shake(float magnitude)
    {
        shakeMag = Mathf.Max(shakeMag, magnitude);
    }

    public void HitStop(float seconds)
    {
        if (hitStop != null) StopCoroutine(hitStop);
        hitStop = StartCoroutine(StopTime(seconds));
    }

    public void Flash(Color color)
    {
        if (flash != null) StartCoroutine(FlashCo(color));
    }

    public void StartPurr()
    {
        if (purrClip == null) return;
        purr.clip = purrClip;
        purr.loop = true;
        purr.volume = 0.55f;
        purr.Play();
        Play(meowClip, 0.4f, Random.Range(0.96f, 1.05f));
    }

    public void Begin()
    {
        Playing = true;
    }

    public void StopPurr()
    {
        if (purr != null) purr.Stop();
    }

    public void PlayHit(bool rage) { Play(hitClip, rage ? 0.7f : 0.45f, Random.Range(0.9f, 1.15f)); }
    public void PlayBreak(bool rage) { Play(breakClip, rage ? 0.8f : 0.55f, Random.Range(0.85f, 1.1f)); }
    public void PlaySwish(bool rage) { Play(swishClip, rage ? 0.35f : 0.18f, Random.Range(1f, 1.2f)); }
    public void PlayThunk() { Play(thunkClip, 0.4f, Random.Range(0.8f, 1f)); }

    public void Slash(Vector2 point, Vector2 direction, bool rage)
    {
        if (slashPrefab == null) return;
        var go = Instantiate(slashPrefab, point, Quaternion.identity);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.localScale = Vector3.one * (rage ? 1.45f : 0.85f);
        var life = go.GetComponent<UnscaledLife>();
        if (life != null) life.life = rage ? 0.1f : 0.07f;
        Sparks(point, direction, rage ? 7 : 3);
    }

    public void Sparks(Vector2 point, Vector2 direction, int count)
    {
        if (sparkPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(sparkPrefab, point + Random.insideUnitCircle * 0.15f, Quaternion.identity);
            go.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
            var spark = go.GetComponent<SparkBit>();
            if (spark == null) continue;
            spark.velocity = direction * Random.Range(1.5f, 4.5f) + Random.insideUnitCircle * 2.2f;
            spark.life = Random.Range(0.16f, 0.32f);
        }
    }

    public void Burst(Vector2 point, Vector2 direction, BreakableDefinition definition, bool rage)
    {
        if (debrisPrefab == null || definition == null) return;
        int count = rage ? 8 : 6;
        Color[] colors = { definition.debrisA, definition.debrisB, definition.debrisC };
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(debrisPrefab, point + (Vector2)Random.insideUnitCircle * 0.2f, Quaternion.identity);
            go.transform.localScale = Vector3.one * Random.Range(0.18f, 0.36f);
            Tint(go, colors[i % colors.Length]);
            var debris = go.GetComponent<Rigidbody2D>();
            if (debris != null)
            {
                Vector2 fly = direction * 0.7f + Random.insideUnitCircle;
                if (fly.sqrMagnitude < 0.01f) fly = Vector2.right;
                debris.AddForce(fly.normalized * Random.Range(2.8f, 5.6f), ForceMode2D.Impulse);
                debris.AddTorque(Random.Range(-6f, 6f), ForceMode2D.Impulse);
            }
            var bit = go.GetComponent<DebrisBit>();
            if (bit != null) bit.life = Random.Range(1.2f, 1.8f);
        }
    }

    static void Tint(GameObject go, Color color)
    {
        var renderer = go.GetComponentInChildren<Renderer>();
        if (renderer == null) return;
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        renderer.SetPropertyBlock(block);
    }

    public void Float(Vector2 world, string message, Color color)
    {
        if (floatPrefab == null || canvasRect == null || cam == null) return;
        var popup = Instantiate(floatPrefab, canvasRect);
        popup.Show(message, color, world, cam, canvasRect);
    }

    void RefreshHud()
    {
        if (levelText == null) return;
        levelText.text = "LV " + Level;
        if (objectiveText != null)
            objectiveText.text = balance != null ? balance.Hint(Level, Rage >= 1f) : "";
        if (timerText != null)
            timerText.text = clock || Won ? Clock(elapsed) : "00:00";
        if (leftText != null)
            leftText.text = "Left " + Alive;
        if (rageText != null)
            rageText.text = Rage >= 1f ? "RAGE FULL" : "RAGE";
        if (rageFill != null)
        {
            rageFill.fillAmount = Rage;
            rageFill.color = Rage >= 1f
                ? Color.Lerp(new Color(1f, 0.42f, 0.12f), new Color(1f, 0.92f, 0.35f), Mathf.PingPong(Time.unscaledTime * 4f, 1f))
                : new Color(0.86f, 0.22f, 0.16f);
        }
        if (xpFill != null && balance != null)
            xpFill.fillAmount = balance.XpFill(Level, Xp);

        bool near = cat != null && human != null && human.InRange(cat.transform.position);
        if (promptText != null)
        {
            if (cat != null && cat.Busy) promptText.text = "purrrr...";
            else if (near && !Won) promptText.text = "E  —  get pet";
            else if (near) promptText.text = "E  —  pet again";
            else promptText.text = "";
        }
        if (hintText != null && clock && elapsed > 7f)
            hintText.gameObject.SetActive(false);
    }

    void Win()
    {
        Won = true;
        clock = false;
        Time.timeScale = 1f;
        if (endPanel != null) endPanel.SetActive(true);
        if (endTitle != null) endTitle.text = "You trashed the room in " + Pretty(elapsed);
        if (endBody != null)
            endBody.text = elapsed < 55f ? "Like a storm." : elapsed < 95f ? "The room didn't stand a chance." : "The last piece fell.";
        if (endHint != null) endHint.text = "R to restart";
        Play(levelClip, 0.45f, 0.8f);
        FastestBoard.Submit(elapsed);
    }

    static string Clock(float time)
    {
        int total = Mathf.FloorToInt(time);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    static string Pretty(float time)
    {
        if (time >= 60f)
        {
            int minutes = (int)time / 60;
            float seconds = time - minutes * 60f;
            return minutes + " min " + seconds.ToString("00") + " sec";
        }
        return time.ToString("0.0") + " seconds";
    }

    IEnumerator StopTime(float seconds)
    {
        Time.timeScale = 0.04f;
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = 1f;
        hitStop = null;
    }

    IEnumerator FlashCo(Color color)
    {
        float duration = 0.12f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var c = color;
            c.a = color.a * (1f - t / duration);
            flash.color = c;
            yield return null;
        }
    }

    void BuildAudio()
    {
        hitClip = Noise("hit", 0.07f, 0.55f, 1400f);
        breakClip = Noise("break", 0.18f, 0.7f, 280f);
        swishClip = Noise("swish", 0.05f, 0.3f, 900f);
        thunkClip = Noise("thunk", 0.09f, 0.5f, 160f);
        levelClip = Tone("level", 520f, 780f, 0.16f, 0.4f);
        meowClip = Tone("meow", 480f, 760f, 0.2f, 0.35f);
        purrClip = PurrLoop();
    }

    static void PlayOn(AudioSource source, AudioClip clip, float volume, float pitch)
    {
        if (clip == null || source == null) return;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
        source.pitch = 1f;
    }

    void Play(AudioClip clip, float volume, float pitch)
    {
        PlayOn(sfx, clip, volume, pitch);
    }

    static AudioClip Noise(string clipName, float duration, float volume, float tone)
    {
        int rate = 44100;
        int count = Mathf.Max(1, (int)(rate * duration));
        var data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float u = 1f - i / (float)count;
            float env = u * u;
            float n = (((i * 1103515245) >> 8) & 255) / 127.5f - 1f;
            float wave = Mathf.Sin(i * tone * 2f * Mathf.PI / rate);
            data[i] = (n * 0.75f + wave * 0.25f) * env * volume;
        }
        var clip = AudioClip.Create(clipName, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip Tone(string clipName, float from, float to, float duration, float volume)
    {
        int rate = 44100;
        int count = Mathf.Max(1, (int)(rate * duration));
        var data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float u = i / (float)count;
            float env = Mathf.Sin(u * Mathf.PI);
            float freq = Mathf.Lerp(from, to, u);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)rate)) * env * volume;
        }
        var clip = AudioClip.Create(clipName, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip PurrLoop()
    {
        int rate = 44100;
        int count = rate * 2;
        var data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float wobble = 0.62f + 0.38f * Mathf.Sin(2f * Mathf.PI * 26f * t);
            float tone = Mathf.Sin(2f * Mathf.PI * 78f * t);
            data[i] = tone * wobble * 0.28f;
        }
        var clip = AudioClip.Create("purr", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
