using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
	[Header("Bomb Settings")]
    [SerializeField] private BombPool bombPool;
    [SerializeField] private Bomb.Parameters bombParameters;
    [Space]

    [Header("Death Settings")]
    [SerializeField] private float deathDuration = 1.0f;
    [SerializeField] private float deathBlinkRateStart = 0.05f;
    [SerializeField] private float deathBlinkRateEnd = 0.2f;
    [Space]

    [Header("Content Settings")]
    [SerializeField] private PlayerAudioLibrary playerAudioLibrary;
    
    private Rigidbody2D rigidBody;
    private Vector2 lookDirection = Vector2.down;
    private Vector2 walkDirection = Vector2.zero;
    private CircleCollider2D circleCollider = null;
    private Animator animator = null;
    private Coroutine deathRoutine = null;
    private AudioSource audioSource = null;
    private SpriteRenderer spriteRenderer;
    private SpriteResolver spriteResolver = null;
    private PlayerControlSettings playerControlSettings = null;
    private HashSet<Bomb> usedBombs = new HashSet<Bomb>();
    private bool isDying => deathRoutine != null;
    private Bomb.Parameters initialBombParameters;

    private void Awake()
    {
        initialBombParameters = bombParameters;
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        spriteResolver = GetComponent<SpriteResolver>();
    }

    void Update()
    {
        if(isDying) {
            return;
		}

        if (playerControlSettings != null) {
            playerControlSettings.HandleInput(out walkDirection, out bool planted);
            if (planted) {
                PlantBomb();
            }
        }
        else {
            FallBackDefaultInputHandling();
        }
    }

	private void OnEnable()
	{
        animator.enabled = true;

    }

	private void FallBackDefaultInputHandling()
    {
        walkDirection = Vector2.zero;

        var w = Input.GetKey(KeyCode.W);
        var s = Input.GetKey(KeyCode.S);
        var a = Input.GetKey(KeyCode.A);
        var d = Input.GetKey(KeyCode.D);

        walkDirection.y += w ? 1.0f : 0.0f;
        walkDirection.y -= s ? 1.0f : 0.0f;
        walkDirection.x += d ? 1.0f : 0.0f;
        walkDirection.x -= a ? 1.0f : 0.0f;
        walkDirection.Normalize();

        if (Input.GetKeyDown(KeyCode.Space)) {
            PlantBomb();
        }
    }

    private void PlantBomb()
    {
        var index = World.Instance.WorldToIndex(transform.position);
        if (!World.Instance.GetDataTile(index).HasBomb) {
            if (bombPool.TryReceive(out var bomb)) {
                usedBombs.Add(bomb);
                bomb.SetOff(index, bombParameters, () => {
                    usedBombs.Remove(bomb);
                    bombPool.Release(bomb);
                });
            }
        }
    }

    public void Setup()
    {
        foreach (var bomb in usedBombs) {
            bombPool.Release(bomb);

        }
        usedBombs.Clear();
        if(isDying) {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
            Color color = spriteRenderer.color;
            color.a = 1.0f;
            spriteRenderer.color = color;
        }
        bombParameters = initialBombParameters;
    }

    private void AnimatorSetVector2(string name, Vector2 that)
    {
        animator.SetFloat($"{name}X", that.x);
        animator.SetFloat($"{name}Y", that.y);
    }

    public void UpgradeBombCapacity(uint capacity)
    {
        bombPool.SetMaximumCapacity(bombPool.MaxCapacity + (int)capacity);
    }

    public void UpgradeBombRange(uint range)
    {
        bombParameters.SetRange(bombParameters.Range + (int)range);
    }

    public void SetSpriteLibrary(SpriteLibraryAsset libraryAsset)
    {
        spriteResolver.spriteLibrary.spriteLibraryAsset = libraryAsset;
        spriteResolver.spriteLibrary.RefreshSpriteResolvers();

        animator.SetBool("isWalking", false);
        AnimatorSetVector2("look", Vector2.down);
        AnimatorSetVector2("walk", Vector2.zero);
    }

    public void SetPlayerControlSettings(PlayerControlSettings playerControlSettingss)
    {
        this.playerControlSettings = playerControlSettingss;
	}

	private void FixedUpdate()
    {
        if (isDying) {
            return;
        }

        const float ONE_PIXEL = 1.0f;

        var index = World.Instance.WorldToIndex(rigidBody.position);
        var nextIndex = World.Instance.WorldToIndex(rigidBody.position + (walkDirection * (circleCollider.radius + ONE_PIXEL)));
        var currentHasBomb = World.Instance.GetDataTile(index) != null && World.Instance.GetDataTile(index).HasBomb;
        var nextHasBomb = World.Instance.GetDataTile(nextIndex) != null && World.Instance.GetDataTile(nextIndex).HasBomb;

        if (currentHasBomb || !nextHasBomb) {
            rigidBody.position += walkDirection;
		}

        bool isWalking = walkDirection.sqrMagnitude > 0.0f; 
        if(isWalking == true) {
            lookDirection = walkDirection;
		}

        animator.SetBool("isWalking", isWalking);
        AnimatorSetVector2("look", lookDirection);
        AnimatorSetVector2("walk", walkDirection);


        var data = World.Instance.GetDataTile(index);
        if (data.HasExplosion) {
            Die();
		}
        if(data.TryGetUpgrade(out var upgrade)) {
            upgrade.ApplyUpgrade(this);
            playerAudioLibrary.PlayOnUpgrade(audioSource);
            upgrade.Despawn();
		}
    }

    private void Die()
    {
        if (deathRoutine == null) {
            deathRoutine = StartCoroutine(Dying());
        }
    }

    private IEnumerator Dying()
    {
        float t = 0.0f;
        float timeIncreasse = 1.0f / deathDuration;
        bool isVisible = false;
        var color = spriteRenderer.color;

        animator.SetBool("isWalking", false);
        AnimatorSetVector2("look", Vector2.down);
        AnimatorSetVector2("walk", Vector2.zero);
        
        playerAudioLibrary.PlayOnDeath(audioSource);

        while (t < 1.0f) {
            color.a = isVisible ? 1.0f : 0.0f;
            spriteRenderer.color = color;
            isVisible = !isVisible;
            var rate = Mathf.SmoothStep(deathBlinkRateStart, deathBlinkRateEnd, t);
            t += rate * timeIncreasse;
            yield return new WaitForSeconds(rate);
		}

        color.a = 1.0f;
        spriteRenderer.color = color;
        gameObject.SetActive(false);
        deathRoutine = null;

        Service<PlayerManager>.Instance.Kill(this);
    }
}
