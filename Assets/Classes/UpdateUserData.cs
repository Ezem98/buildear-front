using Newtonsoft.Json;

public class UpdateUserData
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string username;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string email;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? experience_level;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string image;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? completed_profile;
}
