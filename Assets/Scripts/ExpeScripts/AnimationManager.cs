using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRtist;

public class AnimationManager : MonoBehaviour
{
    public void ClearAnimationFormOrigin(GameObject origin)
    {
        foreach (Transform item in origin.transform)
        {
            GlobalStateTradi.Animation.ClearAnimations(item.gameObject);
            ClearAnimationFormOrigin(item.gameObject);
        }

    }

    private void Start()
    {
        GlobalStateTradi.Animation.StartFrame = 0;
    }

}
