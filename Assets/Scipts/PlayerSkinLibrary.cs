using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine.U2D.Animation;

[System.Serializable]
[CreateAssetMenu(fileName = "PlayerSkinLibrary", menuName = "ScriptableObjects/Player Skin Library", order = 1)]
public class PlayerSkinLibrary : ScriptableObject
{
	[SerializeField] private List<string> keys;
	[SerializeField] private List<SpriteLibraryAsset> libraries;

	public IEnumerable<string> Keys => keys;

	public bool TryGetLibraryIndex(SpriteLibraryAsset library, out int index)
	{
		if(libraries.Contains(library)) {
			index = libraries.IndexOf(library);
			return true;
		}
		index = int.MinValue;
		return false;
	}

	public SpriteLibraryAsset GetSpriteLibrary(string key)
	{
		if (keys.Contains(key)) {
			int index = keys.IndexOf(key);
			return libraries[index];
		}
		return null;
	}

	public bool TryGetSpriteLibrary(string key, out SpriteLibraryAsset library)
	{
		if (keys.Contains(key)) {
			int index = keys.IndexOf(key);
			library = libraries[index];
			return true;
		}
		library = null;
		return false;
	}
}