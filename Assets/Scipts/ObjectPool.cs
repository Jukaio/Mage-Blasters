using System;
using System.Collections.Generic;

#if UNITY_EDITOR || UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

public interface IPool<T> where  T : class
{
	void Clear();
	T Receive();
	bool TryReceive(out T that);
	void Release(T that);
}

public interface IObjectPool<T> : IPool<T> where T : class
{
	int Count { get; }
	int Capacity { get; }
	public int MaxCapacity { get; }
	public int MinCapacity { get; }
	IObjectPool<T> SetMaximumCapacity(int that);
	IObjectPool<T> SetMinimumCapacity(int that);
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
public class ObjectPool<T> : IObjectPool<T> where T : class
{
	public delegate T OnCreate();
	public delegate void OnReceive(T that);
	public delegate void OnRelease(T that);
	public delegate void OnClear(T that);

	private OnCreate onCreate = null;
	private OnReceive onReceive = null;
	private OnRelease onRelease = null;
	private OnClear onClear = null;

	private Stack<T> pool = new Stack<T>();
	private HashSet<T> safetyCheck = null;

	private int used = 0;
	private int capaciy = 0;

#if UNITY_EDITOR || UNITY_5_3_OR_NEWER
	[SerializeField] private int minCapacity = 0;
	[SerializeField] private int maxCapacity = int.MaxValue;
	[SerializeField] private bool isSafe = false;
#else
	private int minCapacity = 0;
	private int maxCapacity = int.MaxValue;
	private bool isSafe = false;
#endif

	public int Count => pool.Count;
	public int Capacity => capaciy;
	public int MaxCapacity => maxCapacity;
	public int MinCapacity => minCapacity;
	public bool IsSafe => isSafe;

	public ObjectPool(OnCreate onCreate, int minCount = 0, int maxCount = int.MaxValue, bool isSafe = false)
	{
		if(minCount > maxCount) {
			throw new PoolArgumentException($"Cannot have minimum count that is bigger than maximum count in {this.ToString()}!");
		}
		
		if(isSafe == true) {
			safetyCheck = new HashSet<T>();
		}
		
		this.onCreate = onCreate;
		this.minCapacity = minCount;
		this.maxCapacity = maxCount;
		this.isSafe = isSafe;
		CacheNewItems(minCount);
	}

	private void CacheNewItems(int count)
	{
		for (int i = 0; i < count; i++) {
			var item = onCreate();
			onRelease(item);
			if (safetyCheck != null) {
				safetyCheck.Add(item);
			}
			pool.Push(item);
			capaciy += 1;
		}
	}

	public ObjectPool<T> SetOnReceive(OnReceive onReceive)
	{
		this.onReceive = onReceive;
		return this;
	}
	public ObjectPool<T> SetOnRelease(OnRelease onRelease)
	{
		this.onRelease = onRelease;
		return this;
	}
	public ObjectPool<T> SetOnClear(OnClear onClear)
	{
		this.onClear = onClear;
		return this;
	}

	public T Receive()
	{
		used += 1;
		if (pool.TryPop(out var item)) {
			if (onReceive != null) {
				onReceive(item);
			}
			return item;
		}

		const int added = 1;
		if (capaciy + added > maxCapacity) {
			throw new PoolOverflowException($"Not enough space in {this.ToString()}!");
		}
		CacheNewItems(added); // Hardcoded 1 for now

		item = pool.Pop();
		onReceive(item);
		return item;
	}

	public void Release(T that) 
	{
		if (safetyCheck != null && !safetyCheck.Contains(that)) {
			throw new PoolIllegalItemException($"{that.ToString()} does not belong to {this.ToString()}");
		}

		used -= 1;
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

		if (safetyCheck != null) {
			safetyCheck?.Clear();
		}

		capaciy = 0;
		CacheNewItems(minCapacity);
	}

	public IObjectPool<T> SetMaximumCapacity(int that)
	{
		maxCapacity = that;
		return this;
	}

	public IObjectPool<T> SetMinimumCapacity(int that)
	{
		minCapacity = that;
		return this;
	}

	public bool TryReceive(out T that)
	{
		const int added = 1;
		if (pool.TryPop(out that)) {
			used += 1;
			if (onReceive != null) {
				onReceive(that);
			}
			return true;
		}
		else if (capaciy + added <= maxCapacity) {
			used += 1;
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

