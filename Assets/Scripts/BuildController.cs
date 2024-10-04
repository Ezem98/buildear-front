using TMPro;
using UnityEngine;

public class BuildController : MonoBehaviour
{
    [SerializeField] public GameObject GuideResponse;
    [SerializeField] public GameObject LoadingModal;
    [SerializeField] public GameObject ChatButton;
    [SerializeField] public TextMeshProUGUI StepTitle;
    [SerializeField] public TextMeshProUGUI StepDescription;
    [SerializeField] public TextMeshProUGUI StepCount;

    public Guide Guide { get; set; }
    public Paso CurrentStep { get; set; }

    private static BuildController _instance;

    public void BackToUI()
    {
        UIController.Instance.SceneHandler("UI");
        // GameObject.Find("UI").SetActive(false);
        // GameObject.Find("XR Origin (AR Rig)").SetActive(false);
    }

    private void Awake()
    {
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