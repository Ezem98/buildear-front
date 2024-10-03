using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonsHandler : MonoBehaviour
{
    [SerializeField] private GameObject ModelActions;
    [SerializeField] private GameObject ResizeActions;
    [SerializeField] private GameObject RotateActions;
    [SerializeField] private ActionManager ActionManager;
    [SerializeField] private CanvasManager CanvasManager;
    [SerializeField] private Transform objectToRotate; // Añade esta línea para especificar el objeto a rotar
    [SerializeField] private float rotationSpeed = 45f; // Velocidad de rotación en grados por segundo

    void OnEnable(){
        
        if (ActionManager != null)
        {
            ActionManager.OnRotateRightAction += RotateRightAction;
        }
    }

        void OnDisable()
    {
        // Desuscribirse del evento de rotación para evitar llamadas a métodos de objetos destruidos
        if (ActionManager != null)
        {
            ActionManager.OnRotateRightAction -= RotateRightAction;
        }
    }

    public void ShowModelActions(bool show)
    {
        ModelActions.SetActive(show);
    }

    public void ToggleResizeActions()
    {
        ResizeActions.SetActive(!ResizeActions.activeSelf);
        Debug.Log("Estoy en ToggleResizeActions");
    }

    public void ActivateModelCanvas()
    {
        Debug.Log("Entre al activateModelCanvas");
        CanvasManager?.ActivateModelCanvas();
    }

    public void HideModelCanvas()
    {
        Debug.Log("Entre al activateModelCanvas");
        CanvasManager?.HideCanvas();
    }
    
    public void RotateRightAction(){
        Debug.Log("Entre al rotate del buttonshandler");
        if (objectToRotate != null)
        {
            // Rota el objeto 45 grados alrededor del eje Y
            objectToRotate.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        else
        {
            Debug.LogWarning("No se ha asignado un objeto para rotar.");
        }
    }
}
