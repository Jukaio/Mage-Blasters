using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAudioLibrary", menuName = "ScriptableObjects/Player Audio Library", order = 1)]
public class PlayerAudioLibrary : ScriptableObject
{
    [SerializeField] private AudioClip OnDeath;
    [SerializeField] private AudioClip OnUpgrade;

    public void PlayOnDeath(AudioSource source)
    {
        if(OnDeath != null) {
            source.clip = OnDeath;
            source.Play();
		}
	}

    public void PlayOnUpgrade(AudioSource source)
    {
        if (OnUpgrade != null) {
            source.clip = OnUpgrade;
            source.Play();
        }
    }
}
