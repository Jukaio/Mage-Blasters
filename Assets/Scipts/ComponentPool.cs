using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
#endif

public class ComponentPool<T> : MonoBehaviour, IObjectPool<T> where T : Component
{
	[SerializeField] private T @default;

	[SerializeField] private PoolParameters poolParameters;
	[SerializeField] private bool fastDestroy = false;
	[SerializeField] private bool isSafe = false;

	private IObjectPool<T> pool = null;

	public int Count => pool.Count;
	public int Capacity => pool.Capacity;
	public int MaxCapacity => pool.MaxCapacity;
	public int MinCapacity => pool.MinCapacity;

	private void Awake()
	{
		var min = pool != null ? pool.MinCapacity : 0;
		var max = pool != null ? pool.MaxCapacity : int.MaxValue;

		pool = isSafe ? new SafeObjectPool<T>(CreateInstance, poolParameters)
							.SetOnReceive(OnReceive)
							.SetOnRelease(OnRelease)
							.SetOnClear(DestroyInstance) :
						new ObjectPool<T>(CreateInstance, poolParameters)
							.SetOnReceive(OnReceive)
							.SetOnRelease(OnRelease)
							.SetOnClear(DestroyInstance);
	}
	
	protected virtual T CreateInstance()
	{
		return Instantiate(@default, transform);
	}

	protected virtual void OnRelease(T that) 
	{ 
		that.gameObject.SetActive(false);
	}

	protected virtual void OnReceive(T that) 
	{
		that.gameObject.SetActive(true);
	}

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

	public UnityEngine.Object GetItself()
	{
		return this;
	}
}

#if UNITY_EDITOR
public class ComponentPoolEditor : EditorWindow
{
	[MenuItem("Custom/Component Pool Editor")]
	public static void OpenWindow()
	{
		GetWindow<ComponentPoolEditor>("Component Pool Editor");
	}

	private Vector2 scrollPosition = Vector2.zero;
	private void OnGUI()
	{
		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

		foreach(var pool in FindAllComponentPoolsAsTheirGenericBases()) {
			var field = pool.baseType.GetField("poolParameters", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			var param = (PoolParameters)field.GetValue(pool.context);
			 
			EditorGUILayout.LabelField($"{pool} -- Capacities: {param}");
		}

		EditorGUILayout.EndScrollView();
	}

	private List<(Component context, System.Type baseType)> FindAllComponentPoolsAsTheirGenericBases()
	{
		bool IsDerivedOfGenericType(System.Type type, System.Type genericType, out System.Type baseType)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType) {
				baseType = type;
				return true;
			}
			if (type.BaseType != null) {
				return IsDerivedOfGenericType(type.BaseType, genericType, out baseType);
			}
			baseType = null;
			return false;
		}

		List<(Component, System.Type)> pools = new List<(Component, System.Type)>();
		var sceneCount = SceneManager.sceneCount;
		for (int i = 0; i < sceneCount; i++) {
			var scene = SceneManager.GetSceneAt(i);

			var rooGameObjects = scene.GetRootGameObjects();
			foreach (var go in rooGameObjects) {
				var allPools = go.GetComponentsInChildren(typeof(Component), true);
				foreach (var pool in allPools) {
					if(IsDerivedOfGenericType(pool.GetType(), typeof(ComponentPool<>), out var baseType)) {
						pools.Add((pool, baseType));
					}
				}
			}
		}
		return pools;
	}
}
#endif
