using UnityEngine;

public class ComponentPool<T> : MonoBehaviour, IObjectPool<T> where T : Component
{
	[SerializeField] private T @default;

	[SerializeField] private ObjectPool<T> pool = null;
	[SerializeField] private bool fastDestroy = false;

	public int Count => pool.Count;
	public int Capacity => pool.Capacity;
	public int MaxCapacity => pool.MaxCapacity;
	public int MinCapacity => pool.MinCapacity;

	private void Awake()
	{
		var min = pool != null ? pool.MinCapacity : 0;
		var max = pool != null ? pool.MaxCapacity : int.MaxValue;
		var isSafe = pool != null ? pool.IsSafe : false;

		pool = new ObjectPool<T>(CreateInstance, min, max, isSafe)
			.SetOnReceive(OnReceive)
			.SetOnRelease(OnRelease)
			.SetOnClear(DestroyInstance);
	}
	
	protected virtual T CreateInstance()
	{
		return Instantiate(@default, transform);
	}

	protected virtual void OnRelease(T that) { }

	protected virtual void OnReceive(T that) { }

	protected virtual void DestroyInstance(T that)
	{
		if (fastDestroy) {
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

	public T Receive()
	{
		var go = pool.Receive();
		return go;
	}

	public void Release(T that)
	{
		pool.Release(that);
	}

	public IObjectPool<T> SetMaximumCapacity(int that)
	{
		_ = pool.SetMaximumCapacity(that);
		return this;
	}

	public IObjectPool<T> SetMinimumCapacity(int that)
	{
		_ = pool.SetMinimumCapacity(that);
		return this;
	}

	public bool TryReceive(out T that)
	{
		return pool.TryReceive(out that);
	}
}
