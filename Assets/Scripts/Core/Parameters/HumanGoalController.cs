using System;
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
        public SkinMeshController RootController;

        [Range(0, 1)]
        public float stiffness;

        public void SetPathToRoot(SkinMeshController controller, List<Transform> path)
        {
            path.ForEach(x =>
            {
                AnimationSet anim = GlobalState.Animation.GetObjectAnimation(x.gameObject);
                PathToRoot.Add(x);

                AnimToRoot.Add(anim);
            });
            Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
            RootController = controller;
        }

        public Vector3 FramePosition(int frame)
        {
            if (null == Animation) Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
            if (null == Animation) return Vector3.zero;

            Matrix4x4 trsMatrix = PathToRoot[0].parent.localToWorldMatrix;

            if (PathToRoot.Count > 1)
            {
                for (int i = 0; i < PathToRoot.Count; i++)
                {
                    if (null != AnimToRoot[i])
                        trsMatrix = trsMatrix * AnimToRoot[i].GetTranformMatrix(frame);
                }
            }
            trsMatrix = trsMatrix * Animation.GetTranformMatrix(frame);

            Maths.DecomposeMatrix(trsMatrix, out Vector3 parentPosition, out Quaternion quaternion, out Vector3 scale);
            return parentPosition;
        }

        public Matrix4x4 FrameMatrix(int frame)
        {
            if (null == Animation) Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
            if (null == Animation) return Matrix4x4.identity;

            Matrix4x4 trsMatrix = PathToRoot[0].parent.localToWorldMatrix;

            if (PathToRoot.Count > 1)
            {
                for (int i = 0; i < PathToRoot.Count; i++)
                {
                    trsMatrix = trsMatrix * AnimToRoot[i].GetTranformMatrix(frame);
                }
            }
            trsMatrix = trsMatrix * Animation.GetTranformMatrix(frame);
            return trsMatrix;
        }

        public void CheckAnimations()
        {
            AnimToRoot.Clear();
            PathToRoot.ForEach(x =>
            {
                AnimToRoot.Add(GlobalState.Animation.GetObjectAnimation(x.gameObject));
            });
            Animation = GlobalState.Animation.GetObjectAnimation(gameObject);
        }
    }
}
