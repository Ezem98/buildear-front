using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BuildController : MonoBehaviour
{
    public GameObject GuideResponse;
    public GameObject LoadingModal;
    public GameObject FinishModal;
    public GameObject RulerManager;
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
    public GameObject ChatModal;
    private readonly Dictionary<string, PlaneDetectionMode> detectionModeDictionary = new() {
        { "horizontal", PlaneDetectionMode.Horizontal },
        { "vertical", PlaneDetectionMode.Vertical },
    };
    private PlaneDetectionMode previousDetectionMode;
    private Dictionary<int, Guide> guidesDictionary = new();
    public Dictionary<int, Guide> GuidesDictionary { get => guidesDictionary; set => guidesDictionary = value; }
    private Dictionary<int, Paso> currentStepDictionary = new();

    public Dictionary<int, Paso> CurrentStepDictionary { get => currentStepDictionary; set => currentStepDictionary = value; }
    private float costAmount = 0;
    public float CostAmount { get => costAmount; set => costAmount = value; }
    private int timeAmount = 0;
    public int TimeAmount { get => timeAmount; set => timeAmount = value; }
    private List<ConversationMessageData> chatMessages = new();
    public List<ConversationMessageData> ChatMessages { get => chatMessages; set => chatMessages = value; }
    private static BuildController _instance;

    public void BackToUI()
    {
        UIController.Instance.SceneHandler("UI");
        GameObject.Find("UI").SetActive(false);
        GameObject.Find("XR Origin (AR Rig)").SetActive(false);
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
        if (ARPlaneManager != null)
        {
            ARPlaneManager.requestedDetectionMode = detectionModeDictionary[UIController.Instance.ModelData.position];
            previousDetectionMode = detectionModeDictionary[UIController.Instance.ModelData.position];
        }
    }

    private void Awake()
    {
        // ARPlaneManager.requestedDetectionMode = detectionModeDictionary[UIController.Instance.ModelData.position];
        // previousDetectionMode = detectionModeDictionary[UIController.Instance.ModelData.position];

        if (GuidesDictionary.GetValueOrDefault(UIController.Instance.CurrentModelIndex) != null)
        {
            MaterialListButton.interactable = true;
            GuideButton.interactable = true;
            FinishButton.interactable = true;
            ChatButton.interactable = true;
        }

        if (_instance != null)
        {
            Destroy(gameObject); // Si ya existe una instancia, destruir este objeto
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Mantener la instancia en todas las escenas
        }
    }

    public static BuildController Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildController>();

                // Si no se encuentra la instancia en la escena, crear una nueva
                if (_instance == null)
                {
                    GameObject singletonObject = new();
                    _instance = singletonObject.AddComponent<BuildController>();
                    singletonObject.name = typeof(BuildController).ToString() + " (Singleton)";
                    // Opcional: Evitar que sea destruido cuando se cambie de escena
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    public void StepForward()
    {
        Debug.Log("StepForward");
        Paso CurrentStep = CurrentStepDictionary[UIController.Instance.CurrentModelIndex];
        Guide Guide = GuidesDictionary[UIController.Instance.CurrentModelIndex];
        if (CurrentStep.paso == Guide.pasos.Count) return;
        CurrentStep = Guide.pasos.Find(x => x.paso == CurrentStep.paso + 1);
        CurrentStepDictionary[UIController.Instance.CurrentModelIndex] = CurrentStep;
        UpdateStep();
    }

    public void StepBackward()
    {
        Debug.Log("StepBackward");
        Paso CurrentStep = CurrentStepDictionary[UIController.Instance.CurrentModelIndex];
        Guide Guide = GuidesDictionary[UIController.Instance.CurrentModelIndex];
        if (CurrentStep.paso == 1) return;
        CurrentStep = Guide.pasos.Find(x => x.paso == CurrentStep.paso - 1);
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
        Guide Guide = GuidesDictionary[UIController.Instance.CurrentModelIndex];
        if (Guide != null)
        {
            ConversationPostData conversationPostData = new()
            {
                user_id = UIController.Instance.UserData.id,
            };
            ApiController.SaveConversation(conversationPostData, onSuccess: (response) =>
            {
                Debug.Log("Conversation id: " + response.id);
                UIController.Instance.CurrentConversationId = response.id;
                HandleChatModal(true);
            }, onError: (error) => Debug.Log(error));
        }
        else
        {
            LoadingModal.GetComponentInChildren<TextMeshProUGUI>().text = "Para iniciar el chat es necesario generar la guía.";
            LoadingModal.SetActive(true);
            StartCoroutine(PassiveMe(5));
        }
    }

    public void KnowMore()
    {
        Guide Guide = GuidesDictionary[UIController.Instance.CurrentModelIndex];
        if (Guide != null)
        {
            if (!UIController.Instance.GuestUser)
            {


                ConversationPostData conversationPostData = new()
                {
                    user_id = UIController.Instance.UserData.id,
                };
                ApiController.SaveConversation(conversationPostData, onSuccess: (response) =>
                {
                    Debug.Log("Conversation id: " + response.id);
                    UIController.Instance.CurrentConversationId = response.id;
                    GuideResponse.SetActive(false);
                    ChatModal.SetActive(true);
                    ChatManager.Instance.CreateCustomUserChatMessage($"¿Puedes darme información más detallada sobre el paso {CurrentStepDictionary[UIController.Instance.CurrentModelIndex].paso}?");
                    ChatMessageData chatMessageData = new()
                    {
                        message = $"A continuación te paso la guía de pasos que me generaste para poder llevar a cabo mi construcción/colocación: {JsonUtility.ToJson(Guide)}. ¿Puedes darme información más detallada sobre el paso {CurrentStepDictionary[UIController.Instance.CurrentModelIndex].paso}?",
                    };
                    ApiController.SendMessageToAI(chatMessageData, onSuccess: (response) =>
                    {
                        ChatManager.Instance.CreateAIChatMessage(response);
                    }, onError: (error) =>
                    {
                        ChatManager.Instance.CreateAIChatMessage("Lo siento, no pude encontrar información adicional sobre el paso que solicitaste.");
                    });
                }, onError: (error) => Debug.Log(error));
            }
            else
            {
                UIController.Instance.CurrentConversationId = -1;
                GuideResponse.SetActive(false);
                ChatModal.SetActive(true);
                ChatManager.Instance.CreateCustomUserChatMessage($"¿Puedes darme información más detallada sobre el paso {CurrentStepDictionary[UIController.Instance.CurrentModelIndex].paso}?");
                ChatMessageData chatMessageData = new()
                {
                    message = $"A continuación te paso la guía de pasos que me generaste para poder llevar a cabo mi construcción/colocación: {JsonUtility.ToJson(Guide)}. ¿Puedes darme información más detallada sobre el paso {CurrentStepDictionary[UIController.Instance.CurrentModelIndex].paso}?",
                };
                ApiController.SendMessageToAI(chatMessageData, onSuccess: (response) =>
                {
                    ChatManager.Instance.CreateAIChatMessage(response);
                }, onError: (error) =>
                {
                    ChatManager.Instance.CreateAIChatMessage("Lo siento, no pude encontrar información adicional sobre el paso que solicitaste.");
                });
            }
        }
    }

    public void HandleChatModal(bool IsOpen)
    {
        ChatModal.SetActive(IsOpen);
    }

    IEnumerator PassiveMe(int secs)
    {
        yield return new WaitForSeconds(secs);
        LoadingModal.SetActive(false);

    }

    private void OnDisable()
    {
        if (!UIController.Instance.GuestUser)
        {
            UIController.Instance.SaveData();
            Debug.Log("Data Saved");
            ConversationMessagePostData conversationMessagePostData = new() { conversation_id = UIController.Instance.CurrentConversationId, messages = ChatMessages };
            ApiController.SaveMessages(conversationMessagePostData, onSuccess: (messages) =>
                            {
                                ChatMessages.Clear();
                            }, onError: (error) =>
                            {
                                Debug.Log(error);
                            });
        }
    }

    private void OnDestroy()
    {
        if (!UIController.Instance.GuestUser)
            UIController.Instance.SaveData();
    }

}