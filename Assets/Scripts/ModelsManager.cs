using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelsManager : MonoBehaviour
{

    [SerializeField] private GameObject ModelsContainer;
    [SerializeField] private ModelButtonManager ModelButtonManager;
    private ApiController ApiController;
    private static ModelsManager _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject); // Si ya existe una instancia, destruir este objetoassss
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Mantener la instancia en todas las escenas
        }
    }

    private void OnEnable()
    {
        if (!ApiController) ApiController = FindObjectOfType<ApiController>();
    }

    private void OnDisable()
    {
        DestroyButtons();
    }

    public void CreateButtons()
    {
        Debug.Log(UIController.Instance?.ModelsData?.ToString());
        foreach (ModelData model in UIController.Instance.ModelsData)
        {
            ModelButtonManager modelButton = Instantiate(ModelButtonManager, ModelsContainer.transform); ;
            modelButton.Title.text = model.name;
            Debug.Log("Model Id: " + model.id);
            modelButton.Id = model.id;
            ApiController.GetModelImage(model.model_image, onSuccess: (image) => modelButton.Image.sprite = image, onError: (error) => Debug.Log(error));
        }

        SceneManager.sceneLoaded -= (scene, mode) => CreateButtons();
    }

    private void DestroyButtons()
    {
        foreach (Transform child in ModelsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public static ModelsManager Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<ModelsManager>();

                // Si no se encuentra la instancia en la escena, crear una nueva
                if (_instance == null)
                {
                    GameObject singletonObject = new();
                    _instance = singletonObject.AddComponent<ModelsManager>();
                    singletonObject.name = typeof(ModelsManager).ToString() + " (Singleton)";

                    // Opcional: Evitar que sea destruido cuando se cambie de escena
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }
}