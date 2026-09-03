using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyDataManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField NameText;
    [SerializeField] private TMP_InputField SurnameText;
    [SerializeField] private TMP_InputField UsernameText;
    [SerializeField] private TMP_InputField EmailText;
    [SerializeField] private TMP_InputField PasswordText;
    [SerializeField] private TMP_InputField NewPasswordText;
    [SerializeField] private Slider Slider;
    [SerializeField] private TextMeshProUGUI SliderValueText;
    [SerializeField] private ApiController ApiController;
    private TextMeshProUGUI inlineError;
    private readonly Dictionary<int, string> experienceLevelDictionary = new(){
        { 0, "Selecciona tu nivel de experiencia aproximado para continuar" },
        { 1, "Principiante" },
        { 2, "Intermedio" },
        { 3, "Avanzado" },
    };
    private int experienceLevel = 0;

    private void Awake()
    {
        CreatePasswordButton();
        CreateInlineError();
    }

    // Start is called before the first frame update
    private void OnEnable()
    {
        SetUserData();
        Slider.onValueChanged.RemoveListener(HandleSliderValueChange);
        Slider.onValueChanged.AddListener(HandleSliderValueChange);
    }

    private void OnDisable()
    {
        Slider?.onValueChanged.RemoveListener(HandleSliderValueChange);
    }

    private void SetUserData()
    {
        UserData userData = UIController.Instance.UserData;
        if (userData == null)
        {
            UIController.Instance.ExpireSession();
            return;
        }
        NameText.text = userData.name;
        SurnameText.text = userData.surname;
        UsernameText.text = userData.username;
        EmailText.text = userData.email;
        experienceLevel = userData.experience_level;
        SliderValueText.text = experienceLevelDictionary.TryGetValue(
            experienceLevel,
            out string label
        ) ? label : experienceLevelDictionary[0];
        Slider.value = experienceLevel;
    }

    public void TryUpdateUserInfo()
    {
        if (ApiController)
        {
            UpdateUserData updateUserData = new()
            {
                username = UsernameText.text,
                email = EmailText.text.ToLower(),
                experience_level = experienceLevel,
                completed_profile = experienceLevel != 0 ? (int)CompletedProfile.Complete : UIController.Instance.UserData.completed_profile,
            };
            ApiController.UpdateUserData(updateUserData, onSuccess: () =>
            {
                SetInlineError(null);
                UIController.Instance.ShowUserMessage("Perfil actualizado.");
                UIController.Instance.ScreenHandler("Profile");
            }, onError: SetInlineError);
        }
    }

    public void TryChangePassword()
    {
        string currentPassword = PasswordText.text;
        string newPassword = NewPasswordText.text;
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            SetInlineError("Ingresá tu contraseña actual.");
            return;
        }
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            SetInlineError("La contraseña nueva debe tener al menos 8 caracteres.");
            return;
        }

        ApiController.ChangePassword(
            new UpdatePasswordData
            {
                password = currentPassword,
                newPassword = newPassword
            },
            onSuccess: () =>
            {
                PasswordText.text = "";
                NewPasswordText.text = "";
                SetInlineError(null);
            },
            onError: SetInlineError
        );
    }

    public void HandleSliderValueChange(float value)
    {
        experienceLevel = (int)value;
        SliderValueText.text = experienceLevelDictionary.TryGetValue(
            experienceLevel,
            out string label
        ) ? label : experienceLevelDictionary[0];
    }

    private void CreatePasswordButton()
    {
        Transform submitTransform = transform.Find("SubmitButton");
        if (submitTransform == null) return;

        RectTransform profileButtonRect = submitTransform.GetComponent<RectTransform>();
        profileButtonRect.anchoredPosition = new Vector2(
            profileButtonRect.anchoredPosition.x,
            -560f
        );

        GameObject passwordButtonObject = Instantiate(
            submitTransform.gameObject,
            transform
        );
        passwordButtonObject.name = "ChangePasswordButton";
        RectTransform passwordButtonRect = passwordButtonObject.GetComponent<RectTransform>();
        passwordButtonRect.anchoredPosition = new Vector2(
            passwordButtonRect.anchoredPosition.x,
            -690f
        );
        Button passwordButton = passwordButtonObject.GetComponent<Button>();
        passwordButton.onClick = new Button.ButtonClickedEvent();
        passwordButton.onClick.AddListener(TryChangePassword);

        TextMeshProUGUI tmpLabel = passwordButtonObject.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpLabel != null) tmpLabel.text = "Cambiar contraseña";
        Text legacyLabel = passwordButtonObject.GetComponentInChildren<Text>();
        if (legacyLabel != null) legacyLabel.text = "Cambiar contraseña";
    }

    private void CreateInlineError()
    {
        GameObject errorObject = new("ProfileError", typeof(RectTransform), typeof(TextMeshProUGUI));
        errorObject.transform.SetParent(transform, false);
        RectTransform errorRect = errorObject.GetComponent<RectTransform>();
        errorRect.anchorMin = new Vector2(0.08f, 0f);
        errorRect.anchorMax = new Vector2(0.92f, 0f);
        errorRect.anchoredPosition = new Vector2(0f, 65f);
        errorRect.sizeDelta = new Vector2(0f, 90f);
        inlineError = errorObject.GetComponent<TextMeshProUGUI>();
        inlineError.alignment = TextAlignmentOptions.Center;
        inlineError.fontSize = 28f;
        inlineError.color = new Color(0.8f, 0.12f, 0.12f);
        inlineError.enableWordWrapping = true;
        inlineError.gameObject.SetActive(false);
    }

    private void SetInlineError(string message)
    {
        if (inlineError == null) return;
        bool visible = !string.IsNullOrWhiteSpace(message);
        inlineError.text = visible ? message : "";
        inlineError.gameObject.SetActive(visible);
    }
}
