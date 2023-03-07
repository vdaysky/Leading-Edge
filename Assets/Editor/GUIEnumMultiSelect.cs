/// <summary>
/// Thanks to JohnnyA.
/// https://forum.unity.com/threads/any-way-to-create-popup-or-list-box-that-allows-multiple-selections.333392/#post-2403105
/// </summary>

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
 
public class GUIEnumMultiSelect : EditorWindow {
    [MenuItem("Examples/Mask Field Usage")]
    public static void Init() {
        GUIEnumMultiSelect window = GetWindow<GUIEnumMultiSelect> ();
        window.Show();
    }
 
    int flags = 0;
    string[] options = new string[]{"CanJump", "CanShoot", "CanSwim"};
    void OnGUI() {
        flags = EditorGUILayout.MaskField ("Player Flags", flags, options);
        List<string> selectedOptions = new List<string>();
        for (int i = 0; i < options.Length; i++)
        {
            if ((flags & (1 << i )) == (1 << i) ) selectedOptions.Add(options[i]);
        }
        if (GUILayout.Button ("Print Options"))
        {
            foreach (string o in selectedOptions) Debug.Log (o);
        }
    }
}