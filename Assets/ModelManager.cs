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
                ApiController.GetModelsUnderBuild(model.id.ToString(), onSuccess: (modelsData) =>
                {
                    int count = (int)(modelsData?.Count != null ? modelsData?.Count : 0);
                    if (count == 0)
                    {
                        UsersCountText.text = "Ningún usuario ha empezado esta construcción todavía";
                    }
                    else if (count == 1)
                    {
                        UsersCountText.text = count + " usuario ya ha empezado esta construcción";
                    }
                    else
                    {
                        UsersCountText.text = count + " usuarios ya han empezado esta construcción";
                    }

                }, onError: (error) => Debug.Log(error));
                ApiController.GetModelImage(model.model_image, onSuccess: (image) => Image.sprite = image, onError: (error) => Debug.Log(error));
            }
        }
    }
}
