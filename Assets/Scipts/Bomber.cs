using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Bomber : SubComponent, Executable
{
    [SerializeField] private BombPool bombPool;
    [SerializeField] private Bomb.Parameters bombParameters;
    [SerializeField] private PlayerAudioLibrary playerAudioLibrary;
    [Space]

    private Bomb.Parameters initialBombParameters;
    private HashSet<Bomb> usedBombs = new HashSet<Bomb>();
    private AudioSource audioSource = null;
    private Transform transform = null;

	public override void OnAwake()
	{
		transform = player.transform;
        initialBombParameters = bombParameters;
        audioSource = player.GetComponent<AudioSource>();
	}

	public override void OnSetup()
	{
        foreach (var bomb in usedBombs) {
            bombPool.Release(bomb);

        }
        usedBombs.Clear();
		bombParameters = initialBombParameters;
    }

	public void Execute()
    {
        var index = World.Instance.WorldToIndex(transform.position);
        if (!World.Instance.GetDataTile(index).HasBomb) {
            if (bombPool.TryReceive(out var bomb)) {
                usedBombs.Add(bomb);
                bomb.SetOff(index, bombParameters, () => {
                    usedBombs.Remove(bomb);
                    bombPool.Release(bomb);
                });
            }
        }
    }

    public void UpgradeCapacity(uint capacity)
    {
        playerAudioLibrary.PlayOnUpgrade(audioSource);
        bombPool.SetMaximumCapacity(bombPool.MaxCapacity + (int)capacity);
    }

    public void UpgradeRange(uint range)
    {
        playerAudioLibrary.PlayOnUpgrade(audioSource);
        bombParameters.SetRange(bombParameters.Range + (int)range);
    }
}
