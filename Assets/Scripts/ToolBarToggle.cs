using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ToolBarToggle : MonoBehaviour
{
    [SerializeField]
    bool initialState;
    [SerializeField]
    UIAnimation uiAnimation;
    [SerializeField]
    UnityEvent turnedOn = new();
    [SerializeField]
    UnityEvent turnedOff = new();

    public UnityEvent TurnedOn => turnedOn;
    public UnityEvent TurnedOff => turnedOff;

    public UIAnimation UIAnimation
    {
        get => uiAnimation;
        set => uiAnimation = value;
    }

    public bool CurrentState => uiAnimation != null && uiAnimation.IsOpen;

    private void Awake()
    {
        if (uiAnimation == null)
        {
            uiAnimation = GetComponent<UIAnimation>();
        }
    }

    public void ToggleState()
    {
        if (uiAnimation != null)
        {
            if (uiAnimation.IsOpen)
            {
                TurnOff();
            }
            else
            {
                TurnOn();
            }
        }
        else
        {
            turnedOn?.Invoke();
        }
    }

    public void Toggle() => ToggleState();

    public void TurnOn()
    {
        uiAnimation?.Open();
        turnedOn?.Invoke();
    }

    public void TurnOff()
    {
        uiAnimation?.Close();
        turnedOff?.Invoke();
    }
}
