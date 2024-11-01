using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI Username;
    [SerializeField] public TextMeshProUGUI Message;
    [SerializeField] public RectTransform RectTransform;
    [SerializeField] public Vector2 padding;

    void Start()
    {
        AdjustMessageSize();
    }

    public void AdjustMessageSize()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
    }


}
