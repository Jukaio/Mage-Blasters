using UnityEngine;
using UnityEditor;

namespace Alteruna.Trinity
{
    [CustomEditor(typeof(UniqueID))]
    public class UniqueIDEditor : Editor
    {
        public GUISkin CustomSkin;
        protected UniqueID mTarget;

        private void Awake()
        {
            mTarget = (UniqueID)target;
        }

        public override void OnInspectorGUI()
        {
            GUIStyle centered = new GUIStyle(GUI.skin.label);
            centered.alignment = TextAnchor.MiddleCenter;
            centered.fontStyle = FontStyle.Bold;
            centered.fontSize = 18;
            EditorGUILayout.LabelField("Unique ID", centered, GUILayout.ExpandWidth(true));

            centered.fontStyle = FontStyle.Normal;
            centered.fontSize = 15;

            if (mTarget.UID == System.Guid.Empty || mTarget.UID == null)
            {
                EditorGUILayout.LabelField("" + mTarget.UIDString, centered, GUILayout.ExpandWidth(true));
            }
            else
            {
                EditorGUILayout.LabelField("" + mTarget.UID.ToString(), centered, GUILayout.ExpandWidth(true));
            }
        }
    }
}
