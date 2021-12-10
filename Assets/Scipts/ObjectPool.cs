using System;
using System.Collections.Generic;

#if UNITY_EDITOR || UNITY_5_3_OR_NEWER
using UnityEngine;
#endif


public interface IPool<T> where T : class
{
	void Clear();
	T Receive();
	bool TryReceive(out T that);
	void Release(T that);
}

public interface IObjectPool<T> : IPool<T> where T : class
{
	public delegate void OnReceive(T that);
	public delegate void OnRelease(T that);
	public delegate void OnClear(T that);

	int Count { get; }
	int Capacity { get; }
	public int MaxCapacity { get; }
	public int MinCapacity { get; }
	IObjectPool<T> SetMaximumCapacity(int that);
	IObjectPool<T> SetMinimumCapacity(int that);
	IObjectPool<T> SetOnReceive(OnReceive onReceive);
	IObjectPool<T> SetOnRelease(OnRelease onRelease);
	IObjectPool<T> SetOnClear(OnClear onClear);
}

public class PoolOverflowException : Exception
{
	public PoolOverflowException(string message) : base(message) { }
}
public class PoolArgumentException : Exception
{
	public PoolArgumentException(string message) : base(message) { }
}
public class PoolIllegalItemException : Exception
{
	public PoolIllegalItemException(string message) : base(message) { }
}

[Serializable]
public struct PoolParameters
{
	[SerializeField] public int minCapacity;
	[SerializeField] public int maxCapacity;

	public PoolParameters(int min, int max)
	{
		this.minCapacity = min;
		this.maxCapacity = max;
	}

	public override string ToString()
	{
		return $"Minimum: {minCapacity} - Maximum: {maxCapacity}";
	}

	public bool IsValid => minCapacity <= maxCapacity;
}

[Serializable]
public class ObjectPool<T> : IObjectPool<T> where T : class
{
	public delegate T OnCreate();

	private OnCreate onCreate = null;
	private IObjectPool<T>.OnReceive onReceive = null;
	private IObjectPool<T>.OnRelease onRelease = null;
	private IObjectPool<T>.OnClear onClear = null;

	private Stack<T> pool = new Stack<T>();

	private int capaciy = 0;
#if UNITY_EDITOR || UNITY_5_3_OR_NEWER
	[SerializeField] private PoolParameters parameters;
#else
	private PoolParameters parameters;
#endif

	public int Count => pool.Count;
	public int Capacity => capaciy;
	public int MaxCapacity => parameters.maxCapacity;
	public int MinCapacity => parameters.minCapacity;

	public ObjectPool(OnCreate onCreate, PoolParameters poolParameters)
	{
		if(!poolParameters.IsValid) {
			throw new PoolArgumentException($"Cannot have minimum count that is bigger than maximum count in {this.ToString()}!");
		}
		
		this.onCreate = onCreate;
		parameters = poolParameters;
		CacheNewItems(parameters.minCapacity);
	}

	private void CacheNewItems(int count)
	{
		for (int i = 0; i < count; i++) {
			var item = onCreate();
			onRelease(item);
			pool.Push(item);
			capaciy += 1;
		}
	}

	public IObjectPool<T> SetOnReceive(IObjectPool<T>.OnReceive onReceive)
	{
		this.onReceive = onReceive;
		return this;
	}
	public IObjectPool<T> SetOnRelease(IObjectPool<T>.OnRelease onRelease)
	{
		this.onRelease = onRelease;
		return this;
	}
	public IObjectPool<T> SetOnClear(IObjectPool<T>.OnClear onClear)
	{
		this.onClear = onClear;
		return this;
	}

	public T Receive()
	{
		if (pool.TryPop(out var item)) {
			if (onReceive != null) {
				onReceive(item);
			}
			return item;
		}

		const int added = 1;
		if (capaciy + added > parameters.maxCapacity) {
			throw new PoolOverflowException($"Not enough space in {this.ToString()}!");
		}
		CacheNewItems(added); // Hardcoded 1 for now

		item = pool.Pop();
		if (onReceive != null) {
			onReceive(item);
		}
		return item;
	}

	public void Release(T that) 
	{
		if (onRelease != null) {
			onRelease(that);
		}
		pool.Push(that);
	}

	public void Clear()
	{
		if(onClear != null) {
			foreach(var element in pool) {
				if (onClear != null) {
					onClear(element);
				}
			}
		}
		pool.Clear();

		capaciy = 0;
		CacheNewItems(parameters.minCapacity);
	}

	public IObjectPool<T> SetMaximumCapacity(int that)
	{
		parameters.maxCapacity = that;
		return this;
	}

	public IObjectPool<T> SetMinimumCapacity(int that)
	{
		parameters.minCapacity = that;
		return this;
	}

	public bool TryReceive(out T that)
	{
		const int added = 1;
		if (pool.TryPop(out that)) {
			if (onReceive != null) {
				onReceive(that);
			}
			return true;
		}
		else if (capaciy + added <= parameters.maxCapacity) {
			CacheNewItems(added); // Hardcoded 1 for now
			that = pool.Pop();
			if (onReceive != null) {
				onReceive(that);
			}
			return true;
		}
		that = null;
		return false;
	}
}

public class SafeObjectPool<T> : IObjectPool<T> where T : class
{
	private ObjectPool<T> pool = null;
	private HashSet<T> lookup = null;

	public int Count => pool.Count;

	public int Capacity => pool.Capacity;

	public int MaxCapacity => pool.MaxCapacity;

	public int MinCapacity => pool.MinCapacity;

	public SafeObjectPool(ObjectPool<T>.OnCreate onCreate, PoolParameters poolParameters)
	{
		pool = new ObjectPool<T>(onCreate, poolParameters);
		lookup = new HashSet<T>();
	}

	public void Clear()
	{
		pool.Clear();	
	}

	public T Receive()
	{
		var item = pool.Receive();
		lookup.Add(item);
		return item;
	}

	public void Release(T that)
	{
		if (!lookup.Contains(that)) {
			throw new PoolIllegalItemException($"{that} does not belong to {this}");
		}

		lookup.Remove(that);
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
		if(pool.TryReceive(out that)) {
			lookup.Add(that);
			return true;
		}
		return false;
	}

	public IObjectPool<T> SetOnReceive(IObjectPool<T>.OnReceive onReceive)
	{
		_ = pool.SetOnReceive(onReceive);
		return this;
	}

	public IObjectPool<T> SetOnRelease(IObjectPool<T>.OnRelease onRelease)
	{
		_ = pool.SetOnRelease(onRelease);
		return this;
	}

	public IObjectPool<T> SetOnClear(IObjectPool<T>.OnClear onClear)
	{
		_ = pool.SetOnClear(onClear);
		return this;
	}
}


