using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace GWOO.Editor.Extensions
{
    public static class RecompileScriptsShortcut
    {
        [Shortcut("Custom/Recompile Scripts", KeyCode.R, ShortcutModifiers.Action)]
        public static void RecompileScripts()
        {
            AssetDatabase.Refresh();
            EditorUtility.RequestScriptReload();
            Debug.Log("Recompiled scripts!");
        }
    }
}