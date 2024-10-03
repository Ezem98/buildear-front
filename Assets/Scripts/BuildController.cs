using TMPro;
using UnityEngine;

public class BuildController : MonoBehaviour
{
    [SerializeField] private GameObject guideResponse;
    [SerializeField] private GameObject loadingModal;
    [SerializeField] private GameObject chatButton;
    [SerializeField] private TextMeshProUGUI stepTitle;
    [SerializeField] private TextMeshProUGUI stepDescription;
    [SerializeField] private TextMeshProUGUI stepCount;

    public Guide Guide { get; set; }
    public Paso CurrentStep { get; set; }
    public GameObject GuideResponse { get; set; }
    public GameObject LoadingModal { get; set; }
    public GameObject ChatButton { get; set; }
    public TextMeshProUGUI StepTitle { get; set; }
    public TextMeshProUGUI StepDescription { get; set; }
    public TextMeshProUGUI StepCount { get; set; }

    private static BuildController _instance;

    public void BackToUI()
    {
        UIController.Instance.SceneHandler("UI");
    }

    void Awake()
    {
        //TouchSimulation.Enable();
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
    }

    private void UpdateStep()
    {
        StepTitle.text = CurrentStep.titulo;
        StepDescription.text = CurrentStep.descripcion;
        StepCount.text = "Paso " + CurrentStep.paso + "/" + Guide.pasos.Count;
    }


}