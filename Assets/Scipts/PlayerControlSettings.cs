using UnityEngine;
#if UNITY_EDITOR
#endif

[CreateAssetMenu(fileName = "PlayerControlSettings", menuName = "ScriptableObjects/Player Control Settings", order = 1)]
public class PlayerControlSettings : ScriptableObject
{
    [SerializeField] private KeyCode up;
    [SerializeField] private KeyCode down;
    [SerializeField] private KeyCode right;
    [SerializeField] private KeyCode left;
    [SerializeField] private KeyCode plantBomb;

    public void HandleInput(out Vector2 direction, out bool isBombPlanted)
    {
        direction = Vector2.zero;
        var w = Input.GetKey(up);
        var s = Input.GetKey(down);
        var a = Input.GetKey(left);
        var d = Input.GetKey(right);

        direction.y += w ? 1.0f : 0.0f;
        direction.y -= s ? 1.0f : 0.0f;
        direction.x += d ? 1.0f : 0.0f;
        direction.x -= a ? 1.0f : 0.0f;
        direction.Normalize();
        isBombPlanted = Input.GetKeyDown(plantBomb);
    }
}
