using UnityEngine;
using UnityEditor;

namespace GWOO.Editor.Utils
{
    public static class CustomGUIStyles
    {
        private static GUIStyle m_foldoutStyle;
        private static GUIStyle m_titleStyle;

        public static GUIStyle FoldoutStyle
        {
            get
            {
                if (m_foldoutStyle == null)
                {
                    m_foldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold
                    };
                }
                return m_foldoutStyle;
            }
        }

        public static GUIStyle TitleStyle
        {
            get
            {
                if (m_titleStyle == null)
                {
                    m_titleStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Bold
                    };
                }
                return m_titleStyle;
            }
        }

        public static void DrawSeparator(float height = 1)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.50f));
        }

        public static void DrawCustomFoldout(ref bool foldout, string title, float sectionSpace = 10)
        {
            EditorGUILayout.Space(sectionSpace);
            DrawSeparator();
        
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.15f));
            foldout = EditorGUI.Foldout(rect, foldout, " --- " + title, true, FoldoutStyle);
            EditorGUILayout.Space(sectionSpace/2);
        }

        public static void DrawCustomToggleFoldout(ref bool foldout, SerializedProperty toggleProperty, string title, float sectionSpace = 10)
        {
            EditorGUILayout.Space(sectionSpace);
            DrawSeparator();

            float buttonWidth = 20f;

            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.15f));

            Rect toggleRect = new(rect.x + 5, rect.y, buttonWidth, rect.height);
            Rect foldoutRect = new(rect.x, rect.y, rect.width, rect.height);
            Rect labelRect = new(rect.x + 5 + buttonWidth, rect.y, rect.width - 45f, rect.height);

            bool toggleClicked = toggleRect.Contains(Event.current.mousePosition);
            if (!toggleClicked)
            {
                foldout = EditorGUI.Foldout(foldoutRect, foldout, GUIContent.none, true, FoldoutStyle);
            }

            EditorGUI.PropertyField(toggleRect, toggleProperty, GUIContent.none);
            EditorGUI.LabelField(labelRect, title, TitleStyle);

            EditorGUILayout.Space(sectionSpace/2);
        }
    }
}