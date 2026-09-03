using Newtonsoft.Json;

[System.Serializable]
public class RegisterData
{
    public string name;
    public string surname;
    public string username;
    public string email;
    public string password;
    public int experience_level = 0;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string image;

    public int completed_profile = 0;
}
