using Newtonsoft.Json;

[System.Serializable]
public class UserModelData
{
    public int id;
    public int user_id;
    public int model_id;
    public bool completed;
    public int current_step;
    // Cambiamos 'guide' a string para la primera deserialización
    public string guide;
    // Esta variable contendrá el objeto deserializado
    [JsonIgnore]
    public Guide guideObject;
    public string created_at;
    public string updated_at;
}