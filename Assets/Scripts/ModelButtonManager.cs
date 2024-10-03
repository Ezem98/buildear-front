using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModelButtonManager : MonoBehaviour
{
    [SerializeField] private Button Button;
    [SerializeField] public TextMeshProUGUI Title;
    [SerializeField] public Image Image;

    //#region Setters 

    private void Start()
    {
        Button.onClick.AddListener(() => UIController.Instance.CurrentModelIndex = 1);
        Button.onClick.AddListener(() => UIController.Instance.ScreenHandler("model"));
    }

    //#endregion
}