using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI FullNameText;
    [SerializeField] private TextMeshProUGUI EmailText;
    [SerializeField] private TextMeshProUGUI CompleteProfileText;
    [SerializeField] private Image ProfileImage;
    [SerializeField] private ApiController ApiController;
    // Start is called before the first frame update
    void OnEnable()
    {
        SetProfileData();
    }

    public void SetProfileData()
    {
        UserData userData = UIController.Instance.UserData;
        FullNameText.text = StringUtils.ToPascalCase($"{userData.name} {userData.surname}");
        EmailText.text = userData.email;
        ApiController.GetModelImage(userData.image, onSuccess: (image) => ProfileImage.sprite = image, onError: (error) => Debug.Log(error));
    }

    public void Logout()
    {
        UIController.Instance.LoggedIn = false;
        UIController.Instance.UserData = null;
        UIController.Instance.MyModelsData = null;
        UIController.Instance.ModelsData = null;
        UIController.Instance.FavoritesModelsData = null;
        UIController.Instance.ScreenHandler("Login");
    }
}
