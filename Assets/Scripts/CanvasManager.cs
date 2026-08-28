using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.ComponentModel.Design;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.UI;
using OpenAI;
using OpenAI.Threads;
using System.Linq;
using Utilities.Extensions;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;


public class CanvasManager : MonoBehaviour, IModelCanvasController
{
    private const string ModelActionsMenu = "modelActions";
    private const string RotateActionsMenu = "rotateActions";
    private const string MoveActionsMenu = "moveActions";
    private const string ResizeActionsMenu = "resizeActions";

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
    private Vector3 previousLocalScale;
    // private GameObject pivotContainer;
    // public float lengthToAdd = 0.01f;
    // private float resizeAmount = 0.01f;
    private string activeMenu;
    private string direction;
    private float resizeAmount = 0.01f;
    private bool isRotatingRight = false;
    private bool isRotatingLeft = false;
    private bool isMovingRight = false;
    private bool isMovingLeft = false;
    private bool isMovingForward = false;
    private bool isMovingBack = false;
    private bool destroyRequested = false;

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
        InstallUiSelectionBlockers();
        BindDeleteButton();
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
        ActionManager.OnSideScale += SideScaleAction;
        ActionManager.OnScaleUp += ScaleUpAction;
        ActionManager.OnDownScaleSide += DownScaleSideAction;
        ActionManager.OnDownScaleUp += DownScaleUpAction;

    }

    private void InstallUiSelectionBlockers()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<ModelUiSelectionBlocker>() == null)
                button.gameObject.AddComponent<ModelUiSelectionBlocker>();
        }
    }

    private void BindDeleteButton()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            bool isDeleteButton = button.name == "Tachito";
            for (int index = 0; !isDeleteButton && index < button.onClick.GetPersistentEventCount(); index++)
                isDeleteButton = button.onClick.GetPersistentMethodName(index) == nameof(DestroyObject);

            if (!isDeleteButton)
                continue;

            button.onClick.RemoveListener(DestroyObject);
            button.onClick.AddListener(DestroyObject);
        }
    }

    public void SideScaleAction()
    {
        ScaleObject("x", "up");
    }
    public void DownScaleSideAction()
    {
        ScaleObject("x", "down");
    }
    public void ScaleUpAction()
    {
        ScaleObject("y", "up");
    }
    public void DownScaleUpAction()
    {
        ScaleObject("y", "down");
    }

    public void ActivateModelCanvas()
    {
        ShowMenu(modelActions);
        HideMenu(resizeActions);
        HideMenu(rotateActions);
        HideMenu(moveActions);
        activeMenu = ModelActionsMenu;
    }
    public void ActivateResizeCanvas()
    {
        previousPosition = objectReference.transform.position;
        previousLocalScale = objectReference.transform.localScale;
        ShowMenu(resizeActions);
        HideMenu(modelActions);
        HideMenu(rotateActions);
        HideMenu(moveActions);
        activeMenu = ResizeActionsMenu;
    }
    public void ActivateRotateCanvas()
    {
        previousRotation = objectReference.transform.rotation;
        ShowMenu(rotateActions);
        HideMenu(modelActions);
        HideMenu(resizeActions);
        HideMenu(moveActions);
        activeMenu = RotateActionsMenu;
    }

    public void ActivateMoveCanvas()
    {
        previousPosition = objectReference.transform.position;
        ShowMenu(moveActions);
        HideMenu(modelActions);
        HideMenu(resizeActions);
        HideMenu(rotateActions);
        activeMenu = MoveActionsMenu;
    }

    public void HideCanvas()
    {
        HideMenu(modelActions);
        HideMenu(resizeActions);
        HideMenu(rotateActions);
        HideMenu(moveActions);
        activeMenu = ModelActionsMenu;
    }

    private static void HideMenu(GameObject actions)
    {
        if (actions == null)
            return;

        foreach (Transform action in actions.transform)
            action.DOScale(Vector3.zero, 0.3f);
    }

    private static void ShowMenu(GameObject actions)
    {
        if (actions == null)
            return;

        foreach (Transform action in actions.transform)
            action.DOScale(Vector3.one, 0.3f);
    }

    public void RotateRightAction()
    {
        if (objectReference != null)
        {
            isRotatingRight = true;
            isRotatingLeft = false;
            Vector3 rotationAxis = SurfacePlacementOffset.GetLocalRotationAxis(
                objectReference,
                true
            );
            objectReference.transform.Rotate(
                rotationAxis,
                rotationSpeed * Time.deltaTime,
                Space.Self
            );
        }
    }

    public void RotateLeftAction()
    {
        if (objectReference != null)
        {
            isRotatingRight = false;
            isRotatingLeft = true;
            Vector3 rotationAxis = SurfacePlacementOffset.GetLocalRotationAxis(
                objectReference,
                false
            );
            objectReference.transform.Rotate(
                rotationAxis,
                rotationSpeed * Time.deltaTime,
                Space.Self
            );
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
        SpawnedModelMetadata sourceMetadata = objectReference.GetComponentInParent<SpawnedModelMetadata>();
        if (sourceMetadata == null)
        {
            Debug.LogError("The selected object has no model metadata and cannot be copied safely.", objectReference);
            return;
        }

        GameObject sourceObject = sourceMetadata.gameObject;
        ObjectSpawner objectSpawner = UIController.Instance.objectSpawner;
        SurfacePlacementOffset sourcePlacementSettings =
            sourceObject.GetComponent<SurfacePlacementOffset>();
        bool shouldSnapCopy = sourcePlacementSettings != null &&
            sourcePlacementSettings.enableEdgeSnap;

        Vector3 newPosition;
        Vector3 direction;
        if (shouldSnapCopy)
        {
            direction = sourceObject.transform.right / 2;
            newPosition = sourceObject.transform.position + direction;
        }
        else
        {

            direction = sourceObject.transform.forward;
            newPosition = sourceObject.transform.position - direction;
        }
        objectCopiedReference = Instantiate(sourceObject, newPosition, sourceObject.transform.rotation);
        SpawnedModelMetadata copiedMetadata = objectCopiedReference.GetComponent<SpawnedModelMetadata>();
        if (copiedMetadata == null)
            copiedMetadata = objectCopiedReference.AddComponent<SpawnedModelMetadata>();
        copiedMetadata.Initialize(sourceMetadata.ModelId);

        SurfacePlacementOffset copiedPlacementSettings =
            objectCopiedReference.GetComponent<SurfacePlacementOffset>();
        if (shouldSnapCopy && copiedPlacementSettings != null)
        {
            if (objectSpawner != null)
                objectSpawner.SnapCopyToFreeEdge(objectCopiedReference, sourceObject);
            else
                ObjectSpawner.SnapNextToObject(objectCopiedReference, sourceObject);
        }

        if (objectSpawner != null)
        {
            objectSpawner.RegisterSpawnedObject(objectCopiedReference);
            objectSpawner.IncrementCount(sourceMetadata.ModelId);
            BuildController.Instance.CalculateAmount();
            BuildController.Instance.CalculateTime();
        }

        CanvasManager objectCopiedCanvas = objectCopiedReference.GetComponentInChildren<CanvasManager>(true);
        HideCanvas();
        if (copiedPlacementSettings != null && copiedPlacementSettings.activateCanvasOnSelect)
            copiedPlacementSettings.HideMenusInSnapGroup(true);
        else if (objectCopiedCanvas != null)
            objectCopiedCanvas.ActivateModelCanvas();
    }

    public void MoveRightAction()
    {
        SetMoveFlag("right");
        if (UIController.Instance.ModelData?.category_id == (int)Categories.Floor)
        {
            objectReference.transform.Translate(Vector3.left * Time.deltaTime);
        }
        else
        {
            objectReference.transform.Translate(Vector3.right * Time.deltaTime);
        }
    }
    public void MoveLeftAction()
    {
        SetMoveFlag("left");
        if (UIController.Instance.ModelData?.category_id == (int)Categories.Floor)
        {
            objectReference.transform.Translate(Vector3.right * Time.deltaTime);
        }
        else
        {
            objectReference.transform.Translate(Vector3.left * Time.deltaTime);
        }
    }
    public void MoveBackAction()
    {
        SetMoveFlag("back");
        if (UIController.Instance.ModelData?.category_id == (int)Categories.Floor)
        {
            objectReference.transform.Translate(Vector3.up * Time.deltaTime);
        }
        else
        {
            objectReference.transform.Translate(Vector3.back * Time.deltaTime);
        }
    }
    public void MoveForwardAction()
    {
        SetMoveFlag("forward");
        if (UIController.Instance.ModelData?.category_id == (int)Categories.Floor)
        {
            objectReference.transform.Translate(Vector3.down * Time.deltaTime);
        }
        else
        {
            objectReference.transform.Translate(Vector3.forward * Time.deltaTime);
        }
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

    public void ScaleObject(string direction, string scaleDirection)
    {
        float factor = (scaleDirection == "up") ? resizeAmount : -resizeAmount;

        if (direction == "x")
        {
            if (objectReference.transform.position.x >= 0)
            {
                objectReference.transform.position = new Vector3(objectReference.transform.position.x + (factor / 2), objectReference.transform.position.y, objectReference.transform.position.z);
            }
            else
            {
                objectReference.transform.position = new Vector3(objectReference.transform.position.x - (factor / 2), objectReference.transform.position.y, objectReference.transform.position.z);
            }
            objectReference.transform.localScale = new Vector3(objectReference.transform.localScale.x + factor, objectReference.transform.localScale.y, objectReference.transform.localScale.z);
        }

        if (direction == "y")
        {
            if (objectReference.transform.position.y >= 0)
            {
                objectReference.transform.position = new Vector3(objectReference.transform.position.x, objectReference.transform.position.y + (factor / 2), objectReference.transform.position.z);
            }
            else
            {
                objectReference.transform.position = new Vector3(objectReference.transform.position.x, objectReference.transform.position.y - (factor / 2), objectReference.transform.position.z);
            }
            objectReference.transform.localScale = new Vector3(objectReference.transform.localScale.x, objectReference.transform.localScale.y + factor, objectReference.transform.localScale.z);
        }
    }
    public void CancelAction()
    {
        if (activeMenu == RotateActionsMenu)
        {
            objectReference.transform.rotation = previousRotation; //Funciona
        }
        else if (activeMenu == MoveActionsMenu)
        {
            objectReference.transform.position = previousPosition; //Funciona
        }
        else if (activeMenu == ResizeActionsMenu)
        {
            objectReference.transform.position = previousPosition; //Funciona
            objectReference.transform.localScale = previousLocalScale; //Funciona
        }
        ActivateModelCanvas();
    }
    // Update is called once per frame
    public void DestroyObject()
    {
        if (destroyRequested)
            return;

        destroyRequested = true;
        SpawnedModelMetadata metadata = objectReference != null
            ? objectReference.GetComponentInParent<SpawnedModelMetadata>()
            : GetComponentInParent<SpawnedModelMetadata>();
        GameObject objectToDestroy = ResolveObjectToDestroy(metadata);
        if (objectToDestroy == null)
        {
            destroyRequested = false;
            return;
        }

        ObjectSpawner objectSpawner = UIController.Instance != null
            ? UIController.Instance.objectSpawner
            : null;

        HideCanvas();
        KillTweensInHierarchy(objectToDestroy);
        PrepareForDeferredDestroy(objectToDestroy);
        StartCoroutine(DestroyAfterInteractionUpdate(objectToDestroy));
        if (objectSpawner != null)
        {
            objectSpawner.SetActive(true);
            objectSpawner.UnregisterSpawnedObject(objectToDestroy);
            if (metadata != null)
                objectSpawner.ReduceCount(metadata.ModelId);
            else
                Debug.LogWarning("The deleted object had no model metadata; its count could not be updated.");
        }

        if (BuildController.Instance != null)
        {
            BuildController.Instance.CalculateAmount();
            BuildController.Instance.CalculateTime();
        }
    }

    private GameObject ResolveObjectToDestroy(SpawnedModelMetadata metadata)
    {
        if (metadata != null)
            return metadata.gameObject;

        Transform modelRoot = objectReference != null ? objectReference.transform : transform;
        while (modelRoot.parent != null)
            modelRoot = modelRoot.parent;

        return modelRoot.gameObject;
    }

    private IEnumerator DestroyAfterInteractionUpdate(GameObject root)
    {
        // XR can retain the selected collider until its next update. Waiting one
        // frame avoids that stale reference and does not depend on Time.timeScale.
        yield return null;
        if (root != null)
            Destroy(root);
    }

    private static void KillTweensInHierarchy(GameObject root)
    {
        foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
            target.DOKill();
    }

    private static void PrepareForDeferredDestroy(GameObject root)
    {
        // Keep the hierarchy alive briefly because XRRayInteractor can still hold a
        // reference to one of its UI children until the next interaction update.
        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            canvas.enabled = false;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
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
