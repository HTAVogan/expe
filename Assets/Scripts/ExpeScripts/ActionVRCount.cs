using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRtist;
public class ActionVRCount : MonoBehaviour
{

    public int numberOfAction;
    private string previousButton = "";




    // Update is called once per frame
    void Update()
    {
        if( VRInput.primaryControllerValues.gripButtonPressed && !previousButton.Contains("gripP"))
        {
            previousButton = "gripP";
            numberOfAction++;
        }
        if (VRInput.primaryControllerValues.triggerButtonPressed && !previousButton.Contains("triggerP"))
        {
            previousButton = "triggerP";
            numberOfAction++;
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

        //VRINPUT GET INPUT STATE ! record which one was press and check if it is not the same : if not : ++
    }
}
