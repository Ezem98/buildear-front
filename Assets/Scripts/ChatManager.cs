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
        MessageManager userMessage = Instantiate(UserMessageManager, MessagesContainer.transform);
        Debug.Log("userMessage: " + userMessage?.Username?.text);
        userMessage.Username.text = UIController.Instance.UserData.username;
        userMessage.Message.text = MessageInputField.text;
        Debug.Log("Create user chat message: " + MessageInputField.text);
        if (ApiController)
        {

            ChatMessageData chatMessageData = new() { message = MessageInputField.text };
            MessageInputField.text = "";
            ApiController.SendMessageToAI(chatMessageData, onSuccess: (response) => CreateAIChatMessage(response), onError: (error) => Debug.Log(error));
        }

    }

    public void CreateAIChatMessage(string message)
    {
        MessageManager AImessage = Instantiate(AIMessageManager, MessagesContainer.transform);
        AImessage.Message.text = message;
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
