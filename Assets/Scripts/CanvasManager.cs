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
    // [SerializeField] private Resize resize;
    // [SerializeField] private Transform objectTransform;
    [SerializeField] private float rotationSpeed = 45f;
    private GameObject objectCopiedReference;
    private Quaternion previousRotation;
    private Vector3 previousPosition;
    // private GameObject pivotContainer;
    // public float lengthToAdd = 0.01f;
    // private float resizeAmount = 0.01f;
    List<string> menu = new() { "modelActions", "resizeActions", "rotateActions", "moveActions" };
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
    }

    public void ActivateModelCanvas()
    {
        modelActions.transform.GetChild(0).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(1).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(2).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(3).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(4).transform.DOScale(new Vector3(1, 1, 1), 0.3f);
        modelActions.transform.GetChild(5).transform.DOScale(new Vector3(1, 1, 1), 0.3f);

        if (activeMenu == "resizeActions")
        {
            resizeActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(5).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(6).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(7).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
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
        if (activeMenu == "resizeActions")
        {
            resizeActions.transform.GetChild(0).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(1).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(2).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(3).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(4).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(5).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(6).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
            resizeActions.transform.GetChild(7).transform.DOScale(new Vector3(0, 0, 0), 0.5f);
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

    public void ScaleRight()
    {
        // if (objectTransform != null)
        // {


        // if (pivotContainer == null)
        // {
        //     pivotContainer = new GameObject("PivotContainer");
        //     pivotContainer.transform.position = objectTransform.position;
        //     objectTransform.SetParent(pivotContainer.transform);
        //     objectTransform.localPosition = new Vector3(-objectTransform.localScale.x / 2, 0, 0);
        // }
        // Vector3 direction = pivotContainer.transform.right;
        // Vector3 directionNormalized = direction.normalized;
        //ScaleContainer(lengthToAdd);
        //Vector3 direction = objectTransform.right;
        //float xDirection = direction.right.x;
        // Vector3 directionNormalized = direction.normalized;
        //pivotContainer.transform.localScale += new Vector3(directionNormalized.x * lengthToAdd, 0, directionNormalized.z * lengthToAdd);
        // transform.localScale += new Vector3(directionNormalized.x * lengthToAdd, 0, directionNormalized.z * lengthToAdd);
        // transform.position += new Vector3(directionNormalized.x * lengthToAdd, 0, directionNormalized.z * lengthToAdd);
        //visualsTransform  = objectReference.transform.GetChild(1);
        // Debug.Log("Entre");
        // var mesh = objectReference.GetComponent<MeshFilter>().sharedMesh.bounds;
        // Debug.Log("BOUNDS: "+mesh);
        //  resize.ResizeOnDirection(resizeAmount);
        // objectTransform.position = new Vector3 (objectTransform.position.x+(resizeAmount / 2), objectTransform.position.y, objectTransform.position.z);
        // objectTransform.localScale += new Vector3 (resizeAmount, 0.0f, 0.0f);
        //Resize(resizeAmount,"x");

        // }
    }

    // void ScaleContainer(float lengthToAdd)
    // {
    //     // Escalar el pivotContainer a lo largo del eje X local
    //     pivotContainer.transform.localScale += new Vector3(lengthToAdd, 0, 0);
    // }

    public void Resize(float amount, string direction)
    {
        if (direction == "x" && transform.position.x >= 0)
        {
            transform.position = new Vector3(transform.position.x + (amount / 2), transform.position.y, transform.position.z);
            transform.localScale = new Vector3(transform.localScale.x + amount, transform.localScale.y, transform.localScale.z);
        }
        if (direction == "x" && transform.position.x < 0)
        {
            transform.position = new Vector3(transform.position.x - (amount / 2), transform.position.y, transform.position.z);
            transform.localScale = new Vector3(transform.localScale.x + amount, transform.localScale.y, transform.localScale.z);
        }
    }

    public void CopyObject()
    {
        Vector3 direction = objectReference.transform.forward;
        Vector3 newPosition = objectReference.transform.position - direction;
        //Debug.Log("ActionManager del objectReference: "+ objectReference.GetComponent<ActionManager>());
        objectCopiedReference = Instantiate(objectReference, newPosition, objectReference.transform.rotation);
        //Debug.Log("ActionManager del objectCopied: "+ objectCopiedReference.GetComponent<ActionManager>());
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
}
