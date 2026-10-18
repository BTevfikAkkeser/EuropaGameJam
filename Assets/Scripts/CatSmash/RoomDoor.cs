using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [SerializeField] Vector2 exit;
    [SerializeField] Vector3 cameraHome;

    static float readyAt;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < readyAt) return;
        if (CatGame.I == null || !CatGame.I.Playing || CatGame.I.Won) return;
        var cat = other.GetComponent<CatPlayer>();
        if (cat == null) return;
        readyAt = Time.time + 0.7f;
        cat.Place(exit);
        CatGame.I.MoveCamera(cameraHome);
    }
}
