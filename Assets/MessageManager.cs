using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI Username;
    [SerializeField] public TextMeshProUGUI Message;
    [SerializeField] public RectTransform RectTransform;

    void Start()
    {
        AdjustMessageSize();
    }

    void AdjustMessageSize()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
    }

}
