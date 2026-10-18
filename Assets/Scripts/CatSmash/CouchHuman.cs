using UnityEngine;

public class CouchHuman : MonoBehaviour
{
    [SerializeField] Transform hand;
    [SerializeField] Material calmMaterial;
    [SerializeField] Material smileMaterial;
    [SerializeField] Vector3 lapOffset = new Vector3(0.38f, 0.08f, 0f);
    [SerializeField] Vector3 petOffset = new Vector3(0.12f, 0.95f, 0f);

    Renderer face;
    bool petting;

    public void Bind(Transform handTransform, Material calm, Material smile)
    {
        hand = handTransform;
        calmMaterial = calm;
        smileMaterial = smile;
    }

    public void SetHand(Transform handTransform)
    {
        hand = handTransform;
    }

    void Awake()
    {
        var visual = transform.Find("Visual");
        if (visual != null) face = visual.GetComponent<Renderer>();
    }

    public bool InRange(Vector2 catPosition)
    {
        float range = CatGame.I != null && CatGame.I.Balance != null ? CatGame.I.Balance.petRange : 1.8f;
        return Vector2.Distance(catPosition, transform.position) < range;
    }

    public void StartPet()
    {
        petting = true;
        Swap(smileMaterial);
    }

    public void StopPet()
    {
        petting = false;
        Swap(calmMaterial);
    }

    void Swap(Material material)
    {
        if (face != null && material != null)
            face.sharedMaterial = material;
    }

    void Update()
    {
        if (hand == null) return;
        Vector3 target = transform.position + (petting ? petOffset : lapOffset);
        float follow = 1f - Mathf.Exp(-12f * Time.deltaTime);
        var next = Vector3.Lerp(hand.position, target, follow);
        if (petting)
            next += new Vector3(Mathf.Sin(Time.time * 16f) * 0.05f, Mathf.Sin(Time.time * 18f) * 0.07f, 0f);
        next.z = 0f;
        hand.position = next;
        hand.rotation = petting
            ? Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 16f) * 22f)
            : Quaternion.identity;
    }
}
