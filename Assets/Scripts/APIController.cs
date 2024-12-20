using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using OpenAI.Chat;

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
                onError?.Invoke(webRequest.downloadHandler.text);
            }
        }
    }

    IEnumerator DeleteRequest(string url, System.Action<string> onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Delete(url))
        {
            // Enviar la solicitud y esperar respuesta
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // Invocar el callback de éxito con la respuesta
                onSuccess?.Invoke("Delete request successful");
            }
            else
            {
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
            onError?.Invoke(webRequest.downloadHandler.text);
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
            APIResponse<UserData> apiResponse = JsonUtility.FromJson<APIResponse<UserData>>(jsonResponse);
            if (apiResponse.data == null)
            {
                Debug.Log("Usuario o contraseña incorrectos");
                return;
            }
            UIController.Instance.UserData = apiResponse.data;
            UIController.Instance.LoggedIn = true;
            if (UIController.Instance.UserData.completed_profile == (int)CompletedProfile.Incomplete)
                UIController.Instance.ScreenHandler("Profile");
            else GetModelsByUserId(apiResponse.data.id, onSuccess: (modelData) =>
                    {
                        UIController.Instance.ScreenHandler("Home");
                        onSuccess?.Invoke();
                    }, onError: (error) =>
                    {
                        Debug.Log(error);
                    });
        }, onError: (jsonResponse) =>
        {
            APIResponse<UserData> apiResponse = JsonUtility.FromJson<APIResponse<UserData>>(jsonResponse);
            onError.Invoke(apiResponse?.message);
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
        // Crear un objeto con los datos del tutorial
        int modelId = UIController.Instance.CurrentModelIndex;
        int userId = UIController.Instance.UserData?.id ?? -1;
        Debug.Log("Model ID: " + modelId);
        Debug.Log("User ID: " + userId);


        ModelData model = UIController.Instance.ModelData;

        BuildController.Instance.LoadingModal.SetActive(true);
        GetUserModel(userId.ToString(), modelId.ToString(), onSuccess: (userModelData) =>
        {
            if (userModelData != null)
            {
                UIController.Instance.UserModelData = userModelData;

                // BuildController.Instance.Guide = userModelData.guideObject;
                if (BuildController.Instance.GuidesDictionary.ContainsKey(modelId))
                    BuildController.Instance.GuidesDictionary[modelId] = userModelData.guideObject;
                else
                    BuildController.Instance.GuidesDictionary.Add(modelId, userModelData.guideObject);

                if (BuildController.Instance.CurrentStepDictionary.ContainsKey(modelId))
                    BuildController.Instance.CurrentStepDictionary[modelId] = userModelData.guideObject.pasos[userModelData.current_step];
                else
                    BuildController.Instance.CurrentStepDictionary.Add(modelId, userModelData.guideObject.pasos[userModelData.current_step]);

                BuildController.Instance.LoadingModal.SetActive(false);
                BuildController.Instance.GuideResponse.SetActive(true);
                BuildController.Instance.StepTitle.text = userModelData.guideObject.pasos[userModelData.current_step].titulo;
                BuildController.Instance.StepDescription.text = userModelData.guideObject.pasos[userModelData.current_step].descripcion;
                BuildController.Instance.StepCount.text = "Paso " + (userModelData.current_step + 1) + "/" + userModelData.guideObject.pasos.Count;
                BuildController.Instance.MaterialListButton.interactable = true;
                BuildController.Instance.GuideButton.interactable = true;
                BuildController.Instance.FinishButton.interactable = true;
                BuildController.Instance.CalculateAmount();
                BuildController.Instance.CalculateTime();
                return;
            }

            int EXPERIENCE_LEVEL = 0;

            if (UIController.Instance.UserData != null)
            {
                Debug.Log("User experience level: " + UIController.Instance.UserData.experience_level);
                EXPERIENCE_LEVEL = UIController.Instance.UserData.experience_level;

            }

            // Debug.Log("Generando tutorial para el modelo: " + modelId);
            // Debug.Log("Model name: " + model.name);
            // Debug.Log("Model height: " + model.height);
            // Debug.Log("Model width: " + model.width);
            // Debug.Log("Model category: " + model.category_id);

            TutorialData tutorialData = new()
            {
                modelCategory = (Categories)model.category_id,
                modelName = model.name,
                modelSize = new()
                {
                    height = model.height,
                    width = model.width,
                },
                experienceLevel = EXPERIENCE_LEVEL != 0 ? EXPERIENCE_LEVEL : (int)ExperienceLevel.Intermediate,
            };
            // Convertir el objeto a un string JSONa
            string jsonData = JsonUtility.ToJson(tutorialData);

            Debug.Log("experience level: " + EXPERIENCE_LEVEL);

            StartCoroutine(PostRequest(baseUrl + "/openai", jsonData, onSuccess: (jsonResponse) =>
            {
                APIResponse<Guide> apiResponse = JsonConvert.DeserializeObject<APIResponse<Guide>>(jsonResponse);

                BuildController.Instance.GuidesDictionary.Add(modelId, apiResponse?.data);
                BuildController.Instance.CurrentStepDictionary.Add(modelId, apiResponse?.data.pasos[0]);

                BuildController.Instance.LoadingModal.SetActive(false);
                BuildController.Instance.GuideResponse.SetActive(true);
                BuildController.Instance.StepTitle.text = apiResponse.data.pasos[0].titulo;
                BuildController.Instance.StepDescription.text = apiResponse.data.pasos[0].descripcion;
                BuildController.Instance.StepCount.text = "Paso 1/" + apiResponse.data.pasos.Count;
                BuildController.Instance.MaterialListButton.interactable = true;
                BuildController.Instance.GuideButton.interactable = true;
                BuildController.Instance.FinishButton.interactable = true;
                BuildController.Instance.CalculateAmount();
                BuildController.Instance.CalculateTime();

                if (!UIController.Instance.GuestUser)
                {
                    UserModelData userModelData = new()
                    {
                        user_id = UIController.Instance.UserData.id,
                        model_id = UIController.Instance.CurrentModelIndex,
                        guideObject = BuildController.Instance.GuidesDictionary[modelId],
                        completed = (int)CompletedProfile.Incomplete,
                        current_step = 1
                    };

                    Debug.Log("Creando UserModel");

                    CreateUserModel(userModelData, onSuccess: (userModelData) =>
                    {
                        Debug.Log("UserModel creado");
                    }, onError: (error) =>
                    {
                        Debug.Log(error);
                    });
                }

            }, onError: (jsonResponse) =>
            {
                Debug.Log(jsonResponse);
            }));
        }, onError: (error) =>
        {
            Debug.Log(error);
        });
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

    public void CreateFavorite(FavoriteData favoriteData)
    {
        // Convertir el objeto a un string JSON
        // string jsonGuide = JsonUtility.ToJson(userModelData.guide);
        string jsonData = JsonUtility.ToJson(favoriteData);

        StartCoroutine(PostRequest(baseUrl + "/favorites", jsonData, onSuccess: (jsonResponse) =>
        {
            APIResponse<FavoriteData> apiResponse = JsonUtility.FromJson<APIResponse<FavoriteData>>(jsonResponse);
            Debug.Log(apiResponse.message);
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void DeleteFavorite(FavoriteData favoriteData)
    {
        StartCoroutine(DeleteRequest(baseUrl + "/favorites/" + favoriteData.user_id + "/" + favoriteData.model_id, onSuccess: (jsonResponse) =>
        {
            APIResponse<FavoriteData> apiResponse = JsonConvert.DeserializeObject<APIResponse<FavoriteData>>(jsonResponse);
            Debug.Log(apiResponse.message);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void IsFavorite(FavoriteData favoriteData, System.Action<bool> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetRequest(baseUrl + "/favorites/" + favoriteData.user_id + "/" + favoriteData.model_id, onSuccess: (jsonResponse) =>
        {
            bool apiResponse = JsonConvert.DeserializeObject<bool>(jsonResponse);
            onSuccess?.Invoke(apiResponse);
            // Deserializar la cadena JSON dentro del campo 'guide'
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
            // bool apiResponse = JsonConvert.DeserializeObject<bool>(jsonResponse);
            // onSuccess?.Invoke(apiResponse);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
        }));
    }

    public void SendMessageToAI(ChatMessageData chatMessageData, System.Action<string> onSuccess, System.Action<string> onError)
    {

        string jsonData = JsonUtility.ToJson(chatMessageData);
        StartCoroutine(PostRequest(baseUrl + "/openai/message", jsonData, onSuccess: (jsonResponse) =>
        {
            Debug.Log("Devolver mensaje");
            APIResponse<string> apiResponse = JsonConvert.DeserializeObject<APIResponse<string>>(jsonResponse);
            onSuccess?.Invoke(apiResponse.data);
            // Deserializar la cadena JSON dentro del campo 'guide'
        }, onError: (jsonResponse) =>
        {
            Debug.Log(jsonResponse);
            onError?.Invoke(jsonResponse);
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

