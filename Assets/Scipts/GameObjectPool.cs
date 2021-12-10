using UnityEngine;

public class GameObjectPool : MonoBehaviour,  IObjectPool<GameObject>
{
	[System.Serializable]
	public class Event : UnityEngine.Events.UnityEvent<GameObject> { }

	[SerializeField] private GameObject @default;

	[SerializeField] private bool fastDestroy = false;

	[Space]
	[SerializeField] private Event onReceiveGameObject = null;
	[SerializeField] private Event onReleaseGameObject = null;

	[SerializeField] private PoolParameters poolParameters;
	[SerializeField] private bool isSafe = false;

	private IObjectPool<GameObject> pool = null;
	public int Count => pool.Count;

	public int Capacity => pool.Capacity;

	public int MaxCapacity => pool.MaxCapacity;

	public int MinCapacity => pool.MinCapacity;

	private void Awake()
	{
		var min = pool != null ? pool.MinCapacity : 0;
		var max = pool != null ? pool.MaxCapacity : int.MaxValue;

		pool = isSafe ? new SafeObjectPool<GameObject>(CreateInstance, poolParameters)
							.SetOnClear(DestroyInstance) :
						new ObjectPool<GameObject>(CreateInstance, poolParameters)
							.SetOnClear(DestroyInstance);
	}

	private GameObject CreateInstance()
	{
		return Instantiate(@default, transform);
	}

	private void DestroyInstance(GameObject that)
	{
		if(fastDestroy) {
			DestroyImmediate(that);
		}
		else {
			Destroy(that);
		}
	}

	public void Clear()
	{
		pool.Clear();
	}

	public GameObject Receive()
	{
		var go = pool.Receive();
		if (onReceiveGameObject != null) {
			onReceiveGameObject.Invoke(go);
		}
		return go;
	}

	public void Release(GameObject that)
	{
		if (onReleaseGameObject != null) {
			onReleaseGameObject.Invoke(that);
		}
		pool.Release(that);
	}

	public IObjectPool<GameObject> SetMaximumCapacity(int that)
	{
		_ = pool.SetMaximumCapacity(that);
		return this;
	}

	public IObjectPool<GameObject> SetMinimumCapacity(int that)
	{
		_ = pool.SetMinimumCapacity(that);
		return this;
	}

	public bool TryReceive(out GameObject that)
	{
		return pool.TryReceive(out that);
	}

	public IObjectPool<GameObject> SetOnReceive(IObjectPool<GameObject>.OnReceive onReceive)
	{
		_ = pool.SetOnReceive(onReceive);
		return this;
	}

	public IObjectPool<GameObject> SetOnRelease(IObjectPool<GameObject>.OnRelease onRelease)
	{
		_ = pool.SetOnRelease(onRelease);
		return this;
	}

	public IObjectPool<GameObject> SetOnClear(IObjectPool<GameObject>.OnClear onClear)
	{
		_ = pool.SetOnClear(onClear);
		return this;
	}
}
