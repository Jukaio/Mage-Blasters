using UnityEngine;

public static class AnimatorExtension
{
    public static void SetVector2(this Animator animator, string name, Vector2 that)
    {
        animator.SetFloat($"{name}X", that.x);
        animator.SetFloat($"{name}Y", that.y);
    }
}
