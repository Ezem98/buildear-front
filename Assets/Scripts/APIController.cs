using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class ApiController : MonoBehaviour
{
    // URL de tu API
    // private readonly string baseUrl = "http://ec2-44-219-46-170.compute-1.amazonaws.com:1234";

    [SerializeField] private string baseUrl = "https://buildear-backend-production.up.railway.app/api/v1";

    private void ApplyAuthorization(UnityWebRequest webRequest)
    {
        string accessToken = UIController.Instance?.AccessToken;
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + accessToken);
        }
    }

    public static string ErrorMessage(string jsonResponse, string fallback)
    {
        try
        {
            JObject payload = JObject.Parse(jsonResponse);
            return payload["error"]?["message"]?.ToString()
                ?? payload["message"]?.ToString()
                ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    public static bool TryParseBooleanResponse(string jsonResponse, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(jsonResponse)) return false;

        try
        {
            JToken payload = JToken.Parse(jsonResponse);
            JToken booleanToken = payload.Type == JTokenType.Boolean
                ? payload
                : payload.Type == JTokenType.Object
                    ? payload["data"]
                    : null;

            if (booleanToken?.Type != JTokenType.Boolean) return false;

            value = booleanToken.Value<bool>();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void HandleExpiredSession(UnityWebRequest webRequest)
    {
        if (webRequest.responseCode == 401 && !webRequest.url.EndsWith("/auth/login"))
        {
            UIController.Instance.ClearSession();
        }
    }

    // Método para realizar el GET
    IEnumerator GetRequest(string url, System.Action<string> onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            ApplyAuthorization(webRequest);
            // Enviar la solicitud y esperar respuesta
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // Invocar el callback de éxito con la respuesta
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                HandleExpiredSession(webRequest);
                // Invocar el callback de error con el mensaje de error
                onError?.Invoke(webRequest.downloadHandler.text);
            }
        }
    }

    IEnumerator DeleteRequest(string url, System.Action onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Delete(url))
        {
            ApplyAuthorization(webRequest);
            // Enviar la solicitud y esperar respuesta
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                HandleExpiredSession(webRequest);
                // Invocar el callback de error con el mensaje de error
                onError?.Invoke(webRequest.downloadHandler.text);
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
        ApplyAuthorization(webRequest);

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
            HandleExpiredSession(webRequest);
            // Invocar el callback de error con el mensaje de error
            onError?.Invoke(webRequest.downloadHandler.text);
        }
    }

    // Método para realizar el PUT
    IEnumerator PatchRequest(string url, string jsonData, System.Action<string> onSuccess, System.Action<string> onError)
    {
        // Crear la solicitud PUT
        UnityWebRequest webRequest = new UnityWebRequest(url, "PATCH");

        // Convertir los datos a un formato de JSON o similar
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Asignar los datos a la solicitud
        webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
        webRequest.downloadHandler = new DownloadHandlerBuffer();

        // Definir el tipo de contenido (importante para APIs que reciben JSON)
        webRequest.SetRequestHeader("Content-Type", "application/json");
        ApplyAuthorization(webRequest);

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
            HandleExpiredSession(webRequest);
            // Invocar el callback de error con el mensaje de error
            onError?.Invoke(webRequest.downloadHandler.text);
        }
    }


    IEnumerator DownloadImage(string url, System.Action<UnityWebRequest> onSuccess, System.Action<string> onError)
    {
        if (!TryNormalizeHttpUrl(url, out string normalizedUrl))
        {
            onError?.Invoke("No se pudo cargar la imagen: la URL está vacía o no usa HTTP/HTTPS.");
            yield break;
        }

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(normalizedUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(webRequest);
            }
            else
            {
                onError?.Invoke(webRequest.error);
            }
        }
    }

    public static bool TryNormalizeHttpUrl(string url, out string normalizedUrl)
    {
        normalizedUrl = null;
        if (string.IsNullOrWhiteSpace(url)) return false;

        string candidate = url.Trim();
        if (candidate.StartsWith("//")) candidate = "https:" + candidate;

        if (!System.Uri.TryCreate(candidate, System.UriKind.Absolute, out System.Uri parsedUrl))
            return false;

        if (parsedUrl.Scheme != System.Uri.UriSchemeHttp && parsedUrl.Scheme != System.Uri.UriSchemeHttps)
            return false;

        normalizedUrl = parsedUrl.AbsoluteUri;
        return true;
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
    public void Register(RegisterData registerData, System.Action onSuccess, System.Action<string> onError)
    {

        // Convertir el objeto a un string JSON
        string jsonData = JsonUtility.ToJson(registerData);

        StartCoroutine(PostRequest(baseUrl + "/users", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = JsonUtility.FromJson<APIResponse<UserData>>(jsonResponse);
            UIController.Instance.ScreenHandler("Login");
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = JsonUtility.FromJson<APIResponse<UserData>>(jsonResponse);
            onError.Invoke(apiResponse.message);
        }));
    }

    public void Login(LoginData loginData, System.Action onSuccess, System.Action<string> onError)
    {
        // Convertir el objeto a un string JSON
        string jsonData = JsonUtility.ToJson(loginData);

        StartCoroutine(PostRequest(baseUrl + "/auth/login", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<AuthData> apiResponse = JsonConvert.DeserializeObject<APIResponse<AuthData>>(jsonResponse);
            if (apiResponse?.data?.user == null || string.IsNullOrWhiteSpace(apiResponse.data.access_token))
            {
                Debug.Log("Usuario o contraseña incorrectos");
                onError?.Invoke("No se pudo iniciar sesión.");
                return;
            }
            UIController.Instance.UserData = apiResponse.data.user;
            UIController.Instance.AccessToken = apiResponse.data.access_token;
            UIController.Instance.AccessTokenExpiresAt = apiResponse.data.expires_at;
            UIController.Instance.LoggedIn = true;
            UIController.Instance.GuestUser = false;
            UIController.Instance.MyModelsData = null;
            if (UIController.Instance.UserData.completed_profile == (int)CompletedProfile.Incomplete)
            {
                UIController.Instance.ScreenHandler("Profile");
                onSuccess?.Invoke();
            }
            else
            {
                UIController.Instance.ScreenHandler("Home");
                onSuccess?.Invoke();
            }
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo iniciar sesión."));
        }));
    }

    public void Logout(System.Action onComplete)
    {
        StartCoroutine(PostRequest(baseUrl + "/auth/logout", "{}", onSuccess: (_) =>
        {
            onComplete?.Invoke();
        }, onError: (_) =>
        {
            // La sesión local debe cerrarse incluso si la red no está disponible.
            onComplete?.Invoke();
        }));
    }

    public void UpdateUserData(UpdateUserData updateUserData, System.Action onSuccess)
    {

        // Convertir el objeto a un string JSON
        string jsonData = JsonUtility.ToJson(updateUserData);

        Debug.Log(jsonData);

        StartCoroutine(PatchRequest(baseUrl + "/users/" + UIController.Instance.UserData.username, jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = JsonUtility.FromJson<APIResponse<UserData>>(jsonResponse);
            UIController.Instance.UserData = apiResponse?.data;
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void GenerateBuildTutorial()
    {
        if (UIController.Instance.GuestUser || !UIController.Instance.HasValidSession())
        {
            BuildController.Instance.ShowTemporaryMessage("Iniciá sesión para generar y guardar una guía.");
            return;
        }

        int modelId = UIController.Instance.CurrentModelIndex;
        int userId = UIController.Instance.UserData.id;
        ModelData model = UIController.Instance.ModelData;
        if (model == null)
        {
            BuildController.Instance.ShowTemporaryMessage("No se pudo identificar el modelo seleccionado.");
            return;
        }

        BuildController.Instance.ShowLoading("Preparando tu guía...");
        GetUserModel(userId.ToString(), modelId.ToString(), onSuccess: (userModelData) =>
        {
            bool hasSavedGuide =
                userModelData?.guideObject?.pasos != null
                && userModelData.guideObject.pasos.Count > 0;
            if (hasSavedGuide)
            {
                UIController.Instance.UserModelData = userModelData;
                int savedStep = userModelData.current_step > 0 ? userModelData.current_step : 1;
                ShowGuide(modelId, userModelData.guideObject, savedStep);
                return;
            }

            RequestGeneratedGuide(modelId, model);
        }, onError: (error) =>
        {
            Debug.Log(error);
            BuildController.Instance.LoadingModal.SetActive(false);
            BuildController.Instance.ShowTemporaryMessage(ErrorMessage(error, "No se pudo consultar la guía guardada."));
        });
    }

    private void RequestGeneratedGuide(int modelId, ModelData model)
    {
        int experienceLevel = UIController.Instance.UserData?.experience_level ?? 0;
        TutorialData tutorialData = new()
        {
            model_id = modelId,
            modelCategory = (Categories)model.category_id,
            modelName = model.name,
            modelSize = new()
            {
                height = model.height,
                width = model.width,
            },
            experienceLevel = experienceLevel != 0 ? experienceLevel : (int)ExperienceLevel.Intermediate,
        };

        string jsonData = JsonUtility.ToJson(tutorialData);
        StartCoroutine(PostRequest(baseUrl + "/openai", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<Guide> apiResponse = JsonConvert.DeserializeObject<APIResponse<Guide>>(jsonResponse);
            if (apiResponse?.data?.pasos == null || apiResponse.data.pasos.Count == 0)
            {
                BuildController.Instance.LoadingModal.SetActive(false);
                BuildController.Instance.ShowTemporaryMessage("La guía recibida no contiene pasos válidos.");
                return;
            }

            int savedStep = 1;
            if (apiResponse.user_model != null)
            {
                apiResponse.user_model.guideObject = apiResponse.data;
                UIController.Instance.UserModelData = apiResponse.user_model;
                savedStep = apiResponse.user_model.current_step > 0
                    ? apiResponse.user_model.current_step
                    : 1;
            }
            ShowGuide(modelId, apiResponse.data, savedStep);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            BuildController.Instance.LoadingModal.SetActive(false);
            BuildController.Instance.ShowTemporaryMessage(ErrorMessage(jsonResponse, "No se pudo generar la guía. Intentá nuevamente."));
        }));
    }

    private void ShowGuide(int modelId, Guide guide, int requestedStep)
    {
        Paso currentStep = guide.pasos.Find(step => step.paso == requestedStep) ?? guide.pasos[0];
        BuildController.Instance.GuidesDictionary[modelId] = guide;
        BuildController.Instance.CurrentStepDictionary[modelId] = currentStep;
        BuildController.Instance.LoadingModal.SetActive(false);
        BuildController.Instance.GuideResponse.SetActive(true);
        BuildController.Instance.StepTitle.text = currentStep.titulo;
        BuildController.Instance.StepDescription.text = currentStep.descripcion;
        BuildController.Instance.StepCount.text = "Paso " + currentStep.paso + "/" + guide.pasos.Count;
        BuildController.Instance.MaterialListButton.interactable = true;
        BuildController.Instance.GuideButton.interactable = true;
        BuildController.Instance.FinishButton.interactable = true;
        BuildController.Instance.ChatButton.interactable = true;
        BuildController.Instance.CalculateAmount();
        BuildController.Instance.CalculateTime();
    }

    public void GetModelsByCategoryId(int categoryId, System.Action onSuccess)
    {
        StartCoroutine(GetRequest(baseUrl + "/models/category/" + categoryId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ModelData>>>(jsonResponse);
            UIController.Instance.ModelsData = apiResponse?.data;
            onSuccess?.Invoke();
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

    public void GetUserModel(string userId, string modelId, System.Action<UserModelData> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/userModels/" + userId + "/" + modelId, onSuccess: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            APIResponse<UserModelData> apiResponse = JsonConvert.DeserializeObject<APIResponse<UserModelData>>(jsonResponse);
            // Deserializar la cadena JSON dentro del campo 'guide'
            if (apiResponse?.data != null)
            {
                apiResponse.data.guideObject = JsonConvert.DeserializeObject<Guide>(apiResponse.data.guide);
            }
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

        StartCoroutine(PostRequest(baseUrl + "/userModels", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserModelData> apiResponse = JsonUtility.FromJson<APIResponse<UserModelData>>(jsonResponse);
            UIController.Instance.UserModelData = apiResponse?.data;
            onSuccess?.Invoke(apiResponse.data);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError.Invoke(jsonResponse);
        }));
    }

    public void UpdateUserModelData(UpdateUserModelData updateUserModelData, System.Action onSuccess)
    {

        // Convertir el objeto a un string JSON
        string jsonData = JsonUtility.ToJson(updateUserModelData);

        StartCoroutine(PatchRequest(baseUrl + "/userModels/" + UIController.Instance.UserModelData.id, jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserModelData> apiResponse = JsonUtility.FromJson<APIResponse<UserModelData>>(jsonResponse);
            UIController.Instance.UserModelData = apiResponse?.data;
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void SearchModels(string search)
    {
        StartCoroutine(GetRequest(baseUrl + "/models/search/" + search, onSuccess: (jsonResponse) =>
        {
            UIController.Instance.SearchModelsData?.Clear();
            APIResponse<List<ModelData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ModelData>>>(jsonResponse);
            UIController.Instance.SearchModelsData = apiResponse?.data;
            UIController.Instance.ComesFromSearch = true;
            UIController.Instance.ScreenHandler("Models");
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void GetModelsByUserId(int userId, System.Action<List<ModelData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/models/user/" + userId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ModelData>>>(jsonResponse);
            UIController.Instance.MyModelsData = apiResponse?.data;
            onSuccess?.Invoke(apiResponse?.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(jsonResponse);
            Debug.Log(jsonResponse);
        }));
    }

    public void GetFavoritesModels(int userId, System.Action<List<ModelData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/models/favorite/" + userId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ModelData>>>(jsonResponse);
            UIController.Instance.FavoritesModelsData = apiResponse?.data;
            onSuccess?.Invoke(apiResponse?.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(jsonResponse);
            Debug.Log(jsonResponse);
        }));
    }

    public void CreateFavorite(
        FavoriteData favoriteData,
        System.Action onSuccess = null,
        System.Action<string> onError = null
    )
    {
        string jsonData = JsonConvert.SerializeObject(new
        {
            user_id = favoriteData.user_id,
            model_id = favoriteData.model_id
        });

        StartCoroutine(PostRequest(baseUrl + "/favorites", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<FavoriteData> apiResponse =
                JsonConvert.DeserializeObject<APIResponse<FavoriteData>>(jsonResponse);
            if (apiResponse?.data == null)
            {
                onError?.Invoke("El servidor no confirmó el favorito.");
                return;
            }

            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo agregar el favorito."));
        }));
    }

    public void DeleteFavorite(
        FavoriteData favoriteData,
        System.Action onSuccess = null,
        System.Action<string> onError = null
    )
    {
        StartCoroutine(DeleteRequest(baseUrl + "/favorites/" + favoriteData.user_id + "/" + favoriteData.model_id, onSuccess: () =>
        {
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo eliminar el favorito."));
        }));
    }

    public void IsFavorite(FavoriteData favoriteData, System.Action<bool> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/favorites/" + favoriteData.user_id + "/" + favoriteData.model_id, onSuccess: (jsonResponse) =>
        {
            if (TryParseBooleanResponse(jsonResponse, out bool isFavorite))
            {
                onSuccess?.Invoke(isFavorite);
                return;
            }

            onError?.Invoke("Respuesta invalida al consultar favoritos.");
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
        }));
    }

    public void SignInGoogle()
    {
        StartCoroutine(GetRequest(baseUrl + "/auth/google", onSuccess: (jsonResponse) =>
        {
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void SendMessageToAI(ChatMessageData chatMessageData, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string jsonData = JsonConvert.SerializeObject(
            chatMessageData,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
        );
        StartCoroutine(PostRequest(baseUrl + "/openai/message", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<string> apiResponse = JsonConvert.DeserializeObject<APIResponse<string>>(jsonResponse);
            if (apiResponse == null || string.IsNullOrWhiteSpace(apiResponse.data))
            {
                onError?.Invoke("La respuesta del asistente está vacía.");
                return;
            }
            if (apiResponse.conversation_id > 0)
            {
                UIController.Instance.CurrentConversationId = apiResponse.conversation_id;
            }
            onSuccess?.Invoke(apiResponse.data);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo obtener una respuesta."));
        }));
    }

    public void SaveConversation(ConversationPostData conversationPostData, System.Action<ConversationData> onSuccess, System.Action<string> onError)
    {
        string conversationData = JsonUtility.ToJson(conversationPostData);
        StartCoroutine(PostRequest(baseUrl + "/conversation", conversationData, onSuccess: (jsonResponse) =>
        {
            APIResponse<ConversationData> apiResponse = JsonConvert.DeserializeObject<APIResponse<ConversationData>>(jsonResponse);
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
        }));
    }

    public void SaveMessages(ConversationMessagePostData conversationMessagesPostData, System.Action<List<ConversationMessageData>> onSuccess, System.Action<string> onError)
    {
        string conversationMessagesData = JsonUtility.ToJson(conversationMessagesPostData);
        StartCoroutine(PostRequest(baseUrl + "/conversationMessage/all", conversationMessagesData, onSuccess: (jsonResponse) =>
        {
            Debug.Log("Crear mensajes de la conver");
            APIResponse<List<ConversationMessageData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ConversationMessageData>>>(jsonResponse);
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(jsonResponse);
        }));
    }

    public void GetConversationMessages(int conversationId, System.Action<List<ConversationMessageData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/conversationMessage/conversation/" + conversationId, onSuccess: (jsonResponse) =>
        {
            Debug.Log("Devolver mensajes de la conver: " + jsonResponse);
            APIResponse<List<ConversationMessageData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ConversationMessageData>>>(jsonResponse);
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
        }));
    }

    public void GetUserConversations(int userId, System.Action<List<ConversationData>> onSuccess, System.Action<string> onError)
    {
        Debug.Log("GetUserConversations: " + userId);
        StartCoroutine(GetRequest(baseUrl + "/conversation/user/" + userId, onSuccess: (jsonResponse) =>
        {
            Debug.Log("Devolver convers: " + jsonResponse);
            APIResponse<List<ConversationData>> apiResponse = JsonConvert.DeserializeObject<APIResponse<List<ConversationData>>>(jsonResponse);
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
        }));
    }
}

