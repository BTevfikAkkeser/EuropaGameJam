using UnityEngine;

[CreateAssetMenu(menuName = "Cat Smash/Cat Skin")]
public class CatSkin : ScriptableObject
{
    public string displayName;
    public RuntimeAnimatorController controller;
    public bool previewWalks;
}
