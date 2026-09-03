using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{

    [SerializeField] private GameObject MessagesContainer;
    [SerializeField] private TMP_InputField MessageInputField;
    [SerializeField] private MessageManager UserMessageManager;
    [SerializeField] private MessageManager AIMessageManager;
    [SerializeField] private Button SendButton;
    [SerializeField] private ApiController ApiController;
    private bool isSending;

    private static ChatManager _instance;
    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            _instance = this;
        }
    }

    public static ChatManager Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<ChatManager>();

                if (_instance == null)
                {
                    Debug.LogError("No hay un ChatManager configurado en la escena.");
                }
            }
            return _instance;
        }
    }

    private void OnDisable()
    {
        DestroyMessages();
    }

    public void CreateUserChatMessage()
    {
        if (isSending || ApiController == null) return;

        string message = MessageInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(message)) return;
        if (!UIController.Instance.HasAuthenticatedSession())
        {
            CreateAIChatMessage("Tu sesión venció. Iniciá sesión nuevamente para continuar.");
            return;
        }

        CreateCustomUserChatMessage(message);
        MessageInputField.text = "";
        SetSending(true);
        int? currentStepNumber = null;
        if (
            BuildController.Instance.CurrentStepDictionary.TryGetValue(
                UIController.Instance.CurrentModelIndex,
                out Paso currentStep
            )
        )
        {
            currentStepNumber = currentStep.paso;
        }
        ChatMessageData chatMessageData = new()
        {
            message = message,
            conversation_id = UIController.Instance.CurrentConversationId > 0
                ? UIController.Instance.CurrentConversationId
                : (int?)null,
            model_id = UIController.Instance.CurrentModelIndex > 0
                ? UIController.Instance.CurrentModelIndex
                : (int?)null,
            current_step = currentStepNumber,
        };
        ApiController.SendMessageToAI(chatMessageData, onSuccess: (response) =>
        {
            CreateAIChatMessage(response);
            SetSending(false);
        }, onError: (error) =>
        {
            CreateAIChatMessage(error);
            SetSending(false);
        });
    }

    public void CreateCustomUserChatMessage(string message)
    {
        MessageManager userMessage = Instantiate(UserMessageManager, MessagesContainer.transform);
        userMessage.Username.text = UIController.Instance.UserData?.username ?? "Invitado";
        userMessage.Message.text = message;
        SetSize(userMessage.Message, userMessage.RectTransform, userMessage.padding);
    }

    public void CreateAIChatMessage(string message)
    {
        MessageManager AImessage = Instantiate(AIMessageManager, MessagesContainer.transform);
        AImessage.Message.text = message;
        SetSize(AImessage.Message, AImessage.RectTransform, AImessage.padding);
    }

    private void SetSending(bool sending)
    {
        isSending = sending;
        SendButton.interactable = !sending;
        MessageInputField.interactable = !sending;
        if (!sending)
        {
            MessageInputField.ActivateInputField();
        }
    }

    public void SetSize(TextMeshProUGUI message, RectTransform RectTransform, Vector2 padding)
    {
        message.ForceMeshUpdate();
        Vector2 textSize = message.GetRenderedValues(false);
        RectTransform.sizeDelta = textSize + padding;
    }

    private void DestroyMessages()
    {
        foreach (Transform child in MessagesContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SubmitMessage()
    {
        Debug.Log("Submit message");
        CreateUserChatMessage();
    }
}
