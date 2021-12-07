public abstract class SubComponent 
{
    public Player player { get; private set; }

    public void Construct(Player player)
    {
        this.player = player;
	}
    public virtual void OnConstruct() { }
    public virtual void OnAwake() { }
    public virtual void OnStart() { }
    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnSetup() { }
}
