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

        public float stiffness;
        public bool IsGoal;
        public bool ShowCurve;
        public Vector3 LowerAngleBound;
        public Vector3 UpperAngleBound;
        public Renderer MeshRenderer;
        public Collider goalCollider;

        public void SetPathToRoot(SkinMeshController controller, List<Transform> path)
        {
            path.ForEach(x =>
            {
                AnimationSet anim = GlobalState.Animation.GetObjectAnimation(x.gameObject);
                PathToRoot.Add(x);

                AnimToRoot.Add(anim);
            });
            if (PathToRoot.Count == 0) PathToRoot.Add(transform);
            Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
            RootController = controller;
        }

        public Vector3 FramePosition(int frame)
        {
            if (null == Animation) Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
            if (null == Animation) return Vector3.zero;

            AnimationSet rootAnimation = GlobalState.Animation.GetObjectAnimation(RootController.gameObject);
            Matrix4x4 trsMatrix = RootController.transform.parent.localToWorldMatrix;
            if (null != rootAnimation) trsMatrix = trsMatrix * rootAnimation.GetTranformMatrix(frame);
            else trsMatrix = trsMatrix * Matrix4x4.TRS(RootController.transform.localPosition, RootController.transform.localRotation, RootController.transform.localScale);

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

        public void UseGoal(bool state)
        {
            ShowRenderer(state);
            UseCollider(state);
        }

        private void ShowRenderer(bool state)
        {
            if (null != MeshRenderer) MeshRenderer.enabled = state;
        }
        private void UseCollider(bool state)
        {
            if (null != goalCollider) goalCollider.enabled = state;
        }
    }
}
