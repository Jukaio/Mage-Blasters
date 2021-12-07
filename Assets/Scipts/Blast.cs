using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Collections.Generic;
using System.Linq;

public class Blast : MonoBehaviour
{
	private SpriteRenderer spriteRenderer;
	private SpriteResolver spriteResolver;
	private string category;
	
	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteResolver = GetComponent<SpriteResolver>();
	}

	public void PlaceAt(Vector2Int index)
	{
		transform.position = World.Instance.IndexToWorld(index);
		World.Instance.GetDataTile(index).HasExplosion = true;
	}

	public void SetSpriteCategory(string category, bool flipX, bool flipY)
	{
		spriteRenderer.flipX = flipX;
		spriteRenderer.flipY = flipY;
		this.category = category;
	}

	// t is between 0 and 1
	public void Animate(float t)
	{
		var count = spriteResolver.spriteLibrary.spriteLibraryAsset.GetCategoryLabelCount(category);
		SetSprite((int)(count * t));
	}

	private void SetSprite(int index)
	{
		spriteResolver.SetCategoryAndLabel(category, index.ToString());
	}

	public void Deactivate()
	{
		var index = World.Instance.WorldToIndex(transform.position);
		World.Instance.GetDataTile(index).HasExplosion = false;
	}
}

