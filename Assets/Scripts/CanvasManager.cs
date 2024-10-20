using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.ComponentModel.Design;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using OpenAI;
using OpenAI.Threads;
using System.Linq;


public class CanvasManager : MonoBehaviour
{

    [SerializeField] private GameObject modelActions;
    [SerializeField] private GameObject resizeActions;
    [SerializeField] private GameObject rotateActions;
    [SerializeField] private GameObject moveActions;
    [SerializeField] private ActionManager ActionManager;
    [SerializeField] private GameObject objectReference;
    [SerializeField] private float rotationSpeed = 45f;
    private GameObject objectCopiedReference;
    private Quaternion previousRotation;
    private Vector3 previousPosition;
    List<string> menu = new() { "modelActions", "rotateActions", "moveActions" };
    private string activeMenu;
    private bool isRotatingRight = false;
    private bool isRotatingLeft = false;
    private bool isMovingRight = false;
    private bool isMovingLeft = false;
    private bool isMovingForward = false;
    private bool isMovingBack = false;
    public string GetActiveMenu()
    {
        return activeMenu;
    }
    public void SetActiveMenu(string value)
    {
        activeMenu = value;
    }

    void Start()
    {
        ActionManager.OnResizeAction += ActivateResizeCanvas;
        ActionManager.OnAceptAction += ActivateModelCanvas;
        ActionManager.OnMoveAction += ActivateMoveCanvas;
        ActionManager.OnCancelAction += CancelAction;
        ActionManager.OnRotateRightAction += RotateRightAction;
        ActionManager.OnRotateLeftAction += RotateLeftAction;
        ActionManager.OnMoveRightAction += MoveRightAction;
        ActionManager.OnMoveLeftAction += MoveLeftAction;
        ActionManager.OnMoveBackAction += MoveBackAction;
        ActionManager.OnMoveForwardAction += MoveForwardAction;
        ActionManager.OnHideCanvas += HideCanvas;
        ActionManager.OnChatAction += StartChat;
    }

    public void ActivateModelCanvas()
    {
        modelActions.transform.GetChild(0).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(1).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(2).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(3).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(4).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(5).transform.DOScale(new Vector3(1, 1, 1), 0.3f);

        if (activeMenu == "rotateActions")
        {
            rotateActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            rotateActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            rotateActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            rotateActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            rotateActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        }
        if (activeMenu == "moveActions")
        {
            moveActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
            moveActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
            moveActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
            moveActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
            moveActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
            moveActions.transform.GetChild(5).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
            moveActions.transform.GetChild(6).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
        }
        activeMenu = menu[0]; // modelActions
    }

    public void ActivateResizeCanvas()
    {
        resizeActions.transform.GetChild(0).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        resizeActions.transform.GetChild(1).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        resizeActions.transform.GetChild(2).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        resizeActions.transform.GetChild(3).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        resizeActions.transform.GetChild(4).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        resizeActions.transform.GetChild(5).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        resizeActions.transform.GetChild(6).transform.DOScale(new Vector3(1, 1, 1), 0.5f);
        resizeActions.transform.GetChild(7).transform.DOScale(new Vector3(1, 1, 1), 0.5f);

        modelActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(5).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        activeMenu = menu[1]; // resizeActions
    }

    public void ActivateRotateCanvas()
    {
        previousRotation = objectReference.transform.rotation;
        rotateActions.transform.GetChild(0).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        rotateActions.transform.GetChild(1).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        rotateActions.transform.GetChild(2).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        rotateActions.transform.GetChild(3).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        rotateActions.transform.GetChild(4).transform.DOScale(new Vector3(1, 1, 1), 0.3f);

        modelActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(5).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        activeMenu = menu[2]; // rotateActions
    }

    public void ActivateMoveCanvas()
    {
        previousPosition = objectReference.transform.position;
        moveActions.transform.GetChild(0).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        moveActions.transform.GetChild(1).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        moveActions.transform.GetChild(2).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        moveActions.transform.GetChild(3).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        moveActions.transform.GetChild(4).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        moveActions.transform.GetChild(5).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        moveActions.transform.GetChild(6).transform.DOScale(new Vector3(1, 1, 1), 0.3f);

        modelActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        modelActions.transform.GetChild(5).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        activeMenu = menu[3]; // moveActions
    }

    public void HideCanvas()
    {
        if (activeMenu == "modelActions")
        {
            modelActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            modelActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            modelActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            modelActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            modelActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            modelActions.transform.GetChild(5).transform.DOScale(Vector3.zero, 0.3f);
        }
        if (activeMenu == "rotateActions")
        {
            rotateActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            rotateActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            rotateActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            rotateActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            rotateActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
        }
        if (activeMenu == "moveActions")
        {
            moveActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            moveActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            moveActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            moveActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            moveActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            moveActions.transform.GetChild(5).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
            moveActions.transform.GetChild(6).transform.DOScale(new Vector3(0, 0, 0), 0.3f);
        }
        activeMenu = menu[0]; // modelActions
    }

    public void RotateRightAction()
    {
        if (objectReference != null)
        {
            isRotatingRight = true;
            isRotatingLeft = false;
            objectReference.transform.Rotate(Vector3.down, rotationSpeed * Time.deltaTime);
        }
    }

    public void RotateLeftAction()
    {
        if (objectReference != null)
        {
            isRotatingRight = false;
            isRotatingLeft = true;
            objectReference.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    public void RotateLeftActionStop()
    {
        isRotatingLeft = false;
    }

    public void RotateRightActionStop()
    {
        isRotatingRight = false;
    }

    public void MoveLeftActionStop()
    {
        isMovingLeft = false;
    }

    public void MoveRightActionStop()
    {
        isMovingRight = false;
    }

    public void MoveForwardActionStop()
    {
        isMovingForward = false;
    }

    public void MoveBackActionStop()
    {
        isMovingBack = false;
    }

    public void CopyObject()
    {
        Vector3 direction = objectReference.transform.forward;
        Vector3 newPosition = objectReference.transform.position - direction;
        objectCopiedReference = Instantiate(objectReference, newPosition, objectReference.transform.rotation);
        var objectCopiedCanvas = objectCopiedReference.GetComponent<CanvasManager>();
        HideCanvas();
        objectCopiedCanvas.ActivateModelCanvas();
    }

    public void MoveRightAction()
    {
        SetMoveFlag("right");
        objectReference.transform.Translate(Vector3.right * Time.deltaTime);
    }
    public void MoveLeftAction()
    {
        SetMoveFlag("left");
        objectReference.transform.Translate(Vector3.left * Time.deltaTime);
    }
    public void MoveBackAction()
    {
        SetMoveFlag("back");
        objectReference.transform.Translate(Vector3.back * Time.deltaTime);
    }
    public void MoveForwardAction()
    {
        SetMoveFlag("forward");
        objectReference.transform.Translate(Vector3.forward * Time.deltaTime);
    }

    public void SetMoveFlag(string direction)
    {
        switch (direction)
        {
            case "right":
                isMovingRight = true;
                isMovingLeft = false;
                isMovingBack = false;
                isMovingForward = false;
                break;
            case "left":
                isMovingRight = false;
                isMovingLeft = true;
                isMovingBack = false;
                isMovingForward = false;
                break;
            case "back":
                isMovingRight = false;
                isMovingLeft = false;
                isMovingBack = true;
                isMovingForward = false;
                break;
            case "forward":
                isMovingRight = false;
                isMovingLeft = false;
                isMovingBack = false;
                isMovingForward = true;
                break;
            default:
                break;
        }
    }

    public void CancelAction()
    {
        if (activeMenu == "rotateActions")
        {
            objectReference.transform.rotation = previousRotation; //Funciona
        }
        if (activeMenu == "moveActions")
        {
            objectReference.transform.position = previousPosition; //Funciona
        }
        ActivateModelCanvas();
    }
    // Update is called once per frame
    public void DestroyObject()
    {
        HideCanvas();
        Destroy(objectReference);
    }
    void Update()
    {
        if (isRotatingRight)
        {
            RotateRightAction();
        }
        if (isRotatingLeft)
        {
            RotateLeftAction();
        }
        if (isMovingRight)
        {
            MoveRightAction();
        }
        if (isMovingLeft)
        {
            MoveLeftAction();
        }
        if (isMovingForward)
        {
            MoveForwardAction();
        }
        if (isMovingBack)
        {
            MoveBackAction();
        }
    }

    public void StartChat()
    {
        BuildController.Instance.StartChat();
    }
}
