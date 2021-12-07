using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class World : SingletonMonobehaviour<World>
{
	public class DataTile : TileBase
	{
		public bool HasBomb { get; set; }
		public bool HasExplosion { get; set; }
		public Upgrade Upgrade { get; set; }

		public bool TryGetUpgrade(out Upgrade upgrade)
		{
			upgrade = Upgrade;
			if(Upgrade == null) {
				return false;
			}
			return true;
		}
	}

	[SerializeField] private Tilemap prefabWalls;
	[SerializeField] private Tilemap prefabDestructibles;

	[SerializeField] private Tilemap ground;
	[SerializeField] private Tilemap data;

	[SerializeField] private MeltPool meltPool;
	[SerializeField] private RandomUpgradePool randomUpgradePool;

	private Tilemap walls;
	private Tilemap destructibles;

	public Vector2Int Origin => Vector2Int.Max((Vector2Int)ground.origin, (Vector2Int)prefabWalls.origin);
	public Vector2Int Size => Vector2Int.Max((Vector2Int)ground.size, (Vector2Int)prefabWalls.size);

	private Grid cachedGrid = null;
	private Grid Grid => cachedGrid != null ? cachedGrid : cachedGrid = GetComponent<Grid>();

	private void Awake()
	{
		for (int y = Origin.y; y < Size.y; y++) {
			for (int x = Origin.x; x < Size.x; x++) {
				data.SetTile(new Vector3Int(x, y, 0), DataTile.CreateInstance<DataTile>());
			}
		}
	}

	private void OnEnable()
	{
		Setup();
	}

	private void OnDisable()
	{
		Service<PlayerManager>.Instance.SetAllPlayersActive(false);
	}

	public void Setup()
	{
		for (int y = Origin.y; y < Size.y; y++) {
			for (int x = Origin.x; x < Size.x; x++) {
				var tile = data.GetTile(new Vector3Int(x, y, 0)) as DataTile;
				tile.HasBomb = false;
				tile.HasExplosion = false;
				if(tile.TryGetUpgrade(out var upgrade)) {
					upgrade.Despawn();
				}
			}
		}

		if(walls != null) {
			DestroyImmediate(walls.gameObject);
		}
		if (destructibles != null) {
			DestroyImmediate(destructibles.gameObject);
		}

		walls = Instantiate(prefabWalls, transform);
		destructibles = Instantiate(prefabDestructibles, transform);

		Service<PlayerManager>.Instance.Setup();
		Service<PlayerManager>.Instance.SetAllPlayersActive(true);
	}

	public Vector2 IndexToWorld(Vector2Int index)
	{
		return Grid.GetCellCenterWorld((Vector3Int)index);
	}

	public Vector2Int WorldToIndex(Vector2 position)
	{
		return (Vector2Int)Grid.WorldToCell(position);
	}

	public Vector2Int WorldToIndex(Component component)
	{
		return WorldToIndex(component.transform.position);
	}

	public void ForEachTile(System.Action<Vector2Int, TileBase> onAction)
	{
		for (int y = walls.origin.y; y < walls.size.y; y++) {
			for (int x = walls.origin.x; x < walls.size.x; x++) {
				var index = new Vector3Int(x, y, 0);
				onAction((Vector2Int)index, data.GetTile(index));
			}
		}
	}

	public bool IsBlocked(Vector2Int index)
	{
		var tile = walls.GetTile((Vector3Int)index);
		return tile != null;
	}

	public bool TryRemoveDestructible(Vector2Int index, float duration) 
	{
		if(HasDestructible(index)) {
			RemoveDestructible(index, duration);
			return true;
		}
		return false;
	}

	public bool HasDestructible(Vector2Int index)
	{
		return destructibles.GetTile((Vector3Int)index) != null;
	}

	private void RemoveDestructible(Vector2Int index, float duration)
	{
		StartCoroutine(AnimateMelting(index, meltPool.Receive(), duration, () =>
		{
			destructibles.SetTile((Vector3Int)index, null);
		}));
	}

	public void MeltAtIndex(Vector2Int index, float duration)
	{
		StartCoroutine(AnimateMelting(index, meltPool.Receive(), duration));
	}

	private IEnumerator AnimateMelting(Vector2Int index, Melt melt, float duration, System.Action onEnd = null)
	{

		melt.transform.position = IndexToWorld(index);
		var timeIncreaseRate = 1 / duration; // hardcoded duration rhs

		var t = 0.0f;
		while (t < 1.0f) {
			melt.Animate(Mathf.SmoothStep(0.0f, 1.0f, t));
			t += Time.deltaTime * timeIncreaseRate;
			yield return null;
		}

		if(randomUpgradePool.TryReceive(out var upgrade)) {
			upgrade.Spawn(index, CleanUpUpgrade);
		}

		meltPool.Release(melt);

		if (onEnd != null) {
			onEnd.Invoke();
		}
	}

	private void CleanUpUpgrade(Upgrade upgrade)
	{
		randomUpgradePool.Release(upgrade);
	}

	public DataTile GetDataTile(Vector2Int index)
	{
		return data.GetTile<DataTile>((Vector3Int)index);
	}

	public bool IsEmpty(Vector2Int index)
	{
		return !IsBlocked(index);
	}
}
