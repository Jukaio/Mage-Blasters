using UnityEngine;

public class BlastRangeUpgrade : Upgrade
{
	[SerializeField] private uint rangeIncrease;

	public override void ApplyUpgrade(Player player)
	{
		player.Bomber.UpgradeRange(rangeIncrease);
	}
}
