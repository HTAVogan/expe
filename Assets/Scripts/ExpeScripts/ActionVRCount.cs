using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRtist;
public class ActionVRCount : MonoBehaviour
{

    public int numberOfAction;
    private string previousButton = "";
    private int previousActionsNumber = 0;

   


    // Update is called once per frame
    void Update()
    {
        if( VRInput.primaryControllerValues.gripButtonPressed && !previousButton.Contains("gripP"))
        {
            previousButton = "gripP";
            numberOfAction++;
            Debug.Log("primary grip pressed");
        }
        if (VRInput.primaryControllerValues.triggerButtonPressed)
        {
            previousButton = "triggerP";
            numberOfAction++;
            Debug.Log("trigger pressed");
        }
        if (VRInput.primaryControllerValues.primary2DAxisClickState && !previousButton.Contains("AxisP"))
        {
            previousButton = "AxisP";
            numberOfAction++;
        }
        if (VRInput.primaryControllerValues.primaryButtonState && !previousButton.Contains("primaryBP"))
        {
            previousButton = "primaryBP";
            numberOfAction++;
        }
        if (VRInput.primaryControllerValues.secondaryButtonState && !previousButton.Contains("secondaryBP"))
        {
            previousButton = "secondaryBP";
            numberOfAction++;
        }
        if (VRInput.secondaryControllerValues.gripButtonPressed && !previousButton.Contains("gripS"))
        {
            previousButton = "gripS";
            numberOfAction++;
        }
        if (VRInput.secondaryControllerValues.triggerButtonPressed && !previousButton.Contains("triggerS"))
        {
            previousButton = "triggerS";
            numberOfAction++;
        }
        if (VRInput.secondaryControllerValues.primary2DAxisClickState && !previousButton.Contains("AxisS"))
        {
            previousButton = "AxisS";
            numberOfAction++;
        }
        if (VRInput.secondaryControllerValues.primaryButtonState && !previousButton.Contains("primaryBS"))
        {
            previousButton = "primaryBS";
            numberOfAction++;
        }
        if (VRInput.secondaryControllerValues.secondaryButtonState && !previousButton.Contains("secondaryBS"))
        {
            previousButton = "secondaryBS";
            numberOfAction++;
        }
        if(previousActionsNumber != numberOfAction)
        {
            Debug.Log(previousActionsNumber + "and now : " + numberOfAction);
            previousActionsNumber = numberOfAction;

        }
    }
}
