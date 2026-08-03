using UnityEngine;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    [DisallowMultipleComponent]
    public class SurfacePlacementOffset : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        float m_Offset;

        public float offset
        {
            get => m_Offset;
            set => m_Offset = Mathf.Max(0f, value);
        }
    }
}
