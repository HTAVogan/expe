using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRtist;

public class AnimationManager : MonoBehaviour
{




    public void ClearAnimationFormOrigin(GameObject origin)
    {
        GlobalStateTradi.Animation.CurrentFrame = 0;
        foreach (Transform item in origin.transform)
        {
            GlobalStateTradi.Animation.ClearAnimations(item.gameObject);
            ClearAnimationFormOrigin(item.gameObject);
        }

    }



}
