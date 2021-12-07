using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeltPool : ComponentPool<Melt>
{
	protected override void OnReceive(Melt that)
	{
		that.gameObject.SetActive(true);
	}

	protected override void OnRelease(Melt that)
	{
		that.gameObject.SetActive(false);
	}
}
