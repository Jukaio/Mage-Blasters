public class PlayerPool : ComponentPool<Player>
{
	protected override void OnReceive(Player that)
	{
		that.gameObject.SetActive(false);
	}

	protected override void OnRelease(Player that)
	{
		that.gameObject.SetActive(false);
	}
}
