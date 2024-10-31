using TMPro;
using UnityEngine;
using Utilities.Extensions;

public class ChatList : MonoBehaviour
{
    [SerializeField] private GameObject ChatItemsContainer;
    [SerializeField] private TextMeshProUGUI LoadingText;
    [SerializeField] private ChatHistoryItemManager ChatHistoryItemManager;
    // Start is called before the first frame update

    public void CreateButtons()
    {
        LoadingText.SetActive(true);
        foreach (ConversationData conversation in UIController.Instance.ConversationsData)
        {
            ChatHistoryItemManager chatHistoryItemButton = Instantiate(ChatHistoryItemManager, ChatItemsContainer.transform); ;
            chatHistoryItemButton.DateTime.text = conversation.created_at;
        }

        LoadingText.SetActive(false);
        if (UIController.Instance.ConversationsData.Count == 0)
        {
            LoadingText.text = "No tienes conversaciones finalizadas";
            LoadingText.SetActive(true);
        }
    }

    // Update is called once per frame
    private void OnDisable()
    {
        DestroyButtons();
    }

    private void DestroyButtons()
    {
        foreach (Transform child in ChatItemsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
