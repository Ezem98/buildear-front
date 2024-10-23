using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertice : MonoBehaviour, IInteractable
{
    private Outline _outline;
    public void Interact()
    {
        transform.parent.gameObject.GetComponent<RulerObjST>().ToggleDelete();
    }

    public void Highlight(bool value)
    {
        _outline.enabled = value;
    }

    void Awake()
    {
        _outline = GetComponent<Outline>();
    }
}
