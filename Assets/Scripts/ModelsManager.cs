using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utilities.Extensions;

public class ModelsManager : MonoBehaviour
{

    [SerializeField] private GameObject ModelsContainer;
    [SerializeField] private TextMeshProUGUI ModelCountText;
    [SerializeField] private TextMeshProUGUI LoadingText;
    [SerializeField] private ModelButtonManager ModelButtonManager;
    [SerializeField] private GridLayoutGroup GridLayoutGroup;
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
        if (UIController.Instance.ModelsData == null)
            LoadingText.SetActive(true);
        else CreateButtons();

        if (!ApiController) ApiController = GetComponent<ApiController>();
        if (!ApiController) ApiController = new();
        if (UIController.Instance.ModelsData.Count == 1) GridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        else GridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
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
            modelButton.Id = model.id;
            if (ApiController)
                ApiController.GetModelImage(model.model_image, onSuccess: (image) => modelButton.Image.sprite = image, onError: (error) => Debug.Log(error));
        }

        LoadingText.SetActive(false);
        if (UIController.Instance.ModelsData.Count > 0)
        {
            ModelCountText.SetActive(true);
            ModelCountText.text = "<b>" + UIController.Instance.ModelsData.Count + "</b>" + " Modelos";
        }
        else
        {
            LoadingText.text = "Sin modelos disponibles.";
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