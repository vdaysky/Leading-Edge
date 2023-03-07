using System;
using System.Collections.Generic;
using UnityEngine;
using util;


public class TagHolder : MonoBehaviour
{
    public SharedTag[] initialTags;
    private readonly HashSet<SharedTag> _tags = new();

    private void Start()
    {
        foreach (var presetTag in initialTags)
        {
            _tags.Add(presetTag);
        }
    }

    public void AddTag(SharedTag customTag) {
        _tags.Add(customTag);
    }
    
    public IfThen HasTag(SharedTag customTag) {
        return new IfThen(()=>_tags.Contains(customTag));
    }
    
    public void RemoveTag(SharedTag customTag) {
        _tags.Remove(customTag);
    }
}
