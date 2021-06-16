using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour {
    OutlineHelper outline;
    bool outlineVisible = false;

    protected virtual void Start() {
        outline = GetComponent<OutlineHelper>();
        if(outline != null) {
            outline.CreateOutlineObject(new System.Type[] { this.GetType() });
            outline.ShowOutline(false);
        }
    }
    protected virtual void LateUpdate() {
        if(outline == null) return;
        if(!outlineVisible) outline.ShowOutline(false);
        if(outlineVisible) outlineVisible = false;
    }

    public void ShowOutline(bool show) {
        if(outline == null) return;
        outline.ShowOutline(show);
        outlineVisible = outline.isCloneVisible;
    }
    public void UpdateOutline() {
        // set active or not based on model ??
    }
}