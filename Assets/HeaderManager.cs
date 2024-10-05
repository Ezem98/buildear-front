using TMPro;
using UnityEngine;

public class HeaderManager : MonoBehaviour
{
    public TMP_InputField SearchInputField;
    [SerializeField] private ApiController ApiController;

    public void TryToSearch()
    {
        if (ApiController)
        {
            ApiController.SearchModels(SearchInputField.text);
            SearchInputField.text = "";
        }
    }
}
