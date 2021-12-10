using System.Collections.Generic;
using UnityEngine;
using Alteruna.Trinity;
using System;

/// <summary>
/// Class <c>SpawnerSynchronizable</c> defines a component which can instantiate objects on all clients in the Playroom simultaneously.
/// </summary>
/// 
public class SpawnerSynchronizable : Synchronizable
{
    public GameObject SpawnableObject;
    private List<GameObject> mSpawnedObjects = new List<GameObject>();

    public override void AssembleData(Writer writer)
    {
        if (mSpawnedObjects.Count > 0)
        {
            GameObject lastObject = mSpawnedObjects[mSpawnedObjects.Count - 1];
            writer.Write(lastObject.transform.position.x);
            writer.Write(lastObject.transform.position.y);
            writer.Write(lastObject.transform.position.z);

            writer.Write(lastObject.transform.rotation.eulerAngles.x);
            writer.Write(lastObject.transform.rotation.eulerAngles.y);
            writer.Write(lastObject.transform.rotation.eulerAngles.z);

            writer.Write(lastObject.transform.localScale.x);
            writer.Write(lastObject.transform.localScale.y);
            writer.Write(lastObject.transform.localScale.z);

            Synchronizable[] synchronizables = lastObject.GetComponentsInChildren<Synchronizable>();
            for (int i = 0; i < synchronizables.Length; i++)
            {
                writer.Write(synchronizables[i].GetUID().ToString());
            }
        }
    }

    public override void DisassembleData(Reader reader)
    {
        GameObject newObj = Instantiate(SpawnableObject);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(newObj, gameObject.scene);

        newObj.transform.position =
            new Vector3(
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat()
            );

        newObj.transform.rotation =
            Quaternion.Euler(
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat()
            );

        newObj.transform.localScale =
            new Vector3(
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat()
            );

        Synchronizable[] synchronizables = newObj.GetComponentsInChildren<Synchronizable>();
        for (int i = 0; i < synchronizables.Length; i++)
        {
            synchronizables[i].OverrideUID(new Guid(reader.ReadString()), false);
        }

        mSpawnedObjects.Add(newObj);
    }

    public void Spawn(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject newObj = Instantiate(SpawnableObject);
        newObj.transform.position = position;
        newObj.transform.rotation = rotation;
        newObj.transform.localScale = scale;
        mSpawnedObjects.Add(newObj);

        Synchronizable[] synchronizables = newObj.GetComponentsInChildren<Synchronizable>();
        for (int i = 0; i < synchronizables.Length; i++)
        {
            synchronizables[i].OverrideUID(Guid.NewGuid(), false);
        }

        Commit();
    }

    [ContextMenu("Test Spawn")]
    private void Test()
    {
        Spawn(transform.position, transform.rotation, transform.localScale);
    }
}
