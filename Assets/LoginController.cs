
using UnityEngine;
using TMPro;

public class LoginController : MonoBehaviour
{
  public TMP_InputField UsernameInputField;
  public TMP_InputField PasswordInputField;
  [SerializeField] private TextMeshProUGUI ErrorMessage;
  [SerializeField] private ApiController ApiController;
  public void TryToLogin()
  {
    if (ApiController)
    {
      LoginData loginData = new()
      {
        username = UsernameInputField.text,
        password = PasswordInputField.text
      };
      ApiController.Login(loginData, onSuccess: () =>
      {
        UsernameInputField.text = "";
        PasswordInputField.text = "";
        UIController.Instance.SaveData();
      }, onError: (errorMessage) => { ErrorMessage.text = errorMessage; ErrorMessage.gameObject.SetActive(true); });
    }
  }
}
