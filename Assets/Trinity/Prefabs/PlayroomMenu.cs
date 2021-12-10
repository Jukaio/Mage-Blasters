using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Alteruna.Trinity;
using System.Collections;

public class PlayroomMenu : MonoBehaviour
{
    private const float REFRESH_LIST_INTERVAL = 5.0f;

    [SerializeField]
    private Text TitleText;
    [SerializeField]
    private ScrollRect ScrollRect;
    [SerializeField]
    private GameObject LANEntryPrefab;
    [SerializeField]
    private GameObject WANEntryPrefab;
    [SerializeField]
    private GameObject CloudImage;
    [SerializeField]
    private GameObject ContentContainer;
    [SerializeField]
    private Button StartButton;
    [SerializeField]
    private Button LeaveButton;

    private Trinity mTrinity;
    private List<AvailablePlayroom> mPlayrooms = new List<AvailablePlayroom>();
    private List<GameObject> mPlayroomObjects = new List<GameObject>();
    private List<IDevice> mDevices = new List<IDevice>();

    private class AvailablePlayroom
    {
        public IDevice Device;
        public bool KnownDevice;
        public uint PlayroomID;
        public string PlayroomName;
    }

    private void OnDisconnected(Trinity trinity, IDevice device, ConnectionStatus status)
    {
        if (status == ConnectionStatus.Failed)
        {
            return;
        }

        if (TitleText != null)
        {
            TitleText.text = "Reconnecting..";
        }

        for (int i = 0; i < mPlayroomObjects.Count; i++)
        {
            Destroy(mPlayroomObjects[i]);
        }
        mPlayroomObjects.Clear();
        mPlayrooms.Clear();

        if (mDevices.Contains(device))
        {
            mDevices.Remove(device);
        }
    }

    private void OnConnected(Trinity trinity, IDevice device, bool outgoing)
    {
        if (TitleText != null)
        {
            if (outgoing)
            {
                TitleText.text = "Trinity Playrooms";
            }
        }

        if (!mDevices.Contains(device))
        {
            mDevices.Add(device);
        }

        StartCoroutine("PollStatistics");
    }

    private void OnNewAvailableDevice(Trinity origin, IDevice device)
    {
        if (device.KnownDevice)
        {
            return;
        }

        mPlayrooms.Add(new AvailablePlayroom
        {
            Device = device,
            KnownDevice = false,
            PlayroomID = 0,
            PlayroomName = device.UserName,
        });

        UpdateList();
    }

    private void OnLostAvailableDevice(Trinity origin, IDevice device)
    {
        if (device.KnownDevice)
        {
            return;
        }

        for (int i = 0; i < mPlayrooms.Count; i++)
        {
            if (mPlayrooms[i].Device == device)
            {
                mPlayrooms.Remove(mPlayrooms[i]);
            }
        }

        UpdateList();
    }

    private void OnUpdatedSessionList(Trinity origin, IDevice device, List<SessionInfo> sessions)
    {
        for (int i = 0; i < sessions.Count; i++)
        {
            // Check if playroom already exists in list
            bool exists = false;
            for (int j = 0; j < mPlayrooms.Count; j++)
            {
                if (mPlayrooms[j].Device == device && sessions[i].ID == mPlayrooms[j].PlayroomID)
                {
                    exists = true;
                }
            }

            if (exists)
            {
                continue;
            }

            mPlayrooms.Add(new AvailablePlayroom
            {
                Device = device,
                KnownDevice = true,
                PlayroomID = sessions[i].ID,
                PlayroomName = sessions[i].DisplayName,
            });
        }

        UpdateList();
    }

    private void UpdateList()
    {
        for (int i = 0; i < mPlayroomObjects.Count; i++)
        {
            Destroy(mPlayroomObjects[i]);
        }
        mPlayroomObjects.Clear();

        if (ContentContainer != null)
        {
            for (int i = 0; i < mPlayrooms.Count; i++)
            {
                AvailablePlayroom room = mPlayrooms[i];

                GameObject entry;
                if (room.KnownDevice)
                {
                    entry = Instantiate(WANEntryPrefab, ContentContainer.transform);
                }
                else
                {
                    entry = Instantiate(LANEntryPrefab, ContentContainer.transform);
                }

                entry.SetActive(true);
                mPlayroomObjects.Add(entry);

                entry.GetComponentInChildren<Text>().text = room.PlayroomName;
                entry.GetComponentInChildren<Button>().onClick.AddListener(() => { JoinRoom(room.Device, room.PlayroomID, room.PlayroomName); });
            }
        }
    }

    private void JoinRoom(IDevice device, uint playroomID, string playroomName)
    {
        mTrinity?.LeavePlayroom();
        mTrinity?.JoinRemotePlayroom(device, playroomID);
    }

    private void JoinOwnRoom()
    {
        mTrinity?.LeavePlayroom();
        mTrinity?.JoinOwnPlayroom();
    }

    private void LeaveProom()
    {
        mTrinity?.LeavePlayroom();
    }

    private void JoinedPlayroom(Trinity trinity, Session session, IDevice device, ushort userID)
    {
        if (TitleText != null)
        {
            TitleText.text = "In Playroom " + session.DisplayName;
        }
    }

    private void LeftPlayroom(Trinity trinity, Session session, IDevice device)
    {
        if (TitleText != null)
        {
            TitleText.text = "Trinity Playrooms";
        }
    }

    private void Start()
    {
        if (mTrinity == null)
        {
            mTrinity = FindObjectOfType<Trinity>();
        }

        if (mTrinity != null)
        {
            mTrinity.NewAvailableDevice.AddListener(OnNewAvailableDevice);
            mTrinity.LostAvailableDevice.AddListener(OnLostAvailableDevice);
            mTrinity.SessionListUpdated.AddListener(OnUpdatedSessionList);
            mTrinity.Disconnected.AddListener(OnDisconnected);
            mTrinity.Connected.AddListener(OnConnected);
            mTrinity.JoinedSession.AddListener(JoinedPlayroom);
            mTrinity.LeftSession.AddListener(LeftPlayroom);
            StartButton.onClick.AddListener(JoinOwnRoom);
            LeaveButton.onClick.AddListener(LeaveProom);
        }
    }

    private IEnumerator PollStatistics()
    {
        while (true)
        {
            for (int i = 0; i < mDevices.Count; i++)
            {
                mTrinity.ListPlayrooms(mDevices[i]);
            }

            yield return new WaitForSeconds(REFRESH_LIST_INTERVAL);
        }
    }
}
