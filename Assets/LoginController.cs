
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
    [SerializeField] private ApiController ApiController;
    // Start is called before the first frame update
    public Image profileImage;
    public string ImageUrl;
    public string webClientId = "960322424389-ectao9p9imso6jun7tvt8ed837q6gleo.apps.googleusercontent.com";
    public GameObject registerScreen;
    public GameObject loginScreen;

    private GoogleSignInConfiguration configuration;

    // Defer the configuration creation until Awake so the web Client ID
    // Can be set via the property inspector in the Editor.
    void Awake() {
      configuration = new GoogleSignInConfiguration {
            WebClientId = webClientId,
            RequestIdToken = true
      };
    }

    public void OnSignIn() {
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;
        GoogleSignIn.Configuration.RequestEmail = true;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
        OnAuthenticationFinished,TaskScheduler.Default);
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task) {
      if (task.IsFaulted) {
        using (IEnumerator<System.Exception> enumerator =
                task.Exception.InnerExceptions.GetEnumerator()) {
          if (enumerator.MoveNext()) {
            GoogleSignIn.SignInException error =
                    (GoogleSignIn.SignInException)enumerator.Current;
            Debug.LogError("Got Error: " + error.Status + " " + error.Message);
          } else {
            Debug.LogError("Got Unexpected Exception?!?" + task.Exception);
          }
        }
      } else if(task.IsCanceled) {
        Debug.LogError("Canceled");
      } else  {
        Debug.LogError("Welcome: " + task.Result.DisplayName + "!");

        Debug.Log(task.Result.DisplayName);
        Debug.Log(task.Result.Email);
        ImageUrl = task.Result.ImageUrl.ToString();
        Debug.Log(task.Result.ImageUrl.ToString());
        registerScreen.SetActive(false);
        loginScreen.SetActive(false);
        StartCoroutine(LoadProfileImage());
      }
    }

    IEnumerator LoadProfileImage()
    {
        WWW www = new WWW(ImageUrl);    
        yield return www;
        profileImage.sprite=Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
    }

    public void OnSignOut() {
      Debug.Log("Calling SignOut");
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
            });
        }
    }
}
