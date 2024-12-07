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
        Slider.onValueChanged.AddListener(HandleSliderValueChange);
    }

    private void SetUserData()
    {
        UserData userData = UIController.Instance.UserData;
        NameText.text = userData.name;
        SurnameText.text = userData.surname;
        UsernameText.text = userData.username;
        EmailText.text = userData.email;
        experienceLevel = userData.experience_level;
        SliderValueText.text = experienceLevelDictionary[experienceLevel];
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
                password = PasswordText.text,
                newPassword = NewPasswordText.text,
                experience_level = experienceLevel,
                completed_profile = experienceLevel != 0 ? (int)CompletedProfile.Complete : UIController.Instance.UserData.completed_profile,
            };
            ApiController.UpdateUserData(updateUserData, onSuccess: () =>
            {
                UIController.Instance.ScreenHandler("Profile");
            });
        }
    }

    public void HandleSliderValueChange(float value)
    {
        SliderValueText.text = experienceLevelDictionary[(int)value];
        experienceLevel = (int)value;
    }
}
