using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

public class ApiController : MonoBehaviour
{
    // URL de tu API
    private readonly string baseUrl = "http://ec2-44-219-46-170.compute-1.amazonaws.com:1234";
    // private readonly string baseUrl = "http://localhost:1234";

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
            UIController.Instance.ScreenHandler("Home");
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void GenerateBuildTutorial()
    {
        // Crear un objeto con los datos del tutorial
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

        BuildController.Instance.LoadingModal.SetActive(true);
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
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
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

    public void GetModelsUnderBuild(string modelId, System.Action<List<ModelData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/userModels/model/" + modelId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ModelData>>>(jsonResponse);
            onSuccess?.Invoke(apiResponse?.data);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
        }));
    }
}

