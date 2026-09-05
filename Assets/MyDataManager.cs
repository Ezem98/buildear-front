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
    [SerializeField] private Button ChangePasswordButton;
    [SerializeField] private TextMeshProUGUI inlineError;
    private readonly Dictionary<int, string> experienceLevelDictionary = new(){
        { 0, "Selecciona tu nivel de experiencia aproximado para continuar" },
        { 1, "Principiante" },
        { 2, "Intermedio" },
        { 3, "Avanzado" },
    };
    private int experienceLevel = 0;

    // Start is called before the first frame update
    private void OnEnable()
    {
        SetUserData();
        Slider.onValueChanged.RemoveListener(HandleSliderValueChange);
        Slider.onValueChanged.AddListener(HandleSliderValueChange);
        ChangePasswordButton?.onClick.RemoveListener(TryChangePassword);
        ChangePasswordButton?.onClick.AddListener(TryChangePassword);
    }

    private void OnDisable()
    {
        Slider?.onValueChanged.RemoveListener(HandleSliderValueChange);
        ChangePasswordButton?.onClick.RemoveListener(TryChangePassword);
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

    private void SetInlineError(string message)
    {
        if (inlineError == null) return;
        bool visible = !string.IsNullOrWhiteSpace(message);
        inlineError.text = visible ? message : "";
        inlineError.gameObject.SetActive(visible);
    }
}
