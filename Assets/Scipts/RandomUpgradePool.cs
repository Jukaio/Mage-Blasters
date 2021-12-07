using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RandomUpgradePool : MonoBehaviour, IPool<Upgrade>
{
	[SerializeField, Range(0.0f, 1.0f)] private float initalSpawnChance = 0.2f;
	[SerializeField, Range(-1.0f, 1.0f)] private float spawnChanceBias = 0.02f;
	private float currentSpawnChance = 0.0f;

	private RandomUpgradeSubPool[] subPools;
	private Dictionary<System.Type, RandomUpgradeSubPool> typeLookup = new Dictionary<System.Type, RandomUpgradeSubPool>();

	private void Start()
	{
		subPools = GetComponentsInChildren<RandomUpgradeSubPool>();
	}

	private void OnEnable()
	{
		currentSpawnChance = initalSpawnChance;
	}

	public Upgrade Receive()
	{
		if(Random.Range(0.0f, 1.0f) < currentSpawnChance) {
			currentSpawnChance = initalSpawnChance;
			if ((subPools.Length > 0)) {
				var pool = subPools[Random.Range(0, subPools.Length)];
				if(pool.TryReceive(out var item)) {
					typeLookup[item.GetType()] = pool;
					return item;
				}
			}			
		}
		else {
			currentSpawnChance += spawnChanceBias;
		}
		return null;
	}

	public void Release(Upgrade that)
	{
		if(typeLookup.TryGetValue(that.GetType(), out var pool)) {
			pool.Release(that);
		}
	}

	public bool TryReceive(out Upgrade that)
	{
		if (Random.Range(0.0f, 1.0f) < currentSpawnChance) {
			currentSpawnChance = initalSpawnChance;
			if ((subPools.Length > 0)) {
				var pool = subPools[Random.Range(0, subPools.Length)];
				if (pool.TryReceive(out var item)) {
					typeLookup[item.GetType()] = pool;
					that = item;
					return true;
				}
			}
		}
		else {
			currentSpawnChance += spawnChanceBias;
		}
		that = null;
		return false;
	}

	public void Clear()
	{
		foreach (var pool in subPools) {
			pool.Clear();
		}
	}
}
