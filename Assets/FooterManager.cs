using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class FooterManager : MonoBehaviour
{
    [SerializeField] Button homeButton;
    [SerializeField] Button catalogueButton;
    [SerializeField] Button favoritesButton;
    [SerializeField] Button profileButton;
    [SerializeField] Button buildButton;
    private Dictionary<string, Button> buttonDictionary;
    // Start is called before the first frame update

    private void Awake()
    {
        buttonDictionary = new()
        {
            { "Home", homeButton },
            { "Catalogue", catalogueButton },
            { "Favorites", favoritesButton },
            { "Profile", profileButton }
        };
    }
    void OnEnable()
    {
        if (UIController.Instance.GuestUser)
        {
            buttonDictionary["Profile"].interactable = false;
            buttonDictionary["Favorites"].interactable = false;
            buildButton.interactable = false;
        }
        else if (UIController.Instance.MyModelsData?.Count > 0)
        {
            ModelData modelData = UIController.Instance.MyModelsData.Last();
            UIController.Instance.CurrentModelIndex = modelData.id;
            UIController.Instance.objectSpawner.spawnOptionId = modelData.id;
            UIController.Instance.ModelData = modelData;
            buildButton.interactable = true;
        }
    }
}
