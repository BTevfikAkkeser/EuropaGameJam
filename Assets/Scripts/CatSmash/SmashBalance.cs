using UnityEngine;

[CreateAssetMenu(menuName = "Cat Smash/Balance", fileName = "SmashBalance")]
public class SmashBalance : ScriptableObject
{
    [Header("Level")]
    [Tooltip("Index 0 is the LV1 gate (0). Later values are the XP required to reach that level.")]
    public int[] levelXp = { 0, 40, 150, 340 };

    [Tooltip("Subtitle for each level, in order.")]
    public string[] levelHints =
    {
        "Smash the small stuff",
        "Medium objects unlocked",
        "Big objects unlocked",
        "Everything breaks"
    };

    public string rageReadyHint = "RAGE FULL — hit hard";

    [Header("Cat")]
    public float baseSpeed = 5.1f;
    public float speedPerLevel = 0.28f;
    public float growPerLevel = 0.07f;
    public float petRange = 1.8f;
    public float petDuration = 2.35f;

    [Header("Attack")]
    public float attackCooldown = 0.2f;
    public float rageCooldown = 0.4f;
    public float attackReach = 0.62f;
    public float rageReach = 0.85f;
    public float attackRadius = 0.6f;
    public float rageRadius = 1.2f;
    public float attackForce = 2.5f;
    public float rageForce = 5.4f;
    public float lunge = 0.1f;
    public float rageLunge = 0.26f;
    public float blockedForceScale = 0.28f;
    public int rageDamage = 2;

    [Header("Rage")]
    public float ragePerHit = 0.1f;
    public float ragePerExtra = 0.07f;
    public float rageSwingCap = 0.36f;
    public float rageOnBlock = 0.05f;

    public int LevelFromXp(int xp)
    {
        int level = 1;
        if (levelXp == null) return 1;
        for (int i = 1; i < levelXp.Length; i++)
        {
            if (xp >= levelXp[i]) level = i + 1;
        }
        return Mathf.Clamp(level, 1, Mathf.Max(1, levelXp.Length));
    }

    public float XpFill(int level, int xp)
    {
        if (levelXp == null || level >= levelXp.Length) return 1f;
        int from = levelXp[Mathf.Max(0, level - 1)];
        int to = levelXp[level];
        if (to <= from) return 1f;
        return Mathf.InverseLerp(from, to, xp);
    }

    public string Hint(int level, bool rageReady)
    {
        if (rageReady) return rageReadyHint;
        if (levelHints == null || levelHints.Length == 0) return "";
        int index = Mathf.Clamp(level - 1, 0, levelHints.Length - 1);
        return levelHints[index];
    }
}
