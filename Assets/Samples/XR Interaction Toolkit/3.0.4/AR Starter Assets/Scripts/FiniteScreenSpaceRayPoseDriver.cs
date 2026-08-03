using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    /// <summary>
    /// Drives the screen-space ray while ignoring invalid positions reported by
    /// Unity Device Simulator when the pointer is outside the simulated screen.
    /// </summary>
    [AddComponentMenu("XR/Input/Finite Screen Space Ray Pose Driver")]
    public class FiniteScreenSpaceRayPoseDriver : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera associated with the screen.")]
        private Camera m_ControllerCamera;

        [SerializeField]
        private XRInputValueReader<Vector2> m_TapStartPositionInput =
            new("Tap Start Position");

        [SerializeField]
        private XRInputValueReader<Vector2> m_DragCurrentPositionInput =
            new("Drag Current Position");

        [SerializeField]
        private XRInputValueReader<int> m_ScreenTouchCountInput =
            new("Screen Touch Count");

        private Vector2 tapStartPosition;

        private void OnEnable()
        {
            if (m_ControllerCamera == null)
            {
                m_ControllerCamera = Camera.main;
                if (m_ControllerCamera == null)
                {
                    Debug.LogWarning(
                        $"Could not find associated {nameof(Camera)} in scene. " +
                        $"This {nameof(FiniteScreenSpaceRayPoseDriver)} will be disabled.",
                        this
                    );
                    enabled = false;
                    return;
                }
            }

            m_TapStartPositionInput.EnableDirectActionIfModeUsed();
            m_DragCurrentPositionInput.EnableDirectActionIfModeUsed();
            m_ScreenTouchCountInput.EnableDirectActionIfModeUsed();
        }

        private void OnDisable()
        {
            m_TapStartPositionInput.DisableDirectActionIfModeUsed();
            m_DragCurrentPositionInput.DisableDirectActionIfModeUsed();
            m_ScreenTouchCountInput.DisableDirectActionIfModeUsed();
        }

        private void Update()
        {
            Vector2 previousTapStartPosition = tapStartPosition;
            bool tappedThisFrame =
                m_TapStartPositionInput.TryReadValue(out tapStartPosition) &&
                previousTapStartPosition != tapStartPosition;

            if (
                m_ScreenTouchCountInput.TryReadValue(out int screenTouchCount) &&
                screenTouchCount > 1
            )
            {
                return;
            }

            if (
                m_DragCurrentPositionInput.TryReadValue(out Vector2 dragPosition) &&
                TryApplyPose(dragPosition)
            )
            {
                return;
            }

            if (tappedThisFrame)
                TryApplyPose(tapStartPosition);
        }

        private bool TryApplyPose(Vector2 screenPosition)
        {
            if (!IsValidScreenPosition(screenPosition, m_ControllerCamera.pixelRect))
                return false;

            Vector3 screenToWorldPoint = m_ControllerCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    m_ControllerCamera.nearClipPlane
                )
            );
            Vector3 direction = screenToWorldPoint - m_ControllerCamera.transform.position;
            if (
                !IsFinite(screenToWorldPoint) ||
                !IsFinite(direction) ||
                direction.sqrMagnitude <= Mathf.Epsilon
            )
            {
                return false;
            }

            Vector3 localPosition = transform.parent != null
                ? transform.parent.InverseTransformPoint(screenToWorldPoint)
                : screenToWorldPoint;
            Quaternion localRotation = Quaternion.LookRotation(direction.normalized);
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            return true;
        }

        public static bool IsValidScreenPosition(Vector2 screenPosition, Rect cameraPixelRect)
        {
            return IsFinite(screenPosition) && cameraPixelRect.Contains(screenPosition);
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
