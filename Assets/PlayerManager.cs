using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerManager : MonoBehaviour
{
	[SerializeField] private PlayerPool playerPool;
	[SerializeField] private Transform[] spawnnPoints;
	[SerializeField] private PlayerControlSettings[] defaultPlayerControlSettings;
	[SerializeField] private PlayerSkinLibrary skinLibrary;
	[SerializeField] private AvatarGUI[] playerGUI;

	private Coroutine routine = null;
	private Dictionary<int, Player> players = new Dictionary<int, Player>();

	private const string CATEGORY = "IdleDown";
	private const string LABEL = "0";

	private int playersAlive = 0;
	public int PlayerCount => players.Count;

	private void Awake()
	{
		Service<PlayerManager>.Set(this);
		if(spawnnPoints.Length < playerPool.MaxCapacity) {
			Debug.LogWarning("There might not be enough spawnpoint");
		}
	}

	public void SetAllPlayersActive(bool isActive)
	{
		foreach (var player in players.Values) {
			if(player != null) {
				player.gameObject.SetActive(isActive);
			}
		}
	}

	public void Kill(Player player)
	{
		if (players.ContainsValue(player)) {
			var pair = players.First(that => player == that.Value);
			playerGUI[pair.Key - 1].MarkAsDead();
			playersAlive--;
		}

		if (routine == null) {
			routine = StartCoroutine(WaitAndMenu());
		}
	}

	private IEnumerator WaitAndMenu()
	{
		yield return new WaitForSeconds(0.25f);
		if (playersAlive <= 1) {
			Service<StateManager>.Instance.Play("Menu");
		}
		routine = null;
	}

	public void Setup()
	{
		foreach (var image in playerGUI) {
			image.gameObject.SetActive(false);
		}
		foreach (var player in players) {
			if (player.Value != null) {
				var id = player.Key;
				var value = player.Value;

				value.Setup();
				var index = World.Instance.WorldToIndex(spawnnPoints[id - 1]);
				value.transform.position = World.Instance.IndexToWorld(index);
				value.SetPlayerControlSettings(defaultPlayerControlSettings[id - 1]);
				playerGUI[id - 1].gameObject.SetActive(true);
				playerGUI[id - 1].MarkAsAlive();
			}
		}
		playersAlive = PlayerCount;
	}

	public void AddPlayer(int id)
	{
		if (playerPool.TryReceive(out var player)) {
			var index = World.Instance.WorldToIndex(spawnnPoints[id - 1]);
			player.transform.position = World.Instance.IndexToWorld(index);
			players[id] = player;
		}
	}

	public void RemovePlayer(int id)
	{
		if(players.TryGetValue(id, out var player)) {
			playerPool.Release(player);
			players.Remove(id);
			playerGUI[id - 1].gameObject.SetActive(false);
		}
	}

	public void SetSpriteLibrary(int id, string name)
	{
		if (players.ContainsKey(id)) {
			if(skinLibrary.TryGetSpriteLibrary(name, out var spritelibraryAsset)){
				players[id].SetSpriteLibrary(spritelibraryAsset);
				playerGUI[id - 1].SetSprite(spritelibraryAsset.GetSprite(CATEGORY, LABEL));
			}
		}
	}
}
