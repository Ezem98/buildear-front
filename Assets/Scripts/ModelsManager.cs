using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class ModelsManager : MonoBehaviour
{

    [SerializeField] private GameObject ModelsContainer;
    [SerializeField] private TextMeshProUGUI ModelCountText;
    [SerializeField] private TextMeshProUGUI LoadingText;
    [SerializeField] private ModelButtonManager ModelButtonManager;
    [SerializeField] private GridLayoutGroup GridLayoutGroup;
    [SerializeField] private ApiController ApiController;
    private static ModelsManager _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            _instance = this;
        }
    }

    private void OnEnable()
    {
        LoadingText.text = "Cargando modelos...";
        LoadingText.SetActive(true);
        ModelCountText.SetActive(false);
        if (UIController.Instance.ComesFromSearch)
        {
            CreateButtons(UIController.Instance.SearchModelsData);
        }
        else
        {
            UIController.Instance.SearchModelsData = null;
            ApiController.GetModelsByCategoryId(UIController.Instance.CurrentCategoryIndex, onSuccess: () =>
            {
                CreateButtons(UIController.Instance.ModelsData);
            });
        }
    }

    private void OnDisable()
    {
        ModelCountText.SetActive(false);
        DestroyButtons();
    }

    public void CreateButtons(List<ModelData> models)
    {
        DestroyButtons();
        ModelCountText.SetActive(false);
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
        if (models.Count == 1) GridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        else GridLayoutGroup.childAlignment = TextAnchor.UpperCenter;

        if (models.Count > 0)
        {
            ModelCountText.SetActive(true);
            string modelLabel = models.Count == 1 ? "Modelo" : "Modelos";
            ModelCountText.text = $"<b>{models.Count}</b> {modelLabel}";
        }
        else
        {
            ModelCountText.SetActive(false);
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
