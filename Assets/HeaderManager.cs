using TMPro;
using UnityEngine;

public class HeaderManager : MonoBehaviour
{
    public TMP_InputField SearchInputField;
    private ApiController ApiController;

    // Start is called before the first frame update
    void Start()
    {
        if (!ApiController)
            ApiController = FindObjectOfType<ApiController>();
    }

    public void TryToSearch()
    {
        if (ApiController)
        {
            ApiController.SearchModels(SearchInputField.text);
            SearchInputField.text = "";
        }
    }
}
