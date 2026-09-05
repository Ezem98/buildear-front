using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;

public class ApiController : MonoBehaviour
{
    private const string BaseUrl = "https://buildear-backend-production.up.railway.app/api/v1";
    private const int DefaultTimeoutSeconds = 30;
    private const int OpenAITimeoutSeconds = 120;
    private bool refreshInProgress;
    private bool lastRefreshSucceeded;

    public static ApiController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool TryGetRequestExecutor(out ApiController executor)
    {
        executor = Instance;
        if (executor != null)
        {
            return true;
        }

        Debug.LogError(
            "No hay un ApiController configurado en la escena BuildUI. "
            + "La solicitud no se ejecutará."
        );
        return false;
    }

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
        APIErrorResponse payload = ParseErrorResponse(jsonResponse);
        return payload?.error?.message ?? payload?.message ?? fallback;
    }

    public static string ErrorCode(string jsonResponse)
    {
        return ParseErrorResponse(jsonResponse)?.error?.code;
    }

    private static APIErrorResponse ParseErrorResponse(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse)) return null;
        try
        {
            return JsonConvert.DeserializeObject<APIErrorResponse>(jsonResponse);
        }
        catch (JsonException)
        {
            return null;
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

    private static bool IsSessionError(string payload)
    {
        string code = ErrorCode(payload);
        return code == "AUTH_REQUIRED" || code == "INVALID_SESSION";
    }

    private static string RequestErrorPayload(UnityWebRequest webRequest)
    {
        string responseBody = webRequest.downloadHandler?.text;
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            Debug.LogError(
                $"{webRequest.method} {webRequest.url} falló "
                + $"({webRequest.responseCode}, {webRequest.result})."
            );
            return responseBody;
        }

        string message = webRequest.responseCode == 401
            ? "Tu sesión venció. Iniciá sesión nuevamente."
            : webRequest.result == UnityWebRequest.Result.ConnectionError
                ? "No se pudo conectar con el servidor. Revisá tu conexión e intentá nuevamente."
                : "El servidor no pudo completar la solicitud. Intentá nuevamente.";

        Debug.LogError(
            $"{webRequest.method} {webRequest.url} falló "
            + $"({webRequest.responseCode}, {webRequest.result}): {webRequest.error}"
        );
        return JsonConvert.SerializeObject(new
        {
            error = new
            {
                message,
                details = new
                {
                    response_code = webRequest.responseCode,
                    transport_error = webRequest.error,
                },
            },
        });
    }

    private static UnityWebRequest CreateRequest(string method, string url, string jsonData)
    {
        UnityWebRequest webRequest = new(url, method);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        if (jsonData != null)
        {
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
            webRequest.SetRequestHeader("Content-Type", "application/json");
        }
        webRequest.timeout = url.Contains("/openai")
            ? OpenAITimeoutSeconds
            : DefaultTimeoutSeconds;
        return webRequest;
    }

    private static void LogRequest(string method, string url)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"API request: {method} {url}");
#endif
    }

    private bool ShouldRefresh(string method, string url)
    {
        if (url.EndsWith("/auth/login") || url.EndsWith("/auth/refresh"))
            return false;
        if (method == UnityWebRequest.kHttpVerbPOST && url.EndsWith("/users"))
            return false;
        return UIController.Instance != null && UIController.Instance.LoggedIn;
    }

    private IEnumerator RefreshAccessToken(System.Action<bool> onComplete, bool force)
    {
        UIController ui = UIController.Instance;
        if (ui == null || !ui.LoggedIn)
        {
            onComplete?.Invoke(false);
            yield break;
        }
        if (!force && ui.HasValidSession())
        {
            onComplete?.Invoke(true);
            yield break;
        }
        if (!ui.HasRefreshSession())
        {
            ui.ExpireSession();
            onComplete?.Invoke(false);
            yield break;
        }

        if (refreshInProgress)
        {
            while (refreshInProgress) yield return null;
            onComplete?.Invoke(lastRefreshSucceeded && ui.HasValidSession());
            yield break;
        }

        refreshInProgress = true;
        lastRefreshSucceeded = false;
        string jsonData = JsonConvert.SerializeObject(new RefreshData
        {
            refresh_token = ui.RefreshToken
        });

        using (UnityWebRequest webRequest = CreateRequest(
            UnityWebRequest.kHttpVerbPOST,
            BaseUrl + "/auth/refresh",
            jsonData
        ))
        {
            LogRequest(UnityWebRequest.kHttpVerbPOST, BaseUrl + "/auth/refresh");
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                APIResponse<AuthData> response = DeserializeResponse<AuthData>(
                    webRequest.downloadHandler.text
                );
                if (response?.data?.user != null
                    && !string.IsNullOrWhiteSpace(response.data.access_token)
                    && !string.IsNullOrWhiteSpace(response.data.refresh_token))
                {
                    ApplyAuthenticatedSession(response.data);
                    lastRefreshSucceeded = true;
                }
            }

            if (!lastRefreshSucceeded)
            {
                string payload = RequestErrorPayload(webRequest);
                ui.ExpireSession(ErrorMessage(
                    payload,
                    "Tu sesión venció. Iniciá sesión nuevamente."
                ));
            }
        }

        refreshInProgress = false;
        onComplete?.Invoke(lastRefreshSucceeded);
    }

    private IEnumerator SendRequest(
        string method,
        string url,
        string jsonData,
        System.Action<string> onSuccess,
        System.Action<string> onError,
        bool retryAfterRefresh = true,
        bool showFeedback = true
    )
    {
        bool refreshAllowed = ShouldRefresh(method, url);
        if (refreshAllowed && !UIController.Instance.HasValidSession())
        {
            bool refreshed = false;
            yield return RefreshAccessToken(result => refreshed = result, false);
            if (!refreshed)
            {
                onError?.Invoke(JsonConvert.SerializeObject(new
                {
                    error = new
                    {
                        code = "INVALID_REFRESH_TOKEN",
                        message = "Tu sesión venció. Iniciá sesión nuevamente."
                    }
                }));
                yield break;
            }
        }

        using (UnityWebRequest webRequest = CreateRequest(method, url, jsonData))
        {
            ApplyAuthorization(webRequest);
            LogRequest(method, url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(webRequest.downloadHandler.text);
                yield break;
            }

            string payload = RequestErrorPayload(webRequest);
            if (
                webRequest.responseCode == 401
                && refreshAllowed
                && retryAfterRefresh
                && IsSessionError(payload)
                && UIController.Instance.HasRefreshSession()
            )
            {
                bool refreshed = false;
                yield return RefreshAccessToken(result => refreshed = result, true);
                if (refreshed)
                {
                    yield return SendRequest(
                        method,
                        url,
                        jsonData,
                        onSuccess,
                        onError,
                        false,
                        showFeedback
                    );
                    yield break;
                }
            }
            else if (
                webRequest.responseCode == 401
                && refreshAllowed
                && IsSessionError(payload)
            )
            {
                UIController.Instance.ExpireSession();
            }

            if (showFeedback)
            {
                UIController.Instance?.ShowUserMessage(
                    ErrorMessage(payload, "No se pudo completar la solicitud."),
                    true
                );
            }
            onError?.Invoke(payload);
        }
    }

    // Método para realizar el GET
    IEnumerator GetRequest(
        string url,
        System.Action<string> onSuccess,
        System.Action<string> onError,
        bool showFeedback = true
    )
    {
        yield return SendRequest(
            UnityWebRequest.kHttpVerbGET,
            url,
            null,
            onSuccess,
            onError,
            showFeedback: showFeedback
        );
    }

    IEnumerator DeleteRequest(string url, System.Action onSuccess, System.Action<string> onError)
    {
        yield return SendRequest(
            UnityWebRequest.kHttpVerbDELETE,
            url,
            null,
            _ => onSuccess?.Invoke(),
            onError
        );
    }

    // Método para realizar el POST
    IEnumerator PostRequest(string url, string jsonData, System.Action<string> onSuccess, System.Action<string> onError)
    {
        yield return SendRequest(UnityWebRequest.kHttpVerbPOST, url, jsonData, onSuccess, onError);
    }

    // Método para realizar el PUT
    IEnumerator PatchRequest(string url, string jsonData, System.Action<string> onSuccess, System.Action<string> onError)
    {
        yield return SendRequest("PATCH", url, jsonData, onSuccess, onError);
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
            webRequest.timeout = DefaultTimeoutSeconds;
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

    private static APIResponse<T> DeserializeResponse<T>(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse)) return null;
        try
        {
            return JsonConvert.DeserializeObject<APIResponse<T>>(jsonResponse);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ApplyAuthenticatedSession(AuthData authData)
    {
        UIController ui = UIController.Instance;
        ui.UserData = authData.user;
        ui.AccessToken = authData.access_token;
        ui.AccessTokenExpiresAt = authData.expires_at;
        ui.RefreshToken = authData.refresh_token;
        ui.RefreshTokenExpiresAt = authData.refresh_expires_at;
        ui.LoggedIn = true;
        ui.GuestUser = false;
        ui.MyModelsData = null;
        ui.SaveData();
    }

    private static string InvalidResponse(string operation)
    {
        string message = $"El servidor devolvió una respuesta inválida al {operation}.";
        UIController.Instance?.ShowUserMessage(message, true);
        return message;
    }

    // Método que llamas para iniciar la solicitud
    public void GetAllUsers()
    {
        StartCoroutine(GetRequest(BaseUrl + "/users", onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData[]> apiResponse = DeserializeResponse<UserData[]>(jsonResponse);
        }, onError: (jsonResponse) =>
        {
            UIController.Instance?.ShowUserMessage(
                ErrorMessage(jsonResponse, "No se pudieron obtener los usuarios."),
                true
            );
        }));
    }

    public void GetUserByUsername(string username)
    {
        StartCoroutine(GetRequest(BaseUrl + "/users/" + UnityWebRequest.EscapeURL(username), onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = DeserializeResponse<UserData>(jsonResponse);
        }, onError: (jsonResponse) =>
        {
            UIController.Instance?.ShowUserMessage(
                ErrorMessage(jsonResponse, "No se pudo obtener el usuario."),
                true
            );
        }));
    }

    // Método que llamas para iniciar la solicitud
    public void Register(RegisterData registerData, System.Action onSuccess, System.Action<string> onError)
    {

        // Convertir el objeto a un string JSON
        string jsonData = JsonConvert.SerializeObject(registerData);

        StartCoroutine(PostRequest(BaseUrl + "/users", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = DeserializeResponse<UserData>(jsonResponse);
            if (apiResponse?.data == null)
            {
                onError?.Invoke(InvalidResponse("registrar el usuario"));
                return;
            }
            UIController.Instance.ScreenHandler("Login");
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo completar el registro."));
        }));
    }

    public void Login(LoginData loginData, System.Action onSuccess, System.Action<string> onError)
    {
        // Convertir el objeto a un string JSON
        string jsonData = JsonConvert.SerializeObject(loginData);

        StartCoroutine(PostRequest(BaseUrl + "/auth/login", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<AuthData> apiResponse = DeserializeResponse<AuthData>(jsonResponse);
            if (
                apiResponse?.data?.user == null
                || string.IsNullOrWhiteSpace(apiResponse.data.access_token)
                || string.IsNullOrWhiteSpace(apiResponse.data.refresh_token)
            )
            {
                onError?.Invoke(InvalidResponse("iniciar sesión"));
                return;
            }
            ApplyAuthenticatedSession(apiResponse.data);
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
        StartCoroutine(PostRequest(BaseUrl + "/auth/logout", "{}", onSuccess: (_) =>
        {
            onComplete?.Invoke();
        }, onError: (_) =>
        {
            // La sesión local debe cerrarse incluso si la red no está disponible.
            onComplete?.Invoke();
        }));
    }

    public void UpdateUserData(
        UpdateUserData updateUserData,
        System.Action onSuccess,
        System.Action<string> onError
    )
    {
        string jsonData = JsonConvert.SerializeObject(
            updateUserData,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
        );
        string username = UnityWebRequest.EscapeURL(UIController.Instance.UserData.username);

        StartCoroutine(PatchRequest(BaseUrl + "/users/" + username, jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = DeserializeResponse<UserData>(jsonResponse);
            if (apiResponse?.data == null)
            {
                onError?.Invoke(InvalidResponse("guardar el perfil"));
                return;
            }
            UIController.Instance.UserData = apiResponse.data;
            UIController.Instance.SaveData();
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo guardar el perfil."));
        }));
    }

    public void ChangePassword(
        UpdatePasswordData passwordData,
        System.Action onSuccess,
        System.Action<string> onError
    )
    {
        string jsonData = JsonConvert.SerializeObject(passwordData);
        StartCoroutine(PostRequest(BaseUrl + "/users/me/password", jsonData, _ =>
        {
            UIController.Instance.ClearSession();
            UIController.Instance.ScreenHandler("Login");
            UIController.Instance.ShowUserMessage(
                "Contraseña actualizada. Iniciá sesión nuevamente.",
                false
            );
            onSuccess?.Invoke();
        }, jsonResponse =>
        {
            onError?.Invoke(ErrorMessage(
                jsonResponse,
                "No se pudo cambiar la contraseña."
            ));
        }));
    }

    public void GenerateBuildTutorial()
    {
        if (this != Instance)
        {
            if (!TryGetRequestExecutor(out ApiController executor))
            {
                return;
            }

            executor.GenerateBuildTutorial();
            return;
        }

        if (UIController.Instance.GuestUser || !UIController.Instance.HasAuthenticatedSession())
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
        }, onError: (_) =>
        {
            HandleSavedGuideLookupFailure(modelId, model);
        });
    }

    private void HandleSavedGuideLookupFailure(int modelId, ModelData model)
    {
        if (!UIController.Instance.HasAuthenticatedSession())
        {
            BuildController.Instance.LoadingModal.SetActive(false);
            BuildController.Instance.ShowTemporaryMessage(
                "Tu sesión venció. Iniciá sesión nuevamente para generar la guía."
            );
            return;
        }

        // No tener todavía un registro en user_models no debe bloquear la guía.
        // El endpoint de generación crea o actualiza ese registro de forma segura.
        RequestGeneratedGuide(modelId, model);
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

        string jsonData = JsonConvert.SerializeObject(tutorialData);
        StartCoroutine(PostRequest(BaseUrl + "/openai", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<Guide> apiResponse = DeserializeResponse<Guide>(jsonResponse);
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
            HandleGuideGenerationFailure(modelId, jsonResponse);
        }));
    }

    private void HandleGuideGenerationFailure(int modelId, string errorResponse)
    {
        Debug.LogError("Falló la generación de la guía.");
        if (!UIController.Instance.HasAuthenticatedSession())
        {
            BuildController.Instance.LoadingModal.SetActive(false);
            BuildController.Instance.ShowTemporaryMessage(
                "Tu sesión venció. Iniciá sesión nuevamente para generar la guía."
            );
            return;
        }

        int userId = UIController.Instance.UserData.id;
        GetUserModel(userId.ToString(), modelId.ToString(), onSuccess: (userModelData) =>
        {
            bool guideWasSaved =
                userModelData?.guideObject?.pasos != null
                && userModelData.guideObject.pasos.Count > 0;
            if (guideWasSaved)
            {
                UIController.Instance.UserModelData = userModelData;
                int savedStep = userModelData.current_step > 0 ? userModelData.current_step : 1;
                ShowGuide(modelId, userModelData.guideObject, savedStep);
                return;
            }

            ShowGuideGenerationError(errorResponse);
        }, onError: (_) => ShowGuideGenerationError(errorResponse));
    }

    private void ShowGuideGenerationError(string errorResponse)
    {
        BuildController.Instance.LoadingModal.SetActive(false);
        BuildController.Instance.ShowTemporaryMessage(
            ErrorMessage(errorResponse, "No se pudo generar la guía. Intentá nuevamente.")
        );
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
        StartCoroutine(GetRequest(BaseUrl + "/models/category/" + categoryId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = DeserializeResponse<List<ModelData>>(jsonResponse);
            UIController.Instance.ModelsData = apiResponse?.data;
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            UIController.Instance?.ShowUserMessage(
                ErrorMessage(jsonResponse, "No se pudieron cargar los modelos."),
                true
            );
        }));
    }

    public void GetModelImage(string url, System.Action<Sprite> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(DownloadImage(url, onSuccess: (webRequest) =>
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            onSuccess?.Invoke(sprite);
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(jsonResponse);
        }));
    }

    public void GetModelsUnderBuild(string modelId, System.Action<List<UserModelData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(BaseUrl + "/userModels/model/" + UnityWebRequest.EscapeURL(modelId), onSuccess: (jsonResponse) =>
        {
            APIResponse<List<UserModelData>> apiResponse = DeserializeResponse<List<UserModelData>>(jsonResponse);
            onSuccess?.Invoke(apiResponse?.data);
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudieron cargar las construcciones."));
        }));
    }

    public void GetUserModel(string userId, string modelId, System.Action<UserModelData> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(BaseUrl + "/userModels/" + UnityWebRequest.EscapeURL(userId) + "/" + UnityWebRequest.EscapeURL(modelId), onSuccess: (jsonResponse) =>
        {
            APIResponse<UserModelData> apiResponse = DeserializeResponse<UserModelData>(jsonResponse);
            if (apiResponse?.data != null)
            {
                try
                {
                    apiResponse.data.guideObject = string.IsNullOrWhiteSpace(apiResponse.data.guide)
                        ? null
                        : JsonConvert.DeserializeObject<Guide>(apiResponse.data.guide);
                }
                catch (JsonException)
                {
                    // Una guía incompleta o de una versión anterior se regenera.
                    apiResponse.data.guideObject = null;
                }
            }
            onSuccess?.Invoke(apiResponse?.data);
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo consultar la guía guardada."));
        }, showFeedback: false));
    }

    public void CreateUserModel(UserModelData userModelData, System.Action<UserModelData> onSuccess, System.Action<string> onError)
    {
        string jsonData = JsonConvert.SerializeObject(userModelData);

        StartCoroutine(PostRequest(BaseUrl + "/userModels", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserModelData> apiResponse = DeserializeResponse<UserModelData>(jsonResponse);
            if (apiResponse?.data == null)
            {
                onError?.Invoke(InvalidResponse("guardar la construcción"));
                return;
            }
            UIController.Instance.UserModelData = apiResponse.data;
            onSuccess?.Invoke(apiResponse.data);
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo guardar la construcción."));
        }));
    }

    public void UpdateUserModelData(UpdateUserModelData updateUserModelData, System.Action onSuccess)
    {

        // Convertir el objeto a un string JSON
        string jsonData = JsonConvert.SerializeObject(updateUserModelData);

        StartCoroutine(PatchRequest(BaseUrl + "/userModels/" + UIController.Instance.UserModelData.id, jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<UserModelData> apiResponse = DeserializeResponse<UserModelData>(jsonResponse);
            UIController.Instance.UserModelData = apiResponse?.data;
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            UIController.Instance?.ShowUserMessage(
                ErrorMessage(jsonResponse, "No se pudo actualizar el progreso."),
                true
            );
        }));
    }

    public void SearchModels(string search)
    {
        StartCoroutine(GetRequest(BaseUrl + "/models/search/" + UnityWebRequest.EscapeURL(search), onSuccess: (jsonResponse) =>
        {
            UIController.Instance.SearchModelsData?.Clear();
            APIResponse<List<ModelData>> apiResponse = DeserializeResponse<List<ModelData>>(jsonResponse);
            UIController.Instance.SearchModelsData = apiResponse?.data;
            UIController.Instance.ComesFromSearch = true;
            UIController.Instance.ScreenHandler("Models");
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            UIController.Instance?.ShowUserMessage(
                ErrorMessage(jsonResponse, "No se pudo completar la búsqueda."),
                true
            );
        }));
    }

    public void GetModelsByUserId(int userId, System.Action<List<ModelData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(BaseUrl + "/models/user/" + userId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = DeserializeResponse<List<ModelData>>(jsonResponse);
            UIController.Instance.MyModelsData = apiResponse?.data;
            onSuccess?.Invoke(apiResponse?.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudieron cargar tus modelos."));
        }));
    }

    public void GetFavoritesModels(int userId, System.Action<List<ModelData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(BaseUrl + "/models/favorite/" + userId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ModelData>> apiResponse = DeserializeResponse<List<ModelData>>(jsonResponse);
            UIController.Instance.FavoritesModelsData = apiResponse?.data;
            onSuccess?.Invoke(apiResponse?.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudieron cargar tus favoritos."));
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

        StartCoroutine(PostRequest(BaseUrl + "/favorites", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<FavoriteData> apiResponse = DeserializeResponse<FavoriteData>(jsonResponse);
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
        StartCoroutine(DeleteRequest(BaseUrl + "/favorites/" + favoriteData.user_id + "/" + favoriteData.model_id, onSuccess: () =>
        {
            onSuccess?.Invoke();
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo eliminar el favorito."));
        }));
    }

    public void IsFavorite(FavoriteData favoriteData, System.Action<bool> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(BaseUrl + "/favorites/" + favoriteData.user_id + "/" + favoriteData.model_id, onSuccess: (jsonResponse) =>
        {
            if (TryParseBooleanResponse(jsonResponse, out bool isFavorite))
            {
                onSuccess?.Invoke(isFavorite);
                return;
            }

            onError?.Invoke("Respuesta invalida al consultar favoritos.");
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo consultar el favorito."));
        }));
    }

    public void SendMessageToAI(ChatMessageData chatMessageData, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string jsonData = JsonConvert.SerializeObject(
            chatMessageData,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
        );
        StartCoroutine(PostRequest(BaseUrl + "/openai/message", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<string> apiResponse = DeserializeResponse<string>(jsonResponse);
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
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo obtener una respuesta."));
        }));
    }

    public void SaveConversation(ConversationPostData conversationPostData, System.Action<ConversationData> onSuccess, System.Action<string> onError)
    {
        string conversationData = JsonConvert.SerializeObject(conversationPostData);
        StartCoroutine(PostRequest(BaseUrl + "/conversation", conversationData, onSuccess: (jsonResponse) =>
        {
            APIResponse<ConversationData> apiResponse = DeserializeResponse<ConversationData>(jsonResponse);
            if (apiResponse?.data == null)
            {
                onError?.Invoke(InvalidResponse("guardar la conversación"));
                return;
            }
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudo guardar la conversación."));
        }));
    }

    public void SaveMessages(ConversationMessagePostData conversationMessagesPostData, System.Action<List<ConversationMessageData>> onSuccess, System.Action<string> onError)
    {
        string conversationMessagesData = JsonConvert.SerializeObject(conversationMessagesPostData);
        StartCoroutine(PostRequest(BaseUrl + "/conversationMessage/all", conversationMessagesData, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ConversationMessageData>> apiResponse = DeserializeResponse<List<ConversationMessageData>>(jsonResponse);
            if (apiResponse?.data == null)
            {
                onError?.Invoke(InvalidResponse("guardar los mensajes"));
                return;
            }
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudieron guardar los mensajes."));
        }));
    }

    public void GetConversationMessages(int conversationId, System.Action<List<ConversationMessageData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(BaseUrl + "/conversationMessage/conversation/" + conversationId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ConversationMessageData>> apiResponse = DeserializeResponse<List<ConversationMessageData>>(jsonResponse);
            if (apiResponse?.data == null)
            {
                onError?.Invoke(InvalidResponse("cargar los mensajes"));
                return;
            }
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudieron cargar los mensajes."));
        }));
    }

    public void GetUserConversations(int userId, System.Action<List<ConversationData>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(BaseUrl + "/conversation/user/" + userId, onSuccess: (jsonResponse) =>
        {
            APIResponse<List<ConversationData>> apiResponse = DeserializeResponse<List<ConversationData>>(jsonResponse);
            if (apiResponse?.data == null)
            {
                onError?.Invoke(InvalidResponse("cargar las conversaciones"));
                return;
            }
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            onError?.Invoke(ErrorMessage(jsonResponse, "No se pudieron cargar las conversaciones."));
        }));
    }
}

