using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VRtist;
using static UnityEngine.InputSystem.InputAction;

public class ActionCountTradi : MonoBehaviour
{
    public int actionsCount;
    public Dictionary<ActionsDone, int> actions = new Dictionary<ActionsDone, int>();

    GostManager manager;



    public enum ActionsDone
    {
        LeftClick,
        RightClick,
        ScrollClick

    }
    void Start()
    {
        actionsCount = 0;
        manager = GameObject.Find("AnimationManager").GetComponent<GostManager>();


    }

    private void OnCancel()
    {
        if (GlobalStateTradi.Instance != null)
        {
            GlobalStateTradi.Instance.RecordTranslation();
        }
    }

    private void OnStart()
    {
        if(GlobalStateTradi.Instance != null)
        {
            GlobalStateTradi.Instance.ModifyPrevious();
        }
    }
    
    public void OnClickedEvent(ActionsDone action)
    {
        if (manager.areGostGenerated)
        {
            actionsCount++;
            actions.TryGetValue(action, out int currentValue);
            currentValue++;
            actions.Remove(action);
            actions.Add(action, currentValue);
        }
    }

    public void LeftClicked(CallbackContext context)
    {
        if (context.started)
        {
            OnStart();
        }
        if (context.performed)
        {
            OnClickedEvent(ActionsDone.LeftClick);
        }
        if (context.canceled)
        {
            OnCancel();
        }

    }
    public void RightClicked(CallbackContext context)
    {
        if (context.started)
        {
            OnStart();
        }
        if (context.performed)
            OnClickedEvent(ActionsDone.RightClick);
        if (context.canceled)
        {
            OnCancel();
        }

    }

    public void ScrollClicked(CallbackContext context)
    {
        if (context.started)
        {
            OnStart();
        }
        if (context.performed)
            OnClickedEvent(ActionsDone.ScrollClick);
        if (context.canceled)
        {
            OnCancel();
        }

    }

}
