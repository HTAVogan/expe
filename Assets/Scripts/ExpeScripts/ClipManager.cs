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
        GlobalStateTradi.Animation.CurrentFrame = 0;
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
                foreach (Transform item in joleen.transform)
                {
                    if (item.gameObject.name.Contains("Hips"))
                    {
                        InitFirstKeyFrame(Throw, item.gameObject);
                    }
                }
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
                foreach (Transform item in abe.transform)
                {
                    if (item.gameObject.name.Contains("Hips"))
                    {
                        InitFirstKeyFrame(Dye, item.gameObject);
                    }
                }
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
                InitFirstKeyFrame(BottleClip, bottle);
                animBottle.Rebind();
                animBottle.enabled = false;
            }

        }
    }

    private void OnClipModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
    {
        if (binding != null)
        {
            if (binding.path != null)
            {
                String[] pathSplitted = binding.path.Split('/');
                if (pathSplitted.Length > 0)
                {

                    string GameObjectName = pathSplitted[pathSplitted.Length - 1];
                    GameObject go = GameObject.Find(GameObjectName);
                    if (go != null)
                    {

                        AnimatableProperty property = GetProperty(binding.propertyName);
                        if (engine.GetAllAnimations().TryGetValue(go, out AnimationSet set))
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                            set.SetCurve(property, convertCurve(curve));
                        }
                        else
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                            AnimationSet animSet = new AnimationSet(go);
                            //animSet.CreatePositionRotationCurves();
                            animSet.SetCurve(property, convertCurve(curve));
                            engine.SetObjectAnimations(go, animSet);
                        }
                    }
                }
            }
            else
            {

            }

        }

    }

    public void BindPropertiesToClip(GameObject go, AnimationClip clip, GameObject root)
    {
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

    private AnimatableProperty GetProperty(String property)
    {

        switch (property)
        {
            case "m_LocalRotation.x":
                return AnimatableProperty.RotationX;
            case "m_LocalRotation.y":
                return AnimatableProperty.RotationY;
            case "m_LocalRotation.z":
                return AnimatableProperty.RotationZ;
            case "m_LocalPosition.x":
                return AnimatableProperty.PositionX;
            case "m_LocalPosition.y":
                return AnimatableProperty.PositionY;
            case "m_LocalPosition.z":
                return AnimatableProperty.PositionZ;
            case "m_LocalScale.x":
                return AnimatableProperty.ScaleX;
            case "m_LocalScale.y":
                return AnimatableProperty.ScaleY;
            case "m_LocalScale.z":
                return AnimatableProperty.ScaleZ;
            case "localEulerAnglesRaw.x":
                return AnimatableProperty.RotationX;
            case "localEulerAnglesRaw.y":
                return AnimatableProperty.RotationY;
            case "localEulerAnglesRaw.z":
                return AnimatableProperty.RotationZ;
            case "localEulerAngles.x":
                return AnimatableProperty.RotationX;
            case "localEulerAngles.y":
                return AnimatableProperty.RotationY;
            case "localEulerAngles.z":
                return AnimatableProperty.RotationZ;
                ;
            default: return AnimatableProperty.Unknown;
        }
    }

    private List<AnimationKey> convertCurve(AnimationCurve curve)
    {
        List<AnimationKey> ret = new List<AnimationKey>();
        foreach (var item in curve.keys)
        {
            Debug.Log(item.time);
            AnimationKey key = new AnimationKey(Mathf.CeilToInt((item.time * 60 * engine.fps)) + 1, item.value);
            ret.Add(key);
        }
        return ret;
    }

    public void InitFirstKeyFrame(AnimationClip clip, GameObject go)
    {
        AnimationCurve curveX = new AnimationCurve();
        AnimationCurve curveY = new AnimationCurve();
        AnimationCurve curveZ = new AnimationCurve();
        curveX.AddKey(new Keyframe(0, go.transform.localPosition.x));
        curveY.AddKey(new Keyframe(0, go.transform.localPosition.y));
        curveZ.AddKey(new Keyframe(0, go.transform.localPosition.z));
        if (go.transform.childCount > 0)
        { 
            clip.SetCurve(go.name, typeof(Transform), "m_LocalPosition.x", curveX);
            clip.SetCurve(go.name, typeof(Transform), "m_LocalPosition.y", curveY);
            clip.SetCurve(go.name, typeof(Transform), "m_LocalPosition.z", curveZ);
        }
        else
        {
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", curveX);
            clip.SetCurve("",typeof(Transform), "m_LocalPosition.y", curveY);
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.z", curveZ);
        }
    }


}
