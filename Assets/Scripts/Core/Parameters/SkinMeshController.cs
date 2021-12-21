using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class SkinMeshController : ParametersController
    {

        public SkinnedMeshRenderer SkinMesh;
        public Transform RootObject;
        public BoxCollider Collider;

        public void Start()
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
            {

                GlobalState.Animation.onFrameEvent.AddListener(RefreshCollider);
                RefreshCollider(GlobalState.Animation.CurrentFrame);
            }
            else
            {
                GlobalStateTradi.Animation.onFrameEvent.AddListener(RefreshCollider);
                RefreshCollider(GlobalStateTradi.Animation.CurrentFrame);
            }

        }

        public void RefreshCollider(int frame)
        {
            Collider.center = RootObject.localPosition + SkinMesh.localBounds.center;
            Collider.size = SkinMesh.localBounds.size;
        }

        public void OnDisable()
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
                GlobalState.Animation.onFrameEvent.RemoveListener(RefreshCollider);
            else
                GlobalStateTradi.Animation.onFrameEvent.RemoveListener(RefreshCollider);

        }

        internal void GetKeyList(SortedList<int, List<Dopesheet.AnimKey>> keys)
        {
            List<Curve> childCurves = new List<Curve>();
            RecursiveAnimation(childCurves, transform);

            childCurves.ForEach(curve =>
            {
                foreach (AnimationKey key in curve.keys)
                {
                    if (!keys.TryGetValue(key.frame, out List<Dopesheet.AnimKey> keylist))
                    {
                        keylist = new List<Dopesheet.AnimKey>();
                        keys[key.frame] = keylist;
                    }
                    keylist.Add(new Dopesheet.AnimKey(key.value, key.interpolation));
                }
            });
        }

        private void RecursiveAnimation(List<Curve> curves, Transform target)
        {
            AnimationSet anim = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (null != anim) curves.Add(anim.GetCurve(AnimatableProperty.PositionX));
            foreach (Transform child in target)
            {
                RecursiveAnimation(curves, child);
            }
        }
    }
}