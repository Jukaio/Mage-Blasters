using UnityEngine;

public class BombCapacityUpgrade : Upgrade
{
	[SerializeField] private uint capacityIncrease;

	public override void ApplyUpgrade(Player player)
	{
		player.Bomber.UpgradeCapacity(capacityIncrease);
	}
}
