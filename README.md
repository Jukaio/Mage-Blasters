# Mage-Busters

Check it out here: https://jukaio.itch.io/mage-blasters

![yayy](https://user-images.githubusercontent.com/47029889/214387721-4bec7d3f-c12f-4dd0-8255-0c758f7610d9.gif)


The game so far is a little bomberman clone with a Menu and Gameplay
The menu allows each player to decide if they want to play with up to four players and it lets them check the controls for their chosen character slot.
The gameplay is simple bomberman with some upgrades and such.

## Design Patterns

### Singleton
- I used the Singleton for global access if I only want and expect ONE instance of said object (In this case world) This backlashed cause resetting and reinitialising it was 
  harder to do, I should have used a Service Locator instead and just overwrite the old instance with a new one (World)
  
  I.e. 
  World newWorld = Instantiate(somePrefab);
  Destroy(Service.Instance)
  Service.Set(newWorld)
  Would have added convenience
- Boilerplate in "SingletonMonobehaviour.cs"
- Usage in "World.cs"

### Service Locator
- I used it for global access for the Player Manager and StateManager so I can reach it from anywher 
- These instances were persistent and never reset. Singleton would have been even beter cause later I realised I wanted the PlayerManager to be one persistent instance
- Boilerplate in "Service.cs"
- Usage in "PlayerManager.cs" and others

### Object Pool
- Player has a bomb pool, bomb has a blast pool, World has an upgrade pool and a melt pool (Bomb that hits something melts)
- Boilerplate in "ObjectPool.cs"
- Implementations and others in several different scripts named "Pool" and their holders

### Decorator
- I combined ComponentPools to one pool that hands out a random powerup among the different pools. Therefore, I decorated a pool with randomness behaviour
- "RandomUpgradePool.cs"
- "ComponentPool.cs" decorates the ObjectPool with MonoBehaviour, or it is an Adapter for MonoBehaviour since brings the behaviour of MonoBehaviour and the ObjectPool together with the option of using a safe Pool

### Component/Composition/Aggregation 
- The player has a Bomber, later on it can get decorated to make super bombs or other kinds of bombs since all we have to do is change the Bomber with another bomb handler
- Entity requires a death component. If a entity lives, it can die so one can not be without. It technically can, but in terms of this game it can't
  Similar to the car and engine analogy
- Additionally, I made my SubComponent that splits a Component into smaller Components
- "Death.cs" and "Bomber.cs"

### Dependency Injection
- Most MonoBehaviours I use call GetComponent and get the contextual data/objects during Unity's way construction
- My SubComponents have a OnAwake method in which they can receive contextual components. As in, all the things they need
- "Death.cs" and "Bomber.cs"

### Spatial Partioning (Sort of)
- I use the unity grid with a custom tile: A data grid. The data grid gets used to avoid physics collision detection
- Bombs, blasts, and so on. Movement uses physics collision detection
- "World.cs" the DataTile class

### State
- I made a State class. The state class is not utilising inheritance like in other State patterns I have seen, instead I use UnityEvents
- I use UnityEvents to allow StateChanges controlled through the Editor. If we want to transition from MenuState to GameplayState, I can change the SetActives through the
  UnityEvents. I can also use special initialisers and such to create the world, play an intro animation or whatever.
- The StateManager does not Update, instead it controls transitions and what is active - It should still count as a State
- "StateManager.cs" and "State.cs"

### Activity Lifecycle
- My SubComponents implement a life cycle to give SubComponents a quick way to implement and guarantee usual lifecycle behaviour
- I have no idea what the real name is. It is compareable to Update (from Game Design Patterns) or (Gameplay Loop) or the Activity LifeCycle in Android Studio
- "SubComponent.cs"

#### There can be more here and there, but some of them feel very natural to me
