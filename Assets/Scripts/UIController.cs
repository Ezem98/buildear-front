using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;


public class UIController : MonoBehaviour
{
    private bool loggedIn = false;
    private bool guestUser = false;
    private string currentScreen = "Onboarding";
    private string previousScreen = "Onboarding";
    private int currentModelIndex;
    private int currentCategoryIndex;
    public int CurrentModelIndex { get => currentModelIndex; set => currentModelIndex = value; }
    public int CurrentCategoryIndex { get => currentCategoryIndex; set => currentCategoryIndex = value; }
    public List<ModelData> ModelsData { get; set; }
    public UserModelData UserModelData { get; set; }
    public List<ModelData> MyModelsData { get; set; }
    public List<ModelData> FavoritesModelsData { get; set; }
    public List<ModelData> SearchModelsData { get; set; }
    public UserData UserData { get; set; }
    public ModelData ModelData { get; set; }
    public bool ComesFromSearch { get; set; }
    public bool LoggedIn { get => loggedIn; set => loggedIn = value; }
    public bool GuestUser { get => guestUser; set => guestUser = value; }
    public string CurrentScreen { get => currentScreen; set => currentScreen = value; }
    public string PreviousScreen { get => previousScreen; set => previousScreen = value; }

    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject onBoarding;
    [SerializeField] private GameObject register;
    [SerializeField] private GameObject login;
    [SerializeField] private GameObject home;
    [SerializeField] private GameObject catalogue;
    [SerializeField] private GameObject models;
    [SerializeField] private GameObject model;
    [SerializeField] private GameObject profile;
    [SerializeField] private GameObject myData;
    [SerializeField] private GameObject favorites;
    [SerializeField] private GameObject footer;
    [SerializeField] private GameObject header;
    private Dictionary<string, GameObject> screenDictionary;
    private Dictionary<string, bool> footerDictionary;
    private Dictionary<string, bool> headerDictionary;
    ObjectSpawner m_ObjectSpawner;

    /// <summary>
    /// The behavior to use to spawn objects.
    /// </summary>
    public ObjectSpawner objectSpawner
    {
        get => m_ObjectSpawner;
        set => m_ObjectSpawner = value;
    }
    private static UIController _instance;

    void Awake()
    {
        TouchSimulation.Enable();
        if (_instance != null)
        {
            Destroy(gameObject); // Si ya existe una instancia, destruir este objetoassss
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Mantener la instancia en todas las escenas
        }

        if (SceneManager.GetActiveScene().name == "UI")
        {
            screenDictionary = new(){
                {"Onboarding", onBoarding},
                {"Register", register},
                {"Login", login},
                {"Home", home},
                {"Catalogue", catalogue},
                {"Models", models},
                {"Model", model},
                {"Profile", profile},
                {"MyData", myData},
                {"Favorites", favorites}
            };

            footerDictionary = new(){
                {"Onboarding", false},
                {"Register", false},
                {"Login", false},
                {"Home", true},
                {"Catalogue", true},
                {"Models", true},
                {"Model", false},
                {"Profile", true},
                {"MyData", true},
                {"Favorites", true}
            };

            headerDictionary = new(){
                {"Onboarding", false},
                {"Register", false},
                {"Login", false},
                {"Home", true},
                {"Catalogue", true},
                {"Models", true},
                {"Model", false},
                {"Profile", false},
                {"MyData", false},
                {"Favorites", false}
            };


            if (loggedIn)
            {
                ScreenHandler("Home");
            }
            else
            {
                ScreenHandler("Onboarding");
            }
        }
    }

    void Start()
    {
        SceneManager.sceneLoaded += (scene, mode) => OnSceneLoaded();
    }

    public void ScreenHandler(string newScreenName)
    {
        previousScreen = currentScreen;
        screenDictionary[currentScreen].SetActive(false);
        screenDictionary[newScreenName].SetActive(true);
        footer.SetActive(footerDictionary[newScreenName]);
        header.SetActive(headerDictionary[newScreenName]);
        currentScreen = newScreenName;
    }

    public static UIController Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIController>();

                // Si no se encuentra la instancia en la escena, crear una nueva
                if (_instance == null)
                {
                    GameObject singletonObject = new();
                    _instance = singletonObject.AddComponent<UIController>();
                    singletonObject.name = typeof(UIController).ToString() + " (Singleton)";

                    // Opcional: Evitar que sea destruido cuando se cambie de escena
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }



    public void SceneHandler(string newSceneName)
    {
        if (newSceneName == "UI")
        {
            // Restaura la pantalla donde estabas antes de salir
            screenDictionary[currentScreen].SetActive(true);
            footer.SetActive(footerDictionary[currentScreen]);
        }
        else
        {
            screenDictionary[currentScreen].SetActive(false);
            footer.SetActive(false);
        }
        SceneManager.LoadScene(newSceneName);
    }

    void OnSceneLoaded()
    {
        if (objectSpawner == null) objectSpawner = FindObjectOfType<ObjectSpawner>();
        if (objectSpawner != null) objectSpawner.spawnOptionId = currentModelIndex;
        else Debug.Log("No se encontró el ObjectSpawner");
    }

    public void ChangeCategory(int categoryIndex)
    {
        currentCategoryIndex = categoryIndex;
        ScreenHandler("Models");
    }

    public void JoinAsGuest()
    {
        guestUser = true;
        ScreenHandler("Home");
    }

    public void GoBack()
    {
        ScreenHandler(previousScreen);
    }
}

