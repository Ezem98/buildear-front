using UnityEngine;
using UnityEngine.UI;

public class ChatHistoryItemManager : MonoBehaviour
{
    [SerializeField] private Button Button;
    [SerializeField] public Text DateTime;

    private void Start()
    {
        Button.onClick.AddListener(() => UIController.Instance.ScreenHandler("ChatHistoryItem"));
    }

    //#endregion
}