using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Alteruna.Trinity.Development
{
    public class PlayroomFinder : EditorWindow
    {
        private const int RADAR_DIAMETER = 200;
        private const int RADAR_NUM_RINGS = 3;
        private const int RADAR_SPIN_SPEED = 10;
        private const int RADAR_FADE_RESOLUTION = 35;
        private const float RADAR_FADE_LENGTH = Mathf.PI * 0.75f;
        private const float RADAR_DOT_FADE_SPEED = 0.8f;
        private const float RADAR_DOT_APPEAR_OFFSET = 0.3f;
        private const float RADAR_DOT_SIZE = 25.0f;
        private const int RADAR_DOT_FONT_SIZE = 10;
        private const float RADAR_DOT_MIN_DISTANCE = 0.2f;
        private const float RADAR_DOT_MAX_DISTANCE = 0.8f;

        private const int LIST_TITLE_HEIGHT = 25;
        private const int LIST_ENTRY_HEIGHT = 20;
        private const int LIST_ENTRY_BUTTON_WIDTH = 60;

        private bool mFinishedSetup = false;
        private Trinity mClient;
        private List<Playroom> mAvailableRooms = new List<Playroom>();
        private List<float> mRoomRadarAngles = new List<float>();
        private List<float> mRoomRadarDistances = new List<float>();
        private Vector2 mScrollPos;
        private float mScanlineAngle;

        [MenuItem("Trinity/Playroom Finder")]
        static void ShowWindow()
        {
            var window = GetWindow<PlayroomFinder>();
            window.titleContent = new GUIContent("Trinity Playrooms");
            window.autoRepaintOnSceneChange = true;
            window.minSize = new Vector2(RADAR_DIAMETER + 10, 300);
            window.Show();
        }

        private void OnNewAvailableDevice(Trinity origin, IDevice device)
        {
            if (mClient != null)
            {
                mClient.GetLocalPlayrooms(mAvailableRooms);
                PlaceRoomsOnRadar();
            }
        }

        private void OnLostAvailableDevice(Trinity origin, IDevice device)
        {
            if (mClient != null)
            {
                mClient.GetLocalPlayrooms(mAvailableRooms);
                PlaceRoomsOnRadar();
            }
        }

        private void PlaceRoomsOnRadar()
        {
            mRoomRadarAngles.Clear();
            for (int i = 0; i < mAvailableRooms.Count; i++)
            {
                mRoomRadarAngles.Add(Random.Range(0.0f, 2.0f * Mathf.PI));
            }
            mRoomRadarDistances.Clear();
            for (int i = 0; i < mAvailableRooms.Count; i++)
            {
                mRoomRadarDistances.Add(Random.Range(RADAR_DOT_MIN_DISTANCE, RADAR_DOT_MAX_DISTANCE));
            }
        }

        private void Start()
        {
            mClient = FindObjectOfType<Trinity>();
            if (mClient != null)
            {
                mClient.NewAvailableDevice.AddListener(OnNewAvailableDevice);
                mClient.LostAvailableDevice.AddListener(OnLostAvailableDevice);
            }
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlaying)
            {
                if (!mFinishedSetup)
                {
                    Start();
                    mFinishedSetup = true;
                }
            }
            else
            {
                mFinishedSetup = false;
            }

            DrawRadar();
            DrawCreateButton();
            DrawList();
            Repaint();
        }

        private void DrawRadar()
        {
            Color oldContentColor = GUI.contentColor;
            Color oldBGColor = GUI.backgroundColor;

            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(RADAR_DIAMETER));
            EditorGUI.DrawRect(rect, Color.black);

            UnityEditor.Handles.color = Color.green;
            Vector3 center = new Vector3(rect.xMin + (rect.width * 0.5f), rect.yMax - (rect.height * 0.5f), 0);

            // Draw lines
            Vector3 from = new Vector3(center.x - (RADAR_DIAMETER * 0.5f), rect.yMin + (RADAR_DIAMETER * 0.5f), 0);
            Vector3 to = new Vector3(center.x + (RADAR_DIAMETER * 0.5f), from.y, 0);
            UnityEditor.Handles.DrawLine(from, to);

            from = new Vector3(center.x, rect.yMin, 0);
            to = new Vector3(center.x, rect.yMax, 0);
            UnityEditor.Handles.DrawLine(from, to);

            // Draw rings
            Vector3 normal = Vector3.forward;
            for (int i = 1; i <= RADAR_NUM_RINGS; i++)
            {
                UnityEditor.Handles.DrawWireDisc(center, normal, ((RADAR_DIAMETER * 0.5f) / RADAR_NUM_RINGS) * i);
            }

            // Draw scanline
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                mScanlineAngle += RADAR_SPIN_SPEED * Time.fixedDeltaTime;
            }

            float sin = Mathf.Sin(mScanlineAngle * Mathf.Deg2Rad);
            float cos = Mathf.Cos(mScanlineAngle * Mathf.Deg2Rad);

            Vector3 outer = new Vector3(cos, sin, 0) * (RADAR_DIAMETER * 0.5f);
            UnityEditor.Handles.DrawLine(center, center + outer);

            // Draw dots
            GUIStyle dotStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };
            dotStyle.normal.background = (Texture2D)Tools.dotTexture;
            dotStyle.fontSize = RADAR_DOT_FONT_SIZE;
            GUI.backgroundColor = Color.green;

            Vector3 dotOuter = outer;
            for (int i = 0; i < mAvailableRooms.Count; i++)
            {
                float scanDistance = (mRoomRadarAngles[i] - (mScanlineAngle * Mathf.Deg2Rad)) % (2.0f * Mathf.PI) + RADAR_DOT_APPEAR_OFFSET;
                sin = Mathf.Sin((mRoomRadarAngles[i]));
                cos = Mathf.Cos((mRoomRadarAngles[i]));
                outer = new Vector3(cos, sin, 0);

                GUI.contentColor = new Color(0.0f, 0.0f, 0.0f, 1.0f / -(scanDistance * RADAR_DOT_FADE_SPEED));
                GUI.backgroundColor = new Color(0.0f, 1.0f, 0.0f, 1.0f / -(scanDistance * RADAR_DOT_FADE_SPEED));

                float distance = mRoomRadarDistances[i] * (RADAR_DIAMETER * 0.5f);
                Vector2 dotPos = center + (outer * distance);
                Vector2 dotSize = new Vector2(RADAR_DOT_SIZE * 1.15f, RADAR_DOT_SIZE);
                if (GUI.Button(new Rect(dotPos - (dotSize * 0.5f), dotSize), i.ToString(), dotStyle))
                {
                    if (EditorApplication.isPlaying)
                    {
                        mClient.JoinRemotePlayroom(mAvailableRooms[i].Host, 0);
                    }
                }
            }

            // Draw scanline fade
            float arcAngle = (RADAR_FADE_LENGTH / (float)RADAR_FADE_RESOLUTION) * Mathf.Rad2Deg;
            float angleOffset = 0;
            Vector3 arcFrom = outer;
            float fade = 0.98f;
            for (int i = 0; i <= RADAR_FADE_RESOLUTION; i++)
            {
                if (fade < 0.1f)
                    break;

                angleOffset = (arcAngle * i);

                fade = fade * fade;
                UnityEditor.Handles.color = new Color(0, 0.8f, 0, fade);
                sin = Mathf.Sin((mScanlineAngle - angleOffset) * Mathf.Deg2Rad);
                cos = Mathf.Cos((mScanlineAngle - angleOffset) * Mathf.Deg2Rad);
                arcFrom = new Vector3(cos, sin, 0);
                UnityEditor.Handles.DrawSolidArc(center, normal, arcFrom, -arcAngle, RADAR_DIAMETER * 0.5f);
            }

            GUI.contentColor = oldContentColor;
            GUI.backgroundColor = oldBGColor;
        }

        private void DrawCreateButton()
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, margin = new RectOffset(5, 5, 0, 2) };

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Start playroom", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(LIST_ENTRY_HEIGHT)))
                {
                    mClient?.LeavePlayroom();
                    mClient?.JoinOwnPlayroom();
                }
                if (GUILayout.Button("Leave playroom", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(LIST_ENTRY_HEIGHT)))
                {
                    mClient?.LeavePlayroom();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawList()
        {
            GUIStyle entryNumberStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, margin = new RectOffset(5, 0, 0, 2) };
            GUIStyle entryStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 0, 2) };
            GUIStyle entryButtonStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 5, 0, 2) };
            entryButtonStyle.normal.background = Texture2D.grayTexture;

            GUILayout.Box("Available Playrooms", GUILayout.ExpandWidth(true), GUILayout.Height(LIST_TITLE_HEIGHT));

            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            for (int i = 0; i < mAvailableRooms.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Box(i.ToString(), entryNumberStyle, GUILayout.Width(30), GUILayout.Height(LIST_ENTRY_HEIGHT));
                GUILayout.Box(mAvailableRooms[i].Name, entryStyle, GUILayout.ExpandWidth(true), GUILayout.Height(LIST_ENTRY_HEIGHT));
                if (GUILayout.Button("JOIN", entryButtonStyle, GUILayout.Width(LIST_ENTRY_BUTTON_WIDTH), GUILayout.Height(LIST_ENTRY_HEIGHT)))
                {
                    if (EditorApplication.isPlaying)
                    {
                        mClient?.LeavePlayroom();
                        mClient?.JoinRemotePlayroom(mAvailableRooms[i].Host, 0);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}