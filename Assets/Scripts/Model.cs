using UnityEngine;

[CreateAssetMenu(fileName = "Model", menuName = "Model", order = 0)]
public class Model : ScriptableObject {
    public string title;
    public string description;
    public Sprite image;
    public int categoryId;
    public int difficultyRating;
}