using UnityEngine;

public abstract class Upgrade : MonoBehaviour
{
	public delegate void CleanUp(Upgrade upgrade);
	private CleanUp cleanUp = null;

	public abstract void ApplyUpgrade(Player player);

	public void Spawn(Vector2Int at, CleanUp cleanUp)
	{
		World.Instance.GetDataTile(at).Upgrade = this;
		transform.position = World.Instance.IndexToWorld(at);

		this.cleanUp = cleanUp;
	}

	public void Despawn()
	{
		var index = World.Instance.WorldToIndex(transform.position);
		World.Instance.GetDataTile(index).Upgrade = null;

		if(cleanUp != null) {
			cleanUp.Invoke(this);
		}
	}
}
