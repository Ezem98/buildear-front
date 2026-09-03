using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
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
    private string refreshToken;
    private string refreshTokenExpiresAt;
    private bool isDuplicate;
    private Coroutine feedbackCoroutine;
    private GameObject feedbackPanel;
    private TextMeshProUGUI feedbackText;
    public int CurrentConversationId { get => currentConversationId; set => currentConversationId = value; }
    public string AccessToken { get => accessToken; set => accessToken = value; }
    public string AccessTokenExpiresAt { get => accessTokenExpiresAt; set => accessTokenExpiresAt = value; }
    public string RefreshToken
    {
        get => refreshToken;
        set
        {
            refreshToken = value;
            if (string.IsNullOrWhiteSpace(value)) SecureTokenStorage.Delete();
            else SecureTokenStorage.Save(value);
        }
    }
    public string RefreshTokenExpiresAt { get => refreshTokenExpiresAt; set => refreshTokenExpiresAt = value; }
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
    [SerializeField] private GameObject arSessionObject;
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
            isDuplicate = true;
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
        if (
            screenDictionary == null
            || !screenDictionary.TryGetValue(newScreenName, out GameObject newScreen)
            || newScreen == null
        )
        {
            Debug.LogError($"No existe la pantalla '{newScreenName}'.", this);
            ShowUserMessage("No se pudo abrir esa pantalla.", true);
            return;
        }

        if (currentScreen == newScreenName) return;

        if (navigationStack.Count == 0 || navigationStack.Peek() != currentScreen)
            navigationStack.Push(currentScreen);
        previousScreen = currentScreen;
        if (screenDictionary.TryGetValue(currentScreen, out GameObject activeScreen))
            activeScreen?.SetActive(false);
        newScreen.SetActive(true);
        footer?.SetActive(footerDictionary.TryGetValue(newScreenName, out bool showFooter) && showFooter);
        header?.SetActive(headerDictionary.TryGetValue(newScreenName, out bool showHeader) && showHeader);
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

                if (_instance == null)
                {
                    Debug.LogError("No hay un UIController configurado en la escena.");
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
        if (newSceneName == "Build" || newSceneName == "BuildUI")
        {
            EnableBuildMode();
            return;
        }

        if (newSceneName == "UI" && currentScreen == "BuildUI")
        {
            GoBack();
            return;
        }

        if (screenDictionary.ContainsKey(newSceneName))
            ScreenHandler(newSceneName);
    }

    public void EnableBuildMode()
    {
        if (buildUI == null)
            return;

        if (currentScreen != "BuildUI")
        {
            navigationStack.Push(currentScreen);
            previousScreen = currentScreen;
            if (screenDictionary.TryGetValue(currentScreen, out GameObject currentScreenObject))
                currentScreenObject?.SetActive(false);
            currentScreen = "BuildUI";
        }

        footer?.SetActive(false);
        header?.SetActive(false);
        canvas?.SetActive(false);

        arSessionObject?.SetActive(true);
        XRComponent?.SetActive(true);

        if (objectSpawner != null)
            objectSpawner.spawnOptionId = currentModelIndex;

        buildUI.SetActive(true);
    }

    public void DisableBuildMode()
    {
        BuildController.Instance?.EndBuildSession();

        buildUI.SetActive(false);

        XRComponent?.SetActive(false);
        arSessionObject?.SetActive(false);
        canvas?.SetActive(true);
    }

    public void ClearBuildWorkspace()
    {
        objectSpawner?.ClearSpawnedObjects();

        ARSession arSession = arSessionObject != null
            ? arSessionObject.GetComponent<ARSession>()
            : null;
        if (arSession != null && arSessionObject.activeInHierarchy)
            arSession.Reset();
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

        if (currentScreen == "BuildUI")
        {
            DisableBuildMode();
        }
        previousScreen = currentScreen;
        if (!screenDictionary.TryGetValue(newScreenName, out GameObject newScreen))
        {
            ShowUserMessage("No se pudo volver a la pantalla anterior.", true);
            return;
        }
        if (screenDictionary.TryGetValue(currentScreen, out GameObject activeScreen))
            activeScreen?.SetActive(false);
        newScreen?.SetActive(true);
        footer?.SetActive(footerDictionary.TryGetValue(newScreenName, out bool showFooter) && showFooter);
        header?.SetActive(headerDictionary.TryGetValue(newScreenName, out bool showHeader) && showHeader);
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
        if (isDuplicate) return;

        PlayerPrefs.SetInt("loggedIn", loggedIn ? 1 : 0);
        if (!loggedIn)
        {
            PlayerPrefs.DeleteKey("accessToken");
            PlayerPrefs.DeleteKey("accessTokenExpiresAt");
            PlayerPrefs.DeleteKey("refreshTokenExpiresAt");
            SecureTokenStorage.Delete();
            PlayerPrefs.DeleteKey("userData");
        }
        else
        {
            PlayerPrefs.SetString("accessToken", accessToken ?? "");
            PlayerPrefs.SetString("accessTokenExpiresAt", accessTokenExpiresAt ?? "");
            PlayerPrefs.SetString("refreshTokenExpiresAt", refreshTokenExpiresAt ?? "");
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
        refreshToken = SecureTokenStorage.Load();
        refreshTokenExpiresAt = PlayerPrefs.GetString("refreshTokenExpiresAt", "");
        if (LoggedIn && !HasValidSession() && !HasRefreshSession())
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
        // Los tokens anteriores al sistema de sesiones no tenían vencimiento.
        // El backend actual no los acepta, por lo que deben forzar un nuevo login.
        if (string.IsNullOrWhiteSpace(accessTokenExpiresAt)) return false;

        return IsFutureTimestamp(accessTokenExpiresAt);
    }

    public bool HasRefreshSession()
    {
        return !string.IsNullOrWhiteSpace(refreshToken)
            && IsFutureTimestamp(refreshTokenExpiresAt);
    }

    public bool HasAuthenticatedSession()
    {
        return LoggedIn && (HasValidSession() || HasRefreshSession());
    }

    private static bool IsFutureTimestamp(string value)
    {
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime expiresAt
        ) && expiresAt > DateTime.UtcNow;
    }

    public void ClearSession()
    {
        ClearBuildWorkspace();
        loggedIn = false;
        accessToken = null;
        accessTokenExpiresAt = null;
        RefreshToken = null;
        refreshTokenExpiresAt = null;
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
        PlayerPrefs.DeleteKey("refreshTokenExpiresAt");
        PlayerPrefs.DeleteKey("userData");
        PlayerPrefs.DeleteKey("conversationsData");
        PlayerPrefs.DeleteKey("currentConversationId");
        PlayerPrefs.SetInt("loggedIn", 0);
        PlayerPrefs.Save();
    }

    public void ExpireSession(string message = null)
    {
        if (currentScreen == "BuildUI") DisableBuildMode();
        canvas?.SetActive(true);
        ClearSession();
        if (screenDictionary != null && screenDictionary.ContainsKey("Login"))
            ScreenHandler("Login");
        navigationStack.Clear();
        navigationStack.Push("Onboarding");
        ShowUserMessage(
            string.IsNullOrWhiteSpace(message)
                ? "Tu sesión venció. Iniciá sesión nuevamente."
                : message,
            true
        );
    }

    public void ShowUserMessage(string message, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        EnsureFeedbackPanel();
        if (feedbackPanel == null || feedbackText == null) return;

        feedbackText.text = message;
        feedbackPanel.GetComponent<Image>().color = isError
            ? new Color(0.55f, 0.12f, 0.12f, 0.96f)
            : new Color(0.12f, 0.42f, 0.25f, 0.96f);
        feedbackPanel.SetActive(true);
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    private void EnsureFeedbackPanel()
    {
        if (feedbackPanel != null) return;

        GameObject feedbackCanvas = new("UserFeedbackCanvas", typeof(Canvas), typeof(CanvasScaler));
        feedbackCanvas.transform.SetParent(transform, false);
        Canvas overlay = feedbackCanvas.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 1000;
        CanvasScaler scaler = feedbackCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        feedbackPanel = new GameObject("UserFeedback", typeof(RectTransform), typeof(Image));
        feedbackPanel.transform.SetParent(feedbackCanvas.transform, false);
        RectTransform panelRect = feedbackPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.88f);
        panelRect.anchorMax = new Vector2(0.92f, 0.98f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject textObject = new("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(feedbackPanel.transform, false);
        feedbackText = textObject.GetComponent<TextMeshProUGUI>();
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.fontSize = 34f;
        feedbackText.color = Color.white;
        feedbackText.enableWordWrapping = true;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 12f);
        textRect.offsetMax = new Vector2(-24f, -12f);
        feedbackPanel.SetActive(false);
    }

    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSecondsRealtime(4f);
        feedbackPanel?.SetActive(false);
        feedbackCoroutine = null;
    }

    private void OnDisable()
    {
        UnsubscribeFromObjectSpawner();
        if (isDuplicate) return;
        SaveData();
    }

    private void OnDestroy()
    {
        UnsubscribeFromObjectSpawner();
        if (isDuplicate) return;
        SaveData();
        if (_instance == this) _instance = null;
    }
}

