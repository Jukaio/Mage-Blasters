using UnityEngine;
using UnityEditor;

namespace Alteruna
{
    namespace Trinity
    {
        [CustomEditor(typeof(Trinity))]
        public class TrinityEditor : Editor
        {
            public GUISkin CustomSkin;
            protected Trinity mTarget;

            private void Awake()
            {
                mTarget = (Trinity)target;
            }

            public override void OnInspectorGUI()
            {
                // Display Logo
                GUIStyle centered = new GUIStyle(GUI.skin.label);
                centered.alignment = TextAnchor.MiddleCenter;
                centered.fontStyle = FontStyle.Bold;
                centered.fontSize = 18;
                EditorGUILayout.LabelField("Trinity", centered, GUILayout.ExpandWidth(true), GUILayout.Height(30));

                // Display client name
                centered.alignment = TextAnchor.MiddleCenter;
                centered.fontStyle = FontStyle.Normal;
                centered.fontSize = 13;
                centered.padding = new RectOffset(0, 0, 0, 0);
                EditorGUILayout.LabelField("Username: " + mTarget.ClientName, centered, GUILayout.ExpandWidth(true), GUILayout.Height(15));

                // Display playroom status
                if (mTarget.InPlayroom)
                {
                    EditorGUILayout.LabelField("In Playroom | User Index: " + mTarget.UserIndex, centered, GUILayout.ExpandWidth(true), GUILayout.Height(15));
                }
                else
                {
                    EditorGUILayout.LabelField("Not in a Playroom", centered, GUILayout.ExpandWidth(true), GUILayout.Height(15));
                }

                GUILayout.Space(5);

                // Display application ID
                EditorGUILayout.LabelField("Application ID", centered, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                if (mTarget.ApplicationID == System.Guid.Empty || mTarget.ApplicationID == null)
                {
                    if (mTarget.AppIDString == "")
                    {
                        mTarget.AppIDString = EditorGUILayout.TextField("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        if (GUILayout.Button("Generate New Application ID", GUILayout.ExpandHeight(true)))
                        {
                            mTarget.ApplicationID = System.Guid.NewGuid();
                            mTarget.AppIDString = mTarget.ApplicationID.ToString();
                        }
                    }
                    else
                    {
                        mTarget.AppIDString = EditorGUILayout.TextField(mTarget.AppIDString, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    }
                }
                else
                {
                    mTarget.AppIDString = EditorGUILayout.TextField(mTarget.ApplicationID.ToString(), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                }

                GUILayout.Space(5);

                SerializedProperty iter = serializedObject.GetIterator();
                iter.NextVisible(true);
                while (iter.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(iter, true);
                }

                this.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
