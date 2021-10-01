using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class HumanGoalController : MonoBehaviour
    {
        public List<Transform> PathToRoot = new List<Transform>();
        public List<AnimationSet> AnimToRoot = new List<AnimationSet>();
        public AnimationSet Animation;

        public void SetPathToRoot(List<Transform> path)
        {
            path.ForEach(x =>
            {
                AnimationSet anim = GlobalState.Animation.GetObjectAnimation(x.gameObject);
                PathToRoot.Add(x);
                AnimToRoot.Add(anim);
            });
            Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
        }

        public Vector3 FramePosition(int frame)
        {
            if (null == Animation) GlobalState.Animation.GetObjectAnimation(this.gameObject);
            if (null == Animation) return Vector3.zero;

            Vector3 parentPosition = GetBonePosition(0, frame);

            for (int i = 1; i < PathToRoot.Count; i++)
            {
                Vector3 position = GetBonePosition(i, frame);
                if(position != Vector3.zero)
                {
                    parentPosition = position;
                    continue;
                }

            }
            return transform.InverseTransformPoint(Vector3.zero);
        }

        public Vector3 GetBonePosition(int index, int frame)
        {
            if (null == AnimToRoot[index]) AnimToRoot[index] = GlobalState.Animation.GetObjectAnimation(PathToRoot[index].gameObject);
            if (null == AnimToRoot[index]) return Vector3.zero;
            Curve posx = AnimToRoot[index].GetCurve(AnimatableProperty.PositionX);
            Curve posy = AnimToRoot[index].GetCurve(AnimatableProperty.PositionY);
            Curve posz = AnimToRoot[index].GetCurve(AnimatableProperty.PositionZ);
            if (null != posx && null != posy && null != posz)
            {
                if (posx.Evaluate(frame, out float px) &&
                    posy.Evaluate(frame, out float py) &&
                    posz.Evaluate(frame, out float pz))
                {
                    return PathToRoot[index].TransformPoint(new Vector3(px, py, pz));
                }
            }
            return Vector3.zero;
        }

        //public Quaternion GetBoneRotation(int index, int frame)
        //{
        //    if (null == AnimToRoot[index]) AnimToRoot[index] = GlobalState.Animation.GetObjectAnimation(PathToRoot[index].gameObject);
        //    if (null == AnimToRoot[index]) return Quaternion.identity;
        //    Curve rotx = AnimToRoot[index].GetCurve(AnimatableProperty.PositionX);
        //    Curve roty = AnimToRoot[index].GetCurve(AnimatableProperty.PositionY);
        //    Curve rotz = AnimToRoot[index].GetCurve(AnimatableProperty.PositionZ);
        //    if (null != rotx && null != roty && null != rotz)
        //    {
        //        if (rotx.Evaluate(frame, out float rx) &&
        //            roty.Evaluate(frame, out float ry) &&
        //            rotz.Evaluate(frame, out float rz))
        //        {
        //            return PathToRoot[index].TransformPoint(new Vector3(px, py, pz));
        //        }
        //    }
        //    return Quaternion.identity;
        //}

    }
}
