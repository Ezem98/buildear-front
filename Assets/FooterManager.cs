using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FooterManager : MonoBehaviour
{
    [SerializeField] Button homeButton;
    [SerializeField] Button catalogueButton;
    [SerializeField] Button favoritesButton;
    [SerializeField] Button profileButton;
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
        }
    }
}
