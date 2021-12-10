using UnityEngine;
using Alteruna.Trinity;

/// <summary>
/// Class <c>ExampleSynchronizable</c> is an example of how a <c>Synchronizable</c> can be defined.
/// </summary>
/// 
public class ExampleSynchronizable : Synchronizable
{
    // Data to be synchronized with other players in our playroom.
    public float SynchronizedFloat = 3.0f;

    // Used to store the previous version of our data so that we know when it has changed.
    float OldSynchronizedFloat;

    public override void DisassembleData(Reader reader)
    {
        // Set our data to the updated value we have recieved from another player.
        SynchronizedFloat = reader.ReadFloat();

        // Save the new data as our old data, otherwise we will immediatly think it changed again.
        OldSynchronizedFloat = SynchronizedFloat;
    }

    public override void AssembleData(Writer writer)
    {
        // Write our data so that it can be sent to the other players in our playroom.
        writer.Write(SynchronizedFloat);
    }

    private void Update()
    {
        // If the value of our float has changed, sync it with the other players in our playroom.
        if (SynchronizedFloat != OldSynchronizedFloat)
        {
            // Store the updated value
            OldSynchronizedFloat = SynchronizedFloat;

            // Tell Trinity that we want to commit our data.
            Commit();
        }
    }
}
