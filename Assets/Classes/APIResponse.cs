[System.Serializable]
public class APIResponse<T>
{
    public bool? successfully;
    public string message;
    public T data;
}