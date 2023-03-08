using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEngine;
using util;

namespace Editor
{
    [CustomEditor(typeof(TagHolder))]
    public class TagHolderInspector : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            var tagHolder = (TagHolder) target;
            
            var flags = 0;
            var options = GetTags();
            var selectedOptions = new List<SharedTag>();

            if (tagHolder.initialTags != null)
            {
                selectedOptions.AddRange(tagHolder.initialTags);
            }

            // load flags
            for (int i = 0; i < options.Count; i++)
            {
                if (selectedOptions.Contains(options[i])) flags |= (1 << i);
            }
            
            selectedOptions.Clear();
            flags = EditorGUILayout.MaskField ("Object Flags", flags, options.Select(x => x.ToString()).ToArray());
            
            // save flags
            for (int i = 0; i < options.Count; i++)
            {
                if ((flags & (1 << i )) == (1 << i) ) selectedOptions.Add(options[i]);
            }
            
            tagHolder.initialTags = selectedOptions.ToArray();
            
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
            SaveChanges();
        }
    
        private static List<SharedTag> GetTags() {
            var tags = new List<SharedTag>();
            
            // get list of enum field names
            var tagNames = System.Enum.GetNames(typeof(SharedTag));
            foreach (var tagName in tagNames)
            {
                tags.Add((SharedTag) System.Enum.Parse(typeof(SharedTag), tagName));
            }

            return tags;
        }
    }
}