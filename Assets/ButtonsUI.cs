using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonsUI : MonoBehaviour
{
    public void OnPlay(string id)
    {
		if(!(Service<PlayerManager>.Instance.PlayerCount > 1)) {
			return;
		}
		Service<StateManager>.Instance.Play(id);
	}

	public void OnQuit()
	{
		Application.Quit();
	}

	public void OnPause()
	{
		Time.timeScale = 0.0f;
	}

	public void OnResume()
	{
		Time.timeScale = 1.0f;
	}
}
