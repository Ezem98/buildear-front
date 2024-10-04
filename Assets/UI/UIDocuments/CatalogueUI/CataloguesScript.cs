using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class CataloguesScript : MonoBehaviour
{
    private UIDocument _document;
    private readonly string[] _categoryNames = { "Paredes", "Techos", "Pisos", "Cimientos", "Aberturas", "Reparaciones" };
    private List<VisualElement> _categoryButtons = new();
    private List<VisualElement> _itemButtons = new();

    // Start is called before the first frame update
    void Start()
    {
        _document = GetComponent<UIDocument>();

        _categoryButtons = _document.rootVisualElement.Query<VisualElement>(className: "category").ToList();
        for (int i = 0; i < _categoryButtons.Count; i++)
        {
            string categoryName = _categoryNames[i];
            _categoryButtons[i].RegisterCallback<ClickEvent>(e => ToggleCategoriesVisibility(categoryName));
        }
    }

    public void ToggleCategoriesVisibility(string categoryName)
    {
        VisualElement categories = _document.rootVisualElement.Query<VisualElement>(className: "categories");
        categories.style.display = DisplayStyle.None;
        VisualElement itemList = _document.rootVisualElement.Query<VisualElement>(className: "itemList");
        itemList.style.display = DisplayStyle.Flex;

        Label itemTitle = _document.rootVisualElement.Query<Label>(name: "itemTitleText");
        itemTitle.text = categoryName;

        _itemButtons = _document.rootVisualElement.Query<VisualElement>(className: "itemContainer").ToList();
        for (int i = 0; i < _itemButtons.Count; i++)
        {
            _itemButtons[i].RegisterCallback<ClickEvent>(e => Pick3DModel());
        }
    }

    public void Pick3DModel()
    {
        SceneManager.LoadScene("Build");

    }
}
