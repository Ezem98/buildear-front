using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;


public class HomeManager : MonoBehaviour
{

    [SerializeField] private GameObject ModelsContainer;
    [SerializeField] private TextMeshProUGUI LoadingText;
    [SerializeField] private ModelButtonManager ModelButtonManager;
    [SerializeField] private ApiController ApiController;
    [SerializeField] private Button ViewAllButton;
    // Start is called before the first frame update
    private void OnEnable()
    {
        if (UIController.Instance.GuestUser)
        {
            LoadingText.text = "Para ver tus modelos en construcción, necesitas ser usuario de BuildeAR ¡Registrate!";
            LoadingText.SetActive(true);
            ViewAllButton.interactable = false;
            return;
        }

        ViewAllButton.interactable = true;
        LoadingText.text = "Cargando modelos...";
        LoadingText.SetActive(true);
        RefreshModels();
    }

    private void OnDisable()
    {
        DestroyButtons();
    }

    public void CreateButtons()
    {
        CreateButtons(UIController.Instance.MyModelsData);
    }

    private void RefreshModels()
    {
        UserData user = UIController.Instance.UserData;
        if (ApiController == null || user == null || user.id <= 0)
        {
            ShowLoadError("No se pudo identificar al usuario.");
            return;
        }

        ApiController.GetModelsByUserId(user.id, onSuccess: (models) =>
        {
            if (!isActiveAndEnabled) return;
            CreateButtons(models);
        }, onError: (error) =>
        {
            if (!isActiveAndEnabled) return;
            ShowLoadError("No se pudieron cargar tus modelos.");
            Debug.LogError(error);
        });
    }

    private void CreateButtons(List<ModelData> models)
    {
        DestroyButtons();
        LoadingText.SetActive(true);
        if (models == null)
            models = new List<ModelData>();

        foreach (ModelData model in models)
        {
            ModelButtonManager modelButton = Instantiate(ModelButtonManager, ModelsContainer.transform); ;
            modelButton.Title.text = model.name;
            modelButton.Id = model.id;
            if (ApiController)
                ApiController.GetModelImage(model.model_image, onSuccess: (image) => modelButton.Image.sprite = image, onError: (error) => Debug.Log(error));
        }

        LoadingText.SetActive(false);
        if (models.Count == 0)
        {
            LoadingText.text = "Aún no has empezado ninguna construcción ¡Animate!";
            LoadingText.SetActive(true);
        }
    }

    private void ShowLoadError(string message)
    {
        LoadingText.text = message;
        LoadingText.SetActive(true);
    }

    private void DestroyButtons()
    {
        foreach (Transform child in ModelsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
