using System.Collections;
using UnityEngine;

[System.Serializable]
public class Death : SubComponent, Executable
{
    [Header("Death Settings")]
    [SerializeField] private float deathDuration = 1.0f;
    [SerializeField] private float deathBlinkRateStart = 0.05f;
    [SerializeField] private float deathBlinkRateEnd = 0.2f;
    [SerializeField] private PlayerAudioLibrary playerAudioLibrary;
    [Space]

    private AudioSource audioSource;
    private Animator animator;
    private SpriteRenderer renderer;

    private Coroutine routine = null;

    public bool IsDying => routine != null;

    public override void OnAwake()
    {
        renderer = player.GetComponent<SpriteRenderer>();
        animator = player.GetComponent<Animator>();
        audioSource = player.GetComponent<AudioSource>();
    }

    public override void OnSetup()
    {
        if (IsDying) {
            player.StopCoroutine(routine);
            routine = null;
            Color color = renderer.color;
            color.a = 1.0f;
            renderer.color = color;
        }
    }

    public void Execute()
    {
        if (routine == null) {
            routine = player.StartCoroutine(Dying());
        }
    }

    private IEnumerator Dying()
    {
        float t = 0.0f;
        float timeIncreasse = 1.0f / deathDuration;
        bool isVisible = false;
        var color = renderer.color;

        animator.SetBool("isWalking", false);
        animator.SetVector2("look", Vector2.down);
        animator.SetVector2("walk", Vector2.zero);

        playerAudioLibrary.PlayOnDeath(audioSource);

        while (t < 1.0f) {
            color.a = isVisible ? 1.0f : 0.0f;
            renderer.color = color;
            isVisible = !isVisible;
            var rate = Mathf.SmoothStep(deathBlinkRateStart, deathBlinkRateEnd, t);
            t += rate * timeIncreasse;
            yield return new WaitForSeconds(rate);
        }

        color.a = 1.0f;
        renderer.color = color;
        player.gameObject.SetActive(false);
        routine = null;

        Service<PlayerManager>.Instance.Kill(player);
    }
}
