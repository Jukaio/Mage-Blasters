using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Alteruna.Trinity
{
    /// <summary>
    /// Class <c>AlterunaTrinity</c> is responsible for establishing a connection to a server. 
    /// Recieves data from other clients and invokes network-related events.
    /// </summary>
    ///
    [RequireComponent(typeof(SynchronizableManager), typeof(NameGenerator))]
    public class Trinity : MonoBehaviour, IDeviceListener, ISessionListener
    {
        /// The frequency at which to update statistics.
        public const int STATISITCS_INTERVAL = 1;

        // Maximum number of users in created sessions.
        public const int DEFAULT_MAX_USERS = 40;

        [HideInInspector]
        public bool InPlayroom { get; private set; }
        [HideInInspector]
        public ushort UserIndex { get; private set; }
        [HideInInspector]
        public Session CurrentPlayroom { get; private set; }

        public LogBase.Severity LogLevel = LogBase.Severity.Error;
        [HideInInspector]
        public System.Guid ApplicationID = System.Guid.Empty;
        [HideInInspector]
        public string AppIDString = "";

        [HideInInspector]
        public DeviceType DeviceType = DeviceType.Laptop;
        [HideInInspector]
        public string ClientName = "default_name";

        [HideInInspector]
        public int LANServerPort = 20000;
        [HideInInspector]
        public int PublishPort = 20000;
        [HideInInspector]
        public bool BroadcastEnabled = true;
        [HideInInspector]
        public bool UseKnownDevDevice = false;

        [HideInInspector]
        public string ServerIP = "";
        [HideInInspector]
        public int ServerPort = 20000;

        [Header("Connection Events")]
        public UnityEvent<Trinity, IDevice, bool> Connected;
        public UnityEvent<Trinity, IDevice, bool> ConnectionLost;
        public UnityEvent<Trinity, IDevice, ConnectionStatus> Disconnected;
        public UnityEvent<Trinity, IDevice> NewAvailableDevice;
        public UnityEvent<Trinity, IDevice> LostAvailableDevice;
        public UnityEvent<Trinity, IDevice, List<SessionInfo>> SessionListUpdated;
        public UnityEvent<Trinity, IDevice, int> LatencyUpdate;
        public UnityEvent<Trinity, IDevice> NetworkError;

        [Header("Session Events")]
        public UnityEvent<Trinity, Session, IDevice, ushort> JoinedSession;
        public UnityEvent<Trinity, Session, IDevice> LeftSession;
        public UnityEvent<Trinity, Session, ushort, string> OtherJoined;
        public UnityEvent<Trinity, Session, ushort, string> OtherLeft;
        public UnityEvent<Trinity, Session, ushort> SessionClosed;
        public UnityEvent<Trinity, Session, IDevice> SessionTransfered;

        //[Header("Observer Events")]
        [HideInInspector]
        public UnityEvent<Trinity, Session, IDevice, ushort> ObservedSession;
        [HideInInspector]
        public UnityEvent<Trinity, Session, IDevice> UnobservedSession;
        [HideInInspector]
        public UnityEvent<Trinity, Session, ushort, string> ObserverJoined;
        [HideInInspector]
        public UnityEvent<Trinity, Session, ushort, string> ObserverLeft;

        [Header("Synchronizable Events")]
        public UnityEvent<Trinity, Synchronizable> PacketSent;
        public UnityEvent<Trinity, Synchronizable> PacketRouted;
        public UnityEvent<Trinity, IPacketProcessor, IDevice, Reliability> PacketRecieved;
        public UnityEvent<Trinity, Synchronizable> LockRequested;
        public UnityEvent<Trinity, Synchronizable> LockAquired;
        public UnityEvent<Trinity, ushort> ForceSynced;

        // Dev Settings
        [SerializeField, HideInInspector]
        public bool AutoJoinOwnSession = false;
        [SerializeField, HideInInspector]
        public bool AutoJoinFirstSession = false;
        [SerializeField, HideInInspector]
        public int DevClientIndex = 0;
        [SerializeField, HideInInspector]
        public bool IsDevClient = false;

        [HideInInspector]
        public NetworkStatistics Statistics;

        private SynchronizableManager mSynchronizableManager;
        private UnityLog mLog;
        private List<IDevice> mDevices = new List<IDevice>();
        private LNL.DeviceManager mDeviceManager;
        private AnyDeviceAuthorizer mAuth;
        private SessionManager mSessionManager;
        private NameGenerator mNameGenerator;

        /// <summary>
        /// Join a locally hosted Playroom and act as a server.
        /// </summary>
        /// 
        public void JoinOwnPlayroom()
        {
            mSessionManager?.Join(mSynchronizableManager);
        }

        /// <summary>
        /// Retrieve a list contaning all playrooms currently available to join.
        /// </summary>
        /// <param name="playrooms">The list which will be populated with available playrooms.</param>
        /// 
        public void GetLocalPlayrooms(List<Playroom> playrooms)
        {
            mDeviceManager?.GetLocalPlayrooms(playrooms);
        }

        /// <summary>
        /// Join a remotely hosted playroom.
        /// </summary>
        /// <param name="device">The playroom to join.</param>
        /// 
        public void JoinRemotePlayroom(IDevice device, uint sessionID)
        {
            if (mDevices.Contains(device))
            {
                mSessionManager?.JoinRemote(device, mSynchronizableManager, sessionID);
            }
        }

        /// <summary>
        /// Join a locally hosted playroom as an observer and act as a server.
        /// </summary>
        /// 
        public void ObserveOwnPlayroom()
        {
            mSessionManager?.Observe(mSynchronizableManager);
        }

        /// <summary>
        /// Join a remotely hosted playroom as an observer.
        /// </summary>
        /// <param name="device">The playroom to join.</param>
        /// 
        public void ObserveRemotePlayroom(IDevice device)
        {
            if (mDevices.Contains(device))
            {
                mSessionManager?.ObserveRemote(device, mSynchronizableManager, 0);
            }
        }

        /// <summary>
        /// Leave the current playroom. 
        /// </summary>
        /// 
        public void LeavePlayroom()
        {
            mSessionManager?.Leave(UserIndex);
        }

        /// <summary>
        /// Poll the server for an updated playroom list.
        /// </summary>
        /// 
        public void ListPlayrooms(IDevice device)
        {
            if (device.KnownDevice)
            {
                mSessionManager?.ListSessions(device);
            }
        }

        public void InvokeRemoteProcedure(string name, ProcedureParameters parameters, ushort toUserID, RemoteProcedureReply replyCallback)
        {
            if (mSynchronizableManager == null)
            {
                mSynchronizableManager = GetComponent<SynchronizableManager>();
            }

            mSynchronizableManager.InvokeRemoteProcedure(name, parameters, toUserID, replyCallback, Reliability.Reliable, false);
        }

        public uint RegisterRemoteProcedure(string name, RemoteProcedure callback)
        {
            if (mSynchronizableManager == null)
            {
                mSynchronizableManager = GetComponent<SynchronizableManager>();
            }

            return mSynchronizableManager.RegisterRemoteProcedure(name, callback);
        }

        public void ReplyRemoteProcedure(uint callID, ProcedureParameters parameters, ushort result)
        {
            if (mSynchronizableManager == null)
            {
                mSynchronizableManager = GetComponent<SynchronizableManager>();
            }

            mSynchronizableManager.ReplyRemoteProcedure(callID, parameters, result);
        }

        private void Start()
        {
            // Verify ApplicationID
            if (ApplicationID == System.Guid.Empty || ApplicationID.ToString() != AppIDString)
            {
                if (AppIDString == "")
                {
                    Debug.LogError("Trinity was started without an Application ID! Please generate one in the inspector.");
                }
                else
                {
                    ApplicationID = System.Guid.Parse(AppIDString);
                }
            }

            // Add listeners to the SynchronizableManagers events
            mSynchronizableManager = GetComponent<SynchronizableManager>();
            mSynchronizableManager.PacketSent.AddListener(OnPacketSent);
            mSynchronizableManager.PacketRouted.AddListener(OnPacketRouted);
            mSynchronizableManager.LockRequested.AddListener(OnLockRequested);
            mSynchronizableManager.LockAquired.AddListener(OnLockAquired);
            mSynchronizableManager.ForceSynced.AddListener(OnForceSynced);

            // Generate a name for this client
            mNameGenerator = GetComponent<NameGenerator>();

            // Generate a new name if we are a dev client
            if (IsDevClient)
            {
                mNameGenerator.Generate();
            }
            ClientName = mNameGenerator.Name;

            // Create and set up the DeviceManager
            mDeviceManager = new Alteruna.Trinity.LNL.DeviceManager(
                      DeviceType, ClientName,
                      (uint)Alteruna.Trinity.Organization.Any,
                      ApplicationID);

            mDeviceManager.UpdateStatisticsInterval = STATISITCS_INTERVAL;
            Statistics = mDeviceManager.Statistics;

            mAuth = new Alteruna.Trinity.AnyDeviceAuthorizer();
            mSessionManager = new Alteruna.Trinity.SessionManager(mDeviceManager, this, DEFAULT_MAX_USERS);

            mDeviceManager.mCompositeListeners.Add(this);

            // Create and set up the Log
            mLog = new UnityLog();
            mLog.LogLevel = LogLevel;
            mSessionManager.Log = mLog;
            mDeviceManager.Log = mLog;

            if (UseKnownDevDevice)
            {
                mDeviceManager.AddKnownDevDevice(ServerPort);
            }

            mDeviceManager.DeviceAuthorizer = mAuth;
            mDeviceManager.Start(LANServerPort, PublishPort, BroadcastEnabled);
        }

        private void Update()
        {
            mDeviceManager?.Update();
        }

        private void OnDestroy()
        {
            mDeviceManager?.Stop();
        }

        // -------- Connection Events --------------------------------

        public void OnLatencyUpdate(IDevice device, int latency)
        {
            LatencyUpdate?.Invoke(this, device, latency);
        }

        public void OnConnectionLost(IDevice device, bool outgoing)
        {
            ConnectionLost?.Invoke(this, device, outgoing);
        }

        public void OnConnected(IDevice device, bool outgoing)
        {
            Connected?.Invoke(this, device, outgoing);
            if (device.KnownDevice)
            {
                mSessionManager?.ListSessions(device);
            }

            if (AutoJoinOwnSession)
            {
                JoinOwnPlayroom();
            }

            if (AutoJoinFirstSession)
            {
                if (device.LocalHost)
                {
                    JoinRemotePlayroom(device, 0);
                }
            }
        }

        public void OnDisconnected(IDevice device, ConnectionStatus status)
        {
            Disconnected?.Invoke(this, device, status);
        }

        public void OnPacketReceived(IPacketProcessor processor, IDevice device, Reliability reliability)
        {
            PacketRecieved?.Invoke(this, processor, device, reliability);
        }

        public void OnNetworkError(IDevice device)
        {
            NetworkError?.Invoke(this, device);
        }

        public void OnAvailable(IDevice device)
        {
            mDevices.Add(device);
            NewAvailableDevice?.Invoke(this, device);
        }

        public void OnUnavailable(IDevice device)
        {
            mDevices.Remove(device);
            LostAvailableDevice?.Invoke(this, device);
        }

        // -------- Session Events -----------------------------------

        public void OnSessionJoined(Session session, IDevice device, ushort id)
        {
            UserIndex = id;
            CurrentPlayroom = session;
            InPlayroom = true;
            JoinedSession?.Invoke(this, session, device, id);
        }

        public void OnSessionLeft(Session session, IDevice device)
        {
            CurrentPlayroom = null;
            InPlayroom = false;
            LeftSession?.Invoke(this, session, device);
        }

        public void OnOtherJoined(Session session, ushort userId, string userName)
        {
            OtherJoined?.Invoke(this, session, userId, userName);
        }

        public void OnOtherLeft(Session session, ushort userId, string userName)
        {
            OtherLeft?.Invoke(this, session, userId, userName);
        }

        public void OnSessionClosed(Session session, ushort reason)
        {
            mSynchronizableManager?.SessionClosed();
            SessionClosed?.Invoke(this, session, reason);
        }

        public void OnSessionTransfered(Session session, IDevice targetDevice)
        {
            SessionTransfered?.Invoke(this, session, targetDevice);
        }

        public void OnUpdatedSessionList(IDevice device, List<SessionInfo> sessions)
        {
            SessionListUpdated?.Invoke(this, device, sessions);
        }

        // -------- Observer Events ----------------------------------

        public void OnSessionObserved(Session session, IDevice device, ushort id)
        {
            ObservedSession?.Invoke(this, session, device, id);
        }

        public void OnSessionUnobserved(Session session, IDevice device)
        {
            UnobservedSession?.Invoke(this, session, device);
        }

        public void OnObserverJoined(Session session, ushort userId, string userName)
        {
            ObserverJoined?.Invoke(this, session, userId, userName);
        }

        public void OnObserverLeft(Session session, ushort userId, string userName)
        {
            ObserverLeft?.Invoke(this, session, userId, userName);
        }

        // -------- Synchronizable Events ----------------------------

        public void OnPacketSent(SynchronizableManager codecManager, Synchronizable synchronizable)
        {
            PacketSent?.Invoke(this, synchronizable);
        }

        public void OnPacketRouted(SynchronizableManager codecManager, Synchronizable synchronizable)
        {
            PacketRouted?.Invoke(this, synchronizable);
        }

        public void OnLockRequested(SynchronizableManager codecManager, Synchronizable synchronizable)
        {
            LockRequested?.Invoke(this, synchronizable);
        }

        public void OnLockAquired(SynchronizableManager codecManager, Synchronizable synchronizable)
        {
            LockAquired?.Invoke(this, synchronizable);
        }

        public void OnForceSynced(SynchronizableManager codecManager, ushort requesterUserId)
        {
            ForceSynced?.Invoke(this, requesterUserId);
        }

        // -------- Unused Events ----------------------------

        public void OnDeviceStatistics(IDevice device, DeviceStatistics statistics)
        { }

        public void OnSessionCreated(IDevice device, uint sessionID, bool success)
        { }

        public void OnLoadTest(IDevice device, uint sessionID)
        { }

        public void OnLoadTestReply(IDevice device, uint sessionID, uint sequenceNumber, float processingTime)
        { }

        public void OnSessionRemoved(IDevice device, uint sessionID, ResponseCode result)
        { }

        public void OnSessionUserDisconnected(IDevice device, uint sessionID, ushort userId, ResponseCode result)
        { }
    }
}
