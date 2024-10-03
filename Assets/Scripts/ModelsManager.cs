using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelsManager : MonoBehaviour
{

    [SerializeField] private GameObject ModelsContainer;
    [SerializeField] private ModelButtonManager ModelButtonManager;
    private ApiController ApiController;
    void Start()
    {
        CreateButtons();
    }

    void CreateButtons()
    {
        foreach (ModelData model in UIController.Instance.ModelsData)
        {
            ModelButtonManager modelButton;
            modelButton = Instantiate(ModelButtonManager, ModelsContainer.transform);
            modelButton.Title = model.name;
            modelButton.Image = ApiController.GetModelImage(model.modelImage);
        }

        SceneManager.sceneLoaded -= (scene, mode) => CreateButtons();
    }
}