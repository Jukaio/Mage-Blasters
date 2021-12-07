using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonMonobehaviour<Resource> : MonoBehaviour 
	where Resource : SingletonMonobehaviour<Resource>
{
	public static object @lock = new object();
	private static Resource instance;
	public static Resource Instance => GetGuaranteedInstance();

	private static Resource GetGuaranteedInstance()
	{
		if(instance == null) {
			lock (@lock) {
				if (instance == null) {
					var gameObjectInScene = FindObjectsOfType<Resource>(true);
					if (gameObjectInScene.Length > 1) {
						throw new UnityException("Don't have more than one instance of " + typeof(Resource).ToString() + "in application");
					}
					else if (gameObjectInScene.Length == 0) {
						GameObject gameObject = new GameObject(typeof(Resource).ToString());
						instance = gameObject.AddComponent<Resource>();

						Application.quitting += () => DestroyImmediate(gameObject);
					}
					else {
						instance = gameObjectInScene[0];
					}
					DontDestroyOnLoad(instance); // In any case
				}
			}
		}
		return instance;
	}
}




