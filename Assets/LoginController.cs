using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoginController : MonoBehaviour
{
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;
    private ApiController ApiController;
    // Start is called before the first frame update
    void Start()
    {
        if (!ApiController)
            ApiController = FindObjectOfType<ApiController>();
    }

    public void TryToLogin()
    {
        if (ApiController)
        {
            LoginData loginData = new();
            loginData.username = UsernameInputField.text;
            loginData.password = PasswordInputField.text;
            ApiController.Login(loginData);
        }
    }
}
