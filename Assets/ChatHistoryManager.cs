using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatHistoryManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TextCount;
    [SerializeField] private ApiController ApiController;
    [SerializeField] private ChatList ChatList;


    // Start is called before the first frame update
    void OnEnable()
    {
        //Agregar llamada para traer la cantidad de chats del usuario
        ApiController.GetUserConversations(UIController.Instance.UserData.id, onSuccess: (conversationData) =>
            {
                Debug.Log("Count: " + conversationData?.Count);
                UIController.Instance.ConversationsData.Clear();
                UIController.Instance.ConversationsData = conversationData;
                TextCount.text = $"Conversaciones finalizadas: <b>{conversationData.Count}</b>";
                ChatList.CreateButtons();
            }, onError: (error) =>
            {
                Debug.Log(error);
            });
    }
}

