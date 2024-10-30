using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class MaterialListManager : MonoBehaviour
{

    [SerializeField] private GameObject MaterialsContainer;
    [SerializeField] private MaterialItemManager MaterialItemManager;
    private static MaterialListManager _instance;

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
        if (BuildController.Instance.GuidesDictionary[UIController.Instance.CurrentModelIndex] != null)
        {
            Guide guide = BuildController.Instance.GuidesDictionary[UIController.Instance.CurrentModelIndex];
            CreateMaterialItems(guide.materiales);
        }
    }

    private void OnDisable()
    {
        DestroyButtons();
    }

    public void CreateMaterialItems(List<GuideMaterial> materials)
    {
        foreach (GuideMaterial material in materials)
        {
            MaterialItemManager materialItem = Instantiate(MaterialItemManager, MaterialsContainer.transform); ;
            materialItem.Quantity.text = material.cantidad;
            materialItem.Material.text = material.material;
            materialItem.Utility.text = material.finalidad;
        }
    }

    private void DestroyButtons()
    {
        foreach (Transform child in MaterialsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public static MaterialListManager Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<MaterialListManager>();

                // Si no se encuentra la instancia en la escena, crear una nueva
                if (_instance == null)
                {
                    GameObject singletonObject = new();
                    _instance = singletonObject.AddComponent<MaterialListManager>();
                    singletonObject.name = typeof(MaterialListManager).ToString() + " (Singleton)";

                    // Opcional: Evitar que sea destruido cuando se cambie de escena
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }
}