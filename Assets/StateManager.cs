using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StateManager : MonoBehaviour
{
    [SerializeField] private string initialStateID;
    private State[] transitions;
    private State current;
    private Coroutine routine = null;
    public bool IsTransitioning => routine != null;

	private void Start()
	{
        Service<StateManager>.Set(this);
        transitions = GetComponentsInChildren<State>(true);

        Play(initialStateID);
	}

	public void Play(string id)
    {
        if(IsTransitioning) {
            return;
		}
        var next = transitions.First(that => that.Id == id);
        routine = StartCoroutine(Running(next));

    }

    private IEnumerator Running(State next)
    {
        if (current != null) {
            current.TranstionExit();
            yield return new WaitWhile(() => current.IsExiting);
        }

        current = next;

        if (current != null) {
            current.TransitionEnter();
            yield return new WaitWhile(() => current.IsEntering);
        }
        routine = null;
    }
}
