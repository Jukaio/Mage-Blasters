public class BlastPool : ComponentPool<Blast>
{
	protected override void OnReceive(Blast that)
	{
		that.gameObject.SetActive(true);
	}

	protected override void OnRelease(Blast that)
	{
		that.gameObject.SetActive(false);
	}
}
