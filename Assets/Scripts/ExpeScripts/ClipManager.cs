using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using VRtist;

public class ClipManager : MonoBehaviour
{


    private GameObject joleen;
    private GameObject abe;
    private GameObject bottle;
    private AnimationEngineTradi engine;
    private AnimationConvert converter;
    public AnimationClip Throw;
    public AnimationClip Dye;
    public AnimationClip BottleClip;
    public RuntimeAnimatorController controllerJoleen;
    public RuntimeAnimatorController controllerBottle;
    public RuntimeAnimatorController controllerAbe;
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

            if (joleen != null)
            {
                Animator animJoleen = joleen.AddComponent<Animator>();
                animJoleen.runtimeAnimatorController = controllerJoleen;
                Throw.ClearCurves();
                BindPropertiesToClip(joleen, Throw, joleen);
                animJoleen.Rebind();
                animJoleen.enabled = false;
            }
        }
        if (abe == null)
        {
            abe = GameObject.Find("Ch39_nonPBR@Dying.7818A175.698");
            if (abe != null)
            {
                Animator animAbe = abe.AddComponent<Animator>();
                animAbe.runtimeAnimatorController = controllerAbe;
                Dye.ClearCurves();
                BindPropertiesToClip(abe, Dye, abe);
                animAbe.Rebind();
                animAbe.enabled = false;
            }
        }
        if (bottle == null)
        {
            bottle = GameObject.Find("bottle.7818A175.703");
            if (bottle != null)
            {
                Animator animBottle = bottle.AddComponent<Animator>();
                animBottle.runtimeAnimatorController = controllerBottle;
                BottleClip.ClearCurves();
                BindPropertiesToClip(bottle, BottleClip, bottle);
                animBottle.Rebind();
                animBottle.enabled = false;
            }

        }
    }

    private void OnClipModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
    {
        //TODO VERIFY ANIMATION PROPERTY AND REBIND ONLY CURVES WITH THAT 
        if (binding.propertyName.Contains("z") && type == AnimationUtility.CurveModifiedType.ClipModified)
        {
            if (clip.name.Contains("Throw"))
            {
                if (joleen != null)
                {
                    Debug.Log(binding.propertyName + " and path is " + binding.path);
                    //GlobalStateTradi.Animation.GetAllAnimations().ContainsKey();
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

    private void BindPropertiesToClip(GameObject go, AnimationClip clip, GameObject root)
    {
        Dictionary<EditorCurveBinding, AnimationCurve> _animationCurveBindings = new Dictionary<EditorCurveBinding, AnimationCurve>();
        EditorCurveBinding[] bindings = AnimationUtility.GetAnimatableBindings(go, root);

        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.type == typeof(Transform))
            {
                clip.SetCurve(binding.path, typeof(Transform), binding.propertyName, new AnimationCurve());
            }
        }
        foreach (Transform item in go.transform)
        {
            BindPropertiesToClip(item.gameObject, clip, root);
        }
    }

    private string GetGameObjectPath(Transform transform, Transform root)
    {
        string path = transform.name;
        while (transform != root)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

}
