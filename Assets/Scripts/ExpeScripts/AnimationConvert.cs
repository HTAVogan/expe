using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace VRtist
{
    public class AnimationConvert : MonoBehaviour
    {

        public class ClipData
        {
            public List<Keyframe> PositionX = new List<Keyframe>();
            public List<Keyframe> PositionY = new List<Keyframe>();
            public List<Keyframe> PositionZ = new List<Keyframe>();
            public List<Keyframe> RotationX = new List<Keyframe>();
            public List<Keyframe> RotationY = new List<Keyframe>();
            public List<Keyframe> RotationZ = new List<Keyframe>();
            public List<Keyframe> RotationW = new List<Keyframe>();
            public List<Keyframe> ScaleX = new List<Keyframe>();
            public List<Keyframe> ScaleY = new List<Keyframe>();
            public List<Keyframe> ScaleZ = new List<Keyframe>();
        }

        public AnimationClip clip;
        public float fps = 60f;

        private Dictionary<GameObject, ClipData> _clipData;

        public void Start()
        {
            if (clip != null) Convert();
        }

        [ContextMenu("Convert")]
        public void Convert(GameObject go = null)
        {
            _clipData = new Dictionary<GameObject, ClipData>();
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                //GameObject go = (AnimationUtility.GetAnimatedObject(gameObject, bindings[i]) as Transform).gameObject;
                if (!_clipData.ContainsKey(go))
                {
                    _clipData[go] = new ClipData();
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, bindings[i]);
                List<Keyframe> target = GetPropertyList(_clipData[go], bindings[i].propertyName);
                foreach(Keyframe key in curve.keys)
                {
                    target.Add(key);
                }
            }

            foreach(KeyValuePair<GameObject, ClipData> animData in _clipData)
            {
                AnimationSet animationSet = new AnimationSet(animData.Key);
                for(int iPos = 0; iPos < animData.Value.PositionX.Count; iPos++)
                {
                    int frame = Mathf.CeilToInt(animData.Value.PositionX[iPos].time * fps) +1;
                    animationSet.curves[AnimatableProperty.PositionX].AddKey(new AnimationKey(frame, animData.Value.PositionX[iPos].value));
                    animationSet.curves[AnimatableProperty.PositionY].AddKey(new AnimationKey(frame, animData.Value.PositionY[iPos].value));
                    animationSet.curves[AnimatableProperty.PositionZ].AddKey(new AnimationKey(frame, animData.Value.PositionZ[iPos].value));
                }
                for (int iScale = 0; iScale < animData.Value.ScaleX.Count; iScale++)
                {
                    int frame = Mathf.CeilToInt(animData.Value.ScaleX[iScale].time * fps) + 1;
                    animationSet.curves[AnimatableProperty.ScaleX].AddKey(new AnimationKey(frame, animData.Value.ScaleX[iScale].value));
                    animationSet.curves[AnimatableProperty.ScaleY].AddKey(new AnimationKey(frame, animData.Value.ScaleY[iScale].value));
                    animationSet.curves[AnimatableProperty.ScaleZ].AddKey(new AnimationKey(frame, animData.Value.ScaleZ[iScale].value));
                }
                Vector3 previousRotation = Vector3.zero;
                for (int iRot = 0; iRot < animData.Value.RotationX.Count; iRot++)
                {
                    Vector3 rotation = new Quaternion(animData.Value.RotationX[iRot].value, animData.Value.RotationY[iRot].value, animData.Value.RotationZ[iRot].value, animData.Value.RotationW[iRot].value).eulerAngles;
                    rotation.x = previousRotation.x + Mathf.DeltaAngle(previousRotation.x, rotation.x);
                    rotation.y = previousRotation.y + Mathf.DeltaAngle(previousRotation.y, rotation.y);
                    rotation.z = previousRotation.z + Mathf.DeltaAngle(previousRotation.z, rotation.z);
                    int frame = Mathf.FloorToInt(animData.Value.RotationX[iRot].time * fps) + 1;
                    animationSet.curves[AnimatableProperty.RotationX].AddKey(new AnimationKey(frame, rotation.x));
                    animationSet.curves[AnimatableProperty.RotationY].AddKey(new AnimationKey(frame, rotation.y));
                    animationSet.curves[AnimatableProperty.RotationZ].AddKey(new AnimationKey(frame, rotation.z));
                    previousRotation = rotation;
                }
                GlobalStateTradi.Animation.SetObjectAnimations(animData.Key, animationSet);
            }
        }

        public List<Keyframe> GetPropertyList(ClipData data, string property)
        {
            switch (property)
            {
                case "m_LocalRotation.x":
                    return data.RotationX;
                case "m_LocalRotation.y":
                    return data.RotationY;
                case "m_LocalRotation.z":
                    return data.RotationZ;
                case "m_LocalRotation.w":
                    return data.RotationW;
                case "m_LocalPosition.x":
                    return data.PositionX;
                case "m_LocalPosition.y":
                    return data.PositionY;
                case "m_LocalPosition.z":
                    return data.PositionZ;
                case "m_LocalScale.x":
                    return data.ScaleX;
                case "m_LocalScale.y":
                    return data.ScaleY;
                case "m_LocalScale.z":
                    return data.ScaleZ;
                default: return null;
            }
        }
    }
}
