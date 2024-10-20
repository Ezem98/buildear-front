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
        if (UIController.Instance.MyModelsData != null)
            CreateButtons();

        if (UIController.Instance.GuestUser)
        {

            LoadingText.text = "Para ver tus modelos en construcción, necesitas ser usuario de BuildeAR ¡Registrate!";
            LoadingText.SetActive(true);
            ViewAllButton.interactable = false;
        }
    }

    private void OnDisable()
    {
        DestroyButtons();
    }

    public void CreateButtons()
    {
        LoadingText.SetActive(true);
        foreach (ModelData model in UIController.Instance.MyModelsData)
        {
            ModelButtonManager modelButton = Instantiate(ModelButtonManager, ModelsContainer.transform); ;
            modelButton.Title.text = model.name;
            modelButton.Id = model.id;
            if (ApiController)
                ApiController.GetModelImage(model.model_image, onSuccess: (image) => modelButton.Image.sprite = image, onError: (error) => Debug.Log(error));
        }

        LoadingText.SetActive(false);
        if (UIController.Instance.MyModelsData.Count == 0)
        {
            LoadingText.text = "Aún no has empezado ninguna construcción ¡Animate!";
            LoadingText.SetActive(true);
        }
    }

    private void DestroyButtons()
    {
        foreach (Transform child in ModelsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
