using TMPro;
using UnityEngine;

public class RegisterController : MonoBehaviour
{
    public TMP_InputField NameInputField;
    public TMP_InputField SurnameInputField;
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
                name = NameInputField.text.ToLower(),
                surname = SurnameInputField.text.ToLower(),
                username = UsernameInputField.text,
                email = EmailInputField.text.ToLower(),
                password = PasswordInputField.text
            };
            ApiController.Register(registerData, onSuccess: () =>
            {
                NameInputField.text = "";
                SurnameInputField.text = "";
                UsernameInputField.text = "";
                EmailInputField.text = "";
                PasswordInputField.text = "";
            });
        }
    }
}
