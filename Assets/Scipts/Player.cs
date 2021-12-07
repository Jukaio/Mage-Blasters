using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public interface Executable
{
    void Execute();
}

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [SerializeField] private Bomber bomber;
    [SerializeField] private Death death;

    [Header("Content Settings")]
    [SerializeField] private PlayerAudioLibrary playerAudioLibrary;
    
    public Bomber Bomber => bomber;

    private Rigidbody2D rigidBody;
    private Vector2 lookDirection = Vector2.down;
    private Vector2 walkDirection = Vector2.zero;
    private CircleCollider2D circleCollider = null;
    private Animator animator = null;
    private SpriteResolver spriteResolver = null;
    private PlayerControlSettings playerControlSettings = null;

    private List<SubComponent> subComponents = new List<SubComponent>();


    private void Awake()
    {
        subComponents.Add(bomber);
        subComponents.Add(death);

        foreach(var component in subComponents) {
            component.Construct(this);
		}

        foreach (var component in subComponents) {
            component.OnAwake();
        }

        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        spriteResolver = GetComponent<SpriteResolver>();
    }

	private void Start()
	{
        foreach (var component in subComponents) {
            component.OnStart();
        }
    }

	private void OnEnable()
	{
        foreach (var component in subComponents) {
            component.OnEnable();
        }
        animator.enabled = true;
    }

	private void OnDisable()
	{
        foreach (var component in subComponents) {
            component.OnDisable();
        }
    }

	void Update()
    {
        if (death.IsDying) {
            return;
		}

        if (playerControlSettings != null) {
            playerControlSettings.HandleInput(out walkDirection, out bool planted);
            if (planted) {
                bomber.Execute();
            }
        }
        else {
            FallBackDefaultInputHandling();
        }
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
            bomber.Execute();
        }
    }

    public void Setup()
    {
        foreach (var component in subComponents) {
            component.OnSetup();
        }
    }

    public void SetSpriteLibrary(SpriteLibraryAsset libraryAsset)
    {
        spriteResolver.spriteLibrary.spriteLibraryAsset = libraryAsset;
        spriteResolver.spriteLibrary.RefreshSpriteResolvers();

        animator.SetBool("isWalking", false);
        animator.SetVector2("look", Vector2.down);
        animator.SetVector2("walk", Vector2.zero);
    }

    public void SetPlayerControlSettings(PlayerControlSettings playerControlSettingss)
    {
        this.playerControlSettings = playerControlSettingss;
	}

	private void FixedUpdate()
    {
        if (death.IsDying) {
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
        animator.SetVector2("look", lookDirection);
        animator.SetVector2("walk", walkDirection);


        var data = World.Instance.GetDataTile(index);
        if (data.HasExplosion) {
            death.Execute();
		}
        if(data.TryGetUpgrade(out var upgrade)) {
            upgrade.ApplyUpgrade(this);
            upgrade.Despawn();
		}
    }
}
