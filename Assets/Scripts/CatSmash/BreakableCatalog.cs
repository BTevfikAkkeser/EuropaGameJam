using UnityEngine;

[CreateAssetMenu(menuName = "Cat Smash/Catalog", fileName = "BreakableCatalog")]
public class BreakableCatalog : ScriptableObject
{
    public BreakableDefinition[] items = System.Array.Empty<BreakableDefinition>();
}
