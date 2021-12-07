using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Bomb : MonoBehaviour
{
	private Stack<Blast> usedBlasts = new Stack<Blast>();

	public static class Observer
	{
		public delegate void OnExplodeAction(Bomb bomb);	
		public static event OnExplodeAction onExplode;

		public static void OnExplode(Bomb context)
		{
			onExplode?.Invoke(context);
		}
	}

	[System.Serializable]
	public struct Parameters
	{
		[SerializeField] private float tickTime;
		[SerializeField] private int range;
		[SerializeField] private float blastDuration;
		[SerializeField] private float bombChainDelay;

		public float TickTime => tickTime;
		public int Range => range;
		public float BlastDuration => blastDuration;
		public float BombChainDelay => bombChainDelay;

		public Parameters(float tickTime, int range, float blastDuration, float bombChainDelay)
		{
			this.tickTime = tickTime;
			this.range = range;
			this.blastDuration = blastDuration;
			this.bombChainDelay = bombChainDelay;
		}
		public void SetTickTime(float time)
		{
			tickTime = time;
		}
		public void SetRange(int range)
		{
			this.range = range;
		}
		public void SetBlastDuration(float duration)
		{
			blastDuration = duration;
		}
		public void SetBombChainDelay(float duration)
		{
			bombChainDelay = duration;
		}
	}

	private static readonly Vector2Int[] directions = new Vector2Int[4]
	{
		Vector2Int.up,
		Vector2Int.right,
		Vector2Int.down,
		Vector2Int.left
	};

	public delegate void OnExplosionEndAction();

	[SerializeField] private BlastPool blastPool = null;

	// This could get grouped up nicely - but nah. Just make sure to keep the arrays the same length
	private const string CENTER_BLAST = "Center";
	private const string VERTICAL_BLAST = "Vertical";
	private const string HORIZONTAL_BLAST = "Horizontal";
	private const string VERTICAL_END_BLAST = "VerticalEnd";
	private const string HORIZONTAL_END_BLAST = "HorizontalEnd";

	private string category = "Normal";
	private SpriteRenderer spriteRenderer = null;
	private SpriteResolver spriteResolver = null;
	private AudioSource audioSource = null;
	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteResolver = GetComponent<SpriteResolver>();
		audioSource = GetComponent<AudioSource>();
		spriteResolver.SetCategoryAndLabel(category, (0).ToString());
	}

	public void OnRelease()
	{
		while(usedBlasts.Count > 0) {
			blastPool.Release(usedBlasts.Pop());
		}
		StopAllCoroutines();
	}

	public void SetOff(Vector2 at, Parameters param, OnExplosionEndAction onExplosionEnd)
	{
		SetOff(World.Instance.WorldToIndex(at), param, onExplosionEnd);
	}

	public void SetOff(Vector2Int at, Parameters param, OnExplosionEndAction onExplosionEnd)
	{
		transform.parent = World.Instance.transform;
		transform.position = World.Instance.IndexToWorld(at);
		World.Instance.GetDataTile(at).HasBomb = true;
		StartCoroutine(Ticking(param, onExplosionEnd));
	}


	private IEnumerator Ticking(Parameters param, OnExplosionEndAction onExplosionEnd)
	{
		var t = 0.0f;

		// This is just a very horrible way to get a simple count for clamping/wrapping/etc. sprites for animation
		var count = spriteResolver.spriteLibrary.spriteLibraryAsset.GetCategoryLabelNames(category).Count();
		var at = World.Instance.WorldToIndex(this);
		var timeIncreaseRate = 1.0f / param.TickTime;
		while (t < 1.0f) {
			var ease = Mathf.SmoothStep(0.0f, 1.0f, Mathf.PingPong(t * 2.0f, 1.0f));
			var index = (int)(count * ease);
			spriteResolver.SetCategoryAndLabel(category, index.ToString());

			if(World.Instance.GetDataTile(at).HasExplosion) {
				break;
			}

			t += Time.deltaTime * timeIncreaseRate;
			yield return null;
		}

		Explode(param, onExplosionEnd);
	}

	private void Explode(Parameters param, OnExplosionEndAction onExplosionEnd)
	{
		Observer.OnExplode(this);

		StartCoroutine(Exploding(param, onExplosionEnd));	
	}

	private IEnumerator Exploding(Parameters param, OnExplosionEndAction onExplosionEnd)
	{
		spriteRenderer.sprite = null;
		usedBlasts.Clear();

		var center = blastPool.Receive();
		var origin = World.Instance.WorldToIndex(this);
		center.PlaceAt(origin);
		center.SetSpriteCategory(CENTER_BLAST, false, false);

		usedBlasts.Push(center);

		foreach(var direction in directions) {
			for (int i = 1; i <= param.Range; i++) {
				var index = GetNeighbourIndex(direction, i);

				var removedDestructible = World.Instance.TryRemoveDestructible(index, param.BlastDuration);
				if (removedDestructible) {
					// Perfect spot to chain bombs differently
					break;
				}
				else if (World.Instance.IsBlocked(index)) {
					break;
				}

				if(World.Instance.GetDataTile(index).TryGetUpgrade(out var upgrade)) {
					upgrade.Despawn();
					World.Instance.MeltAtIndex(index, param.BlastDuration);
				}

				var nextIsBlocked = World.Instance.IsBlocked(index + direction) || removedDestructible;

				var blast = blastPool.Receive();
				blast.SetSpriteCategory(ResolveBlastSprite(direction, nextIsBlocked, i, param.Range),
										ResolveBlastSpriteFlipX(direction, nextIsBlocked, i, param.Range),
										ResolveBlastSpriteFlipY(direction, nextIsBlocked, i, param.Range));
				blast.PlaceAt(index);
				usedBlasts.Push(blast);
			}
		}

		audioSource.Play();

		yield return AnimateBlast(param.BlastDuration, usedBlasts);

		while (usedBlasts.Count > 0) {
			var blast = usedBlasts.Pop();
			blast.Deactivate();
			blastPool.Release(blast);
		}

		World.Instance.GetDataTile(origin).HasBomb = false;

		yield return new WaitWhile(() => audioSource.isPlaying);

		if (onExplosionEnd != null) {
			onExplosionEnd.Invoke();
		}

	}

	private IEnumerator AnimateBlast(float blastDuration, IEnumerable<Blast> usedBlasts)
	{
		var timeIncreaseRate = 1 / blastDuration;

		var t = 0.0f;
		while (t < 1.0f) {

			foreach (var blast in usedBlasts) {
				var ease = Mathf.SmoothStep(0.0f, 1.0f, Mathf.PingPong(t * 2, 1.0f));
				blast.Animate(ease);
			}

			t += Time.deltaTime * timeIncreaseRate;
			yield return null;
		}
	}

	private bool ResolveBlastSpriteFlipX(Vector2Int direction, bool nextIsBlocked, int current, int max)
	{
		if (direction == Vector2Int.zero) {
			return false;
		}
		else if (current != max && !nextIsBlocked) {
			return false;
		}
		else if(direction.x < 0) {
			return true;
		}
		else {
			return false;
		}
	}

	private bool ResolveBlastSpriteFlipY(Vector2Int direction, bool nextIsBlocked, int current, int max)
	{
		if (direction == Vector2Int.zero) {
			return false;
		}
		else if (current != max && !nextIsBlocked) {
			return false;
		}
		else if (direction.y < 0) {
			return true;
		}
		else {
			return false;
		}
	}

	private string ResolveBlastSprite(Vector2Int direction, bool nextIsBlocked, int current, int max)
	{
		if (direction == Vector2Int.zero) {
			return CENTER_BLAST;
		}
		else if (direction.x != 0 && direction.y != 0) {
			throw new System.InvalidOperationException("Diagonal Blast Sprites do not exit");
		}
		else if (direction.x != 0) {
			if(current == max || nextIsBlocked) {
				return HORIZONTAL_END_BLAST;
			}
			else {
				return HORIZONTAL_BLAST;
			}
		}
		else {
			if (current == max || nextIsBlocked) {
				return VERTICAL_END_BLAST;
			}
			else {
				return VERTICAL_BLAST;
			}
		}
	}

	private Vector2Int GetNeighbourIndex(Vector2Int dir, int index)
	{
		return World.Instance.WorldToIndex(this) + (dir * index);
	}
}
