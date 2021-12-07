public class BombPool : ComponentPool<Bomb>
{
	protected override void OnReceive(Bomb that)
	{
		that.gameObject.SetActive(true);
	}

	protected override void OnRelease(Bomb that)
	{
		that.gameObject.SetActive(false);
		that.OnRelease();
	}
}
