using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    public static class ModelUiInteractionGuard
    {
        static int s_HoverCount;

        public static bool isPointerOverModelUi => s_HoverCount > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            s_HoverCount = 0;
        }

        public static void Enter()
        {
            s_HoverCount++;
        }

        public static void Exit()
        {
            s_HoverCount = Mathf.Max(0, s_HoverCount - 1);
        }
    }

    public interface IModelCanvasController
    {
        void ActivateModelCanvas();
        void HideCanvas();
    }

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

        [SerializeField]
        bool m_AlignToSurfaceNormal;

        [SerializeField]
        Vector3 m_LocalSurfaceNormal = Vector3.forward;

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

        public static Vector3 GetLocalRotationAxis(GameObject target, bool rotateRight)
        {
            SurfacePlacementOffset placementSettings =
                target != null ? target.GetComponentInParent<SurfacePlacementOffset>() : null;

            if (placementSettings != null && placementSettings.enableEdgeSnap)
                return rotateRight ? Vector3.back : Vector3.forward;

            return rotateRight ? Vector3.down : Vector3.up;
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

        public bool alignToSurfaceNormal
        {
            get => m_AlignToSurfaceNormal;
            set => m_AlignToSurfaceNormal = value;
        }

        public Vector3 localSurfaceNormal
        {
            get => m_LocalSurfaceNormal;
            set => m_LocalSurfaceNormal = value.sqrMagnitude > Mathf.Epsilon
                ? value.normalized
                : Vector3.forward;
        }

        public Quaternion GetSurfaceAlignedRotation(
            Quaternion currentRotation,
            Vector3 surfaceNormal
        )
        {
            if (!m_AlignToSurfaceNormal || surfaceNormal.sqrMagnitude <= Mathf.Epsilon)
                return currentRotation;

            Vector3 localNormal = m_LocalSurfaceNormal.sqrMagnitude > Mathf.Epsilon
                ? m_LocalSurfaceNormal.normalized
                : Vector3.forward;
            Vector3 currentWorldNormal = currentRotation * localNormal;
            return Quaternion.FromToRotation(currentWorldNormal, surfaceNormal.normalized) *
                currentRotation;
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
            if (ModelUiInteractionGuard.isPointerOverModelUi)
                return false;

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
            GetCanvasController()?.ActivateModelCanvas();
        }

        public void HideMenusInSnapGroup(bool includeThisObject)
        {
            foreach (var placementSettings in FindActiveInLoadedScenes())
            {
                if (!placementSettings.m_ActivateCanvasOnSelect ||
                    placementSettings.m_SnapGroup != m_SnapGroup ||
                    (!includeThisObject && placementSettings == this))
                {
                    continue;
                }

                placementSettings.GetCanvasController()?.HideCanvas();
            }
        }

        IModelCanvasController GetCanvasController()
        {
            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour is IModelCanvasController canvasController)
                    return canvasController;
            }

            return null;
        }

        public static IEnumerable<SurfacePlacementOffset> FindActiveInLoadedScenes()
        {
            foreach (
                var placementSettings in
                Resources.FindObjectsOfTypeAll<SurfacePlacementOffset>()
            )
            {
                if (placementSettings != null &&
                    placementSettings.gameObject.scene.IsValid() &&
                    placementSettings.gameObject.activeInHierarchy)
                {
                    yield return placementSettings;
                }
            }
        }

    }
}
