using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Linq;

public class Melt : MonoBehaviour
{
    private const string CETEGORY = "Melt";
    private SpriteResolver spriteResolver;

	private void Awake()
	{
		spriteResolver = GetComponent<SpriteResolver>();
	}


	// t is between 0 and 1
	public void Animate(float t)
	{
		var enumerable = spriteResolver.spriteLibrary.spriteLibraryAsset.GetCategoryLabelNames(CETEGORY);
		SetSprite((int)(enumerable.Count() * t));
	}

	private void SetSprite(int index)
	{
		spriteResolver.SetCategoryAndLabel(CETEGORY, index.ToString());
	}

	public void Deactivate()
	{
		var index = World.Instance.WorldToIndex(transform.position);
		World.Instance.GetDataTile(index).HasExplosion = false;
	}
}
