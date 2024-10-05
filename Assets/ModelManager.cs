using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class ModelManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TitleText;
    [SerializeField] private TextMeshProUGUI DescriptionText;
    [SerializeField] private TextMeshProUGUI UsersCountText;
    [SerializeField] private Image Image;
    [SerializeField] private FavoritesManager FavoritesManager;
    [SerializeField] private Button FavoriteButton;
    private bool IsFav { get; set; }
    [SerializeField] private ApiController ApiController;
    // Start is called before the first frame update
    void OnEnable()
    {
        Debug.Log(UIController.Instance.PreviousScreen);
        int modelId = UIController.Instance.CurrentModelIndex;
        ModelData model = null;
        if (UIController.Instance.PreviousScreen == "Home")
        {
            model = UIController.Instance.MyModelsData.Find(m => m.id == modelId);
        }
        else if (UIController.Instance.PreviousScreen == "Models")
        {
            Debug.Log("ModelsData: " + UIController.Instance.ModelsData?.Count);
            model = UIController.Instance.ModelsData.Find(m => m.id == modelId);
        }
        else if (UIController.Instance.PreviousScreen == "Favorites")
        {
            Debug.Log("FavoritesModelsData: " + UIController.Instance.FavoritesModelsData.Count);
            model = UIController.Instance.FavoritesModelsData.Find(x => x.id == modelId);
        }

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
            IsFavorite(() =>
            {
                if (IsFav)
                {
                    FavoriteButton.transform.GetChild(0).SetActive(false);
                    FavoriteButton.transform.GetChild(1).SetActive(true);
                }
                else
                {
                    FavoriteButton.transform.GetChild(0).SetActive(true);
                    FavoriteButton.transform.GetChild(1).SetActive(false);
                }
            });
        }
    }

    public void ToggleFavorite()
    {
        IsFavorite(() =>
        {
            if (IsFav)
            {
                FavoritesManager.RemoveFavorite(UIController.Instance.CurrentModelIndex);
                FavoriteButton.transform.GetChild(0).SetActive(true);
                FavoriteButton.transform.GetChild(1).SetActive(false);
            }
            else
            {
                FavoritesManager.AddFavorite(UIController.Instance.CurrentModelIndex);
                FavoriteButton.transform.GetChild(0).SetActive(false);
                FavoriteButton.transform.GetChild(1).SetActive(true);
            }
        });

    }

    private void IsFavorite(System.Action onSuccess)
    {
        FavoriteButton.interactable = false;
        FavoritesManager.IsFavorite(UIController.Instance.CurrentModelIndex, onSuccess: (isFavorite) =>
        {
            IsFav = isFavorite;
            FavoriteButton.interactable = true;
            onSuccess?.Invoke();
        });
    }

    public void GoBack()
    {
        UIController.Instance.ScreenHandler(UIController.Instance.PreviousScreen);
    }
}
