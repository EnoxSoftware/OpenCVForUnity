using UnityEditor;
using UnityEngine;

namespace OpenCVForUnityExample.Documentation.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="ExampleDocumentationViewer"/>.
    /// </summary>
    [CustomEditor(typeof(ExampleDocumentationViewer))]
    public class ExampleDocumentationViewerEditor : UnityEditor.Editor
    {
        private const float MAX_PREVIEW_HEIGHT = 300f;
        private Vector2 _scrollPosition;

        public override void OnInspectorGUI()
        {
            var viewer = (ExampleDocumentationViewer)target;
            string documentationText = viewer.DocumentationText;

            EditorGUILayout.LabelField("Example Documentation", EditorStyles.boldLabel);

            if (string.IsNullOrEmpty(documentationText))
            {
                EditorGUILayout.HelpBox(
                    "Documentation text is not assigned or empty. Run Bake And Setup Example Documentation before release.",
                    MessageType.Warning);
            }
            else
            {
                GUIStyle style = EditorStyles.textField;
                style.wordWrap = true;
                float width = Mathf.Max(1f, EditorGUIUtility.currentViewWidth - 40f);
                float contentHeight = style.CalcHeight(new GUIContent(documentationText), width);
                contentHeight = Mathf.Max(contentHeight, EditorGUIUtility.singleLineHeight);
                Rect outerRect = GUILayoutUtility.GetRect(width, MAX_PREVIEW_HEIGHT);
                var viewRect = new Rect(0f, 0f, width, contentHeight);

                _scrollPosition = GUI.BeginScrollView(outerRect, _scrollPosition, viewRect, false, false);
                EditorGUI.SelectableLabel(new Rect(0f, 0f, width, contentHeight), documentationText, style);
                GUI.EndScrollView();
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
