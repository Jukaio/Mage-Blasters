using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class AvatarFieldUI : MonoBehaviour
{
    [SerializeField] private string title = "Player";
    [SerializeField] private int index;
    [SerializeField] private Text playerName;
    [SerializeField] private Text characterName;
    [SerializeField] private Image character;
    [SerializeField] private PlayerSkinLibrary skinLibrary;

    private string[] cachedNames = null;

    private string[] names => cachedNames != null ? cachedNames : skinLibrary.Keys.ToArray();
    private int spriteIndex = 0;

    private const string CATEGORY = "IdleDown";
    private const string LABEL = "0";

    private void SetCurrent(int index)
    {
        playerName.text = $"{title} #{this.index}";

        var text = names[index];
        character.sprite = skinLibrary.GetSpriteLibrary(text).GetSprite(CATEGORY, LABEL);
        characterName.text = text;
        Service<PlayerManager>.Instance.SetSpriteLibrary(this.index, text);
        spriteIndex = index;
    }

	private void Awake()
	{
        playerName.text = $"Unknown";
    }

    public void AddPlayer()
    {
        Service<PlayerManager>.Instance.AddPlayer(index);
        SetCurrent(index);
    }

    public void RemovePlayer()
    {
        Service<PlayerManager>.Instance.RemovePlayer(index);
        playerName.text = $"Unknown";
    }

    public void NextAvatar()
    {
        SetCurrent((spriteIndex + 1) % names.Length);
    }

    public void PrevousAvatar()
    {
        SetCurrent((spriteIndex + names.Length - 1) % names.Length);
    }
}
