using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

public class ApiController : MonoBehaviour
{
    // URL de tu API
    // private readonly string baseUrl = "http://ec2-44-219-46-170.compute-1.amazonaws.com:1234";
    private readonly string baseUrl = "http://localhost:1234";

    // Método para realizar el GET
    IEnumerator GetRequest(string url, System.Action<string> onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Enviar la solicitud y esperar respuesta
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // Invocar el callback de éxito con la respuesta
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                // Invocar el callback de error con el mensaje de error
                onError?.Invoke(webRequest.error);
            }
        }
    }

    // Método para realizar el POST
    IEnumerator PostRequest(string url, string jsonData, System.Action<string> onSuccess, System.Action<string> onError)
    {
        // Crear la solicitud POST
        UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
        // Convertir los datos a un formato de JSON o similar
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Asignar los datos a la solicitud
        webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
        webRequest.downloadHandler = new DownloadHandlerBuffer();

        // Definir el tipo de contenido (importante para APIs que reciben JSON)
        webRequest.SetRequestHeader("Content-Type", "application/json");

        // Enviar la solicitud y esperar respuesta
        yield return webRequest.SendWebRequest();

        // Manejo de errores
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            // Invocar el callback de éxito con la respuesta
            onSuccess?.Invoke(webRequest.downloadHandler.text);
        }
        else
        {
            // Invocar el callback de error con el mensaje de error
            onError?.Invoke(webRequest.error);
        }
    }

    IEnumerator DownloadImage(string url, System.Action<UnityWebRequest> onSuccess, System.Action<string> onError)
    {
        // Crear la solicitud POST
        UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);

        // Enviar la solicitud y esperar respuesta
        yield return webRequest.SendWebRequest();

        // Manejo de errores
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            // Invocar el callback de éxito con la respuesta
            onSuccess?.Invoke(webRequest);
        }
        else
        {
            // Invocar el callback de error con el mensaje de error
            onError?.Invoke(webRequest.error);
        }
    }

    // Método que llamas para iniciar la solicitud
    public void GetAllUsers()
    {
        StartCoroutine(GetRequest(baseUrl + "/users", onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData[]> apiResponse = JsonConvert.DeserializeObject<APIResponse<UserData[]>>(jsonResponse);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void GetUserByUsername(string username)
    {
        StartCoroutine(GetRequest(baseUrl + "/users/" + username, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = JsonConvert.DeserializeObject<APIResponse<UserData>>(jsonResponse);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    // Método que llamas para iniciar la solicitud
    public void CreateUser()
    {

        // Crear un objeto con los datos del usuario
        UserData userData = new()
        {
            username = "ezem98",
            email = "ezem98@example.com",
            password = "min8chars",
            image = "https://img.freepik.com/free-photo/portrait-man-laughing_23-2148859448.jpg?size=338&ext=jpg&ga=GA1.1.2008272138.1721865600&semt=ais_user",
            experienceLevel = 1
        };

        // Convertir el objeto a un string JSON
        string jsonData = JsonUtility.ToJson(userData);

        StartCoroutine(PostRequest(baseUrl + "/users", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = JsonUtility.FromJson<APIResponse<UserData>>(jsonResponse);
            UIController.Instance.ScreenHandler("Login");
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void Login(LoginData loginData)
    {
        // Convertir el objeto a un string JSON
        string jsonData = JsonUtility.ToJson(loginData);

        StartCoroutine(PostRequest(baseUrl + "/auth/login", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = JsonUtility.FromJson<APIResponse<UserData>>(jsonResponse);
            if (apiResponse.successfully == false)
            {
                Debug.Log("Usuario o contraseña incorrectos");
                return;
            }
            UIController.Instance.UserData = apiResponse.data;
            UIController.Instance.ScreenHandler("Home");
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void GenerateBuildTutorial()
    {
        // Crear un objeto con los datos del tutorial
        int modelId = UIController.Instance.CurrentModelIndex;
        int userId = UIController.Instance.UserData.id;

        BuildController.Instance.LoadingModal.SetActive(true);

        GetUerModel(userId.ToString(), modelId.ToString(), onSuccess: (userModelData) =>
        {
            if (userModelData != null)
            {
                BuildController.Instance.Guide = userModelData.guideObject;
                BuildController.Instance.CurrentStep = userModelData.guideObject.pasos[userModelData.current_step];
                BuildController.Instance.LoadingModal.SetActive(false);
                BuildController.Instance.GuideResponse.SetActive(true);
                BuildController.Instance.ChatButton.SetActive(true);
                BuildController.Instance.StepTitle.text = userModelData.guideObject.pasos[userModelData.current_step].titulo;
                BuildController.Instance.StepDescription.text = userModelData.guideObject.pasos[userModelData.current_step].descripcion;
                BuildController.Instance.StepCount.text = "Paso " + (userModelData.current_step + 1) + "/" + userModelData.guideObject.pasos.Count;
                return;
            }

            TutorialData tutorialData = new()
            {
                modelName = "pared de ladrillo",
                modelSize = new()
                {
                    width = 200,
                    height = 100
                },
                experienceLevel = 1
            };
            // Convertir el objeto a un string JSONa
            string jsonData = JsonUtility.ToJson(tutorialData);

            StartCoroutine(PostRequest(baseUrl + "/openai", jsonData, onSuccess: (jsonResponse) =>
            {
                APIResponse<Guide> apiResponse = JsonConvert.DeserializeObject<APIResponse<Guide>>(jsonResponse);

                BuildController.Instance.Guide = apiResponse?.data;
                BuildController.Instance.CurrentStep = apiResponse?.data.pasos[0];

                BuildController.Instance.LoadingModal.SetActive(false);
                BuildController.Instance.GuideResponse.SetActive(true);
                BuildController.Instance.ChatButton.SetActive(true);
                BuildController.Instance.StepTitle.text = apiResponse.data.pasos[0].titulo;
                BuildController.Instance.StepDescription.text = apiResponse.data.pasos[0].descripcion;
                BuildController.Instance.StepCount.text = "Paso 1/" + apiResponse.data.pasos.Count;

                UserModelData userModelData = new()
                {
                    user_id = UIController.Instance.UserData.id,
                    model_id = UIController.Instance.CurrentModelIndex,
                    guideObject = BuildController.Instance.Guide,
                    completed = false,
                    current_step = 1
                };

                CreateUserModel(userModelData, onSuccess: (userModelData) =>
                {
                    Debug.Log("UserModel creado");
                }, onError: (error) =>
                {
                    Debug.Log(error);
                });

            }, onError: (jsonResponse) =>
            {
                Debug.Log(jsonResponse);
            }));
        }, onError: (error) =>
        {
            Debug.Log(error);
        });

    }

    public void GetModelsByCategoryId(int categoryId)
    {
        StartCoroutine(GetRequest(baseUrl + "/models/category/" + categoryId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ModelData>>>(jsonResponse);
            UIController.Instance.ModelsData = apiResponse?.data;
            ModelsManager.Instance.CreateButtons();
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void GetModelImage(string url, System.Action<Sprite> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(DownloadImage(url, onSuccess: (webRequest) =>
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 5f));

            onSuccess?.Invoke(sprite);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
        }));
    }

    public void GetModelsUnderBuild(string modelId, System.Action<List<UserModelData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/userModels/model/" + modelId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<UserModelData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<UserModelData>>>(jsonResponse);
            onSuccess?.Invoke(apiResponse?.data);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
        }));
    }

    public void GetUerModel(string userId, string modelId, System.Action<UserModelData> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/userModels/" + userId + "/" + modelId, onSuccess: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            APIResponse<UserModelData> apiResponse = JsonConvert.DeserializeObject<APIResponse<UserModelData>>(jsonResponse);
            // Deserializar la cadena JSON dentro del campo 'guide'
            apiResponse.data.guideObject = JsonConvert.DeserializeObject<Guide>(apiResponse.data.guide);
            onSuccess?.Invoke(apiResponse?.data);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
        }));
    }

    public void CreateUserModel(UserModelData userModelData, System.Action<UserModelData> onSuccess, System.Action<string> onError)
    {
        // Convertir el objeto a un string JSON
        // string jsonGuide = JsonUtility.ToJson(userModelData.guide);
        string jsonData = JsonUtility.ToJson(userModelData);

        Debug.Log(jsonData.ToString());

        StartCoroutine(PostRequest(baseUrl + "/userModels", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserModelData> apiResponse = JsonUtility.FromJson<APIResponse<UserModelData>>(jsonResponse);
            Debug.Log(apiResponse.data);
            onSuccess?.Invoke(apiResponse.data);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError.Invoke(jsonResponse);
        }));
    }

    public void SearchModels(string search)
    {
        StartCoroutine(GetRequest(baseUrl + "/models/search/" + search, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ModelData>>>(jsonResponse);
            UIController.Instance.ModelsData = apiResponse?.data;
            UIController.Instance.ScreenHandler("Models");
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }
}

