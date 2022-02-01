using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class ActionCountTradi : MonoBehaviour
{
    public int actionsCount;
    public Dictionary<ActionsDone, int> actions = new Dictionary<ActionsDone, int>();

    public enum ActionsDone
    {
        LeftClick,
        RightClick,
        ScrollClick

    }
    void Start()
    {
        actionsCount = 0;
    }


    public void OnClickedEvent(ActionsDone action)
    {
        actionsCount++;
        actions.TryGetValue(action, out int currentValue);
        currentValue++;
        actions.Remove(action);
        actions.Add(action, currentValue);
    }

    public void LeftClicked(CallbackContext context)
    {
        if (context.performed)
        {
            OnClickedEvent(ActionsDone.LeftClick);
        }
    }
    public void RightClicked(CallbackContext context)
    {
        if(context.performed)
        OnClickedEvent(ActionsDone.RightClick);
    
    }

    public void ScrollClicked(CallbackContext context)
    {
        if (context.performed)
            OnClickedEvent(ActionsDone.ScrollClick);
     
    }

}
