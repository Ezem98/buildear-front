using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class BuildController : MonoBehaviour
{
    public GameObject GuideResponse;
    public GameObject LoadingModal;
    public GameObject FinishModal;
    public GameObject RulerManager;
    public GameObject GreetingPrompt;
    public GameObject RulerPlaceButton;
    public GameObject BackToSpawnModeButton;
    public GameObject Gyroscope;
    public GameObject MaterialList;
    public Button MaterialListButton;
    public Button GuideButton;
    public Button FinishButton;
    public GameObject ToolbarButton;
    public GameObject ObjectSpawner;
    public GameObject CameraPivot;
    public TextMeshProUGUI StepTitle;
    public TextMeshProUGUI StepDescription;
    public TextMeshProUGUI StepCount;
    public TextMeshProUGUI CostText;
    public TextMeshProUGUI TimeText;
    public ARPlaneManager ARPlaneManager;
    public ApiController ApiController;
    public Button ChatButton;
    public Button ChatCloseButton;
    public TMP_InputField ChatInputField;
    public GameObject ChatModal;
    private readonly Dictionary<string, PlaneDetectionMode> detectionModeDictionary = new() {
        { "horizontal", PlaneDetectionMode.Horizontal },
        { "vertical", PlaneDetectionMode.Vertical },
    };
    private PlaneDetectionMode previousDetectionMode;
    private const float FloorPlaneHeightTolerance = 0.15f;
    private bool filterElevatedHorizontalPlanes;
    private float nextPlaneVisibilityRefresh;
    private Dictionary<int, Guide> guidesDictionary = new();
    public Dictionary<int, Guide> GuidesDictionary { get => guidesDictionary; set => guidesDictionary = value; }
    private Dictionary<int, Paso> currentStepDictionary = new();

    public Dictionary<int, Paso> CurrentStepDictionary { get => currentStepDictionary; set => currentStepDictionary = value; }
    private float costAmount = 0;
    public float CostAmount { get => costAmount; set => costAmount = value; }
    private int timeAmount = 0;
    public int TimeAmount { get => timeAmount; set => timeAmount = value; }
    private List<ConversationMessageData> chatMessages = new();
    private Coroutine temporaryMessageCoroutine;
    public List<ConversationMessageData> ChatMessages { get => chatMessages; set => chatMessages = value; }
    private static BuildController _instance;

    public void BackToUI()
    {
        UIController.Instance.GoBack();
    }

    public void BackToSpawnMode()
    {
        RulerManager.SetActive(false);
        RulerPlaceButton.SetActive(false);
        BackToSpawnModeButton.SetActive(false);
        CameraPivot.SetActive(false);
        Gyroscope.SetActive(false);
        MaterialList.SetActive(false);
        ARPlaneManager.requestedDetectionMode = previousDetectionMode;
        ObjectSpawner.SetActive(true);
        ToolbarButton.SetActive(true);
    }

    private void OnEnable()
    {
        BeginBuildSession();
    }

    public void BeginBuildSession()
    {
        ResolveSceneReferences();
        ResetTransientUi();
        InitializeSelectedModel();

        string modelPosition = UIController.Instance.ModelData?.position;
        if (ARPlaneManager != null &&
            !string.IsNullOrWhiteSpace(modelPosition) &&
            detectionModeDictionary.TryGetValue(modelPosition, out PlaneDetectionMode detectionMode))
        {
            ARPlaneManager.requestedDetectionMode = detectionMode;
            previousDetectionMode = detectionMode;
        }

        ConfigureFloorPlacement();

        if (UIController.Instance.UserData?.completed_profile == (int)CompletedProfile.Incomplete || UIController.Instance.GuestUser)
        {
            GreetingPrompt?.SetActive(true);
        }

        bool hasGuide = GuidesDictionary.GetValueOrDefault(UIController.Instance.CurrentModelIndex) != null;
        if (MaterialListButton != null) MaterialListButton.interactable = hasGuide;
        if (GuideButton != null) GuideButton.interactable = hasGuide;
        if (FinishButton != null) FinishButton.interactable = hasGuide;
        if (ChatButton != null) ChatButton.interactable = hasGuide;
    }

    public void EndBuildSession()
    {
        if (temporaryMessageCoroutine != null)
        {
            StopCoroutine(temporaryMessageCoroutine);
            temporaryMessageCoroutine = null;
        }

        ResetTransientUi();
        ObjectSpawner spawner = ObjectSpawner != null
            ? ObjectSpawner.GetComponent<ObjectSpawner>()
            : null;
        spawner?.ClearSpawnedObjects();
        ChatMessages.Clear();
        filterElevatedHorizontalPlanes = false;
        SetAllPlaneRenderersVisible(true);
    }

    private void Update()
    {
        if (!filterElevatedHorizontalPlanes || Time.unscaledTime < nextPlaneVisibilityRefresh)
            return;

        nextPlaneVisibilityRefresh = Time.unscaledTime + 0.25f;
        RefreshFloorPlaneVisibility();
    }

    private void ConfigureFloorPlacement()
    {
        filterElevatedHorizontalPlanes =
            UIController.Instance.ModelData?.category_id == (int)Categories.Floor;

        ARInteractorSpawnTrigger[] spawnTriggers =
            FindObjectsOfType<ARInteractorSpawnTrigger>(true);
        foreach (ARInteractorSpawnTrigger spawnTrigger in spawnTriggers)
        {
            spawnTrigger.planeManager = ARPlaneManager;
            spawnTrigger.requireHorizontalUpSurface = filterElevatedHorizontalPlanes;
            spawnTrigger.requireLowestHorizontalSurface = filterElevatedHorizontalPlanes;
            spawnTrigger.lowestHorizontalSurfaceTolerance = FloorPlaneHeightTolerance;
        }

        if (filterElevatedHorizontalPlanes)
            RefreshFloorPlaneVisibility();
        else
            SetAllPlaneRenderersVisible(true);
    }

    private void RefreshFloorPlaneVisibility()
    {
        if (ARPlaneManager == null)
            return;

        float lowestHeight = float.PositiveInfinity;
        foreach (ARPlane plane in ARPlaneManager.trackables)
        {
            if (IsUsableHorizontalPlane(plane))
                lowestHeight = Mathf.Min(lowestHeight, plane.transform.position.y);
        }

        if (float.IsPositiveInfinity(lowestHeight))
            return;

        foreach (ARPlane plane in ARPlaneManager.trackables)
        {
            bool visible = IsUsableHorizontalPlane(plane) &&
                ARInteractorSpawnTrigger.IsWithinLowestHorizontalSurface(
                    plane.transform.position.y,
                    lowestHeight,
                    FloorPlaneHeightTolerance
                );
            SetPlaneRendererVisible(plane, visible);
        }
    }

    private static bool IsUsableHorizontalPlane(ARPlane plane)
    {
        return plane != null &&
            plane.isActiveAndEnabled &&
            plane.subsumedBy == null &&
            plane.alignment == PlaneAlignment.HorizontalUp;
    }

    private void SetAllPlaneRenderersVisible(bool visible)
    {
        if (ARPlaneManager == null)
            return;

        foreach (ARPlane plane in ARPlaneManager.trackables)
            SetPlaneRendererVisible(plane, visible);
    }

    private static void SetPlaneRendererVisible(ARPlane plane, bool visible)
    {
        Renderer planeRenderer = plane != null ? plane.GetComponent<Renderer>() : null;
        if (planeRenderer != null)
            planeRenderer.enabled = visible;
    }

    private void Awake()
    {
        _instance = this;
        ResolveSceneReferences();
    }

    private void ResolveSceneReferences()
    {
        if (ChatModal == null)
            ChatModal = FindGameObjectInScene("Chat");

        if (ChatModal != null)
        {
            if (ChatCloseButton == null)
            {
                Transform closeButton = FindChildByName(ChatModal.transform, "CloseButton");
                if (closeButton != null)
                    ChatCloseButton = closeButton.GetComponent<Button>();
            }

            if (ChatInputField == null)
                ChatInputField = ChatModal.GetComponentInChildren<TMP_InputField>(true);
        }

        if (ChatCloseButton != null)
        {
            ChatCloseButton.onClick.RemoveListener(CloseChatModal);
            ChatCloseButton.onClick.AddListener(CloseChatModal);
        }
    }

    private void ResetTransientUi()
    {
        GuideResponse?.SetActive(false);
        LoadingModal?.SetActive(false);
        FinishModal?.SetActive(false);
        ChatModal?.SetActive(false);
        GreetingPrompt?.SetActive(false);
        MaterialList?.SetActive(false);
        RulerManager?.SetActive(false);
        RulerPlaceButton?.SetActive(false);
        BackToSpawnModeButton?.SetActive(false);
        CameraPivot?.SetActive(false);
        Gyroscope?.SetActive(false);
        ObjectSpawner?.SetActive(true);
        ToolbarButton?.SetActive(true);
    }

    private void InitializeSelectedModel()
    {
        ObjectSpawner spawner = ObjectSpawner != null
            ? ObjectSpawner.GetComponent<ObjectSpawner>()
            : FindObjectOfType<ObjectSpawner>();

        UIController.Instance.objectSpawner = spawner;
        ConfigureSpawnerForSelectedModel(spawner, UIController.Instance.CurrentModelIndex);
    }

    public static bool ConfigureSpawnerForSelectedModel(ObjectSpawner spawner, int modelId)
    {
        if (spawner == null || modelId <= 0 ||
            spawner.objectPrefabsIndex == null ||
            !spawner.objectPrefabsIndex.Contains(modelId))
        {
            return false;
        }

        spawner.spawnOptionId = modelId;
        return true;
    }

    private GameObject FindGameObjectInScene(string objectName)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            Transform match = FindChildByName(root.transform, objectName);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform match = FindChildByName(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    public static BuildController Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildController>();

            }
            return _instance;
        }
    }

    public void StepForward()
    {
        Paso CurrentStep = CurrentStepDictionary[UIController.Instance.CurrentModelIndex];
        Guide Guide = GuidesDictionary[UIController.Instance.CurrentModelIndex];
        int currentIndex = Guide.pasos.FindIndex(step => step.paso == CurrentStep.paso);
        if (currentIndex < 0 || currentIndex >= Guide.pasos.Count - 1) return;
        CurrentStep = Guide.pasos[currentIndex + 1];
        CurrentStepDictionary[UIController.Instance.CurrentModelIndex] = CurrentStep;
        UpdateStep();
    }

    public void StepBackward()
    {
        Paso CurrentStep = CurrentStepDictionary[UIController.Instance.CurrentModelIndex];
        Guide Guide = GuidesDictionary[UIController.Instance.CurrentModelIndex];
        int currentIndex = Guide.pasos.FindIndex(step => step.paso == CurrentStep.paso);
        if (currentIndex <= 0) return;
        CurrentStep = Guide.pasos[currentIndex - 1];
        CurrentStepDictionary[UIController.Instance.CurrentModelIndex] = CurrentStep;
        UpdateStep();
    }

    public void HandleGuideResponse(bool IsOpen)
    {
        GuideResponse.SetActive(IsOpen);
        UIAnimation.Instance.FadeOut();
    }

    private void UpdateStep()
    {
        Paso CurrentStep = CurrentStepDictionary[UIController.Instance.CurrentModelIndex];
        Guide Guide = GuidesDictionary[UIController.Instance.CurrentModelIndex];
        StepTitle.text = CurrentStep.titulo;
        StepDescription.text = CurrentStep.descripcion;
        StepCount.text = "Paso " + CurrentStep.paso + "/" + Guide.pasos.Count;
    }

    public void RulerAction()
    {
        RulerPlaceButton.SetActive(true);
        RulerManager.SetActive(true);
        BackToSpawnModeButton.SetActive(true);
        CameraPivot.SetActive(true);
        ObjectSpawner.SetActive(false);
        ToolbarButton.SetActive(false);
        UIAnimation.Instance.FadeOut();
    }
    public void GyroscopeAction()
    {
        Gyroscope.SetActive(true);
        ToolbarButton.SetActive(false);
        UIAnimation.Instance.FadeOut();
    }

    public void MaterialListAction()
    {
        MaterialList.SetActive(true);
        ToolbarButton.SetActive(false);
        UIAnimation.Instance.FadeOut();
    }

    public void FinishAction()
    {
        Paso CurrentStep = CurrentStepDictionary[UIController.Instance.CurrentModelIndex];
        UpdateUserModelData userModelData = new()
        {
            completed = (int)CompletedProfile.Complete,
            current_step = CurrentStep.paso,
        };
        ApiController.UpdateUserModelData(userModelData, () =>
        {
            FinishModal.SetActive(true);
            ToolbarButton.SetActive(false);
            UIAnimation.Instance.FadeOut();
        });
    }

    public void CloseFinishModal()
    {
        FinishModal.SetActive(false);
        ToolbarButton.SetActive(true);
    }

    public void StartChat()
    {
        if (UIController.Instance.GuestUser || !UIController.Instance.HasValidSession())
        {
            ShowTemporaryMessage("Iniciá sesión para usar el asistente y guardar la conversación.");
            return;
        }

        if (GuidesDictionary.TryGetValue(UIController.Instance.CurrentModelIndex, out Guide guide) && guide != null)
        {
            UIController.Instance.CurrentConversationId = -1;
            ChatMessages.Clear();
            HandleChatModal(true);
        }
        else
        {
            ShowTemporaryMessage("Para iniciar el chat es necesario generar la guía.");
        }
    }

    public void KnowMore()
    {
        if (UIController.Instance.GuestUser || !UIController.Instance.HasValidSession())
        {
            ShowTemporaryMessage("Iniciá sesión para consultar al asistente.");
            return;
        }

        int modelId = UIController.Instance.CurrentModelIndex;
        if (!GuidesDictionary.ContainsKey(modelId) || !CurrentStepDictionary.ContainsKey(modelId))
        {
            ShowTemporaryMessage("Primero generá una guía para este modelo.");
            return;
        }

        UIController.Instance.CurrentConversationId = -1;
        ChatMessages.Clear();
        ChatCloseButton.interactable = false;
        ChatInputField.interactable = false;
        GuideResponse.SetActive(false);
        ChatModal.SetActive(true);

        int stepNumber = CurrentStepDictionary[modelId].paso;
        string message = $"¿Podés darme más detalle sobre el paso {stepNumber} de esta guía?";
        ChatManager.Instance.CreateCustomUserChatMessage(message);
        ChatMessageData chatMessageData = new()
        {
            message = message,
            model_id = modelId,
            current_step = stepNumber,
        };
        ApiController.SendMessageToAI(chatMessageData, onSuccess: (response) =>
        {
            ChatManager.Instance.CreateAIChatMessage(response);
            ChatCloseButton.interactable = true;
            ChatInputField.interactable = true;
        }, onError: (error) =>
        {
            ChatManager.Instance.CreateAIChatMessage(error);
            ChatCloseButton.interactable = true;
            ChatInputField.interactable = true;
        });
    }

    public void HandleChatModal(bool IsOpen)
    {
        ChatModal?.SetActive(IsOpen);
    }

    private void CloseChatModal()
    {
        HandleChatModal(false);
    }

    IEnumerator PassiveMe(int secs)
    {
        yield return new WaitForSeconds(secs);
        LoadingModal.SetActive(false);
        temporaryMessageCoroutine = null;
    }

    public void ShowTemporaryMessage(string message)
    {
        if (temporaryMessageCoroutine != null)
        {
            StopCoroutine(temporaryMessageCoroutine);
        }
        LoadingModal.GetComponentInChildren<TextMeshProUGUI>().text = message;
        LoadingModal.SetActive(true);
        temporaryMessageCoroutine = StartCoroutine(PassiveMe(5));
    }

    public void ShowLoading(string message)
    {
        if (temporaryMessageCoroutine != null)
        {
            StopCoroutine(temporaryMessageCoroutine);
            temporaryMessageCoroutine = null;
        }
        LoadingModal.GetComponentInChildren<TextMeshProUGUI>().text = message;
        LoadingModal.SetActive(true);
    }

    private void OnDisable()
    {
        SetAllPlaneRenderersVisible(true);

        if (!UIController.Instance.GuestUser)
        {
            UIController.Instance.SaveData();
        }
    }

    public void CalculateAmount()
    {
        ObjectSpawner objectSpawner = UIController.Instance.objectSpawner;
        if (objectSpawner == null)
        {
            return;
        }

        Dictionary<int, int> countDictionary = objectSpawner.CountDictionary;
        costAmount = 0;
        foreach (KeyValuePair<int, Guide> entry in guidesDictionary)
        {
            int modelId = entry.Key;
            Guide guide = entry.Value;
            if (countDictionary.TryGetValue(modelId, out int count))
            {
                costAmount += guide.costo * count;
            }
        }

        if (costAmount <= 0) CostText.text = "Sin estimación";
        else CostText.text = $"≈ {costAmount:0.00} USD";
    }

    public void CalculateTime()
    {
        ObjectSpawner objectSpawner = UIController.Instance.objectSpawner;
        if (objectSpawner != null)
        {
            Dictionary<int, int> countDictionary = objectSpawner.CountDictionary;
            timeAmount = 0;
            foreach (KeyValuePair<int, Guide> entry in guidesDictionary)
            {
                int modelId = entry.Key;
                Guide guide = entry.Value;
                if (countDictionary.TryGetValue(modelId, out int count))
                {
                    timeAmount += guide.tiempo_insumido * count;
                }
            }
            if (timeAmount == 0) TimeText.text = "--.--";
            else TimeText.text = $"{StringUtils.ConvertMinutesToTimeString(timeAmount)}";
        }
    }

    private void OnDestroy()
    {
        if (ChatCloseButton != null)
            ChatCloseButton.onClick.RemoveListener(CloseChatModal);

        if (_instance == this)
            _instance = null;

        if (!UIController.Instance.GuestUser)
            UIController.Instance.SaveData();
    }

}
