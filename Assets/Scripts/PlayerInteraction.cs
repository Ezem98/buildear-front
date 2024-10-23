using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask interactionMask; 
    private Camera camera;
    private IInteractable _currentInteractable;
    private IInteractable _lastInteractable;
    private Ray _ray;
    private RaycastHit _hit;
    private bool _isInteractionLocked;
    private void Awake(){
        camera = Camera.main;
    }
    private void LateUpdate()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (_currentInteractable != null)
        {
            _currentInteractable.Interact();
        }
    }

    public void LockInteraction(bool value)
    {
        if (value)
        {
            _currentInteractable.Highlight(false);
            _currentInteractable = null;
            _lastInteractable = null;
        }
        _isInteractionLocked = value;
    }
    void FixedUpdate()
    {
        if (_isInteractionLocked) {return;}
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out _hit, interactionMask);
        if (_hit.transform == null) { _currentInteractable = null; }
        else {
            _currentInteractable = _hit.collider.GetComponent<IInteractable>();
        }
        if (_currentInteractable == null)
        {
            if (_lastInteractable!=null)
            {
                _lastInteractable.Highlight(false);
                _lastInteractable = null;
            }
            return;
        }
            
        if (_lastInteractable != null)
        {
            if (_lastInteractable != _currentInteractable)
            {
                _lastInteractable.Highlight(false);
                _lastInteractable = null;
            }
        }
        
        _lastInteractable = _currentInteractable;
        _currentInteractable.Highlight(true);
    }
}
