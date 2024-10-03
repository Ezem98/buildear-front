using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModelButtonManager : MonoBehaviour
{
    [SerializeField] private Button Button;
    [SerializeField] public TextMeshProUGUI Title;
    [SerializeField] public Image Image;
    private int id;
    public int Id { set => id = value; }

    //#region Setters 

    private void Start()
    {
        Debug.Log("Id: " + id);
        Button.onClick.AddListener(() => UIController.Instance.CurrentModelIndex = id);
        Button.onClick.AddListener(() => UIController.Instance.ScreenHandler("Model"));
    }

    //#endregion
}