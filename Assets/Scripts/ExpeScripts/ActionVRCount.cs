using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRtist;
public class ActionVRCount : MonoBehaviour
{

    public int numberOfAction;
    public Dictionary<string, int> inputsDone = new Dictionary<string, int>();
    bool isprimGripped = false;
    bool issecondaryGripped = false;
    bool needToUpTranslation = false;
    GostManager gostManager;

    private void Start()
    {
        gostManager = GameObject.Find("GostManager").GetComponent<GostManager>();
    }
    // Update is called once per frame
    void Update()
    {
        if (gostManager.areGostGenerated)
        {

            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip, onPress: () => Onpressed(VRInput.primaryController), onRelease: () =>
            {
                numberOfAction++;
                Debug.Log("Primary gripp pressed, number of action = " + numberOfAction);
                if (needToUpTranslation)
                {
                    if (inputsDone.TryGetValue("gripMove", out int number))
                    {
                        number++;
                        inputsDone.Remove("gripMove");
                        inputsDone.Add("gripMove", number);
                    }
                    else
                    {
                        inputsDone.Add("gripMove", 1);
                    }
                }
                else if (inputsDone.TryGetValue("grip", out int number))
                {
                    number++;
                    inputsDone.Remove("grip");
                    inputsDone.Add("grip", number);
                }
                else { inputsDone.Add("grip", 1); }
                isprimGripped = false;
            });
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.trigger, onRelease: () =>
            {
                numberOfAction++;
                if (inputsDone.TryGetValue("trigger", out int number)) {
                    number++;
                    inputsDone.Remove("trigger");
                    inputsDone.Add("trigger", number);
                }
                else { inputsDone.Add("trigger", 1); }
            });
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primaryButton, onRelease: () =>
            {
                numberOfAction++;
                if (inputsDone.TryGetValue("primaryButton", out int number)) { 
                    number++;
                    inputsDone.Remove("primaryButton");
                    inputsDone.Add("primaryButton", number);
                }
                else
                { inputsDone.Add("primaryButton", 1); }
            });
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primary2DAxisClick, onRelease: () =>
            {
                numberOfAction++;
                if (inputsDone.TryGetValue("joystick", out int number)) {
                    number++;
                    inputsDone.Remove("joystick");
                    inputsDone.Add("joystick", number);
                }
                else { inputsDone.Add("joystick", 1); }
            });
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.secondaryButton, onRelease: () =>
            {
                numberOfAction++;
                if (inputsDone.TryGetValue("secondaryButton", out int number)) { 
                    number++;
                    inputsDone.Remove("secondaryButton");
                    inputsDone.Add("secondaryButton", number);
                }
                else { inputsDone.Add("secondaryButton", 1); }
            });
            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.grip, onPress: () => Onpressed(VRInput.primaryController), onRelease: () =>
            {
                numberOfAction++;
                if (needToUpTranslation)
                {
                    if (inputsDone.TryGetValue("gripMove", out int number))
                    {
                        number++;
                        inputsDone.Remove("gripMove");
                        inputsDone.Add("gripMove", number);
                    }
                    else
                    {
                        inputsDone.Add("gripMove", 1);
                    }
                }
                else if (inputsDone.TryGetValue("secondaryCGrip", out int number)) { 
                    number++;
                    inputsDone.Remove("secondaryCGrip");
                    inputsDone.Add("secondaryCGrip", number);
                }
                else { inputsDone.Add("secondaryCGrip", 1); }
                issecondaryGripped = false;
            });
            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.trigger, onRelease: () =>
            {
                numberOfAction++;
                if (inputsDone.TryGetValue("secondaryCtrigger", out int number)) { 
                    number++;
                    inputsDone.Remove("secondaryCtrigger");
                    inputsDone.Add("secondaryCtrigger", number);
                }
                else { inputsDone.Add("secondaryCtrigger", 1); }
            });
            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.primaryButton, onRelease: () =>
            {
                numberOfAction++;
                if (inputsDone.TryGetValue("secondaryCprimaryButton", out int number)) {
                    number++;
                    inputsDone.Remove("secondaryCprimaryButton");
                    inputsDone.Add("secondaryCprimaryButton", number);
                }
                else { inputsDone.Add("secondaryCprimaryButton", 1); }
            });
            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.primary2DAxisClick, onRelease: () =>
            {
                numberOfAction++;
                if (inputsDone.TryGetValue("secondaryCjoystick", out int number)) { 
                    number++;
                    inputsDone.Remove("secondaryCjoystick");
                    inputsDone.Add("secondaryCjoystick", number);
                }
                else { inputsDone.Add("secondaryCjoystick", 1); }
            });
            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.secondaryButton, onRelease: () =>
            {
                numberOfAction++;
                if (inputsDone.TryGetValue("secondaryCsecondaryButton", out int number)) {
                    number++;
                    inputsDone.Remove("secondaryCsecondaryButton");
                    inputsDone.Add("secondaryCsecondaryButton", number);
                }
                else { inputsDone.Add("secondaryCsecondaryButton", 1); }
            });
        }

    }

    private void Onpressed(InputDevice device)
    {
        if(device == VRInput.primaryController)
        {
            isprimGripped = true;
            if (issecondaryGripped)
            {
                needToUpTranslation = true;
            }
        }
        else
        {
            issecondaryGripped = true;
            if (isprimGripped)
            {
                    needToUpTranslation = true;
            }
        }
    }
}
