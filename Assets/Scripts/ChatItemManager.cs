using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatItemManager : MonoBehaviour
{

    [SerializeField] private GameObject MessagesContainer;
    [SerializeField] private MessageManager UserMessageManager;
    [SerializeField] private MessageManager AIMessageManager;
    [SerializeField] private ApiController ApiController;
    [SerializeField] private TextMeshProUGUI DateTimeText;

    private void Start()
    {
        if (UIController.Instance.CurrentConversationId != -1)
        {
            ConversationData conversationData = UIController.Instance.ConversationsData.Find(conversation => conversation.id == UIController.Instance.CurrentConversationId);
            DateTimeText.text = conversationData?.created_at;
            CreateMessagesButtons(UIController.Instance.CurrentConversationId);
        }
    }

    private void OnDisable()
    {
        DestroyMessages();
    }

    public void CreateUserChatMessage(string message)
    {
        MessageManager userMessage = Instantiate(UserMessageManager, MessagesContainer.transform);
        userMessage.Username.text = UIController.Instance.UserData.username;
        userMessage.Message.text = message;
    }

    public void CreateAIChatMessage(string message)
    {
        MessageManager AImessage = Instantiate(AIMessageManager, MessagesContainer.transform);
        AImessage.Message.text = message;
        SetSize(AImessage.Message, AImessage.RectTransform, AImessage.padding);
    }

    public void SetSize(TextMeshProUGUI message, RectTransform RectTransform, Vector2 padding)
    {
        message.ForceMeshUpdate();
        Vector2 textSize = message.GetRenderedValues(false);
        RectTransform.sizeDelta = textSize + padding;
    }
    public void CreateMessagesButtons(int conversationId)
    {
        ApiController.GetConversationMessages(conversationId, onSuccess: (response) =>
        {
            BuildController.Instance.ChatMessages = response;
            foreach (ConversationMessageData chatMessage in response)
            {
                if (chatMessage.sender == UIController.Instance.UserData.username)
                {
                    CreateUserChatMessage(chatMessage.message);
                }
                else
                {
                    CreateAIChatMessage(chatMessage.message);
                }
            }
        }, onError: (error) => Debug.Log(error));

    }

    private void DestroyMessages()
    {
        foreach (Transform child in MessagesContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
