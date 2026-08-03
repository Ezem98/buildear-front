using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
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
        if (userData == null) return;

        FullNameText.text = StringUtils.ToPascalCase($"{userData.name} {userData.surname}");
        EmailText.text = userData.email;
        if (userData.completed_profile == (int)CompletedProfile.Incomplete)
        {
            CompleteProfileText.gameObject.SetActive(true);
        }
        else
        {
            CompleteProfileText.gameObject.SetActive(false);
        }
        if (!string.IsNullOrWhiteSpace(userData.image))
        {
            ApiController.GetModelImage(
                userData.image,
                onSuccess: (image) => ProfileImage.sprite = image,
                onError: (error) => Debug.LogWarning(error));
        }
    }

    public void Logout()
    {
        void ClearLocalSession()
        {
            UIController.Instance.ClearSession();
            UIController.Instance.ModelsData = null;
            UIController.Instance.ScreenHandler("Onboarding");
        }

        if (ApiController != null && UIController.Instance.HasValidSession())
        {
            ApiController.Logout(ClearLocalSession);
        }
        else
        {
            ClearLocalSession();
        }
    }
}
