using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class FavoritesManager : MonoBehaviour
{

    [SerializeField] private GameObject ModelsContainer;
    [SerializeField] private TextMeshProUGUI LoadingText;
    [SerializeField] private ModelButtonManager ModelButtonManager;
    [SerializeField] private ApiController ApiController;
    [SerializeField] private GridLayoutGroup GridLayoutGroup;

    private static FavoritesManager _instance;
    void Awake()
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
        LoadingText.SetActive(true);
        ApiController.GetFavoritesModels(UIController.Instance.UserData.id, onSuccess: (models) =>
        {
            CreateButtons();
            if (UIController.Instance.FavoritesModelsData?.Count == 1) GridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
            else GridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        }, onError: (error) => Debug.Log(error));

    }

    public static FavoritesManager Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<FavoritesManager>();

                // Si no se encuentra la instancia en la escena, crear una nueva
                if (_instance == null)
                {
                    GameObject singletonObject = new();
                    _instance = singletonObject.AddComponent<FavoritesManager>();
                    singletonObject.name = typeof(FavoritesManager).ToString() + " (Singleton)";

                    // Opcional: Evitar que sea destruido cuando se cambie de escena
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    private void OnDisable()
    {
        DestroyButtons();
    }

    public void CreateButtons()
    {
        LoadingText.SetActive(true);
        foreach (ModelData model in UIController.Instance.FavoritesModelsData)
        {
            ModelButtonManager modelButton = Instantiate(ModelButtonManager, ModelsContainer.transform); ;
            modelButton.Title.text = model.name;
            modelButton.Id = model.id;
            if (ApiController)
                ApiController.GetModelImage(model.model_image, onSuccess: (image) => modelButton.Image.sprite = image, onError: (error) => Debug.Log(error));
        }

        LoadingText.SetActive(false);
        if (UIController.Instance.FavoritesModelsData.Count == 0)
        {
            LoadingText.text = "Aún no has agregado ninguna construcción a favoritos";
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

    public void AddFavorite(int modelId)
    {
        FavoriteData favoriteData = new()
        {
            user_id = UIController.Instance.UserData.id,
            model_id = modelId
        };
        ApiController.CreateFavorite(favoriteData);
    }

    public void RemoveFavorite(int modelId)
    {
        FavoriteData favoriteData = new()
        {
            user_id = UIController.Instance.UserData.id,
            model_id = modelId
        };
        ApiController.DeleteFavorite(favoriteData);
    }

    public void IsFavorite(int modelId, System.Action<bool> onSuccess)
    {
        FavoriteData favoriteData = new()
        {
            user_id = UIController.Instance.UserData.id,
            model_id = modelId
        };
        ApiController.IsFavorite(favoriteData, onSuccess: (isFavorite) =>
        {
            onSuccess?.Invoke(isFavorite);
        }, onError: (error) => Debug.Log(error));
    }
}
