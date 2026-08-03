using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    [DisallowMultipleComponent]
    public class SurfacePlacementOffset : MonoBehaviour, IXRSelectFilter
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

        public bool canProcess => isActiveAndEnabled && m_ActivateCanvasOnSelect;

        void Awake()
        {
            if (m_FitBoxColliderToMesh)
                FitBoxColliderToMesh();

            RefreshSelectionListener();
        }

        void OnDestroy()
        {
            if (m_GrabInteractable != null)
            {
                m_GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                m_GrabInteractable.selectFilters.Remove(this);
            }
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
            {
                m_GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                m_GrabInteractable.selectFilters.Remove(this);
            }

            m_GrabInteractable = GetComponent<XRGrabInteractable>();
            if (m_ActivateCanvasOnSelect && m_GrabInteractable != null)
            {
                m_GrabInteractable.selectEntered.AddListener(OnSelectEntered);
                m_GrabInteractable.selectFilters.Add(this);
            }
        }

        public bool Process(
            IXRSelectInteractor interactor,
            IXRSelectInteractable interactable
        )
        {
            if (interactor is XRRayInteractor rayInteractor &&
                rayInteractor.TryGetCurrentUIRaycastResult(out var uiRaycastResult) &&
                uiRaycastResult.gameObject != null)
            {
                return false;
            }

            return true;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            ActivateSelectionMenu();
        }

        public void ActivateSelectionMenu()
        {
            HideMenusInSnapGroup(false);
            gameObject.SendMessage(
                "ActivateModelCanvas",
                SendMessageOptions.DontRequireReceiver
            );
        }

        public void HideMenusInSnapGroup(bool includeThisObject)
        {
            foreach (var placementSettings in FindObjectsOfType<SurfacePlacementOffset>())
            {
                if (!placementSettings.m_ActivateCanvasOnSelect ||
                    placementSettings.m_SnapGroup != m_SnapGroup ||
                    (!includeThisObject && placementSettings == this))
                {
                    continue;
                }

                placementSettings.gameObject.SendMessage(
                    "HideCanvas",
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }

    }
}
