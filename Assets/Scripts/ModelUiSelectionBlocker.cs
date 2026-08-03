using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public sealed class ModelUiSelectionBlocker : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    bool m_IsPointerInside;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_IsPointerInside)
            return;

        m_IsPointerInside = true;
        ModelUiInteractionGuard.Enter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ReleaseGuard();
    }

    void OnDisable()
    {
        ReleaseGuard();
    }

    void ReleaseGuard()
    {
        if (!m_IsPointerInside)
            return;

        m_IsPointerInside = false;
        ModelUiInteractionGuard.Exit();
    }
}
