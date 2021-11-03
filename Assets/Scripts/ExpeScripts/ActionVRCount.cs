using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRtist;
public class ActionVRCount : MonoBehaviour
{

    public int numberOfAction;
      // Update is called once per frame
    void Update()
    {
        VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.trigger, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primaryButton, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primary2DAxisClick, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.secondaryButton, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.grip, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.trigger, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.primaryButton, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.primary2DAxisClick, onRelease: () => { numberOfAction++; });
        VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.secondaryButton, onRelease: () => { numberOfAction++; });
    }
}
