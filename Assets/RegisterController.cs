using TMPro;
using UnityEngine;

public class RegisterController : MonoBehaviour
{
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;
    public TMP_InputField EmailInputField;
    [SerializeField] private ApiController ApiController;

    public void TryToRegister()
    {
        if (ApiController)
        {
            RegisterData registerData = new()
            {
                username = UsernameInputField.text,
                email = EmailInputField.text,
                password = PasswordInputField.text
            };
            ApiController.Register(registerData);
        }
    }
}
