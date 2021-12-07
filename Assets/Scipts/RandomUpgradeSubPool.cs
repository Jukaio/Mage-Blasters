
public class RandomUpgradeSubPool : ComponentPool<Upgrade> 
{
	protected override void OnReceive(Upgrade that)
	{
		that.gameObject.SetActive(true);
	}

	protected override void OnRelease(Upgrade that)
	{
		that.gameObject.SetActive(false);
	}
}

