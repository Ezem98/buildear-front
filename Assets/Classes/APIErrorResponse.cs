[System.Serializable]
public class APIErrorResponse
{
    public APIError error;
    public string message;
}

[System.Serializable]
public class APIError
{
    public string code;
    public string message;
}
