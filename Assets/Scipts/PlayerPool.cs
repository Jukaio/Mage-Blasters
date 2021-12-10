public class PlayerPool : ComponentPool<Player>
{
	protected override void OnReceive(Player that)
	{
		that.gameObject.SetActive(false);
	}
}
