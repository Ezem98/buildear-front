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

    private static ChatManager _instance;
    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject); // Si ya existe una instancia, destruir este objetoassss
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Mantener la instancia en todas las escenas
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

                // Si no se encuentra la instancia en la escena, crear una nueva
                if (_instance == null)
                {
                    GameObject singletonObject = new();
                    _instance = singletonObject.AddComponent<ChatManager>();
                    singletonObject.name = typeof(ChatManager).ToString() + " (Singleton)";

                    // Opcional: Evitar que sea destruido cuando se cambie de escena
                    DontDestroyOnLoad(singletonObject);
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
        CreateCustomUserChatMessage(MessageInputField.text);
        if (ApiController)
        {
            ChatMessageData chatMessageData = new() { message = MessageInputField.text };
            MessageInputField.text = "";
            ApiController.SendMessageToAI(chatMessageData, onSuccess: (response) => CreateAIChatMessage(response), onError: (error) => Debug.Log(error));
        }

    }

    public void CreateCustomUserChatMessage(string message)
    {
        if (!UIController.Instance.GuestUser)
        {
            ConversationMessageData conversationMessageData = new() { sender = UIController.Instance.UserData.username, message = message, conversation_id = UIController.Instance.CurrentConversationId };
            BuildController.Instance.ChatMessages.Add(conversationMessageData);
        }
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
        if (!UIController.Instance.GuestUser)
        {
            ConversationMessageData conversationMessageData = new() { sender = "BuildeAR Assistant", message = message, conversation_id = UIController.Instance.CurrentConversationId };
            BuildController.Instance.ChatMessages.Add(conversationMessageData);
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
