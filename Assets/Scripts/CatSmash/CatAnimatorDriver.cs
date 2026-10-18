using UnityEngine;

public class CatAnimatorDriver : MonoBehaviour
{
    static readonly string[] Faces = { "Down", "Up", "Left", "Right" };

    Animator animator;
    string current;

    public int Facing { get; private set; }

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Use(RuntimeAnimatorController controller, bool moving, int facing)
    {
        if (animator == null) animator = GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        current = null;
        Show(moving, facing);
    }

    public void Show(bool moving, int facing)
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return;
        Facing = Mathf.Clamp(facing, 0, 3);
        animator.SetBool("Moving", moving);
        animator.SetInteger("Facing", Facing);
        string next = (moving ? "Walk" : "Idle") + Faces[Facing];
        if (next == current) return;
        animator.Play(next, 0, 0f);
        current = next;
    }
}
