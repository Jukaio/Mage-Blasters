using UnityEngine;
using System;

namespace Alteruna.Trinity
{
    /// <summary>
    /// Class <c>Synchronizable</c> defines a base contaning data to be synchronized with other clients in the Playroom.
    /// </summary>
    ///
    [DisallowMultipleComponent, RequireComponent(typeof(UniqueID))]
    public abstract class Synchronizable : MonoBehaviour, ISerializable
    {
        public bool HasOwnership { get; private set; }

        [SerializeField, HideInInspector]
        private UniqueID mUID;

        [SerializeField, HideInInspector]
        private SynchronizableManager mSynchronizableManager;

        private bool mWaitingForLockResponse = false;

        /// <summary>
        /// Called by the <c>SynchronizableManager</c> when we want to commit our data.
        /// </summary>
        /// <param name="writer">Used to write the data we want to be synchronized.</param>
        /// <seealso cref="SynchronizableManager"/>
        ///
        public abstract void AssembleData(Writer writer);

        /// <summary>
        /// Called by the <c>SynchronizableManager</c> when we have recieved data for this synchronizable from another client in our Playroom.
        /// </summary>
        /// <param name="writer">Contains the recieved data.</param>
        /// <seealso cref="SynchronizableManager"/>
        /// 
        public abstract void DisassembleData(Reader reader);

        /// <summary>
        /// This method informs the <c>SynchronizableManager</c> that this synchronizable has new data that needs to be synced.
        /// </summary>
        /// <seealso cref="SynchronizableManager"/>
        /// 
        public void Commit()
        {
            if (mWaitingForLockResponse)
                return;

            mSynchronizableManager.Sync(mUID.UID);
        }

        public void TakeOwnership(bool singleAttempt = false)
        {
            if (HasOwnership)
            {
                return;
            }

            mWaitingForLockResponse = true;
            
            if (singleAttempt)
            {
                mSynchronizableManager.TryLockResource(mUID.UID);
            }
            else
            {
                mSynchronizableManager.WaitLockResource(mUID.UID);
            }

        }

        public void ReleaseOwnership()
        {
            if (HasOwnership)
            {
                mSynchronizableManager.UnlockResource(mUID.UID);
                HasOwnership = false;
            }
        }

        public void Serialize(IPacketProcessor processor)
        {
            Writer writer = new Writer(processor);
            AssembleData(writer);
        }

        public void Unserialize(IPacketProcessor processor)
        {
            Reader reader = new Reader(processor);
            DisassembleData(reader);
        }

        /// <summary>
        /// Override this <c>Synchronizables</c> current unique ID with a new Guid.
        /// </summary>
        /// <param name="newUID">The new Guid.</param>
        /// <param name="deregisterOld">Should this <c>Synchronizable</c> be deregistered from the <c>SerializableManager</c> before re-registering with the new ID?</param>
        public void OverrideUID(Guid newUID, bool deregisterOld = true)
        {
            if (mSynchronizableManager != null)
            {
                if (deregisterOld)
                {
                    mSynchronizableManager.DeregisterCodec(mUID.UID);
                }

                mSynchronizableManager.RegisterSynchronizable(newUID, this);
            }

            mUID.UID = newUID;
        }

        public Guid GetUID()
        {
            return mUID.UID;
        }

        public void LockRequestResponse(bool aquired)
        {
            mWaitingForLockResponse = false;

            if (aquired)
            {
                HasOwnership = true;
            }
        }

        /// <summary>
        /// Override this <c>Synchronizables</c> 
        /// </summary>
        /// <param name="manager"></param>
        public void OverrideSynchronizableManager(SynchronizableManager manager)
        {
            mSynchronizableManager = manager;
        }

        private void OnEnable()
        {
            mUID = GetComponent<UniqueID>();

            if (mSynchronizableManager == null)
            {
                SynchronizableManager[] managers = (SynchronizableManager[])FindObjectsOfType(typeof(SynchronizableManager));
                for (int i = 0; i < managers.Length; i++)
                {
                    if (managers[i].gameObject.scene == gameObject.scene)
                    {
                        mSynchronizableManager = managers[i];
                        break;
                    }
                }
            }

            mUID.MakeUID();

            if (mSynchronizableManager != null)
            {
                mSynchronizableManager.RegisterSynchronizable(mUID.UID, this);
            }
        }
    }
}