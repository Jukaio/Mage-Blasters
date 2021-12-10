using UnityEngine;
using Alteruna.Trinity;

/// <summary>
/// Class <c>TransformSynchronizable</c> defines a component which synchronizes its gameobjects transform with other clients in the Playroom.
/// </summary>
/// 
public class TransformSynchronizable : Synchronizable
{
    // Data
    private Vector3 mOldPosition;
    private Quaternion mOldRotation;
    private Vector3 mOldScale;

    public override void DisassembleData(Reader reader)
    {
        transform.localPosition =
            new Vector3(
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat()
            );

        transform.localRotation =
            Quaternion.Euler(
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat()
            );

        transform.localScale =
            new Vector3(
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat()
            );

        mOldPosition = transform.localPosition;
        mOldRotation = transform.localRotation;
        mOldScale = transform.localScale;
    }

    public override void AssembleData(Writer writer)
    {
        mOldPosition = transform.localPosition;
        mOldRotation = transform.localRotation;
        mOldScale = transform.localScale;

        // Position
        writer.Write(transform.localPosition.x);
        writer.Write(transform.localPosition.y);
        writer.Write(transform.localPosition.z);

        // Rotation
        writer.Write(transform.localRotation.eulerAngles.x);
        writer.Write(transform.localRotation.eulerAngles.y);
        writer.Write(transform.localRotation.eulerAngles.z);

        // Scale
        writer.Write(transform.localScale.x);
        writer.Write(transform.localScale.y);
        writer.Write(transform.localScale.z);
    }

    private void FixedUpdate()
    {
        if (mOldPosition != transform.localPosition ||
            mOldRotation != transform.localRotation ||
            mOldScale != transform.localScale)
        {
            Commit();
        }
    }

    private void Start()
    {
        mOldPosition = transform.localPosition;
        mOldRotation = transform.localRotation;
        mOldScale = transform.localScale;
    }
}