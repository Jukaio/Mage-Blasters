using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Events;

namespace Alteruna.Trinity
{
    public delegate void RemoteProcedure(ushort fromUser, ProcedureParameters parameters, uint callID);
    public delegate void RemoteProcedureReply(ushort fromUser, string name, ProcedureParameters parameters, ushort result);

    /// <summary>
    /// Class <c>SynchronizableManager</c> is responsible for routing incoming and outgoing data to and from all <c>Synchronizables</c> in the Playroom.
    /// </summary>
    /// <seealso cref="Synchronizable"/>
    /// 
    public class SynchronizableManager : MonoBehaviour, ISynchronizationManager, ICodecRequest
    {
        [HideInInspector]
        public UnityEvent<SynchronizableManager, Synchronizable> PacketSent;
        [HideInInspector]
        public UnityEvent<SynchronizableManager, Synchronizable> PacketRouted;
        [HideInInspector]
        public UnityEvent<SynchronizableManager, Synchronizable> LockRequested;
        [HideInInspector]
        public UnityEvent<SynchronizableManager, Synchronizable> LockAquired;
        [HideInInspector]
        public UnityEvent<SynchronizableManager, ushort> ForceSynced;

        [HideInInspector]
        public bool IsObserver;
        private Dictionary<Guid, Synchronizable> mSynchonizables = new Dictionary<Guid, Synchronizable>();
        private Session mSession;
        private bool mProcessingPacket = false;

        // Remote Procedures
        private Dictionary<string, uint> mProcedureNames = new Dictionary<string, uint>();
        private Dictionary<uint, RemoteProcedure> mProcedureCallbacks = new Dictionary<uint, RemoteProcedure>();
        private Dictionary<uint, (string, RemoteProcedureReply, uint)> mProcedureReplies = new Dictionary<uint, (string, RemoteProcedureReply, uint)>();
        private Dictionary<uint, (ushort, uint, uint)> mLocalCalls = new Dictionary<uint, (ushort, uint, uint)>();
        private uint mNumCalls;
        private uint mLocalCallID;

        public void AttachSession(Session attachedSession, ushort userId, bool isObserver)
        {
            mSession = attachedSession;
            IsObserver = isObserver;
        }

        public void SessionClosed()
        {
            mSession = null;
        }

        public void RegisterSynchronizable(Guid id, Synchronizable serializable)
        {
            if (!mSynchonizables.ContainsKey(id) && serializable != null)
            {
                mSynchonizables.Add(id, serializable);
            }
        }

        public void DeregisterCodec(Guid id)
        {
            if (mSynchonizables.ContainsKey(id))
            {
                mSynchonizables.Remove(id);
            }
        }

        public void DecodePacket(SessionSyncPacket packet)
        {
            mProcessingPacket = true;
            packet.UnserializeData(this);
            mProcessingPacket = false;
        }

        public void DecodePacket(SessionForceSyncReplyPacket packet)
        {
            packet.UnserializeData(this);
        }

        public void Sync(Guid id)
        {
            if (mSession != null && mSynchonizables.ContainsKey(id))
            {
                SessionSyncPacket packet = new SessionSyncPacket();
                Synchronizable synchronizable = mSynchonizables[id];
                packet.Synchronizables.Add(
                        new SynchronizableElement
                        {
                            CodecID = id,
                            Synchronizable = synchronizable
                        }
                    );

                mSession.Route(packet, null, Reliability.Unreliable);
                PacketSent.Invoke(this, synchronizable);
            }
        }

        public void ForceSync(ushort requesterUserId)
        {
            SessionForceSyncReplyPacket packet = new SessionForceSyncReplyPacket();
            packet.RequestedUserId = requesterUserId;

            foreach (var codec in mSynchonizables)
            {
                packet.Synchronizables.Add(
                    new SynchronizableElement
                    {
                        CodecID = codec.Key,
                        Synchronizable = codec.Value
                    });
            }

            mSession.SendForceSyncReply(packet);
            ForceSynced.Invoke(this, requesterUserId);
        }

        public void WaitLockResource(Guid id)
        {
            if (mSession != null && mSynchonizables.ContainsKey(id))
            {
                mSession.WaitResource(id, null);
                LockRequested.Invoke(this, mSynchonizables[id]);
            }
        }

        public void TryLockResource(Guid id)
        {
            if (mSession != null && mSynchonizables.ContainsKey(id))
            {
                mSession.TryLockResource(id, null);
                LockRequested.Invoke(this, mSynchonizables[id]);
            }
        }

        public void LockRequestResponse(Guid codecId, bool isAcquired)
        {
            Synchronizable codec = mSynchonizables.FirstOrDefault(c => c.Key == codecId).Value;
            if (codec != null)
            {
                codec.LockRequestResponse(isAcquired);
            }
        }

        public void UnlockResource(Guid id)
        {
            if (mSession != null && mSynchonizables.ContainsKey(id))
            {
                Synchronizable synchronizable = mSynchonizables[id];
                mSession.UnlockResource(id, null);
                LockRequested.Invoke(this, synchronizable);
            }
        }

        public ISerializable GetCodecForId(Guid codecId)
        {
            if (mSynchonizables.ContainsKey(codecId))
            {
                if (mProcessingPacket)
                {
                    PacketRouted.Invoke(this, mSynchonizables[codecId]);
                }

                return mSynchonizables[codecId];
            }

            return null;
        }

        public void ClearCodecs()
        {
            mSynchonizables.Clear();
        }

        // -------- Remote Procedures --------------------------------

        public void HandleRemoteProcedureCall(ushort fromUser, uint procedureId, uint callID, bool fandf, ProcedureParameters parameters)
        {
            if (mProcedureCallbacks.ContainsKey(procedureId))
            {
                mLocalCallID++;
                mProcedureCallbacks[procedureId].Invoke(fromUser, parameters, mLocalCallID);

                if (!fandf)
                {
                    mLocalCalls.Add(mLocalCallID, (fromUser, procedureId, callID));
                }
            }
        }

        public void HandleRemoteProcedureCallReply(ushort fromUser, uint procedureId, uint callID, ProcedureParameters parameters, ushort result)
        {
            if (mProcedureReplies.ContainsKey(callID))
            {
                var call = mProcedureReplies[callID];

                if (call.Item3 > 1)
                {
                    call.Item2.Invoke(fromUser, call.Item1, parameters, result);

                    // Decrement reply count
                    mProcedureReplies[callID] = (call.Item1, call.Item2, call.Item3 - 1);
                }
                else
                {
                    // Invoke one last time
                    call.Item2.Invoke(fromUser, call.Item1, parameters, result);
                    mProcedureReplies.Remove(callID);
                }
            }
        }

        public void ReplyRemoteProcedure(uint callID, ProcedureParameters parameters, ushort result)
        {
            if (mLocalCalls.ContainsKey(callID))
            {
                var call = mLocalCalls[callID];
                mSession?.SendProcedureCallReply(call.Item1, call.Item2, call.Item3, Reliability.Reliable, result);
            }
        }

        public void InvokeRemoteProcedure(string name, ProcedureParameters parameters, ushort toUserID, RemoteProcedureReply replyCallback, Reliability reliability, bool fireAndForget)
        {
            if (mProcedureNames.ContainsKey(name))
            {
                uint procedureID = mProcedureNames[name];
                uint callID = ++mNumCalls;
                bool fanf = ((toUserID == (ushort)Alteruna.Trinity.UserId.All) || fireAndForget || replyCallback == null);
                mSession?.SendRemoteProcedureCall(procedureID, callID, fanf, reliability, parameters);

                if (!fanf)
                {
                    mProcedureReplies.Add(callID, (name, replyCallback, 0));
                }
            }
        }

        public uint RegisterRemoteProcedure(string name, RemoteProcedure callback)
        {
            uint procedureID = (uint)mProcedureNames.Count;
            mProcedureNames.Add(name, procedureID);
            mProcedureCallbacks.Add(procedureID, callback);
            return procedureID;
        }

        public void HandleRemoteProcedureCallAck(uint callID, ushort reciptients)
        {
            if (reciptients < 1)
            {
                return;
            }

            if (mProcedureReplies.ContainsKey(callID))
            {
                mProcedureReplies[callID] = (mProcedureReplies[callID].Item1, mProcedureReplies[callID].Item2, reciptients);
            }
        }
    }
}