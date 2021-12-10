using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace Alteruna.Trinity.Development
{
    public class EventLog : EditorWindow
    {
        [Serializable]
        private class LoggedEvent
        {
            public string Message;
            public Direction Direction;
            public Trinity Origin;
            public long TimeStamp;
            public long LastRepeatTime;
            public int Repeats = 1;
            public GameObject TargetObject = null;
            public Type SyncType;
        }

        private enum Direction
        {
            OutgoingProtocol,
            IncomingProtocol,
            OutgoingUser,
            IncomingUser,
            Nowhere,
        }

        [Serializable]
        private class FilterType
        {
            public bool Selected;
            public Type Type;
        }

        private const float TITLE_HEIGHT = 20.0f;
        private const float FILTER_HEIGHT = 15.0f;
        private const float CONTROLS_HEIGHT = 35.0f;
        private const float ARROW_HEIGHT = 18.0f;
        private const float SQUSIH_SIZE = 20.0f;
        private const double COLLAPSE_UNDER_MS = 2000.0f;
        private const double COMPRESS_AFTER_SECONDS = 2.0f;

        private Color mOtherEventColor = new Color(0.25f, 0.13f, 0.94f, 1f);
        private Color mProtocolColor = new Color(0.89f, 0.49f, 0.09f, 1.0f);
        private Color mUserColor = new Color(0.0f, 0.18f, 0.27f, 1.0f);

        private System.Diagnostics.Stopwatch mRuntime = new System.Diagnostics.Stopwatch();
        private Vector2 mVisualScrollPos;
        private bool mFinishedSetup = false;

        [SerializeField]
        private int mSelectedLog = 0;

        [SerializeField]
        private List<List<LoggedEvent>> mLogEntries = new List<List<LoggedEvent>>();

        [SerializeField]
        private List<FilterType> mLogFilter = new List<FilterType>();

        [MenuItem("Trinity/Event Log")]
        static void ShowWindow()
        {
            EditorWindow editorWindow = GetWindow(typeof(EventLog));
            editorWindow.titleContent = new GUIContent("Trinity Event Log");
            editorWindow.autoRepaintOnSceneChange = true;
            editorWindow.minSize = new Vector2(300, 200);
            editorWindow.Show();
        }

        private void OnGUI()
        {
            // Ensure our runtime stopwatch doesn't get de-synced with the game time
            if (EditorApplication.isPaused)
            {
                mRuntime.Stop();
            }
            else if (!mRuntime.IsRunning)
            {
                mRuntime.Start();
            }

            // Draw window
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawSelectionBar();
            DrawFilterBar();
            DrawVisualLog();
            EditorGUILayout.EndVertical();

            // Controls
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Clear Log", GUILayout.MaxHeight(CONTROLS_HEIGHT)))
            {
                mLogEntries.ForEach(l => l.Clear());
                mVisualScrollPos.y = 0;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                List<string> clientNames = new List<string>();
                for (int i = 0; i < mLogEntries.Count; i++)
                {
                    if (mLogEntries[i].Count > 0)
                    {
                        clientNames.Add(mLogEntries[i][0].Origin.ClientName);
                    }
                }

                if (mSelectedLog < mLogEntries.Count)
                {
                    mSelectedLog = GUILayout.Toolbar(mSelectedLog, clientNames.ToArray(), GUILayout.MaxWidth(position.width));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilterBar()
        {
            GUIStyle filterStyle = new GUIStyle(GUI.skin.button) { margin = new RectOffset(0, 0, 0, 0) };
            filterStyle.wordWrap = true;

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                foreach (FilterType type in mLogFilter)
                {
                    if (type.Type == null)
                        continue;

                    type.Selected = GUILayout.Toggle(type.Selected, type.Type.ToString(), filterStyle, GUILayout.ExpandWidth(true));
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVisualLog()
        {
            Color oldColor = GUI.backgroundColor;

            float scrollViewHeight = (position.height - (TITLE_HEIGHT + CONTROLS_HEIGHT + FILTER_HEIGHT));
            GUIStyle entryStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 2, 2), padding = new RectOffset(20, 20, 3, 3) };
            entryStyle.normal.background = Texture2D.whiteTexture;
            entryStyle.normal.textColor = Color.white;

            GUIStyle arrowStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 0, 0), padding = new RectOffset(0, 0, 0, 0) };
            arrowStyle.normal.background = Texture2D.whiteTexture;

            GUIStyle timesquishStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, margin = new RectOffset(0, 0, 5, 5), padding = new RectOffset(0, 0, 0, 0) };

            mVisualScrollPos = EditorGUILayout.BeginScrollView(mVisualScrollPos, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            if (mLogEntries.Count > 0 && mSelectedLog < mLogEntries.Count)
            {
                List<LoggedEvent> log = mLogEntries[mSelectedLog];

                long carryOverTime = 0;

                // Draw Event Log
                for (int i = 0; i < log.Count; i++)
                {
                    FilterType logFilter = mLogFilter.FirstOrDefault(f => f.Type == log[i].SyncType);
                    if (logFilter != null && logFilter.Selected == false)
                    {
                        carryOverTime += (log[i].TimeStamp - log[i - 1].LastRepeatTime);
                        continue;
                    }

                    // Draw Timesquish
                    if (i > 0)
                    {
                        TimeSpan time = TimeSpan.FromMilliseconds((log[i].TimeStamp - log[i - 1].LastRepeatTime) + carryOverTime);
                        if (time.TotalSeconds > COMPRESS_AFTER_SECONDS)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space((position.width * 0.5f) - (SQUSIH_SIZE * 0.5f));
                            GUILayout.Box(Tools.squishTexture, timesquishStyle, GUILayout.Height(SQUSIH_SIZE), GUILayout.Width(SQUSIH_SIZE));
                            string timeString = time.ToString("mm':'ss'.'fff");
                            GUILayout.Box("[" + timeString + "]", timesquishStyle);
                            EditorGUILayout.EndHorizontal();

                            carryOverTime = 0;
                        }
                    }

                    switch (log[i].Direction)
                    {
                        case Direction.OutgoingProtocol:
                            {
                                GUI.backgroundColor = mProtocolColor;
                                break;
                            }

                        case Direction.IncomingProtocol:
                            {
                                GUI.backgroundColor = mProtocolColor;
                                break;
                            }

                        case Direction.OutgoingUser:
                            {
                                GUI.backgroundColor = mUserColor;
                                break;
                            }

                        case Direction.IncomingUser:
                            {
                                GUI.backgroundColor = mUserColor;
                                break;
                            }

                        case Direction.Nowhere:
                            {
                                GUI.backgroundColor = mOtherEventColor;
                                break;
                            }

                        default:
                            {
                                GUI.backgroundColor = Color.clear;
                                break;
                            }
                    }

                    // Draw Event
                    EditorGUILayout.BeginHorizontal();

                    // Draw Event label
                    string logMessage = "";
                    if (log[i].Repeats > 1)
                    {
                        logMessage = log[i].Message + " x" + log[i].Repeats;
                    }
                    else
                    {
                        logMessage = log[i].Message;
                    }

                    if (log[i].TargetObject == null)
                    {
                        GUILayout.Box(logMessage, entryStyle, GUILayout.ExpandWidth(true));
                    }
                    else
                    {
                        if (GUILayout.Button(logMessage, entryStyle, GUILayout.ExpandWidth(true)))
                        {
                            Selection.SetActiveObjectWithContext(log[i].TargetObject, log[i].TargetObject);
                        }
                    }

                    // Draw Arrow
                    if (log[i].Direction == Direction.OutgoingProtocol || log[i].Direction == Direction.OutgoingUser)
                    {
                        DrawArrow(arrowStyle, GUILayoutUtility.GetLastRect().yMax, true);
                    }
                    else if (log[i].Direction == Direction.IncomingProtocol || log[i].Direction == Direction.IncomingUser)
                    {
                        DrawArrow(arrowStyle, GUILayoutUtility.GetLastRect().yMax, false);
                    }
                    EditorGUILayout.EndHorizontal();

                    GUI.backgroundColor = new Color(0, 0, 0, 0);
                }
            }
            EditorGUILayout.EndScrollView();

            GUI.backgroundColor = oldColor;
        }

        private void DrawArrow(GUIStyle style, float arrowHeight, bool pointRight)
        {
            Rect last = GUILayoutUtility.GetLastRect();

            if (pointRight)
            {
                Rect arrow = new Rect(
                (last.x + last.width) - ARROW_HEIGHT - 3.0f,
                (last.y + (last.height * 0.5f)) - (ARROW_HEIGHT * 0.5f),
                ARROW_HEIGHT,
                ARROW_HEIGHT);

                GUI.DrawTexture(arrow, Tools.arrowR);
            }
            else
            {
                Rect arrow = new Rect(
                last.x + 3.0f,
                (last.y + (last.height * 0.5f)) - (ARROW_HEIGHT * 0.5f),
                ARROW_HEIGHT,
                ARROW_HEIGHT);

                GUI.DrawTexture(arrow, Tools.arrowR);
            }
        }

        private void Update()
        {
            if (EditorApplication.isPlaying)
            {
                if (mFinishedSetup)
                    return;

                mRuntime.Start();

                // Hookup events per client
                foreach (Trinity client in FindObjectsOfType<Trinity>())
                {
                    mLogEntries.Add(new List<LoggedEvent>());
                    HookupEvents(client);
                    OnStarted(client);
                }

                mFinishedSetup = true;
            }
            else
            {
                if (mFinishedSetup)
                {
                    mRuntime.Stop();
                    mRuntime.Reset();
                    mFinishedSetup = false;
                }
            }
        }

        private void HookupEvents(Trinity client)
        {
            if (client != null)
            {
                client.Connected.AddListener(OnConnected);
                client.ConnectionLost.AddListener(OnConnectionLost);

                client.JoinedSession.AddListener(OnSessionJoined);
                client.LeftSession.AddListener(OnSessionLeft);
                client.SessionClosed.AddListener(OnSessionClosed);
                client.OtherJoined.AddListener(OnOtherJoined);
                client.OtherLeft.AddListener(OnOtherLeft);

                client.PacketSent.AddListener(OnPacketSent);
                client.PacketRouted.AddListener(OnPacketRouted);
                client.LockRequested.AddListener(OnLockRequested);
                client.LockAquired.AddListener(OnLockAquired);
                client.ForceSynced.AddListener(OnForceSynced);
            }
        }

        private void LogEvent(Trinity origin, Direction direction, string message, GameObject targetObject = null, Type syncType = null)
        {
            if (mLogEntries[origin.DevClientIndex].Count > 1)
            {
                LoggedEvent Lastlog = mLogEntries[origin.DevClientIndex].Last();
                long repeatDelta = mRuntime.ElapsedMilliseconds - Lastlog.LastRepeatTime;
                if (repeatDelta < COLLAPSE_UNDER_MS &&
                    message == Lastlog.Message &&
                    direction == Lastlog.Direction)
                {
                    // Event is repeat
                    Lastlog.Repeats++;
                    Lastlog.LastRepeatTime = mRuntime.ElapsedMilliseconds;
                    Repaint();
                    return;
                }
            }

            mLogEntries[origin.DevClientIndex].Add(
                new LoggedEvent
                {
                    Direction = direction,
                    Message = message,
                    Origin = origin,
                    TimeStamp = mRuntime.ElapsedMilliseconds,
                    LastRepeatTime = mRuntime.ElapsedMilliseconds,
                    TargetObject = targetObject == null ? null : targetObject,
                    SyncType = syncType == null ? null : syncType,
                }
            );

            if (syncType != null && !mLogFilter.Any(f => f.Type == syncType))
            {
                mLogFilter.Add(
                    new FilterType
                    {
                        Selected = true,
                        Type = syncType,
                    });
            }

            // Scroll to bottom after adding a new event
            mVisualScrollPos.y += 70.0f;
            Repaint();
        }

        private void OnStarted(Trinity origin)
        {
            LogEvent(origin, Direction.Nowhere, "Started");
        }

        private void OnConnected(Trinity origin, IDevice device, bool outgoing)
        {
            if (outgoing)
            {
                LogEvent(origin, Direction.IncomingProtocol, "Connected to " + device.UserName);
            }
            else
            {
                LogEvent(origin, Direction.OutgoingProtocol, "Connected to " + device.UserName);
            }
        }

        private void OnConnectionLost(Trinity origin, IDevice device, bool outgoing)
        {
            if (outgoing)
            {
                LogEvent(origin, Direction.OutgoingProtocol, "Lost connection to " + device.UserName);
            }
            else
            {
                LogEvent(origin, Direction.IncomingProtocol, "Lost connection to " + device.UserName);
            }
        }

        private void OnSessionJoined(Trinity origin, Session session, IDevice device, ushort id)
        {
            LogEvent(origin, Direction.OutgoingProtocol, "Joined session");
        }

        private void OnSessionLeft(Trinity origin, Session session, IDevice device)
        {
            LogEvent(origin, Direction.Nowhere, "Left session");
        }

        private void OnSessionClosed(Trinity origin, Session session, ushort reason)
        {
            LogEvent(origin, Direction.IncomingProtocol, "Session Closed: " + (ResponseCode)reason);
        }

        private void OnOtherJoined(Trinity origin, Session session, ushort userId, string userName)
        {
            LogEvent(origin, Direction.IncomingProtocol, userName + " joined the session");
        }

        private void OnOtherLeft(Trinity origin, Session session, ushort userId, string userName)
        {
            LogEvent(origin, Direction.IncomingProtocol, userName + " left the session");
        }

        private void OnPacketSent(Trinity origin, Synchronizable synchronizable)
        {
            LogEvent(origin, Direction.OutgoingUser, "Synchronized " + synchronizable.GetType() + " on " + synchronizable.gameObject.name, synchronizable.gameObject, synchronizable.GetType());
        }

        private void OnPacketRouted(Trinity origin, Synchronizable synchronizable)
        {
            LogEvent(origin, Direction.IncomingUser, "Recieved data for " + synchronizable.GetType() + " on " + synchronizable.gameObject.name, synchronizable.gameObject, synchronizable.GetType());
        }

        private void OnLockRequested(Trinity origin, Synchronizable synchronizable)
        {
            LogEvent(origin, Direction.OutgoingUser, "Requested lock for " + synchronizable.GetType());
        }

        private void OnLockAquired(Trinity origin, Synchronizable synchronizable)
        {
            LogEvent(origin, Direction.IncomingUser, "Aquired lock for " + synchronizable.GetType());
        }

        private void OnForceSynced(Trinity origin, ushort requesterUserId)
        {
            LogEvent(origin, Direction.OutgoingProtocol, "Force syncing data");
        }
    }
}