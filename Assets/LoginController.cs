
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Google;
using TMPro;
using System.Collections;

public class LoginController : MonoBehaviour
{
  public TMP_InputField UsernameInputField;
  public TMP_InputField PasswordInputField;
  [SerializeField] private TextMeshProUGUI ErrorMessage;
  [SerializeField] private ApiController ApiController;
  // Start is called before the first frame update
  private string ImageUrl;
  private readonly string webClientId = "960322424389-ectao9p9imso6jun7tvt8ed837q6gleo.apps.googleusercontent.com";

  private GoogleSignInConfiguration configuration;

  // Defer the configuration creation until Awake so the web Client ID
  // Can be set via the property inspector in the Editor.
  void Awake()
  {
    configuration = new GoogleSignInConfiguration
    {
      WebClientId = webClientId,
      RequestIdToken = true
    };
  }

  public void OnSignIn()
  {
    GoogleSignIn.Configuration = configuration;
    GoogleSignIn.Configuration.UseGameSignIn = false;
    GoogleSignIn.Configuration.RequestIdToken = true;
    GoogleSignIn.Configuration.RequestEmail = true;
    GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished, TaskScheduler.Default);
  }

  internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
  {
    if (task.IsFaulted)
    {
      using (IEnumerator<System.Exception> enumerator =
              task.Exception.InnerExceptions.GetEnumerator())
      {
        if (enumerator.MoveNext())
        {
          GoogleSignIn.SignInException error =
                  (GoogleSignIn.SignInException)enumerator.Current;
          Debug.LogError("Got Error: " + error.Status + " " + error.Message);
        }
        else
        {
          Debug.LogError("Got Unexpected Exception?!?" + task.Exception);
        }
      }
    }
    else if (task.IsCanceled)
    {
      Debug.LogError("Canceled");
    }
    else
    {
      UsernameInputField.text = task.Result.DisplayName;
      PasswordInputField.text = task.Result.IdToken;
      TryToLogin();
    }
  }

  public void OnSignOut()
  {
    //GoogleSignIn.DefaultInstance.SignOut();
  }


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
