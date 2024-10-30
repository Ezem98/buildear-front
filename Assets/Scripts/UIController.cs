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
    private Stack<string> navigationStack = new();
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
    [SerializeField] private GameObject buildUI;
    [SerializeField] private GameObject XRComponent;
    [SerializeField] private GameObject UIManager;
    private Dictionary<string, GameObject> screenDictionary;
    private Dictionary<string, bool> footerDictionary;
    private Dictionary<string, bool> headerDictionary;
    public ObjectSpawner m_ObjectSpawner;
    public ApiController ApiController;

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
        navigationStack.Push("Onboarding");
        if (_instance != null)
        {
            Destroy(gameObject); // Si ya existe una instancia, destruir este objetoassss
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Mantener la instancia en todas las escenas
        }

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
                {"Favorites", favorites},
                {"BuildUI", buildUI}
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
                {"Favorites", true},
                {"BuildUI", false}
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
                {"Favorites", false},
                {"BuildUI", false}
            };
        LoadData();
    }

    void Start()
    {
        if (loggedIn)
        {
            Debug.Log("Logged in: " + UserData.username);
            ApiController.GetModelsByUserId(UserData.id, onSuccess: (modelData) =>
                    {
                        ScreenHandler("Home");
                    }, onError: (error) =>
                    {
                        Debug.Log(error);
                    });
        }
        else
        {
            ScreenHandler("Onboarding");
        }
    }

    void Update()
    {
        if (objectSpawner != null)
            objectSpawner.objectSpawned += OnObjectSpawned;
    }

    public void ScreenHandler(string newScreenName)
    {
        navigationStack.Push(currentScreen);
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

    public void OnObjectSpawned(GameObject spawnedObject)
    {
        if (spawnedObject != null && ModelData?.category_id == (int)Categories.Floor)
        {
            spawnedObject.transform.position = new Vector3(spawnedObject.transform.position.x, 0.01f, spawnedObject.transform.position.z);
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

    public void EnableBuildMode()
    {

        UIManager.SetActive(false);
        buildUI.SetActive(true);
        objectSpawner.spawnOptionId = currentModelIndex;
    }

    public void DisableBuildMode()
    {
        UIManager.SetActive(true);
        buildUI.SetActive(false);
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
        Debug.Log("Antes del pop: " + navigationStack.Count);
        string newScreenName = navigationStack.Pop();
        Debug.Log("Despues del pop: " + navigationStack.Count);
        Debug.Log("newScreenName: " + newScreenName);
        if (newScreenName == "Login")
        {
            newScreenName = "Home";
        }
        else if (currentScreen == "BuildUI")
        {
            DisableBuildMode();
        }
        previousScreen = currentScreen;
        screenDictionary[currentScreen].SetActive(false);
        screenDictionary[newScreenName].SetActive(true);
        footer.SetActive(footerDictionary[newScreenName]);
        header.SetActive(headerDictionary[newScreenName]);
        currentScreen = newScreenName;
        Debug.Log("currentScreen: " + currentScreen);
        Debug.Log("previousScreen: " + previousScreen);
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt("loggedIn", 1);
        PlayerPrefs.SetInt("spawnOptionId", objectSpawner.spawnOptionId);
        string userJsonData = JsonUtility.ToJson(UserData);
        PlayerPrefs.SetString("userData", userJsonData);
        string modelJsonData = JsonUtility.ToJson(UserData);
        PlayerPrefs.SetString("modelData", modelJsonData);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        LoggedIn = PlayerPrefs.GetInt("loggedIn", 0) == 1;
        objectSpawner.spawnOptionId = PlayerPrefs.GetInt("spawnOptionId", -1);
        string userJsonData = PlayerPrefs.GetString("userData", "{}");
        UserData = JsonUtility.FromJson<UserData>(userJsonData);
        string modelJsonData = PlayerPrefs.GetString("modelData", "{}");
        ModelData = JsonUtility.FromJson<ModelData>(modelJsonData);
    }

    private void OnDisable()
    {
        SaveData();
    }

    private void OnDestroy()
    {
        SaveData();
    }
}

