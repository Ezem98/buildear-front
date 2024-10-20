using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MyDataManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField NameText;
    [SerializeField] private TMP_InputField SurnameText;
    [SerializeField] private TMP_InputField UsernameText;
    [SerializeField] private TMP_InputField EmailText;
    [SerializeField] private TMP_InputField PasswordText;
    [SerializeField] private TMP_InputField NewPasswordText;
    [SerializeField] private ApiController ApiController;

    // Start is called before the first frame update
    private void Start()
    {
        SetUserData();
    }

    private void SetUserData()
    {
        UserData userData = UIController.Instance.UserData;
        NameText.text = userData.name;
        SurnameText.text = userData.surname;
        UsernameText.text = userData.username;
        EmailText.text = userData.email;
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
                experience_level = UIController.Instance.UserData.experience_level,
                completed_profile = UIController.Instance.UserData.completed_profile,
            };
            ApiController.UpdateUserData(updateUserData, onSuccess: () =>
            {
                UIController.Instance.ScreenHandler("Profile");
            });
        }
    }
}
