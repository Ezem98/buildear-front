using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BuildController : MonoBehaviour
{
    public GameObject GuideResponse;
    public GameObject LoadingModal;
    public GameObject FinishModal;
    public GameObject RulerManager;
    public GameObject RulerPlaceButton;
    public GameObject BackToSpawnModeButton;
    public GameObject Gyroscope;
    public GameObject MaterialList;
    public Button MaterialListButton;
    public Button GuideButton;
    public Button FinishButton;
    public GameObject ToolbarButton;
    public GameObject ObjectSpawner;
    public GameObject CameraPivot;
    public TextMeshProUGUI StepTitle;
    public TextMeshProUGUI StepDescription;
    public TextMeshProUGUI StepCount;
    public TextMeshProUGUI CostText;
    public TextMeshProUGUI TimeText;
    public ARPlaneManager ARPlaneManager;
    public ApiController ApiController;
    private readonly Dictionary<string, PlaneDetectionMode> detectionModeDictionary = new() {
        { "horizontal", PlaneDetectionMode.Horizontal },
        { "vertical", PlaneDetectionMode.Vertical },
    };
    private PlaneDetectionMode previousDetectionMode;

    public Guide Guide { get; set; }
    public Paso CurrentStep { get; set; }

    private static BuildController _instance;

    public void BackToUI()
    {
        UIController.Instance.SceneHandler("UI");
        GameObject.Find("UI").SetActive(false);
        GameObject.Find("XR Origin (AR Rig)").SetActive(false);
    }

    public void BackToSpawnMode()
    {
        RulerManager.SetActive(false);
        RulerPlaceButton.SetActive(false);
        BackToSpawnModeButton.SetActive(false);
        CameraPivot.SetActive(false);
        Gyroscope.SetActive(false);
        MaterialList.SetActive(false);
        ARPlaneManager.requestedDetectionMode = previousDetectionMode;
        ObjectSpawner.SetActive(true);
        ToolbarButton.SetActive(true);
    }

    private void Awake()
    {
        ARPlaneManager.requestedDetectionMode = detectionModeDictionary[UIController.Instance.ModelData.position];
        previousDetectionMode = detectionModeDictionary[UIController.Instance.ModelData.position];

        if (Guide != null)
        {
            MaterialListButton.interactable = true;
            GuideButton.interactable = true;
            FinishButton.interactable = true;
        }

        if (_instance != null)
        {
            Destroy(gameObject); // Si ya existe una instancia, destruir este objeto
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Mantener la instancia en todas las escenas
        }
    }

    public static BuildController Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildController>();

                // Si no se encuentra la instancia en la escena, crear una nueva
                if (_instance == null)
                {
                    GameObject singletonObject = new();
                    _instance = singletonObject.AddComponent<BuildController>();
                    singletonObject.name = typeof(BuildController).ToString() + " (Singleton)";
                    // Opcional: Evitar que sea destruido cuando se cambie de escena
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    public void StepForward()
    {
        if (CurrentStep.paso == Guide.pasos.Count) return;
        CurrentStep = Guide.pasos.Find(x => x.paso == CurrentStep.paso + 1);
        UpdateStep();
    }

    public void StepBackward()
    {
        if (CurrentStep.paso == 1) return;
        CurrentStep = Guide.pasos.Find(x => x.paso == CurrentStep.paso - 1);
        UpdateStep();
    }

    public void HandleGuideResponse(bool IsOpen)
    {
        GuideResponse.SetActive(IsOpen);
        UIAnimation.Instance.FadeOut();
    }

    private void UpdateStep()
    {
        StepTitle.text = CurrentStep.titulo;
        StepDescription.text = CurrentStep.descripcion;
        StepCount.text = "Paso " + CurrentStep.paso + "/" + Guide.pasos.Count;
    }

    public void RulerAction()
    {
        RulerPlaceButton.SetActive(true);
        RulerManager.SetActive(true);
        BackToSpawnModeButton.SetActive(true);
        CameraPivot.SetActive(true);
        ObjectSpawner.SetActive(false);
        ToolbarButton.SetActive(false);
        //ARPlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        //ARPlaneManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
        UIAnimation.Instance.FadeOut();
    }
    public void GyroscopeAction()
    {
        Gyroscope.SetActive(true);
        ToolbarButton.SetActive(false);
        UIAnimation.Instance.FadeOut();
    }

    public void MaterialListAction()
    {
        MaterialList.SetActive(true);
        ToolbarButton.SetActive(false);
        UIAnimation.Instance.FadeOut();
    }

    public void FinishAction()
    {

        UpdateUserModelData userModelData = new()
        {
            completed = (int)CompletedProfile.Complete,
            current_step = CurrentStep.paso,
        };
        ApiController.UpdateUserModelData(userModelData, () =>
        {
            FinishModal.SetActive(true);
            ToolbarButton.SetActive(false);
            UIAnimation.Instance.FadeOut();
        });
    }

    public void CloseFinishModal()
    {
        FinishModal.SetActive(false);
        ToolbarButton.SetActive(true);
    }

}