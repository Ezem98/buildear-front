using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModelManager : MonoBehaviour
{


    [SerializeField] private TextMeshProUGUI TitleText;
    [SerializeField] private TextMeshProUGUI DescriptionText;
    [SerializeField] private TextMeshProUGUI UsersCountText;
    [SerializeField] private Image Image;
    private ApiController ApiController;
    // Start is called before the first frame update
    void OnEnable()
    {
        if (!ApiController)
            ApiController = FindObjectOfType<ApiController>();
        Debug.Log("CurrentModelIndex: " + UIController.Instance.CurrentModelIndex);
        int modelId = UIController.Instance.CurrentModelIndex;
        ModelData model = UIController.Instance.ModelsData.Find(x => x.id == modelId);
        if (model != null)
        {
            TitleText.text = model.name;
            DescriptionText.text = model.description;
            if (ApiController)
            {
                ApiController.GetModelsUnderBuild(model.id.ToString(), onSuccess: (modelsData) => UsersCountText.text = modelsData.Count + " usuarios han empezado esta construcción.", onError: (error) => Debug.Log(error));
                ApiController.GetModelImage(model.model_image, onSuccess: (image) => Image.sprite = image, onError: (error) => Debug.Log(error));
            }
        }
    }
}
