using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Alteruna.Trinity
{
    /// <summary>
    /// Class <c>UniqueID</c> defines an application-wide unique ID for identifying objects to be synced between clients in a Playroom.
    /// </summary>
    /// 
    [ExecuteAlways, DisallowMultipleComponent]
    public class UniqueID : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        public string UIDString = "";
        public Guid UID;

        public void MakeUID()
        {
            Awake();
        }

        private void Reset()
        {
            if (UIDString == "")
            {
                UIDString = Guid.NewGuid().ToString();
                #if UNITY_EDITOR
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                #endif
            }

            OnValidate();
        }

        private void OnValidate()
        {
            #if UNITY_EDITOR
            UnityEngine.SceneManagement.Scene currentScene = this.gameObject.scene;
            if (currentScene.isLoaded)
            {
                foreach (GameObject root in currentScene.GetRootGameObjects())
                {
                    foreach (UniqueID uid in root.GetComponentsInChildren<UniqueID>())
                    {
                        if (!UnityEngine.Object.ReferenceEquals(uid.gameObject, this.gameObject))
                        {
                            if (uid.UIDString == UIDString)
                            {
                                UID = Guid.NewGuid();
                                UIDString = UID.ToString();
                            }
                        }
                    }
                }
            }
            #endif
        }

        private void Awake()
        {
            if (UIDString != "")
            {
                UID = new Guid(UIDString);
                UIDString = UID.ToString();
            }
        }
    }
}