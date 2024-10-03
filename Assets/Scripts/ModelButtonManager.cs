using UnityEngine;
using UnityEngine.UI;

public class ModelButtonManager : MonoBehaviour
{
    private string title;
    private Sprite image;

    //#region Setters 
    public string Title { set => title = value; }

    public Sprite Image { set => image = value; }

    private void Start()
    {
        transform.GetChild(0).GetComponent<RawImage>().texture = image.texture;
        transform.GetChild(1).GetComponent<Text>().text = title;

        var button = GetComponent<Button>();
        button.onClick.AddListener(() => UIController.Instance.CurrentModelIndex = 1);
        button.onClick.AddListener(() => UIController.Instance.ScreenHandler("model"));
    }

    //#endregion
}