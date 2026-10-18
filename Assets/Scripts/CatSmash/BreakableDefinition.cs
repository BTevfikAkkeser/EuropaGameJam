using UnityEngine;

[CreateAssetMenu(menuName = "Cat Smash/Breakable", fileName = "Breakable")]
public class BreakableDefinition : ScriptableObject
{
    public string displayName;
    [Tooltip("0 small, 1 medium, 2 large, 3 huge. Required level = tier + 1.")]
    public int tier;
    public int xp = 12;
    public int hp = 1;
    public float mass = 0.5f;
    [Tooltip("Prefab root scale. Rebuild with CatSmash/Bake Scene after changing it.")]
    public float worldSize = 0.5f;
    public bool round;
    public Color debrisA = Color.white;
    public Color debrisB = Color.gray;
    public Color debrisC = Color.black;
}
