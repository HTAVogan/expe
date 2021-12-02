using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using VRtist;

public class ClipManager : MonoBehaviour
{

    public AnimationClip clipJoleen;
    public AnimationClip clipAbe;
    public AnimationClip clipBottle;
    private GameObject joleen;
    private GameObject abe;
    private GameObject bottle;
    private AnimationEngineTradi engine;
    private AnimationConvert converter;
    private void OnEnable()
    {
        AnimationUtility.onCurveWasModified += OnClipModified;
        engine = GlobalStateTradi.Animation;

        converter = gameObject.GetComponent<AnimationConvert>();
    }

    private void Update()
    {
        if (joleen == null)
        {
            joleen = GameObject.Find("Ch34_nonPBR@Throw Object.7818A175.695");
        }
        if (abe == null)
        {
            abe = GameObject.Find("Ch39_nonPBR@Dying.7818A175.698");
        }
        if (bottle == null)
        {
            bottle = GameObject.Find("bottle.7818A175.703");

        }
    }

    private void OnClipModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
    {
        if (clip.name.Contains("Throw"))
        {
            if (joleen != null)
            {

                gameObject.GetComponent<AnimationManager>().ClearAnimationFormOrigin(joleen);
                converter.clip = clip;
                converter.Convert(joleen);
            }

        }
        else if (clip.name.Contains("Dying"))
        {
            if (abe != null)
            {
                gameObject.GetComponent<AnimationManager>().ClearAnimationFormOrigin(abe);

                converter.clip = clip;
                converter.Convert(abe);
            }
        }
        else if (clip.name.Contains("Bottle"))
        {
            if (bottle != null)
            {
                gameObject.GetComponent<AnimationManager>().ClearAnimationFormOrigin(bottle);

                converter.clip = clip;
                converter.Convert(bottle);
            }
        }

    }
}
