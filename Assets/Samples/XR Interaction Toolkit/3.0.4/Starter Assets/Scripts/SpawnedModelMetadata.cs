namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Identifies the backend model represented by a spawned AR object.
    /// The metadata is copied together with the GameObject, so deleting a copy
    /// updates the correct model count even after another model is selected.
    /// </summary>
    public sealed class SpawnedModelMetadata : MonoBehaviour
    {
        public int ModelId { get; private set; }

        public void Initialize(int modelId)
        {
            ModelId = modelId;
        }
    }
}
