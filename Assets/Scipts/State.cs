using UnityEngine;

public class State : MonoBehaviour
{
    [System.Serializable]
    public class Event : UnityEngine.Events.UnityEvent { }
    
    [SerializeField] private string id;
    [SerializeField] private Event onEnter;
    [SerializeField] private Event onExit;

    public string Id => id;

    // Find solution to implement those transitions based on coreroutnes :( 
    public bool IsEntering => false;
    public bool IsExiting => false;

    public void TransitionEnter()
    {
        if(onEnter != null) {
            onEnter.Invoke();
		}
    }
    public void TranstionExit()
    {
        if (onExit != null) {
            onExit.Invoke();
        }
    }
}
