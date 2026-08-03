using UnityEngine;
using UnityEngine.UI;

public class ChatHistoryItemManager : MonoBehaviour
{
    [SerializeField] private Button Button;
    [SerializeField] public Text DateTime;
    private int conversationId = -1;

    public void Initialize(ConversationData conversation)
    {
        conversationId = conversation.id;
        DateTime.text = conversation.created_at;
    }

    private void OnEnable()
    {
        Button.onClick.AddListener(OpenConversation);
    }

    private void OnDisable()
    {
        Button.onClick.RemoveListener(OpenConversation);
    }

    private void OpenConversation()
    {
        if (conversationId < 0)
        {
            Debug.LogError("The conversation history item has no conversation ID configured.", this);
            return;
        }

        UIController.Instance.CurrentConversationId = conversationId;
        UIController.Instance.ScreenHandler("ChatHistoryItem");
    }
}
