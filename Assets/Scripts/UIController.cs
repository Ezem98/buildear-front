using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Newtonsoft.Json;


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
    private List<ConversationData> conversationsData = new();
    public List<ConversationData> ConversationsData { get => conversationsData; set => conversationsData = value; }
    public UserData UserData { get; set; }
    public ModelData ModelData { get; set; }
    private int currentConversationId = -1;
    private string accessToken;
    private string accessTokenExpiresAt;
    public int CurrentConversationId { get => currentConversationId; set => currentConversationId = value; }
    public string AccessToken { get => accessToken; set => accessToken = value; }
    public string AccessTokenExpiresAt { get => accessTokenExpiresAt; set => accessTokenExpiresAt = value; }
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
    [SerializeField] private GameObject ChatHistory;
    [SerializeField] private GameObject ChatHistoryItem;
    private Dictionary<string, GameObject> screenDictionary;
    private Dictionary<string, bool> footerDictionary;
    private Dictionary<string, bool> headerDictionary;
    public ObjectSpawner m_ObjectSpawner;
    public ApiController ApiController;
    private ObjectSpawner subscribedObjectSpawner;

    /// <summary>
    /// The behavior to use to spawn objects.
    /// </summary>
    public ObjectSpawner objectSpawner
    {
        get => m_ObjectSpawner;
        set
        {
            if (m_ObjectSpawner == value)
                return;

            UnsubscribeFromObjectSpawner();
            m_ObjectSpawner = value;

            if (isActiveAndEnabled)
                SubscribeToObjectSpawner();
        }
    }
    private static UIController _instance;

    void Awake()
    {
        TouchSimulation.Enable();
        navigationStack.Push("Onboarding");

        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

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
                {"BuildUI", buildUI},
                {"ChatHistory", ChatHistory},
                {"ChatHistoryItem", ChatHistoryItem}
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
                {"BuildUI", false},
                {"ChatHistory", true},
                {"ChatHistoryItem", true}
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
                {"BuildUI", false},
                {"ChatHistory", false},
                {"ChatHistoryItem", false}
            };
        LoadData();
    }

    void Start()
    {
        if (loggedIn)
        {
            guestUser = false;
            ScreenHandler("Home");
        }
        else
        {
            ScreenHandler("Onboarding");
        }
    }

    private void OnEnable()
    {
        SubscribeToObjectSpawner();
    }

    private void SubscribeToObjectSpawner()
    {
        if (m_ObjectSpawner == null || subscribedObjectSpawner == m_ObjectSpawner)
            return;

        UnsubscribeFromObjectSpawner();
        m_ObjectSpawner.objectSpawned += OnObjectSpawned;
        subscribedObjectSpawner = m_ObjectSpawner;
    }

    private void UnsubscribeFromObjectSpawner()
    {
        if (subscribedObjectSpawner != null)
            subscribedObjectSpawner.objectSpawned -= OnObjectSpawned;

        subscribedObjectSpawner = null;
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
        ComesFromSearch = false;
        SearchModelsData = null;
        ScreenHandler("Models");
    }

    public void JoinAsGuest()
    {
        guestUser = true;
        ScreenHandler("Home");
    }

    public void GoBack()
    {
        string newScreenName = ResolveBackDestination();
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
    }

    private string ResolveBackDestination()
    {
        while (navigationStack.Count > 0)
        {
            string destination = navigationStack.Pop();
            if (destination != currentScreen && screenDictionary.ContainsKey(destination))
                return destination;
        }

        return "Home";
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt("loggedIn", loggedIn ? 1 : 0);
        if (!loggedIn)
        {
            PlayerPrefs.DeleteKey("accessToken");
            PlayerPrefs.DeleteKey("accessTokenExpiresAt");
            PlayerPrefs.DeleteKey("userData");
        }
        else
        {
            PlayerPrefs.SetString("accessToken", accessToken ?? "");
            PlayerPrefs.SetString("accessTokenExpiresAt", accessTokenExpiresAt ?? "");
        }
        if (objectSpawner != null)
            PlayerPrefs.SetInt("spawnOptionId", objectSpawner.spawnOptionId);
        if (UserData != null)
        {
            string userJsonData = JsonConvert.SerializeObject(UserData);
            PlayerPrefs.SetString("userData", userJsonData);
        }
        if (ModelData != null)
        {
            string modelJsonData = JsonConvert.SerializeObject(ModelData);
            PlayerPrefs.SetString("modelData", modelJsonData);
        }
        if (ConversationsData != null)
        {
            string conversationsJsonData = SerializeConversations(ConversationsData);
            PlayerPrefs.SetString("conversationsData", conversationsJsonData);
        }
        if (currentConversationId != -1)
        {
            PlayerPrefs.SetInt("currentConversationId", currentConversationId);
        }
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        LoggedIn = PlayerPrefs.GetInt("loggedIn", 0) == 1;
        accessToken = PlayerPrefs.GetString("accessToken", "");
        accessTokenExpiresAt = PlayerPrefs.GetString("accessTokenExpiresAt", "");
        if (LoggedIn && !HasValidSession())
        {
            ClearSession();
        }
        if (objectSpawner != null)
            objectSpawner.spawnOptionId = PlayerPrefs.GetInt("spawnOptionId", -1);
        string userJsonData = PlayerPrefs.GetString("userData", "{}");
        UserData = DeserializeOrDefault<UserData>(userJsonData);
        if (!LoggedIn || UserData == null || UserData.id <= 0)
        {
            if (LoggedIn) ClearSession();
            UserData = null;
        }
        string modelJsonData = PlayerPrefs.GetString("modelData", "{}");
        ModelData = DeserializeOrDefault<ModelData>(modelJsonData);
        string conversationsJsonData = PlayerPrefs.GetString("conversationsData", "[]");
        ConversationsData = DeserializeConversations(conversationsJsonData);
        currentConversationId = PlayerPrefs.GetInt("currentConversationId", -1);
    }

    public static string SerializeConversations(List<ConversationData> conversations)
    {
        return JsonConvert.SerializeObject(conversations ?? new List<ConversationData>());
    }

    public static List<ConversationData> DeserializeConversations(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<ConversationData>();

        try
        {
            return JsonConvert.DeserializeObject<List<ConversationData>>(json)
                ?? new List<ConversationData>();
        }
        catch (JsonException)
        {
            return new List<ConversationData>();
        }
    }

    public static T DeserializeOrDefault<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public bool HasValidSession()
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return false;
        if (string.IsNullOrWhiteSpace(accessTokenExpiresAt)) return true;

        return DateTime.TryParse(
            accessTokenExpiresAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime expiresAt
        ) && expiresAt > DateTime.UtcNow;
    }

    public void ClearSession()
    {
        loggedIn = false;
        accessToken = null;
        accessTokenExpiresAt = null;
        currentConversationId = -1;
        UserData = null;
        MyModelsData = null;
        ModelsData = null;
        FavoritesModelsData = null;
        SearchModelsData = null;
        ComesFromSearch = false;
        ConversationsData = new();
        PlayerPrefs.DeleteKey("accessToken");
        PlayerPrefs.DeleteKey("accessTokenExpiresAt");
        PlayerPrefs.DeleteKey("userData");
        PlayerPrefs.DeleteKey("conversationsData");
        PlayerPrefs.DeleteKey("currentConversationId");
        PlayerPrefs.SetInt("loggedIn", 0);
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        UnsubscribeFromObjectSpawner();
        SaveData();
    }

    private void OnDestroy()
    {
        UnsubscribeFromObjectSpawner();
        SaveData();
    }
}

