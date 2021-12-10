public class BombPool : ComponentPool<Bomb>
{
	protected override void OnRelease(Bomb that)
	{
		base.OnRelease(that);
		that.OnRelease();
	}
}
