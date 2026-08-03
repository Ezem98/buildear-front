using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    [DisallowMultipleComponent]
    public class SurfacePlacementOffset : MonoBehaviour
    {
        const float k_MinimumColliderThickness = 0.01f;

        [SerializeField, Min(0f)]
        float m_Offset;

        [SerializeField]
        bool m_FitBoxColliderToMesh;

        [SerializeField]
        bool m_EnableEdgeSnap;

        [SerializeField, Min(0f)]
        float m_SnapDistance = 0.25f;

        [SerializeField]
        string m_SnapGroup;

        [SerializeField]
        bool m_ActivateCanvasOnSelect;

        XRGrabInteractable m_GrabInteractable;

        public float offset
        {
            get => m_Offset;
            set => m_Offset = Mathf.Max(0f, value);
        }

        public bool enableEdgeSnap
        {
            get => m_EnableEdgeSnap;
            set => m_EnableEdgeSnap = value;
        }

        public float snapDistance
        {
            get => m_SnapDistance;
            set => m_SnapDistance = Mathf.Max(0f, value);
        }

        public string snapGroup
        {
            get => m_SnapGroup;
            set => m_SnapGroup = value;
        }

        public bool activateCanvasOnSelect
        {
            get => m_ActivateCanvasOnSelect;
            set
            {
                m_ActivateCanvasOnSelect = value;
                RefreshSelectionListener();
            }
        }

        void Awake()
        {
            if (m_FitBoxColliderToMesh)
                FitBoxColliderToMesh();

            RefreshSelectionListener();
        }

        void OnDestroy()
        {
            if (m_GrabInteractable != null)
                m_GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        }

        public void FitBoxColliderToMesh()
        {
            var meshFilter = GetComponent<MeshFilter>();
            var boxCollider = GetComponent<BoxCollider>();
            if (meshFilter == null || meshFilter.sharedMesh == null || boxCollider == null)
                return;

            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            Vector3 colliderSize = meshBounds.size;
            colliderSize.x = Mathf.Max(colliderSize.x, k_MinimumColliderThickness);
            colliderSize.y = Mathf.Max(colliderSize.y, k_MinimumColliderThickness);
            colliderSize.z = Mathf.Max(colliderSize.z, k_MinimumColliderThickness);

            boxCollider.center = meshBounds.center;
            boxCollider.size = colliderSize;
        }

        void RefreshSelectionListener()
        {
            if (m_GrabInteractable != null)
                m_GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);

            m_GrabInteractable = GetComponent<XRGrabInteractable>();
            if (m_ActivateCanvasOnSelect && m_GrabInteractable != null)
                m_GrabInteractable.selectEntered.AddListener(OnSelectEntered);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            gameObject.SendMessage(
                "ActivateModelCanvas",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }
}
