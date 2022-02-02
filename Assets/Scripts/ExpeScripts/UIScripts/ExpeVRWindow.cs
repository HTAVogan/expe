using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpeVRWindow : MonoBehaviour
{
    public GameObject ExpeWindow;
    private bool isActive = false;


   public void OnClick()
    {
        ExpeWindow.SetActive(!isActive);
        isActive = !isActive;
        if (isActive)
        {
            ExpeWindow.transform.position = gameObject.transform.position;
        }
        foreach(Transform t in ExpeWindow.transform)
        {
            t.gameObject.SetActive(isActive);
        }
    }

 
}
