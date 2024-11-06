using UnityEditor;
using GWOO.Editor.Utils;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GWOO.Editor.Attributes
{
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class ObjectEditor : UnityEditor.Editor
    {
        private MethodButtonsDrawer m_methodButtonsDrawer;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            EditorCustomStyles.SetCustomStyleSheet(root);
            
            m_methodButtonsDrawer = new MethodButtonsDrawer(target);
            m_methodButtonsDrawer.DrawButtons(target, root);
            
            return root;
        }
    }
}