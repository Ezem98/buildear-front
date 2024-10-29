using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public event Action OnRotateRightAction;
    public event Action OnRotateLeftAction;
    public event Action OnRotateAction;
    public event Action OnResizeAction;
    public event Action OnDeleteAction;
    public event Action OnUndoAction;
    public event Action OnAceptAction;
    public event Action OnMoveAction;
    public event Action OnCancelAction;
    public event Action OnCopyObject;
    public event Action OnDestroyObject;
    public event Action OnHideCanvas;
    public event Action OnMoveRightAction;
    public event Action OnMoveLeftAction;
    public event Action OnMoveBackAction;
    public event Action OnMoveForwardAction;
    public event Action OnChatAction;
    public event Action OnSideScale;
    public event Action OnScaleUp;
    public event Action OnDownScaleSide;
    public event Action OnDownScaleUp;
    public void ChatAction()
    {
        OnChatAction?.Invoke();
    }

    public void RotateAction()
    {
        OnRotateAction?.Invoke();
    }
    public void RotateRightAction()
    {
        OnRotateRightAction?.Invoke();
    }
    public void RotateLeftAction()
    {
        OnRotateLeftAction?.Invoke();
    }
    public void ResizeAction()
    {
        OnResizeAction?.Invoke();
    }
    public void DeleteAction()
    {
        OnDeleteAction?.Invoke();
    }
    public void UndoAction()
    {
        OnUndoAction?.Invoke();
    }
    public void AceptAction()
    {
        OnAceptAction?.Invoke();
    }
    public void MoveAction()
    {
        OnMoveAction?.Invoke();
    }
    public void MoveRight()
    {
        OnMoveRightAction?.Invoke();
    }
    public void MoveLeft()
    {
        OnMoveLeftAction?.Invoke();
    }
    public void MoveBack()
    {
        OnMoveBackAction?.Invoke();
    }
    public void MoveForward()
    {
        OnMoveForwardAction?.Invoke();
    }
    public void CancelAction()
    {
        OnCancelAction?.Invoke();
    }
    public void CopyObject()
    {
        OnCopyObject?.Invoke();
    }

    public void DestroyObject()
    {
        OnDestroyObject?.Invoke();
    }
    public void HideCanvas()
    {
        OnHideCanvas?.Invoke();
    }
    public void SideScale()
    {
        OnSideScale?.Invoke();
    }
    public void ScaleUp()
    {
        OnScaleUp?.Invoke();
    }
    public void DownScaleSide()
    {
        OnDownScaleSide?.Invoke();
    }
    public void DownScaleUp()
    {
        OnDownScaleUp?.Invoke();
    }
}
